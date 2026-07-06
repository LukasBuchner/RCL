using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured logging for Digital Twin agent operations using high-performance source-generated logging.
///     Covers the runtime agent, WebSocket handler, and agent factory components.
/// </summary>
public static partial class DigitalTwinLogger
{
    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinRuntimeAgent – Warnings
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the agent does not have a requested skill in its available skills list.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The display name of the agent.</param>
    /// <param name="skillName">The name of the skill that was requested.</param>
    /// <param name="skillId">The unique identifier of the skill that was requested.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent '{AgentName}' does not have skill '{SkillName}' (ID: {SkillId}) in available skills")]
    public static partial void LogSkillNotAvailable(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a progress message was received for an execution ID that is not tracked.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executionId">The execution identifier that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Received progress for unknown execution {ExecutionId}")]
    public static partial void LogProgressForUnknownExecution(
        this ILogger logger,
        Guid executionId);

    /// <summary>
    ///     Logs that a completion message was received for an execution ID that is not tracked.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executionId">The execution identifier that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Received completion for unknown execution {ExecutionId}")]
    public static partial void LogCompletionForUnknownExecution(
        this ILogger logger,
        Guid executionId);

    /// <summary>
    ///     Logs that a duration estimate response was received for an unknown query ID.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="queryId">The query identifier that was not found in pending estimates.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Received estimate response for unknown query {QueryId}")]
    public static partial void LogEstimateForUnknownQuery(
        this ILogger logger,
        Guid queryId);

    /// <summary>
    ///     Logs that the Digital Twin agent has disconnected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The display name of the disconnected agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Digital Twin agent '{AgentName}' disconnected")]
    public static partial void LogAgentDisconnected(
        this ILogger logger,
        string agentName);

    /// <summary>
    ///     Logs that a message cannot be sent because the WebSocket is not connected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageType">The type of message that could not be sent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cannot send message '{MessageType}' - WebSocket not connected")]
    public static partial void LogCannotSendNotConnected(
        this ILogger logger,
        string messageType);

    /// <summary>
    ///     Logs that a duration estimate request timed out and a nominal fallback is being used.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill whose estimate timed out.</param>
    /// <param name="agentName">The display name of the agent.</param>
    /// <param name="duration">The nominal fallback duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Duration estimate timeout for skill '{SkillName}' on agent '{AgentName}'. Using nominal fallback: {Duration}s")]
    public static partial void LogEstimateTimeout(
        this ILogger logger,
        string skillName,
        string agentName,
        double duration);

    /// <summary>
    ///     Logs that sending a cancel command for a hold execution failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="executionId">The execution identifier for the cancel command.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to send cancel command for hold execution {ExecutionId}")]
    public static partial void LogCancelHoldCommandFailed(
        this ILogger logger,
        Exception exception,
        Guid executionId);

    /// <summary>
    ///     Logs that sending a cancel command for an execution failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="executionId">The execution identifier for the cancel command.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to send cancel command for execution {ExecutionId}")]
    public static partial void LogCancelCommandFailed(
        this ILogger logger,
        Exception exception,
        Guid executionId);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinRuntimeAgent – Errors
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that an execution ID is already active on the agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executionId">The duplicate execution identifier.</param>
    /// <param name="agentName">The display name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Execution ID {ExecutionId} already active on agent '{AgentName}'")]
    public static partial void LogDuplicateExecution(
        this ILogger logger,
        Guid executionId,
        string agentName);

    /// <summary>
    ///     Logs that sending an ExecuteSkillCommand to the Digital Twin failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="executionId">The execution identifier for the failed command.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to send ExecuteSkillCommand for execution {ExecutionId}")]
    public static partial void LogSendExecuteCommandFailed(
        this ILogger logger,
        Exception exception,
        Guid executionId);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinRuntimeAgent – Debug
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs a Hold Position duration estimate derived from the skill's Duration property.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="duration">The duration value from the skill property in seconds.</param>
    /// <param name="min">The minimum adaptive duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Hold Position estimate for skill '{SkillName}': Duration={Duration}s (adaptive min: {Min}s)")]
    public static partial void LogHoldPositionEstimate(
        this ILogger logger,
        string skillName,
        double duration,
        double min);

    /// <summary>
    ///     Logs a duration estimate received from the Digital Twin for a skill.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="duration">The estimated duration in seconds returned by the Twin.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received duration estimate from Twin for skill '{SkillName}': {Duration}s")]
    public static partial void LogDurationEstimateReceived(
        this ILogger logger,
        string skillName,
        double duration);

    /// <summary>
    ///     Logs that the finish signal was received for a Hold Position adaptive execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The display name of the agent.</param>
    /// <param name="executionId">The execution identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Finish signal received for Hold Position (Agent: {AgentName}, Execution: {ExecutionId})")]
    public static partial void LogHoldFinishSignalReceived(
        this ILogger logger,
        string agentName,
        Guid executionId);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinRuntimeAgent – Trace
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs an updated planned finish time for a Hold Position adaptive execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The display name of the agent.</param>
    /// <param name="target">The new target finish time in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Agent {AgentName} Hold Position planned finish updated: {Target:F1}s")]
    public static partial void LogHoldPlannedFinishUpdated(
        this ILogger logger,
        string agentName,
        double target);

    /// <summary>
    ///     Logs the round-trip time for a pong response from the Digital Twin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The display name of the agent.</param>
    /// <param name="rttMs">The round-trip time in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Digital Twin '{AgentName}' pong RTT: {RttMs}ms")]
    public static partial void LogPongRtt(
        this ILogger logger,
        string agentName,
        double rttMs);

    /// <summary>
    ///     Logs that a message was successfully sent to the Digital Twin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageType">The type of message that was sent.</param>
    /// <param name="agentName">The display name of the target agent.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Sent {MessageType} to Digital Twin '{AgentName}'")]
    public static partial void LogMessageSent(
        this ILogger logger,
        string messageType,
        string agentName);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinRuntimeAgent – Information
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a cancel command is being sent for an execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executionId">The execution identifier being cancelled.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sending cancel command for execution {ExecutionId}")]
    public static partial void LogSendingCancelCommand(
        this ILogger logger,
        Guid executionId);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinWebSocketHandler – Information
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a new Digital Twin WebSocket connection has been established.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "New Digital Twin WebSocket connection established")]
    public static partial void LogConnectionEstablished(
        this ILogger logger);

    /// <summary>
    ///     Logs that a Digital Twin has sent its registration message with available skills.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name reported by the Twin during registration.</param>
    /// <param name="skillCount">The number of skill IDs reported by the Twin.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Digital Twin registered as '{AgentName}' with {SkillCount} skills")]
    public static partial void LogTwinRegistered(
        this ILogger logger,
        string agentName,
        int skillCount);

    /// <summary>
    ///     Logs that a stale in-memory agent is being replaced by a new connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent being replaced.</param>
    /// <param name="agentId">The unique identifier of the agent being replaced.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Replacing stale in-memory agent '{AgentName}' (ID: {AgentId}) with new connection")]
    public static partial void LogReplacingStaleAgent(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs that a Digital Twin agent has been registered with the agent manager.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the registered agent.</param>
    /// <param name="agentId">The unique identifier of the registered agent.</param>
    /// <param name="skillCount">The number of resolved skills for the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Registered Digital Twin agent '{AgentName}' (ID: {AgentId}) with {SkillCount} skills")]
    public static partial void LogAgentRegistered(
        this ILogger logger,
        string agentName,
        Guid agentId,
        int skillCount);

    /// <summary>
    ///     Logs that the Digital Twin connection was cancelled due to application shutdown.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Digital Twin connection cancelled (application shutdown)")]
    public static partial void LogConnectionCancelledShutdown(
        this ILogger logger);

    /// <summary>
    ///     Logs that a Digital Twin agent has been unregistered after disconnection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the unregistered agent.</param>
    /// <param name="agentId">The unique identifier of the unregistered agent.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Unregistered Digital Twin agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogAgentUnregistered(
        this ILogger logger,
        string agentName,
        Guid agentId);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinWebSocketHandler – Warnings
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a Digital Twin connection closed before sending a registration message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Digital Twin connection closed before sending registration")]
    public static partial void LogConnectionClosedBeforeRegistration(
        this ILogger logger);

    /// <summary>
    ///     Logs a WebSocket error for a Digital Twin agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The WebSocket exception.</param>
    /// <param name="agentName">The name of the agent, or "unknown" if registration was not completed.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "WebSocket error for Digital Twin agent '{AgentName}'")]
    public static partial void LogWebSocketError(
        this ILogger logger,
        Exception exception,
        string agentName);

    /// <summary>
    ///     Logs that removing the domain record for a disconnected Digital Twin agent failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to remove domain record for Digital Twin agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogDomainRecordRemovalFailed(
        this ILogger logger,
        Exception exception,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs that invalid JSON was received from a Digital Twin during the registration phase.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Received invalid JSON from Digital Twin during registration")]
    public static partial void LogInvalidJsonDuringRegistration(
        this ILogger logger);

    /// <summary>
    ///     Logs that a message type other than Register was received during the registration phase.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageType">The unexpected message type that was received.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Expected Register message but received '{MessageType}'. Closing connection")]
    public static partial void LogUnexpectedMessageDuringRegistration(
        this ILogger logger,
        string messageType);

    /// <summary>
    ///     Logs that a skill definition ID referenced by the Digital Twin is not known.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unknown skill definition identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Digital Twin references unknown skill definition ID {SkillId}. Skipping")]
    public static partial void LogUnknownSkillDefinition(
        this ILogger logger,
        Guid skillId);

    /// <summary>
    ///     Logs that invalid JSON was received from a Digital Twin during the message receive loop.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent that sent the invalid message.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Received invalid JSON from Digital Twin '{AgentName}'")]
    public static partial void LogInvalidJsonFromTwin(
        this ILogger logger,
        string agentName);

    /// <summary>
    ///     Logs a pong timeout for a Digital Twin connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent that timed out.</param>
    /// <param name="elapsed">The time in seconds since the last message was received.</param>
    /// <param name="timeout">The configured timeout threshold in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Pong timeout for Digital Twin '{AgentName}': no message received for {Elapsed:F1}s (limit {Timeout:F1}s)")]
    public static partial void LogPongTimeout(
        this ILogger logger,
        string agentName,
        double elapsed,
        double timeout);

    /// <summary>
    ///     Logs that sending a ping to the Digital Twin failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="agentName">The name of the agent that could not be pinged.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ping failed for Digital Twin '{AgentName}'")]
    public static partial void LogPingFailed(
        this ILogger logger,
        Exception exception,
        string agentName);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinWebSocketHandler – Errors
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs an unexpected error during Digital Twin WebSocket connection handling.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The unexpected exception.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unexpected error handling Digital Twin WebSocket connection")]
    public static partial void LogUnexpectedConnectionError(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs an error processing an incoming message from a Digital Twin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during message processing.</param>
    /// <param name="agentName">The name of the agent that sent the message.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error processing message from Digital Twin '{AgentName}'")]
    public static partial void LogMessageProcessingError(
        this ILogger logger,
        Exception exception,
        string agentName);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinWebSocketHandler – Skill Resolution Diagnostics
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs the position tags loaded from the database for skill property resolution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of position tags loaded.</param>
    /// <param name="tagSummary">A summary string listing each tag's ID, name, and coordinates.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded {Count} position tags for skill resolution: [{TagSummary}]")]
    public static partial void LogPositionTagsLoaded(
        this ILogger logger,
        int count,
        string tagSummary);

    /// <summary>
    ///     Logs the resolved position coordinates for a skill execution from a Position property.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being executed.</param>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate.</param>
    /// <param name="z">Target Z coordinate.</param>
    /// <param name="alpha">Target rotation alpha.</param>
    /// <param name="beta">Target rotation beta.</param>
    /// <param name="gamma">Target rotation gamma.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Skill '{SkillName}' resolved Position: ({X:F3}, {Y:F3}, {Z:F3}) rot ({Alpha:F1}, {Beta:F1}, {Gamma:F1})")]
    public static partial void LogSkillPositionResolved(
        this ILogger logger,
        string skillName,
        double x, double y, double z,
        double alpha, double beta, double gamma);

    /// <summary>
    ///     Logs the resolved position tag and its coordinates for a skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being executed.</param>
    /// <param name="tagName">The human-readable tag name.</param>
    /// <param name="tagId">The unique identifier of the resolved position tag.</param>
    /// <param name="x">Target X coordinate from the tag.</param>
    /// <param name="y">Target Y coordinate from the tag.</param>
    /// <param name="z">Target Z coordinate from the tag.</param>
    /// <param name="alpha">Target rotation alpha from the tag.</param>
    /// <param name="beta">Target rotation beta from the tag.</param>
    /// <param name="gamma">Target rotation gamma from the tag.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Skill '{SkillName}' resolved PositionTag '{TagName}' (ID={TagId}): ({X:F3}, {Y:F3}, {Z:F3}) rot ({Alpha:F1}, {Beta:F1}, {Gamma:F1})")]
    public static partial void LogSkillPositionTagResolved(
        this ILogger logger,
        string skillName,
        string tagName,
        Guid tagId,
        double x, double y, double z,
        double alpha, double beta, double gamma);

    /// <summary>
    ///     Logs that a skill has no Position or PositionTag property, resulting in zero coordinates.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill missing position data.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skill '{SkillName}' has no Position or PositionTag property — sending zero coordinates")]
    public static partial void LogSkillNoPositionProperty(
        this ILogger logger,
        string skillName);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinWebSocketHandler – Debug
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a skill was resolved from a skill definition for a Digital Twin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the resolved skill.</param>
    /// <param name="skillId">The unique identifier of the resolved skill.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Resolved skill '{SkillName}' (ID: {SkillId}) for Digital Twin")]
    public static partial void LogSkillResolved(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that an unknown message type was received from a Digital Twin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageType">The unrecognized message type string.</param>
    /// <param name="agentName">The name of the agent that sent the message.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received unknown message type '{MessageType}' from Digital Twin '{AgentName}'")]
    public static partial void LogUnknownMessageType(
        this ILogger logger,
        string messageType,
        string agentName);

    // ──────────────────────────────────────────────────────────────────────
    //  DigitalTwinAgentFactory – Debug
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that <c>LoadAgentsAsync</c> was called on the Digital Twin factory, which returns
    ///     an empty list because Digital Twin agents connect dynamically via WebSocket.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "DigitalTwinAgentFactory.LoadAgentsAsync called — returning empty list. Digital Twin agents connect dynamically via WebSocket")]
    public static partial void LogLoadAgentsReturningEmpty(
        this ILogger logger);
}