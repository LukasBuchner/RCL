using FHOOE.Freydis.Infrastructure.Persistence.Caching;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for caching operations.
///     Covers <see cref="CachedRepository{T}" /> cache hit/miss/update/invalidation events and
///     <see cref="MemoryCacheInvalidationService" /> invalidation operations.
/// </summary>
public static partial class CacheLogger
{
    // ──────────────────────────────────────────────────
    //  CachedRepository — Cache hit/miss
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a cache hit for a GetAll operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Trace,
        Message = "Cache hit for GetAll operation of {EntityType}")]
    public static partial void LogGetAllCacheHit(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs a cache miss for a GetAll operation, indicating a database fetch will occur.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Trace,
        Message = "Cache miss for GetAll operation of {EntityType}, fetching from database")]
    public static partial void LogGetAllCacheMiss(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that entities were cached after a GetAll operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities cached.</param>
    /// <param name="entityType">The entity type that was cached.</param>
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Trace,
        Message = "Cached {Count} entities for GetAll operation of {EntityType}")]
    public static partial void LogGetAllCached(this ILogger logger, int count, string entityType);

    /// <summary>
    ///     Logs a cache hit for a GetById operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="id">The entity identifier.</param>
    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Trace,
        Message = "Cache hit for GetById operation of {EntityType} with ID {Id}")]
    public static partial void LogGetByIdCacheHit(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs a cache miss for a GetById operation, indicating a database fetch will occur.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="id">The entity identifier.</param>
    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Trace,
        Message = "Cache miss for GetById operation of {EntityType} with ID {Id}, fetching from database")]
    public static partial void LogGetByIdCacheMiss(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that a single entity was cached after a GetById operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was cached.</param>
    /// <param name="id">The entity identifier.</param>
    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Trace,
        Message = "Cached entity {EntityType} with ID {Id}")]
    public static partial void LogEntityCached(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that a null result was cached for a GetById operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that returned null.</param>
    /// <param name="id">The entity identifier that was not found.</param>
    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Trace,
        Message = "Cached null result for {EntityType} with ID {Id}")]
    public static partial void LogNullResultCached(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs a cache hit for a GetByIds operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="count">The number of IDs in the query.</param>
    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Trace,
        Message = "Cache hit for GetByIds operation of {EntityType} with {Count} IDs")]
    public static partial void LogGetByIdsCacheHit(this ILogger logger, string entityType, int count);

    /// <summary>
    ///     Logs a cache miss for a GetByIds operation, indicating a database fetch will occur.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="count">The number of IDs in the query.</param>
    [LoggerMessage(
        EventId = 3009,
        Level = LogLevel.Trace,
        Message = "Cache miss for GetByIds operation of {EntityType} with {Count} IDs, fetching from database")]
    public static partial void LogGetByIdsCacheMiss(this ILogger logger, string entityType, int count);

    /// <summary>
    ///     Logs that entities were cached after a GetByIds operation.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities cached.</param>
    /// <param name="entityType">The entity type that was cached.</param>
    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Trace,
        Message = "Cached {Count} entities for GetByIds operation of {EntityType}")]
    public static partial void LogGetByIdsCached(this ILogger logger, int count, string entityType);

    // ──────────────────────────────────────────────────
    //  CachedRepository — Create/Update/Delete cache management
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a new entity is being created.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being created.</param>
    [LoggerMessage(
        EventId = 3011,
        Level = LogLevel.Trace,
        Message = "Creating new {EntityType} entity")]
    public static partial void LogCreatingEntity(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that the GetAllAsync cache was updated by adding a new entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was added.</param>
    /// <param name="id">The identifier of the added entity.</param>
    [LoggerMessage(
        EventId = 3012,
        Level = LogLevel.Trace,
        Message = "Updated GetAllAsync cache by adding {EntityType} with ID {Id}")]
    public static partial void LogGetAllCacheAddedEntity(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that the GetAllAsync cache is not present, so cache update was skipped.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type for which cache was not present.</param>
    [LoggerMessage(
        EventId = 3013,
        Level = LogLevel.Trace,
        Message = "GetAllAsync cache not present, skipping cache update for {EntityType}")]
    public static partial void LogGetAllCacheNotPresent(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that an entity was successfully created.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was created.</param>
    [LoggerMessage(
        EventId = 3014,
        Level = LogLevel.Trace,
        Message = "Successfully created {EntityType} entity")]
    public static partial void LogEntityCreated(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that an entity is being updated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being updated.</param>
    [LoggerMessage(
        EventId = 3015,
        Level = LogLevel.Trace,
        Message = "Updating {EntityType} entity")]
    public static partial void LogUpdatingEntity(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that the GetAllAsync cache was updated by replacing an entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was replaced.</param>
    /// <param name="id">The identifier of the replaced entity.</param>
    [LoggerMessage(
        EventId = 3016,
        Level = LogLevel.Trace,
        Message = "Updated GetAllAsync cache by replacing {EntityType} with ID {Id}")]
    public static partial void LogGetAllCacheReplacedEntity(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that the GetByIdAsync cache was updated for an entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was updated in cache.</param>
    /// <param name="id">The identifier of the entity.</param>
    [LoggerMessage(
        EventId = 3017,
        Level = LogLevel.Trace,
        Message = "Updated GetByIdAsync cache for {EntityType} with ID {Id}")]
    public static partial void LogGetByIdCacheUpdated(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs a warning when an entity update operation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that failed to update.</param>
    [LoggerMessage(
        EventId = 3018,
        Level = LogLevel.Warning,
        Message = "Failed to update {EntityType} entity")]
    public static partial void LogEntityUpdateFailed(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that multiple entities are being updated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities being updated.</param>
    /// <param name="entityType">The entity type being updated.</param>
    [LoggerMessage(
        EventId = 3019,
        Level = LogLevel.Trace,
        Message = "Updating {Count} {EntityType} entities")]
    public static partial void LogUpdatingMultipleEntities(this ILogger logger, int count, string entityType);

    /// <summary>
    ///     Logs that the GetAllAsync cache was updated by replacing multiple entities.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities replaced.</param>
    /// <param name="entityType">The entity type that was replaced.</param>
    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Trace,
        Message = "Updated GetAllAsync cache by replacing {Count} {EntityType} entities")]
    public static partial void LogGetAllCacheReplacedMultiple(this ILogger logger, int count, string entityType);

    /// <summary>
    ///     Logs that multiple entities were successfully updated in cache.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities updated.</param>
    /// <param name="entityType">The entity type that was updated.</param>
    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Trace,
        Message = "Successfully updated {Count} {EntityType} entities in cache")]
    public static partial void LogMultipleEntitiesUpdated(this ILogger logger, int count, string entityType);

    /// <summary>
    ///     Logs a warning when a batch update operation partially or fully fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of entities in the batch.</param>
    /// <param name="entityType">The entity type that failed to update.</param>
    [LoggerMessage(
        EventId = 3022,
        Level = LogLevel.Warning,
        Message = "Failed to update some or all of {Count} {EntityType} entities")]
    public static partial void LogMultipleEntityUpdateFailed(this ILogger logger, int count, string entityType);

    /// <summary>
    ///     Logs that an entity deletion is being attempted.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type being deleted.</param>
    /// <param name="id">The identifier of the entity to delete.</param>
    [LoggerMessage(
        EventId = 3023,
        Level = LogLevel.Trace,
        Message = "Deleting {EntityType} entity with ID {Id}")]
    public static partial void LogDeletingEntity(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that the GetAllAsync cache was updated by removing an entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was removed from cache.</param>
    /// <param name="id">The identifier of the removed entity.</param>
    [LoggerMessage(
        EventId = 3024,
        Level = LogLevel.Trace,
        Message = "Updated GetAllAsync cache by removing {EntityType} with ID {Id}")]
    public static partial void LogGetAllCacheRemovedEntity(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that the GetByIdAsync cache entry was removed for an entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type whose cache entry was removed.</param>
    /// <param name="id">The identifier of the entity.</param>
    [LoggerMessage(
        EventId = 3025,
        Level = LogLevel.Trace,
        Message = "Removed GetByIdAsync cache for {EntityType} with ID {Id}")]
    public static partial void LogGetByIdCacheRemoved(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs that an entity was successfully deleted.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that was deleted.</param>
    /// <param name="id">The identifier of the deleted entity.</param>
    [LoggerMessage(
        EventId = 3026,
        Level = LogLevel.Trace,
        Message = "Successfully deleted {EntityType} entity with ID {Id}")]
    public static partial void LogEntityDeleted(this ILogger logger, string entityType, Guid id);

    /// <summary>
    ///     Logs an error when an entity deletion fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type that failed to delete.</param>
    /// <param name="id">The identifier of the entity.</param>
    [LoggerMessage(
        EventId = 3027,
        Level = LogLevel.Error,
        Message = "Failed to delete {EntityType} entity with ID {Id}")]
    public static partial void LogEntityDeleteFailed(this ILogger logger, string entityType, Guid id);

    // ──────────────────────────────────────────────────
    //  MemoryCacheInvalidationService
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that cache invalidation is starting for a specific entity type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type whose cache is being invalidated.</param>
    /// <param name="prefix">The cache key prefix used for invalidation.</param>
    [LoggerMessage(
        EventId = 3030,
        Level = LogLevel.Debug,
        Message = "Invalidating all cache entries for entity type {EntityType} with prefix {Prefix}")]
    public static partial void LogInvalidatingEntityCache(this ILogger logger, string entityType, string prefix);

    /// <summary>
    ///     Logs that cache invalidation completed successfully for an entity type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="entityType">The entity type whose cache was invalidated.</param>
    [LoggerMessage(
        EventId = 3031,
        Level = LogLevel.Debug,
        Message = "Successfully invalidated cache for entity type {EntityType}")]
    public static partial void LogEntityCacheInvalidated(this ILogger logger, string entityType);

    /// <summary>
    ///     Logs that a specific cache key is being invalidated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The specific cache key being invalidated.</param>
    [LoggerMessage(
        EventId = 3032,
        Level = LogLevel.Trace,
        Message = "Invalidating specific cache key: {CacheKey}")]
    public static partial void LogInvalidatingCacheKey(this ILogger logger, string cacheKey);

    /// <summary>
    ///     Logs that a specific cache key was successfully invalidated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The cache key that was invalidated.</param>
    [LoggerMessage(
        EventId = 3033,
        Level = LogLevel.Trace,
        Message = "Successfully invalidated cache key: {CacheKey}")]
    public static partial void LogCacheKeyInvalidated(this ILogger logger, string cacheKey);

    /// <summary>
    ///     Logs that prefix-based cache invalidation is starting.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="keyPrefix">The key prefix pattern for invalidation.</param>
    [LoggerMessage(
        EventId = 3034,
        Level = LogLevel.Debug,
        Message = "Starting prefix-based cache invalidation for pattern: {KeyPrefix}")]
    public static partial void LogPrefixInvalidationStarted(this ILogger logger, string keyPrefix);

    /// <summary>
    ///     Logs the result of a prefix-based cache invalidation, including the number of entries removed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of cache entries that were invalidated.</param>
    /// <param name="keyPrefix">The key prefix pattern that was used.</param>
    [LoggerMessage(
        EventId = 3035,
        Level = LogLevel.Debug,
        Message = "Invalidated {Count} cache entries with prefix {KeyPrefix}")]
    public static partial void LogPrefixInvalidationCompleted(this ILogger logger, int count, string keyPrefix);
}