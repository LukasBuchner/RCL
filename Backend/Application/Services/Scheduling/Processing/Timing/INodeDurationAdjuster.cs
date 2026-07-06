using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Service responsible for adjusting node durations based on their hierarchy and children's timing.
///     Follows the Single Responsibility Principle by focusing only on duration adjustments.
/// </summary>
public interface INodeDurationAdjuster
{
    /// <summary>
    ///     Adjusts parent task durations to match the actual duration span of their children.
    /// </summary>
    /// <param name="nodeHierarchy">The node hierarchy information.</param>
    /// <param name="durations">Dictionary of node durations to update.</param>
    /// <param name="skillExecutionTimings">Timing information for skill execution nodes.</param>
    /// <param name="routerSelections">
    ///     Optional dictionary mapping router node ID to selected target node ID (execution mode).
    ///     If null, uses node properties like ManuallySelectedBranch or SelectedBranchTargetNodeId (planning mode).
    /// </param>
    void AdjustParentTaskDurations(
        NodeHierarchyInfo nodeHierarchy,
        Dictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> skillExecutionTimings,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null);

    /// <summary>
    ///     Calculates the required duration for a parent node based on its children.
    /// </summary>
    /// <param name="children">List of child nodes.</param>
    /// <param name="durations">Dictionary of node durations.</param>
    /// <param name="skillExecutionTimings">Timing information for skill execution nodes.</param>
    /// <returns>Required duration to span all children, or null if no valid timing is available.</returns>
    double? CalculateRequiredDurationForChildren(
        IReadOnlyList<Node> children,
        IReadOnlyDictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> skillExecutionTimings);
}