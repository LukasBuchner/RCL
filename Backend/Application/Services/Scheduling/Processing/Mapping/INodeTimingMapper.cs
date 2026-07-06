using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Maps timing information to domain nodes.
///     Follows Single Responsibility Principle by focusing only on applying timing to nodes.
/// </summary>
public interface INodeTimingMapper
{
    /// <summary>
    ///     Applies timing information to a domain node, creating an updated version.
    /// </summary>
    /// <param name="originalNode">The original node.</param>
    /// <param name="timingInfo">Optional detailed timing information.</param>
    /// <param name="durations">Dictionary of node durations.</param>
    /// <param name="plannedSkills">Optional dictionary of planned skill executions with execution progress data.</param>
    /// <returns>Updated node with timing applied.</returns>
    Node ApplyTimingToNode(
        Node originalNode,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo,
        IReadOnlyDictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, IPlannedSkillExecution>? plannedSkills = null);

    /// <summary>
    ///     Adjusts relative start times for nodes based on their hierarchy.
    ///     Root nodes keep absolute time as relative, children get relative to parent.
    /// </summary>
    /// <param name="timingInfo">The timing information dictionary to modify.</param>
    /// <param name="nodes">The nodes to process for parent-child relationships.</param>
    void AdjustRelativeStartTimesForHierarchy(
        Dictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes);
}