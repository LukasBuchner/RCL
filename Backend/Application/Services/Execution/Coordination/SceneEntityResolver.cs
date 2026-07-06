using System.Collections.Concurrent;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.Execution.Coordination.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Coordination;

/// <summary>
///     Observable-backed implementation of <see cref="ISceneEntityResolver" />.
///     Subscribes to <see cref="IPositionTagApplicationService.OnPositionTagsChanged" />
///     and <see cref="ISceneObjectApplicationService.OnSceneObjectsChanged" /> to keep
///     two <see cref="ConcurrentDictionary{TKey,TValue}" /> caches always current.
///     On each skill execution, stale PositionTag and SceneObject property values are
///     replaced with the latest data from these caches.
/// </summary>
public sealed class SceneEntityResolver : ISceneEntityResolver, IDisposable
{
    private readonly ConcurrentDictionary<Guid, PositionTag> _positionTags = new();
    private readonly ConcurrentDictionary<Guid, SceneObject> _sceneObjects = new();
    private readonly IDisposable _positionTagSubscription;
    private readonly IDisposable _sceneObjectSubscription;
    private readonly ILogger<SceneEntityResolver> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SceneEntityResolver" /> class.
    ///     Performs an initial cache warm from the application services and subscribes
    ///     to their change observables for continuous updates.
    /// </summary>
    /// <param name="positionTagService">Service providing position tag data and change notifications.</param>
    /// <param name="sceneObjectService">Service providing scene object data and change notifications.</param>
    /// <param name="logger">Logger for diagnostic output when stale data is detected and refreshed.</param>
    public SceneEntityResolver(
        IPositionTagApplicationService positionTagService,
        ISceneObjectApplicationService sceneObjectService,
        ILogger<SceneEntityResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(positionTagService);
        ArgumentNullException.ThrowIfNull(sceneObjectService);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initial cache warm
        WarmPositionTagCache(positionTagService.GetAllPositionTagsAsync().GetAwaiter().GetResult());
        WarmSceneObjectCache(sceneObjectService.GetAllSceneObjectsAsync().GetAwaiter().GetResult());

        // Subscribe to live updates
        _positionTagSubscription = positionTagService.OnPositionTagsChanged()
            .Subscribe(WarmPositionTagCache);
        _sceneObjectSubscription = sceneObjectService.OnSceneObjectsChanged()
            .Subscribe(WarmSceneObjectCache);

        _logger.LogResolverInitialized(_positionTags.Count, _sceneObjects.Count);
    }

    /// <inheritdoc />
    public Skill RefreshSceneEntityProperties(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (skill.Properties.Count == 0)
            return skill;

        var hasEntityProperties = false;
        foreach (var prop in skill.Properties)
            if (prop.Value.Type is PositionTagType or SceneObjectType)
            {
                hasEntityProperties = true;
                break;
            }

        if (!hasEntityProperties)
            return skill;

        var refreshedProperties = new List<TypedProperty>(skill.Properties.Count);
        var anyRefreshed = false;

        foreach (var property in skill.Properties)
        {
            var refreshed = RefreshProperty(property);
            refreshedProperties.Add(refreshed);
            if (!ReferenceEquals(refreshed, property))
                anyRefreshed = true;
        }

        if (!anyRefreshed)
            return skill;

        return skill with { Properties = refreshedProperties };
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        _positionTagSubscription.Dispose();
        _sceneObjectSubscription.Dispose();
    }

    /// <summary>
    ///     Refreshes a single property if it contains a stale PositionTag or SceneObject reference.
    /// </summary>
    /// <param name="property">The property to check and potentially refresh.</param>
    /// <returns>The original property if no refresh is needed, or a new property with the current value.</returns>
    private TypedProperty RefreshProperty(TypedProperty property)
    {
        if (property.Value is { Type: PositionTagType, Value: PositionTag staleTag })
        {
            if (_positionTags.TryGetValue(staleTag.Id, out var currentTag))
            {
                if (staleTag.Position.X != currentTag.Position.X ||
                    staleTag.Position.Y != currentTag.Position.Y ||
                    staleTag.Position.Z != currentTag.Position.Z ||
                    staleTag.Position.Alpha != currentTag.Position.Alpha ||
                    staleTag.Position.Beta != currentTag.Position.Beta ||
                    staleTag.Position.Gamma != currentTag.Position.Gamma)
                {
                    _logger.LogPositionTagRefreshed(
                        currentTag.Tag, currentTag.Id,
                        staleTag.Position.X, staleTag.Position.Y, staleTag.Position.Z,
                        currentTag.Position.X, currentTag.Position.Y, currentTag.Position.Z);

                    return property with { Value = TypedValue.PositionTag(currentTag) };
                }
            }
            else
            {
                _logger.LogPositionTagCacheMiss(
                    staleTag.Id, staleTag.Position.X, staleTag.Position.Y, staleTag.Position.Z);
            }
        }
        else if (property.Value is { Type: SceneObjectType, Value: SceneObject staleObj })
        {
            if (_sceneObjects.TryGetValue(staleObj.Id, out var currentObj))
            {
                if (staleObj.Position.X != currentObj.Position.X ||
                    staleObj.Position.Y != currentObj.Position.Y ||
                    staleObj.Position.Z != currentObj.Position.Z ||
                    staleObj.Position.Alpha != currentObj.Position.Alpha ||
                    staleObj.Position.Beta != currentObj.Position.Beta ||
                    staleObj.Position.Gamma != currentObj.Position.Gamma ||
                    staleObj.Name != currentObj.Name)
                {
                    _logger.LogSceneObjectRefreshed(
                        currentObj.Name, currentObj.Id,
                        staleObj.Position.X, staleObj.Position.Y, staleObj.Position.Z,
                        currentObj.Position.X, currentObj.Position.Y, currentObj.Position.Z);

                    return property with { Value = TypedValue.SceneObject(currentObj) };
                }
            }
            else
            {
                _logger.LogSceneObjectCacheMiss(
                    staleObj.Id, staleObj.Name, staleObj.Position.X, staleObj.Position.Y, staleObj.Position.Z);
            }
        }

        return property;
    }

    /// <summary>
    ///     Replaces the position tag cache contents with the provided list.
    /// </summary>
    /// <param name="tags">The current complete list of position tags.</param>
    private void WarmPositionTagCache(IReadOnlyList<PositionTag> tags)
    {
        _positionTags.Clear();
        foreach (var tag in tags)
            _positionTags[tag.Id] = tag;
    }

    /// <summary>
    ///     Replaces the scene object cache contents with the provided list.
    /// </summary>
    /// <param name="objects">The current complete list of scene objects.</param>
    private void WarmSceneObjectCache(IReadOnlyList<SceneObject> objects)
    {
        _sceneObjects.Clear();
        foreach (var obj in objects)
            _sceneObjects[obj.Id] = obj;
    }
}