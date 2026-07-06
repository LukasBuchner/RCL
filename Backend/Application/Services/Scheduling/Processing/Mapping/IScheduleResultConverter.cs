using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Converts timing results to schedule results.
///     Follows Single Responsibility Principle by focusing only on result conversion.
/// </summary>
public interface IScheduleResultConverter
{
    /// <summary>
    ///     Converts a timing result to a schedule result.
    /// </summary>
    /// <param name="timingResult">The timing calculation result.</param>
    /// <param name="nodesWithPositions">The nodes with updated positions.</param>
    /// <returns>A schedule result with node schedules and updated nodes.</returns>
    ScheduleResult ConvertTimingToScheduleResult(TimingResult timingResult, IReadOnlyList<Node> nodesWithPositions);
}