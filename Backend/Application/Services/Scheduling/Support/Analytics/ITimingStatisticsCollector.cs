using FHOOE.Freydis.Application.Services.Scheduling.Computation;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Collects and manages timing statistics for scheduling calculations.
/// </summary>
public interface ITimingStatisticsCollector
{
    /// <summary>
    ///     Creates a new timing statistics record.
    /// </summary>
    TimingCalculationStatistics CreateStatistics();

    /// <summary>
    ///     Updates statistics with phase timings.
    /// </summary>
    TimingCalculationStatistics UpdateStatistics(
        TimingCalculationStatistics baseStats,
        int skillExecutionNodesProcessed,
        int taskNodesProcessed,
        TimeSpan? executionGraphBuildTime = null,
        TimeSpan? schedulingTime = null,
        TimeSpan? domainUpdateTime = null,
        TimeSpan? totalTime = null);
}