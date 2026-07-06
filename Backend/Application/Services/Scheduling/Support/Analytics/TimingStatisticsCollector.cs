using FHOOE.Freydis.Application.Services.Scheduling.Computation;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Default implementation of timing statistics collector.
/// </summary>
public class TimingStatisticsCollector : ITimingStatisticsCollector
{
    /// <inheritdoc />
    public TimingCalculationStatistics CreateStatistics()
    {
        return new TimingCalculationStatistics();
    }

    /// <inheritdoc />
    public TimingCalculationStatistics UpdateStatistics(
        TimingCalculationStatistics baseStats,
        int skillExecutionNodesProcessed,
        int taskNodesProcessed,
        TimeSpan? executionGraphBuildTime = null,
        TimeSpan? schedulingTime = null,
        TimeSpan? domainUpdateTime = null,
        TimeSpan? totalTime = null)
    {
        ArgumentNullException.ThrowIfNull(baseStats);

        return new TimingCalculationStatistics
        {
            SkillExecutionNodesProcessed = skillExecutionNodesProcessed,
            TaskNodesProcessed = taskNodesProcessed,
            ExecutionGraphBuildTime = executionGraphBuildTime ?? baseStats.ExecutionGraphBuildTime,
            SchedulingTime = schedulingTime ?? baseStats.SchedulingTime,
            DomainUpdateTime = domainUpdateTime ?? baseStats.DomainUpdateTime,
            TotalTime = totalTime ?? baseStats.TotalTime
        };
    }
}