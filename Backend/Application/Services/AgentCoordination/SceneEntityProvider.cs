using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.AgentCoordination;

/// <summary>
///     Provides scene entities from the database for agent creation.
/// </summary>
public class SceneEntityProvider : ISceneEntityProvider
{
    private readonly IPositionTagApplicationService _positionTagService;
    private readonly ISceneObjectApplicationService _sceneObjectService;

    /// <summary>
    ///     Initializes a new instance of the SceneEntityProvider.
    /// </summary>
    /// <param name="positionTagService">Position tag application service.</param>
    /// <param name="sceneObjectService">Scene object application service.</param>
    public SceneEntityProvider(
        IPositionTagApplicationService positionTagService,
        ISceneObjectApplicationService sceneObjectService)
    {
        _positionTagService = positionTagService ?? throw new ArgumentNullException(nameof(positionTagService));
        _sceneObjectService = sceneObjectService ?? throw new ArgumentNullException(nameof(sceneObjectService));
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, PositionTag>> GetPositionTagsAsync()
    {
        var tags = await _positionTagService.GetAllPositionTagsAsync();
        return tags.ToDictionary(t => t.Id, t => t);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, SceneObject>> GetSceneObjectsAsync()
    {
        var objects = await _sceneObjectService.GetAllSceneObjectsAsync();
        return objects.ToDictionary(o => o.Id, o => o);
    }
}