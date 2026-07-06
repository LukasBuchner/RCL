using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Orchestrates the execution of procedures loaded from repositories. Composes a reactive
///     reschedule pipeline (built by <see cref="IExecutionPipelineBuilder" />) into per-channel
///     observable streams and pipes each into an <see cref="IObserver{T}" /> surface on the
///     corresponding publisher (nodes, agent planned-finish updates, timing). Starts the run
///     and returns once execution has begun; the run itself proceeds on a detached task exposed
///     through <see cref="CurrentExecution" />, and the procedure's lifecycle (progress,
///     completion, run-time errors) surfaces through the per-channel subscriptions.
/// </summary>
/// <remarks>
///     <para>
///         IMPORTANT: This service is registered as a Singleton (required for GraphQL
///         subscriptions) but supports multiple consecutive executions. Per-run reactive state
///         (the reschedule Subject and the composite of downstream subscriptions) is owned by an
///         <see cref="ExecutionSession" /> created in <see cref="StartLoadedProcedureAsync" /> and
///         released by the session's <see cref="ExecutionSession.DisposeAsync" />.
///     </para>
///     <para>
///         <b>Lifecycle protocol.</b> Termination flows entirely through <c>OnCompleted</c>:
///         <see cref="ExecutionSession.DisposeAsync" /> calls <c>OnCompleted</c> on the per-run
///         <see cref="Subject{T}" /> and disposes the <see cref="CompositeDisposable" /> holding
///         every subscription. It is the single teardown sink — it runs from the detached
///         <c>RunAndCleanupAsync</c> once the run reaches its terminal state, and from the
///         synchronous failure paths when the start never detaches. Publishers'
///         <see cref="IObserver{T}" /> surfaces deliberately
///         swallow <c>OnCompleted</c> so that the singleton channels (<c>NodesChanged</c>,
///         <c>TimingUpdates</c>, per-skill planned-finish subjects) stay hot for subsequent
///         executions — consumers never see a terminal signal on those streams. The source
///         Subject has no unmanaged resources and is explicitly not <c>Dispose</c>d;
///         post-<c>OnCompleted</c> <c>OnNext</c> calls are silent no-ops, absorbing in-flight
///         dispatcher callbacks during teardown.
///     </para>
///     <para>
///         <b>Completion detection is single-phase and declarative.</b> The first
///         <see cref="Rescheduling.ReschedulingResult" /> stamped
///         <see cref="Rescheduling.ReschedulingResult.IsExecutionComplete" /> is the authoritative
///         final snapshot — the coordinator produces it under an all-terminal state where every
///         skill node carries its recorded finish time. <c>Replay(1).AutoConnect(0)</c> multicasts
///         that single value to the completion bridge (<c>ObserveOn(TaskPoolScheduler.Default)</c>
///         + <c>ToTask</c>, which resolves the awaited task off the dispatcher thread) and to each
///         per-channel observable's <c>TakeUntil</c>/<c>Concat</c> pair
///         (<see cref="Common.Reactive.PerExecutionRxExtensions.SampleUntilTerminal{T}" />) — the
///         sampled intermediate phase stops at that moment and is followed by exactly one
///         terminal emission. No subscriber writes back into the source Subject; there is no
///         feedback.
///     </para>
///     <para>
///         <b>Decoupling from consumers.</b> The orchestrator has no direct reference to
///         <c>ProcedureStateTracker</c>, <c>TimingPublisher</c> internals, or the per-skill
///         planned-finish subjects. It subscribes its per-channel observables to the
///         <see cref="IObserver{T}" /> surfaces exposed by
///         <see cref="IExecutionEventPublisher.NodesObserver" />,
///         <see cref="IExecutionTriggerService.PlannedFinishObserver" />, and
///         <see cref="IExecutionTimingPublisher.TimingObserver" />. GraphQL subscriptions and
///         agents subscribe to the publishers' <see cref="IObservable{T}" /> surfaces
///         independently; the orchestrator is not aware of them.
///     </para>
///     <para>
///         Formally verified in Sunstone (Lean 4):
///         - DualLoopConvergence.lean — convergence: execution + rescheduling loop terminates
///     </para>
/// </remarks>
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    private readonly IAgentSerializationValidator _agentSerializationValidator;
    private readonly IDependencyGraphAnalyzer _dependencyGraphAnalyzer;
    private readonly ISkillExecutionEventBus _eventBus;
    private readonly IExecutionEventDispatcher _eventDispatcher;
    private readonly IExecutionEventPublisher _eventPublisher;
    private readonly IExecutionInitializer _executionInitializer;
    private readonly IExecutionTriggerService _executionTriggerService;
    private readonly ILogger<ExecutionOrchestrator> _logger;
    private readonly ILogger<PipelineEvents> _pipeline;
    private readonly ExecutionPipelineConfiguration _pipelineConfig;
    private readonly IExecutionPipelineBuilder _pipelineBuilder;
    private readonly IExecutionProgressMonitor _progressMonitor;
    private readonly IReschedulingCoordinator _reschedulingCoordinator;
    private readonly IScheduler _scheduler;
    private readonly ISkillExecutionStateManager _stateManager;
    private readonly TimeProvider _timeProvider;
    private readonly IExecutionTimingPublisher _timingPublisher;

    private System.Threading.Tasks.Task _currentExecution = System.Threading.Tasks.Task.CompletedTask;
    private int _isExecuting; // 0 = false, 1 = true; use Interlocked for thread safety

    /// <summary>
    ///     Initializes a new instance of <see cref="ExecutionOrchestrator" /> with all required
    ///     execution pipeline services. Wires node and edge change events to the event publisher
    ///     so that subscribers receive real-time updates throughout execution.
    /// </summary>
    /// <param name="logger">Logger for orchestrator-level lifecycle and diagnostic messages.</param>
    /// <param name="pipelineLogger">Logger scoped to pipeline event publishing (publish/suppress decisions).</param>
    /// <param name="timeProvider">Abstraction over wall-clock time, enabling deterministic testing.</param>
    /// <param name="executionInitializer">Loads nodes, edges, schedule, and agent assignments from the currently loaded procedure.</param>
    /// <param name="stateManager">Tracks per-skill execution state throughout the execution run.</param>
    /// <param name="eventPublisher">Pushes node and edge snapshots to GraphQL subscription observers.</param>
    /// <param name="progressMonitor">Determines whether all skills have reached a terminal state.</param>
    /// <param name="dependencyGraphAnalyzer">Builds the prerequisite graph used by the trigger service.</param>
    /// <param name="executionTriggerService">Watches the event bus and fires skills when their prerequisites are satisfied.</param>
    /// <param name="eventBus">Hot observable that emits all skill lifecycle events during execution.</param>
    /// <param name="reschedulingCoordinator">Recomputes the schedule on each reschedule tick and updates node timing.</param>
    /// <param name="timingPublisher">Publishes elapsed-time and progress-percentage snapshots to the frontend.</param>
    /// <param name="eventDispatcher">Routes raw event bus events to the appropriate state and reschedule handlers.</param>
    /// <param name="pipelineBuilder">Constructs the multicast reschedule result stream driven by reschedule requests.</param>
    /// <param name="agentSerializationValidator">
    ///     Verifies that every physical agent in the procedure graph has its assigned skills connected
    ///     by a Finish-to-Start dependency chain, preventing concurrent dispatch to the same robot.
    /// </param>
    /// <param name="pipelineOptions">Pipeline sampling configuration (reschedule, frontend, and agent publish intervals).</param>
    /// <param name="scheduler">
    ///     The scheduler the per-channel <c>Sample</c> operators run on; inject a virtual-time
    ///     scheduler in tests for deterministic sampling.
    /// </param>
    public ExecutionOrchestrator(
        ILogger<ExecutionOrchestrator> logger,
        ILogger<PipelineEvents> pipelineLogger,
        TimeProvider timeProvider,
        IExecutionInitializer executionInitializer,
        ISkillExecutionStateManager stateManager,
        IExecutionEventPublisher eventPublisher,
        IExecutionProgressMonitor progressMonitor,
        IDependencyGraphAnalyzer dependencyGraphAnalyzer,
        IExecutionTriggerService executionTriggerService,
        ISkillExecutionEventBus eventBus,
        IReschedulingCoordinator reschedulingCoordinator,
        IExecutionTimingPublisher timingPublisher,
        IExecutionEventDispatcher eventDispatcher,
        IExecutionPipelineBuilder pipelineBuilder,
        IAgentSerializationValidator agentSerializationValidator,
        IOptions<ExecutionPipelineConfiguration> pipelineOptions,
        IScheduler scheduler)
    {
        _logger = logger;
        _pipeline = pipelineLogger;
        _timeProvider = timeProvider;
        _executionInitializer = executionInitializer;
        _stateManager = stateManager;
        _eventPublisher = eventPublisher;
        _progressMonitor = progressMonitor;
        _dependencyGraphAnalyzer = dependencyGraphAnalyzer;
        _executionTriggerService = executionTriggerService;
        _eventBus = eventBus;
        _reschedulingCoordinator = reschedulingCoordinator;
        _timingPublisher = timingPublisher;
        _eventDispatcher = eventDispatcher;
        _pipelineBuilder = pipelineBuilder;
        _agentSerializationValidator = agentSerializationValidator;
        _pipelineConfig = (pipelineOptions ?? throw new ArgumentNullException(nameof(pipelineOptions))).Value;
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

        // Wire up events to event publisher
        NodeDataChanged += (_, args) => _eventPublisher.PublishNodeChanges(args.Entities);
        EdgeDataChanged += (_, args) => _eventPublisher.PublishEdgeChanges(args.Entities);
    }

    /// <summary>
    ///     Observable for real-time node changes during execution.
    /// </summary>
    public IObservable<IReadOnlyList<Node>> NodesChanged => _eventPublisher.NodesChanged;

    /// <summary>
    ///     Observable for real-time edge changes during execution.
    /// </summary>
    public IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged => _eventPublisher.EdgesChanged;

    /// <summary>
    ///     The task representing the currently running (or most recently completed) execution.
    ///     Completes when the detached run and its cleanup finish. Useful for awaiting completion
    ///     (e.g. tests, graceful shutdown); the GraphQL start mutation does not await it.
    /// </summary>
    public System.Threading.Tasks.Task CurrentExecution => _currentExecution;

    /// <inheritdoc />
    public async Task<bool> StartLoadedProcedureAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            _logger.LogConcurrentStartRejected();
            throw new ExecutionAlreadyInProgressException();
        }

        var startTime = _timeProvider.GetUtcNow();
        _logger.LogExecutionStarting();

        // The session owns this run's reactive state (the reschedule subject, the subscription bag,
        // and the completion task) and is the single teardown sink: its DisposeAsync runs on every
        // exit path, and the single-flight guard is cleared only after it completes.
        var session = new ExecutionSession(
            _pipelineBuilder,
            _executionTriggerService,
            _reschedulingCoordinator,
            _eventPublisher,
            _timingPublisher,
            _eventDispatcher,
            _eventBus,
            _progressMonitor,
            _scheduler,
            _timeProvider,
            _pipelineConfig,
            _logger,
            startTime);

        // Hoisted above the try so the catch/finally fault logging can read it. Reassigned to the
        // real count after InitializeAsync succeeds; stays 0 if init fails.
        var totalSkills = 0;

        try
        {
            var executionStartTime = _timeProvider.GetUtcNow();

            _logger.LogOrchestratorPhase(
                "INITIALIZING",
                Guid.Empty,
                0,
                additionalInfo: "Initializing execution");

            var initResult = await _executionInitializer.InitializeAsync(executionStartTime, cancellationToken);
            if (!initResult.Success)
            {
                _logger.LogExecutionInitFailed(initResult.ErrorMessage);
                _logger.LogOrchestratorPhase(
                    "INITIALIZING",
                    Guid.Empty,
                    0,
                    success: false,
                    errorMessage: initResult.ErrorMessage);
                await session.DisposeAsync();
                Interlocked.Exchange(ref _isExecuting, 0);
                return false;
            }

            // Per-run state lives on this immutable snapshot, never on the singleton, so one run
            // can never read another run's data.
            var runState = new ExecutionRunState
            {
                Nodes = initResult.Nodes,
                Edges = initResult.Edges,
                Schedule = initResult.Schedule,
                StartTime = initResult.ExecutionStartTime,
                ProcedureId = initResult.ProcedureId
            };

            totalSkills = runState.Nodes.OfType<SkillExecutionNode>().Count();

            var initLoadedInfo = $"Loaded {runState.Nodes.Count} nodes, {runState.Edges.Count} edges";
            _logger.LogOrchestratorPhase(
                "INITIALIZING",
                runState.ProcedureId,
                totalSkills,
                totalSkills,
                0,
                0,
                0,
                success: true,
                additionalInfo: initLoadedInfo);

            // Initial emissions: publish the pre-execution snapshot so subscribers that are
            // already connected to the singleton channels see the procedure's starting state
            // before any reschedule result arrives. The session's per-channel observables take
            // over once the source stream starts emitting.
            var initialNodes = runState.Schedule?.UpdatedNodes ?? runState.Nodes;
            PublishNodeChanges(initialNodes);
            PublishEdgeChanges(runState.Edges);
            _timingPublisher.TimingObserver.OnNext(BuildInitialTimingInfo(initialNodes, runState.StartTime));

            _stateManager.Initialize(initialNodes, initResult.AgentAssignments);
            _reschedulingCoordinator.Initialize(runState.ProcedureId, runState.Nodes, runState.Edges,
                runState.StartTime);

            _logger.LogAnalyzingDependencies();
            var dependencyGraph = _dependencyGraphAnalyzer.AnalyzeDependencies(runState.Nodes, runState.Edges);
            var immediateCount = dependencyGraph.GetImmediateStartSkills().Count();
            var adaptiveCount = dependencyGraph.GetAdaptiveSkills().Count();

            await ValidateExecutionPreconditionsAsync(dependencyGraph, runState, initResult, cancellationToken);

            var executingInfo = $"Starting execution: {immediateCount} immediate, {adaptiveCount} adaptive";
            _logger.LogOrchestratorPhase(
                "EXECUTING",
                runState.ProcedureId,
                totalSkills,
                totalSkills,
                0,
                0,
                0,
                additionalInfo: executingInfo);

            var completionTask = session.Start(
                runState, dependencyGraph, initResult.VariableContext, totalSkills, cancellationToken);

            // Detach the run: await completion and tear down on a background task so the GraphQL
            // request returns once execution has started. Run-time progress, completion, and
            // errors surface through the node/edge, timing, and advisory subscriptions.
            _currentExecution = RunAndCleanupAsync(
                session, completionTask, startTime, totalSkills, runState.ProcedureId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogExecutionStartFailed(ex);
            await session.DisposeAsync();
            Interlocked.Exchange(ref _isExecuting, 0);
            return false;
        }
    }

    /// <summary>
    ///     Runs the scheduling-time precondition checks for the loaded procedure: event-level graph
    ///     acyclicity, agent serialization (same-agent skills must be Finish-to-Start separated), and
    ///     adaptive-agent capability for any skill carrying finish prerequisites. Throws when a check
    ///     fails so the caller's start path aborts and cleans up.
    /// </summary>
    /// <param name="dependencyGraph">The expanded prerequisite graph for the loaded procedure.</param>
    /// <param name="runState">The immutable per-run snapshot supplying the nodes and edges to validate.</param>
    /// <param name="initResult">The initialization result carrying the per-skill agent assignments.</param>
    /// <param name="cancellationToken">Token observed while probing each agent's adaptive capability.</param>
    /// <exception cref="AgentSerializationException">
    ///     Thrown when two skills on the same physical agent are not separated by a Finish-to-Start chain.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a skill with finish prerequisites is assigned to an agent that cannot execute it adaptively.
    /// </exception>
    private async System.Threading.Tasks.Task ValidateExecutionPreconditionsAsync(
        DependencyGraph dependencyGraph,
        ExecutionRunState runState,
        ExecutionInitializationResult initResult,
        CancellationToken cancellationToken)
    {
        // Scheduling-time safety net: verify the expanded prerequisite graph has no
        // event-level cycles. The edge-creation validator catches most cycles on raw
        // edges, but TaskNode expansion and ancestor router injection can theoretically
        // create cycles not visible at edge creation time.
        ValidateEventLevelAcyclicity(dependencyGraph);

        // Agent serialization validation: verify same-agent skill pairs are FS-separated.
        var serializationViolations = _agentSerializationValidator.Validate(runState.Nodes, runState.Edges);
        if (serializationViolations.Count > 0)
        {
            foreach (var v in serializationViolations)
                _logger.LogAgentSerializationViolation(
                    v.AgentName,
                    v.UnserializedSkills.Count,
                    v.MissingFsPairs.Count);

            throw new AgentSerializationException(serializationViolations);
        }

        // Scheduling-time validation: verify adaptive agent capability for finish prereqs.
        foreach (var skillId in dependencyGraph.GetAdaptiveSkills())
        {
            if (!initResult.AgentAssignments.TryGetValue(skillId, out var agent))
                continue;

            var skillNode = runState.Nodes.OfType<SkillExecutionNode>()
                .FirstOrDefault(n => n.Id == skillId);
            if (skillNode == null) continue;

            var canAdapt = await agent.CanExecuteAdaptivelyAsync(
                skillNode.SkillExecutionTask.Skill, cancellationToken);
            if (!canAdapt)
                throw new InvalidOperationException(
                    $"Skill '{skillNode.SkillExecutionTask.Skill.Name}' (node {skillId}) " +
                    $"has finish prerequisites (adaptive execution required), but agent " +
                    $"'{agent.Name}' does not support adaptive execution for this skill. " +
                    $"Remove the finish-handle dependency or assign an adaptive-capable agent.");
        }
    }

    /// <summary>
    ///     Awaits the detached execution run to its terminal state, logs a run-time fault, then
    ///     disposes the session and clears the single-flight guard. Runs on a background task so the
    ///     start request is not coupled to the run's duration.
    /// </summary>
    /// <param name="session">The session whose <see cref="ExecutionSession.DisposeAsync" /> releases the run's reactive resources.</param>
    /// <param name="completionTask">The task that resolves (or faults/cancels) when the run reaches its terminal state.</param>
    /// <param name="startTime">The wall-clock time the start began, used for fault logging.</param>
    /// <param name="totalSkills">The total skill count, used for fault logging.</param>
    /// <param name="procedureId">The procedure being executed, used for fault logging.</param>
    private async System.Threading.Tasks.Task RunAndCleanupAsync(
        ExecutionSession session,
        Task<bool> completionTask,
        DateTimeOffset startTime,
        int totalSkills,
        Guid procedureId)
    {
        try
        {
            await completionTask;
        }
        catch (OperationCanceledException)
        {
            // Execution was cancelled via the token; teardown still runs below.
        }
        catch (Exception ex)
        {
            var elapsedTime = (_timeProvider.GetUtcNow() - startTime).TotalSeconds;
            _logger.LogOrchestratorPhase(
                "FAILED",
                procedureId,
                totalSkills,
                elapsedTime: elapsedTime,
                success: false,
                errorMessage: ex.Message);
        }
        finally
        {
            // The session is the single teardown sink; the single-flight guard is cleared only after
            // teardown completes, so a dispose-without-reset can never wedge the next run.
            await session.DisposeAsync();
            Interlocked.Exchange(ref _isExecuting, 0);
        }
    }

    /// <summary>
    ///     Event raised when nodes change during execution.
    /// </summary>
    public event EventHandler<EntityChangedEventArgs<Node>>? NodeDataChanged;

    /// <summary>
    ///     Event raised when edges change during execution.
    /// </summary>
    public event EventHandler<EntityChangedEventArgs<DependencyEdge>>? EdgeDataChanged;

    /// <summary>
    ///     Raises <see cref="NodeDataChanged" /> with the supplied node snapshot. Any exception
    ///     from a subscriber is logged and swallowed so that a misbehaving listener cannot
    ///     interrupt the execution pipeline.
    /// </summary>
    /// <param name="updatedNodes">The node snapshot to publish to downstream observers.</param>
    private void PublishNodeChanges(IReadOnlyList<Node> updatedNodes)
    {
        try
        {
            _pipeline.LogPublishToFrontend(updatedNodes.Count);
            NodeDataChanged?.Invoke(this, new EntityChangedEventArgs<Node>(updatedNodes));
        }
        catch (Exception ex)
        {
            _logger.LogNodePublishFailed(ex);
        }
    }

    /// <summary>
    ///     Raises <see cref="EdgeDataChanged" /> with the supplied edge snapshot. Any exception
    ///     from a subscriber is logged and swallowed so that a misbehaving listener cannot
    ///     interrupt the execution pipeline.
    /// </summary>
    /// <param name="updatedEdges">The edge snapshot to publish to downstream observers.</param>
    private void PublishEdgeChanges(IReadOnlyList<DependencyEdge> updatedEdges)
    {
        try
        {
            _logger.LogEdgePublish(updatedEdges.Count);
            EdgeDataChanged?.Invoke(this, new EntityChangedEventArgs<DependencyEdge>(updatedEdges));
        }
        catch (Exception ex)
        {
            _logger.LogEdgePublishFailed(ex);
        }
    }

    /// <summary>
    ///     Builds the pre-execution timing snapshot published before any reschedule result
    ///     arrives. Consumers see this as the opening value on <c>TimingUpdates</c> when a new
    ///     execution starts.
    /// </summary>
    /// <param name="initialNodes">The node list computed at initialization time (post-schedule if available, else loaded).</param>
    /// <param name="startTime">The wall-clock instant this run started, used as the timing reference.</param>
    /// <returns>A timing snapshot representing the procedure's starting state (<c>IsRunning: true</c>, <c>CurrentTimeSeconds: 0</c>).</returns>
    private ExecutionTimingInfo BuildInitialTimingInfo(IReadOnlyList<Node> initialNodes, DateTimeOffset startTime)
    {
        var maxFinishTime = initialNodes.OfType<SkillExecutionNode>()
            .Select(n => n.SkillExecutionTask.FinishTime)
            .Where(ft => ft.HasValue)
            .Select(ft => ft!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return new ExecutionTimingInfo
        {
            StartTimeUtc = startTime,
            CurrentTimeSeconds = 0,
            EstimatedTotalDurationSeconds = maxFinishTime,
            ProgressPercentage = _progressMonitor.CalculateProgressPercentage(),
            IsRunning = true
        };
    }

    /// <summary>
    ///     Validates that the expanded prerequisite graph has no event-level cycles.
    ///     Builds a waits-for graph with (nodeId, eventType) vertices and checks for
    ///     non-trivial SCCs using Tarjan's algorithm.
    /// </summary>
    /// <param name="dependencyGraph">The expanded prerequisite graph to validate for event-level acyclicity.</param>
    private static void ValidateEventLevelAcyclicity(DependencyGraph dependencyGraph)
    {
        var adj = new Dictionary<(Guid, bool), List<(Guid, bool)>>();

        foreach (var (nodeId, prereqs) in dependencyGraph.Prerequisites)
        {
            // Implicit edge: (nodeId, Finish) → (nodeId, Start)
            AddEventEdge(adj, (nodeId, false), (nodeId, true));

            // Start prerequisite edges
            foreach (var p in prereqs.StartPrerequisites)
            {
                var srcIsStart = p.RequiredEventType == EventTriggerType.Start;
                AddEventEdge(adj, (nodeId, true), (p.DependencySkillId, srcIsStart));
            }

            // Finish prerequisite edges
            foreach (var p in prereqs.FinishPrerequisites)
            {
                var srcIsStart = p.RequiredEventType == EventTriggerType.Start;
                AddEventEdge(adj, (nodeId, false), (p.DependencySkillId, srcIsStart));
            }
        }

        var readOnlyAdj = adj.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<(Guid, bool)>)kvp.Value.AsReadOnly());

        var sccs = Common.SccDetector.FindSccs(readOnlyAdj);
        var cycle = sccs.FirstOrDefault(scc => scc.Count > 1);
        if (cycle != null)
        {
            var description = string.Join(" → ",
                cycle.Select(v => $"({v.Item1.ToString()[..8]}, {(v.Item2 ? "Start" : "Finish")})"));
            throw new InvalidOperationException(
                $"Event-level cycle detected in expanded dependency graph: {description}. " +
                "This creates a structural deadlock. Remove the circular dependency.");
        }
    }

    /// <summary>
    ///     Appends an edge <paramref name="from" /> → <paramref name="to" /> to the adjacency
    ///     list, creating the source vertex's neighbour list on first use.
    /// </summary>
    /// <param name="adj">Adjacency map being constructed.</param>
    /// <param name="from">Source vertex (skill id + event-type boolean: true = Start, false = Finish).</param>
    /// <param name="to">Target vertex (same encoding as <paramref name="from" />).</param>
    private static void AddEventEdge(
        Dictionary<(Guid, bool), List<(Guid, bool)>> adj,
        (Guid, bool) from, (Guid, bool) to)
    {
        if (!adj.TryGetValue(from, out var neighbors))
        {
            neighbors = [];
            adj[from] = neighbors;
        }

        neighbors.Add(to);
    }
}