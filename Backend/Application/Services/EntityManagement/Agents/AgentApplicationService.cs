using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Agents;

/// <summary>
///     Application service for agent operations with direct repository access and integrated reactive notifications.
///     Follows the same simplified pattern as NodeApplicationService.
/// </summary>
/// <remarks>
///     This service implementation provides a simplified approach to agent management by directly using the repository
///     pattern with integrated reactive notifications using Rx.NET's Subject pattern.
/// </remarks>
public sealed class AgentApplicationService : IAgentApplicationService
{
    private readonly IRepository<Agent> _agentRepository;
    private readonly Subject<IReadOnlyList<Agent>> _agentsChangedSubject;
    private readonly ILogger<AgentApplicationService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentApplicationService" /> class.
    /// </summary>
    /// <param name="agentRepository">The repository for agent data persistence operations.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public AgentApplicationService(
        IRepository<Agent> agentRepository,
        ILogger<AgentApplicationService> logger)
    {
        _agentRepository = agentRepository ?? throw new ArgumentNullException(nameof(agentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentsChangedSubject = new Subject<IReadOnlyList<Agent>>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _agentsChangedSubject.Dispose();
    }

    /// <inheritdoc />
    public async Task<Agent> CreateAgentAsync(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        _logger.LogCreateStart("Agent", agent.Id, agent.Name);

        var createdAgent = await _agentRepository.CreateAsync(agent);

        // Notify subscribers with all agents
        await NotifyAgentsChangedAsync();

        return createdAgent;
    }

    /// <inheritdoc />
    public async Task<Agent?> UpdateAgentAsync(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        _logger.LogUpdateStart("Agent", agent.Id, agent.Name);

        var result = await _agentRepository.UpdateAsync(agent);

        if (result)
        {
            await NotifyAgentsChangedAsync();
            return agent;
        }

        _logger.LogUpdateFailed("Agent", agent.Id, agent.Name);
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAgentAsync(Guid agentId)
    {
        _logger.LogDeleteStart("Agent", agentId);

        var result = await _agentRepository.DeleteAsync(agentId);

        if (result)
            await NotifyAgentsChangedAsync();
        else
            _logger.LogDeleteFailed("Agent", agentId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Agent>> GetAllAgentsAsync()
    {
        var agents = await _agentRepository.GetAllAsync();
        _logger.LogGetAll("Agent", agents.Count);
        return agents.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Agent?> GetAgentByIdAsync(Guid agentId)
    {
        _logger.LogGetById("Agent", agentId);
        return await _agentRepository.GetByIdAsync(agentId);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Agent>> OnAgentsChanged()
    {
        return _agentsChangedSubject.AsObservable();
    }

    /// <summary>
    ///     Notifies all subscribers about agent changes by emitting the current state of all agents.
    /// </summary>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task NotifyAgentsChangedAsync()
    {
        try
        {
            var allAgents = await _agentRepository.GetAllAsync();
            _agentsChangedSubject.OnNext(allAgents.AsReadOnly());
            _logger.LogNotificationSent("Agent", allAgents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogNotificationFailed("Agent", ex);
            _agentsChangedSubject.OnError(ex);
        }
    }
}