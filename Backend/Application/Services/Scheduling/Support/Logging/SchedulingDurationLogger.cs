using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for duration provider operations including
///     planning mode and execution-aware duration providers.
/// </summary>
public static partial class SchedulingDurationLogger
{
    // ── PlanningModeDurationProvider ─────────────────────────────────────

    /// <summary>
    ///     Logs the start of planning mode capability analysis for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node being analyzed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Planning mode: Analyzing capability for node {NodeId}")]
    public static partial void LogPlanningModeAnalysisStarted(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs a warning when a node could not be mapped to an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the unmapped node.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map node {NodeId} to an agent")]
    public static partial void LogNodeAgentMappingFailed(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs a warning when capability analysis failed for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="skillId">The skill identifier involved.</param>
    /// <param name="agentId">The agent identifier involved.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Capability analysis failed for node {NodeId}, skill {SkillId}, agent {AgentId}")]
    public static partial void LogCapabilityAnalysisFailed(
        this ILogger logger,
        Guid nodeId,
        Guid skillId,
        Guid agentId);

    // ── ExecutionAwareDurationProvider ──────────────────────────────────

    /// <summary>
    ///     Logs that a node has no live execution progress yet because it is not executing, so the planning
    ///     estimate is used. This is the normal pre-execution path for a not-yet-started node, not a degraded
    ///     substitution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="executionId">The execution identifier (or "null" if not set).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_DURATION | Phase=NO_LIVE_PROGRESS | Node has no live execution progress yet; using planning estimate. NodeId={NodeId} | ExecutionId={ExecutionId}")]
    public static partial void LogNoLiveProgressUsingPlanningEstimate(
        this ILogger logger,
        Guid nodeId,
        string executionId);

    /// <summary>
    ///     Logs a warning when the planning provider returned null during execution-aware analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="executionId">The execution identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SKILL_TIMING | Phase=DURATION_PROVIDER | SkillId={NodeId} | ExecutionId={ExecutionId} | State=PLANNING_FAILED | Error=PlanningProviderReturnedNull")]
    public static partial void LogPlanningProviderReturnedNull(
        this ILogger logger,
        Guid nodeId,
        Guid executionId);
}