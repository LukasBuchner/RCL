using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services;
using FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.Registration;

/// <summary>
///     Bridges runtime agent lifecycle events to the persistent domain model.
///     When an agent connects (via startup or WebSocket), this notifier ensures it is
///     active in the domain so it appears in GraphQL queries and can be assigned skills.
///     On disconnect, the agent is marked <see cref="AgentState.Inactive" /> but persists
///     in the domain so workflows can still reference it.
/// </summary>
public class DomainAgentLifecycleNotifier : IAgentLifecycleNotifier
{
    private readonly ILogger<DomainAgentLifecycleNotifier> _logger;
    private readonly IAgentRegistrationService _registrationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainAgentLifecycleNotifier" /> class.
    /// </summary>
    /// <param name="registrationService">The registration service for persisting agents to the domain model.</param>
    /// <param name="logger">Logger for lifecycle events.</param>
    public DomainAgentLifecycleNotifier(
        IAgentRegistrationService registrationService,
        ILogger<DomainAgentLifecycleNotifier> logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task OnAgentConnectedAsync(IRuntimeAgent agent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _registrationService.EnsureAgentActiveAsync(agent, cancellationToken);
            _logger.LogDomainActivationCompleted(agent.Name, agent.Id);
        }
        catch (Exception ex)
        {
            // Domain activation must never kill the caller (e.g. WebSocket connection).
            // The agent is already registered in-memory and can execute skills;
            // it just won't appear in GraphQL queries until the next successful sync.
            _logger.LogDomainActivationFailed(agent.Name, agent.Id, ex);
        }
    }

    /// <inheritdoc />
    public async Task OnAgentDisconnectedAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var updated = await _registrationService.UpdateAgentStateAsync(agentId, AgentState.Inactive, cancellationToken);
        if (updated != null)
            _logger.LogAgentMarkedInactive(updated.Name, agentId);
        else
            _logger.LogNoRecordForDisconnectedAgent(agentId);
    }
}