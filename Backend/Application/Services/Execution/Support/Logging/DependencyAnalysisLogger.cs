using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides structured logging for dependency analysis during execution pipeline initialization.
/// </summary>
public static partial class DependencyAnalysisLogger
{
    /// <summary>
    ///     Logs comprehensive dependency analysis information for a skill using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="startPrereqCount">Number of prerequisites that must complete before this skill can start.</param>
    /// <param name="finishPrereqCount">Number of prerequisites that must complete before this skill can finish.</param>
    /// <param name="isImmediateStart">Whether the skill can start immediately without waiting for prerequisites.</param>
    /// <param name="isAdaptive">Whether the skill supports adaptive duration.</param>
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Trace,
        Message = "DEPENDENCY_ANALYSIS | SkillId={SkillId} | Name='{SkillName}' | " +
                  "StartPrereqCount={StartPrereqCount} | FinishPrereqCount={FinishPrereqCount} | " +
                  "IsImmediateStart={IsImmediateStart} | IsAdaptive={IsAdaptive}")]
    public static partial void LogDependencyAnalysis(
        this ILogger logger,
        Guid skillId,
        string skillName,
        int startPrereqCount,
        int finishPrereqCount,
        bool isImmediateStart,
        bool isAdaptive);

    /// <summary>
    ///     Logs a start prerequisite for a skill using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dependsOnSkillId">The unique identifier of the prerequisite skill.</param>
    /// <param name="dependsOnSkillName">The name of the prerequisite skill.</param>
    /// <param name="requiredEvent">The required event type (e.g., START or FINISH).</param>
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Trace,
        Message = "DEPENDENCY_ANALYSIS | START_PREREQUISITE | DependsOnSkillId={DependsOnSkillId} | " +
                  "DependsOnSkillName='{DependsOnSkillName}' | RequiredEvent={RequiredEvent}")]
    public static partial void LogStartPrerequisite(
        this ILogger logger,
        Guid dependsOnSkillId,
        string dependsOnSkillName,
        string requiredEvent);

    /// <summary>
    ///     Logs a finish prerequisite for a skill using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dependsOnSkillId">The unique identifier of the prerequisite skill.</param>
    /// <param name="dependsOnSkillName">The name of the prerequisite skill.</param>
    /// <param name="requiredEvent">The required event type (e.g., START or FINISH).</param>
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Trace,
        Message = "DEPENDENCY_ANALYSIS | FINISH_PREREQUISITE | DependsOnSkillId={DependsOnSkillId} | " +
                  "DependsOnSkillName='{DependsOnSkillName}' | RequiredEvent={RequiredEvent}")]
    public static partial void LogFinishPrerequisite(
        this ILogger logger,
        Guid dependsOnSkillId,
        string dependsOnSkillName,
        string requiredEvent);

    /// <summary>
    ///     Logs completion statistics for dependency graph analysis using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="totalSkills">Total number of skills analyzed.</param>
    /// <param name="immediateStartCount">Number of skills that can start immediately.</param>
    /// <param name="adaptiveCount">Number of skills with adaptive duration.</param>
    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message =
            "Dependency analysis complete: {TotalSkills} skills, {ImmediateStartCount} can start immediately, {AdaptiveCount} are adaptive")]
    public static partial void LogDependencyGraphComplete(
        this ILogger logger,
        int totalSkills,
        int immediateStartCount,
        int adaptiveCount);

    /// <summary>
    ///     Logs detection of a dependency cycle using high-performance source-generated logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillIds">Comma-separated list of skill IDs forming the cycle.</param>
    /// <param name="message">Descriptive message about the cycle.</param>
    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Warning,
        Message = "DEPENDENCY_ANALYSIS | CYCLE_DETECTED | SkillIds={SkillIds} | Message={Message}")]
    public static partial void LogDependencyCycle(
        this ILogger logger,
        string skillIds,
        string message);

    // ──────────────────────────────────────────────────
    //  DependencyGraphAnalyzer — detailed analysis
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of dependency analysis for a set of nodes and edges.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes being analyzed.</param>
    /// <param name="edgeCount">The total number of edges being analyzed.</param>
    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Debug,
        Message = "Analyzing dependencies for {NodeCount} nodes and {EdgeCount} edges")]
    public static partial void LogAnalyzingDependencies(
        this ILogger logger,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Logs the processed hierarchy counts after node hierarchy processing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskCount">The number of TaskNodes in the hierarchy.</param>
    /// <param name="skillCount">The number of SkillExecutionNodes in the hierarchy.</param>
    /// <param name="routerCount">The number of RouterNodes in the hierarchy.</param>
    [LoggerMessage(
        EventId = 3011,
        Level = LogLevel.Debug,
        Message = "Processed hierarchy: {TaskCount} tasks, {SkillCount} skills, {RouterCount} routers")]
    public static partial void LogHierarchyProcessed(
        this ILogger logger,
        int taskCount,
        int skillCount,
        int routerCount);

    /// <summary>
    ///     Logs the number of incoming edges for a router node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="edgeCount">The number of incoming edges.</param>
    [LoggerMessage(
        EventId = 3012,
        Level = LogLevel.Trace,
        Message = "Router {RouterName} ({RouterId}) has {EdgeCount} incoming edges")]
    public static partial void LogRouterIncomingEdges(
        this ILogger logger,
        string routerName,
        Guid routerId,
        int edgeCount);

    /// <summary>
    ///     Logs that a router depends on a specific skill with a given event type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="sourceSkillId">The unique identifier of the source skill.</param>
    /// <param name="eventType">The required event type (Start or Finish).</param>
    [LoggerMessage(
        EventId = 3013,
        Level = LogLevel.Trace,
        Message = "  Router {RouterId} depends on skill {SourceSkillId} ({EventType})")]
    public static partial void LogRouterDependsOnSkill(
        this ILogger logger,
        Guid routerId,
        Guid sourceSkillId,
        string eventType);

    /// <summary>
    ///     Logs that a router is nested inside another router and will receive an ancestor prerequisite.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the nested router.</param>
    /// <param name="routerId">The unique identifier of the nested router.</param>
    /// <param name="ancestorRouterName">The name of the ancestor router.</param>
    /// <param name="ancestorRouterId">The unique identifier of the ancestor router.</param>
    [LoggerMessage(
        EventId = 3014,
        Level = LogLevel.Trace,
        Message =
            "Router {RouterName} ({RouterId}) is nested inside router {AncestorRouterName} ({AncestorRouterId}) - adding ancestor Router.Start prerequisite")]
    public static partial void LogNestedRouterPrerequisite(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string ancestorRouterName,
        Guid ancestorRouterId);

    /// <summary>
    ///     Logs the final start prerequisites count for a router.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="startPrereqCount">The number of start prerequisites.</param>
    /// <param name="canStartImmediately">Whether the router can start immediately.</param>
    [LoggerMessage(
        EventId = 3015,
        Level = LogLevel.Trace,
        Message =
            "Router {RouterName} ({RouterId}): {StartPrereqCount} start prerequisites, can start immediately: {CanStartImmediately}")]
    public static partial void LogRouterPrerequisitesComplete(
        this ILogger logger,
        string routerName,
        Guid routerId,
        int startPrereqCount,
        bool canStartImmediately);

    /// <summary>
    ///     Logs the number of incoming edges for a skill (excluding router ancestors).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="edgeCount">The number of incoming edges.</param>
    [LoggerMessage(
        EventId = 3016,
        Level = LogLevel.Trace,
        Message = "Skill {SkillName} ({SkillId}) has {EdgeCount} incoming edges (excluding router ancestors)")]
    public static partial void LogSkillIncomingEdges(
        this ILogger logger,
        string skillName,
        Guid skillId,
        int edgeCount);

    /// <summary>
    ///     Logs details of an edge being processed for a skill.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceId">The source node ID of the edge.</param>
    /// <param name="sourceHandle">The source handle of the edge.</param>
    /// <param name="targetId">The target node ID of the edge.</param>
    /// <param name="targetHandle">The target handle of the edge.</param>
    [LoggerMessage(
        EventId = 3017,
        Level = LogLevel.Trace,
        Message = "  Processing Edge: Source={SourceId} ({SourceHandle}) -> Target={TargetId} ({TargetHandle})")]
    public static partial void LogProcessingEdge(
        this ILogger logger,
        Guid sourceId,
        string sourceHandle,
        Guid targetId,
        string targetHandle);

    /// <summary>
    ///     Logs the number of actual skills resolved from a source node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of resolved skills.</param>
    [LoggerMessage(
        EventId = 3018,
        Level = LogLevel.Trace,
        Message = "    Resolved source to {SkillCount} actual skill(s)")]
    public static partial void LogResolvedSourceSkills(
        this ILogger logger,
        int skillCount);

    /// <summary>
    ///     Logs detailed event type mapping for a resolved source skill.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceSkillId">The resolved source skill ID.</param>
    /// <param name="sourceEvent">The source event type.</param>
    /// <param name="targetEvent">The target event type.</param>
    [LoggerMessage(
        EventId = 3019,
        Level = LogLevel.Trace,
        Message = "      SourceSkillId={SourceSkillId} | SourceEvent={SourceEvent} | TargetEvent={TargetEvent}")]
    public static partial void LogEventTypeMapping(
        this ILogger logger,
        Guid sourceSkillId,
        string sourceEvent,
        string targetEvent);

    /// <summary>
    ///     Logs that a skill is inside a router and will receive a Router.Start prerequisite.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="routerName">The name of the ancestor router.</param>
    /// <param name="routerId">The unique identifier of the ancestor router.</param>
    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Trace,
        Message =
            "Skill {SkillName} ({SkillId}) is inside router {RouterName} ({RouterId}) - adding Router.Start prerequisite")]
    public static partial void LogSkillInsideRouter(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string routerName,
        Guid routerId);

    /// <summary>
    ///     Logs a start prerequisite for a router dependency.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the prerequisite router.</param>
    /// <param name="routerId">The unique identifier of the prerequisite router.</param>
    /// <param name="eventType">The required event type.</param>
    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Trace,
        Message = "    START_PREREQ: Router {RouterName} ({RouterId}) {EventType}")]
    public static partial void LogStartPrereqRouter(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string eventType);

    /// <summary>
    ///     Logs the number of target IDs for a skill including self and ancestors.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="ancestorCount">The count of target IDs including self and all ancestors.</param>
    [LoggerMessage(
        EventId = 3022,
        Level = LogLevel.Trace,
        Message = "Skill {SkillId} has {AncestorCount} target IDs (including self and ancestors)")]
    public static partial void LogSkillAncestorCount(
        this ILogger logger,
        Guid skillId,
        int ancestorCount);

    /// <summary>
    ///     Logs the start of ancestor router search for a node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="parentId">The parent ID of the node, or "null" if none.</param>
    [LoggerMessage(
        EventId = 3023,
        Level = LogLevel.Trace,
        Message =
            "FindAncestorRouter: Searching for router ancestor of {NodeType} {NodeName} ({NodeId}), ParentId={ParentId}")]
    public static partial void LogFindAncestorRouterStart(
        this ILogger logger,
        string nodeType,
        string nodeName,
        Guid nodeId,
        string parentId);

    /// <summary>
    ///     Logs that a parent node was not found during ancestor router search.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="parentId">The unique identifier of the missing parent.</param>
    /// <param name="depth">The depth in the hierarchy when the parent was not found.</param>
    /// <param name="nodeCount">The total number of nodes in the lookup dictionary.</param>
    /// <param name="taskCount">The number of TaskNodes in the lookup.</param>
    /// <param name="skillCount">The number of SkillExecutionNodes in the lookup.</param>
    /// <param name="routerCount">The number of RouterNodes in the lookup.</param>
    [LoggerMessage(
        EventId = 3024,
        Level = LogLevel.Warning,
        Message = "FindAncestorRouter: Parent {ParentId} not found in allNodesById at depth {Depth}. " +
                  "AllNodesById contains {NodeCount} nodes (TaskNodes: {TaskCount}, Skills: {SkillCount}, Routers: {RouterCount})")]
    public static partial void LogParentNotFoundInHierarchy(
        this ILogger logger,
        Guid parentId,
        int depth,
        int nodeCount,
        int taskCount,
        int skillCount,
        int routerCount);

    /// <summary>
    ///     Logs a parent node found at a specific depth during ancestor router search.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="depth">The depth in the hierarchy.</param>
    /// <param name="parentType">The type name of the parent node.</param>
    /// <param name="parentName">The name of the parent node.</param>
    /// <param name="parentId">The unique identifier of the parent node.</param>
    [LoggerMessage(
        EventId = 3025,
        Level = LogLevel.Trace,
        Message = "FindAncestorRouter: At depth {Depth}, found parent {ParentType} {ParentName} ({ParentId})")]
    public static partial void LogFoundParentAtDepth(
        this ILogger logger,
        int depth,
        string parentType,
        string parentName,
        Guid parentId);

    /// <summary>
    ///     Logs the successful discovery of a router ancestor.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the found router.</param>
    /// <param name="routerId">The unique identifier of the found router.</param>
    /// <param name="depth">The depth at which the router was found.</param>
    [LoggerMessage(
        EventId = 3026,
        Level = LogLevel.Trace,
        Message = "FindAncestorRouter: Found router {RouterName} ({RouterId}) at depth {Depth}")]
    public static partial void LogFoundAncestorRouter(
        this ILogger logger,
        string routerName,
        Guid routerId,
        int depth);

    /// <summary>
    ///     Logs that no router ancestor was found after searching a given number of hierarchy levels.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="depth">The number of hierarchy levels searched.</param>
    [LoggerMessage(
        EventId = 3027,
        Level = LogLevel.Trace,
        Message =
            "FindAncestorRouter: No router ancestor found for {NodeType} {NodeName} ({NodeId}) after {Depth} levels")]
    public static partial void LogNoAncestorRouterFound(
        this ILogger logger,
        string nodeType,
        string nodeName,
        Guid nodeId,
        int depth);

    /// <summary>
    ///     Logs that a source node was not found in the hierarchy during dependency analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceId">The unique identifier of the missing source node.</param>
    [LoggerMessage(
        EventId = 3028,
        Level = LogLevel.Warning,
        Message = "Source node {SourceId} not found in hierarchy")]
    public static partial void LogSourceNodeNotFound(
        this ILogger logger,
        Guid sourceId);
}