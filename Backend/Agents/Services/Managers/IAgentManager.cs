using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Services.Managers;

/// <summary>
///     Generic interface for managing runtime agents in the Freydis robotics orchestration system.
///     Provides a common contract for different types of agent managers (dummy, real, simulated, etc.)
///     to ensure consistent agent lifecycle management across the application.
/// </summary>
/// <remarks>
///     <para>
///         This interface serves as the abstraction layer between the domain model agents
///         (<see cref="Agent" />) and the runtime behavioral agents
///         (<see cref="IRuntimeAgent" />). It enables the system to work with different types
///         of agent implementations without changing the core orchestration logic.
///     </para>
///     <para>
///         Implementations of this interface should handle:
///         <list type="bullet">
///             <item>Agent lifecycle management (creation, startup, shutdown)</item>
///             <item>Agent discovery and lookup by ID or name</item>
///             <item>Health monitoring and status reporting</item>
///             <item>Thread-safe operations for concurrent access</item>
///         </list>
///     </para>
///     <para>
///         Known implementations:
///         <list type="bullet">
///             <item>
///                 <see cref="UnifiedAgentManager" /> - Primary implementation that tracks active agents of all types
///             </item>
///             <item>Future: Additional specialized implementations as needed</item>
///         </list>
///     </para>
/// </remarks>
/// <seealso cref="IRuntimeAgent" />
/// <seealso cref="UnifiedAgentManager" />
/// <seealso cref="AgentHealthStatus" />
public interface IAgentManager
{
    /// <summary>
    ///     Gets all currently active runtime agents managed by this manager.
    /// </summary>
    /// <value>
    ///     A read-only list of active <see cref="IRuntimeAgent" /> instances.
    ///     The list is a snapshot and will not reflect changes made after retrieval.
    /// </value>
    /// <remarks>
    ///     This property should return a thread-safe snapshot of active agents.
    ///     For real-time monitoring, consider using health status methods instead.
    /// </remarks>
    /// <seealso cref="ActiveAgentCount" />
    /// <seealso cref="GetAllAgentHealthAsync" />
    IReadOnlyList<IRuntimeAgent> ActiveAgents { get; }

    /// <summary>
    ///     Gets the total number of currently active agents.
    /// </summary>
    /// <value>
    ///     The count of active agents managed by this instance.
    /// </value>
    /// <remarks>
    ///     This property provides a quick way to check agent capacity without
    ///     retrieving the full <see cref="ActiveAgents" /> collection.
    /// </remarks>
    /// <seealso cref="ActiveAgents" />
    int ActiveAgentCount { get; }

    /// <summary>
    ///     Gets a runtime agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve.</param>
    /// <returns>
    ///     The <see cref="IRuntimeAgent" /> instance if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method should be thread-safe and handle concurrent access appropriately.
    ///         The returned agent reference remains valid until <see cref="StopAgentAsync" /> is called.
    ///     </para>
    ///     <para>
    ///         The agent ID typically corresponds to the domain model's <see cref="Agent.Id" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var agent = agentManager.GetAgent(Guid.Parse("123e4567-e89b-12d3-a456-426614174000"));
    /// if (agent != null)
    /// {
    ///     var skills = await agent.GetAvailableSkillsAsync();
    /// }
    /// </code>
    /// </example>
    IRuntimeAgent? GetAgent(Guid agentId);

    /// <summary>
    ///     Gets a runtime agent by its name.
    /// </summary>
    /// <param name="agentName">The name of the agent to retrieve. Case-sensitive.</param>
    /// <returns>
    ///     The <see cref="IRuntimeAgent" /> instance if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Agent names should be unique within a manager instance. If multiple agents
    ///         have the same name, the behavior is implementation-specific.
    ///     </para>
    ///     <para>
    ///         The agent name typically corresponds to the domain model's <see cref="Agent.Name" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetAgent(Guid)" />
    IRuntimeAgent? GetAgent(string agentName);

    /// <summary>
    ///     Manually registers a runtime agent with the manager.
    /// </summary>
    /// <param name="agent">The agent to register.</param>
    /// <returns>
    ///     <c>true</c> if the agent was successfully registered; <c>false</c> if an agent
    ///     with the same <see cref="IRuntimeAgent.Id" /> already exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when agent is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This method allows manual registration of agents created outside the factory pattern,
    ///         such as agents created directly for testing purposes or agents loaded from external sources.
    ///     </para>
    ///     <para>
    ///         The method is idempotent - registering the same agent ID multiple times will return
    ///         <c>false</c> on subsequent attempts without modifying the existing registration.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var agent = new DummyRuntimeAgent(id, name, logger);
    /// bool wasAdded = agentManager.RegisterAgent(agent);
    /// if (!wasAdded)
    /// {
    ///     logger.LogWarning($"Agent {agent.Id} already registered");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="StopAgentAsync" />
    bool RegisterAgent(IRuntimeAgent agent);

    /// <summary>
    ///     Asynchronously gets health status information for all active agents.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a list of <see cref="AgentHealthStatus" /> instances for all active agents.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method aggregates health information from all active agents, including:
    ///         <list type="bullet">
    ///             <item>Current operational status</item>
    ///             <item>Resource utilization (CPU, memory)</item>
    ///             <item>Active execution counts</item>
    ///             <item>Error states and messages</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For large numbers of agents, this operation may take significant time.
    ///         Consider using pagination or filtering in production implementations.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GetAgentHealthAsync" />
    /// <seealso cref="AgentHealthStatus" />
    Task<List<AgentHealthStatus>> GetAllAgentHealthAsync();

    /// <summary>
    ///     Asynchronously gets health status information for a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the <see cref="AgentHealthStatus" /> if the agent is found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     Health status includes real-time metrics such as CPU usage, memory consumption,
    ///     active executions, and error states. This method calls through to the agent's
    ///     <see cref="IRuntimeAgent.GetHealthStatusAsync" /> method.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var health = await agentManager.GetAgentHealthAsync(agentId);
    /// if (health?.IsHealthy == false)
    /// {
    ///     logger.LogWarning($"Agent {health.AgentName} is unhealthy: {health.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetAllAgentHealthAsync" />
    Task<AgentHealthStatus?> GetAgentHealthAsync(Guid agentId);

    /// <summary>
    ///     Asynchronously starts a new runtime agent with the specified identity.
    /// </summary>
    /// <param name="agentId">The unique identifier for the new agent.</param>
    /// <param name="agentName">The display name for the new agent.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the newly created and started <see cref="IRuntimeAgent" /> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an agent with the same ID already exists.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method creates and initializes a new agent instance. The specific type
    ///         of agent created depends on the implementation (e.g., dummy, real, simulated).
    ///     </para>
    ///     <para>
    ///         The created agent will have no initial skills. Skills must be configured
    ///         separately based on the agent's capabilities and the domain model.
    ///     </para>
    /// </remarks>
    /// <seealso cref="StopAgentAsync" />
    Task<IRuntimeAgent> StartAgentAsync(Guid agentId, string agentName);

    /// <summary>
    ///     Asynchronously stops and removes a runtime agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to stop.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     <c>true</c> if the agent was successfully stopped; <c>false</c> if not found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method should gracefully shutdown the agent, including:
    ///         <list type="bullet">
    ///             <item>Cancelling any active skill executions</item>
    ///             <item>Releasing allocated resources</item>
    ///             <item>Updating health status to reflect shutdown</item>
    ///             <item>Removing the agent from the active agents collection</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         After this method returns, any references to the agent should be considered invalid.
    ///     </para>
    /// </remarks>
    /// <seealso cref="StartAgentAsync" />
    Task<bool> StopAgentAsync(Guid agentId);
}