using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     In-memory <see cref="IProcedureRepository" /> used by the reactive-execution convergence
///     benchmark to feed a single, pre-built procedure (with its nodes and edges) into the
///     execution runtime without touching Postgres, GraphQL, or any I/O.
/// </summary>
/// <remarks>
///     <para>
///         The procedure-load path of <c>ExecutionInitializer</c> touches only three members:
///         <see cref="GetByIdAsync" /> (the procedure aggregate), <see cref="GetNodesByProcedureIdAsync" />,
///         and <see cref="GetEdgesByProcedureIdAsync" />. Those three are backed by the
///         <see cref="Procedure" />, <see cref="Node" /> list, and <see cref="DependencyEdge" /> list set
///         through <see cref="SetProcedure" />.
///     </para>
///     <para>
///         Every other repository member throws <see cref="NotSupportedException" />: the benchmark never
///         creates, updates, or deletes entities through the repository, and a fake implementation that
///         silently returned empty results would mask a real change in the load path rather than surface it.
///     </para>
///     <para>
///         This type is single-procedure and not thread-safe; each benchmark iteration builds a fresh
///         instance via <c>[IterationSetup]</c>, so no concurrent access occurs.
///     </para>
/// </remarks>
public sealed class InMemoryProcedureRepository : IProcedureRepository
{
    private Procedure? _procedure;
    private List<Node> _nodes = [];
    private List<DependencyEdge> _edges = [];

    /// <summary>
    ///     Sets the procedure aggregate served by this repository, replacing any previously set one.
    /// </summary>
    /// <param name="procedure">The procedure aggregate root to serve from <see cref="GetByIdAsync" />.</param>
    /// <param name="nodes">The nodes belonging to the procedure, served from <see cref="GetNodesByProcedureIdAsync" />.</param>
    /// <param name="edges">
    ///     The dependency edges belonging to the procedure, served from <see cref="GetEdgesByProcedureIdAsync" />.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public void SetProcedure(Procedure procedure, IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        _procedure = procedure;
        _nodes = nodes.ToList();
        _edges = edges.ToList();
    }

    // --- Procedure aggregate (IRepository<Procedure>) ---

    /// <summary>
    ///     Retrieves the configured procedure when its identifier matches the requested one; otherwise null.
    /// </summary>
    /// <param name="id">The identifier of the procedure to retrieve.</param>
    /// <returns>The configured <see cref="Procedure" /> when its id matches; otherwise null.</returns>
    public Task<Procedure?> GetByIdAsync(Guid id)
    {
        var result = _procedure is not null && _procedure.Id == id ? _procedure : null;
        return Task.FromResult(result);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<Procedure>> GetAllAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<Procedure>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<Procedure> CreateAsync(Procedure entity)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Replaces the configured procedure aggregate when its identifier matches the stored one. The
    ///     real load path calls this through <c>ProcedureOrchestrator.LoadProcedureAsync</c> to mark the
    ///     procedure <see cref="Procedure.IsLoaded" />; the stored nodes and edges are held separately and
    ///     are unaffected.
    /// </summary>
    /// <param name="entity">The updated procedure aggregate to store.</param>
    /// <returns>True when the stored procedure was replaced; false when no matching procedure is held.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity" /> is null.</exception>
    public Task<bool> UpdateAsync(Procedure entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_procedure is null || _procedure.Id != entity.Id)
            return Task.FromResult(false);

        _procedure = entity;
        return Task.FromResult(true);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> UpdateMultipleAsync(IReadOnlyList<Procedure> entities)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> DeleteAsync(Guid id)
    {
        throw new NotSupportedException();
    }

    // --- Node operations ---

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<Node>> GetAllNodesAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Retrieves the nodes of the configured procedure when its identifier matches the requested one;
    ///     otherwise an empty list.
    /// </summary>
    /// <param name="procedureId">The identifier of the procedure whose nodes are requested.</param>
    /// <returns>The configured node list when the procedure id matches; otherwise an empty list.</returns>
    public Task<List<Node>> GetNodesByProcedureIdAsync(Guid procedureId)
    {
        var result = _procedure is not null && _procedure.Id == procedureId ? _nodes.ToList() : [];
        return Task.FromResult(result);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<Node?> GetNodeByIdAsync(Guid id)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<Node>> GetNodesByIdsAsync(IReadOnlyList<Guid> ids)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<Node> CreateNodeAsync(Node node)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> UpdateNodeAsync(Node node)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> UpdateMultipleNodesAsync(IReadOnlyList<Node> nodes)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> DeleteNodeAsync(Guid id)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> DeleteNodesByProcedureIdAsync(Guid procedureId)
    {
        throw new NotSupportedException();
    }

    // --- Edge operations ---

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<DependencyEdge>> GetAllEdgesAsync()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    ///     Retrieves the dependency edges of the configured procedure when its identifier matches the
    ///     requested one; otherwise an empty list.
    /// </summary>
    /// <param name="procedureId">The identifier of the procedure whose edges are requested.</param>
    /// <returns>The configured edge list when the procedure id matches; otherwise an empty list.</returns>
    public Task<List<DependencyEdge>> GetEdgesByProcedureIdAsync(Guid procedureId)
    {
        var result = _procedure is not null && _procedure.Id == procedureId ? _edges.ToList() : [];
        return Task.FromResult(result);
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<DependencyEdge?> GetEdgeByIdAsync(Guid id)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<List<DependencyEdge>> GetEdgesByIdsAsync(IReadOnlyList<Guid> ids)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<DependencyEdge> CreateEdgeAsync(DependencyEdge edge)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> UpdateEdgeAsync(DependencyEdge edge)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> UpdateMultipleEdgesAsync(IReadOnlyList<DependencyEdge> edges)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> DeleteEdgeAsync(Guid id)
    {
        throw new NotSupportedException();
    }

    /// <summary>This member is not supported by the benchmark fake.</summary>
    /// <exception cref="NotSupportedException">Always thrown; the benchmark never calls this member.</exception>
    public Task<bool> DeleteEdgesByProcedureIdAsync(Guid procedureId)
    {
        throw new NotSupportedException();
    }
}