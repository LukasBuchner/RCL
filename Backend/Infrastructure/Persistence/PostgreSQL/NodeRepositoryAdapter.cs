using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     Adapter that implements <see cref="IRepository{Node}" /> by delegating to
///     <see cref="IProcedureRepository" />. Used for DI compatibility with generic
///     consumers (change trackers, notification services) that depend on <see cref="IRepository{Node}" />.
/// </summary>
public class NodeRepositoryAdapter(IProcedureRepository procedureRepository) : IRepository<Node>
{
    public Task<List<Node>> GetAllAsync()
    {
        return procedureRepository.GetAllNodesAsync();
    }

    public Task<Node?> GetByIdAsync(Guid id)
    {
        return procedureRepository.GetNodeByIdAsync(id);
    }

    public Task<List<Node>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        return procedureRepository.GetNodesByIdsAsync(ids);
    }

    public Task<Node> CreateAsync(Node entity)
    {
        return procedureRepository.CreateNodeAsync(entity);
    }

    public Task<bool> UpdateAsync(Node entity)
    {
        return procedureRepository.UpdateNodeAsync(entity);
    }

    public Task<bool> UpdateMultipleAsync(IReadOnlyList<Node> entities)
    {
        return procedureRepository.UpdateMultipleNodesAsync(entities);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return procedureRepository.DeleteNodeAsync(id);
    }
}