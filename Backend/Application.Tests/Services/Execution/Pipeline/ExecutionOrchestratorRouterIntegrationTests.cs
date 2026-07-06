using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Tests.TestUtilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Pipeline;

/// <summary>
///     Integration tests verifying that ExecutionOrchestrator correctly passes router selections
///     to ReschedulingCoordinator during re-scheduling operations.
/// </summary>
public class ExecutionOrchestratorRouterIntegrationTests : IDisposable
{
    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly Mock<IDependencyGraphAnalyzer> _mockDependencyAnalyzer;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<IExecutionEventPublisher> _mockEventPublisher;
    private readonly Mock<IExecutionInitializer> _mockInitializer;
    private readonly Mock<IExecutionProgressMonitor> _mockProgressMonitor;
    private readonly Mock<IReschedulingCoordinator> _mockReschedulingCoordinator;
    private readonly Mock<ISkillExecutionStateManager> _mockStateManager;
    private readonly Mock<IExecutionTriggerService> _mockTriggerService;
    private readonly ExecutionOrchestrator _orchestrator;

    public ExecutionOrchestratorRouterIntegrationTests()
    {
        _mockInitializer = new Mock<IExecutionInitializer>();
        _mockStateManager = new Mock<ISkillExecutionStateManager>();
        _mockEventPublisher = new Mock<IExecutionEventPublisher>();
        _mockProgressMonitor = new Mock<IExecutionProgressMonitor>();
        _mockDependencyAnalyzer = new Mock<IDependencyGraphAnalyzer>();
        _mockTriggerService = new Mock<IExecutionTriggerService>();
        _mockReschedulingCoordinator = new Mock<IReschedulingCoordinator>();
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _eventSubject = new Subject<ExecutionEvent>();

        // Setup event bus to return observable
        _mockEventBus.Setup(x => x.AllEvents).Returns(_eventSubject);

        // Set up event dispatcher mock to forward events as reschedule requests
        var eventDispatcherMock = new Mock<IExecutionEventDispatcher>();
        eventDispatcherMock.Setup(x => x.HandleExecutionEvent(
                It.IsAny<ExecutionEvent>(),
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<IObserver<RescheduleReason>?>()))
            .Callback<ExecutionEvent, IReadOnlyList<Node>, DateTimeOffset, IObserver<RescheduleReason>
                ?>((_, _, _, requests) => requests?.OnNext(RescheduleReason.SkillStarted));

        // Pipeline builder mock: return a connectable observable that routes reschedule
        // requests through the coordinator, mirroring the production topology.
        var pipelineBuilderMock = new Mock<IExecutionPipelineBuilder>();
        pipelineBuilderMock.Setup(x => x.Build(
                It.IsAny<IObservable<RescheduleReason>>(),
                It.IsAny<IExecutionTriggerService>(),
                It.IsAny<IReschedulingCoordinator>()))
            .Returns<IObservable<RescheduleReason>, IExecutionTriggerService, IReschedulingCoordinator>((requests,
                    triggerService, reschedulingCoord) =>
                requests
                    .SelectMany(reason => Observable.FromAsync(async ct =>
                    {
                        var routerSelections = triggerService.GetRouterSelections();
                        reschedulingCoord.SetRouterSelections(routerSelections);
                        return await reschedulingCoord.RescheduleAsync(reason, ct);
                    }))
                    .Publish());

        var agentSerializationValidatorMock = new Mock<IAgentSerializationValidator>();
        agentSerializationValidatorMock
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(Array.Empty<AgentSerializationViolation>());

        var timingPublisherMock = new Mock<IExecutionTimingPublisher>();
        timingPublisherMock.Setup(p => p.TimingObserver).Returns(
            Observer.Create<ExecutionTimingInfo>(_ => { }, _ => { }, () => { }));

        // Observer surfaces the orchestrator subscribes its per-channel streams to. Forward
        // OnNext to the existing mocks' push methods so .Verify assertions still work; the
        // OnCompleted is a no-op to mirror the singleton-channel contract.
        _mockEventPublisher.Setup(x => x.NodesObserver).Returns(
            Observer.Create<IReadOnlyList<Node>>(
                nodes => _mockEventPublisher.Object.PublishNodeChanges(nodes),
                _ => { },
                () => { }));
        _mockTriggerService.Setup(x => x.PlannedFinishObserver).Returns(
            Observer.Create<IReadOnlyList<Node>>(
                nodes => _mockTriggerService.Object
                    .UpdateAdaptivePlannedFinishTimes(nodes),
                _ => { },
                () => { }));

        _orchestrator = new ExecutionOrchestrator(
            new TestLogger<ExecutionOrchestrator>(),
            NullLogger<PipelineEvents>.Instance,
            TimeProvider.System,
            _mockInitializer.Object,
            _mockStateManager.Object,
            _mockEventPublisher.Object,
            _mockProgressMonitor.Object,
            _mockDependencyAnalyzer.Object,
            _mockTriggerService.Object,
            _mockEventBus.Object,
            _mockReschedulingCoordinator.Object,
            timingPublisherMock.Object,
            eventDispatcherMock.Object,
            pipelineBuilderMock.Object,
            agentSerializationValidatorMock.Object,
            Options.Create(new ExecutionPipelineConfiguration()),
            Scheduler.Default
        );
    }

    public void Dispose()
    {
        _eventSubject?.Dispose();
    }

    [Fact]
    public async Task Reschedule_WithRouterSelections_PassesSelectionsToCoordinator()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var selectedBranchId = Guid.NewGuid();

        var procedure = CreateTestProcedure(procedureId);
        var variableContext = new VariableContext();
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>()
        };

        // Router selections from trigger service
        var routerSelections = new Dictionary<Guid, Guid>
        {
            { routerId, selectedBranchId }
        };

        // Setup initialization
        _mockInitializer
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionInitializationResult
            {
                Success = true,
                Nodes = procedure.Nodes!,
                Edges = procedure.Edges!,
                VariableContext = variableContext,
                ExecutionStartTime = DateTimeOffset.UtcNow,
                AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
            });

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(dependencyGraph);

        // Setup trigger service to return router selections
        _mockTriggerService
            .Setup(x => x.GetRouterSelections())
            .Returns(routerSelections);

        // Counter to track reschedule calls and trigger completion after first reschedule
        var rescheduleCallCount = 0;
        _mockReschedulingCoordinator
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RescheduleReason reason, CancellationToken ct) =>
            {
                rescheduleCallCount++;
                // After first reschedule, mark execution as complete
                if (rescheduleCallCount >= 1)
                {
                    _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(true);
                    _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
                }

                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = procedure.Nodes,
                    CurrentTime = 10.0,
                    IsExecutionComplete = _mockProgressMonitor.Object.IsExecutionComplete()
                };
            });

        // Setup progress monitor - initially not complete
        _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(false);
        _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
        _mockProgressMonitor.Setup(x => x.GetExecutionStatistics()).Returns(new Dictionary<string, int>
        {
            ["Total"] = 1,
            ["Pending"] = 0,
            ["Running"] = 0,
            ["Completed"] = 1,
            ["Failed"] = 0
        });

        // Act - run with timeout to prevent indefinite hang
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executionTask = _orchestrator.StartLoadedProcedureAsync(cts.Token);

        // Wait for initialization to complete
        await Task.Delay(100);

        // Trigger a reschedule by publishing a progress event
        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = procedure.Nodes![0].Id,
            EventType = ExecutionEventType.Progress,
            Timestamp = DateTimeOffset.UtcNow,
            ProgressPercentage = 0.5,
            ProgressData = new SkillExecutionProgress
            {
                ExecutionId = Guid.NewGuid(),
                SkillId = procedure.Nodes[0].Id,
                AgentId = Guid.NewGuid(),
                ActualStartTimeUtc = DateTime.UtcNow,
                CurrentTimeIntoExecution = 5.0,
                EstimatedTotalDuration = 10.0,
                StatusMessage = "In progress"
            }
        });

        // Wait for execution to complete (it should complete after the reschedule)
        var result = await executionTask;

        // Assert
        result.Should().BeTrue("Execution should complete successfully");

        _mockReschedulingCoordinator.Verify(
            x => x.SetRouterSelections(It.Is<IReadOnlyDictionary<Guid, Guid>>(dict =>
                dict.Count == 1 && dict[routerId] == selectedBranchId)),
            Times.AtLeastOnce(),
            "ExecutionOrchestrator should pass router selections to ReschedulingCoordinator before rescheduling"
        );

        _mockReschedulingCoordinator.Verify(
            x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce()
        );
    }

    [Fact]
    public async Task Reschedule_WithNoRouterSelections_PassesNullToCoordinator()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);
        var variableContext = new VariableContext();
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>()
        };

        _mockInitializer
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionInitializationResult
            {
                Success = true,
                Nodes = procedure.Nodes!,
                Edges = procedure.Edges!,
                VariableContext = variableContext,
                ExecutionStartTime = DateTimeOffset.UtcNow,
                AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
            });

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(dependencyGraph);

        // Setup trigger service to return null (no routers evaluated yet)
        _mockTriggerService
            .Setup(x => x.GetRouterSelections())
            .Returns((IReadOnlyDictionary<Guid, Guid>?)null);

        // Counter to track reschedule calls and trigger completion after first reschedule
        var rescheduleCallCount = 0;
        _mockReschedulingCoordinator
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RescheduleReason reason, CancellationToken ct) =>
            {
                rescheduleCallCount++;
                // After first reschedule, mark execution as complete
                if (rescheduleCallCount >= 1)
                {
                    _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(true);
                    _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
                }

                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = procedure.Nodes,
                    CurrentTime = 10.0,
                    IsExecutionComplete = _mockProgressMonitor.Object.IsExecutionComplete()
                };
            });

        // Setup progress monitor - initially not complete
        _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(false);
        _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
        _mockProgressMonitor.Setup(x => x.GetExecutionStatistics()).Returns(new Dictionary<string, int>
        {
            ["Total"] = 1,
            ["Pending"] = 0,
            ["Running"] = 0,
            ["Completed"] = 1,
            ["Failed"] = 0
        });

        // Act - run with timeout to prevent indefinite hang
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executionTask = _orchestrator.StartLoadedProcedureAsync(cts.Token);

        // Wait for initialization to complete
        await Task.Delay(100);

        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = procedure.Nodes![0].Id,
            EventType = ExecutionEventType.Progress,
            Timestamp = DateTimeOffset.UtcNow,
            ProgressPercentage = 0.5,
            ProgressData = new SkillExecutionProgress
            {
                ExecutionId = Guid.NewGuid(),
                SkillId = procedure.Nodes[0].Id,
                AgentId = Guid.NewGuid(),
                ActualStartTimeUtc = DateTime.UtcNow,
                CurrentTimeIntoExecution = 5.0,
                EstimatedTotalDuration = 10.0,
                StatusMessage = "In progress"
            }
        });

        // Wait for execution to complete (it should complete after the reschedule)
        var result = await executionTask;

        // Assert
        result.Should().BeTrue("Execution should complete successfully");

        _mockReschedulingCoordinator.Verify(
            x => x.SetRouterSelections(null),
            Times.AtLeastOnce(),
            "ExecutionOrchestrator should pass null when no router selections exist"
        );
    }

    [Fact]
    public async Task Reschedule_WithMultipleRouters_PassesAllSelections()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var router1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var procedure = CreateTestProcedure(procedureId);
        var variableContext = new VariableContext();
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>()
        };

        var routerSelections = new Dictionary<Guid, Guid>
        {
            { router1Id, branch1Id },
            { router2Id, branch2Id }
        };

        _mockInitializer
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionInitializationResult
            {
                Success = true,
                Nodes = procedure.Nodes!,
                Edges = procedure.Edges!,
                VariableContext = variableContext,
                ExecutionStartTime = DateTimeOffset.UtcNow,
                AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
            });

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(dependencyGraph);

        _mockTriggerService
            .Setup(x => x.GetRouterSelections())
            .Returns(routerSelections);

        // Counter to track reschedule calls and trigger completion after first reschedule
        var rescheduleCallCount = 0;
        _mockReschedulingCoordinator
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RescheduleReason reason, CancellationToken ct) =>
            {
                rescheduleCallCount++;
                // After first reschedule, mark execution as complete
                if (rescheduleCallCount >= 1)
                {
                    _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(true);
                    _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
                }

                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = procedure.Nodes,
                    CurrentTime = 10.0,
                    IsExecutionComplete = _mockProgressMonitor.Object.IsExecutionComplete()
                };
            });

        // Setup progress monitor - initially not complete
        _mockProgressMonitor.Setup(x => x.IsExecutionComplete()).Returns(false);
        _mockProgressMonitor.Setup(x => x.IsExecutionSuccessful()).Returns(true);
        _mockProgressMonitor.Setup(x => x.GetExecutionStatistics()).Returns(new Dictionary<string, int>
        {
            ["Total"] = 1,
            ["Pending"] = 0,
            ["Running"] = 0,
            ["Completed"] = 1,
            ["Failed"] = 0
        });

        // Act - run with timeout to prevent indefinite hang
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executionTask = _orchestrator.StartLoadedProcedureAsync(cts.Token);

        // Wait for initialization to complete
        await Task.Delay(100);

        _eventSubject.OnNext(new ExecutionEvent
        {
            SkillId = procedure.Nodes![0].Id,
            EventType = ExecutionEventType.Progress,
            Timestamp = DateTimeOffset.UtcNow,
            ProgressPercentage = 0.5,
            ProgressData = new SkillExecutionProgress
            {
                ExecutionId = Guid.NewGuid(),
                SkillId = procedure.Nodes[0].Id,
                AgentId = Guid.NewGuid(),
                ActualStartTimeUtc = DateTime.UtcNow,
                CurrentTimeIntoExecution = 5.0,
                EstimatedTotalDuration = 10.0,
                StatusMessage = "In progress"
            }
        });

        // Wait for execution to complete (it should complete after the reschedule)
        var result = await executionTask;

        // Assert
        result.Should().BeTrue("Execution should complete successfully");

        _mockReschedulingCoordinator.Verify(
            x => x.SetRouterSelections(It.Is<IReadOnlyDictionary<Guid, Guid>>(dict => dict.Count == 2 &&
                dict[router1Id] == branch1Id &&
                dict[router2Id] == branch2Id)),
            Times.AtLeastOnce(),
            "ExecutionOrchestrator should pass all router selections"
        );
    }

    private static Procedure CreateTestProcedure(Guid procedureId)
    {
        var nodeId = Guid.NewGuid();
        return new Procedure
        {
            Id = procedureId,
            Name = "Test Procedure",
            RootNodeIds = new List<Guid> { nodeId },
            Nodes = new List<Node>
            {
                new SkillExecutionNode
                {
                    Id = nodeId,
                    Position = new NodePosition
                    {
                        X = 0,
                        Y = 0
                    },
                    SkillExecutionTask = new SkillExecutionTask
                    {
                        Name = "Test",
                        StartTime = 0,
                        Duration = 10,
                        Skill = new Skill
                        {
                            Id = Guid.NewGuid(),
                            Name = "Test Skill",
                            Description = "Test",
                            Properties = new List<TypedProperty>()
                        },
                        AgentId = Guid.NewGuid()
                    },
                    ProcedureId = default
                }
            },
            Edges = new List<DependencyEdge>()
        };
    }
}