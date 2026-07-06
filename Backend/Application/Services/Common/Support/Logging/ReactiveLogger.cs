using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Common.Support.Logging;

/// <summary>
///     Provides structured logging for reactive change tracking operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class ReactiveLogger
{
    /// <summary>
    ///     Logs a successful update of tracked entities with the new item count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    /// <param name="count">The number of entities in the update.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Updating {EntityType} entities with {Count} items")]
    public static partial void LogEntitiesUpdated(
        this ILogger logger,
        string entityType,
        int count);

    /// <summary>
    ///     Logs a successful refresh of entities from the repository, including
    ///     the retry attempt number.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of entities loaded from the repository.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    /// <param name="attempt">The current retry attempt number.</param>
    /// <param name="maxRetries">The maximum number of retry attempts configured.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Refreshed {Count} {EntityType} entities from repository (attempt {Attempt}/{MaxRetries})")]
    public static partial void LogEntitiesRefreshed(
        this ILogger logger,
        int count,
        string entityType,
        int attempt,
        int maxRetries);

    /// <summary>
    ///     Logs a transient failure during entity refresh with retry information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the transient failure.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    /// <param name="attempt">The current retry attempt number.</param>
    /// <param name="maxRetries">The maximum number of retry attempts configured.</param>
    /// <param name="backoffMs">The backoff delay in milliseconds before the next retry.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Transient failure refreshing {EntityType} entities (attempt {Attempt}/{MaxRetries}), retrying in {BackoffMs}ms")]
    public static partial void LogRefreshTransientFailure(
        this ILogger logger,
        Exception exception,
        string entityType,
        int attempt,
        int maxRetries,
        int backoffMs);

    /// <summary>
    ///     Logs a final failure to refresh entities after all retry attempts have been exhausted.
    ///     The current in-memory data is preserved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the final failure.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    /// <param name="maxRetries">The maximum number of retry attempts that were attempted.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to refresh {EntityType} entities from repository after {MaxRetries} attempts, preserving current data")]
    public static partial void LogRefreshFinalFailure(
        this ILogger logger,
        Exception exception,
        string entityType,
        int maxRetries);

    /// <summary>
    ///     Logs successful loading of initial entity data from the repository.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of entities loaded.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded initial {Count} {EntityType} entities")]
    public static partial void LogInitialDataLoaded(
        this ILogger logger,
        int count,
        string entityType);

    /// <summary>
    ///     Logs a failure to load initial entity data from the repository.
    ///     The tracker continues to serve empty data until the next refresh.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="entityType">The CLR type name of the tracked entity.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load initial {EntityType} entities — tracker will serve empty data until next refresh")]
    public static partial void LogInitialDataLoadFailed(
        this ILogger logger,
        Exception exception,
        string entityType);

    /// <summary>
    ///     Logs an unhandled exception that escaped the asynchronous initialization path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="trackerType">The name of the tracker class where the exception originated.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception escaped LoadInitialDataAsync in {TrackerType}")]
    public static partial void LogUnhandledInitException(
        this ILogger logger,
        Exception exception,
        string trackerType);

    /// <summary>
    ///     Logs a procedure variable change notification with the current variable count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableCount">The number of variables after the change.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Procedure variables changed: {VariableCount} variable(s)")]
    public static partial void LogProcedureVariablesChanged(
        this ILogger logger,
        int variableCount);

    /// <summary>
    ///     Logs that procedure variables have been cleared because the procedure was unloaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Procedure variables cleared (procedure unloaded)")]
    public static partial void LogProcedureVariablesCleared(
        this ILogger logger);

    /// <summary>
    ///     Logs the successful loading of initial procedure state with node and edge counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes loaded.</param>
    /// <param name="edgeCount">The number of edges loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded initial procedure state: {NodeCount} nodes, {EdgeCount} edges")]
    public static partial void LogInitialProcedureStateLoaded(
        this ILogger logger,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Logs a failure to load the initial procedure state from the repository.
    ///     The tracker continues to serve empty data until the next refresh.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load initial procedure state — tracker will serve empty data until next refresh")]
    public static partial void LogInitialProcedureStateLoadFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs an update to the tracked edges with the new item count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of edges in the update.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Updating edges with {Count} items")]
    public static partial void LogEdgesUpdated(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs an update to the tracked nodes with the new item count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of nodes in the update.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Updating nodes with {Count} items")]
    public static partial void LogNodesUpdated(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs a procedure variable change notification within the unified procedure state tracker.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of variables after the change.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Procedure variables changed: {Count} variable(s)")]
    public static partial void LogProcedureStateVariablesChanged(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs that all procedure state has been cleared because no procedure is loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Procedure state cleared (no procedure loaded)")]
    public static partial void LogProcedureStateCleared(
        this ILogger logger);

    /// <summary>
    ///     Logs that a repository refresh was skipped because no procedure is currently loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the entity that was not refreshed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping {EntityType} repository refresh — no procedure is currently loaded")]
    public static partial void LogNoProcedureLoadedForRefresh(
        this ILogger logger,
        string entityType);

    /// <summary>
    ///     Logs that repository results for a procedure were discarded because the loaded
    ///     procedure changed while the async query was in flight.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The procedure ID whose results were discarded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Discarding stale load results for procedure {ProcedureId} — active procedure changed during load")]
    public static partial void LogStaleProcedureLoadDiscarded(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that one or more entities were dropped from an update because their
    ///     <c>ProcedureId</c> did not match the currently loaded procedure. The mismatch
    ///     indicates a caller is pushing cross-procedure data into the tracker, which
    ///     would otherwise leak into the validator and scheduler streams.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the entity (e.g. "Node", "DependencyEdge").</param>
    /// <param name="droppedCount">The number of entities dropped due to procedure mismatch.</param>
    /// <param name="loadedProcedureId">The currently loaded procedure against which the filter ran.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Dropped {DroppedCount} {EntityType} entities whose ProcedureId did not match the loaded procedure {LoadedProcedureId}; investigate caller for cross-procedure bleed")]
    public static partial void LogCrossProcedureEntitiesDropped(
        this ILogger logger,
        string entityType,
        int droppedCount,
        Guid loadedProcedureId);

    /// <summary>
    ///     Logs that an entity update was rejected because no procedure is currently
    ///     loaded. The tracker refuses to publish entity data in this state because the
    ///     incoming entities cannot be validated against any active procedure scope.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the entity (e.g. "Node", "DependencyEdge").</param>
    /// <param name="count">The number of entities in the rejected update.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Rejected {EntityType} update with {Count} entities — no procedure is currently loaded")]
    public static partial void LogUpdateRejectedNoProcedure(
        this ILogger logger,
        string entityType,
        int count);
}