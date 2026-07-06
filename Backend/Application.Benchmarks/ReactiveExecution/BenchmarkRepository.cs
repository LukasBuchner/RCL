using FHOOE.Freydis.Domain;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Inert in-memory <see cref="IRepository{T}" /> used only as a safety net in the benchmark
///     container. The benchmark deliberately excludes the Infrastructure/Postgres layer that supplies
///     the real generic repositories, yet a handful of replicated Application services (for example the
///     entity-management application services) declare an <see cref="IRepository{T}" /> dependency. Any
///     such service that the execution graph happens to activate resolves this no-op instead of failing.
///     The execution path itself never reads persisted entities through these repositories — it loads its
///     procedure through the in-memory <c>IProcedureRepository</c> and resolves agents through the
///     benchmark <c>IAgentManager</c> — so returning empty results here is harmless.
/// </summary>
/// <typeparam name="T">The aggregate type, matching <see cref="IRepository{T}" />'s reference-type constraint.</typeparam>
public sealed class BenchmarkRepository<T> : IRepository<T> where T : class
{
    /// <inheritdoc />
    public Task<List<T>> GetAllAsync()
    {
        return Task.FromResult(new List<T>());
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task<List<T>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        return Task.FromResult(new List<T>());
    }

    /// <inheritdoc />
    public Task<T> CreateAsync(T entity)
    {
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(T entity)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> UpdateMultipleAsync(IReadOnlyList<T> entities)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id)
    {
        return Task.FromResult(true);
    }
}