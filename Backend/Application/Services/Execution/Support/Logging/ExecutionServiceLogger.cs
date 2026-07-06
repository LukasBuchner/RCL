using FHOOE.Freydis.Application.Services.Execution.Events;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides high-performance source-generated logging for execution service operations.
///     Covers EventBus (hot path), Coordinator binding operations, and Router evaluation.
/// </summary>
public static partial class ExecutionServiceLogger
{
    // ──────────────────────────────────────────────────
    //  Event Bus (hot path — fires on every event)
    // ──────────────────────────────────────────────────

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Trace,
        Message = "Publishing {EventType} event for skill {SkillId} at {Timestamp}")]
    public static partial void LogEventBusPublish(
        this ILogger logger,
        ExecutionEventType eventType,
        Guid skillId,
        DateTimeOffset timestamp);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "SkillExecutionEventBus initialized")]
    public static partial void LogEventBusInitialized(this ILogger logger);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Disposing SkillExecutionEventBus")]
    public static partial void LogEventBusDisposing(this ILogger logger);

    // ──────────────────────────────────────────────────
    //  Coordinator — binding resolution
    // ──────────────────────────────────────────────────

    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Debug,
        Message = "Resolving input bindings for {SkillType} '{SkillName}'")]
    public static partial void LogResolvingInputBindings(
        this ILogger logger,
        string skillType,
        string skillName);

    [LoggerMessage(
        EventId = 4011,
        Level = LogLevel.Debug,
        Message = "Resolved {Count} input bindings for {SkillType} '{SkillName}'")]
    public static partial void LogResolvedInputBindings(
        this ILogger logger,
        int count,
        string skillType,
        string skillName);

    [LoggerMessage(
        EventId = 4012,
        Level = LogLevel.Debug,
        Message = "Applying output bindings for {SkillType} '{SkillName}'")]
    public static partial void LogApplyingOutputBindings(
        this ILogger logger,
        string skillType,
        string skillName);

    [LoggerMessage(
        EventId = 4013,
        Level = LogLevel.Debug,
        Message = "Successfully applied output bindings for {SkillType} '{SkillName}'")]
    public static partial void LogAppliedOutputBindings(
        this ILogger logger,
        string skillType,
        string skillName);

    [LoggerMessage(
        EventId = 4014,
        Level = LogLevel.Warning,
        Message = "Failed to apply output bindings for {SkillType} '{SkillName}': {ErrorMessage}")]
    public static partial void LogOutputBindingsFailed(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        string errorMessage);

    [LoggerMessage(
        EventId = 4015,
        Level = LogLevel.Warning,
        Message = "Failed to write skill outputs to variable context for {SkillType} '{SkillName}': {ErrorMessage}")]
    public static partial void LogVariableWriteFailed(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        string errorMessage);

    [LoggerMessage(
        EventId = 4016,
        Level = LogLevel.Error,
        Message = "Agent '{AgentName}' ({AgentId}) not found for {SkillType} '{SkillName}' ({SkillId})")]
    public static partial void LogAgentNotFound(
        this ILogger logger,
        string agentName,
        Guid agentId,
        string skillType,
        string skillName,
        Guid skillId);

    [LoggerMessage(
        EventId = 4017,
        Level = LogLevel.Error,
        Message = "{SkillType} '{SkillName}' on agent '{AgentName}' failed: {ErrorMessage}")]
    public static partial void LogSkillExecutionFailed(
        this ILogger logger,
        string skillType,
        string skillName,
        string agentName,
        string errorMessage);

    [LoggerMessage(
        EventId = 4018,
        Level = LogLevel.Error,
        Message = "Error executing {SkillType} '{SkillName}' on agent '{AgentName}'")]
    public static partial void LogSkillExecutionError(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        string agentName);

    [LoggerMessage(
        EventId = 4019,
        Level = LogLevel.Error,
        Message = "Failed to start execution of {SkillType} '{SkillName}' on agent '{AgentName}'")]
    public static partial void LogSkillStartFailed(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        string agentName);

    [LoggerMessage(
        EventId = 4020,
        Level = LogLevel.Error,
        Message = "Variable '{VariableName}' not found while resolving input bindings for {SkillType} '{SkillName}'")]
    public static partial void LogVariableNotFound(
        this ILogger logger,
        Exception ex,
        string variableName,
        string skillType,
        string skillName);

    [LoggerMessage(
        EventId = 4021,
        Level = LogLevel.Error,
        Message = "Type mismatch while resolving input bindings for {SkillType} '{SkillName}': {ErrorMessage}")]
    public static partial void LogTypeMismatch(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        string errorMessage);

    // ──────────────────────────────────────────────────
    //  Trigger Service — error callbacks
    // ──────────────────────────────────────────────────

    [LoggerMessage(
        EventId = 4030,
        Level = LogLevel.Error,
        Message = "Error executing {SkillType} '{SkillName}' ({SkillId})")]
    public static partial void LogTriggerExecutionError(
        this ILogger logger,
        Exception ex,
        string skillType,
        string skillName,
        Guid skillId);

    [LoggerMessage(
        EventId = 4031,
        Level = LogLevel.Debug,
        Message = "{SkillType} '{SkillName}' ({SkillId}) execution stream completed")]
    public static partial void LogTriggerStreamCompleted(
        this ILogger logger,
        string skillType,
        string skillName,
        Guid skillId);

    [LoggerMessage(
        EventId = 4032,
        Level = LogLevel.Error,
        Message = "Skill '{SkillName}' ({SkillId}) failed: {ErrorMessage}")]
    public static partial void LogSkillProgressFailed(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string? errorMessage);

    // ──────────────────────────────────────────────────
    //  State Manager
    // ──────────────────────────────────────────────────

    [LoggerMessage(
        EventId = 4040,
        Level = LogLevel.Information,
        Message = "Initialized state manager with {SkillCount} skills and {AgentCount} agent assignments")]
    public static partial void LogStateManagerInitialized(
        this ILogger logger,
        int skillCount,
        int agentCount);

    [LoggerMessage(
        EventId = 4041,
        Level = LogLevel.Warning,
        Message = "Cannot update state for '{SkillName}' ({SkillId}): state not found")]
    public static partial void LogStateNotFound(
        this ILogger logger,
        string skillName,
        Guid skillId);

    [LoggerMessage(
        EventId = 4042,
        Level = LogLevel.Warning,
        Message = "Rejected mutation on terminal skill execution '{SkillName}' ({SkillId}): " +
                  "current status is {Status} and once terminal must remain terminal")]
    public static partial void LogTerminalStateTransitionRejected(
        this ILogger logger,
        string skillName,
        Guid skillId,
        StateManagement.ExecutionStatus status);

    // ──────────────────────────────────────────────────
    //  SkillTriggerHandler
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a skill is already being triggered and the duplicate trigger is skipped.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skill '{SkillName}' ({SkillId}) is already being triggered, skipping to prevent re-entry")]
    public static partial void LogSkillAlreadyTriggering(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a skill node was not found in the skill nodes dictionary.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Skill node '{SkillName}' ({SkillId}) not found in skill nodes")]
    public static partial void LogSkillNodeNotFound(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that prerequisites for a skill were not found in the dependency graph.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Prerequisites for skill '{SkillName}' ({SkillId}) not found in dependency graph")]
    public static partial void LogPrerequisitesNotFound(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a planned finish update is skipped because the skill is not adaptive or not executing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Skipping planned finish update for skill ({SkillId}) (not adaptive or not executing)")]
    public static partial void LogPlannedFinishUpdateSkipped(
        this ILogger logger,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  RouterTriggerHandler
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a router is already being evaluated and the duplicate evaluation is skipped.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router '{RouterName}' ({RouterId}) is already being evaluated, skipping to prevent re-entry")]
    public static partial void LogRouterAlreadyEvaluating(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the start of router evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Evaluating router '{RouterName}' (ID: {RouterId})")]
    public static partial void LogRouterEvaluationStarted(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the router's selected branch targeting a specific node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="targetNodeId">The unique identifier of the selected target node.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Router '{RouterName}' (ID: {RouterId}) selected branch targeting node {TargetNodeId}")]
    public static partial void LogRouterBranchTargetSelected(
        this ILogger logger,
        string routerName,
        Guid routerId,
        Guid targetNodeId);

    /// <summary>
    ///     Logs that a node is in a non-selected branch and will not be executed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the non-selected node.</param>
    /// <param name="nodeId">The unique identifier of the non-selected node.</param>
    /// <param name="routerName">The name of the router that made the selection.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Node '{NodeName}' ({NodeId}) not selected: in non-selected branch of router '{RouterName}' ({RouterId})")]
    public static partial void LogNodeNotSelected(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the number of branch nodes a router is waiting for before publishing Finish.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="count">The number of branch nodes to wait for.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Router '{RouterName}' ({RouterId}) waiting for {Count} branch node(s) to complete before publishing Finish")]
    public static partial void LogRouterWaitingForBranchNodes(
        this ILogger logger,
        string routerName,
        Guid routerId,
        int count);

    /// <summary>
    ///     Logs that all branch skills have completed and the router has finished.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Router '{RouterName}' (ID: {RouterId}) finished — all branch skills completed")]
    public static partial void LogRouterFinished(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a router was cancelled while waiting for branch skills.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router '{RouterName}' (ID: {RouterId}) cancelled while waiting for branch skills")]
    public static partial void LogRouterCancelled(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs a failure during router evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during router evaluation.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Router evaluation failed for '{RouterName}' (ID: {RouterId})")]
    public static partial void LogRouterEvaluationFailed(
        this ILogger logger,
        Exception exception,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the IsSelectedBranch check for a node with its router prerequisites count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the node being checked.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="count">The number of router prerequisites found.</param>
    /// <param name="totalPrereqs">The total number of start prerequisites.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "IsSelectedBranch: '{NodeName}' ({NodeId}), RouterPrereqs={Count}, TotalStartPrereqs={TotalPrereqs}")]
    public static partial void LogIsSelectedBranchCheck(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        int count,
        int totalPrereqs);

    /// <summary>
    ///     Logs a fallback check when a node has no router prerequisites but is inside a router hierarchy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="routerName">The name of the ancestor router.</param>
    /// <param name="routerId">The unique identifier of the ancestor router.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "FALLBACK: '{NodeName}' ({NodeId}) has NO router prerequisites but IS inside router '{RouterName}' ({RouterId}). " +
            "Checking router selection directly. This should not happen if DependencyGraphAnalyzer is working correctly.")]
    public static partial void LogFallbackRouterCheck(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a node is blocked because a router has not yet made its selection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the blocked node.</param>
    /// <param name="nodeId">The unique identifier of the blocked node.</param>
    /// <param name="routerName">The name of the router that has not selected.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "'{NodeName}' ({NodeId}) blocked: Router '{RouterName}' ({RouterId}) hasn't made selection yet")]
    public static partial void LogNodeBlockedByRouter(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a node was skipped because it is not in the selected branch (with fallback context).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the skipped node.</param>
    /// <param name="nodeId">The unique identifier of the skipped node.</param>
    /// <param name="targetName">The name of the selected target node.</param>
    /// <param name="selectedTarget">The unique identifier of the selected target.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "'{NodeName}' ({NodeId}) skipped (FALLBACK): Not in selected branch '{TargetName}' ({SelectedTarget}) of router '{RouterName}' ({RouterId})")]
    public static partial void LogNodeSkippedFallback(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string targetName,
        Guid selectedTarget,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a node was allowed by the fallback check because it is in the selected branch.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the allowed node.</param>
    /// <param name="nodeId">The unique identifier of the allowed node.</param>
    /// <param name="targetName">The name of the selected target node.</param>
    /// <param name="selectedTarget">The unique identifier of the selected target.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "'{NodeName}' ({NodeId}) allowed (FALLBACK): In selected branch '{TargetName}' ({SelectedTarget}) of router '{RouterName}' ({RouterId})")]
    public static partial void LogNodeAllowedFallback(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string targetName,
        Guid selectedTarget,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a node is allowed because it has a Finish-Start edge to a router but is external to the router hierarchy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "'{NodeName}' ({NodeId}) allowed: Has FS to router '{RouterName}' ({RouterId}) but is external (not inside router hierarchy)")]
    public static partial void LogNodeAllowedExternalToRouter(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs that a node was skipped because it is not in the selected branch.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the skipped node.</param>
    /// <param name="nodeId">The unique identifier of the skipped node.</param>
    /// <param name="targetName">The name of the selected target node.</param>
    /// <param name="selectedTarget">The unique identifier of the selected target.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "'{NodeName}' ({NodeId}) skipped: Not in selected branch '{TargetName}' ({SelectedTarget}) of router '{RouterName}' ({RouterId})")]
    public static partial void LogNodeSkipped(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string targetName,
        Guid selectedTarget,
        string routerName,
        Guid routerId);

    // ──────────────────────────────────────────────────
    //  ExecutionTriggerService
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the ExecutionTriggerService is already started and cannot be started again.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ExecutionTriggerService is already started")]
    public static partial void LogTriggerServiceAlreadyStarted(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of the ExecutionTriggerService with the number of skills and routers.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of skills in the dependency graph.</param>
    /// <param name="routerCount">The number of router nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting ExecutionTriggerService with {SkillCount} skills and {RouterCount} routers")]
    public static partial void LogTriggerServiceStarting(
        this ILogger logger,
        int skillCount,
        int routerCount);

    /// <summary>
    ///     Logs an error that occurred in the event stream subscription.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception from the event stream.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error in event stream")]
    public static partial void LogEventStreamError(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that the event stream has completed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Event stream completed")]
    public static partial void LogEventStreamCompleted(
        this ILogger logger);

    /// <summary>
    ///     Logs that the ExecutionTriggerService is not currently started.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ExecutionTriggerService is not started")]
    public static partial void LogTriggerServiceNotStarted(
        this ILogger logger);

    /// <summary>
    ///     Logs the stopping of the ExecutionTriggerService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stopping ExecutionTriggerService")]
    public static partial void LogTriggerServiceStopping(
        this ILogger logger);

    /// <summary>
    ///     Logs a failure to update adaptive planned finish times.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to update adaptive planned finish times")]
    public static partial void LogAdaptivePlannedFinishUpdateFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that router prerequisites have been met for a router node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router prerequisites met: {RouterName} (ID: {RouterId})")]
    public static partial void LogRouterPrerequisitesMet(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the injection of an ancestor router gate prerequisite for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the node that will wait.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="routerName">The name of the ancestor router.</param>
    /// <param name="routerId">The unique identifier of the ancestor router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Injecting ancestor router gate: '{NodeName}' ({NodeId}) will wait for '{RouterName}' ({RouterId}) Start")]
    public static partial void LogAncestorRouterGateInjected(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs an unhandled exception that escaped TriggerRouterAsync.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception escaped TriggerRouterAsync for router {RouterId}")]
    public static partial void LogTriggerRouterUnhandledException(
        this ILogger logger,
        Exception exception,
        Guid routerId);

    /// <summary>
    ///     Logs that a node was not found in either skill or router node collections.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeName">The name of the node (resolved or "Unknown Node").</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Node '{NodeName}' ({NodeId}) not found in skill or router nodes")]
    public static partial void LogNodeNotFoundInCollections(
        this ILogger logger,
        string nodeName,
        Guid nodeId);

    /// <summary>
    ///     Logs that a leafless container fired as a zero-extent self-gating endpoint (Start then Finish).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the leafless container.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Leafless container ({NodeId}) fired as a zero-extent endpoint (Start then Finish)")]
    public static partial void LogLeaflessEndpointFired(
        this ILogger logger,
        Guid nodeId);
}