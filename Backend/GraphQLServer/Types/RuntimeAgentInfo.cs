using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Services;

namespace FHOOE.Freydis.GraphQLServer.Types;

/// <summary>
///     GraphQL-compatible data transfer object that represents comprehensive runtime agent information.
///     Bridges the gap between the domain model (<see cref="Agent" />) and runtime behavior (<see cref="IRuntimeAgent" />)
///     to provide a unified view of agent state for GraphQL queries.
/// </summary>
/// <remarks>
///     <para>
///         This type solves the architectural challenge of exposing runtime agent information through GraphQL
///         while maintaining clean separation between domain entities and behavioral interfaces. It aggregates:
///         <list type="bullet">
///             <item>Static domain data from <see cref="Agent" /> entities (persistent configuration)</item>
///             <item>Dynamic runtime status from <see cref="IRuntimeAgent" /> instances (live state)</item>
///             <item>Health metrics from <see cref="AgentHealthStatus" /> (operational monitoring)</item>
///         </list>
///     </para>
///     <para>
///         The type is designed to be GraphQL-friendly by:
///         <list type="bullet">
///             <item>Using only simple property types (no methods or complex async operations)</item>
///             <item>Providing nullable fields for partial data scenarios</item>
///             <item>Supporting both active runtime agents and inactive domain-only agents</item>
///         </list>
///     </para>
///     <para>
///         Usage scenarios:
///         <list type="number">
///             <item><b>Active Runtime Agent</b>: All fields populated with live data from running agent</item>
///             <item><b>Inactive Domain Agent</b>: Only DomainAgent populated, runtime fields null, IsActive=false</item>
///             <item><b>Runtime-Only Agent</b>: No DomainAgent link, but full runtime data available</item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     GraphQL query example:
///     <code>
/// query {
///   getRuntimeAgents {
///     id
///     name
///     isActive
///     agentType
///     domainAgent {
///       representativeColor
///     }
///     healthStatus {
///       isHealthy
///       cpuUsagePercent
///       activeExecutions
///     }
///     availableSkills {
///       name
///       estimatedDuration
///     }
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="RuntimeAgentService" />
/// <seealso cref="Agent" />
/// <seealso cref="IRuntimeAgent" />
/// <seealso cref="AgentHealthStatus" />
public record RuntimeAgentInfo
{
    /// <summary>
    ///     Gets the unique identifier of the agent.
    /// </summary>
    /// <value>
    ///     A GUID that uniquely identifies this agent across both domain and runtime contexts.
    ///     This ID should match between <see cref="DomainAgent.Id" /> and <see cref="IRuntimeAgent.Id" />.
    /// </value>
    /// <remarks>
    ///     This identifier is used for agent lookup operations and should remain stable
    ///     throughout the agent's lifecycle, even across system restarts.
    /// </remarks>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the display name of the agent.
    /// </summary>
    /// <value>
    ///     A human-readable name for the agent. Should be unique within the system for easier identification.
    /// </value>
    /// <remarks>
    ///     The name is used for both display purposes and as an alternative lookup key.
    ///     See <see cref="IAgentManager.GetAgent(string)" /> for name-based agent retrieval.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the associated domain agent entity, if available.
    /// </summary>
    /// <value>
    ///     The <see cref="Agent" /> domain entity that defines this agent's persistent configuration,
    ///     or <c>null</c> if this is a runtime-only agent without domain persistence.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When present, provides access to domain-specific properties such as:
    ///         <list type="bullet">
    ///             <item><see cref="Agent.RepresentativeColor" /> - Visual identification color</item>
    ///             <item><see cref="Agent.Skills" /> - Configured skill capabilities</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         A null value indicates this agent exists only at runtime (e.g., dynamically created agents).
    ///     </para>
    /// </remarks>
    public Agent? DomainAgent { get; init; }

    /// <summary>
    ///     Gets the current health status of the runtime agent.
    /// </summary>
    /// <value>
    ///     An <see cref="AgentHealthStatus" /> instance with real-time health metrics,
    ///     or <c>null</c> if the agent is not currently active.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Health status is only available for active runtime agents and includes:
    ///         <list type="bullet">
    ///             <item>Operational state (<see cref="AgentHealthStatus.IsHealthy" />)</item>
    ///             <item>
    ///                 Resource usage (<see cref="AgentHealthStatus.CpuUsagePercent" />,
    ///                 <see cref="AgentHealthStatus.MemoryUsageMb" />)
    ///             </item>
    ///             <item>
    ///                 Execution metrics (<see cref="AgentHealthStatus.ActiveExecutions" />,
    ///                 <see cref="AgentHealthStatus.TotalExecutionsCompleted" />)
    ///             </item>
    ///             <item>Error information (<see cref="AgentHealthStatus.ErrorMessage" />)</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This data is fetched from <see cref="IRuntimeAgent.GetHealthStatusAsync" /> for active agents.
    ///     </para>
    /// </remarks>
    public AgentHealthStatus? HealthStatus { get; init; }

    /// <summary>
    ///     Gets the skills currently available to this runtime agent.
    /// </summary>
    /// <value>
    ///     A read-only list of <see cref="Skill" /> instances the agent can execute,
    ///     or <c>null</c> if skill information is not available.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         For active runtime agents, this reflects the actual executable skills from
    ///         <see cref="IRuntimeAgent.GetAvailableSkillsAsync" />. For inactive agents,
    ///         this may show configured skills from the domain model.
    ///     </para>
    ///     <para>
    ///         Skills define what operations the agent can perform in the orchestration system.
    ///         See <see cref="Skill" /> for skill properties and capabilities.
    ///     </para>
    /// </remarks>
    public IReadOnlyList<Skill>? AvailableSkills { get; init; }

    /// <summary>
    ///     Gets whether this agent is currently active in the runtime system.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the agent has an active <see cref="IRuntimeAgent" /> instance;
    ///     <c>false</c> if only domain data exists without runtime presence.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Active agents can:
    ///         <list type="bullet">
    ///             <item>Execute skills via <see cref="IRuntimeAgent.ExecuteSkillAsync" /></item>
    ///             <item>Report real-time health status</item>
    ///             <item>Participate in orchestration procedures</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Inactive agents only exist in the domain model and must be started via
    ///         <see cref="IAgentManager.StartAgentAsync" /> before use.
    ///     </para>
    /// </remarks>
    public required bool IsActive { get; init; }

    /// <summary>
    ///     Gets the implementation type name of the runtime agent.
    /// </summary>
    /// <value>
    ///     A string identifying the concrete type of the <see cref="IRuntimeAgent" /> implementation,
    ///     such as "DummyAgent", "RealAgent", or "Domain" for inactive agents.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This property helps distinguish between different agent implementations:
    ///         <list type="bullet">
    ///             <item>"DummyAgent" - Test/simulation agents from <see cref="Agents.Agents.DummyAgent" /></item>
    ///             <item>"Domain" - Inactive agents that exist only in the domain model</item>
    ///             <item>Future: "RealAgent", "RemoteAgent", etc. for production implementations</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The value is typically derived from <c>runtimeAgent.GetType().Name</c>.
    ///     </para>
    /// </remarks>
    public required string AgentType { get; init; }

    /// <summary>
    ///     Gets when this runtime agent instance was started.
    /// </summary>
    /// <value>
    ///     The UTC timestamp when the agent was initialized, or <c>null</c> if not active.
    /// </value>
    /// <remarks>
    ///     This timestamp is typically sourced from <see cref="AgentHealthStatus.StartedUtc" />
    ///     and can be used to calculate agent uptime and stability metrics.
    /// </remarks>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    ///     Gets the last time this agent reported activity.
    /// </summary>
    /// <value>
    ///     The UTC timestamp of the most recent health check or activity,
    ///     or <c>null</c> if the agent is not active.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Sourced from <see cref="AgentHealthStatus.LastSeenUtc" />, this timestamp
    ///         helps monitor agent responsiveness and detect potential failures.
    ///     </para>
    ///     <para>
    ///         A significant gap between LastSeen and current time may indicate
    ///         communication issues or agent failure.
    ///     </para>
    /// </remarks>
    public DateTime? LastSeen { get; init; }
}