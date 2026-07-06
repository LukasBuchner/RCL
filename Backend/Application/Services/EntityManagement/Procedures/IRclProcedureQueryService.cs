using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Service interface for querying RCL (Robot Control Language) related entities.
/// </summary>
public interface IRclProcedureQueryService
{
    #region DependencyEdge Queries

    /// <summary>
    ///     Asynchronously retrieves a <see cref="DependencyEdge" /> by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the dependency edge.</param>
    /// <returns>
    ///     A <see cref="Task{DependencyEdge}" /> representing the asynchronous operation,
    ///     containing the <see cref="DependencyEdge" /> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<DependencyEdge?> GetDependencyEdgeByIdAsync(Guid id);

    /// <summary>
    ///     Asynchronously retrieves all <see cref="DependencyEdge" /> entities.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{IReadOnlyList}" /> representing the asynchronous operation,
    ///     containing a list of all <see cref="DependencyEdge" /> entities.
    /// </returns>
    Task<IReadOnlyList<DependencyEdge>> GetAllDependencyEdgesAsync();

    #endregion

    #region Node Queries

    /// <summary>
    ///     Asynchronously retrieves a <see cref="Node" /> by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the node.</param>
    /// <returns>
    ///     A <see cref="Task{Node}" /> representing the asynchronous operation,
    ///     containing the <see cref="Node" /> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<Node?> GetNodeByIdAsync(Guid id);

    /// <summary>
    ///     Asynchronously retrieves all <see cref="Node" /> entities.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{IReadOnlyList}" /> representing the asynchronous operation,
    ///     containing a list of all <see cref="Node" /> entities.
    /// </returns>
    Task<IReadOnlyList<Node>> GetAllNodesAsync();

    #endregion
}