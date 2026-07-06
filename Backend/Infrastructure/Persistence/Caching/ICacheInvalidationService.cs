namespace FHOOE.Freydis.Infrastructure.Persistence.Caching;

/// <summary>
///     Interface for managing cache invalidation strategies across repository operations.
///     Follows the Interface Segregation Principle by focusing on cache invalidation concerns.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    ///     Invalidates all cache entries related to a specific entity type.
    ///     This is typically called after Create, Update, or Delete operations to ensure data consistency.
    /// </summary>
    /// <typeparam name="T">The entity type whose cache entries should be invalidated.</typeparam>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    Task InvalidateEntityCacheAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    ///     Invalidates a specific cache entry by its cache key.
    ///     Useful for targeted invalidation of individual cached items.
    /// </summary>
    /// <param name="cacheKey">The specific cache key to invalidate.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    Task InvalidateSpecificKeyAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates multiple cache entries matching a key prefix pattern.
    ///     Enables efficient bulk invalidation for related cache entries.
    /// </summary>
    /// <param name="keyPrefix">The prefix pattern to match for invalidation.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    Task InvalidateByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
}