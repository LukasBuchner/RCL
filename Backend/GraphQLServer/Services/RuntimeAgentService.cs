using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Types;

namespace FHOOE.Freydis.GraphQLServer.Services;

/// <summary>
///     Service that bridges domain <see cref="Agent" /> entities with runtime <see cref="IRuntimeAgent" /> instances
///     to provide a unified view of agent information for GraphQL queries.
/// </summary>
/// <remarks>
///     <para>
///         This service is a critical component in the Freydis architecture, solving the impedance mismatch between:
///         <list type="bullet">
///             <item><b>Domain Layer</b>: <see cref="Agent" /> entities representing persistent agent configuration</item>
///             <item><b>Runtime Layer</b>: <see cref="IRuntimeAgent" /> interfaces providing behavioral capabilities</item>
///             <item><b>GraphQL Layer</b>: Need for clean, queryable data structures without complex async operations</item>
///         </list>
///     </para>
///     <para>
///         The service aggregates data from multiple sources:
///         <list type="number">
///             <item><see cref="IRepository{Agent}" /> - For domain agent persistence</item>
///             <item><see cref="IAgentManager" /> - For runtime agent lifecycle and monitoring</item>
///             <item><see cref="IRuntimeAgent" /> instances - For real-time status and capabilities</item>
///         </list>
///     </para>
///     <para>
///         Thread Safety: This service is registered as Scoped in DI and should be thread-safe for
///         concurrent GraphQL query execution within a single request scope.
///     </para>
/// </remarks>
/// <example>
///     Typical usage in GraphQL resolvers:
///     <code>
/// public async Task&lt;List&lt;RuntimeAgentInfo&gt;&gt; GetRuntimeAgentsAsync(
///     [Service] RuntimeAgentService runtimeAgentService)
/// {
///     return await runtimeAgentService.GetAllRuntimeAgentsAsync();
/// }
/// </code>
/// </example>
/// <seealso cref="RuntimeAgentInfo" />
/// <seealso cref="IAgentManager" />
/// <seealso cref="Agent" />
/// <seealso cref="IRuntimeAgent" />
public class RuntimeAgentService(
    IRepository<Agent> agentRepository,
    IRepository<Skill> skillRepository,
    IAgentManager agentManager)
{
    /// <summary>
    ///     Asynchronously retrieves comprehensive information for all agents in the system,
    ///     combining both domain entities and runtime instances.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a list of <see cref="RuntimeAgentInfo" /> instances representing all known agents.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs a full reconciliation between domain and runtime agents:
    ///         <list type="number">
    ///             <item>Fetches all domain agents from the repository</item>
    ///             <item>Retrieves all active runtime agents from the agent manager</item>
    ///             <item>For each runtime agent, fetches health status and available skills</item>
    ///             <item>Matches runtime agents with their domain counterparts by ID</item>
    ///             <item>Includes domain-only agents (inactive) in the results</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Performance considerations:
    ///         <list type="bullet">
    ///             <item>Makes N+1 async calls for N runtime agents (health + skills)</item>
    ///             <item>Consider implementing pagination for large agent populations</item>
    ///             <item>Results are not cached; each call fetches fresh data</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     GraphQL query to retrieve all agents:
    ///     <code>
    /// query {
    ///   getRuntimeAgents {
    ///     id
    ///     name
    ///     isActive
    ///     domainAgent { representativeColor }
    ///     healthStatus { isHealthy }
    ///   }
    /// }
    /// </code>
    /// </example>
    public async Task<List<RuntimeAgentInfo>> GetAllRuntimeAgentsAsync()
    {
        var domainAgents = await agentRepository.GetAllAsync();
        var runtimeAgents = agentManager.ActiveAgents;
        var result = new List<RuntimeAgentInfo>();

        // Add runtime agents (these may not have domain counterparts)
        foreach (var runtimeAgent in runtimeAgents)
        {
            var domainAgent = domainAgents.FirstOrDefault(da => da.Id == runtimeAgent.Id);
            var healthStatus = await runtimeAgent.GetHealthStatusAsync();
            var skills = await runtimeAgent.GetAvailableSkillsAsync();

            result.Add(new RuntimeAgentInfo
            {
                Id = runtimeAgent.Id,
                Name = runtimeAgent.Name,
                DomainAgent = domainAgent,
                HealthStatus = healthStatus,
                AvailableSkills = skills.ToList(),
                IsActive = true,
                AgentType = runtimeAgent.GetType().Name,
                StartedAt = healthStatus.StartedUtc,
                LastSeen = healthStatus.LastSeenUtc
            });
        }

        // Add domain agents that don't have runtime counterparts
        foreach (var domainAgent in domainAgents.Where(domainAgent => result.All(ra => ra.Id != domainAgent.Id)))
            result.Add(new RuntimeAgentInfo
            {
                Id = domainAgent.Id,
                Name = domainAgent.Name,
                DomainAgent = domainAgent,
                HealthStatus = null,
                AvailableSkills = await GetSkillsByIdsAsync(domainAgent.SkillIds),
                IsActive = false,
                AgentType = "Domain",
                StartedAt = null,
                LastSeen = null
            });

        return result;
    }

    /// <summary>
    ///     Asynchronously retrieves comprehensive information for a specific agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a <see cref="RuntimeAgentInfo" /> instance if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method follows a runtime-first lookup strategy:
    ///         <list type="number">
    ///             <item>First checks for an active runtime agent via <see cref="IAgentManager.GetAgent(Guid)" /></item>
    ///             <item>If found, fetches real-time health and skill data</item>
    ///             <item>If not found in runtime, checks domain repository for inactive agents</item>
    ///             <item>Returns null only if agent doesn't exist in either layer</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The method ensures consistent agent information by always checking both sources
    ///         and preferring runtime data when available.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var agentInfo = await runtimeAgentService.GetRuntimeAgentByIdAsync(agentId);
    /// if (agentInfo?.IsActive == true)
    /// {
    ///     Console.WriteLine($"Agent {agentInfo.Name} has {agentInfo.HealthStatus?.ActiveExecutions} active tasks");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetRuntimeAgentByNameAsync" />
    /// <seealso cref="IAgentManager.GetAgent(Guid)" />
    public async Task<RuntimeAgentInfo?> GetRuntimeAgentByIdAsync(Guid agentId)
    {
        var runtimeAgent = agentManager.GetAgent(agentId);
        var domainAgent = await agentRepository.GetByIdAsync(agentId);

        if (runtimeAgent != null)
        {
            var healthStatus = await runtimeAgent.GetHealthStatusAsync();
            var skills = await runtimeAgent.GetAvailableSkillsAsync();

            return new RuntimeAgentInfo
            {
                Id = runtimeAgent.Id,
                Name = runtimeAgent.Name,
                DomainAgent = domainAgent,
                HealthStatus = healthStatus,
                AvailableSkills = skills.ToList(),
                IsActive = true,
                AgentType = runtimeAgent.GetType().Name,
                StartedAt = healthStatus.StartedUtc,
                LastSeen = healthStatus.LastSeenUtc
            };
        }

        if (domainAgent != null)
            return new RuntimeAgentInfo
            {
                Id = domainAgent.Id,
                Name = domainAgent.Name,
                DomainAgent = domainAgent,
                HealthStatus = null,
                AvailableSkills = await GetSkillsByIdsAsync(domainAgent.SkillIds),
                IsActive = false,
                AgentType = "Domain",
                StartedAt = null,
                LastSeen = null
            };

        return null;
    }

    /// <summary>
    ///     Asynchronously retrieves comprehensive information for a specific agent by its name.
    /// </summary>
    /// <param name="agentName">The name of the agent to retrieve. Case-sensitive.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a <see cref="RuntimeAgentInfo" /> instance if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method provides name-based agent lookup as an alternative to ID-based retrieval:
    ///         <list type="number">
    ///             <item>First attempts to find an active runtime agent by name</item>
    ///             <item>If found, delegates to <see cref="GetRuntimeAgentByIdAsync" /> for full data retrieval</item>
    ///             <item>If not found in runtime, searches all domain agents for a name match</item>
    ///             <item>Name matching is case-sensitive and expects exact matches</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> Agent names should be unique within the system. If multiple agents
    ///         share the same name, this method returns the first match found (runtime agents take precedence).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     GraphQL query using name-based lookup:
    ///     <code>
    /// query {
    ///   getRuntimeAgentByName(agentName: "Robot-01") {
    ///     id
    ///     isActive
    ///     healthStatus {
    ///       isHealthy
    ///       statusMessage
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetRuntimeAgentByIdAsync" />
    /// <seealso cref="IAgentManager.GetAgent(string)" />
    public async Task<RuntimeAgentInfo?> GetRuntimeAgentByNameAsync(string agentName)
    {
        var runtimeAgent = agentManager.GetAgent(agentName);
        if (runtimeAgent != null) return await GetRuntimeAgentByIdAsync(runtimeAgent.Id);

        var domainAgents = await agentRepository.GetAllAsync();
        var domainAgent = domainAgents.FirstOrDefault(a => a.Name == agentName);
        if (domainAgent != null) return await GetRuntimeAgentByIdAsync(domainAgent.Id);

        return null;
    }

    /// <summary>
    ///     Helper method to resolve skills by their IDs.
    /// </summary>
    /// <param name="skillIds">Collection of skill IDs to resolve.</param>
    /// <returns>List of resolved skills.</returns>
    private async Task<List<Skill>> GetSkillsByIdsAsync(IEnumerable<Guid> skillIds)
    {
        var skills = new List<Skill>();
        foreach (var skillId in skillIds)
        {
            var skill = await skillRepository.GetByIdAsync(skillId);
            if (skill != null) skills.Add(skill);
        }

        return skills;
    }
}