using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Service for analyzing timing calculations and collecting statistical insights.
/// </summary>
public class TimingAnalyzer(ILogger<TimingAnalyzer> logger) : ITimingAnalyzer
{
    private readonly ILogger<TimingAnalyzer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Collects statistical information from timing data in a SINGLE pass.
    ///     This method fixes the DRY violation in the original code by calculating all statistics at once.
    /// </summary>
    /// <param name="timingInfo">The timing information for all nodes.</param>
    /// <returns>Statistical summary of timing information.</returns>
    /// <exception cref="ArgumentException">Thrown if timingInfo is null or empty.</exception>
    public TimingStatistics CollectStatistics(IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo)
    {
        if (timingInfo == null || timingInfo.Count == 0)
            throw new ArgumentException("Timing information cannot be null or empty", nameof(timingInfo));

        // Calculate ALL statistics in a single pass
        var minDuration = double.MaxValue;
        var maxDuration = double.MinValue;
        var sumDuration = 0.0;
        var earliestStart = double.MaxValue;
        var latestFinish = double.MinValue;

        foreach (var (_, timing) in timingInfo)
        {
            // Duration stats
            if (timing.Duration < minDuration) minDuration = timing.Duration;
            if (timing.Duration > maxDuration) maxDuration = timing.Duration;
            sumDuration += timing.Duration;

            // Timeline stats
            if (timing.AbsoluteStartTime < earliestStart) earliestStart = timing.AbsoluteStartTime;
            if (timing.AbsoluteFinishTime > latestFinish) latestFinish = timing.AbsoluteFinishTime;
        }

        var nodeCount = timingInfo.Count;
        var avgDuration = sumDuration / nodeCount;
        var totalProcedureSpan = latestFinish - earliestStart;

        _logger.LogTimingStatisticsCollected(nodeCount, minDuration, maxDuration, avgDuration, sumDuration,
            totalProcedureSpan);

        return new TimingStatistics
        {
            MinDuration = minDuration,
            MaxDuration = maxDuration,
            AverageDuration = avgDuration,
            SumDuration = sumDuration,
            NodeCount = nodeCount,
            EarliestStart = earliestStart,
            LatestFinish = latestFinish,
            TotalProcedureSpan = totalProcedureSpan
        };
    }

    /// <summary>
    ///     Analyzes the critical path and parallelism in the procedure.
    ///     Identifies nodes on the critical path (finishing at latest finish time) and
    ///     calculates maximum parallelism across the procedure timeline.
    /// </summary>
    /// <param name="timingInfo">The timing information for all nodes.</param>
    /// <param name="nodes">The domain nodes for reference.</param>
    /// <returns>Critical path analysis including node IDs and parallelism metrics.</returns>
    public CriticalPathInfo AnalyzeCriticalPath(
        IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(timingInfo);
        ArgumentNullException.ThrowIfNull(nodes);

        if (timingInfo.Count == 0)
        {
            _logger.LogNoTimingInfoForCriticalPath();
            return new CriticalPathInfo
            {
                CriticalPathNodeIds = [],
                MaxParallelism = 0,
                PeakParallelismTime = 0
            };
        }

        // Find latest finish time
        var latestFinish = timingInfo.Values.Max(t => t.AbsoluteFinishTime);

        // Identify nodes finishing at the latest finish time (within tolerance)
        var criticalPathNodeIds = timingInfo
            .Where(kv => Math.Abs(kv.Value.AbsoluteFinishTime - latestFinish) < 0.01)
            .Select(kv => kv.Key)
            .ToList();

        _logger.LogCriticalPathResult(criticalPathNodeIds.Count, latestFinish);

        // Calculate parallelism by time slices
        var maxParallelism = 0;
        var peakParallelismTime = 0.0;

        if (timingInfo.Count > 0)
        {
            // Build time slices - for each integer time unit, count how many tasks are running
            var parallelExecutions = new Dictionary<double, int>();

            foreach (var timing in timingInfo.Values)
                // For each time slice from start to finish, increment the counter
                for (var time = Math.Ceiling(timing.AbsoluteStartTime);
                     time < timing.AbsoluteFinishTime;
                     time++)
                {
                    if (!parallelExecutions.ContainsKey(time))
                        parallelExecutions[time] = 0;

                    parallelExecutions[time]++;
                }

            if (parallelExecutions.Count > 0)
            {
                maxParallelism = parallelExecutions.Values.Max();
                var peakEntry = parallelExecutions.First(kv => kv.Value == maxParallelism);
                peakParallelismTime = peakEntry.Key;

                _logger.LogPeakParallelism(maxParallelism, peakParallelismTime);
            }
            else
            {
                // All tasks have zero duration or run sequentially
                maxParallelism = 1;
                _logger.LogNoParallelExecution();
            }
        }

        return new CriticalPathInfo
        {
            CriticalPathNodeIds = criticalPathNodeIds,
            MaxParallelism = maxParallelism,
            PeakParallelismTime = peakParallelismTime
        };
    }
}