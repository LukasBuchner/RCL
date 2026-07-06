using FHOOE.Freydis.Agents.Agents;

namespace FHOOE.Freydis.Agents.Services.Factories;

/// <summary>
///     Enumeration of supported agent types.
/// </summary>
public enum AgentType
{
    /// <summary>
    ///     Dummy agent for testing and simulation purposes.
    /// </summary>
    Dummy = 0,

    /// <summary>
    ///     KUKA iiwa 14 collaborative robot agent.
    /// </summary>
    KukaIiwa14 = 1,

    /// <summary>
    ///     Unity Digital Twin agent connected via WebSocket.
    /// </summary>
    DigitalTwin = 2
}

/// <summary>
///     Generic factory interface for creating runtime agents of any type.
///     Follows Factory Pattern and Open/Closed Principle.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    ///     Gets the type of agents this factory creates.
    /// </summary>
    AgentType AgentType { get; }

    /// <summary>
    ///     Loads agents from configuration asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of runtime agents.</returns>
    Task<List<IRuntimeAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a single agent from configuration object.
    /// </summary>
    /// <param name="configuration">Type-specific configuration object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created runtime agent.</returns>
    /// <exception cref="ArgumentException">If configuration type is invalid.</exception>
    /// <exception cref="ArgumentNullException">If configuration is null.</exception>
    Task<IRuntimeAgent> CreateAgentAsync(object configuration, CancellationToken cancellationToken = default);
}