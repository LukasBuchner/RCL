using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for scheduling processing operations including
///     node hierarchy processing, hierarchy validation, hierarchical sorting,
///     node duration adjustment, timing aggregation, and child node collection.
/// </summary>
public static partial class SchedulingProcessingLogger
{
    // ── NodeHierarchyProcessor ──────────────────────────────────────────

    /// <summary>
    ///     Logs the start of node hierarchy processing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes to process.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Processing node hierarchy with {NodeCount} nodes")]
    public static partial void LogHierarchyProcessingStarted(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs a warning when an empty node list is provided to the hierarchy processor.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Empty node list provided to hierarchy processor, returning empty hierarchy")]
    public static partial void LogEmptyNodeListHierarchy(
        this ILogger logger);

    /// <summary>
    ///     Logs the breakdown of node types found during hierarchy analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeCount">The number of task nodes.</param>
    /// <param name="skillNodeCount">The number of skill execution nodes.</param>
    /// <param name="routerNodeCount">The number of router nodes.</param>
    /// <param name="otherNodeCount">The number of nodes of unexpected types.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Node type analysis: {TaskNodeCount} task nodes, {SkillNodeCount} skill execution nodes, {RouterNodeCount} router nodes, {OtherNodeCount} other nodes")]
    public static partial void LogNodeTypeAnalysis(
        this ILogger logger,
        int taskNodeCount,
        int skillNodeCount,
        int routerNodeCount,
        int otherNodeCount);

    /// <summary>
    ///     Logs a warning when nodes of unexpected types are found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="otherNodeCount">The number of unexpected node types.</param>
    /// <param name="nodeTypes">Comma-separated list of unexpected node type names.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Found {OtherNodeCount} nodes of unexpected types: {NodeTypes}")]
    public static partial void LogUnexpectedNodeTypes(
        this ILogger logger,
        int otherNodeCount,
        string nodeTypes);

    /// <summary>
    ///     Logs details of a task node during hierarchy processing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the task node.</param>
    /// <param name="name">The name of the task.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Task Node: {NodeId}, Name: '{Name}', ParentId: {ParentId}")]
    public static partial void LogTaskNodeDetail(
        this ILogger logger,
        Guid nodeId,
        string name,
        Guid? parentId);

    /// <summary>
    ///     Logs details of a skill execution node during hierarchy processing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the skill node.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentId">The identifier of the assigned agent.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Skill Execution Node: {NodeId}, ParentId: {ParentId}, SkillName: {SkillName}, AgentId: {AgentId}")]
    public static partial void LogSkillExecutionNodeDetail(
        this ILogger logger,
        Guid nodeId,
        Guid? parentId,
        string skillName,
        Guid agentId);

    /// <summary>
    ///     Logs details of a router node during hierarchy processing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the router node.</param>
    /// <param name="name">The name of the router.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    /// <param name="branchCount">The number of conditional branches.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Router Node: {NodeId}, Name: '{Name}', ParentId: {ParentId}, BranchCount: {BranchCount}")]
    public static partial void LogRouterNodeDetail(
        this ILogger logger,
        Guid nodeId,
        string name,
        Guid? parentId,
        int branchCount);

    /// <summary>
    ///     Logs the start of building node relationship mappings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Building node relationship mappings")]
    public static partial void LogBuildingRelationshipMappings(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of hierarchy consistency validation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Validating hierarchy consistency")]
    public static partial void LogValidatingHierarchy(
        this ILogger logger);

    /// <summary>
    ///     Logs that hierarchy validation failed with errors.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorCount">The number of validation errors.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Hierarchy validation failed with {ErrorCount} errors")]
    public static partial void LogHierarchyValidationFailed(
        this ILogger logger,
        int errorCount);

    /// <summary>
    ///     Logs the comprehensive statistics of the processed node hierarchy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeCount">The number of task nodes.</param>
    /// <param name="withChildren">The number of task nodes with children.</param>
    /// <param name="withoutChildren">The number of task nodes without children.</param>
    /// <param name="skillNodeCount">The number of skill execution nodes.</param>
    /// <param name="orphanedSkills">The number of orphaned skill nodes.</param>
    /// <param name="routerNodeCount">The number of router nodes.</param>
    /// <param name="totalRelationships">The total number of parent-to-children relationships.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Successfully processed node hierarchy: {TaskNodeCount} task nodes ({WithChildren} with children, {WithoutChildren} without), {SkillNodeCount} skill execution nodes ({OrphanedSkills} orphaned), {RouterNodeCount} router nodes, {TotalRelationships} total relationships")]
    public static partial void LogHierarchyProcessingCompleted(
        this ILogger logger,
        int taskNodeCount,
        int withChildren,
        int withoutChildren,
        int skillNodeCount,
        int orphanedSkills,
        int routerNodeCount,
        int totalRelationships);

    /// <summary>
    ///     Logs a trace about orphaned skill execution nodes found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="orphanedSkillNodes">The number of orphaned skill execution nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found {OrphanedSkillNodes} skill execution nodes without valid parent task nodes")]
    public static partial void LogOrphanedSkillNodesFound(
        this ILogger logger,
        int orphanedSkillNodes);

    // ── HierarchyValidator ─────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of hierarchy consistency validation with node counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodes">The number of task nodes being validated.</param>
    /// <param name="skillNodes">The number of skill nodes being validated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Validating hierarchy consistency for {TaskNodes} task nodes and {SkillNodes} skill nodes")]
    public static partial void LogHierarchyValidationStarted(
        this ILogger logger,
        int taskNodes,
        int skillNodes);

    /// <summary>
    ///     Logs hierarchy validation errors.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorCount">The number of validation errors.</param>
    /// <param name="errors">Semicolon-separated error descriptions.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Hierarchy validation failed with {ErrorCount} errors: {Errors}")]
    public static partial void LogHierarchyValidationErrors(
        this ILogger logger,
        int errorCount,
        string errors);

    /// <summary>
    ///     Logs hierarchy validation warnings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="warningCount">The number of validation warnings.</param>
    /// <param name="warnings">Semicolon-separated warning descriptions.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Hierarchy validation found {WarningCount} warnings: {Warnings}")]
    public static partial void LogHierarchyValidationWarnings(
        this ILogger logger,
        int warningCount,
        string warnings);

    /// <summary>
    ///     Logs that hierarchy validation passed with all mappings consistent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Hierarchy validation passed: All mappings are consistent")]
    public static partial void LogHierarchyValidationPassed(
        this ILogger logger);

    // ── HierarchicalSorter ─────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of hierarchical sorting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of task nodes to sort.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting hierarchical sort of {Count} task nodes")]
    public static partial void LogHierarchicalSortStarted(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs that a task node has already been processed during sorting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the skipped node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Task node {NodeId} already processed, skipping")]
    public static partial void LogNodeAlreadyProcessed(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs the processing of children for a task node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="childCount">The number of children being processed.</param>
    /// <param name="nodeId">The unique identifier of the parent node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Processing {ChildCount} children of task node {NodeId}")]
    public static partial void LogProcessingChildren(
        this ILogger logger,
        int childCount,
        Guid nodeId);

    /// <summary>
    ///     Logs the addition of a task node to the sorted result.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the added node.</param>
    /// <param name="position">The position in the sorted result.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Added task node {NodeId} to sorted result (position {Position})")]
    public static partial void LogNodeAddedToSortedResult(
        this ILogger logger,
        Guid nodeId,
        int position);

    /// <summary>
    ///     Logs the number of root nodes found for sorting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="rootCount">The number of root nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {RootCount} root nodes to process (including nodes whose parent is not a TaskNode)")]
    public static partial void LogRootNodesForSorting(
        this ILogger logger,
        int rootCount);

    /// <summary>
    ///     Logs a warning about orphaned nodes with invalid parent references.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="orphanedCount">The number of orphaned nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Found {OrphanedCount} orphaned nodes with invalid parent references")]
    public static partial void LogOrphanedNodesWarning(
        this ILogger logger,
        int orphanedCount);

    /// <summary>
    ///     Logs the completion of hierarchical sorting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="inputCount">The number of input nodes.</param>
    /// <param name="outputCount">The number of sorted output nodes.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Hierarchical sort completed: {InputCount} input nodes -> {OutputCount} sorted nodes")]
    public static partial void LogHierarchicalSortCompleted(
        this ILogger logger,
        int inputCount,
        int outputCount);

    /// <summary>
    ///     Logs the completion of hierarchical sorting with all-nodes context including depth info.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="inputCount">The number of input nodes.</param>
    /// <param name="outputCount">The number of sorted output nodes.</param>
    /// <param name="depths">Comma-separated list of node name=depth pairs.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Hierarchical sort with allNodes completed: {InputCount} input nodes -> {OutputCount} sorted nodes (depths: {Depths})")]
    public static partial void LogHierarchicalSortWithDepthsCompleted(
        this ILogger logger,
        int inputCount,
        int outputCount,
        string depths);

    // ── NodeDurationAdjuster ───────────────────────────────────────────

    /// <summary>
    ///     Logs the start of hierarchical duration adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes involved.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting hierarchical duration adjustment for {NodeCount} nodes")]
    public static partial void LogDurationAdjustmentStarted(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs the number of task node schedules provided by the TaskNodeDurationCalculator.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="calculatedCount">The number of calculated schedules.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "TaskNodeDurationCalculator provided {CalculatedCount} task node schedules")]
    public static partial void LogTaskNodeSchedulesProvided(
        this ILogger logger,
        int calculatedCount);

    /// <summary>
    ///     Logs that TaskNodeDurationCalculator results are being used for task nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeCount">The number of task nodes using calculator results.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Using TaskNodeDurationCalculator results for {TaskNodeCount} task nodes")]
    public static partial void LogUsingTaskNodeCalculatorResults(
        this ILogger logger,
        int taskNodeCount);

    /// <summary>
    ///     Logs the duration update for a specific task node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The unique identifier of the task node.</param>
    /// <param name="newDuration">The new calculated duration.</param>
    /// <param name="originalDuration">The original duration before adjustment.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "TaskNode {TaskNodeId} duration set to {NewDuration} (was {OriginalDuration})")]
    public static partial void LogTaskNodeDurationSet(
        this ILogger logger,
        Guid taskNodeId,
        double newDuration,
        double originalDuration);

    /// <summary>
    ///     Logs the number of router node schedules provided by the RouterNodeDurationCalculator.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="calculatedCount">The number of calculated router schedules.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "RouterNodeDurationCalculator provided {CalculatedCount} router node schedules")]
    public static partial void LogRouterNodeSchedulesProvided(
        this ILogger logger,
        int calculatedCount);

    /// <summary>
    ///     Logs the duration update for a specific router node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The unique identifier of the router node.</param>
    /// <param name="newDuration">The new calculated duration.</param>
    /// <param name="originalDuration">The original duration before adjustment.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "RouterNode {RouterNodeId} duration set to {NewDuration} (was {OriginalDuration})")]
    public static partial void LogRouterNodeDurationSet(
        this ILogger logger,
        Guid routerNodeId,
        double newDuration,
        double originalDuration);

    /// <summary>
    ///     Logs that the fallback to legacy hierarchical adjustment is being used.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "TaskNodeDurationCalculator provided no results - falling back to legacy hierarchical adjustment")]
    public static partial void LogFallingBackToLegacyAdjustment(
        this ILogger logger);

    /// <summary>
    ///     Logs at Warning level that the primary TaskNodeDurationCalculator returned no results
    ///     and the legacy hierarchical duration adjustment is used as a fallback.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes in the hierarchy.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "DURATION_ADJUST | TaskNodeDurationCalculator returned no results for {NodeCount} nodes; falling back to legacy hierarchical duration adjustment")]
    public static partial void LogFallingBackToLegacyAdjustmentWarning(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs at Warning level that a skill child node is absent from scheduler timings and its
    ///     original fields are used to reconstruct timing for the parent duration aggregation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillNodeId">The unique identifier of the skill node missing from scheduler timings.</param>
    /// <param name="startTime">The original StartTime used for the reconstruction in seconds.</param>
    /// <param name="duration">The reconstructed duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "DURATION_ADJUST | SkillNode {SkillNodeId} missing from scheduler timings; reconstructed timing from original fields Start={StartTime:F3}s Duration={Duration:F3}s for parent duration aggregation")]
    public static partial void LogSkillChildTimingFallback(
        this ILogger logger,
        Guid skillNodeId,
        double startTime,
        double duration);

    /// <summary>
    ///     Logs the child timing for a TaskNode child during duration calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The unique identifier of the child task node.</param>
    /// <param name="startTime">The start time of the child.</param>
    /// <param name="duration">The duration of the child.</param>
    /// <param name="finishTime">The finish time of the child.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Child TaskNode {TaskNodeId}: StartTime={StartTime}, Duration={Duration}, FinishTime={FinishTime}")]
    public static partial void LogChildTaskNodeTiming(
        this ILogger logger,
        Guid taskNodeId,
        double startTime,
        double duration,
        double finishTime);

    /// <summary>
    ///     Logs the child timing for a RouterNode child during duration calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The unique identifier of the child router node.</param>
    /// <param name="startTime">The start time of the child.</param>
    /// <param name="duration">The duration of the child.</param>
    /// <param name="finishTime">The finish time of the child.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Child RouterNode {RouterNodeId}: StartTime={StartTime}, Duration={Duration}, FinishTime={FinishTime}")]
    public static partial void LogChildRouterNodeTiming(
        this ILogger logger,
        Guid routerNodeId,
        double startTime,
        double duration,
        double finishTime);

    /// <summary>
    ///     Logs the calculated required duration from children.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="duration">The calculated required duration.</param>
    /// <param name="validChildren">The number of child nodes with valid timings.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Calculated required duration {Duration} from {ValidChildren} children")]
    public static partial void LogRequiredDurationCalculated(
        this ILogger logger,
        double duration,
        int validChildren);

    /// <summary>
    ///     Logs the start of a legacy duration adjustment iteration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="iteration">The iteration number.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Starting duration adjustment iteration #{Iteration}")]
    public static partial void LogLegacyAdjustmentIterationStarted(
        this ILogger logger,
        int iteration);

    /// <summary>
    ///     Logs a legacy task node duration adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The unique identifier of the task node.</param>
    /// <param name="original">The original duration.</param>
    /// <param name="new_">The new adjusted duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "TaskNode {TaskNodeId} duration adjusted from {Original} to {New_}")]
    public static partial void LogLegacyTaskNodeDurationAdjusted(
        this ILogger logger,
        Guid taskNodeId,
        double original,
        double new_);

    /// <summary>
    ///     Logs a legacy router node duration adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerNodeId">The unique identifier of the router node.</param>
    /// <param name="original">The original duration.</param>
    /// <param name="new_">The new adjusted duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "RouterNode {RouterNodeId} duration adjusted from {Original} to {New_}")]
    public static partial void LogLegacyRouterNodeDurationAdjusted(
        this ILogger logger,
        Guid routerNodeId,
        double original,
        double new_);

    /// <summary>
    ///     Logs the completion of a legacy duration adjustment iteration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="iteration">The iteration number.</param>
    /// <param name="adjustmentCount">The number of adjustments made in this iteration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Completed iteration #{Iteration}: {AdjustmentCount} adjustments")]
    public static partial void LogLegacyAdjustmentIterationCompleted(
        this ILogger logger,
        int iteration,
        int adjustmentCount);

    /// <summary>
    ///     Logs a warning that the maximum iteration count was reached during duration adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxIterations">The maximum iteration limit.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Duration adjustment reached maximum iterations ({MaxIterations})")]
    public static partial void LogMaxIterationsReached(
        this ILogger logger,
        int maxIterations);

    /// <summary>
    ///     Logs the completion of hierarchical duration adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="totalAdjustments">The total number of adjustments made.</param>
    /// <param name="iterations">The number of iterations performed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Completed hierarchical duration adjustment: {TotalAdjustments} adjustments in {Iterations} iterations")]
    public static partial void LogDurationAdjustmentCompleted(
        this ILogger logger,
        int totalAdjustments,
        int iterations);

    // ── TimingAggregator ───────────────────────────────────────────────

    /// <summary>
    ///     Logs the result of timing aggregation across children.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of timings aggregated.</param>
    /// <param name="duration">The aggregated duration.</param>
    /// <param name="start">The earliest start time.</param>
    /// <param name="finish">The latest finish time.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Aggregated {Count} timings: Duration={Duration}, Start={Start}, Finish={Finish}")]
    public static partial void LogTimingsAggregated(
        this ILogger logger,
        int count,
        double duration,
        double start,
        double finish);

    /// <summary>
    ///     Logs that no timings were provided for aggregation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No timings provided for aggregation, returning null")]
    public static partial void LogNoTimingsForAggregation(
        this ILogger logger);

    // ── ChildNodeCollector ──────────────────────────────────────────────

    /// <summary>
    ///     Logs the number of skill execution child nodes found for a parent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of child skill nodes found.</param>
    /// <param name="parentId">The parent node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {Count} skill execution child nodes for parent {ParentId}")]
    public static partial void LogSkillChildNodesFound(
        this ILogger logger,
        int count,
        Guid parentId);

    /// <summary>
    ///     Logs the number of task node children found for a parent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of child task nodes found.</param>
    /// <param name="parentId">The parent node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {Count} task node children for parent {ParentId}")]
    public static partial void LogTaskChildNodesFound(
        this ILogger logger,
        int count,
        Guid parentId);

    /// <summary>
    ///     Logs the number of router child nodes found for a parent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of child router nodes found.</param>
    /// <param name="parentId">The parent node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {Count} router child nodes for parent {ParentId}")]
    public static partial void LogRouterChildNodesFound(
        this ILogger logger,
        int count,
        Guid parentId);

    /// <summary>
    ///     Logs the total breakdown of all child node types found for a parent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of skill child nodes.</param>
    /// <param name="taskCount">The number of task child nodes.</param>
    /// <param name="routerCount">The number of router child nodes.</param>
    /// <param name="parentId">The parent node identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Found {SkillCount} skill + {TaskCount} task + {RouterCount} router children for parent {ParentId}")]
    public static partial void LogAllChildNodesFound(
        this ILogger logger,
        int skillCount,
        int taskCount,
        int routerCount,
        Guid parentId);
}