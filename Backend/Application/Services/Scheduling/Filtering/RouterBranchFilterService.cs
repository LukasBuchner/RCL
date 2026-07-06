using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Filtering;

/// <summary>
/// Filters nodes to include only selected branches from RouterNodes during scheduling.
/// This ensures the timeline shows only the active execution path, not all possible branches.
/// </summary>
public class RouterBranchFilterService(ILogger<RouterBranchFilterService> logger) : IRouterBranchFilterService
{
    private readonly ILogger<RouterBranchFilterService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Filters the provided nodes to exclude non-selected branches.
    /// For each RouterNode with a SelectedBranchTargetNodeId:
    /// - Includes the selected branch TaskNode and all its descendants
    /// - Excludes all other branch TaskNodes and their descendants
    /// RouterNodes without a selected branch (design-time) pass through all branches.
    /// </summary>
    /// <param name="allNodes">All nodes from the procedure</param>
    /// <param name="routerSelections">
    /// Optional dictionary mapping router node ID to selected target node ID (execution mode).
    /// If null, uses node properties like ManuallySelectedBranch or SelectedBranchTargetNodeId (planning mode).
    /// </param>
    public async Task<BranchFilterResult> FilterNodesAsync(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        if (allNodes.Count == 0)
        {
            _logger.LogNoNodesToFilter();
            return new BranchFilterResult
            {
                IncludedNodes = [],
                ExcludedNodes = [],
                RouterSelections = new Dictionary<Guid, BranchSelection>()
            };
        }

        // Build parent-to-children index for efficient descendant lookup
        var childIndex = BuildParentChildIndex(allNodes);

        // Find all RouterNodes
        var routerNodes = allNodes.OfType<RouterNode>().ToList();

        _logger.LogBranchFilterStarted(allNodes.Count, routerNodes.Count);

        var includedNodeIds = new HashSet<Guid>();
        var excludedNodeIds = new HashSet<Guid>();
        var recordedSelections = new Dictionary<Guid, BranchSelection>();

        // Process each router
        foreach (var router in routerNodes)
        {
            // Always include the router itself
            includedNodeIds.Add(router.Id);

            var selectedBranchText = router.RouterTask.SelectedBranchTargetNodeId?.ToString() ?? "null";
            var manualBranchText = router.RouterTask.ManuallySelectedBranch ?? "null";
            _logger.LogRouterProcessing(
                router.RouterTask.Name,
                router.Id,
                selectedBranchText,
                manualBranchText,
                router.RouterTask.Branches.Count);

            foreach (var branch in router.RouterTask.Branches)
            {
                var targetNodeIdText = branch.TargetNodeId?.ToString() ?? "null";
                _logger.LogBranchDetail(branch.Name, branch.Priority, targetNodeIdText);
            }

            // Priority logic: Execution-time selections > Execution state > Manual selection > Show all branches

            // Priority 0: Check if we have execution-time router selections (execution mode)
            if (routerSelections != null && routerSelections.TryGetValue(router.Id, out var executionSelectedTargetId))
            {
                // Find the branch name for logging
                var selectedBranch = router.RouterTask.Branches
                    .FirstOrDefault(b => b.TargetNodeId == executionSelectedTargetId);
                var selectedBranchName = selectedBranch?.Name ?? "Unknown";

                FilterToSelectedBranch(router, executionSelectedTargetId, selectedBranchName, "Execution mode",
                    includedNodeIds, excludedNodeIds, childIndex, recordedSelections);
            }
            // Priority 1: Check if router has a selected branch from execution state
            else if (router.RouterTask.SelectedBranchTargetNodeId.HasValue)
            {
                var selectedTargetId = router.RouterTask.SelectedBranchTargetNodeId.Value;
                var selectedBranchName = router.RouterTask.SelectedBranchName ?? "Unknown";

                FilterToSelectedBranch(router, selectedTargetId, selectedBranchName, "Execution state",
                    includedNodeIds, excludedNodeIds, childIndex, recordedSelections);
            }
            // Priority 2: Check if user manually selected a branch in design mode
            else if (!string.IsNullOrWhiteSpace(router.RouterTask.ManuallySelectedBranch))
            {
                var manualBranchName = router.RouterTask.ManuallySelectedBranch;

                // Find the branch with this name
                var selectedBranch = router.RouterTask.Branches
                    .FirstOrDefault(b => b.Name == manualBranchName);

                if (selectedBranch?.TargetNodeId.HasValue == true)
                {
                    var selectedTargetId = selectedBranch.TargetNodeId.Value;

                    FilterToSelectedBranch(router, selectedTargetId, manualBranchName, "Manual selection",
                        includedNodeIds, excludedNodeIds, childIndex, recordedSelections);
                }
                else
                {
                    _logger.LogInvalidManualSelection(router.RouterTask.Name, router.Id, manualBranchName);

                    IncludeAllBranches(router, includedNodeIds, childIndex);
                }
            }
            // Priority 3: No selection - include all branches
            else
            {
                _logger.LogRouterNoSelection(router.RouterTask.Name, router.Id);

                IncludeAllBranches(router, includedNodeIds, childIndex);
            }
        }

        // Include all nodes that are not part of any router's branches
        // (nodes that are not direct children of routers or are not excluded)
        foreach (var node in allNodes)
        {
            // Skip if already processed
            if (includedNodeIds.Contains(node.Id) || excludedNodeIds.Contains(node.Id))
                continue;

            // Include if not a branch target of any router
            var isRouterBranchTarget = routerNodes.Any(r =>
                GetBranchTargetNodeIds(r).Contains(node.Id));

            if (!isRouterBranchTarget) includedNodeIds.Add(node.Id);
        }

        // Build result lists.
        // A node may appear in both includedNodeIds and excludedNodeIds when an outer router's
        // the descendant walk includes a nested router's non-selected branch, and the inner
        // router later excludes it. Exclusion takes priority to correctly filter nested branches.
        var includedNodes = allNodes.Where(n => includedNodeIds.Contains(n.Id) && !excludedNodeIds.Contains(n.Id))
            .ToList();
        var excludedNodes = allNodes.Where(n => excludedNodeIds.Contains(n.Id)).ToList();

        _logger.LogBranchFilterCompleted(includedNodes.Count, excludedNodes.Count, recordedSelections.Count);

        return await Task.FromResult(new BranchFilterResult
        {
            IncludedNodes = includedNodes,
            ExcludedNodes = excludedNodes,
            RouterSelections = recordedSelections
        });
    }

    /// <summary>
    /// Builds an index mapping parent node IDs to their child nodes for efficient descendant lookup
    /// </summary>
    private static Dictionary<Guid, List<Node>> BuildParentChildIndex(IReadOnlyList<Node> nodes)
    {
        var index = new Dictionary<Guid, List<Node>>();

        foreach (var node in nodes)
        {
            if (!node.ParentId.HasValue) continue;
            if (!index.TryGetValue(node.ParentId.Value, out var value))
            {
                value = [];
                index[node.ParentId.Value] = value;
            }

            value.Add(node);
        }

        return index;
    }

    /// <summary>
    /// Extracts all branch target node IDs from a RouterNode
    /// </summary>
    private static List<Guid> GetBranchTargetNodeIds(RouterNode router)
    {
        if (router.RouterTask.Branches.Count == 0) return [];

        return router.RouterTask.Branches
            .Where(b => b.TargetNodeId.HasValue)
            .Select(b => b.TargetNodeId!.Value)
            .ToList();
    }

    /// <summary>
    /// Filters router to include only the selected branch and its descendants
    /// </summary>
    private void FilterToSelectedBranch(
        RouterNode router,
        Guid selectedTargetId,
        string selectedBranchName,
        string reason,
        HashSet<Guid> includedNodeIds,
        HashSet<Guid> excludedNodeIds,
        Dictionary<Guid, List<Node>> childIndex,
        Dictionary<Guid, BranchSelection> routerSelections)
    {
        // Get all branch target node IDs
        var allBranchTargetIds = GetBranchTargetNodeIds(router);

        // Include selected branch and its descendants
        includedNodeIds.Add(selectedTargetId);
        var selectedDescendants = HierarchyTraversal.CollectDescendantIds(selectedTargetId, childIndex);
        foreach (var descendantId in selectedDescendants) includedNodeIds.Add(descendantId);

        // Exclude all other branches and their descendants
        var excludedCount = 0;
        foreach (var branchTargetId in allBranchTargetIds)
        {
            if (branchTargetId == selectedTargetId)
                continue; // Skip the selected branch

            excludedNodeIds.Add(branchTargetId);
            var branchDescendants = HierarchyTraversal.CollectDescendantIds(branchTargetId, childIndex);
            foreach (var descendantId in branchDescendants) excludedNodeIds.Add(descendantId);

            excludedCount += 1 + branchDescendants.Count;
        }

        _logger.LogRouterBranchSelected(router.RouterTask.Name, selectedBranchName, reason);

        _logger.LogRouterBranchFilterCounts(router.RouterTask.Name, 1 + selectedDescendants.Count, excludedCount);

        // Record the selection
        routerSelections[router.Id] = new BranchSelection
        {
            RouterNodeId = router.Id,
            RouterName = router.RouterTask.Name,
            SelectedBranchTargetNodeId = selectedTargetId,
            SelectedBranchName = selectedBranchName,
            Reason = reason
        };
    }

    /// <summary>
    /// Includes all branches and their descendants from a router
    /// </summary>
    private static void IncludeAllBranches(
        RouterNode router,
        HashSet<Guid> includedNodeIds,
        Dictionary<Guid, List<Node>> childIndex)
    {
        var allBranchTargetIds = GetBranchTargetNodeIds(router);
        foreach (var branchTargetId in allBranchTargetIds)
        {
            includedNodeIds.Add(branchTargetId);
            var branchDescendants = HierarchyTraversal.CollectDescendantIds(branchTargetId, childIndex);
            foreach (var descendantId in branchDescendants) includedNodeIds.Add(descendantId);
        }
    }
}