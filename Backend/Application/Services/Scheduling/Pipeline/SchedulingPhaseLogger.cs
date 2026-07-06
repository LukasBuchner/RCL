using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Provides structured logging for scheduling pipeline phases and timing analysis using high-performance
///     source-generated logging.
/// </summary>
public sealed class SchedulingPhaseLogger(ILogger<SchedulingPhaseLogger> logger) : ISchedulingPhaseLogger
{
    private readonly ILogger<SchedulingPhaseLogger> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void LogPhaseStart(int phaseNumber, string phaseName, Guid procedureId)
    {
        if (string.IsNullOrWhiteSpace(phaseName))
        {
            _logger.LogPhaseStartInvalidName(procedureId);
            return;
        }

        _logger.LogSchedulingPhaseStart(phaseNumber, phaseName, procedureId);
    }

    /// <inheritdoc />
    public void LogPhaseComplete(int phaseNumber, string phaseName, Guid procedureId, TimeSpan duration,
        string? details)
    {
        if (string.IsNullOrWhiteSpace(phaseName))
        {
            _logger.LogPhaseCompleteInvalidName(procedureId);
            return;
        }

        var durationMs = duration.TotalMilliseconds;
        var detailsText = details ?? "No details";
        _logger.LogSchedulingPhaseComplete(phaseNumber, phaseName, procedureId, durationMs, detailsText);
    }

    /// <inheritdoc />
    public void LogPipelineStart(Guid procedureId, int nodeCount, int edgeCount, bool strictMode, bool preserveOriginal,
        bool includeTiming)
    {
        _logger.LogSchedulingPipelineStart(procedureId, nodeCount, edgeCount, strictMode, preserveOriginal,
            includeTiming);
    }

    /// <inheritdoc />
    public void LogPipelineComplete(Guid procedureId, TimeSpan totalDuration, int scheduleCount,
        TimeSpan[]? phaseDurations)
    {
        if (phaseDurations == null || phaseDurations.Length == 0)
        {
            var totalDurationMs = totalDuration.TotalMilliseconds;
            _logger.LogSchedulingPipelineComplete(procedureId, totalDurationMs, scheduleCount, null);
            return;
        }

        var phaseTimings = string.Join(", ", phaseDurations.Select((d, i) => $"P{i + 1}:{d.TotalMilliseconds:F2}ms"));
        var pipelineDurationMs = totalDuration.TotalMilliseconds;
        _logger.LogSchedulingPipelineComplete(procedureId, pipelineDurationMs, scheduleCount, phaseTimings);
    }

    /// <inheritdoc />
    public void LogTimingStatistics(Guid procedureId, TimingStatistics? statistics)
    {
        if (statistics == null)
        {
            _logger.LogTimingStatisticsNullWarning(procedureId);
            return;
        }

        _logger.LogSchedulingTimingStatistics(
            procedureId,
            statistics.NodeCount,
            statistics.TotalProcedureSpan,
            statistics.EarliestStart,
            statistics.LatestFinish,
            statistics.MinDuration,
            statistics.MaxDuration,
            statistics.AverageDuration,
            statistics.SumDuration);
    }

    /// <inheritdoc />
    public void LogDetailedNodeTimings(Guid procedureId, IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo,
        IReadOnlyList<Node>? nodes)
    {
        if (timingInfo == null || nodes == null)
        {
            _logger.LogDetailedNodeTimingsNullWarning(procedureId);
            return;
        }

        _logger.LogDetailedTaskTimingStart(procedureId);

        // Create a lookup for nodes by ID for efficient access
        var nodeLookup = nodes.ToDictionary(n => n.Id);

        // Group by node type and process separately
        var taskTimings = new List<(TaskNode Node, NodeTimingInfo Timing)>();
        var skillTimings = new List<(SkillExecutionNode Node, NodeTimingInfo Timing)>();
        var routerTimings = new List<(RouterNode Node, NodeTimingInfo Timing)>();
        var otherTimings = new List<(Node Node, NodeTimingInfo Timing)>();

        foreach (var (nodeId, timing) in timingInfo)
            if (nodeLookup.TryGetValue(nodeId, out var node))
                switch (node)
                {
                    case TaskNode taskNode:
                        taskTimings.Add((taskNode, timing));
                        break;
                    case SkillExecutionNode skillNode:
                        skillTimings.Add((skillNode, timing));
                        break;
                    case RouterNode routerNode:
                        routerTimings.Add((routerNode, timing));
                        break;
                    default:
                        otherTimings.Add((node, timing));
                        break;
                }
            else
                _logger.LogSchedulingNodeTimingUnknown(nodeId, timing.Duration, timing.AbsoluteStartTime,
                    timing.AbsoluteFinishTime);

        // Log Task Node timings
        if (taskTimings.Count > 0)
        {
            _logger.LogTimingGroupHeader("Task Nodes", taskTimings.Count);

            // Sort by absolute start time for chronological order
            var sortedTaskTimings = taskTimings.OrderBy(t => t.Timing.AbsoluteStartTime).ToList();

            foreach (var (taskNode, timing) in sortedTaskTimings)
            {
                var taskName = taskNode.Task?.Name ?? "Unnamed Task";
                var parentInfo = taskNode.ParentId.HasValue ? $" [Parent: {taskNode.ParentId.Value:N}]" : " [Root]";

                var timingType = timing.NodeType.ToString();
                _logger.LogSchedulingTaskNodeTiming(
                    taskName,
                    taskNode.Id,
                    parentInfo,
                    timing.Duration,
                    timing.AbsoluteStartTime,
                    timing.RelativeStartTime,
                    timing.AbsoluteFinishTime,
                    timing.RelativeFinishTime,
                    timingType);
            }
        }

        // Log Skill Execution Node timings
        if (skillTimings.Count > 0)
        {
            _logger.LogTimingGroupHeader("Skill Execution Nodes", skillTimings.Count);

            // Sort by absolute start time for chronological order
            var sortedSkillTimings = skillTimings.OrderBy(t => t.Timing.AbsoluteStartTime).ToList();

            foreach (var (skillNode, timing) in sortedSkillTimings)
            {
                var skillName = skillNode.SkillExecutionTask.Skill.Name ?? "Unnamed Skill";
                var agentId = skillNode.SkillExecutionTask.AgentId;
                var parentInfo = skillNode.ParentId.HasValue
                    ? $" [Parent Task: {skillNode.ParentId.Value:N}]"
                    : " [No Parent]";

                var timingType = timing.NodeType.ToString();
                _logger.LogSchedulingSkillNodeTiming(
                    skillName,
                    skillNode.Id,
                    agentId,
                    parentInfo,
                    timing.Duration,
                    timing.AbsoluteStartTime,
                    timing.RelativeStartTime,
                    timing.AbsoluteFinishTime,
                    timing.RelativeFinishTime,
                    timingType);
            }
        }

        // Log Router Node timings
        if (routerTimings.Count > 0)
        {
            _logger.LogTimingGroupHeader("Router Nodes", routerTimings.Count);

            // Sort by absolute start time for chronological order
            var sortedRouterTimings = routerTimings.OrderBy(t => t.Timing.AbsoluteStartTime).ToList();

            foreach (var (routerNode, timing) in sortedRouterTimings)
            {
                var routerName = routerNode.RouterTask?.Name ?? "Unnamed Router";
                var parentInfo = routerNode.ParentId.HasValue
                    ? $" [Parent: {routerNode.ParentId.Value:N}]"
                    : " [Root]";

                var timingType = timing.NodeType.ToString();
                _logger.LogSchedulingRouterNodeTiming(
                    routerName,
                    routerNode.Id,
                    parentInfo,
                    timing.Duration,
                    timing.AbsoluteStartTime,
                    timing.RelativeStartTime,
                    timing.AbsoluteFinishTime,
                    timing.RelativeFinishTime,
                    timingType);
            }
        }

        // Log other node timings if any
        if (otherTimings.Count > 0)
        {
            _logger.LogTimingGroupHeader("Other Nodes", otherTimings.Count);

            foreach (var (node, timing) in otherTimings.OrderBy(t => t.Timing.AbsoluteStartTime))
            {
                var nodeTypeName = node.GetType().Name;
                _logger.LogSchedulingOtherNodeTiming(
                    node.Id,
                    nodeTypeName,
                    timing.Duration,
                    timing.AbsoluteStartTime,
                    timing.AbsoluteFinishTime);
            }
        }

        _logger.LogDetailedTaskTimingCompleted();
    }

    /// <inheritdoc />
    public void LogCriticalPathAnalysis(Guid procedureId, CriticalPathInfo? criticalPathInfo,
        IReadOnlyList<Node>? nodes, IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo)
    {
        if (criticalPathInfo == null || nodes == null || timingInfo == null)
        {
            _logger.LogCriticalPathAnalysisNullWarning(procedureId);
            return;
        }

        _logger.LogCriticalPathAnalysisStart(procedureId);

        // Create a lookup for nodes by ID
        var nodeLookup = nodes.ToDictionary(n => n.Id);

        // Log critical path nodes
        if (criticalPathInfo.CriticalPathNodeIds.Any())
        {
            _logger.LogCriticalPathNodeCount(criticalPathInfo.CriticalPathNodeIds.Count);

            foreach (var nodeId in criticalPathInfo.CriticalPathNodeIds)
                if (nodeLookup.TryGetValue(nodeId, out var node) && timingInfo.TryGetValue(nodeId, out var timing))
                {
                    var nodeName = node switch
                    {
                        TaskNode tn => tn.Task?.Name ?? "Unnamed Task",
                        SkillExecutionNode sn => sn.SkillExecutionTask.Skill.Name ?? "Unnamed Skill",
                        RouterNode rn => rn.RouterTask?.Name ?? "Unnamed Router",
                        _ => "Unknown Node"
                    };

                    _logger.LogSchedulingCriticalPathNode(nodeName, node.Id, timing.Duration);
                }
        }

        // Log parallelism information
        _logger.LogSchedulingPeakParallelism(criticalPathInfo.MaxParallelism, criticalPathInfo.PeakParallelismTime);
    }
}

/// <summary>
///     Source-generated logging methods for SchedulingPhaseLogger following the same pattern as SkillTimingLogger and
///     ExecutionEventLogger.
/// </summary>
public static partial class SchedulingPhaseLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULING_PHASE | Phase={PhaseNumber} | Name={PhaseName} | ProcedureId={ProcedureId} | Status=STARTING")]
    public static partial void LogSchedulingPhaseStart(
        this ILogger logger,
        int phaseNumber,
        string phaseName,
        Guid procedureId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULING_PHASE | Phase={PhaseNumber} | Name={PhaseName} | ProcedureId={ProcedureId} | Status=COMPLETED | Duration={Duration:F2}ms | Details={Details}")]
    public static partial void LogSchedulingPhaseComplete(
        this ILogger logger,
        int phaseNumber,
        string phaseName,
        Guid procedureId,
        double duration,
        string details);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULING_PIPELINE | Status=STARTING | ProcedureId={ProcedureId} | Nodes={NodeCount} | Edges={EdgeCount} | StrictMode={StrictMode} | PreserveOriginal={PreserveOriginal} | IncludeTiming={IncludeTiming}")]
    public static partial void LogSchedulingPipelineStart(
        this ILogger logger,
        Guid procedureId,
        int nodeCount,
        int edgeCount,
        bool strictMode,
        bool preserveOriginal,
        bool includeTiming);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULING_PIPELINE | Status=COMPLETED | ProcedureId={ProcedureId} | TotalDuration={TotalDuration:F2}ms | ScheduleCount={ScheduleCount} | PhaseTimings={PhaseTimings}")]
    public static partial void LogSchedulingPipelineComplete(
        this ILogger logger,
        Guid procedureId,
        double totalDuration,
        int scheduleCount,
        string? phaseTimings);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULING_STATISTICS | ProcedureId={ProcedureId} | NodeCount={NodeCount} | ProcedureSpan={ProcedureSpan:F2}ms | EarliestStart={EarliestStart:F2} | LatestFinish={LatestFinish:F2} | MinDuration={MinDuration:F2}ms | MaxDuration={MaxDuration:F2}ms | AvgDuration={AvgDuration:F2}ms | SumDuration={SumDuration:F2}ms")]
    public static partial void LogSchedulingTimingStatistics(
        this ILogger logger,
        Guid procedureId,
        int nodeCount,
        double procedureSpan,
        double earliestStart,
        double latestFinish,
        double minDuration,
        double maxDuration,
        double avgDuration,
        double sumDuration);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_NODE_TIMING | NodeId={NodeId} | Status=UNKNOWN | Duration={Duration:F2}ms | AbsStart={AbsStart:F2} | AbsFinish={AbsFinish:F2}")]
    public static partial void LogSchedulingNodeTimingUnknown(
        this ILogger logger,
        Guid nodeId,
        double duration,
        double absStart,
        double absFinish);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_TASK_TIMING | Name={TaskName} | NodeId={NodeId} | Parent={ParentInfo} | Duration={Duration:F2}ms | AbsStart={AbsStart:F2} | RelStart={RelStart:F2} | AbsFinish={AbsFinish:F2} | RelFinish={RelFinish:F2} | Type={TimingType}")]
    public static partial void LogSchedulingTaskNodeTiming(
        this ILogger logger,
        string taskName,
        Guid nodeId,
        string parentInfo,
        double duration,
        double absStart,
        double relStart,
        double absFinish,
        double relFinish,
        string timingType);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_SKILL_TIMING | Name={SkillName} | NodeId={NodeId} | AgentId={AgentId} | Parent={ParentInfo} | Duration={Duration:F2}ms | AbsStart={AbsStart:F2} | RelStart={RelStart:F2} | AbsFinish={AbsFinish:F2} | RelFinish={RelFinish:F2} | Type={TimingType}")]
    public static partial void LogSchedulingSkillNodeTiming(
        this ILogger logger,
        string skillName,
        Guid nodeId,
        Guid agentId,
        string parentInfo,
        double duration,
        double absStart,
        double relStart,
        double absFinish,
        double relFinish,
        string timingType);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_OTHER_TIMING | NodeId={NodeId} | NodeType={NodeType} | Duration={Duration:F2}ms | AbsStart={AbsStart:F2} | AbsFinish={AbsFinish:F2}")]
    public static partial void LogSchedulingOtherNodeTiming(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double duration,
        double absStart,
        double absFinish);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "SCHEDULING_CRITICAL_PATH | Name={NodeName} | NodeId={NodeId} | Duration={Duration:F2}ms")]
    public static partial void LogSchedulingCriticalPathNode(
        this ILogger logger,
        string nodeName,
        Guid nodeId,
        double duration);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "SCHEDULING_PARALLELISM | MaxParallelism={MaxParallelism} | PeakTime={PeakTime:F2}")]
    public static partial void LogSchedulingPeakParallelism(
        this ILogger logger,
        int maxParallelism,
        double peakTime);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SCHEDULING_ROUTER_TIMING | Name={RouterName} | NodeId={NodeId} | Parent={ParentInfo} | Duration={Duration:F2}ms | AbsStart={AbsStart:F2} | RelStart={RelStart:F2} | AbsFinish={AbsFinish:F2} | RelFinish={RelFinish:F2} | Type={TimingType}")]
    public static partial void LogSchedulingRouterNodeTiming(
        this ILogger logger,
        string routerName,
        Guid nodeId,
        string parentInfo,
        double duration,
        double absStart,
        double relStart,
        double absFinish,
        double relFinish,
        string timingType);
}