using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;

namespace FHOOE.Freydis.GraphQLServer.Configuration;

/// <summary>
///     Configuration model for skill definitions.
/// </summary>
public class SkillsConfiguration
{
    /// <summary>
    ///     Skill definitions.
    /// </summary>
    public List<SkillDefinition> SkillDefinitions { get; set; } = [];
}

/// <summary>
///     Configuration options for skills initialization.
/// </summary>
public class SkillsOptions
{
    /// <summary>
    ///     Configuration section name.
    /// </summary>
    public const string SectionName = "Skills";

    /// <summary>
    ///     Whether to automatically load skill definitions on startup.
    /// </summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    ///     Path to the skills configuration file.
    /// </summary>
    public string? ConfigurationFile { get; set; }
}