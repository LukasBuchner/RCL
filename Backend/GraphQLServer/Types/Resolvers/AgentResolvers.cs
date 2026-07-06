using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Services.DataLoaders;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Types.Resolvers;

/// <summary>
///     Contains resolvers for the fields on the Agent type.
///     This provides the skills field resolver for normalized data using proper HotChocolate v15 DataLoader pattern.
/// </summary>
public class AgentResolvers
{
    /// <summary>
    ///     Resolves the skills field for an Agent by fetching them using the SkillDataLoader.
    ///     This prevents N+1 query problems by batching all skill requests in a single database query.
    /// </summary>
    public async Task<List<Skill?>> GetSkills(
        [Parent] Agent agent,
        SkillDataLoader skillDataLoader,
        ILogger<AgentResolvers> logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(skillDataLoader);

        if (agent.SkillIds == null || agent.SkillIds.Count == 0) return [];

        try
        {
            var skills = new List<Skill?>();
            foreach (var skillId in agent.SkillIds)
                try
                {
                    var skill = await skillDataLoader.LoadAsync(skillId, cancellationToken);
                    skills.Add(skill);

                    if (skill == null)
                        logger.LogSkillNotFoundForAgent(skillId, agent.Id);
                }
                catch (Exception ex)
                {
                    logger.LogSkillLoadFailedForAgent(ex, skillId, agent.Id);
                    // Add null to maintain consistency with expected count
                    skills.Add(null);
                }

            return skills;
        }
        catch (Exception ex)
        {
            logger.LogSkillsResolutionFailed(ex, agent.Id);
            throw new InvalidOperationException($"Unable to resolve skills for agent {agent.Id}", ex);
        }
    }
}