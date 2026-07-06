using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Services.Providers;

namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Provides skill definitions from the in-memory cache populated during initialization.
/// </summary>
public class SkillDefinitionProvider : ISkillDefinitionProvider
{
    private readonly SkillsInitializationService _skillsInitializationService;

    /// <summary>
    ///     Initializes a new instance of the SkillDefinitionProvider.
    /// </summary>
    /// <param name="skillsInitializationService">Skills initialization service that holds the loaded definitions.</param>
    public SkillDefinitionProvider(SkillsInitializationService skillsInitializationService)
    {
        _skillsInitializationService = skillsInitializationService ??
                                       throw new ArgumentNullException(nameof(skillsInitializationService));
    }

    /// <inheritdoc />
    public Task<Dictionary<Guid, SkillDefinition>> GetSkillDefinitionsAsync()
    {
        // Return a copy of the dictionary to prevent external modification
        var skillDefinitions = new Dictionary<Guid, SkillDefinition>(_skillsInitializationService.SkillDefinitions);
        return Task.FromResult(skillDefinitions);
    }
}