using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Domain;

/// <summary>
///     Repository interface for managing the Procedure aggregate root,
///     including its child entities (Nodes and DependencyEdges).
/// </summary>
public interface IProcedureRepository : IRepository<Procedure>
{
    // --- Node operations ---

    Task<List<Node>> GetAllNodesAsync();
    Task<List<Node>> GetNodesByProcedureIdAsync(Guid procedureId);
    Task<Node?> GetNodeByIdAsync(Guid id);
    Task<List<Node>> GetNodesByIdsAsync(IReadOnlyList<Guid> ids);
    Task<Node> CreateNodeAsync(Node node);
    Task<bool> UpdateNodeAsync(Node node);
    Task<bool> UpdateMultipleNodesAsync(IReadOnlyList<Node> nodes);
    Task<bool> DeleteNodeAsync(Guid id);
    Task<bool> DeleteNodesByProcedureIdAsync(Guid procedureId);

    // --- Edge operations ---

    Task<List<DependencyEdge>> GetAllEdgesAsync();
    Task<List<DependencyEdge>> GetEdgesByProcedureIdAsync(Guid procedureId);
    Task<DependencyEdge?> GetEdgeByIdAsync(Guid id);
    Task<List<DependencyEdge>> GetEdgesByIdsAsync(IReadOnlyList<Guid> ids);
    Task<DependencyEdge> CreateEdgeAsync(DependencyEdge edge);
    Task<bool> UpdateEdgeAsync(DependencyEdge edge);
    Task<bool> UpdateMultipleEdgesAsync(IReadOnlyList<DependencyEdge> edges);
    Task<bool> DeleteEdgeAsync(Guid id);
    Task<bool> DeleteEdgesByProcedureIdAsync(Guid procedureId);
}