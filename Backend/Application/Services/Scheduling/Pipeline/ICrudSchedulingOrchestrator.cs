using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Orchestrates CRUD operations with integrated scheduling and notifications.
///     Provides a centralized service for managing entity operations with automatic scheduling triggers.
/// </summary>
public interface ICrudSchedulingOrchestrator
{
    /// <summary>
    ///     Observable stream of node changes for real-time notifications.
    /// </summary>
    IObservable<IReadOnlyList<Node>> NodesChanged { get; }

    /// <summary>
    ///     Observable stream of edge changes for real-time notifications.
    /// </summary>
    IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged { get; }

    /// <summary>
    ///     Creates a node and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="node">The node to create.</param>
    /// <returns>The created node.</returns>
    Task<Node> CreateNodeAsync(Node node);

    /// <summary>
    ///     Creates a dependency edge and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="edge">The edge to create.</param>
    /// <returns>The created edge.</returns>
    Task<DependencyEdge> CreateDependencyEdgeAsync(DependencyEdge edge);

    /// <summary>
    ///     Updates a node and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="node">The node to update.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateNodeAsync(Node node);

    /// <summary>
    ///     Updates a dependency edge and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="edge">The edge to update.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateDependencyEdgeAsync(DependencyEdge edge);

    /// <summary>
    ///     Deletes a node and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="nodeId">The ID of the node to delete.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteNodeAsync(Guid nodeId);

    /// <summary>
    ///     Deletes a dependency edge and triggers scheduling with notifications.
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="edgeId">The ID of the edge to delete.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteDependencyEdgeAsync(Guid edgeId);

    /// <summary>
    ///     Deletes a node and all its child nodes (cascading delete).
    ///     Waits for scheduling to complete before returning.
    /// </summary>
    /// <param name="nodeId">The ID of the node to delete with its children.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteNodeTreeAsync(Guid nodeId);
}