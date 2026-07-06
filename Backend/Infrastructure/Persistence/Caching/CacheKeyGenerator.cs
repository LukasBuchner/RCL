using System.Security.Cryptography;
using System.Text;

namespace FHOOE.Freydis.Infrastructure.Persistence.Caching;

/// <summary>
///     Default implementation of cache key generator that creates consistent, hierarchical cache keys.
///     Uses entity type names and operation-specific suffixes to ensure uniqueness and enable pattern-based invalidation.
/// </summary>
public class CacheKeyGenerator : ICacheKeyGenerator
{
    private const string CacheKeyPrefix = "freydis";
    private const string GetAllSuffix = "all";
    private const string GetByIdSuffix = "byid";
    private const string GetByIdsSuffix = "byids";

    /// <summary>
    ///     Generates a cache key for retrieving all entities of a specific type.
    ///     Format: freydis:repository:{entityType}:all
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A standardized cache key for the GetAll operation.</returns>
    public string GenerateGetAllKey<T>() where T : class
    {
        var entityType = GetEntityTypeName<T>();
        return $"{CacheKeyPrefix}:repository:{entityType}:{GetAllSuffix}";
    }

    /// <summary>
    ///     Generates a cache key for retrieving a single entity by its identifier.
    ///     Format: freydis:repository:{entityType}:byid:{id}
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>A standardized cache key for the GetById operation.</returns>
    public string GenerateGetByIdKey<T>(Guid id) where T : class
    {
        var entityType = GetEntityTypeName<T>();
        return $"{CacheKeyPrefix}:repository:{entityType}:{GetByIdSuffix}:{id:N}";
    }

    /// <summary>
    ///     Generates a cache key for retrieving multiple entities by their identifiers.
    ///     Uses a hash of sorted IDs to ensure consistent keys regardless of input order.
    ///     Format: freydis:repository:{entityType}:byids:{hash}
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="ids">The collection of unique identifiers.</param>
    /// <returns>A standardized cache key for the GetByIds operation.</returns>
    public string GenerateGetByIdsKey<T>(IReadOnlyList<Guid> ids) where T : class
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0) return GenerateGetAllKey<T>(); // Empty collection behaves like GetAll

        var entityType = GetEntityTypeName<T>();
        var sortedIds = ids.OrderBy(id => id).Select(id => id.ToString("N"));
        var concatenatedIds = string.Join(",", sortedIds);
        var hash = GenerateHash(concatenatedIds);

        return $"{CacheKeyPrefix}:repository:{entityType}:{GetByIdsSuffix}:{hash}";
    }

    /// <summary>
    ///     Generates a cache key prefix for invalidating all cached entries related to a specific entity type.
    ///     Format: freydis:repository:{entityType}
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A cache key prefix for bulk invalidation operations.</returns>
    public string GenerateEntityPrefix<T>() where T : class
    {
        var entityType = GetEntityTypeName<T>();
        return $"{CacheKeyPrefix}:repository:{entityType}";
    }

    /// <summary>
    ///     Extracts a normalized entity type name for cache key generation.
    ///     Uses the simple type name in lowercase for consistency.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A normalized entity type name.</returns>
    private static string GetEntityTypeName<T>() where T : class
    {
        return typeof(T).Name.ToLowerInvariant();
    }

    /// <summary>
    ///     Generates a SHA256 hash of the input string for creating consistent short identifiers.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A hexadecimal hash string (first 16 characters for brevity).</returns>
    private static string GenerateHash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hash[..16]; // Take first 16 characters for brevity
    }
}