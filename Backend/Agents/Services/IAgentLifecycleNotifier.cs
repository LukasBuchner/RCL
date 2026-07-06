using FHOOE.Freydis.Agents.Agents;

namespace FHOOE.Freydis.Agents.Services;

/// <summary>
///     Callback interface for notifying higher layers about runtime agent lifecycle events.
///     Used by dynamic agent sources (e.g. Digital Twin WebSocket connections) that need
///     to trigger domain-level registration without depending on the Application layer directly.
/// </summary>
/// <remarks>
///     This interface follows the Dependency Inversion Principle: the Agents layer defines
///     the abstraction, and the Application layer provides the implementation that bridges
///     runtime agents to the persistent domain model.
/// </remarks>
public interface IAgentLifecycleNotifier
{
    /// <summary>
    ///     Called when a runtime agent has connected and been registered in-memory.
    ///     Implementations should persist the agent in the domain model and synchronize skills.
    /// </summary>
    /// <param name="agent">The newly connected runtime agent.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnAgentConnectedAsync(IRuntimeAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Called when a runtime agent has disconnected and been removed from the in-memory registry.
    ///     Implementations should remove or mark the agent as offline in the domain model.
    /// </summary>
    /// <param name="agentId">The ID of the disconnected agent.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task OnAgentDisconnectedAsync(Guid agentId, CancellationToken cancellationToken = default);
}