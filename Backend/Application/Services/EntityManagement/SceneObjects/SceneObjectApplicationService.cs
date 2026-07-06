using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;

/// <summary>
///     Application service for scene object operations with direct repository access and integrated reactive
///     notifications.
/// </summary>
/// <remarks>
///     This service integrates reactive notifications using Rx.NET's Subject pattern
///     to provide real-time updates to subscribers whenever scene objects are modified.
/// </remarks>
public sealed class SceneObjectApplicationService : ISceneObjectApplicationService
{
    private readonly ILogger<SceneObjectApplicationService> _logger;
    private readonly IRepository<SceneObject> _sceneObjectRepository;
    private readonly Subject<IReadOnlyList<SceneObject>> _sceneObjectsChangedSubject;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SceneObjectApplicationService" /> class.
    /// </summary>
    /// <param name="sceneObjectRepository">The repository for scene object data persistence operations.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public SceneObjectApplicationService(
        IRepository<SceneObject> sceneObjectRepository,
        ILogger<SceneObjectApplicationService> logger)
    {
        _sceneObjectRepository =
            sceneObjectRepository ?? throw new ArgumentNullException(nameof(sceneObjectRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sceneObjectsChangedSubject = new Subject<IReadOnlyList<SceneObject>>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sceneObjectsChangedSubject.Dispose();
    }

    /// <inheritdoc />
    public async Task<SceneObject> CreateSceneObjectAsync(SceneObject sceneObject)
    {
        ArgumentNullException.ThrowIfNull(sceneObject);

        _logger.LogCreateStart("SceneObject", sceneObject.Id, sceneObject.Name);

        var createdSceneObject = await _sceneObjectRepository.CreateAsync(sceneObject);

        // Notify subscribers with all scene objects
        await NotifySceneObjectsChangedAsync();

        return createdSceneObject;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateSceneObjectAsync(SceneObject sceneObject)
    {
        ArgumentNullException.ThrowIfNull(sceneObject);

        _logger.LogUpdateStart("SceneObject", sceneObject.Id, sceneObject.Name);

        var result = await _sceneObjectRepository.UpdateAsync(sceneObject);

        if (result)
            await NotifySceneObjectsChangedAsync();
        else
            _logger.LogUpdateFailed("SceneObject", sceneObject.Id, sceneObject.Name);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSceneObjectAsync(Guid sceneObjectId)
    {
        _logger.LogDeleteStart("SceneObject", sceneObjectId);

        var result = await _sceneObjectRepository.DeleteAsync(sceneObjectId);

        if (result)
            await NotifySceneObjectsChangedAsync();
        else
            _logger.LogDeleteFailed("SceneObject", sceneObjectId);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SceneObject>> GetAllSceneObjectsAsync()
    {
        var sceneObjects = await _sceneObjectRepository.GetAllAsync();
        _logger.LogGetAll("SceneObject", sceneObjects.Count);
        return sceneObjects.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<SceneObject?> GetSceneObjectByIdAsync(Guid sceneObjectId)
    {
        _logger.LogGetById("SceneObject", sceneObjectId);
        return await _sceneObjectRepository.GetByIdAsync(sceneObjectId);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<SceneObject>> OnSceneObjectsChanged()
    {
        return _sceneObjectsChangedSubject.AsObservable();
    }

    /// <summary>
    ///     Notifies all subscribers about scene object changes by emitting the current state of all scene objects.
    /// </summary>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    /// <remarks>
    ///     This method retrieves all scene objects from the repository and emits them through the reactive subject.
    ///     If an error occurs during notification, it logs the error and propagates it to subscribers via OnError.
    ///     This ensures that subscribers are aware of any issues in the notification pipeline.
    /// </remarks>
    private async Task NotifySceneObjectsChangedAsync()
    {
        try
        {
            var allSceneObjects = await _sceneObjectRepository.GetAllAsync();
            _sceneObjectsChangedSubject.OnNext(allSceneObjects.AsReadOnly());
            _logger.LogNotificationSent("SceneObject", allSceneObjects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogNotificationFailed("SceneObject", ex);
            _sceneObjectsChangedSubject.OnError(ex);
        }
    }
}