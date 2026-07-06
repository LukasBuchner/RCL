using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Services.DataLoaders;

/// <summary>
///     DataLoader for resolving the reverse direction of the Agent→Skill relationship.
///     Given a skill ID, returns all agents whose <see cref="Agent.SkillIds" /> contains that skill.
///     This replaces the denormalized <c>Skill.AgentIds</c> field with a query-based approach,
///     eliminating bidirectional consistency issues while preserving N+1 query prevention.
/// </summary>
public class AgentsBySkillIdDataLoader : GroupedDataLoader<Guid, Agent>
{
    private readonly IServiceProvider _services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentsBySkillIdDataLoader" /> class.
    /// </summary>
    /// <param name="services">The service provider for creating scoped services.</param>
    /// <param name="batchScheduler">The batch scheduler for coordinating DataLoader operations.</param>
    /// <param name="options">DataLoader options injected by DI for shared pooled cache objects.</param>
    public AgentsBySkillIdDataLoader(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    ///     Loads all agents grouped by the skill IDs they can perform.
    ///     Fetches all agents once and groups them by their <see cref="Agent.SkillIds" />,
    ///     so that multiple concurrent <c>Skill.agents</c> resolver calls share a single database query.
    /// </summary>
    /// <param name="keys">The skill IDs to find capable agents for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A lookup mapping each skill ID to the agents that can perform it.</returns>
    protected override async Task<ILookup<Guid, Agent>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
            return Array.Empty<Agent>().ToLookup(_ => Guid.Empty);

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var agentService = scope.ServiceProvider.GetRequiredService<IAgentApplicationService>();
            var logger = scope.ServiceProvider.GetService<ILogger<AgentsBySkillIdDataLoader>>();

            var allAgents = await agentService.GetAllAgentsAsync();
            var keySet = keys.ToHashSet();

            // Flatten: for each agent, emit one (skillId, agent) pair per matching skill
            var pairs = allAgents
                .SelectMany(agent => agent.SkillIds
                    .Where(skillId => keySet.Contains(skillId))
                    .Select(skillId => new { SkillId = skillId, Agent = agent }));

            if (logger is not null) logger.LogAgentsBySkillIdResolved(keys.Count, allAgents.Count);

            return pairs.ToLookup(p => p.SkillId, p => p.Agent);
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<AgentsBySkillIdDataLoader>>();
            if (logger is not null) logger.LogAgentsBySkillIdCriticalError(ex, keys.Count);

            return Array.Empty<Agent>().ToLookup(_ => Guid.Empty);
        }
    }
}