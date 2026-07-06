using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;

/// <summary>
///     Application service for scene object management operations with integrated reactive notifications.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     It integrates reactive notifications directly.
///     All operations that modify scene objects automatically trigger notifications to subscribers.
/// </remarks>
public interface ISceneObjectApplicationService : IDisposable
{
    /// <summary>
    ///     Creates a new scene object in the system.
    /// </summary>
    /// <param name="sceneObject">The scene object entity to create. Must not be null and should have a valid ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the created scene object with any
    ///     server-generated values populated.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the sceneObject parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSceneObjectsChanged observable.
    /// </remarks>
    Task<SceneObject> CreateSceneObjectAsync(SceneObject sceneObject);

    /// <summary>
    ///     Updates an existing scene object with new data.
    /// </summary>
    /// <param name="sceneObject">
    ///     The scene object entity containing updated data. Must not be null and must have an existing
    ///     ID.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the update was successful,
    ///     false if the scene object was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the sceneObject parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSceneObjectsChanged observable
    ///     when successful.
    ///     The entire scene object is replaced in the repository with the provided data.
    /// </remarks>
    Task<bool> UpdateSceneObjectAsync(SceneObject sceneObject);

    /// <summary>
    ///     Deletes a scene object from the system.
    /// </summary>
    /// <param name="sceneObjectId">The unique identifier of the scene object to delete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the scene object was not found.
    /// </returns>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnSceneObjectsChanged observable
    ///     when successful.
    /// </remarks>
    Task<bool> DeleteSceneObjectAsync(Guid sceneObjectId);

    /// <summary>
    ///     Retrieves all scene objects from the system.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of all scene
    ///     objects in the system.
    /// </returns>
    /// <remarks>
    ///     The returned list is read-only to prevent external modifications.
    ///     For large datasets, consider implementing pagination in future versions.
    /// </remarks>
    Task<IReadOnlyList<SceneObject>> GetAllSceneObjectsAsync();

    /// <summary>
    ///     Retrieves a specific scene object by its unique identifier.
    /// </summary>
    /// <param name="sceneObjectId">The unique identifier of the scene object to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the scene object if found, null
    ///     otherwise.
    /// </returns>
    /// <remarks>
    ///     This operation performs a direct lookup by ID and is optimized for performance.
    ///     Returns null if no scene object with the specified ID exists in the system.
    /// </remarks>
    Task<SceneObject?> GetSceneObjectByIdAsync(Guid sceneObjectId);

    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when scene objects have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of scene objects whenever any scene object is created,
    ///     updated, or deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all scene object changes.
    ///     Subscribers will receive the entire scene object collection on each change event.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions or other real-time features.
    ///     Subscribers should handle errors appropriately as the observable may emit errors if notification fails.
    ///     Consider disposing of subscriptions when they are no longer needed to prevent memory leaks.
    /// </remarks>
    IObservable<IReadOnlyList<SceneObject>> OnSceneObjectsChanged();
}