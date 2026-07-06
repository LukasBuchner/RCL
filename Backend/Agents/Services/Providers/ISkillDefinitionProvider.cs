using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;

namespace FHOOE.Freydis.Agents.Services.Providers;

/// <summary>
///     Provides access to skill definitions for agent creation.
/// </summary>
public interface ISkillDefinitionProvider
{
    /// <summary>
    ///     Gets all skill definitions from the database.
    /// </summary>
    /// <returns>Dictionary of skill definitions keyed by ID.</returns>
    Task<Dictionary<Guid, SkillDefinition>> GetSkillDefinitionsAsync();
}