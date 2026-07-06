using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Edges;

/// <summary>
///     Application service for dependency edge management operations with integrated reactive notifications.
///     Provides a simple, focused interface for all dependency edge-related operations.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     This interface follows the same pattern as INodeApplicationService with direct repository access.
///     It integrates reactive notifications directly, eliminating the need for separate event dispatchers.
///     All operations that modify edges automatically trigger notifications to subscribers.
/// </remarks>
public interface IDependencyEdgeApplicationService
{
    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when dependency edges have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of dependency edges whenever any edge is created, updated,
    ///     or deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all dependency edge changes.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions.
    /// </remarks>
    IObservable<IReadOnlyList<DependencyEdge>> DependencyEdges { get; }

    /// <summary>
    ///     Creates a new dependency edge in the system.
    /// </summary>
    /// <param name="edge">
    ///     The dependency edge entity to create. Must not be null and should have valid source and target node
    ///     IDs.
    /// </param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created edge.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the edge parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnDependencyEdgesChanged
    ///     observable.
    /// </remarks>
    Task<DependencyEdge> CreateDependencyEdgeAsync(DependencyEdge edge);

    /// <summary>
    ///     Updates an existing dependency edge with new data.
    /// </summary>
    /// <param name="edge">The dependency edge entity containing updated data. Must not be null and must have an existing ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the update was successful,
    ///     false if the edge was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the edge parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnDependencyEdgesChanged observable
    ///     when successful.
    /// </remarks>
    Task<bool> UpdateDependencyEdgeAsync(DependencyEdge edge);

    /// <summary>
    ///     Deletes a dependency edge from the system.
    /// </summary>
    /// <param name="edgeId">The unique identifier of the dependency edge to delete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the edge was not found.
    /// </returns>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnDependencyEdgesChanged observable
    ///     when successful.
    /// </remarks>
    Task<bool> DeleteDependencyEdgeAsync(Guid edgeId);

    /// <summary>
    ///     Retrieves all dependency edges from the system.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of all dependency
    ///     edges.
    /// </returns>
    Task<IReadOnlyList<DependencyEdge>> GetAllDependencyEdgesAsync();

    /// <summary>
    ///     Retrieves a specific dependency edge by its unique identifier.
    /// </summary>
    /// <param name="edgeId">The unique identifier of the dependency edge to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edge if found, null otherwise.</returns>
    Task<DependencyEdge?> GetDependencyEdgeByIdAsync(Guid edgeId);
}