using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Maps timing information to domain nodes.
///     Extracted from TimingCalculationEngine to follow Single Responsibility Principle.
/// </summary>
public class NodeTimingMapper : INodeTimingMapper
{
    private readonly ILogger<NodeTimingMapper> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeTimingMapper" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public NodeTimingMapper(ILogger<NodeTimingMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Node ApplyTimingToNode(
        Node originalNode,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo,
        IReadOnlyDictionary<Guid, double> durations,
        IReadOnlyDictionary<Guid, IPlannedSkillExecution>? plannedSkills = null)
    {
        ArgumentNullException.ThrowIfNull(originalNode);
        ArgumentNullException.ThrowIfNull(durations);

        _logger.LogApplyTimingStart(1, timingInfo != null);

        var nodeTypeName = originalNode.GetType().Name;

        if (timingInfo?.TryGetValue(originalNode.Id, out var timing) == true)
        {
            _logger.LogDetailedTimingAvailable(originalNode.Id, timing.Duration, timing.AbsoluteStartTime,
                timing.AbsoluteFinishTime);
            return originalNode switch
            {
                SkillExecutionNode skillNode => ApplyTimingToSkillExecutionNode(skillNode, timing, plannedSkills),
                TaskNode taskNode => ApplyTimingToTaskNode(taskNode, timing),
                RouterNode routerNode => ApplyTimingToRouterNode(routerNode, timing),
                _ => originalNode
            };
        }

        // If we don't have detailed timing info but have duration, apply just the duration
        if (durations.TryGetValue(originalNode.Id, out var duration))
        {
            _logger.LogDurationOnlyFallback(originalNode.Id, duration);
            var fallbackStartTime = originalNode switch
            {
                SkillExecutionNode s => s.SkillExecutionTask.StartTime,
                TaskNode t => t.Task.StartTime,
                RouterNode r => r.RouterTask.StartTime,
                _ => 0.0
            };
            _logger.LogDurationOnlyFallbackWarning(originalNode.Id, nodeTypeName, duration, fallbackStartTime);

            if (originalNode is SkillExecutionNode skillNodeForLog)
            {
                var newFinishTime = skillNodeForLog.SkillExecutionTask.StartTime + duration;
                _logger.LogSkillTiming(
                    "NODE_TIMING_MAPPER_FALLBACK",
                    skillNodeForLog.Id,
                    skillNodeForLog.SkillExecutionTask.Skill.Name,
                    skillNodeForLog.SkillExecutionTask.AgentId,
                    skillNodeForLog.SkillExecutionTask.ExecutionId,
                    "FALLBACK",
                    false,
                    skillNodeForLog.SkillExecutionTask.StartTime,
                    newFinishTime,
                    duration,
                    additionalInfo: "Using ORIGINAL StartTime + new Duration (no DetailedTiming available)");
            }

            // Apply execution progress if available
            if (originalNode is SkillExecutionNode sNode &&
                plannedSkills?.TryGetValue(sNode.Id, out var plannedSkill) == true)
                return ApplyTimingWithProgressToSkillNode(sNode, duration, plannedSkill);

            return originalNode switch
            {
                SkillExecutionNode skillNode => skillNode with
                {
                    SkillExecutionTask = skillNode.SkillExecutionTask with
                    {
                        Duration = duration,
                        FinishTime = skillNode.SkillExecutionTask.StartTime + duration
                    }
                },
                TaskNode taskNode => taskNode with
                {
                    Task = taskNode.Task with
                    {
                        Duration = duration,
                        FinishTime = taskNode.Task.StartTime + duration
                    }
                },
                RouterNode routerNode => ApplyDurationFallbackToRouterNode(routerNode, duration),
                _ => originalNode
            };
        }

        _logger.LogNoTimingApplied(originalNode.Id, nodeTypeName);
        return originalNode;
    }

    /// <inheritdoc />
    public void AdjustRelativeStartTimesForHierarchy(
        Dictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(timingInfo);
        ArgumentNullException.ThrowIfNull(nodes);

        var nodeDict = nodes.ToDictionary(n => n.Id);
        var updatedTimingInfo = new Dictionary<Guid, NodeTimingInfo>();

        // Calculate earliest start time to use as baseline
        var earliestStartTime = timingInfo.Values.Count > 0
            ? timingInfo.Values.Min(t => t.AbsoluteStartTime)
            : 0.0;

        _logger.LogRelativeTimeAdjustmentStart(nodes.Count, earliestStartTime);

        var adjustmentCount = 0;

        foreach (var (nodeId, timing) in timingInfo)
        {
            if (!nodeDict.TryGetValue(nodeId, out var node))
            {
                // Node not found in the provided list, keep original timing
                updatedTimingInfo[nodeId] = timing;
                continue;
            }

            double newRelativeStartTime;

            var nodeTypeName = node.GetType().Name;

            if (node.ParentId.HasValue && timingInfo.TryGetValue(node.ParentId.Value, out var parentTiming))
            {
                // Child node: relative = absolute - parent absolute
                newRelativeStartTime = timing.AbsoluteStartTime - parentTiming.AbsoluteStartTime;
                adjustmentCount++;

                _logger.LogRelativeAdjustChild(
                    nodeId, nodeTypeName, node.ParentId, timing.AbsoluteStartTime,
                    parentTiming.AbsoluteStartTime, newRelativeStartTime);

                _logger.LogTimingAppliedToNode(
                    nodeId,
                    nodeTypeName,
                    timing.Duration,
                    timing.AbsoluteStartTime,
                    newRelativeStartTime);
            }
            else if (node.ParentId.HasValue)
            {
                // Child node but parent NOT in timing info - POTENTIAL BUG
                newRelativeStartTime = timing.AbsoluteStartTime;

                _logger.LogRelativeAdjustOrphan(nodeId, nodeTypeName, node.ParentId, timing.AbsoluteStartTime);

                if (Math.Abs(timing.RelativeStartTime - newRelativeStartTime) > 0.001) adjustmentCount++;
            }
            else
            {
                // Root node (no parent): relative = absolute
                newRelativeStartTime = timing.AbsoluteStartTime;

                _logger.LogRelativeAdjustRoot(nodeId, nodeTypeName, timing.AbsoluteStartTime,
                    newRelativeStartTime);

                if (Math.Abs(timing.RelativeStartTime - newRelativeStartTime) > 0.001) adjustmentCount++;
            }

            // Update the timing info with corrected relative start time
            var newRelativeFinishTime = newRelativeStartTime + timing.Duration;
            updatedTimingInfo[nodeId] = timing with
            {
                RelativeStartTime = newRelativeStartTime,
                RelativeFinishTime = newRelativeFinishTime
            };
        }

        // Replace the original timing info with the corrected values
        timingInfo.Clear();
        foreach (var (nodeId, updatedTiming) in updatedTimingInfo) timingInfo[nodeId] = updatedTiming;

        _logger.LogRelativeTimeAdjustmentComplete(nodes.Count, adjustmentCount);
    }

    private SkillExecutionNode ApplyTimingToSkillExecutionNode(SkillExecutionNode skillNode, NodeTimingInfo timing,
        IReadOnlyDictionary<Guid, IPlannedSkillExecution>? plannedSkills = null)
    {
        _logger.LogTimingAppliedToNode(
            skillNode.Id,
            nameof(SkillExecutionNode),
            timing.Duration,
            timing.AbsoluteStartTime,
            timing.RelativeStartTime);

        // Check if we have execution progress data for this skill
        if (plannedSkills?.TryGetValue(skillNode.Id, out var plannedSkill) == true)
            // Apply both timing and execution progress data
            return ApplyTimingWithProgressToSkillNode(skillNode, timing, plannedSkill);

        _logger.LogSkillTiming(
            "NODE_TIMING_MAPPER",
            skillNode.Id,
            skillNode.SkillExecutionTask.Skill.Name,
            skillNode.SkillExecutionTask.AgentId,
            skillNode.SkillExecutionTask.ExecutionId,
            "MAPPING",
            false,
            timing.AbsoluteStartTime,
            timing.AbsoluteFinishTime,
            timing.Duration,
            additionalInfo: "Applying DetailedTiming from scheduler");

        var updatedTask = skillNode.SkillExecutionTask with
        {
            Duration = timing.Duration,
            StartTime = timing.AbsoluteStartTime,
            FinishTime = timing.AbsoluteFinishTime
        };

        return skillNode with
        {
            SkillExecutionTask = updatedTask
        };
    }

    private static TaskNode ApplyTimingToTaskNode(TaskNode taskNode, NodeTimingInfo timing)
    {
        var updatedTask = taskNode.Task with
        {
            Duration = timing.Duration,
            StartTime = timing.AbsoluteStartTime,
            FinishTime = timing.AbsoluteFinishTime
        };

        return taskNode with
        {
            Task = updatedTask
        };
    }

    /// <summary>
    ///     Applies timing information to a RouterNode.
    /// </summary>
    /// <param name="routerNode">The router node to update.</param>
    /// <param name="timing">The timing information to apply.</param>
    /// <returns>Updated RouterNode with applied timing.</returns>
    private RouterNode ApplyTimingToRouterNode(RouterNode routerNode, NodeTimingInfo timing)
    {
        _logger.LogRouterNodeTimingApplied(routerNode.Id, timing.Duration, timing.AbsoluteStartTime,
            timing.AbsoluteFinishTime);

        var updatedRouterTask = routerNode.RouterTask with
        {
            Duration = timing.Duration,
            StartTime = timing.AbsoluteStartTime,
            FinishTime = timing.AbsoluteFinishTime
        };

        return routerNode with
        {
            RouterTask = updatedRouterTask
        };
    }

    /// <summary>
    ///     Applies duration fallback to a RouterNode (when only duration is available, not full timing).
    /// </summary>
    /// <param name="routerNode">The router node to update.</param>
    /// <param name="duration">The duration to apply.</param>
    /// <returns>Updated RouterNode with applied duration.</returns>
    private RouterNode ApplyDurationFallbackToRouterNode(RouterNode routerNode, double duration)
    {
        _logger.LogRouterNodeDurationFallback(routerNode.Id, duration);
        _logger.LogRouterNodeDurationFallbackWarning(routerNode.Id, duration, routerNode.RouterTask.StartTime);

        return routerNode with
        {
            RouterTask = routerNode.RouterTask with
            {
                Duration = duration,
                FinishTime = routerNode.RouterTask.StartTime + duration
            }
        };
    }

    /// <summary>
    ///     Applies timing information along with execution progress data to a skill node.
    /// </summary>
    private SkillExecutionNode ApplyTimingWithProgressToSkillNode(SkillExecutionNode skillNode, NodeTimingInfo timing,
        IPlannedSkillExecution plannedSkill)
    {
        // Check if this is a skill execution with runtime data
        if (plannedSkill is not ISkillExecution skillExecution)
        {
            // No execution data available, just apply timing
            _logger.LogSkillTiming(
                "NODE_TIMING_MAPPER_WITH_PROGRESS",
                skillNode.Id,
                skillNode.SkillExecutionTask.Skill.Name,
                skillNode.SkillExecutionTask.AgentId,
                skillNode.SkillExecutionTask.ExecutionId,
                "SCHEDULED",
                plannedSkill is IAdaptiveSkillExecution,
                timing.AbsoluteStartTime,
                timing.AbsoluteFinishTime,
                timing.Duration,
                additionalInfo: "No execution data available");

            var basicTask = skillNode.SkillExecutionTask with
            {
                Duration = timing.Duration,
                StartTime = timing.AbsoluteStartTime,
                FinishTime = timing.AbsoluteFinishTime
            };

            return skillNode with
            {
                SkillExecutionTask = basicTask
            };
        }

        // Calculate progress percentage if we have actual start time
        double? progressPercentage = null;
        var isExecuting = false;

        if (skillExecution.ActualStartTime.HasValue)
        {
            isExecuting = !skillExecution.ActualFinishTime.HasValue; // Running if started but not finished

            if (isExecuting)
            {
                // For running skills, calculate progress from LastProgress if available
                if (skillExecution.LastProgress != null)
                {
                    var currentTime = skillExecution.LastProgress.CurrentTimeIntoExecution;
                    var estimatedDuration = skillExecution.LastProgress.EstimatedTotalDuration;

                    if (estimatedDuration > 0)
                    {
                        progressPercentage = currentTime / estimatedDuration * 100.0;
                        // Clamp to 0-100 range
                        progressPercentage = Math.Clamp(progressPercentage.Value, 0.0, 100.0);
                    }
                }
            }
            else
            {
                // Completed
                progressPercentage = 100.0;
            }
        }

        var executionStatus = isExecuting ? "RUNNING" :
            skillExecution.ActualFinishTime.HasValue ? "COMPLETED" : "SCHEDULED";
        var progressInfo = $"Applied execution progress: {progressPercentage:F2}%";

        _logger.LogSkillTiming(
            "NODE_TIMING_MAPPER_WITH_PROGRESS",
            skillNode.Id,
            skillNode.SkillExecutionTask.Skill.Name,
            skillNode.SkillExecutionTask.AgentId,
            skillNode.SkillExecutionTask.ExecutionId,
            executionStatus,
            plannedSkill is IAdaptiveSkillExecution,
            timing.AbsoluteStartTime,
            timing.AbsoluteFinishTime,
            timing.Duration,
            skillExecution.ActualStartTime,
            skillExecution.ActualFinishTime,
            additionalInfo: progressInfo);

        var updatedTask = skillNode.SkillExecutionTask with
        {
            Duration = timing.Duration,
            StartTime = skillExecution.ActualStartTime ?? timing.AbsoluteStartTime,
            FinishTime = skillExecution.ActualFinishTime ?? timing.AbsoluteFinishTime,
            IsExecuting = isExecuting,
            Progress = progressPercentage
        };

        return skillNode with
        {
            SkillExecutionTask = updatedTask
        };
    }

    /// <summary>
    ///     Applies timing information along with execution progress data to a skill node (simplified version for fallback
    ///     case).
    /// </summary>
    private SkillExecutionNode ApplyTimingWithProgressToSkillNode(SkillExecutionNode skillNode, double duration,
        IPlannedSkillExecution plannedSkill)
    {
        // Check if this is a skill execution with runtime data
        if (plannedSkill is not ISkillExecution skillExecution)
        {
            // No execution data available, just apply duration
            var basicTask = skillNode.SkillExecutionTask with
            {
                Duration = duration,
                FinishTime = skillNode.SkillExecutionTask.StartTime + duration
            };

            return skillNode with
            {
                SkillExecutionTask = basicTask
            };
        }

        // Calculate progress percentage if we have actual start time
        double? progressPercentage = null;
        var isExecuting = false;
        var startTime = skillExecution.ActualStartTime ?? skillNode.SkillExecutionTask.StartTime;
        double finishTime;

        if (skillExecution.ActualStartTime.HasValue)
        {
            isExecuting = !skillExecution.ActualFinishTime.HasValue; // Running if started but not finished

            if (isExecuting)
            {
                // Still running - use estimated duration if available
                finishTime = startTime + (skillExecution.EstimatedDuration ?? duration);

                // For running skills, calculate progress from LastProgress if available
                if (skillExecution.LastProgress != null)
                {
                    var currentTime = skillExecution.LastProgress.CurrentTimeIntoExecution;
                    var estimatedDuration = skillExecution.LastProgress.EstimatedTotalDuration;

                    if (estimatedDuration > 0)
                    {
                        progressPercentage = currentTime / estimatedDuration * 100.0;
                        // Clamp to 0-100 range
                        progressPercentage = Math.Clamp(progressPercentage.Value, 0.0, 100.0);
                    }
                }
            }
            else
            {
                // Completed - use actual finish time
                finishTime = skillExecution.ActualFinishTime ?? startTime + duration;
                progressPercentage = 100.0;
            }
        }
        else
        {
            // Not started yet
            finishTime = startTime + duration;
        }

        var fallbackStatus = isExecuting ? "RUNNING" :
            skillExecution.ActualFinishTime.HasValue ? "COMPLETED" : "SCHEDULED";
        var fallbackProgressInfo = $"Applied execution progress (fallback): {progressPercentage:F2}%";

        _logger.LogSkillTiming(
            "NODE_TIMING_MAPPER_WITH_PROGRESS_FALLBACK",
            skillNode.Id,
            skillNode.SkillExecutionTask.Skill.Name,
            skillNode.SkillExecutionTask.AgentId,
            skillNode.SkillExecutionTask.ExecutionId,
            fallbackStatus,
            plannedSkill is IAdaptiveSkillExecution,
            startTime,
            finishTime,
            duration,
            skillExecution.ActualStartTime,
            skillExecution.ActualFinishTime,
            additionalInfo: fallbackProgressInfo);

        var updatedTask = skillNode.SkillExecutionTask with
        {
            Duration = duration,
            StartTime = startTime,
            FinishTime = finishTime,
            IsExecuting = isExecuting,
            Progress = progressPercentage
        };

        return skillNode with
        {
            SkillExecutionTask = updatedTask
        };
    }
}