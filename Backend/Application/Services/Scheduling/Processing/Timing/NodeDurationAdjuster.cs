using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Service responsible for adjusting node durations based on their hierarchy and children's timing.
///     Extracts duration adjustment logic from TimingCalculationEngine to follow Single Responsibility Principle.
/// </summary>
public class NodeDurationAdjuster : INodeDurationAdjuster
{
    private readonly IHierarchicalSorter _hierarchicalSorter;
    private readonly ILogger<NodeDurationAdjuster> _logger;
    private readonly IRouterNodeDurationCalculator _routerNodeDurationCalculator;
    private readonly ITaskNodeDurationCalculator _taskNodeDurationCalculator;
    private readonly ITimingAggregator _timingAggregator;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeDurationAdjuster" />.
    /// </summary>
    /// <param name="taskNodeDurationCalculator">Service for calculating task node durations.</param>
    /// <param name="routerNodeDurationCalculator">Service for calculating router node durations.</param>
    /// <param name="childNodeCollector">Service for collecting child nodes by type.</param>
    /// <param name="timingAggregator">Service for aggregating timing information.</param>
    /// <param name="hierarchicalSorter">Service for sorting nodes hierarchically.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public NodeDurationAdjuster(
        ITaskNodeDurationCalculator taskNodeDurationCalculator,
        IRouterNodeDurationCalculator routerNodeDurationCalculator,
        IChildNodeCollector childNodeCollector,
        ITimingAggregator timingAggregator,
        IHierarchicalSorter hierarchicalSorter,
        ILogger<NodeDurationAdjuster> logger)
    {
        _taskNodeDurationCalculator = taskNodeDurationCalculator ??
                                      throw new ArgumentNullException(nameof(taskNodeDurationCalculator));
        _routerNodeDurationCalculator = routerNodeDurationCalculator ??
                                        throw new ArgumentNullException(nameof(routerNodeDurationCalculator));
        ArgumentNullException.ThrowIfNull(childNodeCollector);
        _timingAggregator = timingAggregator ?? throw new ArgumentNullException(nameof(timingAggregator));
        _hierarchicalSorter = hierarchicalSorter ?? throw new ArgumentNullException(nameof(hierarchicalSorter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void AdjustParentTaskDurations(
        NodeHierarchyInfo nodeHierarchy,
        Dictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> skillExecutionTimings,
        IReadOnlyDictionary<Guid, Guid>? routerSelections = null)
    {
        ArgumentNullException.ThrowIfNull(nodeHierarchy);
        ArgumentNullException.ThrowIfNull(durations);
        ArgumentNullException.ThrowIfNull(skillExecutionTimings);

        _logger.LogDurationAdjustmentStarted(nodeHierarchy.AllNodes.Count);

        // Check if TaskNodeDurationCalculator has already correctly calculated durations
        var taskNodeDurationCalculatorResults = _taskNodeDurationCalculator.CalculateTaskNodeSchedules(
            nodeHierarchy.AllNodes.ToList(),
            skillExecutionTimings);

        _logger.LogTaskNodeSchedulesProvided(taskNodeDurationCalculatorResults.Count);

        // Only proceed with hierarchical adjustment if TaskNodeDurationCalculator didn't provide results
        if (taskNodeDurationCalculatorResults.Count > 0)
        {
            _logger.LogUsingTaskNodeCalculatorResults(taskNodeDurationCalculatorResults.Count);

            // Update durations with the correctly calculated values from TaskNodeDurationCalculator
            foreach (var (taskNodeId, schedule) in taskNodeDurationCalculatorResults)
            {
                var originalDuration = durations.GetValueOrDefault(taskNodeId, 0);
                durations[taskNodeId] = schedule.Duration;

                _logger.LogTaskNodeDurationSet(taskNodeId, schedule.Duration, originalDuration);
            }

            // After TaskNode calculations, calculate RouterNode durations
            // Combine skillExecutionTimings with taskNodeDurationCalculatorResults so RouterNodes can access both
            var combinedTimings =
                new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>(skillExecutionTimings);
            foreach (var (taskNodeId, schedule) in taskNodeDurationCalculatorResults)
                combinedTimings[taskNodeId] = schedule;

            var routerNodeDurationCalculatorResults = _routerNodeDurationCalculator.CalculateRouterNodeSchedules(
                nodeHierarchy.AllNodes.ToList(),
                combinedTimings,
                routerSelections);

            _logger.LogRouterNodeSchedulesProvided(routerNodeDurationCalculatorResults.Count);

            // Update durations with the correctly calculated values from RouterNodeDurationCalculator
            foreach (var (routerNodeId, schedule) in routerNodeDurationCalculatorResults)
            {
                var originalDuration = durations.GetValueOrDefault(routerNodeId, 0);
                durations[routerNodeId] = schedule.Duration;

                _logger.LogRouterNodeDurationSet(routerNodeId, schedule.Duration, originalDuration);
            }

            return;
        }

        // Fallback: Use legacy hierarchical adjustment only if TaskNodeDurationCalculator didn't provide results
        _logger.LogFallingBackToLegacyAdjustment();
        _logger.LogFallingBackToLegacyAdjustmentWarning(nodeHierarchy.AllNodes.Count);
        PerformLegacyHierarchicalAdjustment(nodeHierarchy, durations, skillExecutionTimings);
    }

    /// <inheritdoc />
    public double? CalculateRequiredDurationForChildren(
        IReadOnlyList<Node> children,
        IReadOnlyDictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> skillExecutionTimings)
    {
        if (children.Count == 0) return null;

        var childTimings = new List<(double Duration, double StartTime, double FinishTime)>();

        foreach (var child in children)
        {
            double startTime, finishTime, duration;

            // Get timing information based on child node type
            switch (child)
            {
                case TaskNode taskNode:
                    startTime = taskNode.Task.StartTime;
                    duration = durations.GetValueOrDefault(taskNode.Id, taskNode.Task.Duration);
                    finishTime = taskNode.Task.StartTime + duration;

                    childTimings.Add((duration, startTime, finishTime));

                    _logger.LogChildTaskNodeTiming(taskNode.Id, startTime, duration, finishTime);
                    break;

                case SkillExecutionNode skillNode:
                    if (skillExecutionTimings.TryGetValue(skillNode.Id, out var timing))
                    {
                        childTimings.Add(timing);
                    }
                    else
                    {
                        // Fall back to the skill's original timing
                        startTime = skillNode.SkillExecutionTask.StartTime;
                        finishTime = skillNode.SkillExecutionTask.FinishTime ??
                                     skillNode.SkillExecutionTask.StartTime + skillNode.SkillExecutionTask.Duration;
                        duration = finishTime - startTime;

                        childTimings.Add((duration, startTime, finishTime));
                        _logger.LogSkillChildTimingFallback(skillNode.Id, startTime, duration);
                    }

                    break;

                case RouterNode routerNode:
                    startTime = routerNode.RouterTask.StartTime;
                    duration = durations.GetValueOrDefault(routerNode.Id, routerNode.RouterTask.Duration);
                    finishTime = routerNode.RouterTask.StartTime + duration;

                    childTimings.Add((duration, startTime, finishTime));

                    _logger.LogChildRouterNodeTiming(routerNode.Id, startTime, duration, finishTime);
                    break;

                default:
                    continue; // Skip unknown node types
            }
        }

        if (childTimings.Count == 0) return null;

        // Use TimingAggregator to calculate the required duration
        var (requiredDuration, _, _) = _timingAggregator.AggregateTimings(childTimings);

        _logger.LogRequiredDurationCalculated(requiredDuration, childTimings.Count);

        return requiredDuration;
    }

    private void PerformLegacyHierarchicalAdjustment(
        NodeHierarchyInfo nodeHierarchy,
        Dictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> skillExecutionTimings)
    {
        var parentToChildren = nodeHierarchy.ParentToChildrenMapping;
        var taskNodes = nodeHierarchy.AllNodes.OfType<TaskNode>().ToList();
        var routerNodes = nodeHierarchy.AllNodes.OfType<RouterNode>().ToList();

        var totalAdjustmentCount = 0;
        var iteration = 0;
        const int maxIterations = 10;

        var taskNodesByDepth = _hierarchicalSorter.SortTaskNodesHierarchically(taskNodes);

        bool adjustmentsMade;
        do
        {
            adjustmentsMade = false;
            iteration++;
            var iterationAdjustmentCount = 0;

            _logger.LogLegacyAdjustmentIterationStarted(iteration);

            foreach (var taskNode in taskNodesByDepth)
                if (parentToChildren.TryGetValue(taskNode.Id, out var children) && children.Count > 0)
                {
                    var requiredDuration =
                        CalculateRequiredDurationForChildren(children, durations, skillExecutionTimings);

                    if (requiredDuration.HasValue && Math.Abs(durations[taskNode.Id] - requiredDuration.Value) > 0.001)
                    {
                        var originalDuration = durations[taskNode.Id];
                        durations[taskNode.Id] = requiredDuration.Value;
                        iterationAdjustmentCount++;
                        totalAdjustmentCount++;
                        adjustmentsMade = true;

                        _logger.LogLegacyTaskNodeDurationAdjusted(
                            taskNode.Id, originalDuration, requiredDuration.Value);
                    }
                }

            // Also adjust RouterNode parents that have children in the parentToChildren mapping
            foreach (var routerNode in routerNodes)
                if (parentToChildren.TryGetValue(routerNode.Id, out var routerChildren) && routerChildren.Count > 0)
                {
                    var requiredDuration =
                        CalculateRequiredDurationForChildren(routerChildren, durations, skillExecutionTimings);

                    if (requiredDuration.HasValue && durations.TryGetValue(routerNode.Id, out var routerDuration) &&
                        Math.Abs(routerDuration - requiredDuration.Value) > 0.001)
                    {
                        var originalDuration = routerDuration;
                        durations[routerNode.Id] = requiredDuration.Value;
                        iterationAdjustmentCount++;
                        totalAdjustmentCount++;
                        adjustmentsMade = true;

                        _logger.LogLegacyRouterNodeDurationAdjusted(
                            routerNode.Id, originalDuration, requiredDuration.Value);
                    }
                }

            _logger.LogLegacyAdjustmentIterationCompleted(iteration, iterationAdjustmentCount);

            if (iteration >= maxIterations)
            {
                _logger.LogMaxIterationsReached(maxIterations);
                break;
            }
        } while (adjustmentsMade);

        _logger.LogDurationAdjustmentCompleted(totalAdjustmentCount, iteration);
    }
}