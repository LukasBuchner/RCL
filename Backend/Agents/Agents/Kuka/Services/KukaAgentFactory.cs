using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Kuka.Configuration;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.OpcUa;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Agents.Utilities;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Agents.Kuka.Services;

/// <summary>
///     Interface for creating KUKA iiwa 14 agents from various sources.
/// </summary>
public interface IKukaAgentFactory
{
    /// <summary>
    ///     Creates KUKA agents from a JSON configuration file.
    /// </summary>
    /// <param name="configurationFilePath">Path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created KUKA agents.</returns>
    Task<List<KukaIiwa14RuntimeAgent>> CreateFromJsonFileAsync(
        string configurationFilePath,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Factory service for creating KUKA iiwa 14 agents from configuration.
/// </summary>
public class KukaAgentFactory : IKukaAgentFactory, IAgentFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly ILogger<KukaAgentFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISceneEntityProvider _sceneEntityProvider;
    private readonly ISkillDefinitionProvider _skillDefinitionProvider;

    /// <summary>
    ///     Initializes a new instance of the KukaAgentFactory.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="loggerFactory">Logger factory for creating agent loggers.</param>
    /// <param name="sceneEntityProvider">Provider for scene entities from database.</param>
    /// <param name="skillDefinitionProvider">Provider for skill definitions from configuration.</param>
    public KukaAgentFactory(
        ILogger<KukaAgentFactory> logger,
        ILoggerFactory loggerFactory,
        ISceneEntityProvider sceneEntityProvider,
        ISkillDefinitionProvider skillDefinitionProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _sceneEntityProvider = sceneEntityProvider ?? throw new ArgumentNullException(nameof(sceneEntityProvider));
        _skillDefinitionProvider =
            skillDefinitionProvider ?? throw new ArgumentNullException(nameof(skillDefinitionProvider));
    }

    /// <inheritdoc />
    public async Task<List<KukaIiwa14RuntimeAgent>> CreateFromJsonFileAsync(
        string configurationFilePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogLoadingFromFile(configurationFilePath);

        if (!File.Exists(configurationFilePath))
        {
            _logger.LogConfigurationFileNotFound(configurationFilePath);
            throw new FileNotFoundException($"Configuration file not found: {configurationFilePath}");
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configurationFilePath, cancellationToken);
            return await CreateFromJsonAsync(jsonContent);
        }
        catch (Exception ex)
        {
            _logger.LogLoadConfigurationFailed(ex, configurationFilePath);
            throw;
        }
    }

    /// <summary>
    ///     Creates KUKA agents from a JSON configuration string.
    /// </summary>
    /// <param name="jsonConfiguration">JSON configuration string.</param>
    /// <returns>List of created KUKA agents.</returns>
    private async Task<List<KukaIiwa14RuntimeAgent>> CreateFromJsonAsync(
        string jsonConfiguration)
    {
        _logger.LogParsingJsonConfiguration();

        try
        {
            var configuration =
                JsonSerializer.Deserialize<KukaIiwa14AgentConfiguration>(jsonConfiguration, JsonOptions);
            if (configuration == null)
            {
                _logger.LogConfigurationDeserializedToNull();
                return [];
            }

            return await CreateFromConfigurationAsync(configuration);
        }
        catch (JsonException ex)
        {
            _logger.LogJsonParsingFailed(ex);
            throw new ArgumentException("Invalid JSON configuration", nameof(jsonConfiguration), ex);
        }
    }

    /// <summary>
    ///     Creates KUKA agents from a configuration object.
    /// </summary>
    /// <param name="configuration">Configuration object.</param>
    /// <returns>List of created KUKA agents.</returns>
    private async Task<List<KukaIiwa14RuntimeAgent>> CreateFromConfigurationAsync(
        KukaIiwa14AgentConfiguration configuration)
    {
        _logger.LogCreatingAgentsFromConfiguration(configuration.Agents.Count);

        // Load position tags, scene objects, and skill definitions from providers
        _logger.LogLoadingProviderData();
        var positionTags = await _sceneEntityProvider.GetPositionTagsAsync();
        var sceneObjects = await _sceneEntityProvider.GetSceneObjectsAsync();
        var skillDefinitions = await _skillDefinitionProvider.GetSkillDefinitionsAsync();

        _logger.LogProviderDataLoaded(positionTags.Count, sceneObjects.Count, skillDefinitions.Count);

        var agents = new List<KukaIiwa14RuntimeAgent>();

        foreach (var agentConfig in configuration.Agents)
            try
            {
                var agent = await CreateKukaAgentAsync(agentConfig, skillDefinitions, positionTags, sceneObjects);
                agents.Add(agent);
                _logger.LogKukaAgentCreated(agent.Name, agent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogKukaAgentCreationFailed(ex, agentConfig.Name);
                throw;
            }

        _logger.LogKukaAgentsCreated(agents.Count);
        return agents;
    }

    /// <summary>
    ///     Creates a single KUKA agent from configuration, resolving skill definitions.
    /// </summary>
    /// <param name="agentConfig">Agent configuration.</param>
    /// <param name="skillDefinitions">Available skill definitions.</param>
    /// <param name="positionTags">Available position tags.</param>
    /// <param name="sceneObjects">Available scene objects.</param>
    /// <returns>Created KUKA agent.</returns>
    private Task<KukaIiwa14RuntimeAgent> CreateKukaAgentAsync(
        KukaIiwa14AgentConfig agentConfig,
        Dictionary<Guid, SkillDefinition> skillDefinitions,
        Dictionary<Guid, PositionTag> positionTags,
        Dictionary<Guid, SceneObject> sceneObjects)
    {
        _logger.LogCreatingKukaAgent(agentConfig.Name);

        // Generate ID if not provided
        var agentId = agentConfig.Id ?? Guid.NewGuid();

        // Convert skills from configuration to domain entities
        var skills = agentConfig.Skills.Select(skillConfig =>
        {
            // Check if this skill references a definition
            if (!skillConfig.SkillDefinitionId.HasValue)
                throw new InvalidOperationException(
                    $"Skill configuration for agent {agentConfig.Name} must have a SkillDefinitionId");

            // Resolve skill definition
            if (!skillDefinitions.TryGetValue(skillConfig.SkillDefinitionId.Value, out var definition))
                throw new InvalidOperationException(
                    $"Skill definition {skillConfig.SkillDefinitionId.Value} not found for agent {agentConfig.Name}");

            var skill = definition.ToSkill(positionTags, sceneObjects);
            _logger.LogSkillResolved(skill.Name, agentConfig.Name);

            return skill;
        }).ToList();

        // Create logger instances
        var agentLogger = _loggerFactory.CreateLogger<KukaIiwa14RuntimeAgent>();
        var connectionManagerLogger = _loggerFactory.CreateLogger<OpcUaConnectionManager>();
        var dataReaderLogger = _loggerFactory.CreateLogger<KukaIiwa14DataReader>();
        var propertyExtractorLogger = _loggerFactory.CreateLogger<SkillPropertyExtractor>();

        // Create certificate manager if encryption is enabled
        OpcUaCertificateManager? certificateManager = null;
        if (agentConfig.Security?.UseEncryption == true)
        {
            var certLogger = _loggerFactory.CreateLogger<OpcUaCertificateManager>();
            certificateManager = new OpcUaCertificateManager(
                certLogger,
                agentConfig.Security.CertificateStorePath);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogCertificateManagerCreated(agentConfig.Name, certificateManager.GetCertificateStorePath());
        }

        // Create OPC UA connection manager
        var securityConfig = agentConfig.Security ?? new OpcUaSecurityConfig();
        var connectionManager = new OpcUaConnectionManager(
            agentConfig.OpcUaEndpoint,
            $"KUKA iiwa 14 Agent - {agentConfig.Name}",
            $"urn:localhost:FHOOE.Freydis:KukaIiwa14:{agentConfig.Name}",
            securityConfig,
            connectionManagerLogger,
            certificateManager);

        // Create supporting components
        var dataReader = new KukaIiwa14DataReader(dataReaderLogger);
        var executionStats = new AgentExecutionStatistics();
        var propertyExtractor = new SkillPropertyExtractor(propertyExtractorLogger);

        // Create KUKA agent with dependency injection
        var agent = new KukaIiwa14RuntimeAgent(
            agentId,
            agentConfig.Name,
            connectionManager,
            dataReader,
            executionStats,
            propertyExtractor,
            skills,
            agentLogger);

        _logger.LogKukaAgentCreatedWithSkills(agentConfig.Name, skills.Count);

        return Task.FromResult(agent);
    }

    #region IAgentFactory Implementation

    /// <inheritdoc />
    public AgentType AgentType => AgentType.KukaIiwa14;

    /// <inheritdoc />
    public Task<List<IRuntimeAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default)
    {
        // For KukaAgentFactory, we need a configuration file path which is not provided in this interface
        // Return empty list as default behavior
        // Real loading should be done via CreateFromJsonFileAsync with specific path
        _logger.LogLoadAgentsWithoutFilePath();
        return Task.FromResult(new List<IRuntimeAgent>());
    }

    /// <inheritdoc />
    public async Task<IRuntimeAgent> CreateAgentAsync(object configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration is not KukaIiwa14AgentConfig kukaConfig)
            throw new ArgumentException(
                $"Invalid configuration type. Expected {nameof(KukaIiwa14AgentConfig)}, got {configuration.GetType().Name}",
                nameof(configuration));

        // Load providers to resolve skill definitions
        var positionTags = await _sceneEntityProvider.GetPositionTagsAsync();
        var sceneObjects = await _sceneEntityProvider.GetSceneObjectsAsync();
        var skillDefinitions = await _skillDefinitionProvider.GetSkillDefinitionsAsync();

        return await CreateKukaAgentAsync(kukaConfig, skillDefinitions, positionTags, sceneObjects);
    }

    #endregion
}