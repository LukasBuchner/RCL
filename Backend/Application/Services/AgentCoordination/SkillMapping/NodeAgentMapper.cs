using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Default implementation of <see cref="INodeAgentMapper" /> that resolves runtime agents
///     via <see cref="IRuntimeAgentProvider" />, ensuring dynamically connected agents
///     (e.g., Digital Twins joining via WebSocket after startup) are always visible.
/// </summary>
public class NodeAgentMapper : INodeAgentMapper
{
    private readonly IAgentApplicationService _agentApplicationService;
    private readonly IRuntimeAgentProvider _agentProvider;
    private readonly ILogger<NodeAgentMapper> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeAgentMapper" />.
    /// </summary>
    /// <param name="agentProvider">
    ///     Provider for looking up runtime agents from the live agent manager.
    ///     Queries are always against the current set of registered agents, not a startup-time snapshot.
    /// </param>
    /// <param name="logger">The logger to record warnings when no agent is found.</param>
    /// <param name="agentApplicationService">The agent application service for fetching domain agent data.</param>
    public NodeAgentMapper(IRuntimeAgentProvider agentProvider, ILogger<NodeAgentMapper> logger,
        IAgentApplicationService agentApplicationService)
    {
        _agentProvider = agentProvider;
        _logger = logger;
        _agentApplicationService = agentApplicationService;
    }

    /// <inheritdoc />
    public async Task<(Skill DomainSkill, Agent DomainAgent, IRuntimeAgent Agent)?> MapAsync(Node node)
    {
        if (node is not SkillExecutionNode sen)
            return null;

        // Get skill directly from the task (it's already stored)
        var skill = sen.SkillExecutionTask.Skill;

        // Fetch agent from the application service
        var agent = await _agentApplicationService.GetAgentByIdAsync(sen.SkillExecutionTask.AgentId);
        if (agent == null)
        {
            _logger.LogAgentNotFound(sen.SkillExecutionTask.AgentId);
            return null;
        }

        var providerTypeName = _agentProvider.GetType().Name;
        var providerHashCode = _agentProvider.GetHashCode();
        _logger.LogRuntimeAgentLookup(
            sen.SkillExecutionTask.AgentId, skill.Name, providerTypeName, providerHashCode);

        var assigned = _agentProvider.GetRuntimeAgent(sen.SkillExecutionTask.AgentId);
        if (assigned != null)
            return (skill, agent, assigned);

        _logger.LogNoRuntimeAgentFound(skill.Id, skill.Name, agent.Id, agent.Name);
        return null;
    }
}