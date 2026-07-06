using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Implementation of scheduling result logging service.
/// </summary>
public class SchedulingResultLogger : ISchedulingResultLogger
{
    private readonly ILogger<SchedulingResultLogger> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="SchedulingResultLogger" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public SchedulingResultLogger(ILogger<SchedulingResultLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogTimingResults(Guid triggeredByEntityId, IReadOnlyList<NodeSchedule> nodeSchedules)
    {
        ArgumentNullException.ThrowIfNull(nodeSchedules);

        _logger.LogTimingResultsTriggered(triggeredByEntityId);

        if (nodeSchedules.Count == 0)
        {
            _logger.LogNoNodeSchedules();
            return;
        }

        // Group schedules by type
        var taskSchedules = nodeSchedules.Where(n => n.NodeType == NodeScheduleType.TaskNode)
            .OrderBy(n => n.AbsoluteStartTime).ToList();
        var skillSchedules = nodeSchedules.Where(n => n.NodeType == NodeScheduleType.SkillExecutionNode)
            .OrderBy(n => n.AbsoluteStartTime).ToList();

        // Calculate summary statistics
        var totalDuration = nodeSchedules.Sum(n => n.Duration);
        var procedureSpan = CalculateProcedureSpan(nodeSchedules);

        _logger.LogScheduleOverview(taskSchedules.Count, skillSchedules.Count, totalDuration, procedureSpan);

        // Log critical path analysis
        LogCriticalPathAnalysis(nodeSchedules);
    }

    private static double CalculateProcedureSpan(IReadOnlyList<NodeSchedule> nodeSchedules)
    {
        if (!nodeSchedules.Any()) return 0.0;

        var minStartTime = nodeSchedules.Min(n => n.AbsoluteStartTime);
        var maxFinishTime = nodeSchedules.Max(n => n.AbsoluteFinishTime);

        return maxFinishTime - minStartTime;
    }

    private void LogCriticalPathAnalysis(IReadOnlyList<NodeSchedule> nodeSchedules)
    {
        if (!nodeSchedules.Any()) return;

        var maxFinishTime = nodeSchedules.Max(s => s.AbsoluteFinishTime);
        var criticalPathCandidates = nodeSchedules.Where(n =>
            Math.Abs(n.AbsoluteFinishTime - maxFinishTime) < 0.01).ToList();

        if (criticalPathCandidates.Count > 0)
        {
            var candidateNodeIds = string.Join(", ", criticalPathCandidates.Select(c => c.NodeId.ToString()));
            _logger.LogCriticalPathCandidates(
                criticalPathCandidates.Count,
                candidateNodeIds);
        }
    }
}