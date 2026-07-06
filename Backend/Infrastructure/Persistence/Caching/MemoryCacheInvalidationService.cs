using System.Collections.Concurrent;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.Caching;

/// <summary>
///     Implementation of cache invalidation service using ASP.NET Core's IMemoryCache.
///     Provides efficient invalidation strategies with prefix-based bulk operations.
/// </summary>
public class MemoryCacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheKeyGenerator _keyGenerator;

    // Track cache keys for prefix-based invalidation (fully lock-free)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _keysByPrefix = new();
    private readonly ILogger<MemoryCacheInvalidationService> _logger;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemoryCacheInvalidationService" /> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance to manage.</param>
    /// <param name="keyGenerator">Service for generating consistent cache keys.</param>
    /// <param name="logger">Logger instance for recording invalidation operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public MemoryCacheInvalidationService(
        IMemoryCache memoryCache,
        ICacheKeyGenerator keyGenerator,
        ILogger<MemoryCacheInvalidationService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Invalidates all cache entries related to a specific entity type.
    ///     Uses prefix-based invalidation to efficiently clear all related cached items.
    /// </summary>
    /// <typeparam name="T">The entity type whose cache entries should be invalidated.</typeparam>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    public async Task InvalidateEntityCacheAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entityPrefix = _keyGenerator.GenerateEntityPrefix<T>();
        _logger.LogInvalidatingEntityCache(typeof(T).Name, entityPrefix);

        await InvalidateByPrefixAsync(entityPrefix, cancellationToken);

        _logger.LogEntityCacheInvalidated(typeof(T).Name);
    }

    /// <summary>
    ///     Invalidates a specific cache entry by its cache key.
    ///     Provides targeted invalidation with detailed logging.
    /// </summary>
    /// <param name="cacheKey">The specific cache key to invalidate.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    public Task InvalidateSpecificKeyAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInvalidatingCacheKey(cacheKey);

        _memoryCache.Remove(cacheKey);
        RemoveFromKeyTracking(cacheKey);

        _logger.LogCacheKeyInvalidated(cacheKey);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Invalidates multiple cache entries matching a key prefix pattern.
    ///     Implements efficient bulk invalidation using tracked key collections.
    /// </summary>
    /// <param name="keyPrefix">The prefix pattern to match for invalidation.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous invalidation operation.</returns>
    public Task InvalidateByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogPrefixInvalidationStarted(keyPrefix);

        var keysToRemove = GetKeysWithPrefix(keyPrefix);
        var removedCount = 0;

        foreach (var key in keysToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _memoryCache.Remove(key);
            removedCount++;
        }

        // Clean up tracking for the prefix
        var prefixesToRemove =
            _keysByPrefix.Keys.Where(p => p.StartsWith(keyPrefix, StringComparison.Ordinal)).ToList();
        foreach (var prefix in prefixesToRemove) _keysByPrefix.TryRemove(prefix, out _);

        _logger.LogPrefixInvalidationCompleted(removedCount, keyPrefix);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Tracks a cache key under its prefix for efficient bulk invalidation.
    ///     This method should be called whenever a new cache entry is created.
    /// </summary>
    /// <param name="cacheKey">The cache key to track.</param>
    public void TrackCacheKey(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey)) return;

        // Extract prefix (everything before the last colon)
        var lastColonIndex = cacheKey.LastIndexOf(':');
        if (lastColonIndex == -1) return;

        var prefix = cacheKey[..lastColonIndex];

        var keySet = _keysByPrefix.GetOrAdd(prefix, _ => new ConcurrentDictionary<string, byte>());
        keySet.TryAdd(cacheKey, 0);
    }

    /// <summary>
    ///     Retrieves all tracked cache keys that start with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to search for.</param>
    /// <returns>A collection of cache keys matching the prefix.</returns>
    private IEnumerable<string> GetKeysWithPrefix(string prefix)
    {
        var matchingKeys = new List<string>();

        foreach (var kvp in _keysByPrefix)
            if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                matchingKeys.AddRange(kvp.Value.Keys);

        return matchingKeys.Distinct();
    }

    /// <summary>
    ///     Removes a cache key from tracking when it's invalidated.
    /// </summary>
    /// <param name="cacheKey">The cache key to remove from tracking.</param>
    private void RemoveFromKeyTracking(string cacheKey)
    {
        foreach (var keySet in _keysByPrefix.Values) keySet.TryRemove(cacheKey, out _);
    }
}