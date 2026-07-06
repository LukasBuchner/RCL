using FHOOE.Freydis.Application.Services.Scheduling.Models;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Service responsible for logging and analyzing scheduling calculation results.
/// </summary>
public interface ISchedulingResultLogger
{
    /// <summary>
    ///     Logs detailed timing results from scheduling calculation.
    /// </summary>
    /// <param name="triggeredByEntityId">ID of the entity that triggered the scheduling.</param>
    /// <param name="nodeSchedules">Collection of node schedules to analyze and log.</param>
    void LogTimingResults(Guid triggeredByEntityId, IReadOnlyList<NodeSchedule> nodeSchedules);
}