using System.Collections.Concurrent;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Agents.Agents.Kuka.Services;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Services.Managers;

/// <summary>
///     Unified agent manager that tracks active runtime agents across all agent types.
/// </summary>
/// <remarks>
///     <para>
///         This is the primary implementation of <see cref="IAgentManager" />. It holds the active
///         <see cref="IRuntimeAgent" /> instances in a thread-safe collection and exposes lookup,
///         health, and lifecycle operations over them. Agents are added through
///         <see cref="RegisterAgent" /> — typically by the startup service after a typed
///         <see cref="IAgentFactory" /> (such as <see cref="IDummyAgentFactory" /> or
///         <see cref="IKukaAgentFactory" />) has created them from configuration.
///     </para>
///     <para>
///         <b>Thread Safety:</b> This implementation is thread-safe for concurrent agent
///         registration, retrieval, and removal; the active-agent collection is backed by a
///         <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}" />.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Create an agent and register it with the manager
/// var manager = new UnifiedAgentManager(logger);
/// var customAgent = new DummyRuntimeAgent(id, name, logger);
/// bool registered = manager.RegisterAgent(customAgent);
///
/// // Query agents
/// var agent = manager.GetAgent(agentId);
/// var health = await manager.GetAgentHealthAsync(agentId);
/// </code>
/// </example>
/// <seealso cref="IAgentManager" />
/// <seealso cref="IAgentFactory" />
/// <seealso cref="IDummyAgentFactory" />
/// <seealso cref="IKukaAgentFactory" />
public class UnifiedAgentManager : IAgentManager
{
    private readonly ConcurrentDictionary<Guid, IRuntimeAgent> _agents = new();
    private readonly ILogger<UnifiedAgentManager> _logger;

    /// <summary>
    ///     Initializes a new instance of the UnifiedAgentManager.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
    public UnifiedAgentManager(ILogger<UnifiedAgentManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<IRuntimeAgent> ActiveAgents => _agents.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public int ActiveAgentCount => _agents.Count;

    /// <inheritdoc />
    public IRuntimeAgent? GetAgent(Guid agentId)
    {
        var found = _agents.TryGetValue(agentId, out var agent);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogAgentLookup(agentId, found, _agents.Count, GetHashCode(),
                string.Join(", ", _agents.Keys));

        return found ? agent : null;
    }

    /// <inheritdoc />
    public IRuntimeAgent? GetAgent(string agentName)
    {
        return _agents.Values.FirstOrDefault(a =>
            a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<List<AgentHealthStatus>> GetAllAgentHealthAsync()
    {
        var healthTasks = _agents.Values.Select(agent => agent.GetHealthStatusAsync());
        var healthStatuses = await Task.WhenAll(healthTasks);
        return healthStatuses.ToList();
    }

    /// <inheritdoc />
    public async Task<AgentHealthStatus?> GetAgentHealthAsync(Guid agentId)
    {
        var agent = GetAgent(agentId);
        if (agent == null)
        {
            _logger.LogAgentNotFound(agentId);
            return null;
        }

        return await agent.GetHealthStatusAsync();
    }

    /// <inheritdoc />
    public Task<IRuntimeAgent> StartAgentAsync(Guid agentId, string agentName)
    {
        throw new NotImplementedException("Dynamic agent start not yet implemented");
    }

    /// <inheritdoc />
    public Task<bool> StopAgentAsync(Guid agentId)
    {
        var removed = _agents.TryRemove(agentId, out var agent);

        if (removed)
            _logger.LogAgentStopped(agent!.Name, agentId);
        else
            _logger.LogAgentNotFoundForStopping(agentId);

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public bool RegisterAgent(IRuntimeAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var added = _agents.TryAdd(agent.Id, agent);

        if (added)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogAgentRegistered(agent.Name, agent.Id, GetHashCode(), _agents.Count);
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogAgentAlreadyRegistered(agent.Id, GetHashCode());
        }

        return added;
    }
}