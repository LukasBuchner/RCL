using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides high-performance source-generated logging for the reactive execution pipeline steps.
///     Each method corresponds to a numbered step in the execution pipeline diagram:
///     Inner loop (1–4b) and outer loop (5–11).
/// </summary>
public static partial class PipelineEventLogger
{
    // ──────────────────────────────────────────────────
    //  Step 1: Event received by Trigger Service
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (1) Trigger Service received a lifecycle event (Start/Finish) from the Event Bus.
    /// </summary>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "(1) Received {EventType} for '{NodeName}' ({NodeId})")]
    public static partial void LogEventReceived(
        this ILogger logger,
        string eventType,
        string nodeName,
        Guid nodeId);

    /// <summary>
    ///     (1) Trigger Service received a Progress event from the Event Bus (high frequency).
    /// </summary>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Trace,
        Message = "(1) Received {EventType} for '{NodeName}' ({NodeId})")]
    public static partial void LogProgressEventReceived(
        this ILogger logger,
        string eventType,
        string nodeName,
        Guid nodeId);

    // ──────────────────────────────────────────────────
    //  Step 2: Prerequisites met, triggering skill
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (2) All prerequisites met, triggering skill execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Debug,
        Message = "(2) Prerequisites met, triggering '{SkillName}' ({SkillId}) on '{AgentName}'")]
    public static partial void LogPrerequisitesMet(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    /// <summary>
    ///     (2) All prerequisites met, triggering adaptive skill execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "(2) Prerequisites met, triggering adaptive '{SkillName}' ({SkillId}) on '{AgentName}'")]
    public static partial void LogAdaptivePrerequisitesMet(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    // ──────────────────────────────────────────────────
    //  Step 3: Coordinator dispatches to agent
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (3) Skill Coordinator dispatching skill to runtime agent.
    /// </summary>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "(3) Dispatching '{SkillName}' ({SkillId}) to agent '{AgentName}'")]
    public static partial void LogDispatchToAgent(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    /// <summary>
    ///     (3) Skill Coordinator dispatching adaptive skill to runtime agent.
    /// </summary>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "(3) Dispatching adaptive '{SkillName}' ({SkillId}) to agent '{AgentName}' (initial={Duration}s)")]
    public static partial void LogAdaptiveDispatchToAgent(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName,
        string duration);

    // ──────────────────────────────────────────────────
    //  Step 4a: Agent streams events back to Coordinator
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (4a) Agent started skill execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Debug,
        Message = "(4a) Agent '{AgentName}' started '{SkillName}' ({SkillId})")]
    public static partial void LogAgentStarted(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     (4a) Agent completed skill execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Debug,
        Message = "(4a) Agent '{AgentName}' completed '{SkillName}' ({SkillId})")]
    public static partial void LogAgentCompleted(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  Step 4b: Coordinator publishes to Event Bus
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (4b) Coordinator published event to Event Bus.
    /// </summary>
    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Trace,
        Message = "(4b) Published {EventType} for '{SkillName}' ({SkillId})")]
    public static partial void LogEventPublished(
        this ILogger logger,
        string eventType,
        string skillName,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  Step 5: Orchestrator receives event from Event Bus
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (5) Execution Orchestrator received a lifecycle event from the Event Bus.
    /// </summary>
    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Debug,
        Message = "(5) Orchestrator received {EventType} for '{SkillName}' ({SkillId})")]
    public static partial void LogOrchestratorEventReceived(
        this ILogger logger,
        string eventType,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     (5) Execution Orchestrator received a Progress event (high frequency).
    /// </summary>
    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Trace,
        Message = "(5) Orchestrator received Progress for '{SkillName}' ({SkillId})")]
    public static partial void LogOrchestratorProgressReceived(
        this ILogger logger,
        string skillName,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  Step 6: State Manager updated
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (6) State Manager updated with new execution state.
    /// </summary>
    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Debug,
        Message = "(6) State -> {NewState} for '{SkillName}' ({SkillId})")]
    public static partial void LogStateUpdated(
        this ILogger logger,
        string newState,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     (6) State Manager updated with progress (high frequency).
    /// </summary>
    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Trace,
        Message = "(6) Progress updated for '{SkillName}' ({SkillId}): {Progress}%")]
    public static partial void LogProgressUpdated(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string progress);

    // ──────────────────────────────────────────────────
    //  Step 7: Reschedule request sampled
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (7) Rate-limited reschedule request sampled from the Rx.NET pipeline.
    /// </summary>
    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Debug,
        Message = "(7) Reschedule sampled, reason='{Reason}'")]
    public static partial void LogRescheduleSampled(
        this ILogger logger,
        RescheduleReason reason);

    // ──────────────────────────────────────────────────
    //  Step 8a–8c: Rescheduling pipeline
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (8a) Rescheduling Coordinator querying State Manager for execution progress.
    /// </summary>
    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Debug,
        Message = "(8a) Querying State Manager for execution progress")]
    public static partial void LogQueryStateManager(
        this ILogger logger);

    /// <summary>
    ///     (8b) Rescheduling Coordinator invoking Timing Orchestrator.
    /// </summary>
    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Debug,
        Message = "(8b) Computing schedule with {NodeCount} nodes")]
    public static partial void LogComputingSchedule(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     (8c) Timing Orchestrator returned computed schedule.
    /// </summary>
    [LoggerMessage(
        EventId = 1017,
        Level = LogLevel.Debug,
        Message = "(8c) Schedule computed: {UpdatedCount} nodes updated in {ElapsedMs}ms")]
    public static partial void LogScheduleComputed(
        this ILogger logger,
        int updatedCount,
        string elapsedMs);

    // ──────────────────────────────────────────────────
    //  Step 9: Updated schedule returned to Orchestrator
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (9) Rescheduled result returned to Execution Orchestrator.
    /// </summary>
    [LoggerMessage(
        EventId = 1018,
        Level = LogLevel.Debug,
        Message =
            "(9) Rescheduled at t={CurrentTime}s, reason='{Reason}', {UpdatedCount} nodes updated in {ElapsedMs}ms")]
    public static partial void LogRescheduled(
        this ILogger logger,
        string currentTime,
        RescheduleReason reason,
        int updatedCount,
        string elapsedMs);

    // ──────────────────────────────────────────────────
    //  Steps 10a–10c: Timing updates forwarded
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (10a) Execution Orchestrator sending planned finish time to Trigger Service.
    /// </summary>
    [LoggerMessage(
        EventId = 1019,
        Level = LogLevel.Debug,
        Message = "(10a) Sending planned finish {PlannedFinish}s to '{SkillName}' ({SkillId})")]
    public static partial void LogSendingPlannedFinish(
        this ILogger logger,
        string plannedFinish,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     (10b) Trigger Service forwarding planned finish time to Skill Coordinator.
    /// </summary>
    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Debug,
        Message = "(10b) Forwarding planned finish {PlannedFinish}s for '{SkillName}' ({SkillId})")]
    public static partial void LogForwardingPlannedFinish(
        this ILogger logger,
        string plannedFinish,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     (10c) Agent received updated planned finish time.
    /// </summary>
    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Debug,
        Message = "(10c) Agent '{AgentName}' received planned finish {PlannedFinish}s for '{SkillName}' ({SkillId})")]
    public static partial void LogAgentReceivedPlannedFinish(
        this ILogger logger,
        string agentName,
        string plannedFinish,
        string skillName,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  Step 11: Frontend receives updates
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     (11) Publishing node updates to frontend via GraphQL subscriptions.
    /// </summary>
    [LoggerMessage(
        EventId = 1022,
        Level = LogLevel.Debug,
        Message = "(11) Publishing {NodeCount} node updates to frontend")]
    public static partial void LogPublishToFrontend(
        this ILogger logger,
        int nodeCount);

    // ──────────────────────────────────────────────────
    //  ExecutionEventDispatcher — router & skill events
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a router event received by the ExecutionEventDispatcher.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="eventType">The type of execution event.</param>
    /// <param name="timestamp">The event timestamp in seconds from procedure start.</param>
    [LoggerMessage(
        EventId = 1023,
        Level = LogLevel.Debug,
        Message =
            "Router event received: {RouterName} (ID: {RouterId}) | EventType={EventType} | Timestamp={Timestamp:F2}s")]
    public static partial void LogRouterEventReceived(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string eventType,
        double timestamp);

    /// <summary>
    ///     Logs that a node was not found for an execution event.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the unresolved node.</param>
    [LoggerMessage(
        EventId = 1024,
        Level = LogLevel.Warning,
        Message = "Node not found for event '{NodeId}' (not a SkillExecutionNode or RouterNode)")]
    public static partial void LogNodeNotFoundForEvent(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs that no agent was found for a skill on a Start event.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        EventId = 1025,
        Level = LogLevel.Warning,
        Message = "No agent found for skill '{SkillName}' ({SkillId}) on Start event")]
    public static partial void LogNoAgentForSkillStart(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a leafless container event was received; it carries a dependency chain and requires no
    ///     state transition.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskName">The name of the leafless container task.</param>
    /// <param name="taskId">The unique identifier of the leafless container task.</param>
    /// <param name="eventType">The type of execution event.</param>
    /// <param name="timestamp">The event timestamp in seconds from procedure start.</param>
    [LoggerMessage(
        EventId = 1026,
        Level = LogLevel.Debug,
        Message =
            "Leafless container event received: {TaskName} (ID: {TaskId}) | EventType={EventType} | Timestamp={Timestamp:F2}s (no state transition)")]
    public static partial void LogLeaflessContainerEventReceived(
        this ILogger logger,
        string taskName,
        Guid taskId,
        string eventType,
        double timestamp);

    /// <summary>
    ///     Logs that a Failed execution event arrived without an error message, so a placeholder
    ///     string is recorded as the failure reason in the persisted skill state.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill whose Failed event carried no message.</param>
    /// <param name="skillId">The unique identifier of the skill node.</param>
    /// <param name="placeholder">The placeholder string substituted for the missing error message.</param>
    [LoggerMessage(
        EventId = 1027,
        Level = LogLevel.Warning,
        Message =
            "Failed event for skill {SkillName} ({SkillId}) carried no ErrorMessage; recording placeholder '{Placeholder}' as the failure reason")]
    public static partial void LogFailedEventMissingErrorMessage(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string placeholder);
}