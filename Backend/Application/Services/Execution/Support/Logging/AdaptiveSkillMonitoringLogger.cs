using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides structured logging for the observability-only adaptive-skill overrun monitor.
/// </summary>
public static partial class AdaptiveSkillMonitoringLogger
{
    /// <summary>
    ///     Logs that an executing skill has run past its scheduled finish by the warning margin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the overrunning skill.</param>
    /// <param name="skillId">The identifier of the overrunning skill node.</param>
    /// <param name="overrunSeconds">Seconds the skill has run past its scheduled finish.</param>
    /// <param name="scheduledFinishSeconds">The scheduled finish time (procedure-relative seconds).</param>
    [LoggerMessage(
        EventId = 2701,
        Level = LogLevel.Warning,
        Message =
            "ADAPTIVE_OVERRUN | Skill='{SkillName}' ({SkillId}) has run {OverrunSeconds:F1}s past its " +
            "scheduled finish of {ScheduledFinishSeconds:F1}s — check whether an upstream task or the operator is blocked")]
    public static partial void LogAdaptiveSkillOverrun(
        this ILogger logger,
        string skillName,
        Guid skillId,
        double overrunSeconds,
        double scheduledFinishSeconds);

    /// <summary>
    ///     Logs that an executing skill has grossly overrun its scheduled finish (escalation margin).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the overrunning skill.</param>
    /// <param name="skillId">The identifier of the overrunning skill node.</param>
    /// <param name="overrunSeconds">Seconds the skill has run past its scheduled finish.</param>
    /// <param name="scheduledFinishSeconds">The scheduled finish time (procedure-relative seconds).</param>
    [LoggerMessage(
        EventId = 2702,
        Level = LogLevel.Warning,
        Message =
            "ADAPTIVE_OVERRUN_ESCALATED | Skill='{SkillName}' ({SkillId}) has run {OverrunSeconds:F1}s past its " +
            "scheduled finish of {ScheduledFinishSeconds:F1}s and is still executing — operator attention recommended")]
    public static partial void LogAdaptiveSkillOverrunEscalated(
        this ILogger logger,
        string skillName,
        Guid skillId,
        double overrunSeconds,
        double scheduledFinishSeconds);

    /// <summary>
    ///     Logs an unexpected error raised inside the monitor's stream handler. The monitor
    ///     swallows the error so it cannot fault the source streams.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The error that occurred.</param>
    [LoggerMessage(
        EventId = 2703,
        Level = LogLevel.Error,
        Message = "ADAPTIVE_OVERRUN_MONITOR_ERROR | The overrun monitor handler raised an error and continued")]
    public static partial void LogAdaptiveSkillMonitorError(
        this ILogger logger,
        Exception exception);
}