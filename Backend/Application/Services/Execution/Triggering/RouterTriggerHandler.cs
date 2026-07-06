using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Handles triggering and lifecycle management of router nodes during execution.
///     Evaluates router conditions, stores branch selections, publishes NotSelected events
///     for non-selected branches, monitors branch completion, and provides branch filtering.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - RouterBranchConsistency.lean — safety: non-selected branch nodes never execute
///     - NoDeadlocks.lean — liveness: every reachable pending node eventually fires
/// </remarks>
public sealed class RouterTriggerHandler(
    ISkillExecutionEventBus eventBus,
    IRouterEvaluationService routerEvaluationService,
    IRouterBranchNavigator branchNavigator,
    ILogger<RouterTriggerHandler> logger)
    : IRouterTriggerHandler
{
    private readonly IRouterBranchNavigator _branchNavigator =
        branchNavigator ?? throw new ArgumentNullException(nameof(branchNavigator));

    private readonly ISkillExecutionEventBus _eventBus =
        eventBus ?? throw new ArgumentNullException(nameof(eventBus));

    private readonly ILogger<RouterTriggerHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IRouterEvaluationService _routerEvaluationService =
        routerEvaluationService ?? throw new ArgumentNullException(nameof(routerEvaluationService));

    private readonly ConcurrentDictionary<Guid, Guid> _routerSelections = new();

    private readonly ConcurrentDictionary<Guid, byte>
        _triggeringRouters = new();

    /// <inheritdoc />
    public async Task TriggerRouterAsync(
        Guid routerId,
        RouterNode routerNode,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken)
    {
        // CRITICAL: Check if this router is already being evaluated to prevent re-entry
        if (!_triggeringRouters.TryAdd(routerId, 0))
        {
            _logger.LogRouterAlreadyEvaluating(routerNode.RouterTask.Name, routerId);
            return;
        }

        try
        {
            _logger.LogRouterEvaluationStarted(routerNode.RouterTask.Name, routerId);

            // Step 1: Evaluate router to determine selected branch (no events published yet)
            if (variableContext == null)
                throw new InvalidOperationException(
                    $"Cannot evaluate router '{routerNode.RouterTask.Name}': Variable context is null. " +
                    "Routers require a variable context to be provided when starting execution.");

            var selectedTargetId = await _routerEvaluationService.EvaluateRouterAsync(routerNode, variableContext);

            // Step 2: Store the selection so IsSelectedBranch can filter correctly
            _routerSelections[routerId] = selectedTargetId;

            _logger.LogRouterBranchTargetSelected(routerNode.RouterTask.Name, routerId, selectedTargetId);

            // Step 2b: Publish NotSelected events for all executable nodes (skills AND nested
            // routers) in non-selected branches so the state manager marks them as terminal
            // and IsExecutionComplete() can resolve.
            foreach (var branch in routerNode.RouterTask.Branches)
            {
                if (!branch.TargetNodeId.HasValue || branch.TargetNodeId.Value == selectedTargetId) continue;

                var nonSelectedNodeIds = _branchNavigator.FindAllDescendantExecutableNodes(branch.TargetNodeId.Value);
                foreach (var nodeId in nonSelectedNodeIds)
                {
                    var nonSelectedNodeName = ResolveNodeName(nodeId, skillNodes, routerNodes);
                    _logger.LogNodeNotSelected(
                        nonSelectedNodeName, nodeId, routerNode.RouterTask.Name, routerId);

                    _eventBus.PublishEvent(new ExecutionEvent
                    {
                        SkillId = nodeId,
                        EventType = ExecutionEventType.NotSelected,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }

            // Step 3: Find direct executable nodes in the selected branch (skills + nested
            // routers, stopping at router boundaries) and set up completion monitoring BEFORE
            // publishing Router.Start, because Start may trigger branch nodes synchronously.
            var branchNodeIds = _branchNavigator.FindDirectExecutableNodesInBranch(selectedTargetId);
            Task? branchCompletionTask = null;

            if (branchNodeIds.Count > 0)
            {
                _logger.LogRouterWaitingForBranchNodes(routerNode.RouterTask.Name, routerId, branchNodeIds.Count);

                var branchCompletionSignal = branchNodeIds
                    .Select(nodeId => _eventBus.AllEvents
                        .Where(e => e.SkillId == nodeId && e.EventType == ExecutionEventType.Finish)
                        .Take(1))
                    .ToList()
                    .CombineLatest()
                    .Take(1);

                // Subscribe now so the signal is active before Router.Start fires
                branchCompletionTask = branchCompletionSignal.ToTask(cancellationToken);
            }

            // Step 4: Publish Router.Start — branch-internal skills react to this
            var startEvent = new ExecutionEvent
            {
                SkillId = routerId,
                EventType = ExecutionEventType.Start,
                Timestamp = DateTimeOffset.UtcNow
            };
            _eventBus.PublishEvent(startEvent);

            // Step 5: Wait for all selected-branch skills to finish executing
            if (branchCompletionTask != null)
                await branchCompletionTask;

            // Step 6: Publish Router.Finish — external nodes with FS edges react to this
            var finishEvent = new ExecutionEvent
            {
                SkillId = routerId,
                EventType = ExecutionEventType.Finish,
                Timestamp = DateTimeOffset.UtcNow
            };
            _eventBus.PublishEvent(finishEvent);

            _logger.LogRouterFinished(routerNode.RouterTask.Name, routerId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogRouterCancelled(routerNode.RouterTask.Name, routerId);
        }
        catch (Exception ex)
        {
            // Do NOT re-throw: this method is fire-and-forget from an Rx callback.
            _logger.LogRouterEvaluationFailed(ex, routerNode.RouterTask.Name, routerId);
        }
        finally
        {
            _triggeringRouters.TryRemove(routerId, out _);
        }
    }

    /// <inheritdoc />
    public bool IsSelectedBranch(
        Guid nodeId,
        SkillEventPrerequisites prerequisites,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes)
    {
        // Find all router prerequisites (Start or Finish) for this node.
        var routerPrereqs = prerequisites.StartPrerequisites
            .Where(p => routerNodes.ContainsKey(p.DependencySkillId))
            .ToList();

        var nodeName = ResolveNodeName(nodeId, skillNodes, routerNodes);
        _logger.LogIsSelectedBranchCheck(
            nodeName, nodeId, routerPrereqs.Count,
            prerequisites.StartPrerequisites.Count);

        switch (routerPrereqs.Count)
        {
            // FALLBACK CHECK: If no router prerequisites in graph, check if node is actually inside a router hierarchy
            case 0 when routerNodes.Count > 0:
                {
                    var ancestorRouter = _branchNavigator.FindAncestorRouter(nodeId);
                    if (ancestorRouter == null) return true;
                    _logger.LogFallbackRouterCheck(
                        nodeName, nodeId, ancestorRouter.RouterTask.Name,
                        ancestorRouter.Id);

                    // If router hasn't made selection yet, node cannot be triggered
                    if (!_routerSelections.TryGetValue(ancestorRouter.Id, out var selectedTargetId))
                    {
                        _logger.LogNodeBlockedByRouter(
                            nodeName, nodeId, ancestorRouter.RouterTask.Name,
                            ancestorRouter.Id);
                        return false;
                    }

                    var fallbackTargetName = ResolveNodeName(selectedTargetId, skillNodes, routerNodes);
                    if (!_branchNavigator.IsNodeInSelectedBranch(nodeId, selectedTargetId))
                    {
                        _logger.LogNodeSkippedFallback(
                            nodeName, nodeId,
                            fallbackTargetName, selectedTargetId,
                            ancestorRouter.RouterTask.Name, ancestorRouter.Id);
                        return false;
                    }

                    _logger.LogNodeAllowedFallback(
                        nodeName, nodeId,
                        fallbackTargetName, selectedTargetId,
                        ancestorRouter.RouterTask.Name, ancestorRouter.Id);
                    return true;

                    // No router in hierarchy, node can be triggered
                }
            // If no router dependencies (and not in router hierarchy), node can be triggered
            case 0:
                return true;
        }

        // For each router dependency, check if this node is inside the selected branch.
        foreach (var routerPrereq in routerPrereqs)
        {
            var routerId = routerPrereq.DependencySkillId;
            var routerNodeName = ResolveNodeName(routerId, skillNodes, routerNodes);

            if (!_routerSelections.TryGetValue(routerId, out var selectedTargetId))
            {
                _logger.LogNodeBlockedByRouter(
                    nodeName, nodeId,
                    routerNodeName, routerId);
                return false;
            }

            // Only filter by branch if this node is actually inside the router's hierarchy.
            var isInsideRouter = _branchNavigator.FindAncestorRouter(nodeId)?.Id == routerId;
            if (!isInsideRouter)
            {
                _logger.LogNodeAllowedExternalToRouter(
                    nodeName, nodeId,
                    routerNodeName, routerId);
                continue;
            }

            var selectedTargetName = ResolveNodeName(selectedTargetId, skillNodes, routerNodes);
            if (!_branchNavigator.IsNodeInSelectedBranch(nodeId, selectedTargetId))
            {
                _logger.LogNodeSkipped(
                    nodeName, nodeId,
                    selectedTargetName, selectedTargetId,
                    routerNodeName, routerId);
                return false;
            }
        }

        // This node is in the selected branch of all routers it depends on (or is external)
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, Guid>? GetRouterSelections()
    {
        return !_routerSelections.IsEmpty ? new Dictionary<Guid, Guid>(_routerSelections) : null;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _triggeringRouters.Clear();
        _routerSelections.Clear();
    }

    private static string ResolveNodeName(
        Guid nodeId,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes)
    {
        if (skillNodes.TryGetValue(nodeId, out var skillNode))
            return skillNode.SkillExecutionTask.Skill.Name;
        if (routerNodes.TryGetValue(nodeId, out var routerNode))
            return routerNode.RouterTask.Name;
        return "Unknown Node";
    }
}