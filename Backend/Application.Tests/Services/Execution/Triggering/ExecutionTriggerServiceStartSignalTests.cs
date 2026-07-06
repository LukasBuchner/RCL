using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ApplicationProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Triggering;

/// <summary>
///     Tests for the reactive start-signal subscription mechanism in <see cref="ExecutionTriggerService"/>.
///     Verifies that start prerequisites are detected via <c>CombineLatest</c> reactive streams
///     rather than imperative polling, and that guard checks (already started, router branch filtering)
///     are applied correctly.
/// </summary>
public class ExecutionTriggerServiceStartSignalTests
{
    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionCoordinator> _mockCoordinator;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<IRouterEvaluationService> _mockRouterEvaluationService;
    private readonly ExecutionTriggerService _service;
    private readonly List<Guid> _triggeredSkillIds = new();

    public ExecutionTriggerServiceStartSignalTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockRouterEvaluationService = new Mock<IRouterEvaluationService>();
        _eventSubject = new Subject<ExecutionEvent>();

        _mockEventBus.Setup(x => x.AllEvents).Returns(_eventSubject);
        _mockEventBus.Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => _eventSubject.OnNext(e));

        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, _, _, _, _) =>
                _triggeredSkillIds.Add(skillId))
            .Returns(Observable.Empty<ApplicationProgress>());

        var branchNavigator = new RouterBranchNavigator();

        var skillTriggerHandler = new SkillTriggerHandler(
            _mockEventBus.Object,
            _mockCoordinator.Object,
            _mockAgentProvider.Object,
            NullLogger<SkillTriggerHandler>.Instance,
            NullLogger<PipelineEvents>.Instance);

        var routerTriggerHandler = new RouterTriggerHandler(
            _mockEventBus.Object,
            _mockRouterEvaluationService.Object,
            branchNavigator,
            NullLogger<RouterTriggerHandler>.Instance);

        _service = new ExecutionTriggerService(
            _mockEventBus.Object,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            NullLogger<ExecutionTriggerService>.Instance,
            NullLogger<PipelineEvents>.Instance);
    }

    /// <summary>
    ///     Verifies that a skill with no start prerequisites is triggered immediately during
    ///     <see cref="ExecutionTriggerService.Start"/> via the synchronous emission of
    ///     <c>Observable.Return(Unit.Default)</c>.
    /// </summary>
    [Fact]
    public void ImmediateStart_NoPrerequisites_TriggersNodeImmediately()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skillNode = CreateSkillNode(skillId, "ImmediateSkill");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act
        _service.Start(dependencyGraph, new List<Node> { skillNode }, null);

        // Assert — triggered synchronously (no delay needed)
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a skill with a single start prerequisite is triggered when
    ///     the required event fires on the event bus.
    /// </summary>
    [Fact]
    public void SingleStartPrerequisite_MetByEvent_TriggersNode()
    {
        // Arrange
        var predecessorId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var predecessorNode = CreateSkillNode(predecessorId, "Predecessor");
        var skillNode = CreateSkillNode(skillId, "Dependent");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [predecessorId] = new()
                {
                    SkillId = predecessorId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = predecessorId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { predecessorNode, skillNode }, null);

        // Not triggered yet
        Assert.DoesNotContain(skillId, _triggeredSkillIds);

        // Act — fire the prerequisite event
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = predecessorId,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a skill with multiple start prerequisites is only triggered
    ///     when all of them have fired.
    /// </summary>
    [Fact]
    public void MultipleStartPrerequisites_AllMet_TriggersNode()
    {
        // Arrange
        var pred1Id = Guid.NewGuid();
        var pred2Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var pred1Node = CreateSkillNode(pred1Id, "Pred1");
        var pred2Node = CreateSkillNode(pred2Id, "Pred2");
        var skillNode = CreateSkillNode(skillId, "Dependent");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [pred1Id] = new()
                {
                    SkillId = pred1Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [pred2Id] = new()
                {
                    SkillId = pred2Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = pred1Id,
                            RequiredEventType = EventTriggerType.Finish
                        },
                        new()
                        {
                            DependencySkillId = pred2Id,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { pred1Node, pred2Node, skillNode }, null);

        // Fire first prerequisite — not yet triggered
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = pred1Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });
        Assert.DoesNotContain(skillId, _triggeredSkillIds);

        // Act — fire second prerequisite
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = pred2Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a node is triggered at most once even if prerequisite events
    ///     fire multiple times, thanks to the <c>Take(1)</c> on the combined signal.
    /// </summary>
    [Fact]
    public void DuplicateEvents_NodeTriggeredOnlyOnce()
    {
        // Arrange
        var predecessorId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var predecessorNode = CreateSkillNode(predecessorId, "Predecessor");
        var skillNode = CreateSkillNode(skillId, "Dependent");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [predecessorId] = new()
                {
                    SkillId = predecessorId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = predecessorId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { predecessorNode, skillNode }, null);

        // Act — fire the prerequisite event multiple times
        var finishEvent = new ExecutionEvent
        {
            SkillId = predecessorId,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        };
        _eventSubject.OnNext(finishEvent);
        _eventSubject.OnNext(finishEvent);
        _eventSubject.OnNext(finishEvent);

        // Assert — triggered exactly once
        Assert.Single(_triggeredSkillIds, id => id == skillId);
    }

    /// <summary>
    ///     Verifies that a node is not triggered when only a partial subset of
    ///     its start prerequisites have fired.
    /// </summary>
    [Fact]
    public void PartialPrerequisites_NodeNotTriggered()
    {
        // Arrange
        var pred1Id = Guid.NewGuid();
        var pred2Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var pred1Node = CreateSkillNode(pred1Id, "Pred1");
        var pred2Node = CreateSkillNode(pred2Id, "Pred2");
        var skillNode = CreateSkillNode(skillId, "Dependent");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [pred1Id] = new()
                {
                    SkillId = pred1Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [pred2Id] = new()
                {
                    SkillId = pred2Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = pred1Id,
                            RequiredEventType = EventTriggerType.Finish
                        },
                        new()
                        {
                            DependencySkillId = pred2Id,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { pred1Node, pred2Node, skillNode }, null);

        // Act — fire only the first prerequisite
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = pred1Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert — dependent skill should NOT be triggered
        Assert.DoesNotContain(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that router branch filtering still works with reactive start signals:
    ///     only the skill in the selected branch is triggered after the router Finish event fires.
    /// </summary>
    [Fact]
    public async Task RouterBranchFiltering_OnlySelectedBranchTriggered()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var selectedSkillId = Guid.NewGuid();
        var nonSelectedSkillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "TestRouter");
        // Branch skills must be inside the router hierarchy for branch filtering to apply
        var selectedSkill = CreateSkillNode(selectedSkillId, "SelectedSkill");
        selectedSkill.ParentId = routerId;
        var nonSelectedSkill = CreateSkillNode(nonSelectedSkillId, "NonSelectedSkill");
        nonSelectedSkill.ParentId = routerId;

        var nodes = new List<Node> { routerNode, selectedSkill, nonSelectedSkill };

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [selectedSkillId] = new()
                {
                    SkillId = selectedSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [nonSelectedSkillId] = new()
                {
                    SkillId = nonSelectedSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(selectedSkillId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(150); // Allow async router evaluation

        // Assert — only selected branch triggered
        Assert.Contains(selectedSkillId, _triggeredSkillIds);
        Assert.DoesNotContain(nonSelectedSkillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a branch-internal skill with a Start prerequisite on a router is triggered
    ///     when the router's <c>await</c> doesn't yield (completed Task from <c>ReturnsAsync</c>).
    ///     The router publishes its Start event synchronously during <c>Start()</c>, which triggers
    ///     the skill's CombineLatest via <c>OnStartPrerequisitesMet</c>.
    /// </summary>
    [Fact]
    public void SkillWithStartPrerequisiteOnRouter_SynchronousEvaluation_TriggersSkill()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "GateRouter");
        var skillNode = CreateSkillNode(skillId, "GatedSkill");

        var nodes = new List<Node> { routerNode, skillNode };

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // ReturnsAsync gives a completed Task — await doesn't yield,
        // so TriggerRouter runs entirely synchronously inside Start().
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(skillId);

        // Override coordinator mock: the router considers the selected target a branch skill
        // and waits for its Finish event before publishing Router.Finish. The mock must publish
        // Start+Finish events so the router's branch-completion signal resolves.
        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((id, _, _, _, _) =>
            {
                _triggeredSkillIds.Add(id);
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<ApplicationProgress>();
            });

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);

        // Assert — skill must be triggered (router Start event fires synchronously during Start)
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Same scenario as <see cref="SkillWithStartPrerequisiteOnRouter_SynchronousEvaluation_TriggersSkill"/>
    ///     but with a truly async router evaluation (Task.Yield forces thread-pool continuation).
    ///     The Start event fires on a background thread after <c>Start()</c> returns.
    /// </summary>
    [Fact]
    public async Task SkillWithStartPrerequisiteOnRouter_AsyncEvaluation_TriggersSkill()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "GateRouter");
        var skillNode = CreateSkillNode(skillId, "GatedSkill");

        var nodes = new List<Node> { routerNode, skillNode };

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Use a gate to control when the async router evaluation completes
        var gate = new TaskCompletionSource<bool>();

        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .Returns(async () =>
            {
                await gate.Task;
                return skillId;
            });

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);

        // Not triggered yet — router evaluation is blocked on the gate
        Assert.DoesNotContain(skillId, _triggeredSkillIds);

        // Release the gate and allow router evaluation to complete
        gate.SetResult(true);
        await Task.Delay(200);

        // Assert — skill triggered after router Start event fired
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Reproduces the exact bug scenario: a router has branch-target skills, and a separate
    ///     external skill has a Finish-Start (FS) dependency on the router. The external skill
    ///     must NOT start until the router has completed evaluation. Without the two-pass
    ///     subscription ordering, the external skill starts immediately because the router
    ///     fires synchronously during <c>Start()</c> and the Finish event is published before
    ///     the external skill's reactive subscription exists.
    /// </summary>
    [Fact]
    public void ExternalSkillWithFSToRouter_DoesNotStartUntilRouterCompletes()
    {
        // Arrange — Router "R" with two branch skills (inside router hierarchy),
        // plus an external skill "E" with FS to R (outside router hierarchy).
        var routerId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid(); // TaskNode container for branch A
        var branchBTaskId = Guid.NewGuid(); // TaskNode container for branch B
        var branchASkillId = Guid.NewGuid();
        var branchBSkillId = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "Router R");
        // Branch task containers are children of the router
        var branchATask = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Branch A",
                Description = "Branch A",
                StartTime = 0,
                Duration = 5
            }
        };
        var branchBTask = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Branch B",
                Description = "Branch B",
                StartTime = 0,
                Duration = 5
            }
        };
        // Branch skills are children of their respective branch TaskNodes (inside router hierarchy)
        var branchASkill = CreateSkillNode(branchASkillId, "BranchA Skill");
        branchASkill.ParentId = branchATaskId;
        var branchBSkill = CreateSkillNode(branchBSkillId, "BranchB Skill");
        branchBSkill.ParentId = branchBTaskId;
        // External skill has no ParentId (outside router hierarchy)
        var externalSkill = CreateSkillNode(externalSkillId, "External Skill");

        var nodes = new List<Node> { routerNode, branchATask, branchBTask, branchASkill, branchBSkill, externalSkill };

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // Branch A skill depends on router Start (selected branch, branch-internal)
                [branchASkillId] = new()
                {
                    SkillId = branchASkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // Branch B skill depends on router Start (non-selected branch, branch-internal)
                [branchBSkillId] = new()
                {
                    SkillId = branchBSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // External skill depends on router Finish (FS dependency, external to router)
                [externalSkillId] = new()
                {
                    SkillId = externalSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Override coordinator mock: publish Start+Finish events so the router can detect
        // branch completion and publish its own Finish event (needed for external skill to trigger)
        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, _, _, _, _) =>
            {
                _triggeredSkillIds.Add(skillId);
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = skillId,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = skillId,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<ApplicationProgress>();
            });

        // Router selects Branch A (targets the TaskNode container, not the skill directly)
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(branchATaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);

        // Assert — external skill must be triggered (it has FS to router, router completed)
        Assert.Contains(externalSkillId, _triggeredSkillIds);

        // Assert — only the selected branch skill is triggered
        Assert.Contains(branchASkillId, _triggeredSkillIds);
        Assert.DoesNotContain(branchBSkillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a Start event type prerequisite (not just Finish) is correctly
    ///     detected by the reactive start signal.
    /// </summary>
    [Fact]
    public void StartEventPrerequisite_TriggersOnStartEvent()
    {
        // Arrange
        var predecessorId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var predecessorNode = CreateSkillNode(predecessorId, "Predecessor");
        var skillNode = CreateSkillNode(skillId, "Dependent");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [predecessorId] = new()
                {
                    SkillId = predecessorId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillId] = new()
                {
                    SkillId = skillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = predecessorId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { predecessorNode, skillNode }, null);

        // Act — fire a Start event (not Finish)
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = predecessorId,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert
        Assert.Contains(skillId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that the service can be reused for consecutive executions.
    ///     After <c>Start()</c> → <c>Stop()</c>, a second <c>Start()</c> must succeed
    ///     and correctly trigger nodes. This is critical because the service is registered
    ///     as a Singleton and the orchestrator calls <c>Stop()</c> in its <c>finally</c> block.
    /// </summary>
    [Fact]
    public void ConsecutiveExecutions_SecondStartSucceeds()
    {
        // Arrange — first execution
        var skillId1 = Guid.NewGuid();
        var skillNode1 = CreateSkillNode(skillId1, "FirstExecution");

        var graph1 = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillId1] = new()
                {
                    SkillId = skillId1,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act — first execution cycle
        _service.Start(graph1, new List<Node> { skillNode1 }, null);
        Assert.Contains(skillId1, _triggeredSkillIds);

        _service.StopMonitoring();

        // Arrange — second execution with a fresh graph and node
        var skillId2 = Guid.NewGuid();
        var skillNode2 = CreateSkillNode(skillId2, "SecondExecution");

        var graph2 = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillId2] = new()
                {
                    SkillId = skillId2,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act — second execution must not log "already started"
        _service.Start(graph2, new List<Node> { skillNode2 }, null);

        // Assert — second execution triggered correctly
        Assert.Contains(skillId2, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that when a router is involved in the first execution, the service still
    ///     resets cleanly and a second execution succeeds. This tests that the <c>async void</c>
    ///     <c>TriggerRouter</c> continuation (which may still be awaiting branch completion) does
    ///     not prevent state reset when <c>Stop()</c> is called.
    /// </summary>
    [Fact]
    public async Task ConsecutiveExecutions_WithRouter_SecondStartSucceeds()
    {
        // Arrange — first execution with a router
        var routerId = Guid.NewGuid();
        var branchSkillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "Router1");
        var branchSkill = CreateSkillNode(branchSkillId, "BranchSkill");
        branchSkill.ParentId = routerId;

        var graph1 = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [branchSkillId] = new()
                {
                    SkillId = branchSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Router evaluation is truly async — TriggerRouter will be stuck awaiting branch completion
        // because the default mock doesn't publish Finish events
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .Returns(async () =>
            {
                await Task.Yield();
                return branchSkillId;
            });

        // Act — first execution
        _service.Start(graph1, new List<Node> { routerNode, branchSkill }, variableContext);
        await Task.Delay(200); // Let the async router evaluation run

        // StopMonitoring while TriggerRouter is potentially still awaiting branchCompletionTask
        _service.StopMonitoring();

        // Arrange — second execution (simple, no router)
        var skillId2 = Guid.NewGuid();
        var skillNode2 = CreateSkillNode(skillId2, "SecondExecution");

        var graph2 = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillId2] = new()
                {
                    SkillId = skillId2,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act — second execution must work
        _service.Start(graph2, new List<Node> { skillNode2 }, null);

        // Assert
        Assert.Contains(skillId2, _triggeredSkillIds);
    }

    /// <summary>
    ///     Reproduces the production bug: a procedure with a router completes all skills,
    ///     but the router's <c>TriggerRouter</c> never publishes its Finish event because
    ///     the <c>branchCompletionTask</c> waits for skill Finish events that were published
    ///     BEFORE the router's branch-completion subscription was created. This causes the
    ///     orchestrator's <c>_executionCompletion</c> to never resolve (if it depends on
    ///     router Finish), and <c>Stop()</c> is never called, leaving <c>_isStarted = true</c>.
    ///     A second <c>Start()</c> then returns immediately with "already started".
    /// </summary>
    [Fact]
    public async Task RouterWithCompletedBranchSkill_RouterFinishesAndAllowsSecondExecution()
    {
        // Arrange — Router selects a branch skill. The skill completes (publishes Start+Finish)
        // during the router's TriggerRouter flow. If the branch-completion subscription is set
        // up AFTER the skill's Finish event fires, the router hangs forever.
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var branchSkillId = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "ProcedureRouter");
        var branchTask = new TaskNode
        {
            Id = branchTaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchTask",
                Description = "Branch container",
                StartTime = 0,
                Duration = 5
            }
        };
        var branchSkill = CreateSkillNode(branchSkillId, "BranchSkill");
        branchSkill.ParentId = branchTaskId;
        var externalSkill = CreateSkillNode(externalSkillId, "ExternalSkill");

        var nodes = new List<Node> { routerNode, branchTask, branchSkill, externalSkill };
        var variableContext = new VariableContext();

        // The coordinator mock publishes Start+Finish for skills (simulating real execution)
        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((id, _, _, _, _) =>
            {
                _triggeredSkillIds.Add(id);
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<ApplicationProgress>();
            });

        // Router evaluation returns the branch TaskNode (async to simulate real I/O)
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .Returns(async () =>
            {
                await Task.Yield();
                return branchTaskId;
            });

        var graph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [branchSkillId] = new()
                {
                    SkillId = branchSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [externalSkillId] = new()
                {
                    SkillId = externalSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = routerId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act — first execution
        _service.Start(graph, nodes, variableContext);

        // Wait for async router evaluation + branch skill execution + router finish
        await Task.Delay(500);

        // Assert — branch skill and external skill must both be triggered.
        // If the router hangs, the external skill (FS dependency on router) never fires.
        Assert.Contains(branchSkillId, _triggeredSkillIds);
        Assert.Contains(externalSkillId, _triggeredSkillIds);

        // The router must have published its Finish event, proving it didn't hang

        // Now stop and verify second execution works
        _service.StopMonitoring();

        var skillId2 = Guid.NewGuid();
        var skillNode2 = CreateSkillNode(skillId2, "SecondExecution");
        var graph2 = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillId2] = new()
                {
                    SkillId = skillId2,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(graph2, new List<Node> { skillNode2 }, null);
        Assert.Contains(skillId2, _triggeredSkillIds);
    }

    /// <summary>
    ///     The safety + liveness case for an empty task between two skills (A → Task(empty) → B).
    ///     The empty task is a leafless firing endpoint: B must not start until the task fires, and the task
    ///     must not fire until A finishes. After A's Finish, the task publishes its own Start+Finish
    ///     (zero-extent endpoint) and B is released — no deadlock.
    /// </summary>
    [Fact]
    public void EmptyTaskBetweenSkills_GatesDependentThroughTheTask()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var emptyTaskId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();

        var skillA = CreateSkillNode(skillAId, "A");
        var emptyTask = CreateTaskNode(emptyTaskId, "EmptyTask");
        var skillB = CreateSkillNode(skillBId, "B");

        var graph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [emptyTaskId] = new()
                {
                    SkillId = emptyTaskId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new() { DependencySkillId = skillAId, RequiredEventType = EventTriggerType.Finish }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new() { DependencySkillId = emptyTaskId, RequiredEventType = EventTriggerType.Finish }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var busEvents = new List<ExecutionEvent>();
        _eventSubject.Subscribe(busEvents.Add);

        // Act — the default coordinator mock dispatches A but does not publish A's events.
        _service.Start(graph, new List<Node> { skillA, emptyTask, skillB }, null);

        // Assert (safety) — A is dispatched, but with no A.Finish the empty task has not fired, so B is blocked.
        Assert.Contains(skillAId, _triggeredSkillIds);
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);

        // Act — fire A's Finish, which releases the empty task.
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillAId,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert (liveness) — the empty task fired its own Start+Finish, and B is now dispatched (no deadlock).
        Assert.Contains(busEvents, e => e.SkillId == emptyTaskId && e.EventType == ExecutionEventType.Start);
        Assert.Contains(busEvents, e => e.SkillId == emptyTaskId && e.EventType == ExecutionEventType.Finish);
        Assert.Contains(skillBId, _triggeredSkillIds);
    }

    /// <summary>
    ///     When a router selects an empty branch, it must finish instantly via the skip-on-empty path
    ///     (the branch has no executable nodes to await), so an external skill with an FS dependency on the
    ///     router fires and the empty branch task fires as a leafless endpoint — no hang. This guards against
    ///     making the router await leafless tasks.
    /// </summary>
    [Fact]
    public async Task RouterSelectsEmptyBranch_DoesNotHang_ExternalSkillAndEmptyTaskFire()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var emptyBranchTaskId = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();

        var router = CreateRouterNode(routerId, "Router");
        var emptyBranchTask = CreateTaskNode(emptyBranchTaskId, "EmptyBranch");
        emptyBranchTask.ParentId = routerId;
        var externalSkill = CreateSkillNode(externalSkillId, "External");

        var nodes = new List<Node> { router, emptyBranchTask, externalSkill };

        var graph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [emptyBranchTaskId] = new()
                {
                    SkillId = emptyBranchTaskId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new() { DependencySkillId = routerId, RequiredEventType = EventTriggerType.Start }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [externalSkillId] = new()
                {
                    SkillId = externalSkillId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new() { DependencySkillId = routerId, RequiredEventType = EventTriggerType.Finish }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();
        var busEvents = new List<ExecutionEvent>();
        _eventSubject.Subscribe(busEvents.Add);

        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(emptyBranchTaskId);

        // Act
        _service.Start(graph, nodes, variableContext);
        await Task.Delay(150);

        // Assert — the router finished instantly (skip-on-empty), releasing the external FS dependent.
        Assert.Contains(busEvents, e => e.SkillId == routerId && e.EventType == ExecutionEventType.Finish);
        Assert.Contains(externalSkillId, _triggeredSkillIds);

        // The empty branch task fired as a leafless endpoint (gated on Router.Start, selected branch).
        Assert.Contains(busEvents, e => e.SkillId == emptyBranchTaskId && e.EventType == ExecutionEventType.Finish);
    }

    #region Helper Methods

    private static SkillExecutionNode CreateSkillNode(Guid id, string skillName)
    {
        var agentId = Guid.NewGuid();
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                StartTime = 0,
                Duration = 5.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = $"Skill: {skillName}",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };
    }

    private static RouterNode CreateRouterNode(Guid id, string name)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                Description = $"Router: {name}",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "status" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "BranchA",
                        TargetNodeId = Guid.NewGuid(),
                        Priority = 1
                    }
                }
            }
        };
    }

    private static TaskNode CreateTaskNode(Guid id, string name)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = name,
                Description = $"Task: {name}",
                StartTime = 0,
                Duration = 0
            }
        };
    }

    #endregion
}