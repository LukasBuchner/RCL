using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;

/// <summary>
///     Application service for position tag management operations with integrated reactive notifications.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     It integrates reactive notifications directly.
///     All operations that modify position tags automatically trigger notifications to subscribers.
/// </remarks>
public interface IPositionTagApplicationService : IDisposable
{
    /// <summary>
    ///     Creates a new position tag in the system.
    /// </summary>
    /// <param name="positionTag">The position tag entity to create. Must not be null and should have a valid ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the created position tag with any
    ///     server-generated values populated.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the positionTag parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnPositionTagsChanged observable.
    /// </remarks>
    Task<PositionTag> CreatePositionTagAsync(PositionTag positionTag);

    /// <summary>
    ///     Updates an existing position tag with new data.
    /// </summary>
    /// <param name="positionTag">
    ///     The position tag entity containing updated data. Must not be null and must have an existing
    ///     ID.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the update was successful,
    ///     false if the position tag was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the positionTag parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnPositionTagsChanged observable
    ///     when successful.
    ///     The entire position tag object is replaced in the repository with the provided data.
    /// </remarks>
    Task<bool> UpdatePositionTagAsync(PositionTag positionTag);

    /// <summary>
    ///     Deletes a position tag from the system.
    /// </summary>
    /// <param name="positionTagId">The unique identifier of the position tag to delete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the position tag was not found.
    /// </returns>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnPositionTagsChanged observable
    ///     when successful.
    /// </remarks>
    Task<bool> DeletePositionTagAsync(Guid positionTagId);

    /// <summary>
    ///     Retrieves all position tags from the system.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of all position
    ///     tags in the system.
    /// </returns>
    /// <remarks>
    ///     The returned list is read-only to prevent external modifications.
    ///     For large datasets, consider implementing pagination in future versions.
    /// </remarks>
    Task<IReadOnlyList<PositionTag>> GetAllPositionTagsAsync();

    /// <summary>
    ///     Retrieves a specific position tag by its unique identifier.
    /// </summary>
    /// <param name="positionTagId">The unique identifier of the position tag to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the position tag if found, null
    ///     otherwise.
    /// </returns>
    /// <remarks>
    ///     This operation performs a direct lookup by ID and is optimized for performance.
    ///     Returns null if no position tag with the specified ID exists in the system.
    /// </remarks>
    Task<PositionTag?> GetPositionTagByIdAsync(Guid positionTagId);

    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when position tags have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of position tags whenever any position tag is created,
    ///     updated, or deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all position tag changes.
    ///     Subscribers will receive the entire position tag collection on each change event.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions or other real-time features.
    ///     Subscribers should handle errors appropriately as the observable may emit errors if notification fails.
    ///     Consider disposing of subscriptions when they are no longer needed to prevent memory leaks.
    /// </remarks>
    IObservable<IReadOnlyList<PositionTag>> OnPositionTagsChanged();
}