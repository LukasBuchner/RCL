using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Provides logging functionality for scheduling pipeline phases and timing analysis.
/// </summary>
public interface ISchedulingPhaseLogger
{
    /// <summary>
    ///     Logs the start of a scheduling phase.
    /// </summary>
    /// <param name="phaseNumber">The phase number.</param>
    /// <param name="phaseName">The phase name.</param>
    /// <param name="procedureId">The procedure ID.</param>
    void LogPhaseStart(int phaseNumber, string phaseName, Guid procedureId);

    /// <summary>
    ///     Logs the completion of a scheduling phase.
    /// </summary>
    /// <param name="phaseNumber">The phase number.</param>
    /// <param name="phaseName">The phase name.</param>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="duration">The phase duration.</param>
    /// <param name="details">Additional details about the phase completion.</param>
    void LogPhaseComplete(int phaseNumber, string phaseName, Guid procedureId, TimeSpan duration, string details);

    /// <summary>
    ///     Logs the start of the scheduling pipeline.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="nodeCount">The number of nodes.</param>
    /// <param name="edgeCount">The number of edges.</param>
    /// <param name="strictMode">Whether strict mode is enabled.</param>
    /// <param name="preserveOriginal">Whether to preserve original task durations.</param>
    /// <param name="includeTiming">Whether to include detailed timing information.</param>
    void LogPipelineStart(Guid procedureId, int nodeCount, int edgeCount, bool strictMode, bool preserveOriginal,
        bool includeTiming);

    /// <summary>
    ///     Logs the completion of the scheduling pipeline.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="totalDuration">The total pipeline duration.</param>
    /// <param name="scheduleCount">The number of schedules created.</param>
    /// <param name="phaseDurations">The durations of each phase.</param>
    void LogPipelineComplete(Guid procedureId, TimeSpan totalDuration, int scheduleCount, TimeSpan[] phaseDurations);

    /// <summary>
    ///     Logs timing statistics for the scheduling operation.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="statistics">The timing statistics.</param>
    void LogTimingStatistics(Guid procedureId, TimingStatistics statistics);

    /// <summary>
    ///     Logs detailed node timing information.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="timingInfo">The node timing information.</param>
    /// <param name="nodes">The list of nodes.</param>
    void LogDetailedNodeTimings(Guid procedureId, IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes);

    /// <summary>
    ///     Logs critical path analysis results.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="criticalPathInfo">The critical path information.</param>
    /// <param name="nodes">The list of nodes.</param>
    /// <param name="timingInfo">The node timing information.</param>
    void LogCriticalPathAnalysis(Guid procedureId, CriticalPathInfo criticalPathInfo, IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo);
}