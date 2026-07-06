using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Filtering;

/// <summary>
/// Filters nodes to include only selected branches from RouterNodes during scheduling.
/// This ensures the timeline shows only the active execution path, not all possible branches.
/// </summary>
public interface IRouterBranchFilterService
{
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
    /// <returns>Filtered result with included/excluded nodes and routing decisions</returns>
    Task<BranchFilterResult> FilterNodesAsync(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null);
}