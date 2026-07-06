using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Monitors execution events and triggers skills/routers when their prerequisites are met.
///     Delegates skill triggering to <see cref="ISkillTriggerHandler"/> and router triggering
///     to <see cref="IRouterTriggerHandler"/>.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - NoPrematureTriggering.lean — safety: nodes fire only when all prerequisites are satisfied
///     - EventBus.lean — CombineLatest monotonicity and append-only bus model
///     - PrerequisiteComputation.lean — soundness of prerequisite graph construction
/// </remarks>
public sealed class ExecutionTriggerService : IExecutionTriggerService, IDisposable
{
    private readonly IRouterBranchNavigator _branchNavigator;
    private readonly ISkillExecutionEventBus _eventBus;

    private readonly ConcurrentBag<ExecutionEvent>
        _firedEvents = []; // Thread-safe collection for concurrent access in Rx pipelines

    private readonly ILogger<ExecutionTriggerService> _logger;
    private readonly ILogger<PipelineEvents> _pipeline;
    private readonly IRouterTriggerHandler _routerTriggerHandler;
    private readonly ISkillTriggerHandler _skillTriggerHandler;
    private readonly ConcurrentBag<IDisposable> _subscriptions = [];

    private CancellationTokenSource _cts = new(); // Not readonly - needs to be recreated after each execution
    private DependencyGraph? _dependencyGraph;
    private bool _isStarted;
    private HashSet<Guid>? _leaflessNodes;
    private Dictionary<Guid, RouterNode>? _routerNodes;
    private Dictionary<Guid, SkillExecutionNode>? _skillNodes;
    private VariableContextEntity? _variableContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionTriggerService" /> class.
    /// </summary>
    /// <param name="eventBus">Hot execution event bus the service subscribes to for prerequisite detection.</param>
    /// <param name="skillTriggerHandler">Handler that actually dispatches skill executions.</param>
    /// <param name="routerTriggerHandler">Handler that evaluates router branches.</param>
    /// <param name="branchNavigator">Navigator used to resolve router branch targets.</param>
    /// <param name="logger">Primary logger for trigger-service lifecycle messages.</param>
    /// <param name="pipelineLogger">Pipeline-scoped logger for publish/suppress decisions.</param>
    public ExecutionTriggerService(
        ISkillExecutionEventBus eventBus,
        ISkillTriggerHandler skillTriggerHandler,
        IRouterTriggerHandler routerTriggerHandler,
        IRouterBranchNavigator branchNavigator,
        ILogger<ExecutionTriggerService> logger,
        ILogger<PipelineEvents> pipelineLogger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _skillTriggerHandler = skillTriggerHandler ?? throw new ArgumentNullException(nameof(skillTriggerHandler));
        _routerTriggerHandler = routerTriggerHandler ?? throw new ArgumentNullException(nameof(routerTriggerHandler));
        _branchNavigator = branchNavigator ?? throw new ArgumentNullException(nameof(branchNavigator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipelineLogger ?? throw new ArgumentNullException(nameof(pipelineLogger));

        PlannedFinishObserver = Observer.Create<IReadOnlyList<Node>>(
            UpdateAdaptivePlannedFinishTimes,
            ex => _logger.LogAdaptivePlannedFinishUpdateFailed(ex),
            () =>
            {
                /* per-skill subjects stay hot across executions */
            });
    }

    /// <inheritdoc />
    public IObserver<IReadOnlyList<Node>> PlannedFinishObserver { get; }

    /// <summary>
    ///     Disposes the service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
        _cts.Dispose();
    }

    /// <summary>
    ///     Starts monitoring execution events and triggering skills/routers based on the dependency graph.
    ///     Creates reactive start-signal subscriptions for each node so that prerequisite satisfaction
    ///     is detected via <c>CombineLatest</c> instead of imperative polling.
    /// </summary>
    public void Start(DependencyGraph dependencyGraph, IReadOnlyList<Node> nodes,
        VariableContextEntity? variableContext)
    {
        ArgumentNullException.ThrowIfNull(dependencyGraph);
        ArgumentNullException.ThrowIfNull(nodes);

        if (_isStarted)
        {
            _logger.LogTriggerServiceAlreadyStarted();
            return;
        }

        var skillCount = dependencyGraph.Prerequisites.Count;
        var routerCount = nodes.OfType<RouterNode>().Count();

        // Validate: if routers exist, variableContext must be provided
        if (routerCount > 0 && variableContext == null)
            throw new ArgumentNullException(nameof(variableContext),
                $"Variable context is required when procedure contains {routerCount} router(s). " +
                "Routers need variables to evaluate conditional branches.");

        _logger.LogTriggerServiceStarting(skillCount, routerCount);

        _dependencyGraph = dependencyGraph;
        _skillNodes = ExtractSkillNodes(nodes);
        _routerNodes = ExtractRouterNodes(nodes);
        _leaflessNodes = ExtractLeaflessNodeIds(nodes, dependencyGraph);
        _variableContext = variableContext;

        // Initialize branch navigator with the node collections for hierarchy traversal
        _branchNavigator.Initialize(
            nodes.ToDictionary(n => n.Id),
            _skillNodes,
            _routerNodes);
        _isStarted = true;

        // Subscribe to all execution events (for logging and _firedEvents tracking)
        var eventSubscription = _eventBus.AllEvents.Subscribe(
            OnEventFired,
            error => _logger.LogEventStreamError(error),
            () => _logger.LogEventStreamCompleted());

        _subscriptions.Add(eventSubscription);

        // Two-pass subscription: first subscribe all nodes that have prerequisites (they listen
        // to the hot event bus and won't fire until matching events arrive), then subscribe
        // immediate-start nodes (empty prerequisites -> Observable.Return emits synchronously).
        // Event-level acyclicity is guaranteed by the validator — no SCC detection needed.
        var immediateStartSignals = new List<(Guid NodeId, IObservable<Unit> Signal)>();

        foreach (var (nodeId, _) in _dependencyGraph.Prerequisites)
        {
            var startPrereqs = _dependencyGraph.GetPrerequisites(nodeId)!.StartPrerequisites;
            var startSignal = CreatePrerequisiteSignal(startPrereqs, true);

            if (startPrereqs.Count > 0)
            {
                var capturedNodeId = nodeId;
                var subscription = startSignal.Subscribe(_ => OnStartPrerequisitesMet(capturedNodeId));
                _subscriptions.Add(subscription);
            }
            else
            {
                immediateStartSignals.Add((nodeId, startSignal));
            }
        }

        // Pass 2: subscribe immediate-start nodes; all dependent subscriptions are already active
        foreach (var (nodeId, signal) in immediateStartSignals)
        {
            var capturedNodeId = nodeId;
            var subscription = signal.Subscribe(_ => OnStartPrerequisitesMet(capturedNodeId));
            _subscriptions.Add(subscription);
        }
    }

    /// <summary>
    ///     Stops monitoring and cleans up all subscriptions.
    ///     Prepares the service for potential re-use (important for Singleton lifetime).
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isStarted)
        {
            _logger.LogTriggerServiceNotStarted();
            return;
        }

        _logger.LogTriggerServiceStopping();

        _isStarted = false;

        // Cancel current operations and dispose the old CTS
        _cts.Cancel();
        _cts.Dispose();

        while (_subscriptions.TryTake(out var subscription)) subscription.Dispose();

        // Reset handler state
        _skillTriggerHandler.Reset();
        _routerTriggerHandler.Reset();

        _firedEvents.Clear();
        _dependencyGraph = null;
        _skillNodes = null;
        _routerNodes = null;
        _leaflessNodes = null;
        _variableContext?.Dispose();
        _variableContext = null;

        // Create a new CTS for the next execution (service is a Singleton, can be reused)
        _cts = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public void UpdatePlannedFinish(Guid skillId, double newPlannedFinishTime)
    {
        if (_skillNodes != null && _skillNodes.TryGetValue(skillId, out var skillNode))
        {
            var skillName = skillNode.SkillExecutionTask.Skill.Name;
            var plannedFinishStr = $"{newPlannedFinishTime:F1}";
            _pipeline.LogForwardingPlannedFinish(plannedFinishStr, skillName, skillId);
        }

        _skillTriggerHandler.UpdatePlannedFinish(skillId, newPlannedFinishTime);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, Guid>? GetRouterSelections()
    {
        return _routerTriggerHandler.GetRouterSelections();
    }

    /// <inheritdoc />
    public void UpdateAdaptivePlannedFinishTimes(IReadOnlyList<Node> nodes)
    {
        try
        {
            foreach (var skillNode in nodes.OfType<SkillExecutionNode>())
            {
                var duration = skillNode.SkillExecutionTask.Duration;

                // Skip terminal skills -- a Finish/Failed/NotSelected event on the bus
                // marks a skill as done, and done skills do not need planned-finish
                // updates. The bus is the single source of truth, matching the formal
                // model in Sunstone/Sunstone/Common/ExecutionStatus.lean (statusOf).
                if (HasNodeReachedTerminalState(skillNode.Id))
                    continue;

                var durationStr = $"{duration:F1}";
                var adaptiveSkillName = skillNode.SkillExecutionTask.Skill.Name ?? "Unnamed";
                _pipeline.LogSendingPlannedFinish(durationStr, adaptiveSkillName, skillNode.Id);
                _skillTriggerHandler.UpdatePlannedFinish(skillNode.Id, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAdaptivePlannedFinishUpdateFailed(ex);
        }
    }

    /// <summary>
    ///     Resolves a human-readable name for a node (skill or router) by its ID.
    /// </summary>
    private string ResolveNodeName(Guid nodeId)
    {
        if (_skillNodes != null && _skillNodes.TryGetValue(nodeId, out var skillNode))
            return skillNode.SkillExecutionTask.Skill.Name;
        if (_routerNodes != null && _routerNodes.TryGetValue(nodeId, out var routerNode))
            return routerNode.RouterTask.Name;
        return "Unknown Node";
    }

    private static Dictionary<Guid, SkillExecutionNode> ExtractSkillNodes(IReadOnlyList<Node> nodes)
    {
        return nodes
            .OfType<SkillExecutionNode>()
            .ToDictionary(n => n.Id);
    }

    private static Dictionary<Guid, RouterNode> ExtractRouterNodes(IReadOnlyList<Node> nodes)
    {
        return nodes
            .OfType<RouterNode>()
            .ToDictionary(n => n.Id);
    }

    /// <summary>
    ///     Extracts the IDs of leafless container tasks — TaskNodes with no executable descendant. These are
    ///     exactly the TaskNode entries the dependency analyzer placed in the prerequisites map as zero-extent
    ///     firing endpoints, so they are gated like skills and routers and dispatched by
    ///     <see cref="TriggerLeaflessEndpoint" />.
    /// </summary>
    /// <param name="nodes">All nodes in the procedure.</param>
    /// <param name="dependencyGraph">The dependency graph whose prerequisites map identifies firing nodes.</param>
    /// <returns>The set of leafless container task IDs.</returns>
    private static HashSet<Guid> ExtractLeaflessNodeIds(
        IReadOnlyList<Node> nodes, DependencyGraph dependencyGraph)
    {
        return nodes
            .OfType<TaskNode>()
            .Where(n => dependencyGraph.Prerequisites.ContainsKey(n.Id))
            .Select(n => n.Id)
            .ToHashSet();
    }

    private void OnEventFired(ExecutionEvent executionEvent)
    {
        var eventNodeName = ResolveNodeName(executionEvent.SkillId);
        var eventTypeName = executionEvent.EventType.ToString();

        if (executionEvent.EventType == ExecutionEventType.Progress)
            _pipeline.LogProgressEventReceived(eventTypeName, eventNodeName, executionEvent.SkillId);
        else
            _pipeline.LogEventReceived(eventTypeName, eventNodeName, executionEvent.SkillId);

        // Track the fired event for HasNodeStarted() guard and IsSelectedBranch() fallback
        _firedEvents.Add(executionEvent);
    }

    /// <summary>
    ///     Called by a reactive start-signal subscription when all start prerequisites for a node are met.
    ///     Applies guard checks (already started, router branch filtering) before dispatching to TriggerNode.
    /// </summary>
    private void OnStartPrerequisitesMet(Guid nodeId)
    {
        // Guard: skip if the node has already been started (prevents duplicate triggering)
        if (HasNodeStarted(nodeId)) return;

        var graph = _dependencyGraph;
        if (graph == null) return;

        var prerequisites = graph.GetPrerequisites(nodeId);
        if (prerequisites == null) return;

        // Router branch filtering: only trigger if this node is in the selected branch
        if (_routerNodes != null && _skillNodes != null &&
            !_routerTriggerHandler.IsSelectedBranch(nodeId, prerequisites, _routerNodes, _skillNodes))
            return;

        // Log prerequisite satisfaction
        if (_skillNodes != null && _skillNodes.TryGetValue(nodeId, out var skillNode))
            _logger.LogExecutionEvent(
                "CHECKING_PREREQUISITES",
                nodeId,
                skillNode.SkillExecutionTask.Skill.Name,
                agentId: skillNode.SkillExecutionTask.AgentId,
                state: "PENDING",
                prerequisitesMet: prerequisites.StartPrerequisites.Count,
                prerequisitesTotal: prerequisites.StartPrerequisites.Count,
                additionalInfo: "Prerequisites satisfied, triggering skill");
        else if (_routerNodes != null && _routerNodes.TryGetValue(nodeId, out var routerNode))
            _logger.LogRouterPrerequisitesMet(routerNode.RouterTask.Name, nodeId);

        TriggerNode(nodeId);
    }

    private bool HasNodeStarted(Guid nodeId)
    {
        return _firedEvents.Any(e => e.SkillId == nodeId && e.EventType == ExecutionEventType.Start);
    }

    /// <summary>
    ///     Returns true if a terminal <see cref="ExecutionEventType" /> (Finish, Failed,
    ///     or NotSelected) for the given node has been observed on the bus. Bus events
    ///     are the single source of truth for "this skill is done", so the rescheduling
    ///     pipeline can detect skills to skip without consulting the state manager.
    /// </summary>
    /// <param name="nodeId">The skill node ID to check for a terminal event.</param>
    /// <returns>
    ///     <c>true</c> if a Finish, Failed, or NotSelected event for <paramref name="nodeId" />
    ///     has been recorded in <see cref="_firedEvents" />; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Safe to call from the rescheduling pipeline. The event dispatcher and this
    ///     service both subscribe to the same hot <see cref="ISkillExecutionEventBus" />,
    ///     and Rx Subject delivers events to subscribers synchronously in subscription
    ///     order. The dispatcher is wired first in <c>ExecutionOrchestrator</c>, so the
    ///     state transition and the <c>_firedEvents</c> append both complete before any
    ///     downstream observer (including the planned-finish observer) runs.
    /// </remarks>
    private bool HasNodeReachedTerminalState(Guid nodeId)
    {
        return _firedEvents.Any(e => e.SkillId == nodeId && e.EventType.IsTerminal());
    }

    /// <summary>
    ///     Triggers execution of a node (skill or router).
    ///     Dispatches to appropriate handler based on node type.
    /// </summary>
    private void TriggerNode(Guid nodeId)
    {
        // Check if it's a router
        if (_routerNodes != null && _routerNodes.TryGetValue(nodeId, out var routerNode))
        {
            // Fire-and-forget: TriggerRouterAsync handles its own exceptions internally.
            _ = _routerTriggerHandler.TriggerRouterAsync(
                nodeId, routerNode, _routerNodes, _skillNodes!, _variableContext, _cts.Token).ContinueWith(
                t => _logger.LogTriggerRouterUnhandledException(t.Exception!.InnerException!, nodeId),
                TaskContinuationOptions.OnlyOnFaulted);
            return;
        }

        // Otherwise, it's a skill
        if (_skillNodes != null && _skillNodes.ContainsKey(nodeId))
        {
            var effectiveGraph = _dependencyGraph!;
            var subscription = _skillTriggerHandler.TriggerSkill(
                nodeId, _skillNodes, effectiveGraph, _variableContext, _cts.Token);
            if (subscription != null)
                _subscriptions.Add(subscription);
            return;
        }

        // Otherwise, it's a leafless container task: a zero-extent self-gating endpoint.
        if (_leaflessNodes != null && _leaflessNodes.Contains(nodeId))
        {
            TriggerLeaflessEndpoint(nodeId);
            return;
        }

        var unresolvedNodeName = ResolveNodeName(nodeId);
        _logger.LogNodeNotFoundInCollections(unresolvedNodeName, nodeId);
    }

    /// <summary>
    ///     Fires a leafless container as a zero-extent self-gating endpoint: publishes its Start immediately
    ///     followed by its Finish on the event bus. The Start satisfies any incoming dependency whose target
    ///     is this container, and the Finish releases dependents waiting on it, so the dependency chain is
    ///     carried through an empty task or branch. The container runs no skill and has no agent, so it is not
    ///     tracked for completion.
    /// </summary>
    /// <param name="nodeId">The leafless container's node ID.</param>
    private void TriggerLeaflessEndpoint(Guid nodeId)
    {
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = nodeId,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        });
        _eventBus.PublishEvent(new ExecutionEvent
        {
            SkillId = nodeId,
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });
        _logger.LogLeaflessEndpointFired(nodeId);
    }

    private IObservable<ExecutionEvent> CreateSinglePrerequisiteObservable(EventPrerequisite prerequisite)
    {
        var requiredEventType = prerequisite.RequiredEventType == EventTriggerType.Start
            ? ExecutionEventType.Start
            : ExecutionEventType.Finish;

        return _eventBus.AllEvents
            .Where(e => e.SkillId == prerequisite.DependencySkillId &&
                        (e.EventType == requiredEventType ||
                         e.EventType == ExecutionEventType.Failed ||
                         e.EventType == ExecutionEventType.NotSelected))
            .Take(1);
    }

    private IObservable<Unit> CreatePrerequisiteSignal(
        IReadOnlyList<EventPrerequisite> prerequisites,
        bool emitImmediatelyIfEmpty = false)
    {
        if (prerequisites.Count == 0)
            return emitImmediatelyIfEmpty
                ? Observable.Return(Unit.Default)
                : Observable.Never<Unit>();

        var prerequisiteObservables = prerequisites
            .Select(CreateSinglePrerequisiteObservable)
            .ToList();

        return prerequisiteObservables.CombineLatest()
            .Select(_ => Unit.Default)
            .Take(1);
    }
}