namespace FHOOE.Freydis.Application.Services.Execution.Utilities;

/// <summary>
///     Calculates execution time metrics.
/// </summary>
/// <remarks>
///     This service provides time calculation utilities for execution monitoring,
///     including elapsed time calculations since execution start.
/// </remarks>
public interface IExecutionTimeCalculator
{
    /// <summary>
    ///     Calculates elapsed time in seconds between start and current time.
    /// </summary>
    /// <param name="startTime">The execution start time</param>
    /// <param name="currentTime">The current time</param>
    /// <returns>Elapsed time in seconds</returns>
    double CalculateElapsedSeconds(DateTimeOffset startTime, DateTimeOffset currentTime);
}