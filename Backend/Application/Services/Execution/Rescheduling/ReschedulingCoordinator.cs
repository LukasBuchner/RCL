using System.Diagnostics;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Rescheduling;

/// <summary>
///     Coordinates the re-scheduling of remaining skills based on current execution state.
/// </summary>
/// <remarks>
///     <para>
///         This coordinator acts as a bridge between the execution orchestrator and the scheduling pipeline.
///         It collects execution state, builds progress data, and requests re-scheduling with appropriate context.
///         Separation of this logic allows for better testability and follows the Single Responsibility Principle.
///     </para>
///     <para>
///         Formally verified in Sunstone (Lean 4):
///         - DualLoopConvergence.lean — convergence: execution + rescheduling loop terminates
///         - AdaptiveDurationExtension.lean — adaptive duration changes within bounds preserve feasibility
///     </para>
/// </remarks>
public class ReschedulingCoordinator : IReschedulingCoordinator
{
    private readonly ILogger<ReschedulingCoordinator> _logger;
    private readonly ILogger<PipelineEvents> _pipeline;
    private readonly IExecutionProgressDataBuilder _progressDataBuilder;
    private readonly IExecutionProgressMonitor _progressMonitor;
    private readonly ISkillExecutionStateManager _stateManager;
    private readonly IExecutionTimeCalculator _timeCalculator;
    private readonly TimeProvider _timeProvider;
    private readonly ITimingCalculationOrchestrator _timingOrchestrator;
    private IReadOnlyList<DependencyEdge> _currentEdges = new List<DependencyEdge>();
    private IReadOnlyList<Node> _currentNodes = new List<Node>();
    private DateTimeOffset _executionStartTime;

    // Execution context (initialized by ExecutionOrchestrator)
    private Guid _procedureId;
    private IReadOnlyDictionary<Guid, Guid>? _routerSelections;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReschedulingCoordinator" /> class.
    /// </summary>
    /// <param name="logger">Logger for rescheduling coordination events.</param>
    /// <param name="pipelineLogger">Logger for pipeline execution events.</param>
    /// <param name="timeProvider">Provider for current time values.</param>
    /// <param name="stateManager">Manager for skill execution state tracking.</param>
    /// <param name="progressDataBuilder">Builder for converting execution states into progress data.</param>
    /// <param name="progressMonitor">Monitor used to stamp each emitted result with a snapshot-consistent completeness flag.</param>
    /// <param name="timeCalculator">Calculator for elapsed execution time.</param>
    /// <param name="timingOrchestrator">Orchestrator that performs scheduling calculations, including router branch filtering.</param>
    public ReschedulingCoordinator(
        ILogger<ReschedulingCoordinator> logger,
        ILogger<PipelineEvents> pipelineLogger,
        TimeProvider timeProvider,
        ISkillExecutionStateManager stateManager,
        IExecutionProgressDataBuilder progressDataBuilder,
        IExecutionProgressMonitor progressMonitor,
        IExecutionTimeCalculator timeCalculator,
        ITimingCalculationOrchestrator timingOrchestrator)
    {
        _logger = logger;
        _pipeline = pipelineLogger;
        _timeProvider = timeProvider;
        _stateManager = stateManager;
        _progressDataBuilder = progressDataBuilder;
        _progressMonitor = progressMonitor;
        _timeCalculator = timeCalculator;
        _timingOrchestrator = timingOrchestrator;
    }

    /// <summary>
    ///     Initializes the coordinator with execution context.
    ///     This must be called by the ExecutionOrchestrator before calling RescheduleAsync.
    /// </summary>
    /// <param name="procedureId">The procedure ID</param>
    /// <param name="nodes">The current nodes</param>
    /// <param name="edges">The current edges</param>
    /// <param name="executionStartTime">The execution start time</param>
    public void Initialize(
        Guid procedureId,
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        DateTimeOffset executionStartTime)
    {
        _procedureId = procedureId;
        _currentNodes = nodes;
        _currentEdges = edges;
        _executionStartTime = executionStartTime;
    }

    /// <summary>
    ///     Updates the router selections from execution trigger service.
    ///     This allows filtering of non-selected branches during rescheduling.
    /// </summary>
    /// <param name="routerSelections">Dictionary mapping router node ID to selected target node ID</param>
    public void SetRouterSelections(IReadOnlyDictionary<Guid, Guid>? routerSelections)
    {
        _routerSelections = routerSelections;
    }

    /// <inheritdoc />
    public async Task<ReschedulingResult> RescheduleAsync(RescheduleReason reason,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Calculate current execution time
            var currentTime = _timeCalculator.CalculateElapsedSeconds(_executionStartTime, _timeProvider.GetUtcNow());

            var reasonString = reason.ToString();
            _logger.LogReschedulingStarted(reasonString, currentTime);

            // Log rescheduling request
            var currentTimeInfo = $"CurrentTime={currentTime:F1}s";
            _logger.LogRescheduling(
                "REQUEST",
                reason,
                additionalInfo: currentTimeInfo);

            // Build progress data from state manager
            _pipeline.LogQueryStateManager();
            var allStates = _stateManager.GetAllStates();
            var progressData = _progressDataBuilder.BuildProgressData(allStates, _timeProvider.GetUtcNow());

            // Create scheduling request — orchestrator handles router branch filtering internally
            var schedulingRequest = new SchedulingRequest
            {
                ProcedureId = _procedureId,
                Nodes = _currentNodes,
                Edges = _currentEdges,
                CurrentTime = currentTime,
                StrictMode = false,
                IncludeDetailedTiming = true,
                PreserveOriginalTaskDurations = false,
                ProcedureStartTimeUtc = _executionStartTime.UtcDateTime,
                ExecutionProgressData = progressData,
                RouterSelections = _routerSelections
            };

            // Log computing schedule phase
            var computingInfo = $"Requesting schedule from timing orchestrator with {_currentNodes.Count} nodes";
            _logger.LogRescheduling(
                "COMPUTING_SCHEDULE",
                reason,
                additionalInfo: computingInfo);

            // Request re-scheduling from timing calculation orchestrator
            _pipeline.LogComputingSchedule(_currentNodes.Count);
            var scheduleResult = await _timingOrchestrator.CalculateAsync(schedulingRequest, cancellationToken);

            stopwatch.Stop();

            // Log schedule computation result
            var updatedNodeCount = scheduleResult.UpdatedNodes?.Count ?? 0;
            var elapsedMs = $"{stopwatch.Elapsed.TotalMilliseconds:F0}";
            _pipeline.LogScheduleComputed(updatedNodeCount, elapsedMs);

            if (scheduleResult is { Success: true, UpdatedNodes: not null })
            {
                var currentTimeStr = $"{currentTime:F1}";
                var rescheduledElapsedMs = $"{stopwatch.Elapsed.TotalMilliseconds:F0}";
                _pipeline.LogRescheduled(currentTimeStr, reason, scheduleResult.UpdatedNodes.Count,
                    rescheduledElapsedMs);

                var updatedNodes = scheduleResult.UpdatedNodes.ToList();

                // Apply live router selections to RouterNodes for UI display
                // This ensures the dropdown shows the correct selected branch during execution
                if (_routerSelections is { Count: > 0 })
                    updatedNodes = ApplyRouterSelectionsToNodes(updatedNodes);

                // Log updating nodes phase
                var updatingNodesInfo = $"Updated {updatedNodes.Count} nodes";
                _logger.LogRescheduling(
                    "UPDATING_NODES",
                    reason,
                    updatedNodes.Count,
                    stopwatch.Elapsed.TotalMilliseconds,
                    true,
                    additionalInfo: updatingNodesInfo);

                return new ReschedulingResult
                {
                    Success = true,
                    UpdatedNodes = updatedNodes,
                    ScheduleResult = scheduleResult with { UpdatedNodes = updatedNodes },
                    CurrentTime = currentTime,
                    IsExecutionComplete = _progressMonitor.IsExecutionComplete()
                };
            }

            _logger.LogReschedulingFailed(scheduleResult?.ErrorMessage);

            // Log failure
            var failureErrorMessage = scheduleResult?.ErrorMessage ?? "Unknown error";
            _logger.LogRescheduling(
                "UPDATING_NODES",
                reason,
                0,
                stopwatch.Elapsed.TotalMilliseconds,
                false,
                failureErrorMessage);

            return new ReschedulingResult
            {
                Success = false,
                UpdatedNodes = null,
                ScheduleResult = scheduleResult,
                ErrorMessage = scheduleResult?.ErrorMessage ?? "Unknown error",
                CurrentTime = currentTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogReschedulingException(ex);
            stopwatch.Stop();

            // Log exception
            _logger.LogRescheduling(
                "UPDATING_NODES",
                reason,
                0,
                stopwatch.Elapsed.TotalMilliseconds,
                false,
                ex.Message);

            return new ReschedulingResult
            {
                Success = false,
                UpdatedNodes = null,
                ErrorMessage = $"Exception during re-scheduling: {ex.Message}",
                CurrentTime = 0.0
            };
        }
    }

    /// <summary>
    ///     Updates the current nodes after a successful re-schedule.
    ///     This allows the coordinator to use the latest nodes in subsequent re-schedules.
    /// </summary>
    /// <param name="nodes">The updated nodes</param>
    public void UpdateNodes(IReadOnlyList<Node> nodes)
    {
        _currentNodes = nodes;
    }

    /// <summary>
    ///     Applies live router selections to RouterNodes so the UI displays the correct selected branch.
    ///     This updates SelectedBranchTargetNodeId and SelectedBranchName based on execution-time selections.
    /// </summary>
    /// <param name="nodes">The nodes to update</param>
    /// <returns>Updated nodes with router selections applied</returns>
    private List<Node> ApplyRouterSelectionsToNodes(List<Node> nodes)
    {
        if (_routerSelections == null || _routerSelections.Count == 0)
            return nodes;

        return nodes.Select(node =>
        {
            if (node is not RouterNode routerNode)
                return node;

            if (!_routerSelections.TryGetValue(routerNode.Id, out var selectedTargetId))
                return node;

            // Find the branch name from the branches list
            var selectedBranch = routerNode.RouterTask.Branches
                .FirstOrDefault(b => b.TargetNodeId == selectedTargetId);
            var selectedBranchName = selectedBranch?.Name ?? routerNode.RouterTask.SelectedBranchName;
            if (selectedBranch is null)
                _logger.LogRouterBranchLookupMiss(
                    routerNode.RouterTask.Name, selectedTargetId, routerNode.RouterTask.SelectedBranchName);

            // Only update if the selection has changed
            if (routerNode.RouterTask.SelectedBranchTargetNodeId == selectedTargetId)
                return node;

            var oldTarget = routerNode.RouterTask.SelectedBranchTargetNodeId?.ToString() ?? "null";
            var branchDisplayName = selectedBranchName ?? "Unknown";
            _logger.LogRouterSelectionApplied(
                routerNode.RouterTask.Name,
                oldTarget,
                selectedTargetId,
                branchDisplayName);

            var updatedRouterTask = routerNode.RouterTask with
            {
                SelectedBranchTargetNodeId = selectedTargetId,
                SelectedBranchName = selectedBranchName,
                SelectedAtUtc = DateTime.UtcNow
            };


            return routerNode with { RouterTask = updatedRouterTask };
        }).ToList();
    }
}