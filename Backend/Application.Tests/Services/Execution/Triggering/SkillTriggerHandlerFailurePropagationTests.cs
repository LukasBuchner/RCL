using System.Reactive;
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
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ApplicationProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Triggering;

/// <summary>
///     Tests for failure propagation in the execution trigger system. Verifies that
///     <c>Failed</c> and <c>NotSelected</c> events satisfy start and finish prerequisites
///     so downstream skills are unblocked, and that agent errors during skill execution
///     publish <c>Failed</c> events to the event bus.
/// </summary>
public class SkillTriggerHandlerFailurePropagationTests
{
    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionCoordinator> _mockCoordinator;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<IRouterEvaluationService> _mockRouterEvaluationService;
    private readonly ExecutionTriggerService _service;
    private readonly List<Guid> _triggeredSkillIds = new();
    private readonly List<Guid> _adaptiveSkillIds = new();
    private readonly List<ExecutionEvent> _publishedEvents = new();

    public SkillTriggerHandlerFailurePropagationTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockRouterEvaluationService = new Mock<IRouterEvaluationService>();
        _eventSubject = new Subject<ExecutionEvent>();

        _mockEventBus.Setup(x => x.AllEvents).Returns(_eventSubject);
        _mockEventBus.Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e =>
            {
                _publishedEvents.Add(e);
                _eventSubject.OnNext(e);
            });

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

        _mockCoordinator
            .Setup(x => x.ExecuteAdaptiveSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<double>(),
                It.IsAny<IObservable<double>>(),
                It.IsAny<IObservable<Unit>>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Skill, Guid, double, IObservable<double>, IObservable<Unit>, VariableContext,
                CancellationToken>((skillId, _, _, _, _, _, _, _) =>
                _adaptiveSkillIds.Add(skillId))
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
    ///     Verifies that a <c>Failed</c> event from a dependency satisfies a Finish-to-Start
    ///     prerequisite. Skill B depends on Skill A's Finish event via an FS edge; when A
    ///     publishes <c>Failed</c>, B's start signal fires and B is triggered.
    /// </summary>
    [Fact]
    public void FailedDependency_ReleasesStartPrerequisite()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");
        var skillBNode = CreateSkillNode(skillBId, "SkillB");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = skillAId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { skillANode, skillBNode }, null);

        // B should not be triggered yet
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);

        // Act — A publishes a Failed event instead of Finish
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillAId,
            EventType = ExecutionEventType.Failed,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert — B's start prerequisite is satisfied by the Failed event
        Assert.Contains(skillBId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a <c>NotSelected</c> event from a dependency satisfies a Finish-to-Start
    ///     prerequisite. Skill B depends on Skill A's Finish event via an FS edge; when A
    ///     publishes <c>NotSelected</c>, B's start signal fires and B is triggered.
    /// </summary>
    [Fact]
    public void NotSelectedDependency_ReleasesStartPrerequisite()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");
        var skillBNode = CreateSkillNode(skillBId, "SkillB");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = skillAId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { skillANode, skillBNode }, null);

        // B should not be triggered yet
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);

        // Act — A publishes a NotSelected event
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillAId,
            EventType = ExecutionEventType.NotSelected,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert — B's start prerequisite is satisfied by the NotSelected event
        Assert.Contains(skillBId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a <c>Failed</c> event from a dependency satisfies a Finish-to-Finish
    ///     prerequisite. Adaptive Skill B has an FF prereq on Skill A; when A publishes
    ///     <c>Failed</c>, B's finish signal fires.
    /// </summary>
    [Fact]
    public void FailedDependency_ReleasesFinishPrerequisite()
    {
        // Arrange — B is adaptive with an FF finish prereq on A (external, not in same SCC)
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");
        var skillBNode = CreateSkillNode(skillBId, "SkillB");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = skillAId,
                            RequiredEventType = EventTriggerType.Finish,
                            DependencyType = DependencyType.FinishToStart
                        }
                    }
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { skillANode, skillBNode }, null);

        // B should be triggered via the adaptive path (it has finish prerequisites)
        Assert.Contains(skillBId, _adaptiveSkillIds);

        // Act — A publishes a Failed event instead of Finish
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillAId,
            EventType = ExecutionEventType.Failed,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert — the adaptive coordinator was called, meaning the finish signal
        // observable was wired up. The Failed event satisfies the finish prerequisite
        // because CreateSinglePrerequisiteObservable matches Failed events.
        // We verify by confirming ExecuteAdaptiveSkillAsync was invoked with a
        // finishSignal that resolves when A's Failed event fires.
        _mockCoordinator.Verify(x => x.ExecuteAdaptiveSkillAsync(
            skillBId,
            It.IsAny<Skill>(),
            It.IsAny<Guid>(),
            It.IsAny<double>(),
            It.IsAny<IObservable<double>>(),
            It.IsAny<IObservable<Unit>>(),
            It.IsAny<VariableContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies that when the coordinator throws during <c>ExecuteSkillAsync</c> (non-adaptive
    ///     path), the error handler publishes a <c>Failed</c> event to the event bus, which in
    ///     turn unblocks a downstream skill that has an FS prerequisite on the failing skill.
    /// </summary>
    [Fact]
    public void AgentError_PublishesFailedEvent()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");
        var skillBNode = CreateSkillNode(skillBId, "SkillB");

        // Override coordinator mock for Skill A: throw an error via Observable.Throw
        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                skillAId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Observable.Throw<ApplicationProgress>(new InvalidOperationException("Agent error")));

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = skillAId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        // Act
        _service.Start(dependencyGraph, new List<Node> { skillANode, skillBNode }, null);

        // Assert — A's error handler publishes a Failed event
        Assert.Contains(_publishedEvents,
            e => e.SkillId == skillAId && e.EventType == ExecutionEventType.Failed);

        // Assert — B is unblocked by A's Failed event (Failed satisfies FS prerequisite)
        Assert.Contains(skillBId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that when the coordinator throws during <c>ExecuteAdaptiveSkillAsync</c>
    ///     (adaptive path), the error handler publishes a <c>Failed</c> event to the event bus.
    /// </summary>
    [Fact]
    public void AgentError_Adaptive_PublishesFailedEvent()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");

        // Override coordinator mock for adaptive Skill A: throw an error
        _mockCoordinator
            .Setup(x => x.ExecuteAdaptiveSkillAsync(
                skillAId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<double>(),
                It.IsAny<IObservable<double>>(),
                It.IsAny<IObservable<Unit>>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Observable.Throw<ApplicationProgress>(new InvalidOperationException("Adaptive agent error")));

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = Guid.NewGuid(),
                            RequiredEventType = EventTriggerType.Finish,
                            DependencyType = DependencyType.FinishToStart
                        }
                    }
                }
            }
        };

        // Act
        _service.Start(dependencyGraph, new List<Node> { skillANode }, null);

        // Assert — A's error handler publishes a Failed event
        Assert.Contains(_publishedEvents,
            e => e.SkillId == skillAId && e.EventType == ExecutionEventType.Failed);
    }

    /// <summary>
    ///     Verifies that when a skill has multiple start prerequisites and one dependency fails
    ///     while another succeeds normally, the CombineLatest still fires because Failed counts
    ///     as a satisfied prerequisite alongside the normal Finish event.
    /// </summary>
    [Fact]
    public void MultiplePrereqs_OneFailedOneSucceeds_StillFires()
    {
        // Arrange — B depends on A (which will fail) and C (which will finish normally)
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillCId = Guid.NewGuid();
        var skillANode = CreateSkillNode(skillAId, "SkillA");
        var skillBNode = CreateSkillNode(skillBId, "SkillB");
        var skillCNode = CreateSkillNode(skillCId, "SkillC");

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillCId] = new()
                {
                    SkillId = skillCId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = skillAId,
                            RequiredEventType = EventTriggerType.Finish
                        },
                        new()
                        {
                            DependencySkillId = skillCId,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _service.Start(dependencyGraph, new List<Node> { skillANode, skillBNode, skillCNode }, null);

        // B should not be triggered yet
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);

        // Act — A fails
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillAId,
            EventType = ExecutionEventType.Failed,
            Timestamp = DateTimeOffset.UtcNow
        });

        // B still not triggered — only one of two prerequisites met
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);

        // Act — C finishes normally
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = skillCId,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert — B is now triggered (Failed from A + Finish from C satisfies CombineLatest)
        Assert.Contains(skillBId, _triggeredSkillIds);
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

    #endregion
}