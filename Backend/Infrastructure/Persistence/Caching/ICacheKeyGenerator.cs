namespace FHOOE.Freydis.Infrastructure.Persistence.Caching;

/// <summary>
///     Interface for generating standardized cache keys for repository operations.
///     Follows the Single Responsibility Principle by focusing solely on key generation logic.
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>
    ///     Generates a cache key for retrieving all entities of a specific type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A standardized cache key for the GetAll operation.</returns>
    string GenerateGetAllKey<T>() where T : class;

    /// <summary>
    ///     Generates a cache key for retrieving a single entity by its identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>A standardized cache key for the GetById operation.</returns>
    string GenerateGetByIdKey<T>(Guid id) where T : class;

    /// <summary>
    ///     Generates a cache key for retrieving multiple entities by their identifiers.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="ids">The collection of unique identifiers.</param>
    /// <returns>A standardized cache key for the GetByIds operation.</returns>
    string GenerateGetByIdsKey<T>(IReadOnlyList<Guid> ids) where T : class;

    /// <summary>
    ///     Generates a cache key prefix for invalidating all cached entries related to a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A cache key prefix for bulk invalidation operations.</returns>
    string GenerateEntityPrefix<T>() where T : class;
}