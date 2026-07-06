using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Specialized change tracker for Node entities.
/// </summary>
/// <remarks>
///     This interface provides a strongly-typed wrapper around the generic EntityChangeTracker
///     for Node entities, making it easier to inject and use in Node-specific services.
/// </remarks>
public interface INodeChangeTracker
{
    /// <summary>
    ///     Gets an observable stream of node changes.
    /// </summary>
    IObservable<IReadOnlyList<Node>> Nodes { get; }

    /// <summary>
    ///     Updates the tracker with new node data.
    /// </summary>
    /// <param name="entities">The updated nodes.</param>
    void UpdateEntities(IReadOnlyList<Node> entities);

    /// <summary>
    ///     Gets the current snapshot of all nodes synchronously.
    /// </summary>
    IReadOnlyList<Node> GetCurrentNodes();

    /// <summary>
    ///     Refreshes the tracker data by reloading all entities from the repository.
    /// </summary>
    Task RefreshFromRepositoryAsync();
}