using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Agents.Agents.Kuka;
using FHOOE.Freydis.Agents.Agents.Kuka.Services;
using FHOOE.Freydis.Agents.Services;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.GraphQLServer.Configuration;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Service responsible for starting up agents based on configuration.
/// </summary>
public interface IAgentStartupService
{
    /// <summary>
    ///     Initializes agents based on the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAgentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Implementation of agent startup service that handles agent initialization for all enabled agent types.
///     Uses a unified factory-based pattern for loading agents and a single lifecycle notification
///     path via <see cref="IAgentLifecycleNotifier" /> to register them in the domain model.
/// </summary>
/// <remarks>
///     <para>
///         Each agent type (Dummy, KUKA, Real) is independently enabled via its sub-configuration's
///         <c>Enabled</c> flag. All enabled types follow the same initialization flow:
///     </para>
///     <list type="number">
///         <item>Load agents from factory using configuration file path</item>
///         <item>Register agents with <see cref="IAgentManager" /> (in-memory)</item>
///         <item>
///             Notify <see cref="IAgentLifecycleNotifier" /> which ensures the agent is active
///             in the domain model (idempotent — handles both new and restarted agents)
///         </item>
///         <item>Run any type-specific post-activation hook (e.g. OPC UA connect for KUKA)</item>
///     </list>
///     <para>
///         Digital Twin agents are client-initiated (connect via WebSocket) and require no startup initialization.
///     </para>
/// </remarks>
public class AgentStartupService : IAgentStartupService
{
    private readonly IAgentManager _agentManager;
    private readonly AgentsConfiguration _agentsConfig;
    private readonly IDummyAgentFactory _dummyAgentFactory;
    private readonly IKukaAgentFactory _kukaAgentFactory;
    private readonly IAgentLifecycleNotifier _lifecycleNotifier;
    private readonly ILogger<AgentStartupService> _logger;

    /// <summary>
    ///     Initializes a new instance of the AgentStartupService.
    /// </summary>
    /// <param name="agentsOptions">Agent configuration options containing per-type enabled flags and file paths.</param>
    /// <param name="agentManager">Unified agent manager for all agent types.</param>
    /// <param name="dummyAgentFactory">Factory for creating dummy agents from configuration.</param>
    /// <param name="kukaAgentFactory">Factory for creating KUKA agents from configuration.</param>
    /// <param name="lifecycleNotifier">
    ///     Notifier for domain-level activation of agents. Ensures agents are persisted
    ///     in PostgreSQL and have their skills synchronized on every startup.
    /// </param>
    /// <param name="logger">Logger instance for diagnostic information.</param>
    public AgentStartupService(
        IOptions<AgentsConfiguration> agentsOptions,
        IAgentManager agentManager,
        IDummyAgentFactory dummyAgentFactory,
        IKukaAgentFactory kukaAgentFactory,
        IAgentLifecycleNotifier lifecycleNotifier,
        ILogger<AgentStartupService> logger)
    {
        _agentsConfig = agentsOptions.Value;
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _dummyAgentFactory = dummyAgentFactory ?? throw new ArgumentNullException(nameof(dummyAgentFactory));
        _kukaAgentFactory = kukaAgentFactory ?? throw new ArgumentNullException(nameof(kukaAgentFactory));
        _lifecycleNotifier = lifecycleNotifier ?? throw new ArgumentNullException(nameof(lifecycleNotifier));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAgentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInitializingAgents(
            _agentsConfig.DummyAgents.Enabled, _agentsConfig.KukaAgents.Enabled,
            _agentsConfig.RealAgents.Enabled, _agentsConfig.DigitalTwin.Enabled);

        try
        {
            if (_agentsConfig.DummyAgents.Enabled)
                await InitializeFromConfigAsync(
                    ct => CastAgents(_dummyAgentFactory.CreateFromJsonFileAsync(
                        _agentsConfig.DummyAgents.ConfigurationFile!, ct)),
                    _agentsConfig.DummyAgents.AutoLoad,
                    _agentsConfig.DummyAgents.ConfigurationFile,
                    "Dummy",
                    null,
                    cancellationToken);

            if (_agentsConfig.KukaAgents.Enabled)
                await InitializeFromConfigAsync(
                    ct => CastAgents(_kukaAgentFactory.CreateFromJsonFileAsync(
                        _agentsConfig.KukaAgents.ConfigurationFile!, ct)),
                    _agentsConfig.KukaAgents.AutoLoad,
                    _agentsConfig.KukaAgents.ConfigurationFile,
                    "KUKA",
                    ConnectKukaAgentAsync,
                    cancellationToken);

            if (_agentsConfig.RealAgents.Enabled)
                _logger.LogRealAgentsNotYetImplemented();

            // Digital Twin agents are client-initiated (WebSocket) — no startup initialization needed
        }
        catch (Exception ex)
        {
            _logger.LogAgentInitFailed(ex);
            throw;
        }
    }

    /// <summary>
    ///     Loads agents from a factory, registers them in-memory and in the domain model,
    ///     and optionally runs a type-specific post-activation hook for each agent.
    /// </summary>
    /// <param name="loadAgents">
    ///     Delegate that loads agents from the factory (e.g. from a JSON config file).
    /// </param>
    /// <param name="autoLoad">Whether auto-load is enabled for this agent type.</param>
    /// <param name="configFile">The configuration file path (for logging and validation).</param>
    /// <param name="agentTypeName">Human-readable name of the agent type (for log messages).</param>
    /// <param name="postActivationHook">
    ///     Optional callback invoked after each agent is activated in the domain model.
    ///     Used for type-specific setup like OPC UA connection for KUKA agents.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task InitializeFromConfigAsync(
        Func<CancellationToken, Task<List<IRuntimeAgent>>> loadAgents,
        bool autoLoad,
        string? configFile,
        string agentTypeName,
        Func<IRuntimeAgent, CancellationToken, Task>? postActivationHook,
        CancellationToken cancellationToken)
    {
        if (!autoLoad)
        {
            _logger.LogAgentAutoLoadDisabled(agentTypeName);
            return;
        }

        if (string.IsNullOrEmpty(configFile))
        {
            _logger.LogNoAgentConfigFile(agentTypeName);
            return;
        }

        _logger.LogLoadingAgents(agentTypeName, configFile);

        try
        {
            var agents = await loadAgents(cancellationToken);
            _logger.LogFactoryCreatedAgents(agents.Count, agentTypeName);

            var activatedCount = 0;

            foreach (var agent in agents)
                try
                {
                    // Step 1: Register in-memory with the unified manager
                    _agentManager.RegisterAgent(agent);

                    // Step 2: Ensure active in domain model (idempotent — handles new + restart)
                    await _lifecycleNotifier.OnAgentConnectedAsync(agent, cancellationToken);

                    // Step 3: Run type-specific post-activation (e.g. OPC UA connect)
                    if (postActivationHook != null)
                        await postActivationHook(agent, cancellationToken);

                    activatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogAgentActivationFailed(ex, agentTypeName, agent.Name, agent.Id);
                }

            _logger.LogAgentsReady(agentTypeName, activatedCount, agents.Count);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogAgentConfigFileNotFound(ex, agentTypeName, configFile);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogAgentLoadFailed(ex, agentTypeName);
            throw;
        }
    }

    /// <summary>
    ///     Connects a KUKA agent to its OPC UA server after domain activation.
    /// </summary>
    /// <param name="agent">The runtime agent (must be a <see cref="KukaIiwa14RuntimeAgent" />).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task ConnectKukaAgentAsync(IRuntimeAgent agent, CancellationToken cancellationToken)
    {
        if (agent is KukaIiwa14RuntimeAgent kukaAgent)
            await kukaAgent.ConnectAsync(cancellationToken);
    }

    /// <summary>
    ///     Casts a typed agent list to the base <see cref="IRuntimeAgent" /> list.
    /// </summary>
    private static async Task<List<IRuntimeAgent>> CastAgents<T>(Task<List<T>> agentsTask) where T : IRuntimeAgent
    {
        var agents = await agentsTask;
        return agents.Cast<IRuntimeAgent>().ToList();
    }
}