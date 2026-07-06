using System.Reactive;
using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Agents.Dummy.Services;

/// <summary>
///     Interface for creating dummy agents from various sources.
/// </summary>
public interface IDummyAgentFactory
{
    /// <summary>
    ///     Creates dummy agents from a JSON configuration file.
    /// </summary>
    /// <param name="configurationFilePath">Path to the JSON configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created dummy agents.</returns>
    Task<List<DummyRuntimeAgent>> CreateFromJsonFileAsync(string configurationFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates dummy agents from a JSON configuration string.
    /// </summary>
    /// <param name="jsonConfiguration">JSON configuration string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created dummy agents.</returns>
    Task<List<DummyRuntimeAgent>> CreateFromJsonAsync(string jsonConfiguration,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates dummy agents from a configuration object.
    /// </summary>
    /// <param name="configuration">Configuration object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created dummy agents.</returns>
    Task<List<DummyRuntimeAgent>> CreateFromConfigurationAsync(DummyAgentConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a single dummy agent from configuration.
    /// </summary>
    /// <param name="agentConfig">Agent configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created dummy agent.</returns>
    Task<DummyRuntimeAgent> CreateAgentAsync(DummyAgentConfig agentConfig,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Factory service for creating dummy agents from configuration.
/// </summary>
public class DummyAgentFactory : IDummyAgentFactory, IAgentFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly ILogger<DummyAgentFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISceneEntityProvider _sceneEntityProvider;
    private readonly ISkillDefinitionProvider _skillDefinitionProvider;

    /// <summary>
    ///     Initializes a new instance of the DummyAgentFactory.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="loggerFactory">Logger factory for creating agent loggers.</param>
    /// <param name="sceneEntityProvider">Provider for scene entities from database.</param>
    /// <param name="skillDefinitionProvider">Provider for skill definitions from configuration.</param>
    public DummyAgentFactory(
        ILogger<DummyAgentFactory> logger,
        ILoggerFactory loggerFactory,
        ISceneEntityProvider sceneEntityProvider,
        ISkillDefinitionProvider skillDefinitionProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _sceneEntityProvider = sceneEntityProvider ?? throw new ArgumentNullException(nameof(sceneEntityProvider));
        _skillDefinitionProvider =
            skillDefinitionProvider ?? throw new ArgumentNullException(nameof(skillDefinitionProvider));
    }

    /// <inheritdoc />
    public async Task<List<DummyRuntimeAgent>> CreateFromJsonFileAsync(string configurationFilePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDummyLoadingFromFile(configurationFilePath);

        if (!File.Exists(configurationFilePath))
        {
            _logger.LogDummyConfigurationFileNotFound(configurationFilePath);
            throw new FileNotFoundException($"Configuration file not found: {configurationFilePath}");
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configurationFilePath, cancellationToken);
            return await CreateFromJsonAsync(jsonContent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogConfigurationLoadFailed(ex, configurationFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DummyRuntimeAgent>> CreateFromJsonAsync(string jsonConfiguration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDummyParsingJsonConfiguration();

        try
        {
            var configuration = JsonSerializer.Deserialize<DummyAgentConfiguration>(jsonConfiguration, JsonOptions);
            if (configuration != null) return await CreateFromConfigurationAsync(configuration, cancellationToken);
            _logger.LogDummyConfigurationDeserializedToNull();
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogDummyJsonParsingFailed(ex);
            throw new ArgumentException("Invalid JSON configuration", nameof(jsonConfiguration), ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<DummyRuntimeAgent>> CreateFromConfigurationAsync(DummyAgentConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDummyCreatingAgentsFromConfiguration(configuration.Agents.Count);

        // Load position tags, scene objects, and skill definitions from providers
        _logger.LogLoadingSceneData();
        var positionTags = await _sceneEntityProvider.GetPositionTagsAsync();
        var sceneObjects = await _sceneEntityProvider.GetSceneObjectsAsync();
        var skillDefinitions = await _skillDefinitionProvider.GetSkillDefinitionsAsync();

        _logger.LogSceneDataLoaded(positionTags.Count, sceneObjects.Count, skillDefinitions.Count);

        var agents = new List<DummyRuntimeAgent>();

        foreach (var agentConfig in configuration.Agents)
            try
            {
                var agent = await CreateAgentAsync(agentConfig, skillDefinitions, positionTags, sceneObjects);
                agents.Add(agent);
                _logger.LogAgentCreated(agent.Name, agent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogAgentCreationFailed(ex, agentConfig.Name);
                throw;
            }

        _logger.LogAllAgentsCreated(agents.Count);
        return agents;
    }

    /// <inheritdoc />
    public Task<DummyRuntimeAgent> CreateAgentAsync(DummyAgentConfig agentConfig,
        CancellationToken cancellationToken = default)
    {
        // Backward compatibility: delegate to new method with empty lookups
        return CreateAgentAsync(
            agentConfig,
            new Dictionary<Guid, SkillDefinition>(),
            new Dictionary<Guid, PositionTag>(),
            new Dictionary<Guid, SceneObject>());
    }

    /// <summary>
    ///     Creates a single dummy agent from configuration, resolving skill definitions.
    /// </summary>
    /// <param name="agentConfig">Agent configuration.</param>
    /// <param name="skillDefinitions">Available skill definitions.</param>
    /// <param name="positionTags">Available position tags.</param>
    /// <param name="sceneObjects">Available scene objects.</param>
    /// <returns>Created dummy agent.</returns>
    private Task<DummyRuntimeAgent> CreateAgentAsync(
        DummyAgentConfig agentConfig,
        Dictionary<Guid, SkillDefinition> skillDefinitions,
        Dictionary<Guid, PositionTag> positionTags,
        Dictionary<Guid, SceneObject> sceneObjects)
    {
        _logger.LogCreatingAgent(agentConfig.Name);

        // Generate ID if not provided
        var agentId = agentConfig.Id ?? Guid.NewGuid();

        // Convert skills from configuration to domain entities
        var skills = agentConfig.Skills.Select(skillConfig =>
        {
            Skill skill;

            // Check if this skill references a definition
            if (skillConfig.SkillDefinitionId.HasValue)
            {
                // Resolve skill definition
                if (!skillDefinitions.TryGetValue(skillConfig.SkillDefinitionId.Value, out var definition))
                    throw new InvalidOperationException(
                        $"Skill definition {skillConfig.SkillDefinitionId.Value} not found for agent {agentConfig.Name}");

                skill = definition.ToSkill(positionTags, sceneObjects);
                _logger.LogSkillResolvedFromDefinition(skill.Name, agentConfig.Name);
            }
            else
            {
                // Inline skill definition (backward compatibility)
                skill = skillConfig.ToSkillInline();
                _logger.LogInlineSkillCreated(skill.Name, agentConfig.Name);
            }

            return skill;
        }).ToList();

        // Create loggers for the agents
        var baseAgentLogger = _loggerFactory.CreateLogger<DummyRuntimeAgent>();
        var configurableAgentLogger = _loggerFactory.CreateLogger<ConfigurableDummyRuntimeAgent>();

        // Create a ConfigurableDummyRuntimeAgent to support JSON configuration behavior
        var configurableAgent = new ConfigurableDummyRuntimeAgent(
            agentId,
            agentConfig.Name,
            skills,
            baseAgentLogger,
            configurableAgentLogger,
            agentConfig);

        // Create a wrapper that extends DummyRuntimeAgent but delegates to the configurable agent
        var wrapperAgent = new ConfigurableDummyAgentWrapper(
            agentId,
            agentConfig.Name,
            skills,
            baseAgentLogger,
            configurableAgent);

        _logger.LogConfigurableAgentCreated(agentConfig.Name, skills.Count);

        return Task.FromResult((DummyRuntimeAgent)wrapperAgent);
    }

    #region IAgentFactory Implementation

    /// <inheritdoc />
    public AgentType AgentType => AgentType.Dummy;

    /// <inheritdoc />
    public Task<List<IRuntimeAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default)
    {
        // For DummyAgentFactory, we need a configuration file path which is not provided in this interface
        // Return empty list as default behavior
        // Real loading should be done via CreateFromJsonFileAsync with specific path
        _logger.LogLoadAgentsCalledWithoutFilePath();
        return Task.FromResult(new List<IRuntimeAgent>());
    }

    /// <inheritdoc />
    async Task<IRuntimeAgent> IAgentFactory.CreateAgentAsync(object configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration is not DummyAgentConfig dummyConfig)
            throw new ArgumentException(
                $"Invalid configuration type. Expected {nameof(DummyAgentConfig)}, got {configuration.GetType().Name}",
                nameof(configuration));

        return await CreateAgentAsync(dummyConfig, cancellationToken);
    }

    #endregion
}

/// <summary>
///     Wrapper that extends DummyRuntimeAgent and delegates configurable behavior to ConfigurableDummyRuntimeAgent.
/// </summary>
public class ConfigurableDummyAgentWrapper : DummyRuntimeAgent
{
    private readonly ConfigurableDummyRuntimeAgent _configurableAgent;

    /// <summary>
    ///     Initializes a new configurable dummy agent wrapper.
    /// </summary>
    /// <param name="id">Agent ID.</param>
    /// <param name="name">Agent name.</param>
    /// <param name="availableSkills">Available skills.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configurableAgent">The configurable agent to delegate to.</param>
    public ConfigurableDummyAgentWrapper(
        Guid id,
        string name,
        IEnumerable<Skill> availableSkills,
        ILogger<DummyRuntimeAgent> logger,
        ConfigurableDummyRuntimeAgent configurableAgent) : base(id, name, availableSkills, logger)
    {
        _configurableAgent = configurableAgent;
    }

    /// <inheritdoc />
    public override async Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill,
        CancellationToken cancellationToken = default)
    {
        return await _configurableAgent.GetExecutionEstimateAsync(skill, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        return _configurableAgent.CanExecuteAdaptivelyAsync(skill, cancellationToken);
    }
}

/// <summary>
///     Enhanced DummyAgent that supports configuration-based behavior.
///     Uses composition instead of inheritance to provide configurable behavior.
/// </summary>
public class ConfigurableDummyRuntimeAgent : IRuntimeAgent
{
    private readonly DummyRuntimeAgent _baseRuntimeAgent;
    private readonly ILogger<ConfigurableDummyRuntimeAgent> _logger;
    private readonly Random _random = new();

    /// <summary>
    ///     Initializes a new configurable dummy agent.
    /// </summary>
    /// <param name="id">Agent ID.</param>
    /// <param name="name">Agent name.</param>
    /// <param name="availableSkills">Available skills.</param>
    /// <param name="baseLogger">Logger instance for base agent.</param>
    /// <param name="logger">Logger instance for configurable agent.</param>
    /// <param name="config">Agent configuration.</param>
    public ConfigurableDummyRuntimeAgent(
        Guid id,
        string name,
        IEnumerable<Skill> availableSkills,
        ILogger<DummyRuntimeAgent> baseLogger,
        ILogger<ConfigurableDummyRuntimeAgent> logger,
        DummyAgentConfig config)
    {
        _baseRuntimeAgent = new DummyRuntimeAgent(id, name, availableSkills, baseLogger);
        Configuration = config;
        _logger = logger;
    }

    /// <summary>
    ///     Gets the agent configuration.
    /// </summary>
    public DummyAgentConfig Configuration { get; }

    /// <inheritdoc />
    public Guid Id => _baseRuntimeAgent.Id;

    /// <inheritdoc />
    public string Name => _baseRuntimeAgent.Name;

    /// <inheritdoc />
    public async Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var baseHealth = await _baseRuntimeAgent.GetHealthStatusAsync(cancellationToken);

        // Use configured CPU usage range
        var cpuUsage = _random.NextDouble() * (Configuration.CpuUsage.Max - Configuration.CpuUsage.Min) +
                       Configuration.CpuUsage.Min;

        // Override availability based on configured max concurrent executions
        var isAvailable = baseHealth.ActiveExecutions < Configuration.MaxConcurrentExecutions;

        return baseHealth with
        {
            CpuUsagePercent = cpuUsage,
            IsAvailable = isAvailable,
            StatusMessage = baseHealth.ActiveExecutions > 0
                ? $"Executing {baseHealth.ActiveExecutions}/{Configuration.MaxConcurrentExecutions} skill(s)"
                : "Idle - ready for work",
            AdditionalMetrics =
            new Dictionary<string, object>(baseHealth.AdditionalMetrics ?? new Dictionary<string, object>())
            {
                ["MaxConcurrentExecutions"] = Configuration.MaxConcurrentExecutions,
                ["ConfiguredCpuRange"] = $"{Configuration.CpuUsage.Min:F1}%-{Configuration.CpuUsage.Max:F1}%",
                ["Description"] = Configuration.Description ?? "Configurable dummy agent"
            }
        };
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default)
    {
        return _baseRuntimeAgent.GetAvailableSkillsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill,
        CancellationToken cancellationToken = default)
    {
        _logger.LogGettingExecutionEstimate(skill.Name, skill.Id, Name);

        // Find the skill configuration by matching skill ID against SkillDefinitionId or direct Id
        var skillConfig = Configuration.Skills.FirstOrDefault(s =>
            s.SkillDefinitionId == skill.Id || s.Id == skill.Id);

        if (skillConfig != null)
        {
            _logger.LogSkillConfigFoundById(skill.Name, skill.Id);
        }
        else
        {
            // Fallback: match by name
            skillConfig = Configuration.Skills.FirstOrDefault(s =>
                s.Name != null && s.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));

            if (skillConfig != null)
            {
                _logger.LogSkillConfigFoundByName(skill.Name);
            }
            else
            {
                _logger.LogSkillConfigNotFound(skill.Name, skill.Id, Name);
                return await _baseRuntimeAgent.GetExecutionEstimateAsync(skill, cancellationToken);
            }
        }

        var canAdapt = skillConfig.CanExecuteAdaptively;
        var nominalDuration = skillConfig.NominalDuration;

        double? minAdaptive = null;

        if (canAdapt) minAdaptive = skillConfig.MinAdaptiveDuration ?? nominalDuration * 0.75;

        _logger.LogExecutionEstimateCreated(skill.Name, Name, canAdapt, nominalDuration, minAdaptive);

        return new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = Id,
            CanExecuteAdaptively = canAdapt,
            EstimatedNominalDuration = nominalDuration,
            MinAdaptiveDuration = minAdaptive
        };
    }

    /// <inheritdoc />
    public Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _logger.LogCheckingAdaptiveCapability(skill.Name, skill.Id, Name);

        // Find the skill configuration by matching skill ID against SkillDefinitionId or direct Id
        var skillConfig = Configuration.Skills.FirstOrDefault(s =>
            // Fallback: match by name
            s.SkillDefinitionId == skill.Id || s.Id == skill.Id) ?? Configuration.Skills.FirstOrDefault(s =>
            s.Name != null && s.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));

        var canExecuteAdaptively = skillConfig?.CanExecuteAdaptively ?? false;
        _logger.LogAdaptiveCapabilityResult(skill.Name, Name, canExecuteAdaptively);

        return Task.FromResult(canExecuteAdaptively);
    }

    /// <inheritdoc />
    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(Guid executionId, Skill skillToExecute,
        CancellationToken cancellationToken)
    {
        return _baseRuntimeAgent.ExecuteSkillAsync(executionId, skillToExecute, cancellationToken);
    }

    /// <inheritdoc />
    public IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId,
        Skill skillToExecute,
        double initialTargetDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        CancellationToken cancellationToken)
    {
        return _baseRuntimeAgent.ExecuteSkillAdaptivelyAsync(
            executionId,
            skillToExecute,
            initialTargetDuration,
            plannedFinishTimes,
            finishSignal,
            cancellationToken);
    }
}