using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Service responsible for calculating RouterNode durations based on branch target node timings.
///     When a branch is selected, the duration equals the selected branch's target node duration.
///     When no branch is selected, the duration spans all branch targets (earliest start to latest finish)
///     so the router visually covers all possible execution paths.
/// </summary>
public interface IRouterNodeDurationCalculator
{
    /// <summary>
    ///     Calculates the duration for a RouterNode based on its branch target node timings.
    ///     When a branch is selected, returns the selected branch's target duration.
    ///     When no branch is selected, returns the maximum span across all branch targets
    ///     (earliest start to latest finish). Returns null only if no branch timing data is available.
    /// </summary>
    /// <param name="routerNode">The RouterNode to calculate duration for.</param>
    /// <param name="allNodes">All nodes in the procedure (used to find the target node).</param>
    /// <param name="nodeTimings">Map of node IDs to their calculated timings (duration, start, finish).</param>
    /// <param name="routerSelections">
    ///     Optional dictionary mapping router node ID to selected target node ID (execution mode).
    ///     If null, uses node properties like ManuallySelectedBranch or SelectedBranchTargetNodeId (planning mode).
    /// </param>
    /// <returns>
    ///     Calculated duration for the RouterNode, or null if no branch timing data is available.
    /// </returns>
    double? CalculateRouterNodeDuration(
        RouterNode routerNode,
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null);

    /// <summary>
    ///     Calculates complete schedule information (duration, start, finish) for all RouterNodes.
    ///     When a branch is selected, the schedule matches the selected branch's target schedule.
    ///     When no branch is selected, the schedule spans all branch targets (earliest start to latest finish).
    ///     Falls back to the original stored duration only when no branch timing data is available.
    /// </summary>
    /// <param name="allNodes">All nodes in the procedure.</param>
    /// <param name="nodeTimings">Map of node IDs to their calculated schedule information.</param>
    /// <param name="routerSelections">
    ///     Optional dictionary mapping router node ID to selected target node ID (execution mode).
    ///     If null, uses node properties like ManuallySelectedBranch or SelectedBranchTargetNodeId (planning mode).
    /// </param>
    /// <returns>Dictionary mapping RouterNode IDs to their calculated schedule information.</returns>
    IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> CalculateRouterNodeSchedules(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null);
}