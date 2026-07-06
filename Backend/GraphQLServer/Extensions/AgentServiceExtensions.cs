using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Services;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Agents.Agents.Kuka.Services;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.GraphQLServer.Configuration;
using FHOOE.Freydis.GraphQLServer.Services.Initialization;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using Serilog;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring agent services based on configuration.
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    ///     Adds agent services to the service collection based on configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration containing agent settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method configures the agent management infrastructure. All typed agent factories
    ///         are registered in the container, and <see cref="UnifiedAgentManager" /> is registered as
    ///         the primary <see cref="IAgentManager" />. The per-type <c>Enabled</c> flags in each agent
    ///         sub-configuration determine which agents <see cref="AgentStartupService" /> loads at
    ///         startup:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><b>DummyAgents.Enabled</b>: Loads dummy agents via <see cref="IDummyAgentFactory" /> for testing and debugging</item>
    ///         <item><b>KukaAgents.Enabled</b>: Loads KUKA agents via <see cref="IKukaAgentFactory" /> for KUKA robot integration</item>
    ///         <item><b>DigitalTwin.Enabled</b>: Enables <see cref="IDigitalTwinAgentFactory" /> for client-initiated VR Digital Twin connections</item>
    ///         <item><b>RealAgents.Enabled</b>: Reserved for future production agent implementations</item>
    ///     </list>
    /// </remarks>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register agent configuration options
        services.Configure<AgentsConfiguration>(configuration.GetSection("Agents"));
        services.Configure<SceneOptions>(configuration.GetSection(SceneOptions.SectionName));
        services.Configure<SkillsOptions>(configuration.GetSection(SkillsOptions.SectionName));

        // Register scene and skill initialization services
        services.AddScoped<ISceneInitializationService, SceneInitializationService>();
        services.AddSingleton<SkillsInitializationService>();
        services.AddSingleton<ISkillsInitializationService>(sp => sp.GetRequiredService<SkillsInitializationService>());

        // IMPORTANT: Register ISkillDefinitionProvider BEFORE factories to satisfy dependencies
        services.AddSingleton<ISkillDefinitionProvider, SkillDefinitionProvider>();

        // Get agent configuration to determine factory registration strategy
        var boundAgentConfig = configuration.GetSection("Agents").Get<AgentsConfiguration>();
        var agentConfig = boundAgentConfig ?? new AgentsConfiguration();
        if (boundAgentConfig is null)
            Log.Warning(
                "Agent configuration section 'Agents' was missing or unbindable; falling back to default AgentsConfiguration (Dummy and DigitalTwin enabled by default)");

        // Register agent factories (requires ISkillDefinitionProvider)
        RegisterAgentFactories(services, agentConfig, configuration);

        // Register UnifiedAgentManager as the primary IAgentManager for all modes
        services.AddSingleton<UnifiedAgentManager>();
        services.AddSingleton<IAgentManager>(provider => provider.GetRequiredService<UnifiedAgentManager>());

        // Register agent startup service for automatic initialization on application start
        services.AddSingleton<IAgentStartupService, AgentStartupService>();

        // Register hosted service for automatic scene and agent initialization
        services.AddHostedService<InitializationHostedService>();

        return services;
    }

    /// <summary>
    ///     Registers agent factories and supporting services in the DI container.
    /// </summary>
    /// <param name="services">The service collection to register factories with.</param>
    /// <param name="agentConfig">The agent configuration containing per-type enabled flags.</param>
    /// <param name="configuration">The application configuration for binding sub-sections.</param>
    /// <remarks>
    ///     All factories are registered regardless of enabled state to satisfy dependency injection.
    ///     The enabled flags determine which factories are actually used at runtime via the manager.
    ///     Digital Twin WebSocket handler and configuration are only registered when Digital Twin is enabled.
    /// </remarks>
    private static void RegisterAgentFactories(IServiceCollection services, AgentsConfiguration agentConfig,
        IConfiguration configuration)
    {
        // Always register all factories to satisfy dependency injection requirements
        // AgentStartupService requires factories regardless of enabled state
        services.AddSingleton<IDummyAgentFactory, DummyAgentFactory>();
        services.AddSingleton<IKukaAgentFactory, KukaAgentFactory>();
        services.AddSingleton<IDigitalTwinAgentFactory, DigitalTwinAgentFactory>();

        // Register Digital Twin WebSocket handler and configuration only when enabled
        if (!agentConfig.DigitalTwin.Enabled) return;
        services.Configure<DigitalTwinAgentConfiguration>(cfg =>
        {
            var boundDtConfig = configuration.GetSection("Agents:DigitalTwin").Get<DigitalTwinAgentsConfiguration>();
            var dtConfig = boundDtConfig ?? new DigitalTwinAgentsConfiguration();
            if (boundDtConfig is null)
                Log.Warning(
                    "Digital Twin configuration subsection 'Agents:DigitalTwin' was missing or unbindable; falling back to default Digital Twin parameters (NominalDuration={NominalDurationSeconds}s, PingInterval={PingIntervalSeconds}s, EstimateTimeout={EstimateTimeoutSeconds}s)",
                    dtConfig.NominalDurationSeconds,
                    dtConfig.PingIntervalSeconds,
                    dtConfig.EstimateTimeoutSeconds);
            cfg.NominalDurationSeconds = dtConfig.NominalDurationSeconds;
            cfg.PingIntervalSeconds = dtConfig.PingIntervalSeconds;
            cfg.EstimateTimeoutSeconds = dtConfig.EstimateTimeoutSeconds;
            cfg.MaxConcurrentExecutions = dtConfig.MaxConcurrentExecutions;
            cfg.ReceiveBufferSize = dtConfig.ReceiveBufferSize;
        });
        services.AddSingleton<DigitalTwinWebSocketHandler>();
    }
}