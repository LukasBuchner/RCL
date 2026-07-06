using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;

namespace FHOOE.Freydis.Agents.Services.Providers;

/// <summary>
///     Interface for providing access to runtime agents.
///     This interface serves as an alias to IAgentManager for backward compatibility
///     and specific use cases where only read access to agents is needed.
/// </summary>
/// <remarks>
///     This interface is designed to provide a simplified view of agent management
///     focused on accessing existing runtime agents without lifecycle management operations.
///     It essentially exposes the read-only aspects of IAgentManager.
/// </remarks>
/// <seealso cref="IAgentManager" />
/// <seealso cref="IRuntimeAgent" />
public interface IRuntimeAgentProvider
{
    /// <summary>
    ///     Gets all available runtime agents.
    /// </summary>
    /// <returns>A collection of all available runtime agents.</returns>
    IEnumerable<IRuntimeAgent> GetAvailableRuntimeAgents();

    /// <summary>
    ///     Gets a runtime agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <returns>The runtime agent if found; otherwise, null.</returns>
    IRuntimeAgent? GetRuntimeAgent(Guid id);
}