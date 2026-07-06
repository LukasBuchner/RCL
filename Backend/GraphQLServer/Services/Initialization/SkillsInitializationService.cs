using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.GraphQLServer.Configuration;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Implementation of skills initialization service.
///     Loads skill definitions from configuration and stores them in memory.
/// </summary>
public class SkillsInitializationService : ISkillsInitializationService
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<SkillsInitializationService> _logger;
    private readonly Dictionary<Guid, SkillDefinition> _skillDefinitions = new();
    private readonly SkillsOptions _skillsOptions;

    /// <summary>
    ///     Initializes a new instance of the SkillsInitializationService.
    /// </summary>
    /// <param name="skillsOptions">Skills configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public SkillsInitializationService(
        IOptions<SkillsOptions> skillsOptions,
        ILogger<SkillsInitializationService> logger)
    {
        _skillsOptions = skillsOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets all loaded skill definitions.
    /// </summary>
    public IReadOnlyDictionary<Guid, SkillDefinition> SkillDefinitions => _skillDefinitions;

    /// <inheritdoc />
    public async Task InitializeSkillsAsync(CancellationToken cancellationToken = default)
    {
        if (!_skillsOptions.AutoLoad)
        {
            _logger.LogSkillsAutoLoadDisabled();
            return;
        }

        if (string.IsNullOrEmpty(_skillsOptions.ConfigurationFile))
        {
            _logger.LogNoSkillsConfigFile();
            return;
        }

        _logger.LogLoadingSkillDefs(_skillsOptions.ConfigurationFile);

        try
        {
            // Load configuration
            var configJson = await File.ReadAllTextAsync(_skillsOptions.ConfigurationFile, cancellationToken);
            var configuration = JsonSerializer.Deserialize<SkillsConfiguration>(configJson,
                CaseInsensitiveJsonOptions);

            if (configuration == null)
                throw new InvalidOperationException("Failed to deserialize skills configuration");

            // Store skill definitions in memory
            _skillDefinitions.Clear();
            if (configuration.SkillDefinitions is { Count: > 0 })
                foreach (var skillDef in configuration.SkillDefinitions)
                {
                    _skillDefinitions[skillDef.Id] = skillDef;
                    _logger.LogLoadedSkillDef(skillDef.Name, skillDef.Id);
                }

            _logger.LogSkillsLoaded(_skillDefinitions.Count);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogSkillsConfigFileNotFound(ex, _skillsOptions.ConfigurationFile);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogSkillsInitFailed(ex);
            throw;
        }
    }
}