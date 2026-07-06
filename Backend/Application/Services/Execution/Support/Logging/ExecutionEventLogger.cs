using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides structured logging for execution events throughout the execution pipeline.
/// </summary>
public static partial class ExecutionEventLogger
{
    /// <summary>
    ///     Logs comprehensive execution event information using high-performance source-generated logging.
    ///     Use this for lifecycle events (START, FINISH, TRIGGER) and prerequisite checking.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">The pipeline phase (e.g., INIT, CHECKING_PREREQUISITES, TRIGGERING_SKILL, COORDINATING_START).</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="executionId">The execution instance ID (nullable).</param>
    /// <param name="agentId">The ID of the agent assigned to or executing the skill (nullable).</param>
    /// <param name="state">The execution state (e.g., PENDING, RUNNING, COMPLETED, FAILED).</param>
    /// <param name="eventType">The execution event type (e.g., START, FINISH, PROGRESS, TRIGGER).</param>
    /// <param name="timestamp">Event timestamp in seconds from procedure start (nullable).</param>
    /// <param name="progressPercentage">Progress percentage (0-100) for PROGRESS events (nullable).</param>
    /// <param name="prerequisitesMet">Number of prerequisites met (nullable).</param>
    /// <param name="prerequisitesTotal">Total number of prerequisites (nullable).</param>
    /// <param name="isAdaptive">Whether the skill supports adaptive duration.</param>
    /// <param name="finishSignalFired">Whether the adaptive finish signal has fired (nullable).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "EXECUTION_EVENT | Phase={Phase} | SkillId={SkillId} | Name='{SkillName}' | " +
                  "ExecutionId={ExecutionId} | AgentId={AgentId} | State={State} | EventType={EventType} | " +
                  "Timestamp={Timestamp:F3}s | Progress={ProgressPercentage:F1}% | " +
                  "Prerequisites={PrerequisitesMet}/{PrerequisitesTotal} | IsAdaptive={IsAdaptive} | " +
                  "FinishSignalFired={FinishSignalFired} | Info={AdditionalInfo}")]
    public static partial void LogExecutionEvent(
        this ILogger logger,
        string phase,
        Guid skillId,
        string skillName,
        Guid? executionId = null,
        Guid? agentId = null,
        string? state = null,
        string? eventType = null,
        double? timestamp = null,
        double? progressPercentage = null,
        int? prerequisitesMet = null,
        int? prerequisitesTotal = null,
        bool? isAdaptive = null,
        bool? finishSignalFired = null,
        string? additionalInfo = null);

    /// <summary>
    ///     Logs progress event information at Trace level (very frequent - fires every 1%).
    ///     Use this specifically for PROGRESS events during skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">The pipeline phase (typically REPORTING_PROGRESS).</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentId">The ID of the agent executing the skill (nullable).</param>
    /// <param name="progressPercentage">Progress percentage (0-100).</param>
    /// <param name="timestamp">Event timestamp in seconds from procedure start (nullable).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Trace,
        Message = "PROGRESS_EVENT | Phase={Phase} | SkillId={SkillId} | Name='{SkillName}' | " +
                  "AgentId={AgentId} | Progress={ProgressPercentage:F1}% | Timestamp={Timestamp:F3}s | Info={AdditionalInfo}")]
    public static partial void LogProgressEvent(
        this ILogger logger,
        string phase,
        Guid skillId,
        string skillName,
        Guid? agentId = null,
        double? progressPercentage = null,
        double? timestamp = null,
        string? additionalInfo = null);

    /// <summary>
    ///     Logs orchestrator-level execution information using high-performance source-generated logging.
    ///     This structured log is intended for machine parsing and log aggregation systems.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">
    ///     The orchestrator phase (e.g., STARTING, INITIALIZING, EXECUTING, RESCHEDULING, COMPLETING,
    ///     CLEANUP).
    /// </param>
    /// <param name="procedureId">The unique identifier of the procedure being executed.</param>
    /// <param name="totalSkills">Total number of skills in the procedure.</param>
    /// <param name="pendingSkills">Number of skills in PENDING state (nullable).</param>
    /// <param name="runningSkills">Number of skills in RUNNING state (nullable).</param>
    /// <param name="completedSkills">Number of skills in COMPLETED state (nullable).</param>
    /// <param name="failedSkills">Number of skills in FAILED state (nullable).</param>
    /// <param name="elapsedTime">Elapsed time since execution start in seconds (nullable).</param>
    /// <param name="rescheduleReason">Reason for rescheduling (optional).</param>
    /// <param name="success">Whether the phase completed successfully (nullable).</param>
    /// <param name="errorMessage">Error message if phase failed (optional).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "EXECUTION_ORCHESTRATOR | Phase={Phase} | ProcedureId={ProcedureId} | " +
                  "TotalSkills={TotalSkills} | Pending={PendingSkills} | Running={RunningSkills} | " +
                  "Completed={CompletedSkills} | Failed={FailedSkills} | ElapsedTime={ElapsedTime:F3}s | " +
                  "RescheduleReason={RescheduleReason} | Success={Success} | Error={ErrorMessage} | Info={AdditionalInfo}")]
    public static partial void LogOrchestratorPhase(
        this ILogger logger,
        string phase,
        Guid procedureId,
        int totalSkills,
        int? pendingSkills = null,
        int? runningSkills = null,
        int? completedSkills = null,
        int? failedSkills = null,
        double? elapsedTime = null,
        string? rescheduleReason = null,
        bool? success = null,
        string? errorMessage = null,
        string? additionalInfo = null);

    /// <summary>
    ///     Logs state transition information using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="executionId">The execution instance ID (nullable).</param>
    /// <param name="fromState">The state being transitioned from.</param>
    /// <param name="toState">The state being transitioned to.</param>
    /// <param name="agentId">The ID of the agent (nullable).</param>
    /// <param name="timestamp">Timestamp of the transition in seconds from procedure start (nullable).</param>
    /// <param name="reason">Reason for the transition (optional).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "STATE_TRANSITION | SkillId={SkillId} | Name='{SkillName}' | ExecutionId={ExecutionId} | " +
                  "Transition={FromState}→{ToState} | AgentId={AgentId} | Timestamp={Timestamp:F3}s | " +
                  "Reason={Reason} | Info={AdditionalInfo}")]
    public static partial void LogStateTransition(
        this ILogger logger,
        Guid skillId,
        string skillName,
        Guid? executionId,
        string fromState,
        string toState,
        Guid? agentId = null,
        double? timestamp = null,
        string? reason = null,
        string? additionalInfo = null);

    /// <summary>
    ///     Logs rescheduling operation information using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">The rescheduling phase (e.g., REQUEST, BUILDING_PROGRESS, COMPUTING_SCHEDULE, UPDATING_NODES).</param>
    /// <param name="reason">The reason for rescheduling.</param>
    /// <param name="skillsUpdated">Number of skills updated during rescheduling (nullable).</param>
    /// <param name="elapsedTime">Time taken for the rescheduling operation in milliseconds (nullable).</param>
    /// <param name="success">Whether the rescheduling was successful (nullable).</param>
    /// <param name="errorMessage">Error message if rescheduling failed (optional).</param>
    /// <param name="additionalInfo">Additional context information (optional).</param>
    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Debug,
        Message = "RESCHEDULING | Phase={Phase} | Reason={Reason} | SkillsUpdated={SkillsUpdated} | " +
                  "ElapsedTime={ElapsedTime:F1}ms | Success={Success} | Error={ErrorMessage} | Info={AdditionalInfo}")]
    public static partial void LogRescheduling(
        this ILogger logger,
        string phase,
        RescheduleReason reason,
        int? skillsUpdated = null,
        double? elapsedTime = null,
        bool? success = null,
        string? errorMessage = null,
        string? additionalInfo = null);

    // ──────────────────────────────────────────────────
    //  ExecutionOrchestrator — lifecycle and publishing
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that an execution is already in progress and a concurrent start request was rejected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Warning,
        Message = "Execution is already in progress — rejecting concurrent start request")]
    public static partial void LogConcurrentStartRejected(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of event-driven execution of a loaded procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Information,
        Message = "Starting event-driven execution of loaded procedure")]
    public static partial void LogExecutionStarting(
        this ILogger logger);

    /// <summary>
    ///     Logs that execution initialization failed with a specific error message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The initialization error message.</param>
    [LoggerMessage(
        EventId = 2012,
        Level = LogLevel.Error,
        Message = "Failed to initialize execution: {ErrorMessage}")]
    public static partial void LogExecutionInitFailed(
        this ILogger logger,
        string? errorMessage);

    /// <summary>
    ///     Logs the start of dependency analysis for event-driven execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2013,
        Level = LogLevel.Information,
        Message = "Analyzing dependencies for event-driven execution")]
    public static partial void LogAnalyzingDependencies(
        this ILogger logger);

    /// <summary>
    ///     Logs an error that occurred in the event bus stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception from the event bus stream.</param>
    [LoggerMessage(
        EventId = 2014,
        Level = LogLevel.Error,
        Message = "Error in event bus stream")]
    public static partial void LogEventBusStreamError(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that the event bus stream has completed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2015,
        Level = LogLevel.Debug,
        Message = "Event bus stream completed")]
    public static partial void LogEventBusStreamCompleted(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of the event-driven execution trigger service.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2016,
        Level = LogLevel.Information,
        Message = "Starting event-driven execution trigger service")]
    public static partial void LogTriggerServiceStarting(
        this ILogger logger);

    /// <summary>
    ///     Logs a failure to start execution of a loaded procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that prevented execution start.</param>
    [LoggerMessage(
        EventId = 2017,
        Level = LogLevel.Error,
        Message = "Failed to start execution of loaded procedure")]
    public static partial void LogExecutionStartFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs the start of event-driven execution cleanup.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2018,
        Level = LogLevel.Information,
        Message = "Cleaning up event-driven execution")]
    public static partial void LogExecutionCleanup(
        this ILogger logger);

    /// <summary>
    ///     Logs a failure to refresh change trackers from repository after execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during refresh.</param>
    [LoggerMessage(
        EventId = 2019,
        Level = LogLevel.Error,
        Message = "Failed to refresh change trackers from repository after execution")]
    public static partial void LogRefreshChangeTrackersFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs the failure of an individual teardown step during execution cleanup. The remaining
    ///     teardown steps still run, so one failing step cannot skip a later one or wedge the next execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="step">The teardown step that failed (for example "OnCompleted" or "StopMonitoring").</param>
    /// <param name="exception">The exception thrown by the step.</param>
    [LoggerMessage(
        EventId = 2020,
        Level = LogLevel.Warning,
        Message = "Execution cleanup step '{Step}' failed; continuing with remaining teardown")]
    public static partial void LogCleanupStepFailed(
        this ILogger logger,
        string step,
        Exception exception);

    /// <summary>
    ///     Logs the final execution completion with success status and statistics.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="completed">The number of completed skills.</param>
    /// <param name="total">The total number of skills.</param>
    /// <param name="failed">The number of failed skills.</param>
    [LoggerMessage(
        EventId = 2021,
        Level = LogLevel.Information,
        Message =
            "Event-driven execution completed. Success: {Success}, Completed: {Completed}/{Total}, Failed: {Failed}")]
    public static partial void LogExecutionCompleted(
        this ILogger logger,
        bool success,
        int completed,
        int total,
        int failed);

    /// <summary>
    ///     Logs that node publishing is skipped during the cleanup phase.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of suppressed node updates.</param>
    [LoggerMessage(
        EventId = 2022,
        Level = LogLevel.Debug,
        Message = "Skipping PublishNodeChanges during cleanup phase ({NodeCount} nodes suppressed)")]
    public static partial void LogNodePublishSkippedDuringCleanup(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs a failure to publish node changes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that prevented publishing.</param>
    [LoggerMessage(
        EventId = 2023,
        Level = LogLevel.Error,
        Message = "Failed to publish node changes")]
    public static partial void LogNodePublishFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that edge publishing is skipped during the cleanup phase.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="edgeCount">The number of suppressed edge updates.</param>
    [LoggerMessage(
        EventId = 2024,
        Level = LogLevel.Debug,
        Message = "Skipping PublishEdgeChanges during cleanup phase ({EdgeCount} edges suppressed)")]
    public static partial void LogEdgePublishSkippedDuringCleanup(
        this ILogger logger,
        int edgeCount);

    /// <summary>
    ///     Logs the publishing of edge changes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="edgeCount">The number of edges being published.</param>
    [LoggerMessage(
        EventId = 2025,
        Level = LogLevel.Debug,
        Message = "Publishing edge changes for {EdgeCount} edges")]
    public static partial void LogEdgePublish(
        this ILogger logger,
        int edgeCount);

    /// <summary>
    ///     Logs a failure to publish edge changes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that prevented publishing.</param>
    [LoggerMessage(
        EventId = 2026,
        Level = LogLevel.Error,
        Message = "Failed to publish edge changes")]
    public static partial void LogEdgePublishFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs a failure to publish execution timing information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that prevented timing publishing.</param>
    [LoggerMessage(
        EventId = 2027,
        Level = LogLevel.Error,
        Message = "Failed to publish execution timing")]
    public static partial void LogTimingPublishFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that adaptive planned finish time updates are skipped during the cleanup phase.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 2028,
        Level = LogLevel.Debug,
        Message = "Skipping UpdateAdaptivePlannedFinishTimes during cleanup phase")]
    public static partial void LogAdaptiveUpdateSkippedDuringCleanup(
        this ILogger logger);

    /// <summary>
    ///     Logs an agent serialization violation detected during pre-execution validation,
    ///     identifying the offending agent and the number of skills and missing FS pairs involved.
    ///     One call is emitted per offending agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The human-readable name of the agent with the violation.</param>
    /// <param name="unserializedSkillCount">
    ///     The number of skills on this agent that participate in at least one missing FS pair.
    /// </param>
    /// <param name="missingPairCount">
    ///     The number of unordered skill pairs on this agent that lack a Finish-to-Start chain.
    /// </param>
    [LoggerMessage(
        EventId = 2029,
        Level = LogLevel.Warning,
        Message =
            "Agent serialization violation: agent '{AgentName}' has {UnserializedSkillCount} skills with {MissingPairCount} missing FS pairs")]
    public static partial void LogAgentSerializationViolation(
        this ILogger logger,
        string agentName,
        int unserializedSkillCount,
        int missingPairCount);
}