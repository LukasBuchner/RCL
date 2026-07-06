using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Nodes;

/// <summary>
///     Application service for node management operations with integrated reactive notifications.
///     This service directly interacts with the repository layer and provides real-time updates through reactive streams.
/// </summary>
/// <remarks>
///     It integrates reactive notifications directly.
///     All operations that modify nodes automatically trigger notifications to subscribers.
/// </remarks>
public interface INodeApplicationService
{
    /// <summary>
    ///     Gets an observable sequence that notifies subscribers when nodes have changed in the system.
    /// </summary>
    /// <returns>
    ///     An observable sequence that emits the complete list of nodes whenever any node is created, updated, or
    ///     deleted.
    /// </returns>
    /// <remarks>
    ///     This observable provides real-time notifications for all node changes.
    ///     Subscribers will receive the entire node collection on each change event.
    ///     The observable uses Rx.NET and is suitable for implementing GraphQL subscriptions or other real-time features.
    ///     Subscribers should handle errors appropriately as the observable may emit errors if notification fails.
    ///     Consider disposing of subscriptions when they are no longer needed to prevent memory leaks.
    /// </remarks>
    IObservable<IReadOnlyList<Node>> Nodes { get; }

    /// <summary>
    ///     Creates a new node in the system.
    /// </summary>
    /// <param name="node">The node entity to create. Must not be null and should have a valid ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the created node with any
    ///     server-generated values populated.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the node parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnNodesChanged observable.
    ///     It also initiates a background duration calculation for the affected procedure.
    /// </remarks>
    Task<Node> CreateNodeAsync(Node node);

    /// <summary>
    ///     Updates an existing node with new data.
    /// </summary>
    /// <param name="node">The node entity containing updated data. Must not be null and must have an existing ID.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the update was successful,
    ///     false if the node was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the node parameter is null.</exception>
    /// <remarks>
    ///     This operation automatically triggers a notification to all subscribers via the OnNodesChanged observable when
    ///     successful.
    ///     The entire node object is replaced in the repository with the provided data.
    /// </remarks>
    Task<bool> UpdateNodeAsync(Node node);

    /// <summary>
    ///     Deletes a node and its entire descendant tree from the system.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node to delete. This will also delete all child nodes recursively.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains true if the deletion was
    ///     successful, false if the node was not found.
    /// </returns>
    /// <remarks>
    ///     This operation performs a cascading delete, removing the specified node and all of its descendants.
    ///     It automatically triggers a notification to all subscribers via the OnNodesChanged observable when successful.
    ///     Use with caution as this operation cannot be undone and affects multiple nodes.
    /// </remarks>
    Task<bool> DeleteNodeAsync(Guid nodeId);

    /// <summary>
    ///     Retrieves all nodes from the system.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of all nodes in
    ///     the system.
    /// </returns>
    /// <remarks>
    ///     This operation returns all nodes regardless of their type (TaskNode, SkillExecutionNode, etc.).
    ///     The returned list is read-only to prevent external modifications.
    ///     For large datasets, consider implementing pagination in future versions.
    /// </remarks>
    Task<IReadOnlyList<Node>> GetAllNodesAsync();

    /// <summary>
    ///     Retrieves a specific node by its unique identifier.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the node if found, null otherwise.</returns>
    /// <remarks>
    ///     This operation performs a direct lookup by ID and is optimized for performance.
    ///     Returns null if no node with the specified ID exists in the system.
    /// </remarks>
    Task<Node?> GetNodeByIdAsync(Guid nodeId);

    /// <summary>
    ///     Retrieves all child nodes of a specific parent node.
    /// </summary>
    /// <param name="parentId">The unique identifier of the parent node whose children should be retrieved.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of all child
    ///     nodes.
    /// </returns>
    /// <remarks>
    ///     This operation returns only direct children of the specified parent node.
    ///     Returns an empty list if the parent node has no children or doesn't exist.
    ///     The returned list is read-only to prevent external modifications.
    /// </remarks>
    Task<IReadOnlyList<Node>> GetNodesByParentIdAsync(Guid parentId);
}