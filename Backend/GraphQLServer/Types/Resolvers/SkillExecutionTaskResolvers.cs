using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.DataLoaders;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Types.Resolvers;

/// <summary>
///     Contains resolvers for the fields on the SkillExecutionTask type.
///     This provides the agent field resolver for normalized data using proper HotChocolate v15 DataLoader pattern.
///     The skill field is stored directly and doesn't need resolution.
/// </summary>
public class SkillExecutionTaskResolvers
{
    /// <summary>
    ///     Resolves the agent field for a SkillExecutionTask by fetching it using the AgentDataLoader.
    ///     This prevents N+1 query problems by batching all agent requests in a single database query.
    /// </summary>
    public async Task<Agent?> GetAgent(
        [Parent] SkillExecutionTask skillExecutionTask,
        AgentDataLoader agentDataLoader,
        ILogger<SkillExecutionTaskResolvers> logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(skillExecutionTask);
        ArgumentNullException.ThrowIfNull(agentDataLoader);

        try
        {
            var agent = await agentDataLoader.LoadAsync(skillExecutionTask.AgentId, cancellationToken);

            if (agent == null)
                logger.LogAgentNotFoundForTask(
                    skillExecutionTask.AgentId, skillExecutionTask.Name ?? "Unknown");

            return agent;
        }
        catch (Exception ex)
        {
            logger.LogAgentLoadFailedForTask(ex,
                skillExecutionTask.AgentId, skillExecutionTask.Name ?? "Unknown");
            throw new InvalidOperationException(
                $"Unable to resolve agent {skillExecutionTask.AgentId} for SkillExecutionTask", ex);
        }
    }
}