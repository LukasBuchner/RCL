using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Minimal <see cref="IAgentManager" /> used by the reactive-execution convergence benchmark.
///     It backs only the two members the execution runtime exercises during a run:
///     <see cref="RegisterAgent" /> (to add the mock <c>DummyRuntimeAgent</c>s built per iteration) and
///     <see cref="GetAgent(System.Guid)" /> (the lookup <c>ExecutionInitializer</c> performs by
///     <c>SkillExecutionTask.AgentId</c>).
/// </summary>
/// <remarks>
///     <para>
///         Agents are held in a plain <see cref="Dictionary{TKey,TValue}" /> keyed by
///         <see cref="IRuntimeAgent.Id" />. The full <see cref="UnifiedAgentManager" /> uses a
///         concurrent dictionary for production thread-safety; the benchmark does not need that because
///         each iteration builds and registers its agents on a single thread before execution starts.
///     </para>
///     <para>
///         Every member outside the register/lookup pair throws <see cref="NotSupportedException" />:
///         the benchmark never queries health, looks up by name, starts, or stops agents, and a fake that
///         returned plausible values for those would obscure an unexpected call from the runtime rather
///         than surface it.
///     </para>
/// </remarks>
public sealed class BenchmarkAgentManager : IAgentManager
{
    private readonly Dictionary<Guid, IRuntimeAgent> _agents = new();

    /// <summary>
    ///     The runtime agents currently registered for this run, as a fresh snapshot of the backing
    ///     dictionary's values. The execution runtime reads this collection when mapping skill nodes to
    ///     their agents (through the runtime-agent provider and the agent application service), so it must
    ///     return the registered agents rather than throw.
    /// </summary>
    public IReadOnlyList<IRuntimeAgent> ActiveAgents => [.. _agents.Values];

    /// <summary>
    ///     The number of registered runtime agents.
    /// </summary>
    public int ActiveAgentCount => _agents.Count;

    /// <summary>
    ///     Retrieves a registered runtime agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to retrieve.</param>
    /// <returns>The registered <see cref="IRuntimeAgent" /> if present; otherwise null.</returns>
    public IRuntimeAgent? GetAgent(Guid agentId)
    {
        return _agents.GetValueOrDefault(agentId);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark looks agents up by id only.</exception>
    public IRuntimeAgent? GetAgent(string agentName)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Registers a runtime agent so the execution runtime can resolve it by id during a run.
    /// </summary>
    /// <param name="agent">The agent to register.</param>
    /// <returns>
    ///     True if the agent was added; false if an agent with the same
    ///     <see cref="IRuntimeAgent.Id" /> is already registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agent" /> is null.</exception>
    public bool RegisterAgent(IRuntimeAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return _agents.TryAdd(agent.Id, agent);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never queries agent health.</exception>
    public Task<List<AgentHealthStatus>> GetAllAgentHealthAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never queries agent health.</exception>
    public Task<AgentHealthStatus?> GetAgentHealthAsync(Guid agentId)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; agents are pre-built and registered, never started here.</exception>
    public Task<IRuntimeAgent> StartAgentAsync(Guid agentId, string agentName)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark discards the whole manager per iteration.</exception>
    public Task<bool> StopAgentAsync(Guid agentId)
    {
        throw new NotSupportedException();
    }
}