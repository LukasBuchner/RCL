using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for skill timing information throughout the scheduling pipeline.
/// </summary>
public static partial class SkillTimingLogger
{
    /// <summary>
    ///     Logs comprehensive skill timing information using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">The pipeline phase (e.g., PROGRESS_COLLECTION, DURATION_PROVIDER, PLAN_START).</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentId">The ID of the agent assigned to or executing the skill.</param>
    /// <param name="executionId">The execution instance ID (nullable).</param>
    /// <param name="state">The execution state (e.g., NOT_STARTED, RUNNING, FINISHED).</param>
    /// <param name="isAdaptive">Whether the skill supports adaptive duration.</param>
    /// <param name="plannedStart">Planned start time in seconds from procedure start (nullable).</param>
    /// <param name="plannedFinish">Planned finish time in seconds from procedure start (nullable).</param>
    /// <param name="plannedDuration">Planned duration in seconds (nullable).</param>
    /// <param name="actualStart">Actual start time in seconds from procedure start (nullable).</param>
    /// <param name="actualFinish">Actual finish time in seconds from procedure start (nullable).</param>
    /// <param name="estimatedDuration">Estimated duration in seconds (nullable).</param>
    /// <param name="currentTime">Current time in seconds from procedure start (nullable).</param>
    /// <param name="minDuration">Minimum achievable duration for adaptive skills (nullable).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "SKILL_TIMING | Phase={Phase} | SkillId={SkillId} | Name={SkillName} | AgentId={AgentId} | " +
                  "ExecutionId={ExecutionId} | State={State} | IsAdaptive={IsAdaptive} | " +
                  "PlannedStart={PlannedStart:F3}s | PlannedFinish={PlannedFinish:F3}s | PlannedDuration={PlannedDuration:F3}s | " +
                  "ActualStart={ActualStart:F3}s | ActualFinish={ActualFinish:F3}s | EstimatedDuration={EstimatedDuration:F3}s | " +
                  "CurrentTime={CurrentTime:F3}s | MinDuration={MinDuration:F3}s | " +
                  "Info={AdditionalInfo}")]
    public static partial void LogSkillTiming(
        this ILogger logger,
        string phase,
        Guid skillId,
        string skillName,
        Guid agentId,
        Guid? executionId,
        string state,
        bool isAdaptive,
        double? plannedStart = null,
        double? plannedFinish = null,
        double? plannedDuration = null,
        double? actualStart = null,
        double? actualFinish = null,
        double? estimatedDuration = null,
        double? currentTime = null,
        double? minDuration = null,
        string? additionalInfo = null);
}