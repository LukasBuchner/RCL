using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Specialized change tracker for DependencyEdge entities.
/// </summary>
/// <remarks>
///     This interface provides a strongly-typed wrapper around the generic EntityChangeTracker
///     for DependencyEdge entities, making it easier to inject and use in edge-specific services.
/// </remarks>
public interface IDependencyEdgeChangeTracker
{
    /// <summary>
    ///     Gets an observable stream of dependency edge changes.
    /// </summary>
    IObservable<IReadOnlyList<DependencyEdge>> Edges { get; }

    /// <summary>
    ///     Updates the tracker with new dependency edge data.
    /// </summary>
    /// <param name="entities">The updated dependency edges.</param>
    void UpdateEntities(IReadOnlyList<DependencyEdge> entities);

    /// <summary>
    ///     Gets the current snapshot of all dependency edges synchronously.
    /// </summary>
    IReadOnlyList<DependencyEdge> GetCurrentEdges();

    /// <summary>
    ///     Refreshes the tracker data by reloading all entities from the repository.
    /// </summary>
    Task RefreshFromRepositoryAsync();
}