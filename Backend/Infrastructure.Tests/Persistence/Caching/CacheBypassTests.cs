using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Infrastructure.Persistence.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.Caching;

/// <summary>
///     Tests that reproduce the cache bypass bug where ProcedureOrchestrator
///     writes through a raw IProcedureRepository while queries read through
///     CachedRepository&lt;Procedure&gt;, causing stale data.
/// </summary>
public class CacheBypassTests
{
    /// <summary>
    ///     Reproduces the exact production bug: a procedure created via the raw
    ///     IProcedureRepository does NOT appear in CachedRepository.GetAllAsync()
    ///     because the cache is never invalidated by the raw write path.
    /// </summary>
    [Fact]
    public async Task CreateViaRawRepository_ThenQueryViaCached_DoesNotReturnNewProcedure()
    {
        // Arrange — simulate the DI setup: raw repo + cached decorator
        var store = new List<Procedure>();
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.CreateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync((Procedure p) =>
            {
                store.Add(p);
                return p;
            });

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache via CachedRepository (simulates first frontend query)
        var initialList = await cachedRepo.GetAllAsync();
        initialList.Should().BeEmpty();

        // Act — create directly via raw repository (simulates ProcedureOrchestrator path)
        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            Name = "New Procedure",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            IsLoaded = false
        };
        await rawRepo.Object.CreateAsync(procedure);

        // Assert — query via CachedRepository returns STALE data (the bug!)
        var afterCreate = await cachedRepo.GetAllAsync();
        // This FAILS because cache still has the empty list — the bug we're fixing
        afterCreate.Should().BeEmpty("cache was never invalidated by the raw write");
    }

    /// <summary>
    ///     Verifies the fix: when creation goes through the CachedRepository,
    ///     the GetAll cache is updated and the new procedure appears immediately.
    /// </summary>
    [Fact]
    public async Task CreateViaCachedRepository_ThenQueryViaCached_ReturnsNewProcedure()
    {
        // Arrange
        var store = new List<Procedure>();
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.CreateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync((Procedure p) =>
            {
                store.Add(p);
                return p;
            });

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache
        var initialList = await cachedRepo.GetAllAsync();
        initialList.Should().BeEmpty();

        // Act — create through CachedRepository (the fix: orchestrator uses this path)
        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            Name = "New Procedure",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            IsLoaded = false
        };
        await cachedRepo.CreateAsync(procedure);

        // Assert — cache is updated, new procedure appears
        var afterCreate = await cachedRepo.GetAllAsync();
        afterCreate.Should().ContainSingle();
        afterCreate[0].Id.Should().Be(procedure.Id);
        afterCreate[0].Name.Should().Be("New Procedure");
    }

    /// <summary>
    ///     Verifies that updates through the raw repository also cause stale cache reads.
    /// </summary>
    [Fact]
    public async Task UpdateViaRawRepository_ThenQueryViaCached_ReturnsStaleData()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = new Procedure
        {
            Id = procedureId,
            Name = "Original Name",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            IsLoaded = false
        };
        var store = new List<Procedure> { procedure };
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);

        rawRepo.Setup(r => r.UpdateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync((Procedure p) =>
            {
                var idx = store.FindIndex(x => x.Id == p.Id);
                if (idx >= 0) store[idx] = p;
                return true;
            });

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache
        var initial = await cachedRepo.GetAllAsync();
        initial.Should().ContainSingle().Which.Name.Should().Be("Original Name");

        // Act — update via raw repository (bypasses cache)
        var updated = procedure with { Name = "Updated Name" };
        await rawRepo.Object.UpdateAsync(updated);

        // Assert — cache still has stale data
        var afterUpdate = await cachedRepo.GetAllAsync();
        afterUpdate.Should().ContainSingle().Which.Name.Should().Be("Original Name",
            "cache was never invalidated by the raw update");
    }

    /// <summary>
    ///     Verifies that deletes through the raw repository also cause stale cache reads.
    /// </summary>
    [Fact]
    public async Task DeleteViaRawRepository_ThenQueryViaCached_StillReturnsDeletedProcedure()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = new Procedure
        {
            Id = procedureId,
            Name = "To Delete",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            IsLoaded = false
        };
        var store = new List<Procedure> { procedure };
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);

        rawRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) =>
            {
                var removed = store.RemoveAll(p => p.Id == id);
                return removed > 0;
            });

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache
        var initial = await cachedRepo.GetAllAsync();
        initial.Should().ContainSingle();

        // Act — delete via raw repository (bypasses cache)
        await rawRepo.Object.DeleteAsync(procedureId);

        // Assert — cache still has the deleted procedure
        var afterDelete = await cachedRepo.GetAllAsync();
        afterDelete.Should().ContainSingle("cache was never invalidated by the raw delete");
    }

    /// <summary>
    ///     Reproduces the ProcedureVariableService bug: updating a procedure's variables
    ///     via the raw repository does not update the cached version.
    /// </summary>
    [Fact]
    public async Task UpdateViaRawRepository_Variables_ThenQueryViaCached_ReturnsStaleVariables()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = new Procedure
        {
            Id = procedureId,
            Name = "Procedure With Variables",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            Variables = new List<VariableDefinition>(),
            IsLoaded = false
        };
        var store = new List<Procedure> { procedure };
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);
        rawRepo.Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(() => store.FirstOrDefault(p => p.Id == procedureId));
        rawRepo.Setup(r => r.UpdateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync((Procedure p) =>
            {
                var idx = store.FindIndex(x => x.Id == p.Id);
                if (idx >= 0) store[idx] = p;
                return true;
            });

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache
        var initial = await cachedRepo.GetAllAsync();
        initial.Should().ContainSingle().Which.Variables.Should().BeEmpty();

        // Act — update via raw repository (simulates old ProcedureVariableService path)
        var withVariable = procedure with
        {
            Variables = new List<VariableDefinition>
            {
                new() { Name = "TestVar", Type = new StringType() }
            }
        };
        await rawRepo.Object.UpdateAsync(withVariable);

        // Assert — cache still has the version without variables
        var afterUpdate = await cachedRepo.GetAllAsync();
        afterUpdate.Should().ContainSingle().Which.Variables.Should().BeEmpty(
            "cache was never invalidated by the raw update");
    }

    /// <summary>
    ///     Verifies the fix: updating via the cached repository updates the cache.
    /// </summary>
    [Fact]
    public async Task UpdateViaCachedRepository_Variables_ThenQueryViaCached_ReturnsUpdatedVariables()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = new Procedure
        {
            Id = procedureId,
            Name = "Procedure With Variables",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            Variables = new List<VariableDefinition>(),
            IsLoaded = false
        };
        var store = new List<Procedure> { procedure };
        var rawRepo = new Mock<IRepository<Procedure>>();

        rawRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => [.. store]);
        rawRepo.Setup(r => r.UpdateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync((Procedure p) =>
            {
                var idx = store.FindIndex(x => x.Id == p.Id);
                if (idx >= 0) store[idx] = p;
                return true;
            });

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        var keyGen = new CacheKeyGenerator();
        var invalidationService = new MemoryCacheInvalidationService(
            cache, keyGen, NullLogger<MemoryCacheInvalidationService>.Instance);

        var cachedRepo = new CachedRepository<Procedure>(
            rawRepo.Object, cache, keyGen, invalidationService,
            NullLogger<CachedRepository<Procedure>>.Instance);

        // Prime the cache
        var initial = await cachedRepo.GetAllAsync();
        initial.Should().ContainSingle().Which.Variables.Should().BeEmpty();

        // Act — update via cached repository (the fix: ProcedureVariableService uses this path)
        var withVariable = procedure with
        {
            Variables = new List<VariableDefinition>
            {
                new() { Name = "TestVar", Type = new StringType() }
            }
        };
        await cachedRepo.UpdateAsync(withVariable);

        // Assert — cache is updated, variable appears
        var afterUpdate = await cachedRepo.GetAllAsync();
        afterUpdate.Should().ContainSingle().Which.Variables.Should().ContainSingle()
            .Which.Name.Should().Be("TestVar");
    }
}