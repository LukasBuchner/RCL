using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Owns the reactive state of a single execution run: the reschedule <see cref="Subject{T}" />,
///     the <see cref="CompositeDisposable" /> holding every pipeline subscription, and the task that
///     resolves with the run's overall success. One instance backs exactly one run, created per
///     <see cref="ExecutionOrchestrator.StartLoadedProcedureAsync" />, so no run shares mutable state
///     with another. The permanent publisher singletons (node/edge, timing, planned-finish) are only
///     written into, never owned, so GraphQL subscription continuity is unaffected.
/// </summary>
/// <remarks>
///     <para>
///         <b>Completion bridge.</b> The single terminal pipeline value — the first
///         <see cref="ReschedulingResult" /> stamped <c>IsExecutionComplete</c> — is delivered onto a
///         real pool thread via <c>ObserveOn(TaskPoolScheduler.Default)</c> and projected to the run's
///         success through <c>ToTask</c>. That pool hop is the re-entry guard: it moves the awaiting
///         <see cref="DisposeAsync" /> (whose <c>subscriptions.Dispose()</c> tears down the live
///         <c>Connect()</c> chain) off the dispatcher thread, so teardown never disposes the operator
///         graph from inside its own terminal <c>OnNext</c>. The scheduler for the hop is always
///         <see cref="TaskPoolScheduler.Default" /> — never the sampling scheduler — because a
///         virtual-time scheduler would deliver on the caller thread and reintroduce the re-entry.
///     </para>
///     <para>
///         <b>Teardown.</b> <see cref="DisposeAsync" /> is the single teardown sink for every path —
///         successful run, cancellation, and synchronous start failure. Each step runs under its own
///         <c>try</c>/<c>catch</c> so an early throw cannot skip <c>StopMonitoring()</c> (which must
///         run or the trigger service keeps monitoring and wedges the next run). It is idempotent;
///         clearing the orchestrator's single-flight guard is the orchestrator's responsibility and
///         happens only after this teardown completes.
///     </para>
///     <para>
///         The source <see cref="Subject{T}" /> is terminated via <c>OnCompleted</c> and never
///         <c>Dispose</c>d: post-<c>OnCompleted</c> <c>OnNext</c> calls are silent no-ops, absorbing
///         any in-flight dispatcher callback during teardown.
///     </para>
/// </remarks>
internal sealed class ExecutionSession : IAsyncDisposable
{
    private readonly ISkillExecutionEventBus _eventBus;
    private readonly IExecutionEventDispatcher _eventDispatcher;
    private readonly IExecutionEventPublisher _eventPublisher;
    private readonly IExecutionTriggerService _executionTriggerService;
    private readonly ILogger<ExecutionOrchestrator> _logger;
    private readonly ExecutionPipelineConfiguration _pipelineConfig;
    private readonly IExecutionPipelineBuilder _pipelineBuilder;
    private readonly IExecutionProgressMonitor _progressMonitor;
    private readonly Subject<RescheduleReason> _rescheduleRequests = new();
    private readonly IReschedulingCoordinator _reschedulingCoordinator;
    private readonly IScheduler _scheduler;
    private readonly DateTimeOffset _startTime;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly TimeProvider _timeProvider;
    private readonly IExecutionTimingPublisher _timingPublisher;

    private bool _disposed;
    private ExecutionRunState? _runState;
    private int _totalSkills;

    /// <summary>
    ///     Initializes a session with the collaborators it writes into during a run. The per-run
    ///     reschedule subject and subscription bag are created here so that <see cref="DisposeAsync" />
    ///     is a valid teardown sink even when a run never reaches <see cref="Start" /> (a synchronous
    ///     start failure).
    /// </summary>
    /// <param name="pipelineBuilder">Builds the connectable reschedule-result stream.</param>
    /// <param name="executionTriggerService">Receives planned-finish updates, is started, and is stopped on teardown.</param>
    /// <param name="reschedulingCoordinator">Computes each reschedule result for the pipeline.</param>
    /// <param name="eventPublisher">Receives node snapshots and refreshes change trackers on teardown.</param>
    /// <param name="timingPublisher">Receives timing snapshots.</param>
    /// <param name="eventDispatcher">Routes event-bus events into reschedule requests.</param>
    /// <param name="eventBus">Source of skill lifecycle events driving the run.</param>
    /// <param name="progressMonitor">Supplies completion statistics and success at the terminal value.</param>
    /// <param name="scheduler">Scheduler the per-channel <c>Sample</c> operators run on.</param>
    /// <param name="timeProvider">Wall-clock source used for elapsed-time logging.</param>
    /// <param name="pipelineConfig">Per-channel publish intervals.</param>
    /// <param name="logger">Logger for run lifecycle and teardown diagnostics.</param>
    /// <param name="startTime">The wall-clock instant the start began, used for teardown elapsed logging.</param>
    public ExecutionSession(
        IExecutionPipelineBuilder pipelineBuilder,
        IExecutionTriggerService executionTriggerService,
        IReschedulingCoordinator reschedulingCoordinator,
        IExecutionEventPublisher eventPublisher,
        IExecutionTimingPublisher timingPublisher,
        IExecutionEventDispatcher eventDispatcher,
        ISkillExecutionEventBus eventBus,
        IExecutionProgressMonitor progressMonitor,
        IScheduler scheduler,
        TimeProvider timeProvider,
        ExecutionPipelineConfiguration pipelineConfig,
        ILogger<ExecutionOrchestrator> logger,
        DateTimeOffset startTime)
    {
        _pipelineBuilder = pipelineBuilder;
        _executionTriggerService = executionTriggerService;
        _reschedulingCoordinator = reschedulingCoordinator;
        _eventPublisher = eventPublisher;
        _timingPublisher = timingPublisher;
        _eventDispatcher = eventDispatcher;
        _eventBus = eventBus;
        _progressMonitor = progressMonitor;
        _scheduler = scheduler;
        _timeProvider = timeProvider;
        _pipelineConfig = pipelineConfig;
        _logger = logger;
        _startTime = startTime;
    }

    /// <summary>
    ///     Wires the per-execution reactive pipeline into the subscription bag — the three sampled
    ///     per-channel observables (frontend nodes, agent planned-finish, timing), the completion
    ///     bridge, the multicast <c>Connect()</c>, and the event-bus → dispatcher → reschedule-subject
    ///     input — then starts trigger monitoring. All subscribers are attached before <c>Connect()</c>
    ///     so the single terminal value is never missed.
    /// </summary>
    /// <param name="runState">The immutable per-run snapshot supplying nodes, start time, and procedure id.</param>
    /// <param name="dependencyGraph">The expanded prerequisite graph the trigger service monitors.</param>
    /// <param name="variableContext">Variable context for router evaluation, or <see langword="null" /> when none.</param>
    /// <param name="totalSkills">The total skill count, used for teardown logging.</param>
    /// <param name="cancellationToken">Token that cancels the returned completion task.</param>
    /// <returns>A task that resolves with the run's overall success when the terminal pipeline value arrives.</returns>
    public Task<bool> Start(
        ExecutionRunState runState,
        DependencyGraph dependencyGraph,
        VariableContextEntity? variableContext,
        int totalSkills,
        CancellationToken cancellationToken)
    {
        _runState = runState;
        _totalSkills = totalSkills;

        // Build the reactive reschedule result stream. The builder returns a connectable observable
        // (Sample → ConcurrentMapLatest → Publish); subscribers are attached below, then Connect()
        // starts the flow.
        var resultStream = _pipelineBuilder.Build(
            _rescheduleRequests, _executionTriggerService, _reschedulingCoordinator);

        var successful = resultStream
            .Where(r => r is { Success: true, UpdatedNodes: not null });

        // Intermediate progress values, excluding the terminal IsExecutionComplete emission so the
        // terminal value appears only once downstream — through the .Concat tail of SampleUntilTerminal.
        var intermediateResults = successful
            .Where(r => !r.IsExecutionComplete);

        // The first result stamped IsExecutionComplete is the authoritative final snapshot.
        // Replay(1).AutoConnect(0) multicasts that single value to every consumer that subscribes:
        // the completion bridge below and each per-channel observable's TakeUntil/Concat pair.
        // resultStream is cold until Connect() below, and these subscriptions attach first via
        // AutoConnect(0), so no terminal emission is missed.
        var finalResult = successful
            .Where(r => r.IsExecutionComplete)
            .Take(1)
            .Replay(1)
            .AutoConnect(0);

        // Each per-channel observable samples intermediate values during execution, then emits exactly
        // one terminal value, then completes; it is piped to a publisher's IObserver surface. The
        // observers deliberately swallow OnCompleted so the singleton channels stay hot across runs.
        void SubscribeChannel<TOut>(double intervalMs, Func<ReschedulingResult, TOut> project, IObserver<TOut> observer)
        {
            _subscriptions.Add(intermediateResults
                .SampleUntilTerminal(finalResult, TimeSpan.FromMilliseconds(intervalMs), _scheduler)
                .Select(project)
                .Subscribe(observer));
        }

        SubscribeChannel(_pipelineConfig.FrontendPublishIntervalMs, r => r.UpdatedNodes!,
            _eventPublisher.NodesObserver);
        SubscribeChannel(_pipelineConfig.AgentPublishIntervalMs, r => r.UpdatedNodes!,
            _executionTriggerService.PlannedFinishObserver);
        SubscribeChannel(_pipelineConfig.TimingPublishIntervalMs, r => BuildTimingInfo(r, runState.StartTime),
            _timingPublisher.TimingObserver);

        // Completion bridge: hop the single terminal value onto a real pool thread, then summarise it
        // to the run's success. ObserveOn(TaskPoolScheduler.Default) IS the re-entry guard — it moves
        // the awaiting DisposeAsync (which disposes this very Connect()/Publish chain) off the
        // dispatcher thread. The scheduler here must stay TaskPoolScheduler.Default, never _scheduler.
        // ToTask owns its own subscription, so it is not added to the bag; subscribing here, before
        // Connect(), preserves the no-missed-terminal guarantee.
        var completionTask = finalResult
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(_ => SummariseAndComputeSuccess(runState))
            .ToTask(cancellationToken);

        // All subscribers attached — connect the multicast source.
        _subscriptions.Add(resultStream.Connect());

        // Event bus → dispatcher → reschedule Subject. The Subject is passed directly as the
        // IObserver<RescheduleReason>; Subject.OnNext silently drops post-OnCompleted, which is
        // exactly what teardown wants.
        _subscriptions.Add(_eventBus.AllEvents.Subscribe(
            e => _eventDispatcher.HandleExecutionEvent(
                e, runState.Nodes, runState.StartTime, _rescheduleRequests),
            error => _logger.LogEventBusStreamError(error),
            () => _logger.LogEventBusStreamCompleted()));

        _logger.LogTriggerServiceStarting();
        _executionTriggerService.Start(dependencyGraph, runState.Nodes, variableContext);

        return completionTask;
    }

    /// <summary>
    ///     Releases the per-execution reactive pipeline and refreshes the change trackers. Terminates
    ///     the reschedule subject via <c>OnCompleted</c>, disposes every subscription, stops trigger
    ///     monitoring, and reloads the persisted node/edge state. Each step is isolated so a throw in
    ///     one cannot skip a later one (notably <c>StopMonitoring</c>). Idempotent: a second call is a
    ///     no-op. Does not clear the orchestrator's single-flight guard — that happens in the
    ///     orchestrator after this completes.
    /// </summary>
    /// <returns>A task that completes when teardown — including the awaited change-tracker refresh — finishes.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        var elapsedTime = (_timeProvider.GetUtcNow() - _startTime).TotalSeconds;
        var procedureId = _runState?.ProcedureId ?? Guid.Empty;
        _logger.LogExecutionCleanup();
        _logger.LogOrchestratorPhase(
            "CLEANUP",
            procedureId,
            _totalSkills,
            elapsedTime: elapsedTime,
            additionalInfo: "Disposing resources");

        try
        {
            _rescheduleRequests.OnCompleted();
        }
        catch (Exception ex)
        {
            _logger.LogCleanupStepFailed("OnCompleted", ex);
        }

        try
        {
            _subscriptions.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogCleanupStepFailed("DisposeSubscriptions", ex);
        }

        try
        {
            _executionTriggerService.StopMonitoring();
        }
        catch (Exception ex)
        {
            _logger.LogCleanupStepFailed("StopMonitoring", ex);
        }

        try
        {
            await _eventPublisher.RefreshChangeTrackersFromRepositoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogRefreshChangeTrackersFailed(ex);
        }
    }

    /// <summary>
    ///     Logs the execution summary and computes the run's overall success. Invoked once, on a pool
    ///     thread (via <c>ObserveOn</c>), when the terminal pipeline value arrives; its return value
    ///     resolves the completion task.
    /// </summary>
    /// <param name="runState">The immutable per-run snapshot supplying the procedure id and start time.</param>
    /// <returns><see langword="true" /> when the execution completed successfully.</returns>
    private bool SummariseAndComputeSuccess(ExecutionRunState runState)
    {
        var stats = _progressMonitor.GetExecutionStatistics();
        var success = _progressMonitor.IsExecutionSuccessful();
        var elapsedSeconds = (_timeProvider.GetUtcNow() - runState.StartTime).TotalSeconds;

        _logger.LogExecutionCompleted(
            success, stats["Completed"], stats["Total"], stats["Failed"]);
        _logger.LogOrchestratorPhase(
            "COMPLETING",
            runState.ProcedureId,
            stats["Total"],
            stats["Pending"],
            stats["Running"],
            stats["Completed"],
            stats["Failed"],
            elapsedSeconds,
            success: success,
            additionalInfo: success ? "Execution succeeded" : "Execution failed");

        return success;
    }

    /// <summary>
    ///     Projects a <see cref="ReschedulingResult" /> into an <see cref="ExecutionTimingInfo" />
    ///     snapshot for the timing publisher. The <c>IsRunning</c> flag is derived from the result's
    ///     own completeness stamp so that sampled and terminal emissions are always internally
    ///     consistent.
    /// </summary>
    /// <param name="result">The rescheduling result to project.</param>
    /// <param name="startTime">The wall-clock instant this run started, used as the timing reference.</param>
    /// <returns>A timing snapshot constructed from the result and the execution start time.</returns>
    private ExecutionTimingInfo BuildTimingInfo(ReschedulingResult result, DateTimeOffset startTime)
    {
        var nodes = result.UpdatedNodes!;
        var maxFinishTime = nodes.OfType<SkillExecutionNode>()
            .Select(n => n.SkillExecutionTask.FinishTime)
            .Where(ft => ft.HasValue)
            .Select(ft => ft!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return new ExecutionTimingInfo
        {
            StartTimeUtc = startTime,
            CurrentTimeSeconds = result.CurrentTime,
            EstimatedTotalDurationSeconds = maxFinishTime,
            ProgressPercentage = _progressMonitor.CalculateProgressPercentage(),
            IsRunning = !result.IsExecutionComplete
        };
    }
}