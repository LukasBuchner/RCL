using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for node timing operations throughout the scheduling pipeline.
/// </summary>
public static partial class NodeTimingLogger
{
    /// <summary>
    ///     Logs the start of applying timing information to nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes to process.</param>
    /// <param name="hasDetailedTiming">Whether detailed timing information is available from schedule plan.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_TIMING | Phase=APPLY_TIMING_START | NodeCount={NodeCount} | HasDetailedTiming={HasDetailedTiming}")]
    public static partial void LogApplyTimingStart(
        this ILogger logger,
        int nodeCount,
        bool hasDetailedTiming);

    /// <summary>
    ///     Logs when detailed timing information is available for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="duration">Duration in seconds.</param>
    /// <param name="startTime">Start time in seconds from procedure start.</param>
    /// <param name="finishTime">Finish time in seconds from procedure start.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_TIMING | Phase=DETAILED_TIMING | NodeId={NodeId} | Duration={Duration:F3}s | StartTime={StartTime:F3}s | FinishTime={FinishTime:F3}s")]
    public static partial void LogDetailedTimingAvailable(
        this ILogger logger,
        Guid nodeId,
        double duration,
        double startTime,
        double finishTime);

    /// <summary>
    ///     Logs when falling back to duration-only timing for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="duration">Duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_TIMING | Phase=DURATION_FALLBACK | NodeId={NodeId} | Duration={Duration:F3}s")]
    public static partial void LogDurationOnlyFallback(
        this ILogger logger,
        Guid nodeId,
        double duration);

    /// <summary>
    ///     Logs at Warning level when a node receives duration-only timing because detailed scheduler
    ///     timing is absent; the original StartTime is kept and only FinishTime is recomputed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The CLR type name of the node.</param>
    /// <param name="duration">The duration applied in seconds.</param>
    /// <param name="startTime">The original StartTime retained in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "NODE_TIMING | Phase=DURATION_FALLBACK | NodeId={NodeId} | NodeType={NodeType} | Detailed scheduler timing missing; applied duration-only ({Duration:F3}s) keeping original StartTime={StartTime:F3}s (FinishTime recomputed) instead of scheduler-computed times")]
    public static partial void LogDurationOnlyFallbackWarning(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double duration,
        double startTime);

    /// <summary>
    ///     Logs at Warning level when a node has neither detailed timing nor a duration entry and
    ///     is returned completely unmodified; no timing was applied.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The CLR type name of the node.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "NODE_TIMING | Phase=NO_TIMING_APPLIED | NodeId={NodeId} | NodeType={NodeType} | No detailed timing and no duration entry found; node returned unmodified with original timing intact")]
    public static partial void LogNoTimingApplied(
        this ILogger logger,
        Guid nodeId,
        string nodeType);

    /// <summary>
    ///     Logs the start of relative time adjustment for nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes to adjust.</param>
    /// <param name="earliestStartTime">The earliest start time used as baseline in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_TIMING | Phase=RELATIVE_TIME_START | NodeCount={NodeCount} | EarliestStartTime={EarliestStartTime:F3}s")]
    public static partial void LogRelativeTimeAdjustmentStart(
        this ILogger logger,
        int nodeCount,
        double earliestStartTime);

    /// <summary>
    ///     Logs the completion of relative time adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes processed.</param>
    /// <param name="adjustedCount">The number of nodes successfully adjusted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "NODE_TIMING | Phase=RELATIVE_TIME_COMPLETE | NodeCount={NodeCount} | AdjustedCount={AdjustedCount}")]
    public static partial void LogRelativeTimeAdjustmentComplete(
        this ILogger logger,
        int nodeCount,
        int adjustedCount);

    /// <summary>
    ///     Logs timing information applied to a specific node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The type of the node (e.g., TaskNode, SkillExecutionNode).</param>
    /// <param name="duration">Duration in seconds.</param>
    /// <param name="absoluteStart">Absolute start time in seconds from procedure start (nullable).</param>
    /// <param name="relativeStart">Relative start time in seconds from earliest start (nullable).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_TIMING | Phase=TIMING_APPLIED | NodeId={NodeId} | NodeType={NodeType} | Duration={Duration:F3}s | AbsoluteStart={AbsoluteStart:F3}s | RelativeStart={RelativeStart:F3}s")]
    public static partial void LogTimingAppliedToNode(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double duration,
        double? absoluteStart,
        double? relativeStart);

    /// <summary>
    ///     Logs timing information with execution progress for a skill node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="progress">Execution progress percentage (0-100).</param>
    /// <param name="duration">Duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_TIMING | Phase=TIMING_WITH_PROGRESS | NodeId={NodeId} | SkillName={SkillName} | Progress={Progress:F1}% | Duration={Duration:F3}s")]
    public static partial void LogTimingWithProgress(
        this ILogger logger,
        Guid nodeId,
        string skillName,
        double progress,
        double duration);

    // ── TimingCalculationEngine ─────────────────────────────────────────

    /// <summary>
    ///     Logs the start of timing calculation for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting timing calculation for procedure {ProcedureId}")]
    public static partial void LogTimingCalculationStarted(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that no skill execution nodes were found for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No skill execution nodes found for procedure {ProcedureId}")]
    public static partial void LogNoSkillExecutionNodes(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that building the execution graph failed for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to build execution graph for procedure {ProcedureId} - applying fallback duration adjustments")]
    public static partial void LogExecutionGraphBuildFailed(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs a warning that an empty execution graph was produced and the fallback path is being used. The
    ///     fallback returns original node durations without scheduler-computed detailed timing, so downstream
    ///     positioning has no per-node start times and falls back to non-time-based (id) ordering; the displayed
    ///     schedule is therefore not authoritative.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Empty execution graph for procedure {ProcedureId} - using fallback (no detailed timing; displayed schedule positions are not scheduler-derived)")]
    public static partial void LogEmptyExecutionGraph(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that schedule planning failed for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to plan schedule for procedure {ProcedureId}")]
    public static partial void LogSchedulePlanningFailed(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the successful completion of timing calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully completed timing calculation for procedure {ProcedureId}")]
    public static partial void LogTimingCalculationCompleted(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that the schedule is infeasible for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The infeasibility exception.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Schedule infeasible for procedure {ProcedureId}")]
    public static partial void LogScheduleInfeasible(
        this ILogger logger,
        Exception exception,
        Guid procedureId);

    /// <summary>
    ///     Logs a schedule model error for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The model error exception.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Schedule model error for procedure {ProcedureId}")]
    public static partial void LogScheduleModelError(
        this ILogger logger,
        Exception exception,
        Guid procedureId);

    /// <summary>
    ///     Logs that a scheduling operation failed for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Scheduling operation failed for procedure {ProcedureId}")]
    public static partial void LogSchedulingOperationFailed(
        this ILogger logger,
        Exception exception,
        Guid procedureId);

    /// <summary>
    ///     Logs the start time calculated for a router node from incoming edges.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="edgeCount">The number of incoming edges used.</param>
    /// <param name="startTime">The calculated start time.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "RouterNode {RouterNodeId} start time calculated from {EdgeCount} incoming edge(s): {StartTime}")]
    public static partial void LogRouterNodeStartTimeFromEdges(
        this ILogger logger,
        Guid routerNodeId,
        int edgeCount,
        double startTime);

    /// <summary>
    ///     Logs the timing added for a router node to the detailed timing info.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="finishTime">The finish time.</param>
    /// <param name="duration">The duration.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Added RouterNode {RouterNodeId} timing to detailedTimingInfo: Start={StartTime}, Finish={FinishTime}, Duration={Duration}")]
    public static partial void LogRouterNodeTimingAdded(
        this ILogger logger,
        Guid routerNodeId,
        double startTime,
        double finishTime,
        double duration);

    /// <summary>
    ///     Logs the zero-extent placement of a standalone leafless task node at its predecessor-driven start.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The standalone empty task node identifier.</param>
    /// <param name="startTime">The predecessor-based start time at which the task is pinned with zero extent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Standalone leafless TaskNode {TaskNodeId} placed as zero-extent point at Start={StartTime}")]
    public static partial void LogStandaloneEmptyTaskTiming(
        this ILogger logger,
        Guid taskNodeId,
        double startTime);

    // ── TaskNodeDurationCalculator ──────────────────────────────────────

    /// <summary>
    ///     Logs that a task node has no child nodes and returns null.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "TaskNode {TaskNodeId} has no child nodes, returning null")]
    public static partial void LogTaskNodeNoChildren(
        this ILogger logger,
        Guid taskNodeId);

    /// <summary>
    ///     Logs that a task node has children but none have calculated timings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "TaskNode {TaskNodeId} has child nodes but none have calculated timings, returning null")]
    public static partial void LogTaskNodeNoChildTimings(
        this ILogger logger,
        Guid taskNodeId);

    /// <summary>
    ///     Logs the calculated duration for a task node based on its children.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The calculated duration.</param>
    /// <param name="start">The earliest child start time.</param>
    /// <param name="finish">The latest child finish time.</param>
    /// <param name="skillChildren">The number of skill children with timings.</param>
    /// <param name="taskChildren">The number of task children with timings.</param>
    /// <param name="routerChildren">The number of router children with timings.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculated duration for TaskNode {TaskNodeId}: {Duration} (from start={Start} to finish={Finish}, {SkillChildren} skill + {TaskChildren} task + {RouterChildren} router children)")]
    public static partial void LogTaskNodeDurationCalculated(
        this ILogger logger,
        Guid taskNodeId,
        double duration,
        double start,
        double finish,
        int skillChildren,
        int taskChildren,
        int routerChildren);

    /// <summary>
    ///     Logs the start of calculating durations for container task nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerCount">The number of container task nodes.</param>
    /// <param name="totalCount">The total number of task nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculating durations for {ContainerCount} container TaskNodes out of {TotalCount} total (hierarchical order)")]
    public static partial void LogContainerDurationCalculationStarted(
        this ILogger logger,
        int containerCount,
        int totalCount);

    /// <summary>
    ///     Logs that a container task node's duration was calculated from its children.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The calculated duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Container TaskNode {TaskNodeId} duration calculated as {Duration} based on children")]
    public static partial void LogContainerTaskNodeDuration(
        this ILogger logger,
        Guid taskNodeId,
        double duration);

    /// <summary>
    ///     Logs that a non-container task node's duration was calculated but excluded from results.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The calculated duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Child TaskNode {TaskNodeId} duration calculated as {Duration} but excluded from results (not a container)")]
    public static partial void LogChildTaskNodeDurationExcluded(
        this ILogger logger,
        Guid taskNodeId,
        double duration);

    /// <summary>
    ///     Logs that a container task node is using its original duration because no child timings are available.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The original duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Container TaskNode {TaskNodeId} using original duration {Duration} (no child timings available)")]
    public static partial void LogContainerTaskNodeOriginalDuration(
        this ILogger logger,
        Guid taskNodeId,
        double duration);

    /// <summary>
    ///     Logs the completion of container task node duration calculations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerCount">The number of container task nodes.</param>
    /// <param name="totalCount">The total number of task nodes.</param>
    /// <param name="adjustedCount">The number of task nodes adjusted from child timings.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculated durations for {ContainerCount} container TaskNodes out of {TotalCount} total ({AdjustedCount} adjusted based on children)")]
    public static partial void LogContainerDurationCalculationCompleted(
        this ILogger logger,
        int containerCount,
        int totalCount,
        int adjustedCount);

    /// <summary>
    ///     Logs the start of calculating complete schedules for task nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of task nodes to process.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculating complete schedules for {Count} TaskNodes based on child timings (processing in hierarchical order)")]
    public static partial void LogTaskNodeScheduleCalculationStarted(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs the hierarchical processing order header.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Hierarchical processing order:")]
    public static partial void LogHierarchicalProcessingOrderHeader(
        this ILogger logger);

    /// <summary>
    ///     Logs a task node entry in the hierarchical processing order.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="index">The processing index (1-based).</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="taskName">The task name.</param>
    /// <param name="parentId">The parent identifier (or "null").</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "  {Index}: TaskNode {TaskNodeId} '{TaskName}' (ParentId: {ParentId})")]
    public static partial void LogHierarchicalProcessingOrderEntry(
        this ILogger logger,
        int index,
        Guid taskNodeId,
        string taskName,
        string parentId);

    /// <summary>
    ///     Logs the children found for a task node during schedule calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="taskName">The task name.</param>
    /// <param name="skillChildCount">The number of skill children.</param>
    /// <param name="taskChildCount">The number of task children.</param>
    /// <param name="routerChildCount">The number of router children.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "TaskNode {TaskNodeId} '{TaskName}': Found {SkillChildCount} skill children, {TaskChildCount} task children, {RouterChildCount} router children")]
    public static partial void LogTaskNodeChildrenFound(
        this ILogger logger,
        Guid taskNodeId,
        string taskName,
        int skillChildCount,
        int taskChildCount,
        int routerChildCount);

    /// <summary>
    ///     Logs the timing availability for a child task node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="childId">The child task node identifier.</param>
    /// <param name="childName">The child task name.</param>
    /// <param name="hasTiming">Whether timing data is available for the child.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "  Child TaskNode {ChildId} '{ChildName}': HasTiming={HasTiming}")]
    public static partial void LogChildTaskNodeTimingAvailability(
        this ILogger logger,
        Guid childId,
        string childName,
        bool hasTiming);

    /// <summary>
    ///     Logs the timing details of a child task node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="start">The start time.</param>
    /// <param name="finish">The finish time.</param>
    /// <param name="duration">The duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "    Child timing: Start={Start}, Finish={Finish}, Duration={Duration}")]
    public static partial void LogChildTaskNodeTimingDetails(
        this ILogger logger,
        double start,
        double finish,
        double duration);

    /// <summary>
    ///     Logs that timing was found for a skill child node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="childId">The skill child node identifier.</param>
    /// <param name="start">The start time.</param>
    /// <param name="finish">The finish time.</param>
    /// <param name="duration">The duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "  Found timing for skill child {ChildId}: Start={Start}, Finish={Finish}, Duration={Duration}")]
    public static partial void LogSkillChildTimingFound(
        this ILogger logger,
        Guid childId,
        double start,
        double finish,
        double duration);

    /// <summary>
    ///     Logs that no timing was found for a skill child node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="childId">The skill child node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "  No timing found for skill child {ChildId}")]
    public static partial void LogSkillChildTimingNotFound(
        this ILogger logger,
        Guid childId);

    /// <summary>
    ///     Logs the calculated schedule for a task node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The calculated duration.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="finishTime">The finish time.</param>
    /// <param name="skillChildCount">The number of skill children used.</param>
    /// <param name="taskChildCount">The number of task children used.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "TaskNode {TaskNodeId} schedule calculated: Duration={Duration}, Start={StartTime}, Finish={FinishTime} (based on {SkillChildCount} skill + {TaskChildCount} task children)")]
    public static partial void LogTaskNodeScheduleCalculated(
        this ILogger logger,
        Guid taskNodeId,
        double duration,
        double startTime,
        double finishTime,
        int skillChildCount,
        int taskChildCount);

    /// <summary>
    ///     Logs that a task node is using its original schedule with no child timings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The original duration.</param>
    /// <param name="finishTime">The calculated finish time.</param>
    /// <param name="reason">The reason for using the original schedule.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "TaskNode {TaskNodeId} using original schedule: Duration={Duration}, Start=0, Finish={FinishTime} ({Reason})")]
    public static partial void LogTaskNodeOriginalSchedule(
        this ILogger logger,
        Guid taskNodeId,
        double duration,
        double finishTime,
        string reason);

    /// <summary>
    ///     Logs the completion of task node schedule calculations with adjusted count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The total number of task nodes.</param>
    /// <param name="adjustedCount">The number of task nodes adjusted from child data.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculated complete schedules for {Count} TaskNodes ({AdjustedCount} adjusted based on children)")]
    public static partial void LogTaskNodeScheduleCalculationCompleted(
        this ILogger logger,
        int count,
        int adjustedCount);

    // ── RouterNodeDurationCalculator ────────────────────────────────────

    /// <summary>
    ///     Logs that a router node has no selected branch and is using the max branch duration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The max branch duration being used.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "RouterNode {RouterNodeId} has no selected branch, using max branch duration: {Duration}")]
    public static partial void LogRouterNodeMaxBranchDuration(
        this ILogger logger,
        Guid routerNodeId,
        double duration);

    /// <summary>
    ///     Logs that a router node has no selected branch and no branch timings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "RouterNode {RouterNodeId} has no selected branch and no branch timings available, returning null")]
    public static partial void LogRouterNodeNoBranchTimings(
        this ILogger logger,
        Guid routerNodeId);

    /// <summary>
    ///     Logs that a router node's selected target has no calculated timing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="targetNodeId">The selected target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "RouterNode {RouterNodeId} selected target node {TargetNodeId} has no calculated timing, returning null")]
    public static partial void LogRouterNodeTargetNoTiming(
        this ILogger logger,
        Guid routerNodeId,
        Guid targetNodeId);

    /// <summary>
    ///     Logs the calculated duration for a router node from its selected target.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The calculated duration.</param>
    /// <param name="targetNodeId">The selected target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculated duration for RouterNode {RouterNodeId}: {Duration} (from selected target node {TargetNodeId})")]
    public static partial void LogRouterNodeDurationFromTarget(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        Guid targetNodeId);

    /// <summary>
    ///     Logs the start of calculating complete schedules for router nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of router nodes to process.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculating complete schedules for {Count} RouterNodes based on selected branch target timings")]
    public static partial void LogRouterNodeScheduleCalculationStarted(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs a router node using the max branch schedule when no branch is selected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="finishTime">The finish time.</param>
    /// <param name="branchCount">The number of branches evaluated.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} using max branch schedule: Duration={Duration}, Start={StartTime}, Finish={FinishTime} (no selected branch, {BranchCount} branches evaluated)")]
    public static partial void LogRouterNodeMaxBranchSchedule(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime,
        int branchCount);

    /// <summary>
    ///     Logs a router node using its original schedule when no branch timings are available.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="finishTime">The finish time.</param>
    /// <param name="reason">The reason for using the original schedule.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} using original schedule: Duration={Duration}, Start={StartTime}, Finish={FinishTime} ({Reason})")]
    public static partial void LogRouterNodeOriginalSchedule(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime,
        string reason);

    /// <summary>
    ///     Logs the calculated schedule for a router node from its selected target.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="finishTime">The finish time.</param>
    /// <param name="targetNodeId">The selected target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} schedule calculated: Duration={Duration}, Start={StartTime}, Finish={FinishTime} (from selected target node {TargetNodeId})")]
    public static partial void LogRouterNodeScheduleFromTarget(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime,
        Guid targetNodeId);

    /// <summary>
    ///     Logs the completion of router node schedule calculations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The total number of router nodes.</param>
    /// <param name="adjustedCount">The number adjusted from selected branches.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Calculated complete schedules for {Count} RouterNodes ({AdjustedCount} adjusted based on selected branches)")]
    public static partial void LogRouterNodeScheduleCalculationCompleted(
        this ILogger logger,
        int count,
        int adjustedCount);

    /// <summary>
    ///     Logs a router node using an execution-time selection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="targetNodeId">The selected target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} using execution-time selection: Target {TargetNodeId}")]
    public static partial void LogRouterNodeExecutionTimeSelection(
        this ILogger logger,
        Guid routerNodeId,
        Guid targetNodeId);

    /// <summary>
    ///     Logs a router node using an execution state selection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="targetNodeId">The selected target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} using execution state selection: Target {TargetNodeId}")]
    public static partial void LogRouterNodeExecutionStateSelection(
        this ILogger logger,
        Guid routerNodeId,
        Guid targetNodeId);

    /// <summary>
    ///     Logs a router node using a manual branch selection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="branchName">The manually selected branch name.</param>
    /// <param name="targetNodeId">The target node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} using manual selection '{BranchName}': Target {TargetNodeId}")]
    public static partial void LogRouterNodeManualSelection(
        this ILogger logger,
        Guid routerNodeId,
        string branchName,
        Guid targetNodeId);

    /// <summary>
    ///     Logs that a router node's manual selection has no valid target.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="branchName">The manually selected branch name.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "RouterNode {RouterNodeId} has invalid manual selection '{BranchName}' - no target found")]
    public static partial void LogRouterNodeInvalidManualSelection(
        this ILogger logger,
        Guid routerNodeId,
        string branchName);

    /// <summary>
    ///     Logs that a router node has no selection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "RouterNode {RouterNodeId} has no selection")]
    public static partial void LogRouterNodeNoSelection(
        this ILogger logger,
        Guid routerNodeId);

    /// <summary>
    ///     Logs at Warning level that a router node fell back to its stored design-time duration because
    ///     no selected branch was present and no branch target timings were available.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The design-time duration used.</param>
    /// <param name="startTime">The design-time start time used.</param>
    /// <param name="finishTime">The computed finish time (startTime + duration).</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Router {RouterNodeId} schedule fell back to stored design-time duration (Duration={Duration}, Start={StartTime}, Finish={FinishTime}) because no selected branch and no branch target timings were available")]
    public static partial void LogRouterNodeScheduleFellBackToStored(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime);

    /// <summary>
    ///     Logs at Warning level that a router node fell back to its stored design-time duration because
    ///     the selected branch target had no calculated timing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The design-time duration used.</param>
    /// <param name="startTime">The design-time start time used.</param>
    /// <param name="finishTime">The computed finish time (startTime + duration).</param>
    /// <param name="selectedTargetNodeId">The selected branch target node whose timing was absent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Router {RouterNodeId} schedule fell back to stored design-time duration (Duration={Duration}, Start={StartTime}, Finish={FinishTime}) because selected branch target {SelectedTargetNodeId} had no calculated timing")]
    public static partial void LogRouterNodeSelectedTargetTimingMissing(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime,
        Guid selectedTargetNodeId);

    /// <summary>
    ///     Logs at Warning level that a container task node fell back to its stored design-time duration
    ///     because no child timing could be calculated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The design-time duration substituted.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Container task {TaskNodeId} fell back to stored design-time duration {Duration} because no child timing could be calculated")]
    public static partial void LogContainerTaskNodeDurationFellBackToStored(
        this ILogger logger,
        Guid taskNodeId,
        double duration);

    /// <summary>
    ///     Logs at Warning level that a task node with children fell back to its stored design-time duration
    ///     anchored at time 0 because none of its children had calculated timings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The task node identifier.</param>
    /// <param name="duration">The design-time duration substituted.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Task {TaskNodeId} schedule fell back to stored design-time duration {Duration} anchored at time 0 because its children carried no calculated timings")]
    public static partial void LogTaskNodeScheduleFellBackToStored(
        this ILogger logger,
        Guid taskNodeId,
        double duration);
}