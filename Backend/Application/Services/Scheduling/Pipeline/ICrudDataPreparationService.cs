using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Prepares entity collections for scheduling calculations by loading current state
///     and applying pending CRUD mutations (add, replace, or remove).
/// </summary>
public interface ICrudDataPreparationService
{
    /// <summary>
    ///     Prepares scheduling data with a new entity added to the collection.
    /// </summary>
    Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForCreateAsync<T>(T entity) where T : class;

    /// <summary>
    ///     Prepares scheduling data with an existing entity replaced in the collection.
    /// </summary>
    Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForUpdateAsync<T>(T entity) where T : class;

    /// <summary>
    ///     Prepares scheduling data with an entity removed from the collection.
    /// </summary>
    Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForDeleteAsync<T>(Guid entityId) where T : class;

    /// <summary>
    ///     Prepares scheduling data with an entire node tree removed from the collection.
    /// </summary>
    Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForTreeDeleteAsync(Guid nodeId);
}