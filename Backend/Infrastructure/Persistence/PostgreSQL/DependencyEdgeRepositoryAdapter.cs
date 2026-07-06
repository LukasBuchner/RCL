using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     Adapter that implements <see cref="IRepository{DependencyEdge}" /> by delegating to
///     <see cref="IProcedureRepository" />. Used for DI compatibility with generic
///     consumers (change trackers, notification services) that depend on <see cref="IRepository{DependencyEdge}" />.
/// </summary>
public class DependencyEdgeRepositoryAdapter(IProcedureRepository procedureRepository) : IRepository<DependencyEdge>
{
    public Task<List<DependencyEdge>> GetAllAsync()
    {
        return procedureRepository.GetAllEdgesAsync();
    }

    public Task<DependencyEdge?> GetByIdAsync(Guid id)
    {
        return procedureRepository.GetEdgeByIdAsync(id);
    }

    public Task<List<DependencyEdge>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        return procedureRepository.GetEdgesByIdsAsync(ids);
    }

    public Task<DependencyEdge> CreateAsync(DependencyEdge entity)
    {
        return procedureRepository.CreateEdgeAsync(entity);
    }

    public Task<bool> UpdateAsync(DependencyEdge entity)
    {
        return procedureRepository.UpdateEdgeAsync(entity);
    }

    public Task<bool> UpdateMultipleAsync(IReadOnlyList<DependencyEdge> entities)
    {
        return procedureRepository.UpdateMultipleEdgesAsync(entities);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return procedureRepository.DeleteEdgeAsync(id);
    }
}