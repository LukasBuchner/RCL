namespace FHOOE.Freydis.Domain;

/// <summary>
///     Generic repository interface for CRUD operations on domain entities.
///     Provides a standard data access contract that abstracts the underlying persistence mechanism.
/// </summary>
/// <typeparam name="T">The domain entity type. Must be a reference type.</typeparam>
/// <remarks>
///     Implementations may use any persistence strategy (PostgreSQL, in-memory, cached).
///     All operations are asynchronous. Update and delete operations return a boolean
///     indicating success (true) or failure (false, e.g., entity not found).
/// </remarks>
public interface IRepository<T> where T : class
{
    /// <summary>
    ///     Retrieves all entities of type <typeparamref name="T" /> from the repository.
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    ///     Retrieves an entity by its unique identifier, or null if not found.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    ///     Retrieves multiple entities by their identifiers. Returns only the entities that exist.
    /// </summary>
    Task<List<T>> GetByIdsAsync(IReadOnlyList<Guid> ids);

    /// <summary>
    ///     Persists a new entity and returns the created instance.
    /// </summary>
    Task<T> CreateAsync(T entity);

    /// <summary>
    ///     Updates an existing entity. Returns true if the entity was found and updated, false otherwise.
    /// </summary>
    Task<bool> UpdateAsync(T entity);

    /// <summary>
    ///     Updates multiple entities in a single operation. Returns true if all entities were updated successfully.
    /// </summary>
    Task<bool> UpdateMultipleAsync(IReadOnlyList<T> entities);

    /// <summary>
    ///     Deletes an entity by its identifier. Returns true if the entity was found and deleted, false otherwise.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}