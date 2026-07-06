namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Resolves an agent's human-readable display name from its unique identifier.
///     Acts as a narrow facade over <see cref="FHOOE.Freydis.Agents.Services.Managers.IAgentManager" />
///     so that validators remain ISP-compliant and their test mocks stay trivial.
/// </summary>
public interface IAgentNameResolver
{
    /// <summary>
    ///     Returns the display name for the agent identified by <paramref name="agentId" />.
    ///     When no active agent with that ID is found, a safe fallback string is returned instead
    ///     of throwing, so violation records always contain a printable label.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent whose name is requested.</param>
    /// <returns>
    ///     The agent's <see cref="FHOOE.Freydis.Agents.Agents.IRuntimeAgent.Name" /> when the agent
    ///     is currently registered; otherwise <c>"Unknown Agent"</c>.
    /// </returns>
    string Resolve(Guid agentId);
}