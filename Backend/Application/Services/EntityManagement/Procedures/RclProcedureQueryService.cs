using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Service for querying a procedure.
/// </summary>
public class RclProcedureQueryService : IRclProcedureQueryService
{
    private readonly IProcedureRepository _procedureRepository;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RclProcedureQueryService" /> class.
    /// </summary>
    /// <param name="procedureRepository">The repository for procedure aggregate entities.</param>
    public RclProcedureQueryService(IProcedureRepository procedureRepository)
    {
        _procedureRepository = procedureRepository ??
                               throw new ArgumentNullException(nameof(procedureRepository));
    }

    #region DependencyEdge Queries

    /// <inheritdoc />
    public async Task<DependencyEdge?> GetDependencyEdgeByIdAsync(Guid id)
    {
        return await _procedureRepository.GetEdgeByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DependencyEdge>> GetAllDependencyEdgesAsync()
    {
        return await _procedureRepository.GetAllEdgesAsync();
    }

    #endregion

    #region Node Queries

    /// <inheritdoc />
    public async Task<Node?> GetNodeByIdAsync(Guid id)
    {
        return await _procedureRepository.GetNodeByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Node>> GetAllNodesAsync()
    {
        return await _procedureRepository.GetAllNodesAsync();
    }

    #endregion
}