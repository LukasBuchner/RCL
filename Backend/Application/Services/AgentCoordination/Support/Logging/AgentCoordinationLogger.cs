using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;

/// <summary>
///     Provides structured logging for agent coordination operations using high-performance source-generated logging.
/// </summary>
public static partial class AgentCoordinationLogger
{
    /// <summary>
    ///     Logs the start of agent registration at DEBUG level with detailed information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the agent being registered.</param>
    /// <param name="agentName">The name of the agent being registered.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "AGENT_COORDINATION | Operation=Registration | Status=START | AgentId={AgentId} | AgentName={AgentName}")]
    public static partial void LogAgentRegistrationStart(
        this ILogger logger,
        Guid agentId,
        string agentName);

    /// <summary>
    ///     Logs successful completion of agent registration with human-readable message at INFO level.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the registered agent.</param>
    /// <param name="skillCount">Total number of skills associated with the agent.</param>
    /// <param name="skillsChanged">Total number of skills that changed (created + updated).</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Registered agent {AgentName} with {SkillCount} skills ({SkillsChanged} changed)")]
    public static partial void LogAgentRegistrationSuccess(
        this ILogger logger,
        string agentName,
        int skillCount,
        int skillsChanged);

    /// <summary>
    ///     Logs detailed registration completion information at DEBUG level with pipe-separated format.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the registered agent.</param>
    /// <param name="agentName">The name of the registered agent.</param>
    /// <param name="skillCount">Total number of skills associated with the agent.</param>
    /// <param name="skillsCreated">Number of new skills created during registration.</param>
    /// <param name="skillsUpdated">Number of existing skills updated during registration.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "AGENT_COORDINATION | Operation=Registration | Status=SUCCESS | AgentId={AgentId} | AgentName={AgentName} | SkillCount={SkillCount} | SkillsCreated={SkillsCreated} | SkillsUpdated={SkillsUpdated}")]
    public static partial void LogAgentRegistrationSuccessDetailed(
        this ILogger logger,
        Guid agentId,
        string agentName,
        int skillCount,
        int skillsCreated,
        int skillsUpdated);

    /// <summary>
    ///     Logs the start of skill synchronization for an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the agent whose skills are being synchronized.</param>
    /// <param name="agentName">The name of the agent whose skills are being synchronized.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "AGENT_COORDINATION | Operation=SkillSync | Status=START | AgentId={AgentId} | AgentName={AgentName}")]
    public static partial void LogSkillSyncStart(
        this ILogger logger,
        Guid agentId,
        string agentName);

    /// <summary>
    ///     Logs successful completion of skill synchronization with human-readable message.
    ///     Logs at INFO level if changes occurred, DEBUG level if no changes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="processed">Total number of skills processed.</param>
    /// <param name="changed">Total number of skills that changed (created + updated).</param>
    public static void LogSkillSyncComplete(
        this ILogger logger,
        string agentName,
        int processed,
        int changed)
    {
        if (changed > 0)
            LogSkillSyncCompleteWithChanges(logger, processed, agentName, changed);
        else
            LogSkillSyncCompleteNoChanges(logger, processed, agentName);
    }

    /// <summary>
    ///     Logs skill synchronization completion when changes were detected, at INFO level.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processed">Total number of skills processed.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="changed">Number of skills that changed.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Synchronized {Processed} skills for agent {AgentName}: {Changed} changed")]
    private static partial void LogSkillSyncCompleteWithChanges(
        ILogger logger,
        int processed,
        string agentName,
        int changed);

    /// <summary>
    ///     Logs skill synchronization completion when no changes were detected, at DEBUG level.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processed">Total number of skills processed.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Synchronized {Processed} skills for agent {AgentName}: no changes")]
    private static partial void LogSkillSyncCompleteNoChanges(
        ILogger logger,
        int processed,
        string agentName);

    /// <summary>
    ///     Logs detailed skill synchronization completion information at DEBUG level with pipe-separated format.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="processed">Total number of skills processed.</param>
    /// <param name="created">Number of skills created.</param>
    /// <param name="updated">Number of skills updated.</param>
    /// <param name="unchanged">Number of skills that remained unchanged.</param>
    /// <param name="errors">Number of errors encountered during synchronization.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "AGENT_COORDINATION | Operation=SkillSync | Status=COMPLETE | AgentId={AgentId} | AgentName={AgentName} | Processed={Processed} | Created={Created} | Updated={Updated} | Unchanged={Unchanged} | Errors={Errors}")]
    public static partial void LogSkillSyncCompleteDetailed(
        this ILogger logger,
        Guid agentId,
        string agentName,
        int processed,
        int created,
        int updated,
        int unchanged,
        int errors);

    // ── Agent Registration ──────────────────────────────────────────────

    /// <summary>
    ///     Logs that an existing agent is being reactivated after being found in the repository.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent being reactivated.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent '{AgentName}' (ID: {AgentId}) already registered, reactivating")]
    public static partial void LogAgentReactivating(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs a failure to reactivate an existing agent during the update step.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent that failed to reactivate.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to reactivate agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogAgentReactivationFailed(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs successful reactivation of an existing agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the reactivated agent.</param>
    /// <param name="agentId">The unique identifier of the reactivated agent.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reactivated agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogAgentReactivated(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs an attempt to update state for an agent that does not exist in the repository.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the non-existent agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Attempted to update state for non-existent agent: {AgentId}")]
    public static partial void LogAgentStateUpdateNonExistent(
        this ILogger logger,
        Guid agentId);

    /// <summary>
    ///     Logs an agent state change that transitions from one state to another.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent whose state changed.</param>
    /// <param name="oldState">The previous agent state.</param>
    /// <param name="newState">The new agent state.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Agent {AgentName} state changed: {OldState} → {NewState}")]
    public static partial void LogAgentStateChanged(
        this ILogger logger,
        string agentName,
        AgentState oldState,
        AgentState newState);

    /// <summary>
    ///     Logs that an agent state update was a no-op because the state is unchanged.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="state">The current (unchanged) agent state.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent {AgentName} state unchanged: {State}")]
    public static partial void LogAgentStateUnchanged(
        this ILogger logger,
        string agentName,
        AgentState state);

    // ── Domain Agent Lifecycle ──────────────────────────────────────────

    /// <summary>
    ///     Logs successful domain activation completion for a connected agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the activated agent.</param>
    /// <param name="agentId">The unique identifier of the activated agent.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Domain activation completed for agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogDomainActivationCompleted(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs a domain activation failure for a connected agent. The agent remains operational
    ///     but may not appear in queries until the next successful synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent that failed to activate.</param>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="exception">The exception that caused the activation failure.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Domain activation failed for agent '{AgentName}' (ID: {AgentId}) — agent remains operational but may not appear in queries until reconnect")]
    public static partial void LogDomainActivationFailed(
        this ILogger logger,
        string agentName,
        Guid agentId,
        Exception exception);

    /// <summary>
    ///     Logs that an agent has been marked Inactive on disconnect.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the disconnected agent.</param>
    /// <param name="agentId">The unique identifier of the disconnected agent.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Agent '{AgentName}' (ID: {AgentId}) marked Inactive on disconnect")]
    public static partial void LogAgentMarkedInactive(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs that no domain record was found for a disconnected agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the disconnected agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No domain record found for disconnected agent (ID: {AgentId})")]
    public static partial void LogNoRecordForDisconnectedAgent(
        this ILogger logger,
        Guid agentId);

    // ── Skill Synchronization ───────────────────────────────────────────

    /// <summary>
    ///     Logs the number of skills reported by an agent during synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="skillCount">The number of skills the agent reports.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent {AgentName} reports {SkillCount} skills")]
    public static partial void LogAgentReportsSkills(
        this ILogger logger,
        string agentName,
        int skillCount);

    /// <summary>
    ///     Logs a failure to synchronize a specific skill for an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill that failed to sync.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to sync skill {SkillName} for agent {AgentName}")]
    public static partial void LogSkillSyncFailed(
        this ILogger logger,
        string skillName,
        string agentName,
        Exception exception);

    /// <summary>
    ///     Logs a failure of the overall skill synchronization process for an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent whose synchronization failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Skill synchronization failed for agent {AgentName}")]
    public static partial void LogSkillSyncProcessFailed(
        this ILogger logger,
        string agentName,
        Exception exception);

    /// <summary>
    ///     Logs that the service is checking whether a skill exists in the repository.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being checked.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Ensuring skill exists: {SkillName} (ID: {SkillId})")]
    public static partial void LogEnsureSkillExists(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a new skill is being created during synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being created.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Creating new skill: {SkillName}")]
    public static partial void LogCreatingNewSkill(
        this ILogger logger,
        string skillName);

    /// <summary>
    ///     Logs that an existing skill is being updated during synchronization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being updated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating existing skill: {SkillName}")]
    public static partial void LogUpdatingExistingSkill(
        this ILogger logger,
        string skillName);
}