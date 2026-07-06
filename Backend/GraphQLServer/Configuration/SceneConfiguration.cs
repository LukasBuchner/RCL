using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Configuration;

/// <summary>
///     Configuration model for scene entities (position tags and scene objects).
/// </summary>
public class SceneConfiguration
{
    /// <summary>
    ///     Position tag definitions for the scene.
    /// </summary>
    public List<PositionTag> PositionTags { get; set; } = [];

    /// <summary>
    ///     Scene object definitions.
    /// </summary>
    public List<SceneObject> SceneObjects { get; set; } = [];
}

/// <summary>
///     Configuration options for scene initialization.
/// </summary>
public class SceneOptions
{
    /// <summary>
    ///     Configuration section name.
    /// </summary>
    public const string SectionName = "Scene";

    /// <summary>
    ///     Whether to automatically load scene entities on startup.
    /// </summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    ///     Path to the scene configuration file.
    /// </summary>
    public string? ConfigurationFile { get; set; }
}