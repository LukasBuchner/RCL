using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for the scheduling pipeline orchestration layer,
///     including CRUD orchestration, notification, data preparation, cascade deletion,
///     timing calculation orchestration, node positioning, and phase logging.
/// </summary>
public static partial class SchedulingPipelineLogger
{
    // ── CrudSchedulingOrchestrator ──────────────────────────────────────

    /// <summary>
    ///     Logs that orchestration is starting for a specific entity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The unique identifier of the entity being orchestrated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Orchestrating action for entity {EntityId}")]
    public static partial void LogOrchestrationStarted(
        this ILogger logger,
        Guid entityId);

    /// <summary>
    ///     Logs that a repository operation failed for a given entity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Repository operation failed for entity {EntityId}")]
    public static partial void LogRepositoryOperationFailed(
        this ILogger logger,
        Guid entityId);

    /// <summary>
    ///     Logs successful completion of an orchestrated action.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully completed action for entity {EntityId}")]
    public static partial void LogOrchestrationCompleted(
        this ILogger logger,
        Guid entityId);

    /// <summary>
    ///     Logs that an orchestrated action failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Action failed for entity {EntityId}")]
    public static partial void LogOrchestrationFailed(
        this ILogger logger,
        Exception exception,
        Guid entityId);

    /// <summary>
    ///     Logs the start of a scheduling calculation for a specific entity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityId">The unique identifier of the entity that triggered scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting scheduling calculation for entity {EntityId}")]
    public static partial void LogSchedulingCalculationStarted(
        this ILogger logger,
        Guid entityId);

    /// <summary>
    ///     Logs that scheduling is being triggered for a procedure with specific data counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    /// <param name="nodeCount">The number of nodes in the scheduling request.</param>
    /// <param name="edgeCount">The number of edges in the scheduling request.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Triggering scheduling for procedure {ProcedureId} with {NodeCount} nodes and {EdgeCount} edges")]
    public static partial void LogSchedulingTriggered(
        this ILogger logger,
        Guid procedureId,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Logs that the scheduling pipeline returned a failure result.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message from the scheduling pipeline.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scheduling pipeline failed: {ErrorMessage}")]
    public static partial void LogSchedulingPipelineFailed(
        this ILogger logger,
        string? errorMessage);

    /// <summary>
    ///     Logs that the scheduling calculation failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="entityId">The unique identifier of the triggering entity.</param>
    /// <param name="errorMessage">The error message from the exception.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Scheduling calculation failed for entity {EntityId}: {ErrorMessage}")]
    public static partial void LogSchedulingCalculationFailed(
        this ILogger logger,
        Exception exception,
        Guid entityId,
        string errorMessage);

    // ── CrudNotificationService ────────────────────────────────────────

    /// <summary>
    ///     Logs that subscribers have been notified with calculated scheduling results.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes in the notification.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notified subscribers with calculated scheduling results (count: {NodeCount})")]
    public static partial void LogCalculatedResultsNotified(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs that nodes are being persisted to the repository.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes being persisted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating {NodeCount} nodes in repository")]
    public static partial void LogNodesPersisting(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs that a bulk update operation failed and the service is falling back to per-node updates.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception from the bulk update attempt.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Bulk update failed; falling back to per-node updates")]
    public static partial void LogBulkUpdateFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs the result of individual node updates after a bulk update fallback.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="updatedCount">The number of nodes successfully updated.</param>
    /// <param name="totalCount">The total number of nodes attempted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updated {UpdatedCount}/{TotalCount} nodes individually")]
    public static partial void LogIndividualNodeUpdatesCompleted(
        this ILogger logger,
        int updatedCount,
        int totalCount);

    /// <summary>
    ///     Logs that an error occurred during the repository node update process.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error occurred during repository node update")]
    public static partial void LogRepositoryNodeUpdateError(
        this ILogger logger,
        Exception exception);

    // ── CrudDataPreparationService ─────────────────────────────────────

    /// <summary>
    ///     Logs that entities have been loaded from the repository for scheduling preparation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of loaded nodes.</param>
    /// <param name="edgeCount">The number of loaded edges.</param>
    /// <param name="procedureId">The procedure from which entities were loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded {NodeCount} nodes and {EdgeCount} edges for procedure {ProcedureId}")]
    public static partial void LogEntitiesLoaded(
        this ILogger logger,
        int nodeCount,
        int edgeCount,
        Guid procedureId);

    // ── CascadeDeletionService ─────────────────────────────────────────

    /// <summary>
    ///     Logs the start of a single node deletion process.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node being deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Processing single node deletion for Node {NodeId}")]
    public static partial void LogSingleNodeDeletionStarted(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs that a node deletion failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node that failed to delete.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to delete node {NodeId}")]
    public static partial void LogNodeDeletionFailed(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs successful deletion of a node and its orphaned edges.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the deleted node.</param>
    /// <param name="edgeCount">The number of orphaned edges that were also deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully deleted node {NodeId} and {EdgeCount} orphaned edges")]
    public static partial void LogNodeDeletionCompleted(
        this ILogger logger,
        Guid nodeId,
        int edgeCount);

    /// <summary>
    ///     Logs the start of a node tree deletion process.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The root node identifier of the tree being deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Processing DeleteNodeTree for Node {NodeId}")]
    public static partial void LogNodeTreeDeletionStarted(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs that a parent node failed to delete during tree deletion.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the parent node that failed.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to delete parent node {NodeId} in tree deletion.")]
    public static partial void LogParentNodeDeletionFailed(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs successful deletion of a node tree with its descendants and orphaned edges.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The root node identifier of the deleted tree.</param>
    /// <param name="descendantCount">The number of descendant nodes deleted.</param>
    /// <param name="edgeCount">The number of orphaned edges deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Successfully deleted node tree for {NodeId} with {DescendantCount} descendants and {EdgeCount} orphaned edges.")]
    public static partial void LogNodeTreeDeletionCompleted(
        this ILogger logger,
        Guid nodeId,
        int descendantCount,
        int edgeCount);

    /// <summary>
    ///     Logs that an orphaned edge failed to delete during cascade deletion.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="edgeId">The unique identifier of the orphaned edge.</param>
    /// <param name="nodeId">The context node whose deletion triggered the orphaned edge cleanup.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete orphaned edge {EdgeId} during deletion of node {NodeId}")]
    public static partial void LogOrphanedEdgeDeletionFailed(
        this ILogger logger,
        Guid edgeId,
        Guid nodeId);

    // ── TimingCalculationOrchestrator ───────────────────────────────────

    /// <summary>
    ///     Logs a warning when an empty node list is provided to the scheduling pipeline.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Empty node list provided for procedure {ProcedureId}, returning empty result")]
    public static partial void LogEmptyNodeListWarning(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the result of Phase 0 router branch filtering.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="excludedCount">The number of nodes excluded from scheduling.</param>
    /// <param name="includedCount">The number of nodes included in scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Phase 0 filtered {ExcludedCount} nodes from scheduling (non-selected branches). Including {IncludedCount} nodes.")]
    public static partial void LogRouterBranchFilterResult(
        this ILogger logger,
        int excludedCount,
        int includedCount);

    /// <summary>
    ///     Logs that Phase 2 timing calculation failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    /// <param name="duration">The elapsed time in milliseconds.</param>
    /// <param name="errorMessage">The error message from the timing calculation.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Phase 2 failed for procedure {ProcedureId} after {Duration}ms: {ErrorMessage}")]
    public static partial void LogTimingCalculationPhaseFailed(
        this ILogger logger,
        Guid procedureId,
        double duration,
        string? errorMessage);

    /// <summary>
    ///     Logs the result of merging hidden state from router branch filtering.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="hiddenCount">The number of hidden nodes added to the result.</param>
    /// <param name="totalCount">The total number of nodes in the merged result.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Merged hidden state: {HiddenCount} hidden nodes added, {TotalCount} total nodes in result")]
    public static partial void LogHiddenStateMerged(
        this ILogger logger,
        int hiddenCount,
        int totalCount);

    /// <summary>
    ///     Logs that the scheduling pipeline was cancelled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    /// <param name="duration">The elapsed time in milliseconds before cancellation.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scheduling pipeline cancelled for procedure {ProcedureId} after {Duration}ms")]
    public static partial void LogSchedulingPipelineCancelled(
        this ILogger logger,
        Guid procedureId,
        double duration);

    /// <summary>
    ///     Logs an unexpected error in the scheduling pipeline.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    /// <param name="duration">The elapsed time in milliseconds.</param>
    /// <param name="errorMessage">The error message from the exception.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Scheduling pipeline failed for procedure {ProcedureId} after {Duration}ms: {ErrorMessage}")]
    public static partial void LogSchedulingPipelineError(
        this ILogger logger,
        Exception exception,
        Guid procedureId,
        double duration,
        string errorMessage);

    // ── NodePositioningService ──────────────────────────────────────────

    /// <summary>
    ///     Logs that no timing information was provided so nodes are returned unchanged.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No timing information provided. Returning nodes unchanged.")]
    public static partial void LogNoTimingInformationProvided(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of position calculation for nodes with timing information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes with timing information.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Calculating positions for {NodeCount} nodes with timing information")]
    public static partial void LogPositionCalculationStarted(
        this ILogger logger,
        int nodeCount);

    /// <summary>
    ///     Logs the counts of positions and dimensions being applied to nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="xCount">The number of X positions calculated.</param>
    /// <param name="yCount">The number of Y positions calculated.</param>
    /// <param name="widthCount">The number of widths calculated.</param>
    /// <param name="heightCount">The number of heights calculated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Applying {XCount} X positions, {YCount} Y positions, {WidthCount} widths, and {HeightCount} heights")]
    public static partial void LogPositionDimensionCounts(
        this ILogger logger,
        int xCount,
        int yCount,
        int widthCount,
        int heightCount);

    /// <summary>
    ///     Logs the X-axis position update for a specific node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    /// <param name="oldX">The previous X position.</param>
    /// <param name="newX">The new X position.</param>
    /// <param name="y">The current Y position.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_POSITIONING | NodeId={NodeId} | NodeType={NodeType} | ParentId={ParentId} | OldX={OldX:F2} | NewX={NewX:F2} | Y={Y:F2}")]
    public static partial void LogNodeXPositionUpdate(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        Guid? parentId,
        double oldX,
        double newX,
        double y);

    /// <summary>
    ///     Logs the width calculation for a specific node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    /// <param name="width">The calculated width.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_WIDTH | NodeId={NodeId} | NodeType={NodeType} | ParentId={ParentId} | Width={Width:F2}")]
    public static partial void LogNodeWidthCalculated(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        Guid? parentId,
        double width);

    /// <summary>
    ///     Logs the height calculation for a specific node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The type name of the node.</param>
    /// <param name="parentId">The parent node identifier (nullable).</param>
    /// <param name="height">The calculated height.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_HEIGHT | NodeId={NodeId} | NodeType={NodeType} | ParentId={ParentId} | Height={Height:F2}")]
    public static partial void LogNodeHeightCalculated(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        Guid? parentId,
        double height);

    /// <summary>
    ///     Logs successful completion of position and dimension application.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes processed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully applied positions, widths, and heights to {NodeCount} nodes")]
    public static partial void LogPositioningCompleted(
        this ILogger logger,
        int nodeCount);

    // ── SchedulingPhaseLogger ──────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when LogPhaseStart is called with a null or empty phase name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LogPhaseStart called with null or empty phase name for procedure {ProcedureId}")]
    public static partial void LogPhaseStartInvalidName(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs a warning when LogPhaseComplete is called with a null or empty phase name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LogPhaseComplete called with null or empty phase name for procedure {ProcedureId}")]
    public static partial void LogPhaseCompleteInvalidName(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs a warning when LogTimingStatistics is called with null statistics.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LogTimingStatistics called with null statistics for procedure {ProcedureId}")]
    public static partial void LogTimingStatisticsNullWarning(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs a warning when LogDetailedNodeTimings is called with null parameters.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LogDetailedNodeTimings called with null parameters for procedure {ProcedureId}")]
    public static partial void LogDetailedNodeTimingsNullWarning(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the start of detailed task timing analysis for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Detailed task timing for procedure {ProcedureId}")]
    public static partial void LogDetailedTaskTimingStart(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the count of nodes in a timing group category.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="category">The category name (e.g. "Task Nodes", "Skill Execution Nodes").</param>
    /// <param name="count">The number of nodes in the category.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "{Category} ({Count}):")]
    public static partial void LogTimingGroupHeader(
        this ILogger logger,
        string category,
        int count);

    /// <summary>
    ///     Logs the completion of detailed task timing analysis.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Completed detailed task timing analysis")]
    public static partial void LogDetailedTaskTimingCompleted(
        this ILogger logger);

    /// <summary>
    ///     Logs a warning when LogCriticalPathAnalysis is called with null parameters.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LogCriticalPathAnalysis called with null parameters for procedure {ProcedureId}")]
    public static partial void LogCriticalPathAnalysisNullWarning(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the start of critical path analysis for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure identifier.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Critical Path Analysis for procedure {ProcedureId}:")]
    public static partial void LogCriticalPathAnalysisStart(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the count of nodes on the critical path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of nodes on the critical path.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Critical Path ({Count} nodes):")]
    public static partial void LogCriticalPathNodeCount(
        this ILogger logger,
        int count);
}