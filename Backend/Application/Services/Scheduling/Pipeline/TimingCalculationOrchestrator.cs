using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Orchestrator for timing calculations in the scheduling pipeline.
///     Composes specialized services to handle the complete timing calculation workflow
///     including router branch filtering, node hiding, hierarchy processing, timing, and positioning.
/// </summary>
public class TimingCalculationOrchestrator : ITimingCalculationOrchestrator
{
    private readonly IDurationProviderFactory _durationProviderFactory;
    private readonly ILogger<TimingCalculationOrchestrator> _logger;
    private readonly INodeHidingService _nodeHidingService;
    private readonly INodeHierarchyProcessor _nodeHierarchyProcessor;
    private readonly INodePositioningService _nodePositioningService;
    private readonly IRouterBranchFilterService _routerBranchFilterService;
    private readonly IScheduleResultConverter _scheduleResultConverter;
    private readonly ISchedulingPhaseLogger _schedulingPhaseLogger;
    private readonly ITimingAnalyzer _timingAnalyzer;
    private readonly ITimingCalculationEngine _timingCalculationEngine;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TimingCalculationOrchestrator" /> class.
    /// </summary>
    /// <param name="nodeHierarchyProcessor">Service for processing node hierarchy.</param>
    /// <param name="timingCalculationEngine">Engine for calculating timings.</param>
    /// <param name="scheduleResultConverter">Converter for schedule results.</param>
    /// <param name="durationProviderFactory">Factory for creating duration providers.</param>
    /// <param name="nodePositioningService">Service for positioning nodes.</param>
    /// <param name="timingAnalyzer">Analyzer for timing statistics and critical paths.</param>
    /// <param name="schedulingPhaseLogger">Logger for scheduling phases.</param>
    /// <param name="routerBranchFilterService">Service for filtering nodes based on router branch selections.</param>
    /// <param name="nodeHidingService">Service for applying hidden state to excluded nodes.</param>
    /// <param name="logger">Logger for general orchestration events.</param>
    public TimingCalculationOrchestrator(
        INodeHierarchyProcessor nodeHierarchyProcessor,
        ITimingCalculationEngine timingCalculationEngine,
        IScheduleResultConverter scheduleResultConverter,
        IDurationProviderFactory durationProviderFactory,
        INodePositioningService nodePositioningService,
        ITimingAnalyzer timingAnalyzer,
        ISchedulingPhaseLogger schedulingPhaseLogger,
        IRouterBranchFilterService routerBranchFilterService,
        INodeHidingService nodeHidingService,
        ILogger<TimingCalculationOrchestrator> logger)
    {
        _nodeHierarchyProcessor =
            nodeHierarchyProcessor ?? throw new ArgumentNullException(nameof(nodeHierarchyProcessor));
        _timingCalculationEngine =
            timingCalculationEngine ?? throw new ArgumentNullException(nameof(timingCalculationEngine));
        _scheduleResultConverter =
            scheduleResultConverter ?? throw new ArgumentNullException(nameof(scheduleResultConverter));
        _durationProviderFactory =
            durationProviderFactory ?? throw new ArgumentNullException(nameof(durationProviderFactory));
        _nodePositioningService =
            nodePositioningService ?? throw new ArgumentNullException(nameof(nodePositioningService));
        _timingAnalyzer = timingAnalyzer ?? throw new ArgumentNullException(nameof(timingAnalyzer));
        _schedulingPhaseLogger =
            schedulingPhaseLogger ?? throw new ArgumentNullException(nameof(schedulingPhaseLogger));
        _routerBranchFilterService =
            routerBranchFilterService ?? throw new ArgumentNullException(nameof(routerBranchFilterService));
        _nodeHidingService = nodeHidingService ?? throw new ArgumentNullException(nameof(nodeHidingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Calculates the schedule for the provided scheduling request.
    /// </summary>
    /// <param name="request">The scheduling request containing nodes, edges, and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A schedule result containing the calculated schedule or error information.</returns>
    public async Task<ScheduleResult> CalculateAsync(SchedulingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startTime = DateTime.UtcNow;
        _schedulingPhaseLogger.LogPipelineStart(
            request.ProcedureId,
            request.Nodes.Count,
            request.Edges.Count,
            request.StrictMode,
            request.PreserveOriginalTaskDurations,
            request.IncludeDetailedTiming);

        // Handle empty nodes case
        if (request.Nodes.Count == 0)
        {
            _logger.LogEmptyNodeListWarning(request.ProcedureId);
            return new ScheduleResult
            {
                Success = true,
                NodeSchedules = [],
                ErrorMessage = null,
                UpdatedNodes = []
            };
        }

        try
        {
            var edgesToProcess = request.Edges;

            // Phase 0: Filter nodes based on router branch selections.
            // Execution-time uses explicit RouterSelections; design-time uses ManuallySelectedBranch
            // on each RouterNode. When neither is set, all branches are included.
            var phase0Start = DateTime.UtcNow;
            _schedulingPhaseLogger.LogPhaseStart(0, "Filter Router Branches", request.ProcedureId);

            IReadOnlyList<Node> nodesToProcess;
            IReadOnlyList<Node> excludedNodes;
            IReadOnlyList<Node> allNodesWithHiddenState;

            var hasRouters = request.Nodes.Any(n => n is RouterNode);

            if (hasRouters)
            {
                var filterResult = await _routerBranchFilterService.FilterNodesAsync(
                    request.Nodes, request.RouterSelections);

                nodesToProcess = filterResult.IncludedNodes;
                excludedNodes = filterResult.ExcludedNodes;

                // Always apply hidden state — this both hides excluded nodes and
                // clears stale Hidden=true on nodes that are now included.
                var excludedNodeIds = excludedNodes.Select(n => n.Id).ToList();
                allNodesWithHiddenState =
                    await _nodeHidingService.ApplyHiddenStateAsync(request.Nodes, excludedNodeIds);
            }
            else
            {
                // No routers — skip filtering entirely.
                nodesToProcess = request.Nodes;
                excludedNodes = [];
                allNodesWithHiddenState = request.Nodes;
            }

            var phase0Duration = DateTime.UtcNow - phase0Start;
            _schedulingPhaseLogger.LogPhaseComplete(
                0, "Filter Router Branches", request.ProcedureId, phase0Duration,
                $"Included {nodesToProcess.Count} nodes, excluded {excludedNodes.Count} nodes");

            if (excludedNodes.Count > 0)
                _logger.LogRouterBranchFilterResult(excludedNodes.Count, nodesToProcess.Count);

            // Phase 1: Process node hierarchy
            var phase1Start = DateTime.UtcNow;
            _schedulingPhaseLogger.LogPhaseStart(1, "Process Hierarchy", request.ProcedureId);

            var nodeHierarchy = _nodeHierarchyProcessor.ProcessHierarchy(nodesToProcess);

            var phase1Duration = DateTime.UtcNow - phase1Start;
            _schedulingPhaseLogger.LogPhaseComplete(
                1, "Process Hierarchy", request.ProcedureId, phase1Duration,
                $"Processed {nodeHierarchy.TaskNodes.Count} task nodes, {nodeHierarchy.SkillExecutionNodes.Count} skill nodes");

            // Phase 2: Create duration provider and calculate timings
            var phase2Start = DateTime.UtcNow;
            _schedulingPhaseLogger.LogPhaseStart(2, "Calculate Timings", request.ProcedureId);

            var durationProvider = _durationProviderFactory.CreateDurationProvider(
                request.IsExecutionMode,
                request.ProcedureStartTimeUtc,
                request.ExecutionProgressData);

            var timingOptions = new TimingCalculationOptions
            {
                ProcedureId = request.ProcedureId,
                CurrentTime = request.CurrentTime,
                StrictMode = request.StrictMode,
                PreserveOriginalTaskDurations = request.PreserveOriginalTaskDurations,
                IncludeDetailedTiming = request.IncludeDetailedTiming,
                DurationProvider = durationProvider,
                RouterSelections = request.RouterSelections
            };

            var timingResult = await _timingCalculationEngine.CalculateTimingsAsync(
                nodeHierarchy,
                edgesToProcess,
                timingOptions,
                cancellationToken);

            var phase2Duration = DateTime.UtcNow - phase2Start;

            if (!timingResult.Success)
            {
                _logger.LogTimingCalculationPhaseFailed(
                    request.ProcedureId, phase2Duration.TotalMilliseconds, timingResult.ErrorMessage);
                _schedulingPhaseLogger.LogPhaseComplete(
                    2, "Calculate Timings", request.ProcedureId, phase2Duration,
                    $"Failed: {timingResult.ErrorMessage}");

                return new ScheduleResult
                {
                    Success = false,
                    NodeSchedules = [],
                    ErrorMessage = timingResult.ErrorMessage,
                    UpdatedNodes = null
                };
            }

            _schedulingPhaseLogger.LogPhaseComplete(
                2, "Calculate Timings", request.ProcedureId, phase2Duration,
                $"Calculated timings for {timingResult.UpdatedNodes.Count} nodes");

            // Phase 3: Apply positions and heights
            var phase3Start = DateTime.UtcNow;
            _schedulingPhaseLogger.LogPhaseStart(3, "Calculate Positions", request.ProcedureId);

            var updatedNodesWithPositions = _nodePositioningService.ApplyPositionsAndHeights(
                timingResult.UpdatedNodes,
                timingResult.DetailedTimingInfo,
                nodeHierarchy.ParentToChildrenMapping);

            var phase3Duration = DateTime.UtcNow - phase3Start;
            _schedulingPhaseLogger.LogPhaseComplete(
                3, "Calculate Positions", request.ProcedureId, phase3Duration,
                $"Positioned {updatedNodesWithPositions.Count} nodes");

            // Phase 4: Convert to schedule result
            var phase4Start = DateTime.UtcNow;
            _schedulingPhaseLogger.LogPhaseStart(4, "Convert Results", request.ProcedureId);

            var scheduleResult = _scheduleResultConverter.ConvertTimingToScheduleResult(
                timingResult,
                updatedNodesWithPositions);

            var phase4Duration = DateTime.UtcNow - phase4Start;
            _schedulingPhaseLogger.LogPhaseComplete(
                4, "Convert Results", request.ProcedureId, phase4Duration,
                $"Converted to schedule result with {scheduleResult.NodeSchedules.Count} schedules");

            // Phase 5: Merge hidden state from Phase 0 filtering back into the schedule result.
            // Always runs when routers exist to both hide excluded nodes AND clear stale
            // Hidden=true on nodes that became visible (e.g. when ManuallySelectedBranch was cleared).
            if (hasRouters && scheduleResult.UpdatedNodes != null)
            {
                var hiddenStateMap = allNodesWithHiddenState.ToDictionary(n => n.Id, n => n.Hidden);
                var mergedNodes = scheduleResult.UpdatedNodes
                    .Select(n =>
                    {
                        if (hiddenStateMap.TryGetValue(n.Id, out var hidden) && n.Hidden != hidden)
                            return n with { Hidden = hidden };
                        return n;
                    })
                    .ToList();

                // Add hidden nodes that weren't in the schedule result (excluded from scheduling)
                var scheduledNodeIds = new HashSet<Guid>(scheduleResult.UpdatedNodes.Select(n => n.Id));
                var hiddenNodesNotScheduled = allNodesWithHiddenState
                    .Where(n => n.Hidden == true && !scheduledNodeIds.Contains(n.Id))
                    .ToList();

                mergedNodes.AddRange(hiddenNodesNotScheduled);

                scheduleResult = scheduleResult with { UpdatedNodes = mergedNodes };

                _logger.LogHiddenStateMerged(hiddenNodesNotScheduled.Count, mergedNodes.Count);
            }

            // Log statistics and analysis if detailed timing info is available
            if (timingResult.DetailedTimingInfo != null)
            {
                var statistics = _timingAnalyzer.CollectStatistics(timingResult.DetailedTimingInfo);
                _schedulingPhaseLogger.LogTimingStatistics(request.ProcedureId, statistics);

                _schedulingPhaseLogger.LogDetailedNodeTimings(
                    request.ProcedureId,
                    timingResult.DetailedTimingInfo,
                    updatedNodesWithPositions);

                var criticalPath = _timingAnalyzer.AnalyzeCriticalPath(
                    timingResult.DetailedTimingInfo,
                    updatedNodesWithPositions);

                _schedulingPhaseLogger.LogCriticalPathAnalysis(
                    request.ProcedureId,
                    criticalPath,
                    updatedNodesWithPositions,
                    timingResult.DetailedTimingInfo);
            }

            var totalDuration = DateTime.UtcNow - startTime;
            _schedulingPhaseLogger.LogPipelineComplete(
                request.ProcedureId,
                totalDuration,
                scheduleResult.NodeSchedules.Count,
                [phase0Duration, phase1Duration, phase2Duration, phase3Duration, phase4Duration]);

            return scheduleResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogSchedulingPipelineCancelled(
                request.ProcedureId, (DateTime.UtcNow - startTime).TotalMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            var totalDuration = DateTime.UtcNow - startTime;
            var message = $"Unexpected error in scheduling pipeline for procedure {request.ProcedureId}: {ex.Message}";
            _logger.LogSchedulingPipelineError(
                ex, request.ProcedureId, totalDuration.TotalMilliseconds, ex.Message);

            return new ScheduleResult
            {
                Success = false,
                NodeSchedules = [],
                ErrorMessage = message,
                UpdatedNodes = null
            };
        }
    }
}