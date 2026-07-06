using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Services.Providers;

/// <summary>
///     Provides access to scene entities (position tags, scene objects) for agent creation.
/// </summary>
public interface ISceneEntityProvider
{
    /// <summary>
    ///     Gets all position tags from the database.
    /// </summary>
    /// <returns>Dictionary of position tags keyed by ID.</returns>
    Task<Dictionary<Guid, PositionTag>> GetPositionTagsAsync();

    /// <summary>
    ///     Gets all scene objects from the database.
    /// </summary>
    /// <returns>Dictionary of scene objects keyed by ID.</returns>
    Task<Dictionary<Guid, SceneObject>> GetSceneObjectsAsync();
}