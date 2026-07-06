using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Agents.DigitalTwin.Services;

/// <summary>
///     Factory for Digital Twin agents. Unlike other agent factories, Digital Twin agents are not
///     loaded from configuration files — they connect dynamically via WebSocket. Therefore,
///     <see cref="LoadAgentsAsync" /> returns an empty list and <see cref="CreateAgentAsync" /> is
///     not used. Agents are created by <see cref="DigitalTwinWebSocketHandler" /> when a Twin connects.
/// </summary>
/// <remarks>
///     <para>
///         This factory exists to satisfy the <see cref="IAgentFactory" /> contract and allow
///         the <see cref="UnifiedAgentManager" /> to recognise the
///         <see cref="AgentType.DigitalTwin" /> type. The actual agent creation is handled by the
///         WebSocket handler, which calls <see cref="IAgentManager.RegisterAgent" /> directly.
///     </para>
/// </remarks>
public class DigitalTwinAgentFactory : IDigitalTwinAgentFactory, IAgentFactory
{
    private readonly ILogger<DigitalTwinAgentFactory> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DigitalTwinAgentFactory" /> class.
    /// </summary>
    /// <param name="logger">Logger instance for factory events.</param>
    public DigitalTwinAgentFactory(ILogger<DigitalTwinAgentFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public AgentType AgentType => AgentType.DigitalTwin;

    /// <summary>
    ///     Returns an empty list. Digital Twin agents connect dynamically via WebSocket
    ///     and are not loaded from configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An empty list of runtime agents.</returns>
    public Task<List<IRuntimeAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogLoadAgentsReturningEmpty();
        return Task.FromResult(new List<IRuntimeAgent>());
    }

    /// <summary>
    ///     Not supported. Digital Twin agents are created by
    ///     <see cref="DigitalTwinWebSocketHandler" /> during WebSocket handshake.
    /// </summary>
    /// <param name="configuration">Unused.</param>
    /// <param name="cancellationToken">Unused.</param>
    /// <returns>A faulted task with <see cref="NotSupportedException" />.</returns>
    public Task<IRuntimeAgent> CreateAgentAsync(object configuration,
        CancellationToken cancellationToken = default)
    {
        return Task.FromException<IRuntimeAgent>(new NotSupportedException(
            "Digital Twin agents cannot be created via factory. " +
            "They are created dynamically when a Twin connects via WebSocket."));
    }
}

/// <summary>
///     Marker interface for the Digital Twin agent factory, following the same pattern
///     as <see cref="Dummy.Services.IDummyAgentFactory" /> and
///     <see cref="Kuka.Services.IKukaAgentFactory" />.
/// </summary>
public interface IDigitalTwinAgentFactory;