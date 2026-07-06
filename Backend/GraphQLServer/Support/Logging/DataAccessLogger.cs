namespace FHOOE.Freydis.GraphQLServer.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for GraphQL data access
///     operations. Covers DataLoaders (<c>AgentDataLoader</c>, <c>SkillDataLoader</c>,
///     <c>AgentsBySkillIdDataLoader</c>) and field resolvers (<c>AgentResolvers</c>,
///     <c>PropertyResolvers</c>, <c>SkillExecutionTaskResolvers</c>). All methods are extension
///     methods on <see cref="ILogger" /> and use the <see cref="LoggerMessageAttribute" /> source
///     generator to eliminate boxing allocations and guard-check overhead (CA1848).
/// </summary>
public static partial class DataAccessLogger
{
    // ──────────────────────────────────────────────────
    //  AgentDataLoader
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning that an agent was not found during batch loading.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} not found")]
    public static partial void LogAgentNotFound(
        this ILogger logger,
        Guid agentId);

    /// <summary>
    ///     Logs an error when loading an individual agent fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="agentId">The identifier of the agent that failed to load.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load agent with ID {AgentId}")]
    public static partial void LogAgentLoadFailed(
        this ILogger logger,
        Exception exception,
        Guid agentId);

    /// <summary>
    ///     Logs a debug summary of a successful agent batch load, including how many agents
    ///     were loaded out of the total requested.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="loadedCount">The number of agents successfully loaded.</param>
    /// <param name="requestedCount">The total number of agents requested.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully loaded {LoadedCount} out of {RequestedCount} agents")]
    public static partial void LogAgentBatchLoadCompleted(
        this ILogger logger,
        int loadedCount,
        int requestedCount);

    /// <summary>
    ///     Logs a critical error when the entire agent batch loading operation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <param name="keyCount">The number of agent keys that were being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Critical error in AgentDataLoader batch loading for {KeyCount} keys")]
    public static partial void LogAgentBatchLoadCriticalError(
        this ILogger logger,
        Exception exception,
        int keyCount);

    // ──────────────────────────────────────────────────
    //  SkillDataLoader
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning that a skill was not found during batch loading.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillId">The identifier of the skill that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skill with ID {SkillId} not found")]
    public static partial void LogSkillNotFound(
        this ILogger logger,
        Guid skillId);

    /// <summary>
    ///     Logs an error when loading an individual skill fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="skillId">The identifier of the skill that failed to load.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load skill with ID {SkillId}")]
    public static partial void LogSkillLoadFailed(
        this ILogger logger,
        Exception exception,
        Guid skillId);

    /// <summary>
    ///     Logs a debug summary of a successful skill batch load, including how many skills
    ///     were loaded out of the total requested.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="loadedCount">The number of skills successfully loaded.</param>
    /// <param name="requestedCount">The total number of skills requested.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully loaded {LoadedCount} out of {RequestedCount} skills")]
    public static partial void LogSkillBatchLoadCompleted(
        this ILogger logger,
        int loadedCount,
        int requestedCount);

    /// <summary>
    ///     Logs a critical error when the entire skill batch loading operation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <param name="keyCount">The number of skill keys that were being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Critical error in SkillDataLoader batch loading for {KeyCount} keys")]
    public static partial void LogSkillBatchLoadCriticalError(
        this ILogger logger,
        Exception exception,
        int keyCount);

    // ──────────────────────────────────────────────────
    //  AgentsBySkillIdDataLoader
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a debug summary of the agents-by-skill-ID resolution, including how many
    ///     skill keys were resolved and the total number of agents considered.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="keyCount">The number of skill IDs that were requested.</param>
    /// <param name="agentCount">The total number of agents evaluated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "AgentsBySkillIdDataLoader: resolved agents for {KeyCount} skills from {AgentCount} total agents")]
    public static partial void LogAgentsBySkillIdResolved(
        this ILogger logger,
        int keyCount,
        int agentCount);

    /// <summary>
    ///     Logs a critical error when the agents-by-skill-ID batch loading operation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <param name="keyCount">The number of skill keys that were being resolved.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Critical error in AgentsBySkillIdDataLoader for {KeyCount} skill keys")]
    public static partial void LogAgentsBySkillIdCriticalError(
        this ILogger logger,
        Exception exception,
        int keyCount);

    // ──────────────────────────────────────────────────
    //  AgentResolvers
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when a skill referenced by an agent could not be found during resolution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillId">The identifier of the skill that was not found.</param>
    /// <param name="agentId">The identifier of the agent that referenced the missing skill.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skill with ID {SkillId} referenced by agent {AgentId} could not be found")]
    public static partial void LogSkillNotFoundForAgent(
        this ILogger logger,
        Guid skillId,
        Guid agentId);

    /// <summary>
    ///     Logs an error when loading a specific skill for an agent fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="skillId">The identifier of the skill that failed to load.</param>
    /// <param name="agentId">The identifier of the agent for which the skill was being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load skill {SkillId} for agent {AgentId}")]
    public static partial void LogSkillLoadFailedForAgent(
        this ILogger logger,
        Exception exception,
        Guid skillId,
        Guid agentId);

    /// <summary>
    ///     Logs an error when resolving the entire skills collection for an agent fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the resolution failure.</param>
    /// <param name="agentId">The identifier of the agent whose skills could not be resolved.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to resolve skills for agent {AgentId}")]
    public static partial void LogSkillsResolutionFailed(
        this ILogger logger,
        Exception exception,
        Guid agentId);

    // ──────────────────────────────────────────────────
    //  PropertyResolvers
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when mapping a TypedValue to a PropertyValue fails for a specific property.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the mapping failure.</param>
    /// <param name="propertyName">The name of the typed property that failed to map.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to map TypedValue to PropertyValue for typedProperty {PropertyName}")]
    public static partial void LogPropertyValueMappingFailed(
        this ILogger logger,
        Exception exception,
        string propertyName);

    // ──────────────────────────────────────────────────
    //  SkillExecutionTaskResolvers
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when the agent referenced by a SkillExecutionTask could not be found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent that was not found.</param>
    /// <param name="taskName">The name of the SkillExecutionTask that referenced the missing agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} referenced by SkillExecutionTask {TaskName} could not be found")]
    public static partial void LogAgentNotFoundForTask(
        this ILogger logger,
        Guid agentId,
        string taskName);

    /// <summary>
    ///     Logs an error when loading the agent for a SkillExecutionTask fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="agentId">The identifier of the agent that failed to load.</param>
    /// <param name="taskName">The name of the SkillExecutionTask for which the agent was being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load agent {AgentId} for SkillExecutionTask {TaskName}")]
    public static partial void LogAgentLoadFailedForTask(
        this ILogger logger,
        Exception exception,
        Guid agentId,
        string taskName);
}