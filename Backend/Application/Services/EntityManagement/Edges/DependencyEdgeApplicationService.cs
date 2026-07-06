using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Edges;

/// <summary>
///     Application service for procedure-scoped dependency edge operations with integrated reactive notifications.
///     Ensures all operations are isolated to the currently loaded procedure.
/// </summary>
/// <remarks>
///     This service provides procedure-scoped access to dependency edges, ensuring data isolation
///     between different procedures. All CRUD operations validate that entities belong to the currently
///     loaded procedure. Query operations are automatically filtered to return only edges from the current
///     procedure, and the reactive observable stream is filtered to emit only edges belonging to the
///     current procedure context.
/// </remarks>
public sealed class DependencyEdgeApplicationService : IDependencyEdgeApplicationService
{
    private readonly ICrudSchedulingOrchestrator _crudOrchestrator;
    private readonly IDependencyEdgeChangeTracker _edgeChangeTracker;
    private readonly ILogger<DependencyEdgeApplicationService> _logger;
    private readonly IProcedureContext _procedureContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DependencyEdgeApplicationService" /> class.
    /// </summary>
    /// <param name="crudOrchestrator">The CRUD orchestrator for coordinated operations with scheduling.</param>
    /// <param name="edgeChangeTracker">The change tracker for dependency edge entities.</param>
    /// <param name="procedureContext">The procedure context for validating procedure ownership.</param>
    /// <param name="logger">The logger instance for diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
    public DependencyEdgeApplicationService(
        ICrudSchedulingOrchestrator crudOrchestrator,
        IDependencyEdgeChangeTracker edgeChangeTracker,
        IProcedureContext procedureContext,
        ILogger<DependencyEdgeApplicationService> logger)
    {
        _crudOrchestrator = crudOrchestrator ?? throw new ArgumentNullException(nameof(crudOrchestrator));
        _edgeChangeTracker = edgeChangeTracker ?? throw new ArgumentNullException(nameof(edgeChangeTracker));
        _procedureContext = procedureContext ?? throw new ArgumentNullException(nameof(procedureContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DependencyEdge> CreateDependencyEdgeAsync(DependencyEdge edge)
    {
        _procedureContext.ValidateProcedureOwnership(edge.ProcedureId);
        return await _crudOrchestrator.CreateDependencyEdgeAsync(edge);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDependencyEdgeAsync(DependencyEdge edge)
    {
        _procedureContext.ValidateProcedureOwnership(edge.ProcedureId);
        return await _crudOrchestrator.UpdateDependencyEdgeAsync(edge);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDependencyEdgeAsync(Guid edgeId)
    {
        var edge = _edgeChangeTracker.GetCurrentEdges().FirstOrDefault(e => e.Id == edgeId);
        if (edge is null) return false;

        _procedureContext.ValidateProcedureOwnership(edge.ProcedureId);
        return await _crudOrchestrator.DeleteDependencyEdgeAsync(edgeId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DependencyEdge>> GetAllDependencyEdgesAsync()
    {
        var procedureId = _procedureContext.CurrentProcedureId;

        // If no procedure is loaded, return an empty list (valid at startup)
        if (!procedureId.HasValue)
        {
            _logger.LogGetAll("DependencyEdge", 0);
            return Task.FromResult<IReadOnlyList<DependencyEdge>>(Array.Empty<DependencyEdge>());
        }

        var edges = _edgeChangeTracker.GetCurrentEdges()
            .Where(e => e.ProcedureId == procedureId.Value)
            .ToList();

        _logger.LogGetAll("DependencyEdge", edges.Count);
        return Task.FromResult<IReadOnlyList<DependencyEdge>>(edges.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<DependencyEdge?> GetDependencyEdgeByIdAsync(Guid edgeId)
    {
        _logger.LogGetById("DependencyEdge", edgeId);
        var edge = _edgeChangeTracker.GetCurrentEdges().FirstOrDefault(e => e.Id == edgeId);

        if (edge != null) _procedureContext.ValidateProcedureOwnership(edge.ProcedureId);

        return Task.FromResult(edge);
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<DependencyEdge>> DependencyEdges =>
        _procedureContext.ProcedureChanges.StartWith(_procedureContext.CurrentProcedureId).CombineLatest(
            _edgeChangeTracker.Edges,
            (procedureId, allEdges) => FilterByProcedureId(allEdges, procedureId));

    /// <summary>
    ///     Filters a list of dependency edges to only include those belonging to the specified procedure.
    /// </summary>
    /// <param name="edges">The complete list of dependency edges.</param>
    /// <param name="procedureId">The procedure ID to filter by, or null to return no edges.</param>
    /// <returns>A filtered list containing only edges from the specified procedure.</returns>
    /// <remarks>
    ///     This method is called reactively whenever either the procedure context changes
    ///     or the edge collection changes, ensuring the filtered view stays synchronized.
    /// </remarks>
    private static IReadOnlyList<DependencyEdge> FilterByProcedureId(IReadOnlyList<DependencyEdge> edges,
        Guid? procedureId)
    {
        if (!procedureId.HasValue) return Array.Empty<DependencyEdge>();

        return edges.Where(e => e.ProcedureId == procedureId.Value).ToList();
    }
}