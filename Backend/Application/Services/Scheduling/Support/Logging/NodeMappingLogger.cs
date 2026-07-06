using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for node relationship mapping operations using high-performance source-generated
///     logging.
/// </summary>
public static partial class NodeMappingLogger
{
    /// <summary>
    ///     Logs the start of task-to-skill mapping operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_MAPPING | Type=TaskToSkill | Status=STARTING | TaskNodes={TaskNodeCount} | SkillNodes={SkillNodeCount}")]
    public static partial void LogTaskToSkillMappingStart(
        this ILogger logger,
        int taskNodeCount,
        int skillNodeCount);

    /// <summary>
    ///     Logs a task-to-skill relationship.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=TaskToSkill | TaskId={TaskId} | ChildSkills={ChildSkillCount}")]
    public static partial void LogTaskToSkillRelationship(
        this ILogger logger,
        Guid taskId,
        int childSkillCount);

    /// <summary>
    ///     Logs the completion of task-to-skill mapping.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_MAPPING | Type=TaskToSkill | Status=COMPLETED | Mappings={MappingCount} | MappedSkills={MappedSkills} | UnmappedSkills={UnmappedSkills}")]
    public static partial void LogTaskToSkillMappingComplete(
        this ILogger logger,
        int mappingCount,
        int mappedSkills,
        int unmappedSkills);

    /// <summary>
    ///     Logs orphaned skill nodes detected during mapping.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_MAPPING | Type=TaskToSkill | Status=ORPHANED_DETECTED | OrphanedCount={OrphanedCount} | OrphanedIds={OrphanedIds}")]
    public static partial void LogOrphanedSkillNodes(
        this ILogger logger,
        int orphanedCount,
        string orphanedIds);

    /// <summary>
    ///     Logs the start of skill-to-task mapping operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_MAPPING | Type=SkillToTask | Status=STARTING | SkillNodes={SkillNodeCount} | TaskNodes={TaskNodeCount}")]
    public static partial void LogSkillToTaskMappingStart(
        this ILogger logger,
        int skillNodeCount,
        int taskNodeCount);

    /// <summary>
    ///     Logs creation of task node lookup.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=SkillToTask | Status=LOOKUP_CREATED | TaskLookupEntries={TaskLookupCount}")]
    public static partial void LogTaskLookupCreated(
        this ILogger logger,
        int taskLookupCount);

    /// <summary>
    ///     Logs a skill-to-task relationship.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=SkillToTask | SkillId={SkillId} | ParentTaskId={ParentTaskId}")]
    public static partial void LogSkillToTaskRelationship(
        this ILogger logger,
        Guid skillId,
        Guid parentTaskId);

    /// <summary>
    ///     Logs a skill with an invalid parent reference.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_MAPPING | Type=SkillToTask | Status=INVALID_PARENT | SkillId={SkillId} | InvalidParentId={InvalidParentId}")]
    public static partial void LogSkillWithInvalidParent(
        this ILogger logger,
        Guid skillId,
        Guid invalidParentId);

    /// <summary>
    ///     Logs a skill without a parent reference.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=SkillToTask | Status=NO_PARENT | SkillId={SkillId}")]
    public static partial void LogSkillWithoutParent(
        this ILogger logger,
        Guid skillId);

    /// <summary>
    ///     Logs the completion of skill-to-task mapping.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_MAPPING | Type=SkillToTask | Status=COMPLETED | MappedSkills={MappedSkills} | SkillsWithoutParents={SkillsWithoutParents} | InvalidParents={InvalidParents}")]
    public static partial void LogSkillToTaskMappingComplete(
        this ILogger logger,
        int mappedSkills,
        int skillsWithoutParents,
        int invalidParents);

    /// <summary>
    ///     Logs warning about invalid parent references.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "NODE_MAPPING | Type=SkillToTask | Status=INVALID_REFERENCES | InvalidParentCount={InvalidParentCount}")]
    public static partial void LogInvalidParentReferencesWarning(
        this ILogger logger,
        int invalidParentCount);

    /// <summary>
    ///     Logs the start of parent-to-children mapping operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=ParentToChildren | Status=STARTING | TotalNodes={NodeCount}")]
    public static partial void LogParentToChildrenMappingStart(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs creation of node lookup groups.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=ParentToChildren | Status=GROUPS_CREATED | DistinctParents={GroupCount}")]
    public static partial void LogNodeLookupGroupsCreated(
        this ILogger logger,
        int groupCount);

    /// <summary>
    ///     Logs root nodes found during mapping.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=ParentToChildren | Status=ROOT_NODES | RootNodeCount={RootNodeCount}")]
    public static partial void LogRootNodesFound(
        this ILogger logger,
        int rootNodeCount);

    /// <summary>
    ///     Logs details of a root node.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=ParentToChildren | NodeId={NodeId} | NodeType={NodeType}")]
    public static partial void LogRootNodeDetail(
        this ILogger logger,
        Guid nodeId,
        string nodeType);

    /// <summary>
    ///     Logs a parent-child relationship.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_MAPPING | Type=ParentToChildren | ParentId={ParentId} | ChildCount={ChildCount}")]
    public static partial void LogParentChildRelationship(
        this ILogger logger,
        Guid parentId,
        int childCount);

    /// <summary>
    ///     Logs the completion of parent-to-children mapping.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "NODE_MAPPING | Type=ParentToChildren | Status=COMPLETED | RootNodes={RootNodeCount} | ChildRelationships={ChildRelationships} | ParentGroups={ParentGroups}")]
    public static partial void LogParentToChildrenMappingComplete(
        this ILogger logger,
        int rootNodeCount,
        int childRelationships,
        int parentGroups);

    // ── NodeTimingMapper ───────────────────────────────────────────────

    /// <summary>
    ///     Logs the relative time adjustment for a child node whose parent was found in timing info.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The child node identifier.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="parentId">The parent node identifier.</param>
    /// <param name="absoluteStart">The absolute start time of the child.</param>
    /// <param name="parentAbsoluteStart">The absolute start time of the parent.</param>
    /// <param name="newRelativeStart">The calculated relative start time.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_TIMING | Phase=RELATIVE_ADJUST_CHILD | NodeId={NodeId} | NodeType={NodeType} | ParentId={ParentId} | ParentFound=TRUE | AbsoluteStart={AbsoluteStart:F3}s | ParentAbsoluteStart={ParentAbsoluteStart:F3}s | NewRelativeStart={NewRelativeStart:F3}s")]
    public static partial void LogRelativeAdjustChild(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        Guid? parentId,
        double absoluteStart,
        double parentAbsoluteStart,
        double newRelativeStart);

    /// <summary>
    ///     Logs a warning for a child node whose parent is not present in the timing info.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The orphaned child node identifier.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="parentId">The parent node identifier that was not found.</param>
    /// <param name="absoluteStart">The absolute start time being used as relative time.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "NODE_TIMING | Phase=RELATIVE_ADJUST_ORPHAN | NodeId={NodeId} | NodeType={NodeType} | ParentId={ParentId} | ParentFound=FALSE | AbsoluteStart={AbsoluteStart:F3}s | USING_ABSOLUTE_AS_RELATIVE (parent not in timingInfo!)")]
    public static partial void LogRelativeAdjustOrphan(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        Guid? parentId,
        double absoluteStart);

    /// <summary>
    ///     Logs the relative time adjustment for a root node (no parent).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The root node identifier.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="absoluteStart">The absolute start time.</param>
    /// <param name="newRelativeStart">The relative start time (same as absolute for root nodes).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_TIMING | Phase=RELATIVE_ADJUST_ROOT | NodeId={NodeId} | NodeType={NodeType} | ParentId=NULL | AbsoluteStart={AbsoluteStart:F3}s | NewRelativeStart={NewRelativeStart:F3}s")]
    public static partial void LogRelativeAdjustRoot(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double absoluteStart,
        double newRelativeStart);

    /// <summary>
    ///     Logs timing being applied to a router node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The duration being applied.</param>
    /// <param name="startTime">The start time being applied.</param>
    /// <param name="finishTime">The finish time being applied.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NodeTimingMapper: Applying timing to RouterNode {RouterNodeId}: Duration={Duration}, StartTime={StartTime}, FinishTime={FinishTime}")]
    public static partial void LogRouterNodeTimingApplied(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime,
        double finishTime);

    /// <summary>
    ///     Logs a duration fallback being applied to a router node when detailed timing is unavailable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The fallback duration being applied.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NodeTimingMapper: Applying duration fallback to RouterNode {RouterNodeId}: Duration={Duration} (FALLBACK PATH - no detailed timing)")]
    public static partial void LogRouterNodeDurationFallback(
        this ILogger logger,
        Guid routerNodeId,
        double duration);

    /// <summary>
    ///     Logs at Warning level when a RouterNode receives duration-only timing because detailed
    ///     scheduler timing is absent; the original StartTime is kept and only FinishTime is recomputed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The router node identifier.</param>
    /// <param name="duration">The duration applied in seconds.</param>
    /// <param name="startTime">The original StartTime retained in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "NodeTimingMapper: Duration fallback for RouterNode {RouterNodeId}: applied duration-only {Duration:F3}s keeping original StartTime {StartTime:F3}s (no detailed timing)")]
    public static partial void LogRouterNodeDurationFallbackWarning(
        this ILogger logger,
        Guid routerNodeId,
        double duration,
        double startTime);
}