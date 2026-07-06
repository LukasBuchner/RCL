using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Orchestrates CRUD operations with parallel scheduling and two-phase notifications.
///     Delegates data preparation, cascade deletion, and notification to extracted services.
/// </summary>
public class CrudSchedulingOrchestrator : ICrudSchedulingOrchestrator
{
    private readonly ICascadeDeletionService _cascadeDeletion;
    private readonly ICrudDataPreparationService _dataPreparation;
    private readonly ILogger<CrudSchedulingOrchestrator> _logger;
    private readonly ICrudNotificationService _notification;
    private readonly IProcedureContext _procedureContext;
    private readonly IProcedureRepository _procedureRepository;
    private readonly ISchedulingResultLogger _resultLogger;
    private readonly ITimingCalculationOrchestrator _timingCalculationOrchestrator;

    public CrudSchedulingOrchestrator(
        IProcedureRepository procedureRepository,
        ICrudDataPreparationService dataPreparation,
        ICascadeDeletionService cascadeDeletion,
        ICrudNotificationService notification,
        ISchedulingResultLogger resultLogger,
        ITimingCalculationOrchestrator timingCalculationOrchestrator,
        IProcedureContext procedureContext,
        ILogger<CrudSchedulingOrchestrator> logger)
    {
        _procedureRepository = procedureRepository;
        _dataPreparation = dataPreparation;
        _cascadeDeletion = cascadeDeletion;
        _notification = notification;
        _resultLogger = resultLogger;
        _timingCalculationOrchestrator = timingCalculationOrchestrator;
        _procedureContext = procedureContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Node>> NodesChanged => _notification.NodesChanged;

    /// <inheritdoc />
    public IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged => _notification.EdgesChanged;

    /// <inheritdoc />
    public async Task<Node> CreateNodeAsync(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return await OrchestrateActionAsync(node.Id,
            () => _procedureRepository.CreateNodeAsync(node),
            () => _dataPreparation.PrepareForCreateAsync(node));
    }

    /// <inheritdoc />
    public async Task<DependencyEdge> CreateDependencyEdgeAsync(DependencyEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return await OrchestrateActionAsync(edge.Id,
            () => _procedureRepository.CreateEdgeAsync(edge),
            () => _dataPreparation.PrepareForCreateAsync(edge));
    }

    /// <inheritdoc />
    public async Task<bool> UpdateNodeAsync(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return await OrchestrateActionAsync(node.Id,
            () => _procedureRepository.UpdateNodeAsync(node),
            () => _dataPreparation.PrepareForUpdateAsync(node));
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDependencyEdgeAsync(DependencyEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        return await OrchestrateActionAsync(edge.Id,
            () => _procedureRepository.UpdateEdgeAsync(edge),
            () => _dataPreparation.PrepareForUpdateAsync(edge));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteNodeAsync(Guid nodeId)
    {
        return await OrchestrateActionAsync(nodeId,
            () => _cascadeDeletion.DeleteSingleNodeAsync(nodeId),
            () => _dataPreparation.PrepareForDeleteAsync<Node>(nodeId));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDependencyEdgeAsync(Guid edgeId)
    {
        return await OrchestrateActionAsync(edgeId,
            () => _procedureRepository.DeleteEdgeAsync(edgeId),
            () => _dataPreparation.PrepareForDeleteAsync<DependencyEdge>(edgeId));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteNodeTreeAsync(Guid nodeId)
    {
        return await OrchestrateActionAsync(nodeId,
            () => _cascadeDeletion.DeleteNodeTreeAsync(nodeId),
            () => _dataPreparation.PrepareForTreeDeleteAsync(nodeId));
    }

    /// <summary>
    ///     Core orchestration: prepare data → run repository + scheduling in parallel → notify.
    /// </summary>
    private async Task<TResult> OrchestrateActionAsync<TResult>(
        Guid entityId,
        Func<Task<TResult>> repositoryAction,
        Func<Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>> dataPreparation)
    {
        _logger.LogOrchestrationStarted(entityId);

        try
        {
            var (nodesForScheduling, edgesForScheduling) = await dataPreparation();

            // Execute repository and scheduling tasks in parallel
            var repositoryTask = repositoryAction();
            var schedulingTask = TriggerSchedulingAsync(nodesForScheduling, edgesForScheduling, entityId);
            await Task.WhenAll(repositoryTask, schedulingTask);

            var scheduleResult = await schedulingTask;

            if (scheduleResult is { Success: true, UpdatedNodes.Count: > 0 })
            {
                if (scheduleResult.NodeSchedules.Count > 0)
                    _resultLogger.LogTimingResults(entityId, scheduleResult.NodeSchedules);

                await _notification.PersistAndNotifyAsync(scheduleResult.UpdatedNodes);
            }
            else
            {
                await _notification.NotifyPersistedStateAsync();
            }

            var result = await repositoryTask;

            if (result is false)
                _logger.LogRepositoryOperationFailed(entityId);
            else
                _logger.LogOrchestrationCompleted(entityId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogOrchestrationFailed(ex, entityId);
            throw;
        }
    }

    private async Task<ScheduleResult?> TriggerSchedulingAsync(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        Guid triggeredByEntityId)
    {
        _logger.LogSchedulingCalculationStarted(triggeredByEntityId);

        try
        {
            var procedureId = _procedureContext.RequireCurrentProcedureId();

            var schedulingRequest = new SchedulingRequest
            {
                ProcedureId = procedureId,
                Nodes = nodes,
                Edges = edges,
                StrictMode = false,
                IncludeDetailedTiming = true,
                PreserveOriginalTaskDurations = true
            };

            _logger.LogSchedulingTriggered(procedureId, nodes.Count, edges.Count);

            var scheduleResult =
                await _timingCalculationOrchestrator.CalculateAsync(schedulingRequest);

            if (!scheduleResult.Success)
                _logger.LogSchedulingPipelineFailed(scheduleResult.ErrorMessage);

            return scheduleResult;
        }
        catch (Exception ex)
        {
            _logger.LogSchedulingCalculationFailed(ex, triggeredByEntityId, ex.Message);
            return null;
        }
    }
}