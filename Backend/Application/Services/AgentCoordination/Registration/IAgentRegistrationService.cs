using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.Registration;

/// <summary>
///     Service responsible for managing runtime agent registration in the domain model.
///     Provides idempotent activation for agent startup and reconnection, and state
///     transitions for lifecycle events such as disconnect.
/// </summary>
public interface IAgentRegistrationService
{
    /// <summary>
    ///     Ensures a runtime agent is active in the domain model (idempotent).
    ///     If the agent does not exist, it is created with <see cref="AgentState.Active" />.
    ///     If the agent already exists, its state is set to <see cref="AgentState.Active" />,
    ///     its skills are synchronized, and its <c>LastSeenUtc</c> timestamp is updated.
    /// </summary>
    /// <param name="runtimeAgent">The runtime agent to activate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The domain agent entity (created or reactivated).</returns>
    Task<Agent> EnsureAgentActiveAsync(IRuntimeAgent runtimeAgent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the state of an existing agent in the domain model.
    /// </summary>
    /// <param name="agentId">The ID of the agent to update.</param>
    /// <param name="state">The new state of the agent.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated agent if found; null otherwise.</returns>
    Task<Agent?> UpdateAgentStateAsync(Guid agentId, AgentState state, CancellationToken cancellationToken = default);
}