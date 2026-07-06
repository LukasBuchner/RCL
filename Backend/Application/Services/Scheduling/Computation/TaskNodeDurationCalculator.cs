using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Default implementation of <see cref="ITaskNodeDurationCalculator" />.
///     Calculates TaskNode durations based on their child SkillExecutionNodes' timing.
/// </summary>
public class TaskNodeDurationCalculator : ITaskNodeDurationCalculator
{
    private readonly IChildNodeCollector _childNodeCollector;
    private readonly IHierarchicalSorter _hierarchicalSorter;
    private readonly ILogger<TaskNodeDurationCalculator> _logger;
    private readonly ITimingAggregator _timingAggregator;

    /// <summary>
    ///     Initializes a new instance of <see cref="TaskNodeDurationCalculator" />.
    /// </summary>
    /// <param name="childNodeCollector">Service for collecting child nodes.</param>
    /// <param name="timingAggregator">Service for aggregating timing information.</param>
    /// <param name="hierarchicalSorter">Service for sorting nodes hierarchically.</param>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public TaskNodeDurationCalculator(
        IChildNodeCollector childNodeCollector,
        ITimingAggregator timingAggregator,
        IHierarchicalSorter hierarchicalSorter,
        ILogger<TaskNodeDurationCalculator> logger)
    {
        _childNodeCollector = childNodeCollector ?? throw new ArgumentNullException(nameof(childNodeCollector));
        _timingAggregator = timingAggregator ?? throw new ArgumentNullException(nameof(timingAggregator));
        _hierarchicalSorter = hierarchicalSorter ?? throw new ArgumentNullException(nameof(hierarchicalSorter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public double? CalculateTaskNodeDuration(
        TaskNode taskNode,
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings)
    {
        ArgumentNullException.ThrowIfNull(taskNode);
        ArgumentNullException.ThrowIfNull(allNodes);
        ArgumentNullException.ThrowIfNull(nodeTimings);

        // Find all child nodes (SkillExecutionNodes, TaskNodes, AND RouterNodes)
        var (childSkillNodes, childTaskNodes, childRouterNodes) =
            _childNodeCollector.CollectAllChildNodes(taskNode.Id, allNodes);

        if (childSkillNodes.Count == 0 && childTaskNodes.Count == 0 && childRouterNodes.Count == 0)
        {
            _logger.LogTaskNodeNoChildren(taskNode.Id);
            return null;
        }

        // Get timings for all child nodes that have been scheduled
        var childTimings = new List<(double Duration, double StartTime, double FinishTime)>();

        // Add skill node timings
        foreach (var child in childSkillNodes)
            if (nodeTimings.TryGetValue(child.Id, out var skillTiming))
                childTimings.Add(skillTiming);

        // Add task node timings
        foreach (var child in childTaskNodes)
            if (nodeTimings.TryGetValue(child.Id, out var taskTiming))
                childTimings.Add(taskTiming);

        // Add router node timings
        foreach (var child in childRouterNodes)
            if (nodeTimings.TryGetValue(child.Id, out var routerTiming))
                childTimings.Add(routerTiming);

        if (childTimings.Count == 0)
        {
            _logger.LogTaskNodeNoChildTimings(taskNode.Id);
            return null;
        }

        // Calculate duration as span from earliest start to latest finish using TimingAggregator
        var (calculatedDuration, earliestStart, latestFinish) = _timingAggregator.AggregateTimings(childTimings);

        var scheduledSkillCount = childSkillNodes.Count(c => nodeTimings.ContainsKey(c.Id));
        var scheduledTaskCount = childTaskNodes.Count(c => nodeTimings.ContainsKey(c.Id));
        var scheduledRouterCount = childRouterNodes.Count(c => nodeTimings.ContainsKey(c.Id));

        _logger.LogTaskNodeDurationCalculated(
            taskNode.Id, calculatedDuration, earliestStart, latestFinish,
            scheduledSkillCount, scheduledTaskCount, scheduledRouterCount);

        return calculatedDuration;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, double> CalculateAllTaskNodeDurations(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> nodeTimings)
    {
        var taskNodeDurations = new Dictionary<Guid, double>();
        var taskNodes = allNodes.OfType<TaskNode>().ToList();

        // Only process container TaskNodes (those without ParentId or those that have children)
        // Child TaskNodes should not appear in the final results as they are not containers
        var containerTaskNodes = taskNodes.Where(tn =>
        {
            if (!tn.ParentId.HasValue) return true; // Root TaskNodes

            // Check if this TaskNode has any children using ChildNodeCollector
            var (childSkillNodes, childTaskNodes, childRouterNodes) =
                _childNodeCollector.CollectAllChildNodes(tn.Id, allNodes);
            return childSkillNodes.Count > 0 || childTaskNodes.Count > 0 || childRouterNodes.Count > 0;
        }).ToList();

        // Process TaskNodes in hierarchical order (children before parents)
        var sortedTaskNodes = _hierarchicalSorter.SortTaskNodesHierarchically(taskNodes);

        _logger.LogContainerDurationCalculationStarted(containerTaskNodes.Count, taskNodes.Count);

        // Create a mutable copy of nodeTimings that we can add to as we calculate TaskNode timings
        var allNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>(nodeTimings);

        foreach (var taskNode in sortedTaskNodes)
        {
            var calculatedDuration = CalculateTaskNodeDuration(taskNode, allNodes, allNodeTimings);

            if (calculatedDuration.HasValue)
            {
                // Only add to results if this is a container TaskNode
                if (containerTaskNodes.Contains(taskNode))
                {
                    taskNodeDurations[taskNode.Id] = calculatedDuration.Value;

                    _logger.LogContainerTaskNodeDuration(taskNode.Id, calculatedDuration.Value);
                }
                else
                {
                    _logger.LogChildTaskNodeDurationExcluded(taskNode.Id, calculatedDuration.Value);
                }

                // Add this TaskNode's timing to allNodeTimings so parent TaskNodes can use it (regardless of container status)
                // We need to estimate start/finish times based on children
                var childTimings = new List<(double Duration, double StartTime, double FinishTime)>();

                // Gather child node timings using ChildNodeCollector
                var (childSkillNodes, childTaskNodesList, childRouterNodesList) =
                    _childNodeCollector.CollectAllChildNodes(taskNode.Id, allNodes);

                // Add skill node timings
                foreach (var sn in childSkillNodes)
                    if (allNodeTimings.TryGetValue(sn.Id, out var snTiming))
                        childTimings.Add(snTiming);

                // Add task node timings
                foreach (var tn in childTaskNodesList)
                    if (allNodeTimings.TryGetValue(tn.Id, out var tnTiming))
                        childTimings.Add(tnTiming);

                // Add router node timings
                foreach (var rn in childRouterNodesList)
                    if (allNodeTimings.TryGetValue(rn.Id, out var rnTiming))
                        childTimings.Add(rnTiming);

                if (childTimings.Count > 0)
                {
                    var (_, earliestStart, latestFinish) = _timingAggregator.AggregateTimings(childTimings);
                    allNodeTimings[taskNode.Id] = (calculatedDuration.Value, earliestStart, latestFinish);
                }
            }
            else if (containerTaskNodes.Contains(taskNode))
            {
                // Use original duration only for container TaskNodes
                taskNodeDurations[taskNode.Id] = taskNode.Task.Duration;
                _logger.LogContainerTaskNodeOriginalDuration(taskNode.Id, taskNode.Task.Duration);
                _logger.LogContainerTaskNodeDurationFellBackToStored(taskNode.Id, taskNode.Task.Duration);
            }
        }

        var calculatedContainerCount = sortedTaskNodes.Count(tn =>
            containerTaskNodes.Contains(tn) && CalculateTaskNodeDuration(tn, allNodes, allNodeTimings).HasValue);

        _logger.LogContainerDurationCalculationCompleted(
            containerTaskNodes.Count,
            taskNodes.Count,
            calculatedContainerCount);

        return taskNodeDurations;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> CalculateTaskNodeSchedules(
        IReadOnlyList<Node> allNodes,
        IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)> childNodeTimings)
    {
        var taskNodeSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();
        var taskNodes = allNodes.OfType<TaskNode>().ToList();

        // Process TaskNodes in hierarchical order (deepest first) using topological sort
        var sortedTaskNodes = _hierarchicalSorter.SortTaskNodesHierarchically(taskNodes);

        _logger.LogTaskNodeScheduleCalculationStarted(taskNodes.Count);

        // Create a mutable copy that includes both initial timings AND progressively calculated TaskNode timings
        var allNodeTimings =
            new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>(childNodeTimings);

        // Debug log the hierarchical processing order
        _logger.LogHierarchicalProcessingOrderHeader();
        for (var i = 0; i < sortedTaskNodes.Count; i++)
        {
            var taskNode = sortedTaskNodes[i];
            var parentIdText = taskNode.ParentId?.ToString() ?? "null";
            _logger.LogHierarchicalProcessingOrderEntry(
                i + 1, taskNode.Id, taskNode.Task.Name, parentIdText);
        }

        foreach (var taskNode in sortedTaskNodes)
        {
            // Find all direct children using ChildNodeCollector
            var (childSkillExecutions, childTaskNodes, childRouterNodes) =
                _childNodeCollector.CollectAllChildNodes(taskNode.Id, allNodes);

            var hasAnyChildren = childSkillExecutions.Count > 0 || childTaskNodes.Count > 0 ||
                                 childRouterNodes.Count > 0;

            _logger.LogTaskNodeChildrenFound(
                taskNode.Id, taskNode.Task.Name, childSkillExecutions.Count, childTaskNodes.Count,
                childRouterNodes.Count);

            // Debug log nested TaskNode children and their timing availability
            if (childTaskNodes.Count > 0)
                foreach (var childTask in childTaskNodes)
                    if (allNodeTimings.TryGetValue(childTask.Id, out var timing))
                    {
                        _logger.LogChildTaskNodeTimingAvailability(childTask.Id, childTask.Task.Name, true);
                        _logger.LogChildTaskNodeTimingDetails(timing.StartTime, timing.FinishTime, timing.Duration);
                    }
                    else
                    {
                        _logger.LogChildTaskNodeTimingAvailability(childTask.Id, childTask.Task.Name, false);
                    }

            if (hasAnyChildren)
            {
                // Collect timings from all child types that have calculated timings
                var childTimings = new List<(double Duration, double StartTime, double FinishTime)>();

                // Add skill execution child timings
                foreach (var child in childSkillExecutions)
                    if (allNodeTimings.TryGetValue(child.Id, out var timing))
                    {
                        childTimings.Add(timing);
                        _logger.LogSkillChildTimingFound(child.Id, timing.StartTime, timing.FinishTime,
                            timing.Duration);
                    }
                    else
                    {
                        _logger.LogSkillChildTimingNotFound(child.Id);
                    }

                // Add task node child timings (supporting nested TaskNodes)
                childTimings.AddRange(childTaskNodes
                    .Where(child => allNodeTimings.ContainsKey(child.Id))
                    .Select(child => allNodeTimings[child.Id]));

                // Add router node child timings (supporting nested RouterNodes)
                childTimings.AddRange(childRouterNodes
                    .Where(child => allNodeTimings.ContainsKey(child.Id))
                    .Select(child => allNodeTimings[child.Id]));

                if (childTimings.Count > 0)
                {
                    // TaskNode spans from earliest child start to latest child finish using TimingAggregator
                    var (calculatedDuration, earliestStart, latestFinish) =
                        _timingAggregator.AggregateTimings(childTimings);

                    var schedule = (calculatedDuration, earliestStart, latestFinish);
                    taskNodeSchedules[taskNode.Id] = schedule;

                    // CRITICAL: Add this TaskNode's schedule to allNodeTimings so parent TaskNodes can use it
                    allNodeTimings[taskNode.Id] = schedule;

                    var timedSkillCount = childSkillExecutions.Count(c => allNodeTimings.ContainsKey(c.Id));
                    var timedTaskCount = childTaskNodes.Count(c => allNodeTimings.ContainsKey(c.Id));

                    _logger.LogTaskNodeScheduleCalculated(
                        taskNode.Id, calculatedDuration, earliestStart, latestFinish,
                        timedSkillCount, timedTaskCount);
                }
                else
                {
                    // No child timings available, use original duration starting at time 0
                    var originalDuration = taskNode.Task.Duration;
                    taskNodeSchedules[taskNode.Id] = (originalDuration, 0.0, originalDuration);

                    _logger.LogTaskNodeOriginalSchedule(
                        taskNode.Id, originalDuration, originalDuration, "no child timings available");
                    _logger.LogTaskNodeScheduleFellBackToStored(taskNode.Id, originalDuration);
                }
            }
            else
            {
                // A childless task is an empty container with no executable work, so it occupies no
                // scheduled extent. A zero-extent schedule keeps it from claiming its nominal duration
                // on the timeline.
                taskNodeSchedules[taskNode.Id] = (0.0, 0.0, 0.0);

                _logger.LogTaskNodeOriginalSchedule(
                    taskNode.Id, 0.0, 0.0, "no children (empty container)");
            }
        }

        var nodesWithChildTimings = taskNodes.Count(tn =>
        {
            var (skillChildren, taskChildren, routerChildren) =
                _childNodeCollector.CollectAllChildNodes(tn.Id, allNodes);
            return skillChildren.Any(sen => childNodeTimings.ContainsKey(sen.Id)) ||
                   taskChildren.Any(childTn => childNodeTimings.ContainsKey(childTn.Id)) ||
                   routerChildren.Any(rn => childNodeTimings.ContainsKey(rn.Id));
        });

        _logger.LogTaskNodeScheduleCalculationCompleted(
            taskNodes.Count,
            nodesWithChildTimings);

        return taskNodeSchedules;
    }
}