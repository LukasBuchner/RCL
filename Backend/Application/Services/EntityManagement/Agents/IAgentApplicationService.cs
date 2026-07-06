using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Agents;

/// <summary>
///     Application service for agent management operations with integrated reactive notifications.
///     Provides a simple, focused interface for all agent-related operations.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     This interface follows the same pattern as INodeApplicationService with direct repository access.
///     It integrates reactive notifications directly, eliminating the need for separate event dispatchers.
///     All operations that modify agents automatically trigger notifications to subscribers.
/// </remarks>
public interface IAgentApplicationService : IDisposable
{
    /// <summary>
    ///     Creates a new agent in the system.
    /// </summary>
    /// <param name="agent">The agent entity to create. Must not be null and should have valid data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created agent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the agent parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnAgentsChanged observable.
    /// </remarks>
    Task<Agent> CreateAgentAsync(Agent agent);

    /// <summary>
    ///     Updates an existing agent with new data.
    /// </summary>
    /// <param name="agent">The agent entity containing updated data. Must not be null and must have an existing ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the updated agent if successful,
    ///     null otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the agent parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnAgentsChanged observable when
    ///     successful.
    /// </remarks>
    Task<Agent?> UpdateAgentAsync(Agent agent);

    /// <summary>
    ///     Deletes an agent from the system.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to delete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the agent was not found.
    /// </returns>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnAgentsChanged observable when
    ///     successful.
    /// </remarks>
    Task<bool> DeleteAgentAsync(Guid agentId);

    /// <summary>
    ///     Retrieves all agents from the system.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all agents.</returns>
    Task<IReadOnlyList<Agent>> GetAllAgentsAsync();

    /// <summary>
    ///     Retrieves a specific agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the agent if found, null
    ///     otherwise.
    /// </returns>
    Task<Agent?> GetAgentByIdAsync(Guid agentId);

    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when agents have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of agents whenever any agent is created, updated, or
    ///     deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all agent changes.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions.
    /// </remarks>
    IObservable<IReadOnlyList<Agent>> OnAgentsChanged();
}