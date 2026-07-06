using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;

/// <summary>
///     Provides structured logging for agent capability analysis operations using high-performance source-generated
///     logging.
/// </summary>
public static partial class AgentCapabilityLogger
{
    /// <summary>
    ///     Logs the start of capability analysis for a skill execution node.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CAPABILITY_ANALYSIS | Status=STARTING | NodeId={NodeId} | SkillName={SkillName} | AgentName={AgentName} | AgentId={AgentId}")]
    public static partial void LogCapabilityAnalysisStart(
        this ILogger logger,
        Guid nodeId,
        string skillName,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs the adaptive capability check for an agent.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CAPABILITY_ANALYSIS | Type=AdaptiveCheck | AgentName={AgentName} | SkillName={SkillName} | IsAdaptive={IsAdaptive} | HasExecutionConstraints={HasExecutionConstraints}")]
    public static partial void LogAdaptiveCapabilityCheck(
        this ILogger logger,
        string agentName,
        string skillName,
        bool isAdaptive,
        bool hasExecutionConstraints);

    /// <summary>
    ///     Logs the retrieval of execution constraints for an agent skill.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CAPABILITY_ANALYSIS | Type=ExecutionConstraints | AgentId={AgentId} | SkillName={SkillName} | MinDuration={MinDuration}")]
    public static partial void LogExecutionConstraintsRetrieved(
        this ILogger logger,
        Guid agentId,
        string skillName,
        int minDuration);

    /// <summary>
    ///     Logs detection of zero duration in skill capability.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "CAPABILITY_ANALYSIS | Type=ZeroDuration | Status=DETECTED | SkillName={SkillName} | AgentName={AgentName} | DurationType={DurationType}")]
    public static partial void LogZeroDurationDetected(
        this ILogger logger,
        string skillName,
        string agentName,
        string durationType);

    /// <summary>
    ///     Logs the creation of a planned skill execution.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CAPABILITY_ANALYSIS | Type=PlannedSkillCreated | Status=SUCCESS | SkillId={SkillId} | SkillName={SkillName} | AgentName={AgentName} | Duration={Duration} | IsAdaptive={IsAdaptive}")]
    public static partial void LogPlannedSkillCreated(
        this ILogger logger,
        Guid skillId,
        string skillName,
        string agentName,
        int duration,
        bool isAdaptive);

    /// <summary>
    ///     Logs an error during capability analysis.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "CAPABILITY_ANALYSIS | Status=ERROR | NodeId={NodeId} | SkillName={SkillName} | ErrorMessage={ErrorMessage}")]
    public static partial void LogCapabilityAnalysisError(
        this ILogger logger,
        Guid nodeId,
        string skillName,
        string errorMessage);

    /// <summary>
    ///     Logs that no execution constraints were returned for a skill on an agent,
    ///     meaning the agent does not have the skill or could not provide estimates.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill for which no constraints were returned.</param>
    /// <param name="agentName">The name of the agent that failed to provide constraints.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "No execution constraints returned for skill '{SkillName}' on agent '{AgentName}'. This means the agent doesn't have this skill or couldn't provide estimates.")]
    public static partial void LogNoExecutionConstraints(
        this ILogger logger,
        string skillName,
        string agentName);

    /// <summary>
    ///     Logs a failure to create a planned skill execution, including the adaptive status and constraint availability.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill for which creation failed.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="isAdaptive">Whether the agent reported the skill as adaptive.</param>
    /// <param name="hasConstraints">Whether execution constraints were available.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Failed to create planned skill execution for skill '{SkillName}' on agent '{AgentName}'. IsAdaptive={IsAdaptive}, HasConstraints={HasConstraints}")]
    public static partial void LogPlannedSkillCreationFailed(
        this ILogger logger,
        string skillName,
        string agentName,
        bool isAdaptive,
        bool hasConstraints);

    // ── Node-Agent Mapping ──────────────────────────────────────────────

    /// <summary>
    ///     Logs that the domain agent for a node was not found in the application service.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The ID of the agent that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} not found.")]
    public static partial void LogAgentNotFound(
        this ILogger logger,
        Guid agentId);

    /// <summary>
    ///     Logs a runtime agent lookup attempt during node-agent mapping.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The ID of the agent being looked up.</param>
    /// <param name="skillName">The name of the skill assigned to the node.</param>
    /// <param name="providerType">The runtime type of the agent provider implementation.</param>
    /// <param name="providerHash">The hash code of the agent provider instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NodeAgentMapper looking up runtime agent for AgentId={AgentId}, SkillName={SkillName}, providerType={ProviderType}, providerInstance={ProviderHash}")]
    public static partial void LogRuntimeAgentLookup(
        this ILogger logger,
        Guid agentId,
        string skillName,
        string providerType,
        int providerHash);

    /// <summary>
    ///     Logs that no runtime agent was found for a skill that should be assigned to a specific agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The ID of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentId">The ID of the agent the skill should be assigned to.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "No runtime agent found for Skill ID {SkillId}, Name: {SkillName}. Which should be assigned to Agent ID {AgentId}, Name: {AgentName}.")]
    public static partial void LogNoRuntimeAgentFound(
        this ILogger logger,
        Guid skillId,
        string skillName,
        Guid agentId,
        string agentName);
}