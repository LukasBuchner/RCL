using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Infrastructure.Persistence.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Extensions;

/// <summary>
///     Extension methods for configuring caching services in the dependency injection container.
/// </summary>
public static class CachingServiceExtensions
{
    /// <summary>
    ///     Adds comprehensive caching services to the service collection with intelligent repository decorators.
    /// </summary>
    public static IServiceCollection AddRepositoryCaching(
        this IServiceCollection services,
        Action<MemoryCacheOptions>? memoryCacheOptions = null)
    {
        if (memoryCacheOptions != null)
            services.AddMemoryCache(memoryCacheOptions);
        else
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 10000;
                options.CompactionPercentage = 0.25;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
        services.AddSingleton<ICacheInvalidationService, MemoryCacheInvalidationService>();

        DecorateRegisteredRepositories(services);

        return services;
    }

    private static void DecorateRegisteredRepositories(IServiceCollection services)
    {
        var repositoryDescriptors = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType &&
                                 descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>))
            .ToList();

        foreach (var descriptor in repositoryDescriptors)
        {
            var entityType = descriptor.ServiceType.GetGenericArguments()[0];

            services.Remove(descriptor);

            services.Add(new ServiceDescriptor(
                descriptor.ServiceType,
                provider => CreateGenericCachedRepository(provider, entityType, descriptor),
                descriptor.Lifetime));
        }
    }

    private static object CreateGenericCachedRepository(IServiceProvider provider, Type entityType,
        ServiceDescriptor originalDescriptor)
    {
        var baseRepository = originalDescriptor.ImplementationFactory?.Invoke(provider) ??
                             (originalDescriptor.ImplementationType != null
                                 ? ActivatorUtilities.CreateInstance(provider, originalDescriptor.ImplementationType)
                                 : throw new InvalidOperationException(
                                     $"Cannot create base repository for {entityType.Name}"));

        var cachedRepositoryType = typeof(CachedRepository<>).MakeGenericType(entityType);
        var loggerType = typeof(ILogger<>).MakeGenericType(cachedRepositoryType);

        return ActivatorUtilities.CreateInstance(provider, cachedRepositoryType,
            baseRepository,
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<ICacheKeyGenerator>(),
            provider.GetRequiredService<ICacheInvalidationService>(),
            provider.GetRequiredService(loggerType));
    }
}