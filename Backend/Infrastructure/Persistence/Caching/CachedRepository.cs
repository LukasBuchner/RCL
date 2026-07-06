using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.Caching;

/// <summary>
///     Base cached repository that decorates any IRepository implementation with intelligent caching.
///     Uses the Decorator pattern to add caching functionality without modifying existing repositories.
///     Follows SOLID principles and provides DRY caching logic for all entity types.
/// </summary>
/// <typeparam name="T">The entity type that this repository manages.</typeparam>
public class CachedRepository<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> _baseRepository;
    private readonly MemoryCacheEntryOptions _defaultCacheOptions;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly MemoryCacheInvalidationService? _keyTracker;
    private readonly ILogger<CachedRepository<T>> _logger;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CachedRepository{T}" /> class.
    /// </summary>
    /// <param name="baseRepository">The underlying repository implementation to decorate with caching.</param>
    /// <param name="memoryCache">The memory cache instance for storing cached entities.</param>
    /// <param name="keyGenerator">Service for generating consistent cache keys.</param>
    /// <param name="cacheInvalidationService">Service for managing cache invalidation.</param>
    /// <param name="logger">Logger instance for recording cache operations.</param>
    /// <param name="cacheOptions">Optional cache configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public CachedRepository(
        IRepository<T> baseRepository,
        IMemoryCache memoryCache,
        ICacheKeyGenerator keyGenerator,
        ICacheInvalidationService cacheInvalidationService,
        ILogger<CachedRepository<T>> logger,
        MemoryCacheEntryOptions? cacheOptions = null)
    {
        _baseRepository = baseRepository ?? throw new ArgumentNullException(nameof(baseRepository));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Track keys for invalidation if the service supports it
        _keyTracker = cacheInvalidationService as MemoryCacheInvalidationService;

        // Set default cache options: 15-minute sliding expiration with high priority
        _defaultCacheOptions = cacheOptions ?? new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Priority = CacheItemPriority.High,
            Size = 1 // For memory cache size limiting
        };
    }

    /// <summary>
    ///     Retrieves all entities, using cache when available or populating cache from the database.
    ///     Cache key format: freydis:repository:{entitytype}:all
    /// </summary>
    /// <returns>A list of all entities from cache or database.</returns>
    public async Task<List<T>> GetAllAsync()
    {
        var cacheKey = _keyGenerator.GenerateGetAllKey<T>();

        if (_memoryCache.TryGetValue(cacheKey, out List<T>? cachedEntities))
        {
            _logger.LogGetAllCacheHit(typeof(T).Name);
            return cachedEntities!;
        }

        _logger.LogGetAllCacheMiss(typeof(T).Name);

        var entities = await _baseRepository.GetAllAsync();

        _memoryCache.Set(cacheKey, entities, _defaultCacheOptions);
        _keyTracker?.TrackCacheKey(cacheKey);

        _logger.LogGetAllCached(entities.Count, typeof(T).Name);

        return entities;
    }

    /// <summary>
    ///     Retrieves a single entity by ID, using cache when available or populating cache from the database.
    ///     Cache key format: freydis:repository:{entitytype}:byid:{id}
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    public async Task<T?> GetByIdAsync(Guid id)
    {
        var cacheKey = _keyGenerator.GenerateGetByIdKey<T>(id);

        if (_memoryCache.TryGetValue(cacheKey, out T? cachedEntity))
        {
            _logger.LogGetByIdCacheHit(typeof(T).Name, id);
            return cachedEntity;
        }

        _logger.LogGetByIdCacheMiss(typeof(T).Name, id);

        var entity = await _baseRepository.GetByIdAsync(id);

        if (entity != null)
        {
            _memoryCache.Set(cacheKey, entity, _defaultCacheOptions);
            _keyTracker?.TrackCacheKey(cacheKey);
            _logger.LogEntityCached(typeof(T).Name, id);
        }
        else
        {
            // Cache null results for a shorter time to avoid repeated DB queries
            var nullCacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.Low,
                Size = 1
            };
            _memoryCache.Set(cacheKey, (T?)null, nullCacheOptions);
            _keyTracker?.TrackCacheKey(cacheKey);
            _logger.LogNullResultCached(typeof(T).Name, id);
        }

        return entity;
    }

    /// <summary>
    ///     Retrieves multiple entities by their IDs, using cache when available or populating cache from the database.
    ///     Cache key format: freydis:repository:{entitytype}:byids:{hash}
    /// </summary>
    /// <param name="ids">The collection of unique identifiers to retrieve.</param>
    /// <returns>A list of entities matching the provided IDs.</returns>
    public async Task<List<T>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0) return [];

        var cacheKey = _keyGenerator.GenerateGetByIdsKey<T>(ids);

        if (_memoryCache.TryGetValue(cacheKey, out List<T>? cachedEntities))
        {
            _logger.LogGetByIdsCacheHit(typeof(T).Name, ids.Count);
            return cachedEntities!;
        }

        _logger.LogGetByIdsCacheMiss(typeof(T).Name, ids.Count);

        var entities = await _baseRepository.GetByIdsAsync(ids);

        _memoryCache.Set(cacheKey, entities, _defaultCacheOptions);
        _keyTracker?.TrackCacheKey(cacheKey);

        _logger.LogGetByIdsCached(entities.Count, typeof(T).Name);

        return entities;
    }

    /// <summary>
    ///     Creates a new entity and updates the cache incrementally to maintain consistency without full invalidation.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The created entity.</returns>
    public async Task<T> CreateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogCreatingEntity(typeof(T).Name);

        var createdEntity = await _baseRepository.CreateAsync(entity);

        // Update GetAllAsync cache instead of invalidating
        var getAllKey = _keyGenerator.GenerateGetAllKey<T>();
        if (_memoryCache.TryGetValue(getAllKey, out List<T>? cached) && cached != null)
        {
            var updated = new List<T>(cached) { createdEntity };
            _memoryCache.Set(getAllKey, updated, _defaultCacheOptions);
            var createdId = GetEntityId(createdEntity);
            _logger.LogGetAllCacheAddedEntity(typeof(T).Name, createdId);
        }
        else
        {
            _logger.LogGetAllCacheNotPresent(typeof(T).Name);
        }

        _logger.LogEntityCreated(typeof(T).Name);

        return createdEntity;
    }

    /// <summary>
    ///     Updates an existing entity and updates the cache incrementally to maintain consistency without full invalidation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogUpdatingEntity(typeof(T).Name);

        var result = await _baseRepository.UpdateAsync(entity);

        if (result)
        {
            var entityId = GetEntityId(entity);

            // Update GetAllAsync cache by replacing the entity
            var getAllKey = _keyGenerator.GenerateGetAllKey<T>();
            if (_memoryCache.TryGetValue(getAllKey, out List<T>? cached) && cached != null)
            {
                var updated = cached.Select(e =>
                    GetEntityId(e) == entityId ? entity : e
                ).ToList();
                _memoryCache.Set(getAllKey, updated, _defaultCacheOptions);
                _logger.LogGetAllCacheReplacedEntity(typeof(T).Name, entityId);
            }

            // Update GetByIdAsync cache
            var getByIdKey = _keyGenerator.GenerateGetByIdKey<T>(entityId);
            _memoryCache.Set(getByIdKey, entity, _defaultCacheOptions);
            _keyTracker?.TrackCacheKey(getByIdKey);
            _logger.LogGetByIdCacheUpdated(typeof(T).Name, entityId);
        }
        else
        {
            _logger.LogEntityUpdateFailed(typeof(T).Name);
        }

        return result;
    }

    /// <summary>
    ///     Updates multiple entities and updates the cache incrementally to maintain consistency without full invalidation.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    /// <returns>True if all updates were successful, false otherwise.</returns>
    public async Task<bool> UpdateMultipleAsync(IReadOnlyList<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Count == 0) return true;

        _logger.LogUpdatingMultipleEntities(entities.Count, typeof(T).Name);

        var result = await _baseRepository.UpdateMultipleAsync(entities);

        if (result)
        {
            var entityMap = entities.ToDictionary(GetEntityId);

            // Update GetAllAsync cache by replacing multiple entities
            var getAllKey = _keyGenerator.GenerateGetAllKey<T>();
            if (_memoryCache.TryGetValue(getAllKey, out List<T>? cached) && cached != null)
            {
                var updated = cached.Select(e =>
                {
                    var id = GetEntityId(e);
                    return entityMap.TryGetValue(id, out var updatedEntity) ? updatedEntity : e;
                }).ToList();
                _memoryCache.Set(getAllKey, updated, _defaultCacheOptions);
                _logger.LogGetAllCacheReplacedMultiple(entities.Count, typeof(T).Name);
            }

            // Update individual GetByIdAsync caches
            foreach (var entity in entities)
            {
                var entityId = GetEntityId(entity);
                var getByIdKey = _keyGenerator.GenerateGetByIdKey<T>(entityId);
                _memoryCache.Set(getByIdKey, entity, _defaultCacheOptions);
                _keyTracker?.TrackCacheKey(getByIdKey);
            }

            _logger.LogMultipleEntitiesUpdated(entities.Count, typeof(T).Name);
        }
        else
        {
            _logger.LogMultipleEntityUpdateFailed(entities.Count, typeof(T).Name);
        }

        return result;
    }

    /// <summary>
    ///     Deletes an entity by ID and updates the cache incrementally to maintain consistency without full invalidation.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogDeletingEntity(typeof(T).Name, id);

        var result = await _baseRepository.DeleteAsync(id);

        if (result)
        {
            // Update GetAllAsync cache by removing the entity
            var getAllKey = _keyGenerator.GenerateGetAllKey<T>();
            if (_memoryCache.TryGetValue(getAllKey, out List<T>? cached) && cached != null)
            {
                var updated = cached.Where(e => GetEntityId(e) != id).ToList();
                _memoryCache.Set(getAllKey, updated, _defaultCacheOptions);
                _logger.LogGetAllCacheRemovedEntity(typeof(T).Name, id);
            }

            // Remove GetByIdAsync cache entry
            var getByIdKey = _keyGenerator.GenerateGetByIdKey<T>(id);
            _memoryCache.Remove(getByIdKey);
            _logger.LogGetByIdCacheRemoved(typeof(T).Name, id);

            _logger.LogEntityDeleted(typeof(T).Name, id);
        }
        else
        {
            _logger.LogEntityDeleteFailed(typeof(T).Name, id);
        }

        return result;
    }

    /// <summary>
    ///     Extracts the Id property value from an entity.
    /// </summary>
    /// <param name="entity">The entity to extract the ID from.</param>
    /// <returns>The entity's unique identifier.</returns>
    private static Guid GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException(
                $"Entity type {typeof(T).Name} does not have an 'Id' property");

        var id = idProperty.GetValue(entity);
        if (id is not Guid guidId)
            throw new InvalidOperationException(
                $"Entity type {typeof(T).Name} has an 'Id' property that is not of type Guid");

        return guidId;
    }
}