using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Services.Providers;

/// <summary>
///     Implementation of <see cref="IRuntimeAgentProvider" /> that adapts an <see cref="IAgentManager" />.
///     Provides a simplified read-only interface to runtime agents by delegating
///     all lookups to the underlying agent manager.
/// </summary>
/// <remarks>
///     This class serves as an adapter between the full <see cref="IAgentManager" /> interface
///     and the simplified <see cref="IRuntimeAgentProvider" /> interface, exposing only
///     read-only agent access without lifecycle management operations.
/// </remarks>
public class RuntimeAgentProvider : IRuntimeAgentProvider
{
    private readonly IAgentManager _agentManager;
    private readonly ILogger<RuntimeAgentProvider> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuntimeAgentProvider" /> class.
    /// </summary>
    /// <param name="agentManager">The agent manager to delegate lookups to.</param>
    /// <param name="logger">Logger for tracing agent lookup operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentManager" /> is null.</exception>
    public RuntimeAgentProvider(IAgentManager agentManager, ILogger<RuntimeAgentProvider> logger)
    {
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogProviderCreated(GetHashCode(), _agentManager.GetHashCode(), _agentManager.GetType().Name);
    }

    /// <inheritdoc />
    public IEnumerable<IRuntimeAgent> GetAvailableRuntimeAgents()
    {
        return _agentManager.ActiveAgents;
    }

    /// <inheritdoc />
    public IRuntimeAgent? GetRuntimeAgent(Guid id)
    {
        var agent = _agentManager.GetAgent(id);
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogRuntimeAgentLookup(id, agent != null, GetHashCode(), _agentManager.GetHashCode());
        return agent;
    }
}