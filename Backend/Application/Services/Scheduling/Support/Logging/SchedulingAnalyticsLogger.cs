using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for scheduling analytics, result logging, schedule planning,
///     router branch filtering, and conditional dependency resolution.
/// </summary>
public static partial class SchedulingAnalyticsLogger
{
    // ── TimingAnalyzer ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs computed timing statistics across all nodes in a single-pass analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes analyzed.</param>
    /// <param name="minDuration">The minimum duration across all nodes.</param>
    /// <param name="maxDuration">The maximum duration across all nodes.</param>
    /// <param name="avgDuration">The average duration across all nodes.</param>
    /// <param name="sumDuration">The sum of all node durations.</param>
    /// <param name="totalSpan">The total procedure span (latest finish minus earliest start).</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Timing statistics: {NodeCount} nodes, duration range [{MinDuration:F2}-{MaxDuration:F2}], average {AvgDuration:F2}, sum {SumDuration:F2}, span {TotalSpan:F2}")]
    public static partial void LogTimingStatisticsCollected(
        this ILogger logger,
        int nodeCount,
        double minDuration,
        double maxDuration,
        double avgDuration,
        double sumDuration,
        double totalSpan);

    /// <summary>
    ///     Logs a warning when no timing information is available for critical path analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No timing information available for critical path analysis")]
    public static partial void LogNoTimingInfoForCriticalPath(
        this ILogger logger);

    /// <summary>
    ///     Logs the result of critical path analysis showing nodes at the procedure end time.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="criticalNodeCount">The number of nodes on the critical path.</param>
    /// <param name="latestFinish">The latest finish time of the procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Critical path analysis: {CriticalNodeCount} nodes finishing at procedure end time {LatestFinish:F2}")]
    public static partial void LogCriticalPathResult(
        this ILogger logger,
        int criticalNodeCount,
        double latestFinish);

    /// <summary>
    ///     Logs the peak parallelism during procedure execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxParallelism">The maximum number of simultaneously running tasks.</param>
    /// <param name="peakTime">The time at which peak parallelism occurs.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Peak parallelism: {MaxParallelism} tasks running simultaneously at time {PeakTime:F2}")]
    public static partial void LogPeakParallelism(
        this ILogger logger,
        int maxParallelism,
        double peakTime);

    /// <summary>
    ///     Logs that no parallel execution periods were found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "No parallel execution periods found (all tasks run sequentially or have zero duration)")]
    public static partial void LogNoParallelExecution(
        this ILogger logger);

    // ── SchedulingResultLogger ──────────────────────────────────────────

    /// <summary>
    ///     Logs that a timing calculation has completed for a triggering entity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The entity that triggered the timing calculation.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Timing calculation completed triggered by entity {EntityId}")]
    public static partial void LogTimingResultsTriggered(
        this ILogger logger,
        Guid entityId);

    /// <summary>
    ///     Logs that no node schedules were returned from scheduling.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No node schedules returned from scheduling calculation")]
    public static partial void LogNoNodeSchedules(
        this ILogger logger);

    /// <summary>
    ///     Logs the overview of a scheduling result including task counts, skill counts, and durations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskCount">The number of task node schedules.</param>
    /// <param name="skillCount">The number of skill node schedules.</param>
    /// <param name="totalDuration">The total duration summed across all schedules.</param>
    /// <param name="procedureSpan">The procedure span from earliest start to latest finish.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Schedule Overview: {TaskCount} tasks, {SkillCount} skills, Total Duration: {TotalDuration:F2}, Procedure Span: {ProcedureSpan:F2}")]
    public static partial void LogScheduleOverview(
        this ILogger logger,
        int taskCount,
        int skillCount,
        double totalDuration,
        double procedureSpan);

    /// <summary>
    ///     Logs critical path candidates that finish at the procedure end time.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of critical path candidate nodes.</param>
    /// <param name="nodeIds">Comma-separated list of candidate node identifiers.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Critical Path Candidates ({Count} nodes): {NodeIds}")]
    public static partial void LogCriticalPathCandidates(
        this ILogger logger,
        int count,
        string nodeIds);

    // ── SchedulePlanner ─────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of schedule planning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of skills to plan.</param>
    /// <param name="currentTime">The current time offset for planning.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SCHEDULE_PLAN | Phase=PLAN_START | SkillCount={SkillCount} | CurrentTime={CurrentTime:F3}s")]
    public static partial void LogSchedulePlanStarted(
        this ILogger logger,
        int skillCount,
        double currentTime);

    /// <summary>
    ///     Logs the successful completion of schedule planning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="currentTime">The current time offset at completion.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "SCHEDULE_PLAN | Phase=PLAN_COMPLETE | Success=true | CurrentTime={CurrentTime:F3}s")]
    public static partial void LogSchedulePlanCompleted(
        this ILogger logger,
        double currentTime);

    /// <summary>
    ///     Logs that the schedule is infeasible.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The infeasibility exception.</param>
    /// <param name="currentTime">The current time offset at failure.</param>
    /// <param name="errorMessage">The error message describing the infeasibility.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "SCHEDULE_PLAN | Phase=PLAN_INFEASIBLE | CurrentTime={CurrentTime:F3}s | Error={ErrorMessage}")]
    public static partial void LogSchedulePlanInfeasible(
        this ILogger logger,
        Exception exception,
        double currentTime,
        string errorMessage);

    /// <summary>
    ///     Logs a schedule model error during planning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The model error exception.</param>
    /// <param name="currentTime">The current time offset at failure.</param>
    /// <param name="errorMessage">The error message describing the model error.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "SCHEDULE_PLAN | Phase=PLAN_MODEL_ERROR | CurrentTime={CurrentTime:F3}s | Error={ErrorMessage}")]
    public static partial void LogSchedulePlanModelError(
        this ILogger logger,
        Exception exception,
        double currentTime,
        string errorMessage);

    /// <summary>
    ///     Logs a general schedule planning failure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="currentTime">The current time offset at failure.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "SCHEDULE_PLAN | Phase=PLAN_FAILED | CurrentTime={CurrentTime:F3}s | Error={ErrorMessage}")]
    public static partial void LogSchedulePlanFailed(
        this ILogger logger,
        Exception exception,
        double currentTime,
        string errorMessage);

    /// <summary>
    ///     Logs an invalid input error during schedule planning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception with invalid input details.</param>
    /// <param name="currentTime">The current time offset at failure.</param>
    /// <param name="errorMessage">The error message describing the invalid input.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "SCHEDULE_PLAN | Phase=PLAN_INVALID_INPUT | CurrentTime={CurrentTime:F3}s | Error={ErrorMessage}")]
    public static partial void LogSchedulePlanInvalidInput(
        this ILogger logger,
        Exception exception,
        double currentTime,
        string errorMessage);

    // ── RouterBranchFilterService ───────────────────────────────────────

    /// <summary>
    ///     Logs that there are no nodes to filter.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "No nodes to filter")]
    public static partial void LogNoNodesToFilter(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of router branch filtering with node and router counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="totalNodes">The total number of nodes to filter.</param>
    /// <param name="routerCount">The number of router nodes found.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Filtering {TotalNodes} nodes with {RouterCount} router nodes")]
    public static partial void LogBranchFilterStarted(
        this ILogger logger,
        int totalNodes,
        int routerCount);

    /// <summary>
    ///     Logs the state of a router being processed during filtering.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="executionState">The execution-time selected branch target (or "null").</param>
    /// <param name="manualSelection">The manually selected branch name (or "null").</param>
    /// <param name="branchCount">The number of branches on the router.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Processing router '{RouterName}' ({RouterId}): SelectedBranchTargetNodeId={ExecutionState}, ManuallySelectedBranch={ManualSelection}, Branches={BranchCount}")]
    public static partial void LogRouterProcessing(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string executionState,
        string manualSelection,
        int branchCount);

    /// <summary>
    ///     Logs details of a branch within a router being processed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="priority">The priority of the branch.</param>
    /// <param name="targetId">The target node identifier (or "null").</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "  Branch: Name={BranchName}, Priority={Priority}, TargetNodeId={TargetId}")]
    public static partial void LogBranchDetail(
        this ILogger logger,
        string branchName,
        int priority,
        string targetId);

    /// <summary>
    ///     Logs a warning about an invalid manual branch selection on a router.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="branchName">The invalid branch name.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Router '{RouterName}' ({RouterId}) has invalid manual selection '{BranchName}', including all branches")]
    public static partial void LogInvalidManualSelection(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string branchName);

    /// <summary>
    ///     Logs that a router has no branch selection and all branches are included.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Router '{RouterName}' ({RouterId}) has no selection, including all branches")]
    public static partial void LogRouterNoSelection(
        this ILogger logger,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs the selected branch for a router.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="branchName">The name of the selected branch.</param>
    /// <param name="reason">The reason for the selection (e.g. "Execution mode", "Manual selection").</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router '{RouterName}' selected branch '{BranchName}' ({Reason})")]
    public static partial void LogRouterBranchSelected(
        this ILogger logger,
        string routerName,
        string branchName,
        string reason);

    /// <summary>
    ///     Logs the include/exclude counts for a router after branch filtering.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="includedCount">The number of included nodes.</param>
    /// <param name="excludedCount">The number of excluded nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Router '{RouterName}': included {IncludedCount} nodes, excluded {ExcludedCount} nodes")]
    public static partial void LogRouterBranchFilterCounts(
        this ILogger logger,
        string routerName,
        int includedCount,
        int excludedCount);

    /// <summary>
    ///     Logs the completion of branch filtering with summary counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="includedCount">The total number of included nodes.</param>
    /// <param name="excludedCount">The total number of excluded nodes.</param>
    /// <param name="selectionCount">The number of router selections recorded.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Filtering complete: {IncludedCount} included, {ExcludedCount} excluded, {SelectionCount} router selections")]
    public static partial void LogBranchFilterCompleted(
        this ILogger logger,
        int includedCount,
        int excludedCount,
        int selectionCount);
}