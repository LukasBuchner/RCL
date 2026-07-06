using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for PostgreSQL operations.
///     Covers <see cref="PostgresDbContext" /> connection lifecycle and migration events,
///     and repository-level disconnection warnings across all PostgreSQL repositories.
/// </summary>
public static partial class PostgresLogger
{
    // ──────────────────────────────────────────────────
    //  PostgresDbContext — Connection lifecycle
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a connection attempt to PostgreSQL is starting.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 3100,
        Level = LogLevel.Information,
        Message = "Connecting to PostgreSQL")]
    public static partial void LogConnecting(this ILogger logger);

    /// <summary>
    ///     Logs that the connection to PostgreSQL was established successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Successfully connected to PostgreSQL")]
    public static partial void LogConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when the PostgreSQL connection attempt fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the connection failure.</param>
    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Error,
        Message = "Failed to connect to PostgreSQL. Server will start with limited functionality.")]
    public static partial void LogConnectionFailed(this ILogger logger, Exception ex);

    // ──────────────────────────────────────────────────
    //  PostgresDbContext — Schema migration
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when the migration SQL file cannot be found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Warning,
        Message = "Migration SQL file not found. Skipping automatic schema migration")]
    public static partial void LogMigrationFileNotFound(this ILogger logger);

    /// <summary>
    ///     Logs that the database schema migration completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 3104,
        Level = LogLevel.Information,
        Message = "Database schema migration completed successfully")]
    public static partial void LogMigrationCompleted(this ILogger logger);

    /// <summary>
    ///     Logs an error when a database schema migration fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the migration failure.</param>
    [LoggerMessage(
        EventId = 3105,
        Level = LogLevel.Error,
        Message = "Database schema migration failed. Tables may need to be created manually")]
    public static partial void LogMigrationFailed(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs that a migration file was found at the specified path.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="path">The file path where the migration file was found.</param>
    [LoggerMessage(
        EventId = 3106,
        Level = LogLevel.Debug,
        Message = "Found migration file at {Path}")]
    public static partial void LogMigrationFileFound(this ILogger logger, string path);

    // ──────────────────────────────────────────────────
    //  Repository — Disconnection warnings
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when a repository returns an empty list due to PostgreSQL being disconnected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tableName">The database table name that was being queried.</param>
    [LoggerMessage(
        EventId = 3110,
        Level = LogLevel.Warning,
        Message = "PostgreSQL is not connected. Returning empty list for {TableName}")]
    public static partial void LogDisconnectedEmptyList(this ILogger logger, string tableName);

    /// <summary>
    ///     Logs a warning when a repository returns null due to PostgreSQL being disconnected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tableName">The database table name that was being queried.</param>
    /// <param name="id">The entity identifier that was being looked up.</param>
    [LoggerMessage(
        EventId = 3111,
        Level = LogLevel.Warning,
        Message = "PostgreSQL is not connected. Returning null for {TableName} with ID {Id}")]
    public static partial void LogDisconnectedNullResult(this ILogger logger, string tableName, Guid id);

    /// <summary>
    ///     Logs a warning when a repository cannot perform a delete due to PostgreSQL being disconnected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tableName">The database table name from which entities were to be deleted.</param>
    /// <param name="procedureId">The procedure identifier scoping the deletion.</param>
    [LoggerMessage(
        EventId = 3112,
        Level = LogLevel.Warning,
        Message = "PostgreSQL is not connected. Cannot delete {TableName} for procedure {ProcedureId}")]
    public static partial void LogDisconnectedCannotDelete(this ILogger logger, string tableName, Guid procedureId);

    // ──────────────────────────────────────────────────
    //  Repository — Row-mapping fallbacks
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when the <c>skill_ids</c> column of an agent row is null or not a <see cref="Guid" /> array,
    ///     causing the skill list to be substituted with an empty collection.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent whose skill assignments are dropped.</param>
    [LoggerMessage(
        EventId = 3120,
        Level = LogLevel.Warning,
        Message =
            "Agent {AgentId} skill_ids column was null or not a Guid[]; substituting empty skill list (assigned skills dropped)")]
    public static partial void LogAgentSkillIdsFallback(this ILogger logger, Guid agentId);

    /// <summary>
    ///     Logs a warning when the <c>state</c> column of an agent row contains an unrecognized value,
    ///     causing the state to default to <paramref name="defaultState" />.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentId">The identifier of the agent with the unrecognized state.</param>
    /// <param name="storedState">The raw state string read from the database.</param>
    /// <param name="defaultState">The fallback state used in place of the unrecognized value.</param>
    [LoggerMessage(
        EventId = 3121,
        Level = LogLevel.Warning,
        Message = "Agent {AgentId} stored state '{StoredState}' is unrecognized; defaulting to {DefaultState}")]
    public static partial void LogAgentStateParseFallback(this ILogger logger, Guid agentId, string storedState,
        AgentState defaultState);

    /// <summary>
    ///     Logs a warning when the <c>root_node_ids</c> column of a procedure row is null or not a <see cref="Guid" />
    ///     array, causing the root-node list to be substituted with an empty collection.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="procedureId">The identifier of the procedure whose graph roots are dropped.</param>
    [LoggerMessage(
        EventId = 3122,
        Level = LogLevel.Warning,
        Message =
            "Procedure {ProcedureId} root_node_ids column was null or not a Guid[]; substituting empty root-node list (graph roots dropped)")]
    public static partial void LogProcedureRootNodeIdsFallback(this ILogger logger, Guid procedureId);

    /// <summary>
    ///     Logs a warning when the <c>data</c> column of a <c>TaskNode</c> row is null or empty,
    ///     causing the task definition to be substituted with an empty zero-duration <c>Task</c>.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeId">The identifier of the node whose task definition is lost.</param>
    /// <param name="procedureId">The identifier of the procedure containing the node.</param>
    [LoggerMessage(
        EventId = 3123,
        Level = LogLevel.Warning,
        Message =
            "TaskNode {NodeId} in procedure {ProcedureId} had null/empty data; substituting empty zero-duration Task (task definition lost)")]
    public static partial void LogTaskNodeDataFallback(this ILogger logger, Guid nodeId, Guid procedureId);

    /// <summary>
    ///     Logs a warning when the <c>position</c> column of a <c>PositionTag</c> row is null or empty,
    ///     causing the tag's coordinates to be substituted with an origin <c>Position</c>.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tagId">The identifier of the position tag whose coordinates are discarded.</param>
    [LoggerMessage(
        EventId = 3124,
        Level = LogLevel.Warning,
        Message =
            "PositionTag {TagId} had null/empty position; substituting origin Position (stored coordinates discarded)")]
    public static partial void LogPositionTagPositionFallback(this ILogger logger, Guid tagId);

    /// <summary>
    ///     Logs a warning when the <c>position</c> column of a <c>SceneObject</c> row is null or empty,
    ///     causing the object's coordinates to be substituted with an origin <c>Position</c>.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="sceneObjectId">The identifier of the scene object whose coordinates are discarded.</param>
    [LoggerMessage(
        EventId = 3125,
        Level = LogLevel.Warning,
        Message =
            "SceneObject {SceneObjectId} had null/empty position; substituting origin Position (stored coordinates discarded)")]
    public static partial void LogSceneObjectPositionFallback(this ILogger logger, Guid sceneObjectId);
}