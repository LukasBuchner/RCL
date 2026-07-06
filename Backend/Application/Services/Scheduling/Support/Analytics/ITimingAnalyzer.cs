using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Service for analyzing timing calculations and collecting statistical insights.
///     Responsible for calculating summary statistics and identifying critical paths.
/// </summary>
public interface ITimingAnalyzer
{
    /// <summary>
    ///     Collects statistical information from timing data in a single pass.
    ///     Calculates min/max/avg durations, earliest start, latest finish, and procedure span.
    /// </summary>
    /// <param name="timingInfo">The timing information for all nodes.</param>
    /// <returns>Statistical summary of timing information.</returns>
    /// <exception cref="ArgumentException">Thrown if timingInfo is null or empty.</exception>
    TimingStatistics CollectStatistics(IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo);

    /// <summary>
    ///     Analyzes the critical path and parallelism in the procedure.
    ///     Identifies nodes on the critical path (finishing at latest finish time) and
    ///     calculates maximum parallelism across the procedure timeline.
    /// </summary>
    /// <param name="timingInfo">The timing information for all nodes.</param>
    /// <param name="nodes">The domain nodes for reference.</param>
    /// <returns>Critical path analysis including node IDs and parallelism metrics.</returns>
    CriticalPathInfo AnalyzeCriticalPath(
        IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes);
}