using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Service responsible for calculating TaskNode durations based on their child SkillExecutionNodes.
///     Follows the Single Responsibility Principle by focusing solely on TaskNode duration calculation.
/// </summary>
public interface ITaskNodeDurationCalculator
{
    /// <summary>
    ///     Calculates the duration for a TaskNode based on its child SkillExecutionNodes' timing.
    ///     The duration spans from the earliest child start time to the latest child finish time.
    /// </summary>
    /// <param name="taskNode">The TaskNode to calculate duration for.</param>
    /// <param name="allNodes">All nodes in the procedure (used to find children).</param>
    /// <param name="nodeTimings">Map of node IDs to their calculated timings (duration, start, finish).</param>
    /// <returns>Calculated duration for the TaskNode, or null if no children found.</returns>
    double? CalculateTaskNodeDuration(
        TaskNode taskNode,
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings);

    /// <summary>
    ///     Calculates durations for all TaskNodes that have child SkillExecutionNodes.
    /// </summary>
    /// <param name="allNodes">All nodes in the procedure.</param>
    /// <param name="nodeTimings">Map of node IDs to their calculated timings.</param>
    /// <returns>Dictionary mapping TaskNode IDs to their calculated durations.</returns>
    IReadOnlyDictionary<Guid, double> CalculateAllTaskNodeDurations(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings);

    /// <summary>
    ///     Calculates complete schedule information (duration, start, finish) for all TaskNodes based on their child
    ///     SkillExecutionNodes.
    ///     Uses containment logic where TaskNode schedule spans from earliest child start to latest child finish.
    /// </summary>
    /// <param name="allNodes">All nodes in the procedure.</param>
    /// <param name="childNodeTimings">Map of child node IDs to their calculated schedule information.</param>
    /// <returns>Dictionary mapping TaskNode IDs to their calculated schedule information.</returns>
    IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> CalculateTaskNodeSchedules(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> childNodeTimings);
}