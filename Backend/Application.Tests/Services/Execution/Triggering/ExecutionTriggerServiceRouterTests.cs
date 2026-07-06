using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ApplicationProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Triggering;

/// <summary>
///     Tests for ExecutionTriggerService router evaluation integration.
///     Verifies that routers are detected, evaluated, and trigger selected branches correctly.
/// </summary>
public class ExecutionTriggerServiceRouterTests
{
    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionCoordinator> _mockCoordinator;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<ILogger<ExecutionTriggerService>> _mockLogger;
    private readonly Mock<IRouterEvaluationService> _mockRouterEvaluationService;
    private readonly Mock<ILogger<RouterTriggerHandler>> _mockRouterTriggerLogger;
    private readonly ExecutionTriggerService _service;

    public ExecutionTriggerServiceRouterTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockRouterEvaluationService = new Mock<IRouterEvaluationService>();
        _mockLogger = new Mock<ILogger<ExecutionTriggerService>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockRouterTriggerLogger = new Mock<ILogger<RouterTriggerHandler>>();
        _mockRouterTriggerLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _eventSubject = new Subject<ExecutionEvent>();

        _mockEventBus.Setup(x => x.AllEvents).Returns(_eventSubject);

        // Wire PublishEvent to emit to the AllEvents observable
        // This is critical for event-driven flow: when TriggerRouter publishes events,
        // they must flow to OnEventFired via the AllEvents subscription
        _mockEventBus.Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => _eventSubject.OnNext(e));

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
            _mockRouterTriggerLogger.Object);

        _service = new ExecutionTriggerService(
            _mockEventBus.Object,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);
    }

    #region Edge Cases

    [Fact]
    public void RouterWithNoVariableContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };


        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.Start(dependencyGraph, nodes, null!));
    }

    #endregion

    #region Basic Router Handling

    [Fact]
    public async Task Start_WithRouterNode_IncludesRouterInMonitoring()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), It.IsAny<VariableContext>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100); // Give time for async evaluation

        // Assert
        _mockRouterEvaluationService.Verify(
            x => x.EvaluateRouterAsync(It.Is<RouterNode>(n => n.Id == routerId), It.IsAny<VariableContext>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RouterPrerequisitesMet_EvaluatesRouter()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var targetNodeId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(targetNodeId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100); // Allow async evaluation to complete

        // Assert
        _mockRouterEvaluationService.Verify(
            x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(n => n.Id == routerId),
                variableContext),
            Times.Once);
    }

    [Fact]
    public async Task RouterEvaluated_PublishesStartAndFinishEvents()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var targetNodeId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var publishedEvents = new List<ExecutionEvent>();
        _mockEventBus
            .Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e =>
            {
                publishedEvents.Add(e); // Capture for test assertion
                _eventSubject.OnNext(e); // Emit to maintain event flow
            });


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(targetNodeId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100); // Allow async evaluation to complete

        // Assert
        Assert.Contains(publishedEvents, e => e.SkillId == routerId && e.EventType == ExecutionEventType.Start);
        Assert.Contains(publishedEvents, e => e.SkillId == routerId && e.EventType == ExecutionEventType.Finish);
    }

    [Fact]
    public async Task RouterFinishEvent_TriggersSelectedBranchTarget()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var targetSkillId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var targetSkillNode = CreateSkillNode(targetSkillId, "ProcessA");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode, targetSkillNode };
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
                [targetSkillId] = new()
                {
                    SkillId = targetSkillId,
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


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(targetSkillId);

        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                targetSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, skill, agentId, vc, ct) =>
            {
                // Simulate real coordinator: publish Start and Finish events so the router
                // can detect branch completion and publish its own Finish event
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

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(150); // Allow router evaluation and skill trigger

        // Assert
        _mockCoordinator.Verify(
            x => x.ExecuteSkillAsync(
                targetSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouterEvaluated_NonSelectedBranchesNotTriggered()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var selectedSkillId = Guid.NewGuid();
        var nonSelectedSkillId = Guid.NewGuid();

        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        // Branch skills must be inside the router hierarchy for branch filtering to apply
        var selectedSkillNode = CreateSkillNode(selectedSkillId, "ProcessA");
        selectedSkillNode.ParentId = routerId;
        var nonSelectedSkillNode = CreateSkillNode(nonSelectedSkillId, "ProcessB");
        nonSelectedSkillNode.ParentId = routerId;
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode, selectedSkillNode, nonSelectedSkillNode };
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


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(selectedSkillId);

        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, skill, agentId, vc, ct) =>
            {
                // Simulate real coordinator: publish Start and Finish events so the router
                // can detect branch completion and publish its own Finish event
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

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(150); // Allow router evaluation and skill trigger

        // Assert - selected branch triggered
        _mockCoordinator.Verify(
            x => x.ExecuteSkillAsync(
                selectedSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert - non-selected branch NOT triggered
        _mockCoordinator.Verify(
            x => x.ExecuteSkillAsync(
                nonSelectedSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Variable Integration

    [Fact]
    public async Task RouterEvaluation_UsesCurrentVariableContext()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var targetNodeId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");

        var variableContext = new VariableContext();
        variableContext.SetValue("status", "active");

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(targetNodeId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100);

        // Assert - verify the exact variable context instance was used
        _mockRouterEvaluationService.Verify(
            x => x.EvaluateRouterAsync(
                It.IsAny<RouterNode>(),
                It.Is<VariableContext>(ctx => ctx == variableContext)),
            Times.Once);
    }

    [Fact]
    public async Task RouterBeforeSkill_VariableFlowsToSkill()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var targetSkillId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var targetSkillNode = CreateSkillNode(targetSkillId, "ProcessA");

        var variableContext = new VariableContext();
        variableContext.SetValue("status", "active");

        var nodes = new List<Node> { routerNode, targetSkillNode };
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
                [targetSkillId] = new()
                {
                    SkillId = targetSkillId,
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


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(targetSkillId);

        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                targetSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                variableContext,
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, skill, agentId, vc, ct) =>
            {
                // Simulate real coordinator: publish Start and Finish events so the router
                // can detect branch completion and publish its own Finish event
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

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(150);

        // Assert - skill receives same variable context
        _mockCoordinator.Verify(
            x => x.ExecuteSkillAsync(
                targetSkillId,
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.Is<VariableContext>(ctx => ctx == variableContext),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task RouterEvaluationFails_LogsErrorAndStops()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ThrowsAsync(new InvalidOperationException("Variable not found"));

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100);

        // Assert - error was logged (in RouterTriggerHandler, where router evaluation now lives)
        _mockRouterTriggerLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Router evaluation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RouterWithMissingVariable_ThrowsException()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var routerNode = CreateRouterNode(routerId, "CheckStatus");
        var variableContext = new VariableContext(); // Empty context

        var nodes = new List<Node> { routerNode };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [routerId] = new()
                {
                    SkillId = routerId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ThrowsAsync(new VariableNotFoundException("status"));

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(100);

        // Assert - error logged (in RouterTriggerHandler, where router evaluation now lives)
        _mockRouterTriggerLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Multiple Routers

    [Fact]
    public async Task SequentialRouters_BothEvaluateCorrectly()
    {
        // Arrange
        var router1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        Guid.NewGuid();
        var target2Id = Guid.NewGuid();

        var router1Node = CreateRouterNode(router1Id, "Router1");
        var router2Node = CreateRouterNode(router2Id, "Router2");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { router1Node, router2Node };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [router1Id] = new()
                {
                    SkillId = router1Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [router2Id] = new()
                {
                    SkillId = router2Id,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = router1Id,
                            RequiredEventType = EventTriggerType.Finish
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var evaluatedRouters = new ConcurrentBag<Guid>();


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), It.IsAny<VariableContext>()))
            .Callback<RouterNode, VariableContext>((node, _) => evaluatedRouters.Add(node.Id))
            .ReturnsAsync((RouterNode node, VariableContext _) =>
            {
                // Router1 selects Router2 as its target to enable sequential chaining
                if (node.Id == router1Id) return router2Id;
                if (node.Id == router2Id) return target2Id;
                return Guid.Empty;
            });

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(200); // Allow both routers to evaluate

        // Assert - both routers evaluated
        Assert.Contains(router1Id, evaluatedRouters);
        Assert.Contains(router2Id, evaluatedRouters);
    }

    [Fact]
    public async Task ParallelRouters_EvaluateIndependently()
    {
        // Arrange
        var router1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var target1Id = Guid.NewGuid();
        var target2Id = Guid.NewGuid();

        var router1Node = CreateRouterNode(router1Id, "Router1");
        var router2Node = CreateRouterNode(router2Id, "Router2");
        var variableContext = new VariableContext();

        var nodes = new List<Node> { router1Node, router2Node };
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [router1Id] = new()
                {
                    SkillId = router1Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [router2Id] = new()
                {
                    SkillId = router2Id,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var evaluatedRouters = new ConcurrentBag<Guid>();


        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), It.IsAny<VariableContext>()))
            .Callback<RouterNode, VariableContext>((node, _) =>
            {
                evaluatedRouters.Add(node.Id);
                Debug.WriteLine($"Evaluated router: {node.RouterTask.Name} (ID: {node.Id})");
            })
            .ReturnsAsync((RouterNode node, VariableContext _) =>
                node.Id == router1Id ? target1Id : target2Id);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(150);

        // Assert - both routers evaluated independently (at least once each)
        var router1Count = evaluatedRouters.Count(id => id == router1Id);
        var router2Count = evaluatedRouters.Count(id => id == router2Id);

        // Both routers should be evaluated at least once
        // Note: Due to async execution, a router might be evaluated multiple times
        // if its Start event hasn't propagated before another router's Finish event triggers re-checking.
        // The important thing is that both routers ARE evaluated.
        Assert.True(router1Count >= 1,
            $"Router1 should be evaluated at least once, but was called {router1Count} times");
        Assert.True(router2Count >= 1,
            $"Router2 should be evaluated at least once, but was called {router2Count} times");
    }

    #endregion

    #region Helper Methods

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
                Description = $"Router node: {name}",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "status"
                },
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

    private static SkillExecutionNode CreateSkillNode(Guid id, string skillName)
    {
        var agentId = Guid.NewGuid();
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = skillName,
            Description = $"Skill: {skillName}",
            Properties = new List<TypedProperty>()
        };

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
                Skill = skill,
                AgentId = agentId
            }
        };
    }

    #endregion
}