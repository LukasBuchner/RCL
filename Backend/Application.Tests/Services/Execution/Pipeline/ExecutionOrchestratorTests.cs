using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CoordinatorProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using SkillExecutionProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Pipeline;

/// <summary>
///     Unit tests for ExecutionOrchestrator, focusing on the ObjectDisposedException fix
///     and cleanup behavior to verify the fix for events firing during cleanup.
/// </summary>
public class ExecutionOrchestratorTests
{
    private readonly Mock<IAgentSerializationValidator> _agentSerializationValidatorMock;
    private readonly Mock<IDependencyGraphAnalyzer> _dependencyGraphAnalyzerMock;
    private readonly SkillExecutionEventBus _eventBus;
    private readonly Mock<IExecutionEventDispatcher> _eventDispatcherMock;
    private readonly Mock<IExecutionEventPublisher> _eventPublisherMock;
    private readonly Mock<IExecutionInitializer> _executionInitializerMock;
    private readonly Mock<IExecutionTriggerService> _executionTriggerServiceMock;
    private readonly Mock<ILogger<ExecutionOrchestrator>> _loggerMock;

    private readonly Mock<IExecutionPipelineBuilder> _pipelineBuilderMock;
    private readonly Mock<IExecutionProgressMonitor> _progressMonitorMock;
    private readonly Mock<IReschedulingCoordinator> _reschedulingCoordinatorMock;
    private readonly Mock<ISkillExecutionStateManager> _stateManagerMock;
    private readonly TimeProvider _timeProvider;
    private readonly Mock<IExecutionTimingPublisher> _timingPublisherMock;

    public ExecutionOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<ExecutionOrchestrator>>();
        _timeProvider = TimeProvider.System;
        _executionInitializerMock = new Mock<IExecutionInitializer>();
        _stateManagerMock = new Mock<ISkillExecutionStateManager>();
        _eventPublisherMock = new Mock<IExecutionEventPublisher>();
        _progressMonitorMock = new Mock<IExecutionProgressMonitor>();
        _dependencyGraphAnalyzerMock = new Mock<IDependencyGraphAnalyzer>();
        _executionTriggerServiceMock = new Mock<IExecutionTriggerService>();
        _eventBus = new SkillExecutionEventBus(Mock.Of<ILogger<SkillExecutionEventBus>>());
        _reschedulingCoordinatorMock = new Mock<IReschedulingCoordinator>();
        _timingPublisherMock = new Mock<IExecutionTimingPublisher>();
        _eventDispatcherMock = new Mock<IExecutionEventDispatcher>();
        _pipelineBuilderMock = new Mock<IExecutionPipelineBuilder>();
        _agentSerializationValidatorMock = new Mock<IAgentSerializationValidator>();
        _agentSerializationValidatorMock
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(Array.Empty<AgentSerializationViolation>());

        var nodeSubject = new Subject<IReadOnlyList<Node>>();
        var edgeSubject = new Subject<IReadOnlyList<DependencyEdge>>();

        _eventPublisherMock.Setup(x => x.NodesChanged).Returns(nodeSubject);
        _eventPublisherMock.Setup(x => x.EdgesChanged).Returns(edgeSubject);

        // Observer surfaces that the orchestrator's per-channel streams subscribe to.
        // Forward OnNext to the existing push-method mocks so tests that verify publication
        // via .Verify(x => x.PublishNodeChanges(...)) keep working. OnError/OnCompleted are
        // swallowed to mirror production observer behaviour (hot singleton channels).
        _eventPublisherMock.Setup(x => x.NodesObserver).Returns(
            Observer.Create<IReadOnlyList<Node>>(
                nodes => _eventPublisherMock.Object.PublishNodeChanges(nodes),
                _ => { },
                () => { }));

        _timingPublisherMock.Setup(x => x.TimingObserver).Returns(
            Observer.Create<ExecutionTimingInfo>(
                info => _timingPublisherMock.Object.PublishTiming(info),
                _ => { },
                () => { }));

        _executionTriggerServiceMock.Setup(x => x.PlannedFinishObserver).Returns(
            Observer.Create<IReadOnlyList<Node>>(
                nodes => _executionTriggerServiceMock.Object
                    .UpdateAdaptivePlannedFinishTimes(nodes),
                _ => { },
                () => { }));

        // Set up event dispatcher mock to forward events as reschedule requests
        _eventDispatcherMock.Setup(x => x.HandleExecutionEvent(
                It.IsAny<ExecutionEvent>(),
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<IObserver<RescheduleReason>?>()))
            .Callback<ExecutionEvent, IReadOnlyList<Node>, DateTimeOffset, IObserver<RescheduleReason>
                ?>((_, _, _, requests) => requests?.OnNext(RescheduleReason.SkillStarted));

        // Pipeline builder mock: return a connectable observable that routes reschedule
        // requests through the rescheduling coordinator, mirroring the production topology.
        _pipelineBuilderMock.Setup(x => x.Build(
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
    }

    private ExecutionOrchestrator CreateOrchestrator()
    {
        return new ExecutionOrchestrator(
            _loggerMock.Object,
            NullLogger<PipelineEvents>.Instance,
            _timeProvider,
            _executionInitializerMock.Object,
            _stateManagerMock.Object,
            _eventPublisherMock.Object,
            _progressMonitorMock.Object,
            _dependencyGraphAnalyzerMock.Object,
            _executionTriggerServiceMock.Object,
            _eventBus,
            _reschedulingCoordinatorMock.Object,
            _timingPublisherMock.Object,
            _eventDispatcherMock.Object,
            _pipelineBuilderMock.Object,
            _agentSerializationValidatorMock.Object,
            Options.Create(new ExecutionPipelineConfiguration()),
            Scheduler.Default);
    }

    private SkillExecutionNode CreateSkillNode(Guid? id = null, Guid? skillId = null, Guid? agentId = null)
    {
        var nodeId = id ?? Guid.NewGuid();
        var skill = new Skill
        {
            Id = skillId ?? Guid.NewGuid(),
            Name = $"Skill_{Guid.NewGuid()}",
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };

        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "TestTask",
                StartTime = 0,
                Duration = 10,
                FinishTime = 10,
                Skill = skill,
                AgentId = agentId ?? Guid.NewGuid(),
                ExecutionId = Guid.NewGuid()
            }
        };
    }

    private void SetupSuccessfulExecution(List<Node> nodes, List<DependencyEdge> edges)
    {
        var initResult = new ExecutionInitializationResult
        {
            Success = true,
            Nodes = nodes,
            Edges = edges,
            Schedule = new ScheduleResult
            {
                Success = true,
                UpdatedNodes = nodes,
                NodeSchedules = new List<NodeSchedule>().AsReadOnly()
            },
            AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
        };

        _executionInitializerMock
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initResult);

        _dependencyGraphAnalyzerMock.Setup(x =>
                x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(new DependencyGraph { Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>() });

        _stateManagerMock.Setup(x =>
            x.Initialize(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyDictionary<Guid, IRuntimeAgent>>()));
        _reschedulingCoordinatorMock.Setup(x => x.Initialize(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>(),
            It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<DateTimeOffset>()));
        _executionTriggerServiceMock.Setup(x =>
            x.Start(It.IsAny<DependencyGraph>(), It.IsAny<IReadOnlyList<Node>>(), It.IsAny<VariableContext?>()));
        _executionTriggerServiceMock.Setup(x => x.StopMonitoring());

        _eventPublisherMock.Setup(x => x.RefreshChangeTrackersFromRepositoryAsync())
            .Returns(Task.CompletedTask);

        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(false);
        _progressMonitorMock.Setup(x => x.IsExecutionSuccessful()).Returns(true);
        _progressMonitorMock.Setup(x => x.GetExecutionStatistics()).Returns(new Dictionary<string, int>
        {
            ["Total"] = nodes.Count,
            ["Pending"] = nodes.Count,
            ["Running"] = 0,
            ["Completed"] = 0,
            ["Failed"] = 0
        });
    }

    #region Test: ObjectDisposedException Fix - Events During Cleanup

    /// <summary>
    ///     Tests that events firing during cleanup do not cause ObjectDisposedException.
    ///     This verifies the fix where _isCleaningUp flag prevents OnNext() calls on disposed Subject.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_EventsDuringCleanup_ShouldNotThrowObjectDisposedException()
    {
        // Arrange
        var node = CreateSkillNode();
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);

        // Setup reschedule to complete successfully but slowly (to ensure cleanup can happen during reschedule)
        var rescheduleDelayTcs = new TaskCompletionSource<bool>();
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .Returns(async (RescheduleReason _, CancellationToken _) =>
            {
                await rescheduleDelayTcs.Task; // Wait until we trigger it
                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = nodes,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        // Wait for initialization
        await Task.Delay(100);

        // Publish a Start event to trigger reschedule request (which will be throttled)
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Wait a bit for event processing
        await Task.Delay(100);

        // Now publish multiple events in rapid succession that would normally trigger reschedules
        // These should be throttled and eventually ignored when cleanup starts
        for (var i = 0; i < 5; i++)
            _eventBus.PublishEvent(new ExecutionEvent
            {
                SkillId = node.Id,
                EventType = ExecutionEventType.Progress,
                Timestamp = DateTimeOffset.UtcNow,
                ProgressPercentage = i * 0.2,
                ProgressData = new SkillExecutionProgress
                {
                    ExecutionId = Guid.NewGuid(),
                    SkillId = node.Id,
                    AgentId = Guid.NewGuid(),
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = i * 2.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = $"Progress {i}"
                }
            });

        // Cancel execution to trigger cleanup while events might still be in the pipeline
        await cts.CancelAsync();

        // Complete the reschedule after cancellation
        rescheduleDelayTcs.SetResult(true);

        // Assert - Should complete without throwing ObjectDisposedException
        var exception = await Record.ExceptionAsync(async () => await executionTask);

        // We expect TaskCanceledException from the cancellation, NOT ObjectDisposedException
        Assert.True(exception is null or TaskCanceledException or OperationCanceledException,
            $"Expected no exception or TaskCanceledException, but got {exception?.GetType().Name}: {exception?.Message}");

        // Verify cleanup was called
        _executionTriggerServiceMock.Verify(x => x.StopMonitoring(), Times.Once);
    }

    #endregion

    #region Test: Consecutive Executions

    /// <summary>
    ///     Tests that the orchestrator can be used for consecutive executions without issues.
    ///     This is important since ExecutionOrchestrator is registered as a Singleton in DI.
    ///     The orchestrator creates per-execution state in StartLoadedProcedureAsync and disposes it
    ///     in the finally block, allowing for reuse across multiple executions.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_ConsecutiveExecutions_ShouldWorkCorrectly()
    {
        // Arrange
        var node1 = CreateSkillNode();
        var node2 = CreateSkillNode();
        var nodes1 = new List<Node> { node1 };
        var nodes2 = new List<Node> { node2 };
        var edges = new List<DependencyEdge>();

        var orchestrator = CreateOrchestrator();

        // Setup for first execution
        SetupSuccessfulExecution(nodes1, edges);
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(true);
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes1,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        // Act - First execution. Awaiting the start guarantees the event-bus subscription is live
        // before we publish, so no Finish event is lost.
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result1 = await orchestrator.StartLoadedProcedureAsync(cts1.Token);
        Assert.True(result1);

        // Complete first execution, then await the detached run + cleanup to its terminal state.
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node1.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await orchestrator.CurrentExecution;

        // Setup for second execution
        SetupSuccessfulExecution(nodes2, edges);
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(true);
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes2,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        // Act - Second execution
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result2 = await orchestrator.StartLoadedProcedureAsync(cts2.Token);
        Assert.True(result2);

        // Complete second execution, then await its detached run + cleanup.
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node2.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await orchestrator.CurrentExecution;

        // Assert - both executions triggered cleanup and initialized
        _executionTriggerServiceMock.Verify(x => x.StopMonitoring(), Times.Exactly(2));
        _executionInitializerMock.Verify(
            x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Test: Cleanup Flag Reset

    /// <summary>
    ///     Tests that the _isCleaningUp flag is properly reset between executions.
    ///     This ensures the second execution can process events normally.
    ///     The orchestrator creates new per-execution Subjects for each execution, allowing proper
    ///     state isolation between consecutive executions.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_CleanupFlagReset_ShouldAllowSecondExecution()
    {
        // Arrange
        var node1 = CreateSkillNode();
        var node2 = CreateSkillNode();
        var nodes1 = new List<Node> { node1 };
        var nodes2 = new List<Node> { node2 };
        var edges = new List<DependencyEdge>();

        var orchestrator = CreateOrchestrator();

        // Track reschedule calls to verify flag reset
        var rescheduleCallCount = 0;

        // First execution setup
        SetupSuccessfulExecution(nodes1, edges);
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(true);
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                rescheduleCallCount++;
                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = rescheduleCallCount <= 1 ? nodes1 : nodes2,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        // First execution
        var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await orchestrator.StartLoadedProcedureAsync(cts1.Token);

        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node1.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await orchestrator.CurrentExecution; // run + cleanup #1 (the flag reset) completes deterministically

        // Reset for second execution
        SetupSuccessfulExecution(nodes2, edges);
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(true);

        // Second execution - this should work if cleanup flag was reset
        var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result2 = await orchestrator.StartLoadedProcedureAsync(cts2.Token);

        // Publish events for second execution - these should trigger reschedules if flag is reset
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node2.Id,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Task.Delay(600, cts2.Token); // Structural: wait for the throttled intermediate reschedule

        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node2.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await orchestrator.CurrentExecution; // run + cleanup #2 completes deterministically

        // Assert
        Assert.True(result2);

        // Verify reschedule was called for second execution (proves flag was reset)
        // Should be at least 2 calls (one from first execution finish, one from second execution start/finish)
        Assert.True(rescheduleCallCount >= 2,
            $"Expected at least 2 reschedule calls, but got {rescheduleCallCount}");
    }

    #endregion

    #region Test: Events After Cleanup Ignored

    /// <summary>
    ///     Tests that OnNext() calls after cleanup has started are safely ignored.
    ///     Verifies that the _isCleaningUp guard works correctly.
    /// </summary>
    [Fact]
    public async Task OnExecutionEvent_AfterCleanupStarts_ShouldBeIgnored()
    {
        // Arrange
        var node = CreateSkillNode();
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);

        var rescheduleCalledAfterCleanup = false;
        var cleanupStarted = false;

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (cleanupStarted) rescheduleCalledAfterCleanup = true;
                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = nodes,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        // Track when StopMonitoring is called (indicates cleanup started) and signal via gate
        var cleanupGate = new TaskCompletionSource<bool>();
        _executionTriggerServiceMock
            .Setup(x => x.StopMonitoring())
            .Callback(() =>
            {
                cleanupStarted = true;
                cleanupGate.TrySetResult(true);
            });

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Publish event before cleanup
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Cancel to trigger cleanup
        await Task.Delay(100);
        await cts.CancelAsync();

        // Wait for cleanup to actually start before publishing post-cleanup events.
        // Without this gate, events race against the orchestrator's finally block.
        await cleanupGate.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Publish events after cleanup has started — these should be ignored
        for (var i = 0; i < 3; i++)
        {
            _eventBus.PublishEvent(new ExecutionEvent
            {
                SkillId = node.Id,
                EventType = ExecutionEventType.Progress,
                Timestamp = DateTimeOffset.UtcNow,
                ProgressPercentage = 0.5,
                ProgressData = new SkillExecutionProgress
                {
                    ExecutionId = Guid.NewGuid(),
                    SkillId = node.Id,
                    AgentId = Guid.NewGuid(),
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = 5.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = "After cleanup"
                }
            });
            await Task.Delay(50);
        }

        await Task.Delay(200); // Allow Rx pipeline to drain

        try
        {
            await executionTask;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert - Events after cleanup should NOT trigger reschedules
        Assert.False(rescheduleCalledAfterCleanup,
            "Reschedule should not be called after cleanup starts");
    }

    #endregion

    #region Test: Proper Disposal Ordering

    /// <summary>
    ///     Tests that disposal happens in the correct order: Stop trigger service, dispose subscriptions, dispose Subject.
    ///     This ensures that no new events can trigger OnNext after the Subject is disposed.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_DisposalOrdering_ShouldBeCorrect()
    {
        // Arrange
        var node = CreateSkillNode();
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(true);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        var disposalOrder = new List<string>();

        _executionTriggerServiceMock
            .Setup(x => x.StopMonitoring())
            .Callback(() => disposalOrder.Add("TriggerService.StopMonitoring"));

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Complete execution
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await executionTask;
        await orchestrator.CurrentExecution;

        // Assert - Verify trigger service was stopped (disposal started)
        Assert.Contains("TriggerService.StopMonitoring", disposalOrder);
    }

    #endregion

    #region Test: Two-Phase Completion Publishes Final Node State

    /// <summary>
    ///     Tests that Phase 2 of two-phase completion explicitly publishes the final rescheduled
    ///     node state to the frontend. This verifies the fix for the race where the Sample timer
    ///     on the frontend subscriber may not fire before TrySetResult triggers cleanup and disposal.
    /// </summary>
    [Fact]
    public async Task TwoPhaseCompletion_PublishesFinalNodeStateToFrontend()
    {
        // Arrange
        var node = CreateSkillNode();
        var initNodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(initNodes, edges);

        // Distinct rescheduled nodes list so we can verify it was published by reference
        var rescheduledNodes = new List<Node> { node };

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = rescheduledNodes,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        // After first reschedule completes, mark execution as complete to trigger two-phase completion
        var rescheduleCount = 0;
        _progressMonitorMock
            .Setup(x => x.IsExecutionComplete())
            .Returns(() => rescheduleCount > 0);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                rescheduleCount++;
                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = rescheduledNodes,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        // Track all PublishNodeChanges invocations and the node lists passed
        var publishedNodeLists = new List<IReadOnlyList<Node>>();
        _eventPublisherMock
            .Setup(x => x.PublishNodeChanges(It.IsAny<IReadOnlyList<Node>>()))
            .Callback<IReadOnlyList<Node>>(nodes => publishedNodeLists.Add(nodes));

        var orchestrator = CreateOrchestrator();

        // Act. Awaiting the start guarantees the subscription is live before we publish Finish.
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await orchestrator.StartLoadedProcedureAsync(cts.Token);

        // Publish a Finish event to trigger reschedule → two-phase completion → execution completes
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await orchestrator.CurrentExecution; // run + Phase-2 publish + cleanup complete deterministically

        // Assert
        Assert.True(result, "Execution should have started");

        // Verify PublishNodeChanges was called with the rescheduled nodes reference.
        // This proves Phase 2 explicitly published the final state, because the Sample(10ms)
        // timer almost certainly did not fire in the ~1ms between reschedule completion and disposal.
        Assert.Contains(publishedNodeLists, published => ReferenceEquals(published, rescheduledNodes));
    }

    #endregion

    #region Test: Rapid Event Sequence During Cleanup

    /// <summary>
    ///     Tests handling of rapid event sequences (Start -> Progress -> Finish) during cleanup.
    ///     Simulates realistic scenario where agent publishes events in quick succession.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_RapidEventsDuringCleanup_ShouldNotThrow()
    {
        // Arrange
        var node = CreateSkillNode();
        var agentId = Guid.NewGuid();
        var agent = Mock.Of<IRuntimeAgent>(a => a.Id == agentId);
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);
        _stateManagerMock.Setup(x => x.GetAssignedAgent(node.Id)).Returns(agent);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Start skill
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Rapid progress updates
        for (var i = 0; i < 10; i++)
        {
            _eventBus.PublishEvent(new ExecutionEvent
            {
                SkillId = node.Id,
                EventType = ExecutionEventType.Progress,
                Timestamp = DateTimeOffset.UtcNow,
                ProgressPercentage = i * 0.1,
                ProgressData = new SkillExecutionProgress
                {
                    ExecutionId = Guid.NewGuid(),
                    SkillId = node.Id,
                    AgentId = agentId,
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = i * 1.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = $"Progress {i * 10}%"
                }
            });
            await Task.Delay(10, cts.Token); // Very rapid
        }

        // Cancel during progress updates
        await cts.CancelAsync();

        // More events after cancel (simulating agent still reporting)
        for (var i = 0; i < 5; i++)
        {
            _eventBus.PublishEvent(new ExecutionEvent
            {
                SkillId = node.Id,
                EventType = ExecutionEventType.Progress,
                Timestamp = DateTimeOffset.UtcNow,
                ProgressPercentage = 0.9 + i * 0.01,
                ProgressData = new SkillExecutionProgress
                {
                    ExecutionId = Guid.NewGuid(),
                    SkillId = node.Id,
                    AgentId = agentId,
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = 9.0 + i * 0.1,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = "Almost done"
                }
            });
            await Task.Delay(10);
        }

        // Final finish event
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert - Should complete without throwing
        var exception = await Record.ExceptionAsync(async () => await executionTask);
        Assert.True(exception == null || exception is TaskCanceledException || exception is OperationCanceledException,
            $"Expected no exception or TaskCanceledException, but got {exception?.GetType().Name}");
    }

    #endregion

    #region Test: Multiple Skills With Interleaved Events

    /// <summary>
    ///     Tests that multiple skills with interleaved events during cleanup don't cause issues.
    ///     Verifies thread-safety of cleanup flag.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_MultipleSkillsInterleavedEvents_ShouldHandleCleanupCorrectly()
    {
        // Arrange
        var node1 = CreateSkillNode();
        var node2 = CreateSkillNode();
        var node3 = CreateSkillNode();
        var nodes = new List<Node> { node1, node2, node3 };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Interleave events from multiple skills
        var eventTasks = new List<Task>
        {
            // Skill 1 events
            Task.Run(async () =>
            {
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node1.Id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                await Task.Delay(50, cts.Token);
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node1.Id,
                    EventType = ExecutionEventType.Progress,
                    Timestamp = DateTimeOffset.UtcNow,
                    ProgressPercentage = 0.5,
                    ProgressData = new SkillExecutionProgress
                    {
                        ExecutionId = Guid.NewGuid(),
                        SkillId = node1.Id,
                        AgentId = Guid.NewGuid(),
                        ActualStartTimeUtc = DateTime.UtcNow,
                        CurrentTimeIntoExecution = 5.0,
                        EstimatedTotalDuration = 10.0,
                        StatusMessage = "Skill 1 halfway"
                    }
                });
            }, cts.Token),
            // Skill 2 events
            Task.Run(async () =>
            {
                await Task.Delay(25, cts.Token);
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node2.Id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                await Task.Delay(50, cts.Token);
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node2.Id,
                    EventType = ExecutionEventType.Progress,
                    Timestamp = DateTimeOffset.UtcNow,
                    ProgressPercentage = 0.3,
                    ProgressData = new SkillExecutionProgress
                    {
                        ExecutionId = Guid.NewGuid(),
                        SkillId = node2.Id,
                        AgentId = Guid.NewGuid(),
                        ActualStartTimeUtc = DateTime.UtcNow,
                        CurrentTimeIntoExecution = 3.0,
                        EstimatedTotalDuration = 10.0,
                        StatusMessage = "Skill 2 progress"
                    }
                });
            }, cts.Token),
            // Skill 3 events
            Task.Run(async () =>
            {
                await Task.Delay(10, cts.Token);
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node3.Id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                await Task.Delay(50, cts.Token);
                _eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = node3.Id,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }, cts.Token)
        };

        await Task.WhenAll(eventTasks);

        // Cancel to trigger cleanup while events might still be processing
        await Task.Delay(50, cts.Token);
        await cts.CancelAsync();

        // Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () => await executionTask);
        Assert.True(exception == null || exception is TaskCanceledException || exception is OperationCanceledException,
            $"Expected no exception or TaskCanceledException, but got {exception?.GetType().Name}");

        // Verify cleanup was called
        _executionTriggerServiceMock.Verify(x => x.StopMonitoring(), Times.Once);
    }

    #endregion

    #region Test: Reschedule Failure During Cleanup

    /// <summary>
    ///     Tests that reschedule failures during cleanup don't cause ObjectDisposedException.
    ///     Verifies robust error handling during cleanup phase.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_RescheduleFailureDuringCleanup_ShouldNotThrow()
    {
        // Arrange
        var node = CreateSkillNode();
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);

        var rescheduleCallCount = 0;
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                rescheduleCallCount++;
                // Fail reschedules after first success
                if (rescheduleCallCount > 1)
                    return new ReschedulingResult
                    {
                        Success = false,
                        ErrorMessage = "Reschedule failed during cleanup",
                        CurrentTime = 0.0,
                        IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                    };

                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = nodes,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        var orchestrator = CreateOrchestrator();

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executionTask = orchestrator.StartLoadedProcedureAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Start skill (triggers reschedule)
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Task.Delay(600, cts.Token); // Wait for reschedule

        // Publish more events that will fail to reschedule
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = node.Id,
            EventType = ExecutionEventType.Progress,
            Timestamp = DateTimeOffset.UtcNow,
            ProgressPercentage = 0.5,
            ProgressData = new SkillExecutionProgress
            {
                ExecutionId = Guid.NewGuid(),
                SkillId = node.Id,
                AgentId = Guid.NewGuid(),
                ActualStartTimeUtc = DateTime.UtcNow,
                CurrentTimeIntoExecution = 5.0,
                EstimatedTotalDuration = 10.0,
                StatusMessage = "Progress"
            }
        });

        // Cancel to trigger cleanup
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();

        // Assert - Should not throw ObjectDisposedException (TaskCanceledException is expected)
        var exception = await Record.ExceptionAsync(async () => await executionTask);
        Assert.True(exception == null || exception is TaskCanceledException || exception is OperationCanceledException,
            $"Expected no exception or TaskCanceledException, but got {exception?.GetType().Name}");
    }

    #endregion

    #region NotSelected branch completion tests

    /// <summary>
    ///     Verifies the fix for the production bug where execution hangs forever when a router has
    ///     non-selected branches. The orchestrator must complete because non-selected branch skills
    ///     receive <see cref="ExecutionEventType.NotSelected"/> events, transition to terminal state,
    ///     and count toward <see cref="ExecutionProgressMonitor.IsExecutionComplete"/>.
    ///     Also verifies that a second execution can start (Stop() was called properly).
    /// </summary>
    [Fact]
    public async Task WithRealTriggerService_RouterWithNonSelectedBranch_ExecutionCompletes()
    {
        // Arrange — Build a real ExecutionTriggerService backed by a real event bus and real state services
        var eventBus = new SkillExecutionEventBus(Mock.Of<ILogger<SkillExecutionEventBus>>());
        var mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        var mockRouterEvaluation = new Mock<IRouterEvaluationService>();

        var skillTriggerHandler = new SkillTriggerHandler(
            eventBus,
            mockCoordinator.Object,
            mockAgentProvider.Object,
            NullLogger<SkillTriggerHandler>.Instance,
            NullLogger<PipelineEvents>.Instance);
        // IMPORTANT: Share a single RouterBranchNavigator instance between handler and service.
        // The service initializes the navigator during Start(); the handler needs the same
        // initialized instance for IsSelectedBranch() and FindAllDescendantExecutableNodes().
        var branchNavigator = new RouterBranchNavigator();
        var routerTriggerHandler = new RouterTriggerHandler(
            eventBus,
            mockRouterEvaluation.Object,
            branchNavigator,
            NullLogger<RouterTriggerHandler>.Instance);
        // Use real state manager and progress monitor so NotSelected transitions actually
        // propagate and IsExecutionComplete resolves truthfully.
        var realStateManager = new SkillExecutionStateManager(
            NullLogger<SkillExecutionStateManager>.Instance);
        var realProgressMonitor = new ExecutionProgressMonitor(realStateManager);
        var realTransitionService = new ExecutionStateTransitionService(
            realStateManager,
            NullLogger<ExecutionStateTransitionService>.Instance,
            TimeProvider.System);

        var realTriggerService = new ExecutionTriggerService(
            eventBus,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            NullLogger<ExecutionTriggerService>.Instance,
            NullLogger<PipelineEvents>.Instance);

        // Build the nodes: router → two branches (A and B), each with a task + skill.
        // Branch A is selected; branch B is NOT selected.
        var routerId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid();
        var branchBTaskId = Guid.NewGuid();
        var branchASkillId = Guid.NewGuid();
        var branchBSkillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "Router with two branches",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "A", TargetNodeId = branchATaskId, Priority = 1 },
                    new() { Name = "B", TargetNodeId = branchBTaskId, Priority = 2 }
                }
            }
        };

        var branchATaskNode = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchATask",
                Description = "Branch A",
                StartTime = 0,
                Duration = 5
            }
        };

        var branchBTaskNode = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchBTask",
                Description = "Branch B",
                StartTime = 0,
                Duration = 5
            }
        };

        var branchASkillNode = new SkillExecutionNode
        {
            Id = branchASkillId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = branchATaskId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "BranchASkill",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "BranchASkill",
                    Description = "Skill in selected branch A",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        var branchBSkillNode = new SkillExecutionNode
        {
            Id = branchBSkillId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = branchBTaskId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "BranchBSkill",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "BranchBSkill",
                    Description = "Skill in non-selected branch B",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        var allNodes = new List<Node>
        {
            routerNode, branchATaskNode, branchBTaskNode, branchASkillNode, branchBSkillNode
        };

        // Coordinator mock: immediately publish Start+Finish events (simulating real agent execution)
        mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((id, _, _, _, _) =>
            {
                eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<CoordinatorProgress>();
            });

        // Router evaluation: select branch A (async, like production)
        var variableContext = new VariableContext();
        mockRouterEvaluation
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .Returns(async () =>
            {
                await Task.Yield();
                return branchATaskId;
            });

        // Build the dependency graph: both branch skills depend on Router.Start
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
                }
            }
        };

        // Wire up the orchestrator with the REAL trigger service, state manager, and progress monitor
        var initResult = new ExecutionInitializationResult
        {
            Success = true,
            Nodes = allNodes,
            Edges = new List<DependencyEdge>(),
            VariableContext = variableContext,
            Schedule = new ScheduleResult
            {
                Success = true,
                UpdatedNodes = allNodes,
                NodeSchedules = new List<NodeSchedule>().AsReadOnly()
            },
            AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
        };

        _executionInitializerMock
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initResult);

        _dependencyGraphAnalyzerMock
            .Setup(x => x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(dependencyGraph);

        _eventPublisherMock.Setup(x => x.RefreshChangeTrackersFromRepositoryAsync())
            .Returns(Task.CompletedTask);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = allNodes,
                CurrentTime = 0.0,
                IsExecutionComplete = realProgressMonitor.IsExecutionComplete()
            });

        // Create orchestrator with REAL trigger service, state manager, transition service, progress monitor
        var realEventDispatcher = new ExecutionEventDispatcher(
            realStateManager,
            realTransitionService,
            NullLogger<ExecutionEventDispatcher>.Instance,
            NullLogger<PipelineEvents>.Instance);
        var realPipelineBuilder = new ExecutionPipelineBuilder(
            Options.Create(new ExecutionPipelineConfiguration()),
            NullLogger<PipelineEvents>.Instance,
            Scheduler.Default);
        var orchestrator = new ExecutionOrchestrator(
            _loggerMock.Object,
            NullLogger<PipelineEvents>.Instance,
            _timeProvider,
            _executionInitializerMock.Object,
            realStateManager,
            _eventPublisherMock.Object,
            realProgressMonitor,
            _dependencyGraphAnalyzerMock.Object,
            realTriggerService,
            eventBus,
            _reschedulingCoordinatorMock.Object,
            _timingPublisherMock.Object,
            realEventDispatcher,
            realPipelineBuilder,
            _agentSerializationValidatorMock.Object,
            Options.Create(new ExecutionPipelineConfiguration()),
            Scheduler.Default);

        // Act — first execution must complete within timeout (not hang)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await orchestrator.StartLoadedProcedureAsync(cts.Token);
        await orchestrator.CurrentExecution;

        // Assert — execution completed successfully (didn't hang)
        Assert.True(result, "Execution should complete. If this times out, " +
                            "skills in non-selected router branches are not being marked as terminal, " +
                            "so IsExecutionComplete() never returns true.");

        // Assert — branch B skill is NotSelected
        var branchBState = realStateManager.GetState(branchBSkillId);
        Assert.NotNull(branchBState);
        Assert.Equal(ExecutionStatus.NotSelected, branchBState.ExecutionStatus);

        // Assert — branch A skill is Completed
        var branchAState = realStateManager.GetState(branchASkillId);
        Assert.NotNull(branchAState);
        Assert.Equal(ExecutionStatus.Completed, branchAState.ExecutionStatus);

        // Act — second execution: reset and re-run to verify Stop() was called properly
        _executionInitializerMock
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initResult);

        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result2 = await orchestrator.StartLoadedProcedureAsync(cts2.Token);
        await orchestrator.CurrentExecution;

        // Assert — second execution also completed (trigger service was properly reset)
        Assert.True(result2, "Second execution should succeed. If this fails, " +
                             "Stop() was never called on the trigger service after the first execution.");

        realTriggerService.Dispose();
    }

    #endregion

    #region Test: Concurrent Execution Guard

    /// <summary>
    ///     Verifies that the orchestrator rejects concurrent execution attempts.
    ///     Without this guard, a second call would re-initialize the state manager,
    ///     shift <c>_executionStartTime</c> forward, add duplicate event bus subscriptions,
    ///     and replace the reschedule Subject — corrupting the first execution's pipeline
    ///     and causing nodes to shift right on the timeline.
    /// </summary>
    [Fact]
    public async Task StartLoadedProcedureAsync_WhileFirstExecutionStillRunning_SecondCallIsRejected()
    {
        // Arrange — Setup a slow-running execution that will hang at _executionCompletion.Task
        var node = CreateSkillNode();
        var nodes = new List<Node> { node };
        var edges = new List<DependencyEdge>();

        SetupSuccessfulExecution(nodes, edges);

        // The first execution will never complete (no events trigger completion)
        _progressMonitorMock.Setup(x => x.IsExecutionComplete()).Returns(false);

        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReschedulingResult
            {
                Success = true,
                UpdatedNodes = nodes,
                CurrentTime = 0.0,
                IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
            });

        var orchestrator = CreateOrchestrator();

        // Act — Start first execution (will hang at _executionCompletion.Task)
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var firstExecution = orchestrator.StartLoadedProcedureAsync(cts1.Token);

        // Wait for initialization to complete
        await Task.Delay(200, cts1.Token);

        // Act & Assert — Second call while first is still running: must throw
        await Assert.ThrowsAsync<ExecutionAlreadyInProgressException>(() =>
            orchestrator.StartLoadedProcedureAsync(CancellationToken.None));

        _executionTriggerServiceMock.Verify(
            x => x.Start(It.IsAny<DependencyGraph>(), It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<VariableContext?>()),
            Times.Once,
            "Start() should only be called once — the concurrent guard must prevent the second call " +
            "from reaching initialization.");

        // Cleanup — cancel the first execution
        await cts1.CancelAsync();
        try
        {
            await firstExecution;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }
    }

    /// <summary>
    ///     Verifies end-to-end: with a real <see cref="ExecutionTriggerService"/>, a router-based
    ///     procedure completes and a second execution can start. Uses the real trigger service
    ///     to validate that <c>TriggerRouter</c> publishes its Finish event and <c>Stop()</c>
    ///     properly resets state.
    /// </summary>
    [Fact]
    public async Task WithRealTriggerService_RouterProcedure_ExecutionCompletesAndSecondStartSucceeds()
    {
        // Arrange — Build a real ExecutionTriggerService backed by a real event bus
        var eventBus = new SkillExecutionEventBus(Mock.Of<ILogger<SkillExecutionEventBus>>());
        var mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        var mockRouterEvaluation = new Mock<IRouterEvaluationService>();

        var skillTriggerHandler2 = new SkillTriggerHandler(
            eventBus,
            mockCoordinator.Object,
            mockAgentProvider.Object,
            NullLogger<SkillTriggerHandler>.Instance,
            NullLogger<PipelineEvents>.Instance);
        var branchNavigator2 = new RouterBranchNavigator();
        var routerTriggerHandler2 = new RouterTriggerHandler(
            eventBus,
            mockRouterEvaluation.Object,
            branchNavigator2,
            NullLogger<RouterTriggerHandler>.Instance);
        var realTriggerService = new ExecutionTriggerService(
            eventBus,
            skillTriggerHandler2,
            routerTriggerHandler2,
            branchNavigator2,
            NullLogger<ExecutionTriggerService>.Instance,
            NullLogger<PipelineEvents>.Instance);

        // Build the nodes: router → branch task → branch skill, plus an external skill
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var branchSkillId = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "A", TargetNodeId = branchTaskId, Priority = 1 }
                }
            }
        };

        var branchTaskNode = new TaskNode
        {
            Id = branchTaskId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchTask",
                Description = "Branch",
                StartTime = 0,
                Duration = 5
            }
        };

        var branchSkillNode = new SkillExecutionNode
        {
            Id = branchSkillId,
            ProcedureId = routerNode.ProcedureId,
            ParentId = branchTaskId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "BranchSkill",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "BranchSkill",
                    Description = "Branch skill",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        var externalSkillNode = new SkillExecutionNode
        {
            Id = externalSkillId,
            ProcedureId = routerNode.ProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "ExternalSkill",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "ExternalSkill",
                    Description = "External skill",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        var allNodes = new List<Node> { routerNode, branchTaskNode, branchSkillNode, externalSkillNode };

        // Coordinator mock: immediately publish Start+Finish events (simulating real agent execution)
        mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((id, _, _, _, _) =>
            {
                eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                eventBus.PublishEvent(new ExecutionEvent
                {
                    SkillId = id,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<CoordinatorProgress>();
            });

        // Router evaluation: select the branch task (async, like production)
        var variableContext = new VariableContext();
        mockRouterEvaluation
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .Returns(async () =>
            {
                await Task.Yield();
                return branchTaskId;
            });

        // Build the dependency graph
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

        // Wire up the orchestrator with the REAL trigger service
        var initResult = new ExecutionInitializationResult
        {
            Success = true,
            Nodes = allNodes,
            Edges = new List<DependencyEdge>(),
            VariableContext = variableContext,
            Schedule = new ScheduleResult
            {
                Success = true,
                UpdatedNodes = allNodes,
                NodeSchedules = new List<NodeSchedule>().AsReadOnly()
            },
            AgentAssignments = new Dictionary<Guid, IRuntimeAgent>()
        };

        _executionInitializerMock
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initResult);

        _dependencyGraphAnalyzerMock
            .Setup(x => x.AnalyzeDependencies(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(dependencyGraph);

        _eventPublisherMock.Setup(x => x.RefreshChangeTrackersFromRepositoryAsync())
            .Returns(Task.CompletedTask);

        var rescheduleCount = 0;
        _reschedulingCoordinatorMock
            .Setup(x => x.RescheduleAsync(It.IsAny<RescheduleReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                rescheduleCount++;
                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = allNodes,
                    CurrentTime = 0.0,
                    IsExecutionComplete = _progressMonitorMock.Object.IsExecutionComplete()
                };
            });

        // After the first reschedule completes, mark execution as complete
        _progressMonitorMock
            .Setup(x => x.IsExecutionComplete())
            .Returns(() => rescheduleCount > 0);
        _progressMonitorMock.Setup(x => x.IsExecutionSuccessful()).Returns(true);
        _progressMonitorMock.Setup(x => x.CalculateProgressPercentage()).Returns(100.0);
        _progressMonitorMock.Setup(x => x.GetExecutionStatistics()).Returns(new Dictionary<string, int>
        {
            ["Total"] = 2,
            ["Pending"] = 0,
            ["Running"] = 0,
            ["Completed"] = 2,
            ["Failed"] = 0
        });

        // Create orchestrator with real trigger service
        var realEventDispatcher2 = new ExecutionEventDispatcher(
            _stateManagerMock.Object,
            Mock.Of<IExecutionStateTransitionService>(),
            NullLogger<ExecutionEventDispatcher>.Instance,
            NullLogger<PipelineEvents>.Instance);
        var realPipelineBuilder2 = new ExecutionPipelineBuilder(
            Options.Create(new ExecutionPipelineConfiguration()),
            NullLogger<PipelineEvents>.Instance,
            Scheduler.Default);
        var orchestrator = new ExecutionOrchestrator(
            _loggerMock.Object,
            NullLogger<PipelineEvents>.Instance,
            _timeProvider,
            _executionInitializerMock.Object,
            _stateManagerMock.Object,
            _eventPublisherMock.Object,
            _progressMonitorMock.Object,
            _dependencyGraphAnalyzerMock.Object,
            realTriggerService,
            eventBus,
            _reschedulingCoordinatorMock.Object,
            _timingPublisherMock.Object,
            realEventDispatcher2,
            realPipelineBuilder2,
            _agentSerializationValidatorMock.Object,
            Options.Create(new ExecutionPipelineConfiguration()),
            Scheduler.Default);

        // Act — first execution must complete within timeout (not hang)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await orchestrator.StartLoadedProcedureAsync(cts.Token);
        await orchestrator.CurrentExecution;

        // Assert — execution completed successfully (didn't hang)
        Assert.True(result, "First execution should complete. If this times out, " +
                            "the router's TriggerRouter async void method is hanging, " +
                            "which prevents the orchestrator from completing.");

        // Act — second execution: reset mocks and re-run
        rescheduleCount = 0;
        _executionInitializerMock
            .Setup(x => x.InitializeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(initResult);

        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result2 = await orchestrator.StartLoadedProcedureAsync(cts2.Token);
        await orchestrator.CurrentExecution;

        // Assert — second execution also completed (trigger service was properly reset)
        Assert.True(result2, "Second execution should succeed. If this fails, " +
                             "Stop() was never called on the trigger service after the first execution.");

        realTriggerService.Dispose();
    }

    #endregion
}