using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Implementation of timing aggregation service.
/// </summary>
public class TimingAggregator : ITimingAggregator
{
    private readonly ILogger<TimingAggregator> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="TimingAggregator" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public TimingAggregator(ILogger<TimingAggregator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public (double Duration, double StartTime, double FinishTime) AggregateTimings(
        IEnumerable<(double Duration, double StartTime, double FinishTime)> childTimings)
    {
        ArgumentNullException.ThrowIfNull(childTimings);

        var timingList = childTimings.ToList();
        if (timingList.Count == 0)
            throw new ArgumentException("Cannot aggregate empty timing collection", nameof(childTimings));

        var earliestStart = timingList.Min(t => t.StartTime);
        var latestFinish = timingList.Max(t => t.FinishTime);
        var aggregatedDuration = latestFinish - earliestStart;

        _logger.LogTimingsAggregated(timingList.Count, aggregatedDuration, earliestStart, latestFinish);

        return (aggregatedDuration, earliestStart, latestFinish);
    }

    /// <inheritdoc />
    public (double Duration, double StartTime, double FinishTime)? TryAggregateTimings(
        IEnumerable<(double Duration, double StartTime, double FinishTime)> childTimings)
    {
        ArgumentNullException.ThrowIfNull(childTimings);

        var timingList = childTimings.ToList();
        if (timingList.Count == 0)
        {
            _logger.LogNoTimingsForAggregation();
            return null;
        }

        return AggregateTimings(timingList);
    }
}