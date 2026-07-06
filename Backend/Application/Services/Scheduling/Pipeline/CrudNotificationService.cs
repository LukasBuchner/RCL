using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Implements two-phase notification: immediate with calculated scheduling results,
///     then final with authoritative persisted state from repositories.
///     Also handles bulk persistence with fallback to per-node updates.
/// </summary>
public class CrudNotificationService : ICrudNotificationService
{
    private readonly IDependencyEdgeChangeTracker _edgeChangeTracker;
    private readonly ILogger<CrudNotificationService> _logger;
    private readonly INodeChangeTracker _nodeChangeTracker;
    private readonly IProcedureContext _procedureContext;
    private readonly IProcedureRepository _procedureRepository;

    public CrudNotificationService(
        IProcedureRepository procedureRepository,
        INodeChangeTracker nodeChangeTracker,
        IDependencyEdgeChangeTracker edgeChangeTracker,
        IProcedureContext procedureContext,
        ILogger<CrudNotificationService> logger)
    {
        _procedureRepository = procedureRepository;
        _nodeChangeTracker = nodeChangeTracker;
        _edgeChangeTracker = edgeChangeTracker;
        _procedureContext = procedureContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Node>> NodesChanged => _nodeChangeTracker.Nodes;

    /// <inheritdoc />
    public IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged => _edgeChangeTracker.Edges;

    /// <inheritdoc />
    public async Task PersistAndNotifyAsync(IReadOnlyList<Node> calculatedNodes)
    {
        ArgumentNullException.ThrowIfNull(calculatedNodes);

        // Phase 1: immediate notification with calculated results
        _nodeChangeTracker.UpdateEntities(calculatedNodes);
        _logger.LogCalculatedResultsNotified(calculatedNodes.Count);

        // Persist timing data to repository
        await PersistUpdatedNodesAsync(calculatedNodes);

        // Phase 2: final notification with persisted state
        await NotifyPersistedStateAsync();
    }

    /// <inheritdoc />
    public async Task NotifyPersistedStateAsync()
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodesTask = _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        var edgesTask = _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);
        await Task.WhenAll(nodesTask, edgesTask);

        _nodeChangeTracker.UpdateEntities(await nodesTask);
        _edgeChangeTracker.UpdateEntities(await edgesTask);
    }

    private async Task PersistUpdatedNodesAsync(IReadOnlyList<Node> updatedNodes)
    {
        if (updatedNodes.Count == 0) return;

        _logger.LogNodesPersisting(updatedNodes.Count);

        try
        {
            if (updatedNodes.Count > 1)
                try
                {
                    var bulkSuccess = await _procedureRepository.UpdateMultipleNodesAsync(updatedNodes);
                    if (bulkSuccess) return;
                }
                catch (Exception bulkEx)
                {
                    _logger.LogBulkUpdateFailed(bulkEx);
                }

            var updatedCount = 0;
            foreach (var updatedNode in updatedNodes)
            {
                var updateResult = await _procedureRepository.UpdateNodeAsync(updatedNode);
                if (updateResult) updatedCount++;
            }

            _logger.LogIndividualNodeUpdatesCompleted(updatedCount, updatedNodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogRepositoryNodeUpdateError(ex);
            throw;
        }
    }
}