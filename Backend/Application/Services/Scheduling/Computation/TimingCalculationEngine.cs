using System.Diagnostics;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;
using PlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.PlannedSkillExecution;
using PlannedAdaptiveSkillExecution =
    FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.PlannedAdaptiveSkillExecution;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Implementation of the timing calculation engine.
///     Follows SOLID principles - delegates specific responsibilities to specialised services.
/// </summary>
public class TimingCalculationEngine : ITimingCalculationEngine
{
    private readonly INodeDurationAdjuster _durationAdjuster;
    private readonly IExecutionGraphBuilder _executionGraphBuilder;
    private readonly ILogger<TimingCalculationEngine> _logger;
    private readonly INodeResolver _nodeResolver;
    private readonly ISchedulePlanner _schedulePlanner;
    private readonly ITimingStatisticsCollector _statisticsCollector;
    private readonly ITaskNodeDurationCalculator _taskNodeDurationCalculator;
    private readonly INodeTimingMapper _timingMapper;

    /// <summary>
    ///     Initialises a new instance of <see cref="TimingCalculationEngine" /> with all services.
    /// </summary>
    public TimingCalculationEngine(
        IExecutionGraphBuilder executionGraphBuilder,
        ISchedulePlanner schedulePlanner,
        ITaskNodeDurationCalculator taskNodeDurationCalculator,
        ITimingStatisticsCollector statisticsCollector,
        INodeDurationAdjuster durationAdjuster,
        INodeTimingMapper timingMapper,
        INodeResolver nodeResolver,
        ILogger<TimingCalculationEngine> logger)
    {
        _executionGraphBuilder =
            executionGraphBuilder ?? throw new ArgumentNullException(nameof(executionGraphBuilder));
        _schedulePlanner = schedulePlanner ?? throw new ArgumentNullException(nameof(schedulePlanner));
        _taskNodeDurationCalculator = taskNodeDurationCalculator ??
                                      throw new ArgumentNullException(nameof(taskNodeDurationCalculator));
        _statisticsCollector = statisticsCollector ?? throw new ArgumentNullException(nameof(statisticsCollector));
        _durationAdjuster = durationAdjuster ?? throw new ArgumentNullException(nameof(durationAdjuster));
        _timingMapper = timingMapper ?? throw new ArgumentNullException(nameof(timingMapper));
        _nodeResolver = nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <inheritdoc />
    public async Task<TimingResult> CalculateTimingsAsync(
        NodeHierarchyInfo nodeHierarchy,
        IReadOnlyList<DependencyEdge> edges,
        TimingCalculationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodeHierarchy);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(options);

        var totalStopwatch = Stopwatch.StartNew();
        var statistics = _statisticsCollector.CreateStatistics();

        _logger.LogTimingCalculationStarted(options.ProcedureId);

        try
        {
            // Handle case where no skill execution nodes exist
            if (!nodeHierarchy.HasSkillExecutionNodes)
                return HandleNoSkillScenario(nodeHierarchy, options, totalStopwatch);

            // Phase 1: Build execution graph
            var executionGraphStopwatch = Stopwatch.StartNew();
            var executionGraph = await _executionGraphBuilder.BuildAsync(
                nodeHierarchy.AllNodes, edges, options.DurationProvider, options.StrictMode);
            executionGraphStopwatch.Stop();

            if (executionGraph == null)
                return HandleExecutionGraphFailure(
                    nodeHierarchy, options, totalStopwatch, executionGraphStopwatch.Elapsed);

            if (executionGraph.SkillExecutions.Count == 0)
                return HandleEmptyExecutionGraph(
                    nodeHierarchy, options, totalStopwatch, executionGraphStopwatch.Elapsed);

            // Phase 2: Plan the schedule with currentTime
            var schedulingStopwatch = Stopwatch.StartNew();
            var planningSucceeded = _schedulePlanner.Plan(executionGraph, options.CurrentTime);
            schedulingStopwatch.Stop();

            if (!planningSucceeded)
                return HandleSchedulingFailure(
                    nodeHierarchy, options, totalStopwatch,
                    executionGraphStopwatch.Elapsed, schedulingStopwatch.Elapsed);

            // Phase 3: Extract timing information and update domain model
            var domainUpdateStopwatch = Stopwatch.StartNew();
            var timingResult = BuildTimingResultAsync(
                executionGraph, nodeHierarchy.AllNodes, options, nodeHierarchy, edges);
            domainUpdateStopwatch.Stop();

            totalStopwatch.Stop();

            var finalStatistics = _statisticsCollector.UpdateStatistics(
                statistics,
                nodeHierarchy.SkillExecutionNodes.Count,
                nodeHierarchy.TaskNodes.Count,
                executionGraphStopwatch.Elapsed,
                schedulingStopwatch.Elapsed,
                domainUpdateStopwatch.Elapsed,
                totalStopwatch.Elapsed);

            _logger.LogTimingCalculationCompleted(options.ProcedureId);

            return timingResult with { Statistics = finalStatistics };
        }
        catch (ScheduleInfeasibleException ex)
        {
            totalStopwatch.Stop();
            _logger.LogScheduleInfeasible(ex, options.ProcedureId);

            return new TimingResult
            {
                Durations = new Dictionary<Guid, double>(),
                UpdatedNodes = new List<Node>().AsReadOnly(),
                Success = false,
                ErrorMessage = $"Schedule infeasible: {ex.Message}",
                Statistics = _statisticsCollector.UpdateStatistics(statistics, 0, 0, totalTime: totalStopwatch.Elapsed)
            };
        }
        catch (ScheduleModelException ex)
        {
            totalStopwatch.Stop();
            _logger.LogScheduleModelError(ex, options.ProcedureId);

            return new TimingResult
            {
                Durations = new Dictionary<Guid, double>(),
                UpdatedNodes = new List<Node>().AsReadOnly(),
                Success = false,
                ErrorMessage = $"Schedule model error: {ex.Message}",
                Statistics = _statisticsCollector.UpdateStatistics(statistics, 0, 0, totalTime: totalStopwatch.Elapsed)
            };
        }
        catch (InvalidOperationException ex)
        {
            totalStopwatch.Stop();
            _logger.LogSchedulingOperationFailed(ex, options.ProcedureId);

            return new TimingResult
            {
                Durations = new Dictionary<Guid, double>(),
                UpdatedNodes = new List<Node>().AsReadOnly(),
                Success = false,
                ErrorMessage = $"Scheduling operation failed: {ex.Message}",
                Statistics = _statisticsCollector.UpdateStatistics(statistics, 0, 0, totalTime: totalStopwatch.Elapsed)
            };
        }
    }

    private TimingResult HandleNoSkillScenario(
        NodeHierarchyInfo nodeHierarchy,
        TimingCalculationOptions options,
        Stopwatch totalStopwatch)
    {
        _logger.LogNoSkillExecutionNodes(options.ProcedureId);

        var durations = new Dictionary<Guid, double>();
        var detailedTimingInfo = options.IncludeDetailedTiming ? new Dictionary<Guid, NodeTimingInfo>() : null;
        var updatedNodes = new List<Node>();

        // Process nodes with their original durations
        foreach (var node in nodeHierarchy.AllNodes)
        {
            var (duration, startTime, finishTime) = GetNodeOriginalTiming(node);
            durations[node.Id] = duration;

            if (detailedTimingInfo != null)
                detailedTimingInfo[node.Id] = new NodeTimingInfo
                {
                    Duration = duration,
                    AbsoluteStartTime = startTime,
                    AbsoluteFinishTime = finishTime,
                    RelativeStartTime = startTime,
                    RelativeFinishTime = finishTime,
                    NodeType = node is SkillExecutionNode ? NodeTimingType.SkillExecution : NodeTimingType.Original,
                    IsCalculated = false
                };

            updatedNodes.Add(node);
        }

        totalStopwatch.Stop();

        return new TimingResult
        {
            Durations = durations,
            DetailedTimingInfo = detailedTimingInfo,
            UpdatedNodes = updatedNodes.AsReadOnly(),
            Success = true,
            Statistics = _statisticsCollector.UpdateStatistics(
                _statisticsCollector.CreateStatistics(),
                nodeHierarchy.SkillExecutionNodes.Count,
                nodeHierarchy.TaskNodes.Count,
                totalTime: totalStopwatch.Elapsed)
        };
    }

    private TimingResult HandleExecutionGraphFailure(
        NodeHierarchyInfo nodeHierarchy,
        TimingCalculationOptions options,
        Stopwatch totalStopwatch,
        TimeSpan executionGraphBuildTime)
    {
        _logger.LogExecutionGraphBuildFailed(options.ProcedureId);

        var fallbackResult = CreateFallbackResult(nodeHierarchy, options, false);
        totalStopwatch.Stop();

        return fallbackResult with
        {
            Success = false,
            ErrorMessage = "Failed to build execution graph",
            Statistics = _statisticsCollector.UpdateStatistics(
                fallbackResult.Statistics,
                nodeHierarchy.SkillExecutionNodes.Count,
                nodeHierarchy.TaskNodes.Count,
                executionGraphBuildTime,
                totalTime: totalStopwatch.Elapsed)
        };
    }

    private TimingResult HandleEmptyExecutionGraph(
        NodeHierarchyInfo nodeHierarchy,
        TimingCalculationOptions options,
        Stopwatch totalStopwatch,
        TimeSpan executionGraphBuildTime)
    {
        _logger.LogEmptyExecutionGraph(options.ProcedureId);

        var fallbackResult = CreateFallbackResult(nodeHierarchy, options);
        totalStopwatch.Stop();

        return fallbackResult with
        {
            Success = true, // Empty execution graph is handled as success with fallback
            ErrorMessage = null,
            Statistics = _statisticsCollector.UpdateStatistics(
                fallbackResult.Statistics,
                nodeHierarchy.SkillExecutionNodes.Count,
                nodeHierarchy.TaskNodes.Count,
                executionGraphBuildTime,
                totalTime: totalStopwatch.Elapsed)
        };
    }

    private TimingResult HandleSchedulingFailure(
        NodeHierarchyInfo nodeHierarchy,
        TimingCalculationOptions options,
        Stopwatch totalStopwatch,
        TimeSpan executionGraphBuildTime,
        TimeSpan schedulingTime)
    {
        _logger.LogSchedulePlanningFailed(options.ProcedureId);

        var fallbackResult = CreateFallbackResult(nodeHierarchy, options, false);
        totalStopwatch.Stop();

        return fallbackResult with
        {
            Success = false,
            ErrorMessage = "Failed to plan schedule",
            Statistics = _statisticsCollector.UpdateStatistics(
                fallbackResult.Statistics,
                nodeHierarchy.SkillExecutionNodes.Count,
                nodeHierarchy.TaskNodes.Count,
                executionGraphBuildTime,
                schedulingTime,
                totalTime: totalStopwatch.Elapsed)
        };
    }

    private TimingResult CreateFallbackResult(NodeHierarchyInfo nodeHierarchy, TimingCalculationOptions options,
        bool defaultSuccess = true)
    {
        var durations = new Dictionary<Guid, double>();
        var detailedTimingInfo = options.IncludeDetailedTiming ? new Dictionary<Guid, NodeTimingInfo>() : null;

        // Build skill execution timing map
        var skillExecutionTimings = nodeHierarchy.SkillExecutionNodes.ToDictionary(
            sn => sn.Id,
            sn => (
                sn.SkillExecutionTask.Duration,
                sn.SkillExecutionTask.StartTime,
                FinishTime: sn.SkillExecutionTask.FinishTime ??
                            sn.SkillExecutionTask.StartTime + sn.SkillExecutionTask.Duration
            ));

        // Initialise durations
        foreach (var node in nodeHierarchy.AllNodes)
        {
            var (duration, _, _) = GetNodeOriginalTiming(node);
            durations[node.Id] = duration;
        }

        // Apply duration adjustments
        _durationAdjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillExecutionTimings,
            options.RouterSelections);

        // Create updated nodes with adjusted durations (no execution progress in fallback case)
        var updatedNodes = nodeHierarchy.AllNodes.Select(originalNode =>
            _timingMapper.ApplyTimingToNode(originalNode, detailedTimingInfo, durations)).ToList();

        return new TimingResult
        {
            Durations = durations,
            DetailedTimingInfo = detailedTimingInfo,
            UpdatedNodes = updatedNodes.AsReadOnly(),
            Success = defaultSuccess,
            Statistics = _statisticsCollector.CreateStatistics()
        };
    }

    private TimingResult BuildTimingResultAsync(
        IExecutionGraph executionGraph,
        IReadOnlyList<Node> originalNodes,
        TimingCalculationOptions options,
        NodeHierarchyInfo nodeHierarchy,
        IReadOnlyList<DependencyEdge> edges)
    {
        var durations = new Dictionary<Guid, double>();
        var detailedTimingInfo = options.IncludeDetailedTiming ? new Dictionary<Guid, NodeTimingInfo>() : null;

        // Extract skill execution timing information
        var skillExecutionTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        foreach (var skillExecution in executionGraph.SkillExecutions)
        {
            // Skip zero-extent leafless placeholders (Core-only, not application-planned skills): they are LP
            // ordering carriers, not real skills. Skipping pre-write keeps them out of the durations and
            // skill-timing maps that feed task-schedule and parent-duration adjustment; Part A positions the
            // empty container's display deterministically at F_pred.
            if (skillExecution is not IPlannedSkillExecution) continue;

            durations[skillExecution.Id] = skillExecution.PlannedDuration;
            skillExecutionTimings[skillExecution.Id] = (
                skillExecution.PlannedDuration,
                skillExecution.PlannedStartTime,
                skillExecution.PlannedFinishTime
            );

            if (detailedTimingInfo == null) continue;

            detailedTimingInfo[skillExecution.Id] = new NodeTimingInfo
            {
                Duration = skillExecution.PlannedDuration,
                AbsoluteStartTime = skillExecution.PlannedStartTime,
                AbsoluteFinishTime = skillExecution.PlannedFinishTime,
                RelativeStartTime = skillExecution.PlannedStartTime,
                RelativeFinishTime = skillExecution.PlannedFinishTime,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            };

            // Extract additional info if available from concrete types
            var skillName = "Unknown";
            var agentId = Guid.Empty;
            var isAdaptive = false;
            double? minDuration = null;

            if (skillExecution is PlannedSkillExecution planned)
            {
                skillName = planned.Name;
                agentId = planned.DomainAgent.Id;
            }
            else if (skillExecution is PlannedAdaptiveSkillExecution adaptive)
            {
                skillName = adaptive.Name;
                agentId = adaptive.DomainAgent.Id;
                isAdaptive = true;
                minDuration = adaptive.MinDuration;
            }

            _logger.LogSkillTiming(
                "PLAN_EXTRACTION",
                skillExecution.Id,
                skillName,
                agentId,
                null,
                "PLANNED",
                isAdaptive,
                skillExecution.PlannedStartTime,
                skillExecution.PlannedFinishTime,
                skillExecution.PlannedDuration,
                minDuration: minDuration,
                additionalInfo: "Extracted from SchedulePlanner result");
        }

        // Calculate task node schedules
        var taskNodeSchedules =
            _taskNodeDurationCalculator.CalculateTaskNodeSchedules(originalNodes, skillExecutionTimings);

        foreach (var (taskNodeId, schedule) in taskNodeSchedules)
        {
            durations[taskNodeId] = schedule.Duration;

            if (detailedTimingInfo != null)
                detailedTimingInfo[taskNodeId] = new NodeTimingInfo
                {
                    Duration = schedule.Duration,
                    AbsoluteStartTime = schedule.StartTime,
                    AbsoluteFinishTime = schedule.FinishTime,
                    RelativeStartTime = schedule.StartTime,
                    RelativeFinishTime = schedule.FinishTime,
                    NodeType = NodeTimingType.Task,
                    IsCalculated = true
                };
        }

        // Apply duration adjustments
        _durationAdjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillExecutionTimings,
            options.RouterSelections);

        // After duration adjustments, expose RouterNode timing in detailedTimingInfo for NodeTimingMapper,
        // collapsing any router whose shown branch carries no executable work to a zero-extent point at
        // the router's own resolved start so an empty branch never occupies the timeline.
        if (detailedTimingInfo != null)
        {
            // A branch carries no work when its target resolves to no executable nodes. This is the same
            // resolution the dependency graph uses, so the schedule and the runtime agree on emptiness.
            bool BranchHasNoExecutableWork(Guid branchTargetId)
            {
                return !_nodeResolver.ResolveToExecutableIds(branchTargetId, nodeHierarchy).Any();
            }

            // Ids of empty branch subtrees already pinned to a router's start, so the standalone-task
            // pass below does not re-position them off their router-driven slot.
            var routerCollapsedEmpties = new HashSet<Guid>();

            var routerNodes = nodeHierarchy.RouterNodes;
            foreach (var routerNode in routerNodes)
                // Duration has been updated by AdjustParentTaskDurations
                if (durations.TryGetValue(routerNode.Id, out var routerDuration))
                {
                    // Get the original or recalculated start time
                    // RouterNodeDurationCalculator sets duration based on selected branch target
                    var originalTiming = GetNodeOriginalTiming(routerNode);

                    // Try to infer start time from selected branch target if available
                    var startTime = originalTiming.StartTime;
                    var finishTime = originalTiming.FinishTime;

                    // Find the selected branch target node to get its timing
                    Guid? selectedTargetNodeId = null;
                    if (options.RouterSelections != null &&
                        options.RouterSelections.TryGetValue(routerNode.Id, out var executionTarget))
                    {
                        selectedTargetNodeId = executionTarget;
                    }
                    else if (routerNode.RouterTask.SelectedBranchTargetNodeId.HasValue)
                    {
                        selectedTargetNodeId = routerNode.RouterTask.SelectedBranchTargetNodeId.Value;
                    }
                    else if (!string.IsNullOrWhiteSpace(routerNode.RouterTask.ManuallySelectedBranch))
                    {
                        var selectedBranch = routerNode.RouterTask.Branches
                            .FirstOrDefault(b => b.Name == routerNode.RouterTask.ManuallySelectedBranch);
                        selectedTargetNodeId = selectedBranch?.TargetNodeId;
                    }

                    // Branch targets present in the hierarchy (excluded branches are filtered out upstream).
                    var branchTargetIds = routerNode.RouterTask.Branches
                        .Where(b => b.TargetNodeId.HasValue && detailedTimingInfo.ContainsKey(b.TargetNodeId.Value))
                        .Select(b => b.TargetNodeId!.Value)
                        .ToList();

                    // The router shows no work when its selected branch is empty, or — with no selection —
                    // when every shown branch is empty.
                    var selectedBranchEmpty =
                        selectedTargetNodeId.HasValue && BranchHasNoExecutableWork(selectedTargetNodeId.Value);
                    var routerHasNoWork = selectedTargetNodeId.HasValue
                        ? selectedBranchEmpty
                        : branchTargetIds.Count > 0 && branchTargetIds.All(BranchHasNoExecutableWork);

                    // Inherit the selected branch's timing only when that branch carries work; an empty
                    // selected branch falls through to the predecessor-based start so the router is not
                    // pinned to the empty branch's time 0.
                    if (selectedTargetNodeId.HasValue && !selectedBranchEmpty &&
                        detailedTimingInfo.TryGetValue(selectedTargetNodeId.Value, out var targetTiming))
                    {
                        startTime = targetTiming.AbsoluteStartTime;
                        finishTime = targetTiming.AbsoluteFinishTime;
                    }
                    else
                    {
                        // Calculate start time from incoming edge dependencies: a router starts when its
                        // predecessor finishes (FS edge) or starts (SS edge).
                        var incomingEdges = edges.Where(e => e.TargetId == routerNode.Id).ToList();
                        if (incomingEdges.Count > 0)
                        {
                            startTime = ComputePredecessorBasedStart(routerNode.Id, edges, detailedTimingInfo);
                            _logger.LogRouterNodeStartTimeFromEdges(routerNode.Id, incomingEdges.Count, startTime);
                        }

                        finishTime = startTime + routerDuration;
                    }

                    // Collapse a work-free router to a zero-extent point at its own start, zeroing both the
                    // local duration and the duration map so ApplyTimingToNode and the width calculator
                    // report an honest zero width. This overwrites any nominal fallback from
                    // RouterNodeDurationCalculator when no branch carried timing.
                    if (routerHasNoWork)
                    {
                        routerDuration = 0;
                        finishTime = startTime;
                        durations[routerNode.Id] = 0;
                    }

                    detailedTimingInfo[routerNode.Id] = new NodeTimingInfo
                    {
                        Duration = routerDuration,
                        AbsoluteStartTime = startTime,
                        AbsoluteFinishTime = finishTime,
                        RelativeStartTime = startTime,
                        RelativeFinishTime = finishTime,
                        NodeType = NodeTimingType.Router,
                        IsCalculated = true
                    };

                    _logger.LogRouterNodeTimingAdded(routerNode.Id, startTime, finishTime, routerDuration);

                    // Collapse every empty branch subtree to the router's start with zero extent, so an
                    // empty branch occupies no timeline span in any selection mode.
                    foreach (var emptyNodeId in from branchTargetId in branchTargetIds
                                                where BranchHasNoExecutableWork(branchTargetId)
                                                select HierarchyTraversal
                                                    .CollectDescendants(branchTargetId, nodeHierarchy.AllNodes)
                                                    .Select(n => n.Id)
                                                    .Append(branchTargetId)
                             into emptySubtree
                                                from emptyNodeId in emptySubtree
                                                where detailedTimingInfo.ContainsKey(emptyNodeId)
                                                select emptyNodeId)
                    {
                        durations[emptyNodeId] = 0;
                        detailedTimingInfo[emptyNodeId] = new NodeTimingInfo
                        {
                            Duration = 0,
                            AbsoluteStartTime = startTime,
                            AbsoluteFinishTime = startTime,
                            RelativeStartTime = startTime,
                            RelativeFinishTime = startTime,
                            NodeType = NodeTimingType.Task,
                            IsCalculated = true
                        };
                        routerCollapsedEmpties.Add(emptyNodeId);
                    }
                }

            // Position standalone leafless TaskNodes (an empty task not owned by a router branch): pin
            // each to a zero-extent point at the max over its incoming dependency edges of predecessor
            // finish (or predecessor start for an SS edge), defaulting to 0 with no positioned predecessor.
            // Skills and routers are already in detailedTimingInfo above, so their finishes resolve here.
            // Descendants of an already-placed empty task are skipped, so a nested empty inherits the
            // ancestor's slot instead of resolving its own (predecessor-less) start to 0.
            var standaloneCollapsed = new HashSet<Guid>();
            foreach (var emptyTask in nodeHierarchy.TaskNodes)
            {
                if (routerCollapsedEmpties.Contains(emptyTask.Id)) continue;
                if (standaloneCollapsed.Contains(emptyTask.Id)) continue;
                if (!BranchHasNoExecutableWork(emptyTask.Id)) continue;

                var taskStart = ComputePredecessorBasedStart(emptyTask.Id, edges, detailedTimingInfo);

                PinZeroExtent(emptyTask.Id, taskStart, durations, detailedTimingInfo);
                _logger.LogStandaloneEmptyTaskTiming(emptyTask.Id, taskStart);

                // Collapse the descendant subtree of the empty task to the same zero-extent point so a
                // nested empty container shares its parent's logical slot.
                foreach (var descendant in HierarchyTraversal.CollectDescendants(emptyTask.Id, nodeHierarchy.AllNodes))
                {
                    PinZeroExtent(descendant.Id, taskStart, durations, detailedTimingInfo);
                    standaloneCollapsed.Add(descendant.Id);
                }
            }
        }

        // Create dictionary of planned skill executions for progress data mapping
        // Note: We need to cast to Application's IPlannedSkillExecution for proper type matching
        var plannedSkills = executionGraph.SkillExecutions
            .Where(se => se is IPlannedSkillExecution)
            .ToDictionary(se => se.Id, se => (IPlannedSkillExecution)se);

        // Create updated nodes with timing applied (including execution progress if available)
        var updatedNodes = originalNodes.Select(originalNode =>
            _timingMapper.ApplyTimingToNode(originalNode, detailedTimingInfo, durations, plannedSkills)).ToList();

        // Adjust relative start times for hierarchy
        if (detailedTimingInfo != null)
            _timingMapper.AdjustRelativeStartTimesForHierarchy(detailedTimingInfo, originalNodes);

        return new TimingResult
        {
            Durations = durations,
            DetailedTimingInfo = detailedTimingInfo,
            UpdatedNodes = updatedNodes.AsReadOnly(),
            Success = true
        };
    }

    /// <summary>
    ///     Pins a node to a zero-extent timing point at <paramref name="startTime" />, writing both the
    ///     duration map and the detailed timing entry so the width calculator and node mapper report an
    ///     honest zero width. Absolute and relative start equal finish, and the duration is zero.
    /// </summary>
    /// <param name="nodeId">The id of the node to pin.</param>
    /// <param name="startTime">The start time at which the node is pinned with a zero extent.</param>
    /// <param name="durations">The duration map updated to <c>0</c> for the node.</param>
    /// <param name="detailedTimingInfo">The detailed timing map receiving the zero-extent entry for the node.</param>
    private static void PinZeroExtent(
        Guid nodeId,
        double startTime,
        Dictionary<Guid, double> durations,
        Dictionary<Guid, NodeTimingInfo> detailedTimingInfo)
    {
        durations[nodeId] = 0;
        detailedTimingInfo[nodeId] = new NodeTimingInfo
        {
            Duration = 0,
            AbsoluteStartTime = startTime,
            AbsoluteFinishTime = startTime,
            RelativeStartTime = startTime,
            RelativeFinishTime = startTime,
            NodeType = NodeTimingType.Task,
            IsCalculated = true
        };
    }

    /// <summary>
    ///     Computes the predecessor-based start time for a node from its incoming dependency edges.
    ///     For each edge targeting <paramref name="nodeId" /> whose source already has positioned timing,
    ///     the contributed time is the source's absolute finish time, or its absolute start time when the
    ///     edge is a start-to-start dependency (source handle maps to <see cref="EventTriggerType.Start" />
    ///     via <see cref="HandleDependencyTypeMapper.ToEventType" />). The result is the maximum over all
    ///     such contributions, and <c>0</c> when no incoming edge has a positioned predecessor.
    /// </summary>
    /// <param name="nodeId">The id of the node whose start time is computed.</param>
    /// <param name="edges">All dependency edges; only those targeting <paramref name="nodeId" /> are used.</param>
    /// <param name="detailedTimingInfo">Already-positioned node timings used to read predecessor start/finish.</param>
    /// <returns>The maximum predecessor-driven start time, or <c>0</c> when no predecessor is positioned.</returns>
    private static double ComputePredecessorBasedStart(
        Guid nodeId,
        IReadOnlyList<DependencyEdge> edges,
        Dictionary<Guid, NodeTimingInfo> detailedTimingInfo)
    {
        double maxPredecessorStart = 0;

        foreach (var edge in edges)
        {
            if (edge.TargetId != nodeId) continue;
            if (!detailedTimingInfo.TryGetValue(edge.SourceId, out var predecessorTiming)) continue;

            var contributedStart =
                HandleDependencyTypeMapper.ToEventType(edge.SourceHandle) == EventTriggerType.Start
                    ? predecessorTiming.AbsoluteStartTime // SS edge - start when the predecessor starts
                    : predecessorTiming.AbsoluteFinishTime; // FS edge - start when the predecessor finishes
            maxPredecessorStart = Math.Max(maxPredecessorStart, contributedStart);
        }

        return maxPredecessorStart;
    }

    /// <summary>
    ///     Gets the original timing information from a node based on its type.
    /// </summary>
    /// <param name="node">The node to extract timing from.</param>
    /// <returns>Tuple containing duration, start time, and finish time.</returns>
    private static (double Duration, double StartTime, double FinishTime) GetNodeOriginalTiming(Node node)
    {
        return node switch
        {
            TaskNode taskNode => (
                taskNode.Task.Duration,
                taskNode.Task.StartTime,
                taskNode.Task.FinishTime ?? taskNode.Task.StartTime + taskNode.Task.Duration
            ),
            SkillExecutionNode skillNode => (
                skillNode.SkillExecutionTask.Duration,
                skillNode.SkillExecutionTask.StartTime,
                skillNode.SkillExecutionTask.FinishTime ??
                skillNode.SkillExecutionTask.StartTime + skillNode.SkillExecutionTask.Duration
            ),
            RouterNode routerNode => (
                routerNode.RouterTask.Duration,
                routerNode.RouterTask.StartTime,
                routerNode.RouterTask.FinishTime ?? routerNode.RouterTask.StartTime + routerNode.RouterTask.Duration
            ),
            _ => (0, 0, 0)
        };
    }
}