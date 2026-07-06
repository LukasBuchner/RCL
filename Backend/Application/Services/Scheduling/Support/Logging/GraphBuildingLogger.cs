using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for graph building operations using high-performance source-generated logging.
/// </summary>
public static partial class GraphBuildingLogger
{
    /// <summary>
    ///     Logs the start of graph building operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">Total number of nodes in the procedure.</param>
    /// <param name="edgeCount">Total number of edges in the procedure.</param>
    /// <param name="strictMode">Whether strict mode is enabled for graph building.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "GRAPH_BUILD | Phase=START | NodeCount={NodeCount} | EdgeCount={EdgeCount} | StrictMode={StrictMode}")]
    public static partial void LogGraphBuildStart(
        this ILogger logger,
        int nodeCount,
        int edgeCount,
        bool strictMode);

    /// <summary>
    ///     Logs a specific phase during graph building.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="phase">The current phase of graph building (e.g., FILTERING, ANALYZING, MAPPING).</param>
    /// <param name="nodeCount">Number of nodes being processed in this phase.</param>
    /// <param name="edgeCount">Number of edges being processed in this phase.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GRAPH_BUILD | Phase={Phase} | NodeCount={NodeCount} | EdgeCount={EdgeCount}")]
    public static partial void LogGraphBuildPhase(
        this ILogger logger,
        string phase,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Logs the result of skill analysis during graph building.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="analyzedCount">Number of skills successfully analyzed.</param>
    /// <param name="totalCount">Total number of skills attempted to analyze.</param>
    /// <param name="droppedCount">Number of skills dropped due to analysis failures.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "GRAPH_BUILD | Phase=SKILL_ANALYSIS | AnalyzedCount={AnalyzedCount} | TotalCount={TotalCount} | DroppedCount={DroppedCount}")]
    public static partial void LogSkillAnalysisResult(
        this ILogger logger,
        int analyzedCount,
        int totalCount,
        int droppedCount);

    /// <summary>
    ///     Logs a failure during node analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node that failed analysis.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "GRAPH_BUILD | Phase=NODE_ANALYSIS_FAILURE | NodeId={NodeId} | Error={ErrorMessage}")]
    public static partial void LogNodeAnalysisFailure(
        this ILogger logger,
        Guid nodeId,
        string errorMessage);

    /// <summary>
    ///     Logs a warning when an empty graph is detected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">The reason why the graph is empty.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "GRAPH_BUILD | Phase=EMPTY_GRAPH_WARNING | Reason={Reason}")]
    public static partial void LogEmptyGraphWarning(
        this ILogger logger,
        string reason);

    /// <summary>
    ///     Logs the successful completion of graph building.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executableSkillCount">Number of executable skills in the final graph.</param>
    /// <param name="duration">Time taken to build the graph in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "GRAPH_BUILD | Phase=COMPLETE | ExecutableSkillCount={ExecutableSkillCount} | Duration={Duration:F1}ms")]
    public static partial void LogGraphBuildComplete(
        this ILogger logger,
        int executableSkillCount,
        double duration);

    /// <summary>
    ///     Logs how many leafless containers were materialized as zero-extent firing endpoints.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="placeholderCount">Number of zero-extent placeholders materialized.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GRAPH_BUILD | Phase=MATERIALIZE_LEAFLESS | PlaceholderCount={PlaceholderCount}")]
    public static partial void LogLeaflessMaterialized(
        this ILogger logger,
        int placeholderCount);

    /// <summary>
    ///     Logs at Warning level that a dependency edge was dropped in non-strict mode because one
    ///     endpoint resolved to no executable skills; the ordering constraint is not enforced.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceId">The source node ID of the dropped edge.</param>
    /// <param name="targetId">The target node ID of the dropped edge.</param>
    /// <param name="sourceExecCount">Number of executable skills resolved for the source (0 when source is the empty endpoint).</param>
    /// <param name="targetExecCount">Number of executable skills resolved for the target (0 when target is the empty endpoint).</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "GRAPH_BUILD | Phase=DEPENDENCY_DROP | SourceId={SourceId} | TargetId={TargetId} | SourceExecCount={SourceExecCount} | TargetExecCount={TargetExecCount} | Effect=OrderingConstraintNotEnforced")]
    public static partial void LogDependencyEdgeDropped(
        this ILogger logger,
        Guid sourceId,
        Guid targetId,
        int sourceExecCount,
        int targetExecCount);
}