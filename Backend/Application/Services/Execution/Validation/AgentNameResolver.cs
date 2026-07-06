using FHOOE.Freydis.Agents.Services.Managers;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Resolves an agent's display name by delegating to <see cref="IAgentManager.GetAgent(Guid)" />.
///     Returns a safe fallback string when the agent is not currently registered so that callers
///     always receive a printable label without needing null-handling.
/// </summary>
/// <param name="agentManager">
///     The agent manager used to look up active runtime agents by their unique identifier.
/// </param>
public sealed class AgentNameResolver(IAgentManager agentManager) : IAgentNameResolver
{
    private readonly IAgentManager _agentManager =
        agentManager ?? throw new ArgumentNullException(nameof(agentManager));

    /// <inheritdoc />
    public string Resolve(Guid agentId)
    {
        var agent = _agentManager.GetAgent(agentId);
        return agent?.Name ?? "Unknown Agent";
    }
}