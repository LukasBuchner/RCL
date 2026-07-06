namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Service responsible for aggregating timing information from multiple sources.
/// </summary>
public interface ITimingAggregator
{
    /// <summary>
    ///     Aggregates timing information from a collection of child timings.
    ///     Calculates the span from earliest start time to latest finish time.
    /// </summary>
    /// <param name="childTimings">Collection of child timing information.</param>
    /// <returns>Aggregated timing information with duration, start time, and finish time.</returns>
    /// <exception cref="ArgumentException">Thrown when the collection is empty.</exception>
    (double Duration, double StartTime, double FinishTime) AggregateTimings(
        IEnumerable<(double Duration, double StartTime, double FinishTime)> childTimings);

    /// <summary>
    ///     Attempts to aggregate timing information from a collection of child timings.
    ///     Returns null if the collection is empty.
    /// </summary>
    /// <param name="childTimings">Collection of child timing information.</param>
    /// <returns>Aggregated timing information, or null if no timings provided.</returns>
    (double Duration, double StartTime, double FinishTime)? TryAggregateTimings(
        IEnumerable<(double Duration, double StartTime, double FinishTime)> childTimings);
}