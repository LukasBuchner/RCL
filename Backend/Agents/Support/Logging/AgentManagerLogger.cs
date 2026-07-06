using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Managers;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for agent manager operations.
///     Covers <see cref="UnifiedAgentManager" /> (factory registration, agent loading, and
///     agent lifecycle) through a single shared static logger class.
/// </summary>
public static partial class AgentManagerLogger
{
    // ──────────────────────────────────────────────────
    //  Unified Agent Manager — Factory registration
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when a factory for the specified agent type is already registered.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type whose factory registration was attempted.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Factory for agent type {AgentType} is already registered")]
    public static partial void LogFactoryAlreadyRegistered(
        this ILogger logger,
        AgentType agentType);

    /// <summary>
    ///     Logs that a factory for the specified agent type was successfully registered.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type whose factory was registered.</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Registered factory for agent type {AgentType}")]
    public static partial void LogFactoryRegistered(
        this ILogger logger,
        AgentType agentType);

    // ──────────────────────────────────────────────────
    //  Unified Agent Manager — Factory loading
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when no factory is registered for the requested agent type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type that has no registered factory.</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "No factory registered for agent type {AgentType}")]
    public static partial void LogNoFactoryRegistered(
        this ILogger logger,
        AgentType agentType);

    /// <summary>
    ///     Logs that agent loading from a factory has started for the specified agent type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type being loaded from its factory.</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Loading agents from factory for type {AgentType}")]
    public static partial void LogLoadingAgentsFromFactory(
        this ILogger logger,
        AgentType agentType);

    /// <summary>
    ///     Logs that an agent was successfully added to the active agents collection during factory loading.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the added agent.</param>
    /// <param name="agentId">The unique identifier of the added agent.</param>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Added agent {AgentName} (ID: {AgentId}) to active agents")]
    public static partial void LogAgentAddedToActive(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs a warning when an agent with a duplicate ID is skipped during factory loading.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The duplicate agent identifier that was skipped.</param>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} already exists, skipping")]
    public static partial void LogAgentAlreadyExists(
        this ILogger logger,
        Guid agentId);

    /// <summary>
    ///     Logs that agents were successfully loaded from a factory, including the total count.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of agents that were loaded.</param>
    /// <param name="agentType">The agent type that was loaded.</param>
    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Information,
        Message = "Loaded {Count} agents from factory for type {AgentType}")]
    public static partial void LogAgentsLoadedFromFactory(
        this ILogger logger,
        int count,
        AgentType agentType);

    /// <summary>
    ///     Logs an error when agent loading from a factory fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <param name="agentType">The agent type whose factory loading failed.</param>
    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Error,
        Message = "Failed to load agents from factory for type {AgentType}")]
    public static partial void LogAgentLoadFailed(
        this ILogger logger,
        Exception ex,
        AgentType agentType);

    // ──────────────────────────────────────────────────
    //  Unified Agent Manager — Agent lookup
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs diagnostic details of an agent lookup by ID, including the current manager state
    ///     (agent count, manager hash, and all known agent IDs) for troubleshooting.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The agent identifier being looked up.</param>
    /// <param name="found">Whether the agent was found in the manager.</param>
    /// <param name="agentCount">The total number of agents currently registered.</param>
    /// <param name="managerHash">The hash code of the manager instance for identity tracking.</param>
    /// <param name="agentIds">A comma-separated string of all registered agent IDs.</param>
    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Debug,
        Message =
            "GetAgent lookup for {AgentId}: found={Found}, agentCount={AgentCount}, managerInstance={ManagerHash}, agentIds=[{AgentIds}]")]
    public static partial void LogAgentLookup(
        this ILogger logger,
        Guid agentId,
        bool found,
        int agentCount,
        int managerHash,
        string agentIds);

    /// <summary>
    ///     Logs a warning when an agent health check is requested but the agent is not found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent that was not found.</param>
    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} not found")]
    public static partial void LogAgentNotFound(
        this ILogger logger,
        Guid agentId);

    // ──────────────────────────────────────────────────
    //  Unified Agent Manager — Agent lifecycle
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that an agent was successfully stopped and removed from the manager.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the stopped agent.</param>
    /// <param name="agentId">The unique identifier of the stopped agent.</param>
    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Information,
        Message = "Stopped and removed agent {AgentName} (ID: {AgentId})")]
    public static partial void LogAgentStopped(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs a warning when an agent stop is requested but the agent is not found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent that was not found for stopping.</param>
    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} not found for stopping")]
    public static partial void LogAgentNotFoundForStopping(
        this ILogger logger,
        Guid agentId);

    /// <summary>
    ///     Logs that an agent was successfully registered with the unified agent manager,
    ///     including the manager instance hash and current agent count for diagnostics.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the registered agent.</param>
    /// <param name="agentId">The unique identifier of the registered agent.</param>
    /// <param name="managerHash">The hash code of the manager instance for identity tracking.</param>
    /// <param name="agentCount">The total number of agents after registration.</param>
    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Information,
        Message =
            "Registered agent {AgentName} (ID: {AgentId}) with manager (instance={ManagerHash}, agentCount={AgentCount})")]
    public static partial void LogAgentRegistered(
        this ILogger logger,
        string agentName,
        Guid agentId,
        int managerHash,
        int agentCount);

    /// <summary>
    ///     Logs a warning when an agent registration is skipped because the agent ID already exists,
    ///     including the manager instance hash for diagnostics.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The duplicate agent identifier that was skipped.</param>
    /// <param name="managerHash">The hash code of the manager instance for identity tracking.</param>
    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Warning,
        Message = "Agent with ID {AgentId} already registered, skipping (instance={ManagerHash})")]
    public static partial void LogAgentAlreadyRegistered(
        this ILogger logger,
        Guid agentId,
        int managerHash);
}