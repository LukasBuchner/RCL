using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Services.DataLoaders;

/// <summary>
///     DataLoader for batching Skill entity requests to prevent N+1 query problems.
///     Follows the official HotChocolate v15 pattern with IServiceProvider and proper scoping.
/// </summary>
public class SkillDataLoader : BatchDataLoader<Guid, Skill>
{
    private readonly IServiceProvider _services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillDataLoader" /> class.
    /// </summary>
    /// <param name="services">The service provider for creating scoped services.</param>
    /// <param name="batchScheduler">The batch scheduler for coordinating DataLoader operations.</param>
    /// <param name="options">DataLoader options injected by DI for shared pooled cache objects.</param>
    public SkillDataLoader(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    ///     Loads a batch of skills by their IDs using the existing service infrastructure.
    ///     Uses proper async scope management following the official HotChocolate v15 pattern.
    ///     Includes comprehensive error handling to prevent pipeline failures.
    /// </summary>
    /// <param name="keys">The collection of skill IDs to load.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary mapping skill IDs to their corresponding Skill entities.</returns>
    protected override async Task<IReadOnlyDictionary<Guid, Skill>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var resultDict = new Dictionary<Guid, Skill>();

        if (keys.Count == 0) return resultDict;

        try
        {
            // Create async scope for proper service lifetime management
            await using var scope = _services.CreateAsyncScope();
            var queryService = scope.ServiceProvider.GetRequiredService<ISkillApplicationService>();
            var logger = scope.ServiceProvider.GetService<ILogger<SkillDataLoader>>();

            // Use individual calls for now - can be optimized later with batch methods
            var skillTasks = keys.Select(async key =>
            {
                try
                {
                    var skill = await queryService.GetSkillByIdAsync(key);
                    if (skill != null) return new KeyValuePair<Guid, Skill?>(key, skill);

                    if (logger is not null) logger.LogSkillNotFound(key);
                    return new KeyValuePair<Guid, Skill?>(key, null);
                }
                catch (Exception ex)
                {
                    if (logger is not null) logger.LogSkillLoadFailed(ex, key);
                    return new KeyValuePair<Guid, Skill?>(key, null);
                }
            });

            var results = await Task.WhenAll(skillTasks);

            // Only add non-null skills to the result dictionary
            foreach (var result in results.Where(r => r.Value != null)) resultDict[result.Key] = result.Value!;

            if (logger is not null) logger.LogSkillBatchLoadCompleted(resultDict.Count, keys.Count);
        }
        catch (Exception ex)
        {
            // This is a critical error - log it but don't crash the entire request
            var logger = _services.GetService<IServiceProvider>()?.GetService<ILogger<SkillDataLoader>>();
            if (logger is not null) logger.LogSkillBatchLoadCriticalError(ex, keys.Count);

            // Return empty dictionary rather than crashing the request
            return resultDict;
        }

        return resultDict;
    }
}