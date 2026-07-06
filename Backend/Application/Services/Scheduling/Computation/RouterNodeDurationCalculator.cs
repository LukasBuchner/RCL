using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Default implementation of <see cref="IRouterNodeDurationCalculator" />.
///     Calculates RouterNode durations based on branch target node timings.
///     When a branch is selected, uses that branch's timing. When no branch is selected,
///     uses the maximum duration across all branch targets so the router visually spans
///     all possible execution paths.
/// </summary>
public class RouterNodeDurationCalculator : IRouterNodeDurationCalculator
{
    private readonly ILogger<RouterNodeDurationCalculator> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="RouterNodeDurationCalculator" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public RouterNodeDurationCalculator(ILogger<RouterNodeDurationCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public double? CalculateRouterNodeDuration(
        RouterNode routerNode,
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null)
    {
        ArgumentNullException.ThrowIfNull(routerNode);
        ArgumentNullException.ThrowIfNull(allNodes);
        ArgumentNullException.ThrowIfNull(nodeTimings);

        // Determine the selected target node ID using dual-mode logic
        var selectedTargetNodeId = GetSelectedTargetNodeId(routerNode, routerSelections);

        if (!selectedTargetNodeId.HasValue)
        {
            // No selection - use max duration across all branch targets
            var maxBranchSchedule = CalculateMaxBranchSchedule(routerNode, nodeTimings);
            if (maxBranchSchedule.HasValue)
            {
                _logger.LogRouterNodeMaxBranchDuration(routerNode.Id, maxBranchSchedule.Value.Duration);
                return maxBranchSchedule.Value.Duration;
            }

            _logger.LogRouterNodeNoBranchTimings(routerNode.Id);
            return null;
        }

        // Look up the target node's timing information
        if (!nodeTimings.TryGetValue(selectedTargetNodeId.Value, out var targetTiming))
        {
            _logger.LogRouterNodeTargetNoTiming(routerNode.Id, selectedTargetNodeId.Value);
            return null;
        }

        _logger.LogRouterNodeDurationFromTarget(routerNode.Id, targetTiming.Duration, selectedTargetNodeId.Value);

        return targetTiming.Duration;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        CalculateRouterNodeSchedules(
            IReadOnlyList<Node> allNodes,
            IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings,
            IReadOnlyDictionary<Guid, Guid>? routerSelections = null)
    {
        ArgumentNullException.ThrowIfNull(allNodes);
        ArgumentNullException.ThrowIfNull(nodeTimings);

        var routerNodeSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();
        var routerNodes = allNodes.OfType<RouterNode>().ToList();

        _logger.LogRouterNodeScheduleCalculationStarted(routerNodes.Count);

        foreach (var routerNode in routerNodes)
        {
            // Determine the selected target node ID using dual-mode logic
            var selectedTargetNodeId = GetSelectedTargetNodeId(routerNode, routerSelections);

            if (!selectedTargetNodeId.HasValue)
            {
                // No selection - use max branch schedule so the router spans all possible paths
                var maxBranchSchedule = CalculateMaxBranchSchedule(routerNode, nodeTimings);
                if (maxBranchSchedule.HasValue)
                {
                    routerNodeSchedules[routerNode.Id] = maxBranchSchedule.Value;

                    _logger.LogRouterNodeMaxBranchSchedule(
                        routerNode.Id, maxBranchSchedule.Value.Duration, maxBranchSchedule.Value.StartTime,
                        maxBranchSchedule.Value.FinishTime, routerNode.RouterTask.Branches.Count);
                }
                else
                {
                    // No branch timings available - fall back to original stored duration
                    var originalDuration = routerNode.RouterTask.Duration;
                    var originalStartTime = routerNode.RouterTask.StartTime;
                    routerNodeSchedules[routerNode.Id] = (originalDuration, originalStartTime,
                        originalStartTime + originalDuration);

                    _logger.LogRouterNodeOriginalSchedule(
                        routerNode.Id, originalDuration, originalStartTime, originalStartTime + originalDuration,
                        "no selected branch, no branch timings available");
                    _logger.LogRouterNodeScheduleFellBackToStored(
                        routerNode.Id, originalDuration, originalStartTime, originalStartTime + originalDuration);
                }

                continue;
            }

            // Look up the target node's timing information
            if (nodeTimings.TryGetValue(selectedTargetNodeId.Value, out var targetTiming))
            {
                // RouterNode schedule matches the selected branch's target node schedule
                var schedule = (targetTiming.Duration, targetTiming.StartTime, targetTiming.FinishTime);
                routerNodeSchedules[routerNode.Id] = schedule;

                _logger.LogRouterNodeScheduleFromTarget(
                    routerNode.Id, targetTiming.Duration, targetTiming.StartTime, targetTiming.FinishTime,
                    selectedTargetNodeId.Value);
            }
            else
            {
                // Target node timing not available - use original duration
                var originalDuration = routerNode.RouterTask.Duration;
                var originalStartTime = routerNode.RouterTask.StartTime;
                routerNodeSchedules[routerNode.Id] = (originalDuration, originalStartTime,
                    originalStartTime + originalDuration);

                var reason = $"selected target {selectedTargetNodeId.Value} has no timing";
                _logger.LogRouterNodeOriginalSchedule(
                    routerNode.Id, originalDuration, originalStartTime, originalStartTime + originalDuration,
                    reason);
                _logger.LogRouterNodeSelectedTargetTimingMissing(
                    routerNode.Id, originalDuration, originalStartTime, originalStartTime + originalDuration,
                    selectedTargetNodeId.Value);
            }
        }

        var routersWithTimedTargets = routerNodes.Count(rn =>
            GetSelectedTargetNodeId(rn, routerSelections).HasValue &&
            nodeTimings.ContainsKey(GetSelectedTargetNodeId(rn, routerSelections)!.Value));

        _logger.LogRouterNodeScheduleCalculationCompleted(
            routerNodes.Count,
            routersWithTimedTargets);

        return routerNodeSchedules;
    }

    /// <summary>
    ///     Calculates the schedule spanning all non-empty branch targets by finding the earliest start
    ///     and latest finish across the branches that carry scheduled work. Zero-extent (empty) branches
    ///     contribute nothing to the span, so the router covers only its real execution paths.
    /// </summary>
    /// <param name="routerNode">The RouterNode whose branches to evaluate.</param>
    /// <param name="nodeTimings">Map of node IDs to their calculated timings.</param>
    /// <returns>
    ///     The aggregated schedule spanning the non-empty branches, or null when no branch carries
    ///     scheduled work.
    /// </returns>
    private static (double Duration, double StartTime, double FinishTime)? CalculateMaxBranchSchedule(
        RouterNode routerNode,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings)
    {
        var branchTimings = routerNode.RouterTask.Branches
            .Where(b => b.TargetNodeId.HasValue && nodeTimings.ContainsKey(b.TargetNodeId.Value))
            .Select(b => nodeTimings[b.TargetNodeId!.Value])
            .Where(t => t.Duration > 0)
            .ToList();

        if (branchTimings.Count == 0)
            return null;

        var earliestStart = branchTimings.Min(t => t.StartTime);
        var latestFinish = branchTimings.Max(t => t.FinishTime);
        var duration = latestFinish - earliestStart;

        return (duration, earliestStart, latestFinish);
    }

    /// <summary>
    ///     Determines the selected target node ID using dual-mode logic:
    ///     - Execution mode: Use routerSelections dictionary
    ///     - Planning mode: Use SelectedBranchTargetNodeId or ManuallySelectedBranch
    /// </summary>
    /// <param name="routerNode">The RouterNode to determine selection for.</param>
    /// <param name="routerSelections">
    ///     Optional dictionary mapping router node ID to selected target node ID (execution mode).
    /// </param>
    /// <returns>The selected target node ID, or null if no selection exists.</returns>
    private Guid? GetSelectedTargetNodeId(RouterNode routerNode, IReadOnlyDictionary<Guid, Guid>? routerSelections)
    {
        // Priority 0: Execution-time selections (execution mode)
        if (routerSelections != null && routerSelections.TryGetValue(routerNode.Id, out var executionSelectedTargetId))
        {
            _logger.LogRouterNodeExecutionTimeSelection(routerNode.Id, executionSelectedTargetId);
            return executionSelectedTargetId;
        }

        // Priority 1: Execution state (SelectedBranchTargetNodeId set during execution)
        if (routerNode.RouterTask.SelectedBranchTargetNodeId.HasValue)
        {
            var selectedTargetId = routerNode.RouterTask.SelectedBranchTargetNodeId.Value;
            _logger.LogRouterNodeExecutionStateSelection(routerNode.Id, selectedTargetId);
            return selectedTargetId;
        }

        // Priority 2: Manual selection in design mode
        if (!string.IsNullOrWhiteSpace(routerNode.RouterTask.ManuallySelectedBranch))
        {
            var manualBranchName = routerNode.RouterTask.ManuallySelectedBranch;
            var selectedBranch = routerNode.RouterTask.Branches
                .FirstOrDefault(b => b.Name == manualBranchName);

            if (selectedBranch?.TargetNodeId.HasValue == true)
            {
                _logger.LogRouterNodeManualSelection(routerNode.Id, manualBranchName,
                    selectedBranch.TargetNodeId.Value);
                return selectedBranch.TargetNodeId.Value;
            }

            _logger.LogRouterNodeInvalidManualSelection(routerNode.Id, manualBranchName);
        }

        // No selection available
        _logger.LogRouterNodeNoSelection(routerNode.Id);
        return null;
    }
}