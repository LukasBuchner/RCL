using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Services.DataLoaders;

namespace FHOOE.Freydis.GraphQLServer.Types.Resolvers;

/// <summary>
///     Contains resolvers for the fields on the Skill type.
///     The <c>agents</c> field is derived from <see cref="Agent.SkillIds" /> via a DataLoader query,
///     eliminating the need for a denormalized <c>Skill.AgentIds</c> field.
/// </summary>
public class SkillResolvers
{
    /// <summary>
    ///     Resolves the agents field for a Skill by querying all agents whose
    ///     <see cref="Agent.SkillIds" /> contains this skill's ID.
    ///     Uses <see cref="AgentsBySkillIdDataLoader" /> to batch and cache requests,
    ///     preventing N+1 query problems across multiple skill resolutions.
    /// </summary>
    /// <param name="skill">The parent skill being resolved.</param>
    /// <param name="dataLoader">The batching DataLoader that groups agents by skill ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The list of agents capable of executing this skill.</returns>
    public async Task<List<Agent>> GetAgents(
        [Parent] Skill skill,
        AgentsBySkillIdDataLoader dataLoader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var agents = await dataLoader.LoadAsync(skill.Id, cancellationToken);
        return (agents ?? []).ToList();
    }
}