using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Headless <see cref="IAgentApplicationService" /> backed by the benchmark
///     <see cref="IAgentManager" />. It replaces the production <c>AgentApplicationService</c>, which
///     depends on an Infrastructure-layer <c>IRepository&lt;Agent&gt;</c> not present in the benchmark
///     container. The execution graph reaches this service through <c>NodeAgentMapper</c> when it maps
///     skill nodes to their agents for duration estimation, so the read methods project the registered
///     runtime agents onto Domain <see cref="Agent" /> entities; the write methods are inert.
/// </summary>
public sealed class BenchmarkAgentApplicationService : IAgentApplicationService
{
    private readonly IAgentManager _agentManager;
    private readonly Subject<IReadOnlyList<Agent>> _agentsChanged = new();

    /// <summary>
    ///     Initializes the service over the benchmark agent manager whose
    ///     <see cref="IAgentManager.ActiveAgents" /> hold the mock agents registered for the current run.
    /// </summary>
    /// <param name="agentManager">The benchmark agent manager supplying the registered runtime agents.</param>
    public BenchmarkAgentApplicationService(IAgentManager agentManager)
    {
        _agentManager = agentManager;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Agent>> GetAllAgentsAsync()
    {
        return Task.FromResult<IReadOnlyList<Agent>>(
            _agentManager.ActiveAgents.Select(ToDomainAgent).ToList());
    }

    /// <inheritdoc />
    public Task<Agent?> GetAgentByIdAsync(Guid agentId)
    {
        var agent = _agentManager.ActiveAgents.FirstOrDefault(a => a.Id == agentId);
        return Task.FromResult(agent is null ? null : ToDomainAgent(agent));
    }

    /// <inheritdoc />
    public Task<Agent> CreateAgentAsync(Agent agent)
    {
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<Agent?> UpdateAgentAsync(Agent agent)
    {
        return Task.FromResult<Agent?>(agent);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAgentAsync(Guid agentId)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Agent>> OnAgentsChanged()
    {
        return _agentsChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _agentsChanged.Dispose();
    }

    /// <summary>
    ///     Projects a registered runtime agent onto a Domain <see cref="Agent" /> carrying the identity and
    ///     display name the scheduling layer needs to map nodes to agents.
    /// </summary>
    /// <param name="agent">The registered runtime agent.</param>
    /// <returns>The corresponding Domain agent entity.</returns>
    private static Agent ToDomainAgent(FHOOE.Freydis.Agents.Agents.IRuntimeAgent agent)
    {
        return new Agent { Id = agent.Id, Name = agent.Name, RepresentativeColor = "#808080" };
    }
}