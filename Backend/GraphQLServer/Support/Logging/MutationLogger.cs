namespace FHOOE.Freydis.GraphQLServer.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for GraphQL mutation operations.
///     All methods are extension methods on <see cref="ILogger" /> and use the
///     <see cref="LoggerMessageAttribute" /> source generator to eliminate boxing allocations
///     and guard-check overhead (CA1848).
/// </summary>
public static partial class MutationLogger
{
    // ──────────────────────────────────────────────────
    //  Mutation lifecycle — Called / Completed
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a GraphQL mutation was called for a specific entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation (e.g., "CreateSceneObjectAsync").</param>
    /// <param name="entityId">The unique identifier of the entity being operated on.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} called | EntityId={EntityId}")]
    public static partial void LogMutationCalled(
        this ILogger logger,
        string operation,
        Guid entityId);

    /// <summary>
    ///     Logs that a GraphQL mutation was called without a specific entity identifier,
    ///     typically for operations that act on the currently loaded procedure or system state.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation (e.g., "StartLoadedProcedureAsync").</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} called")]
    public static partial void LogMutationCalledSimple(
        this ILogger logger,
        string operation);

    /// <summary>
    ///     Logs that a GraphQL mutation completed successfully for a specific entity.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation that completed.</param>
    /// <param name="entityId">The unique identifier of the entity that was operated on.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} completed | EntityId={EntityId}")]
    public static partial void LogMutationCompleted(
        this ILogger logger,
        string operation,
        Guid entityId);

    /// <summary>
    ///     Logs that a GraphQL mutation completed for a specific entity, including whether
    ///     the operation succeeded. Used by update and delete mutations that return a boolean result.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation that completed.</param>
    /// <param name="entityId">The unique identifier of the entity that was operated on.</param>
    /// <param name="success">Whether the mutation operation succeeded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} completed | EntityId={EntityId} | Success={Success}")]
    public static partial void LogMutationCompletedWithSuccess(
        this ILogger logger,
        string operation,
        Guid entityId,
        bool success);

    /// <summary>
    ///     Logs that a GraphQL mutation completed with a descriptive result string.
    ///     Used by operations whose outcome is best expressed as a free-text value
    ///     (e.g., "StartLoadedProcedureAsync" returning an enum value).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation that completed.</param>
    /// <param name="result">A string describing the operation result.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} completed | Result={Result}")]
    public static partial void LogMutationCompletedWithResult(
        this ILogger logger,
        string operation,
        string result);

    // ──────────────────────────────────────────────────
    //  Mutation lifecycle — Rejected
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a GraphQL mutation was rejected before completing, including the reason.
    ///     Typically used when a domain-level precondition fails (e.g., execution already in progress).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation that was rejected.</param>
    /// <param name="reason">A human-readable description of why the mutation was rejected.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} rejected | Reason={Reason}")]
    public static partial void LogMutationRejected(
        this ILogger logger,
        string operation,
        string reason);

    // ──────────────────────────────────────────────────
    //  Variable operations
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a variable-related GraphQL mutation was called, including the owning
    ///     procedure and the variable name being operated on.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation (e.g., "AddVariableToProcedure").</param>
    /// <param name="procedureId">The unique identifier of the procedure owning the variable.</param>
    /// <param name="variableName">The name of the variable being operated on.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} called | ProcedureId={ProcedureId} | Variable={VariableName}")]
    public static partial void LogMutationCalledWithVariable(
        this ILogger logger,
        string operation,
        Guid procedureId,
        string variableName);

    /// <summary>
    ///     Logs that a procedure-scoped GraphQL mutation completed, identified by procedure ID.
    ///     Used after variable additions, updates, or removals that return the modified procedure.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation that completed.</param>
    /// <param name="procedureId">The unique identifier of the procedure that was modified.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} completed | ProcedureId={ProcedureId}")]
    public static partial void LogMutationCompletedForProcedure(
        this ILogger logger,
        string operation,
        Guid procedureId);

    // ──────────────────────────────────────────────────
    //  Procedure management
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a named GraphQL mutation was called, including the entity name.
    ///     Used by <c>CreateProcedureAsync</c> where the name is the primary identifier at call time.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operation">The name of the mutation operation (e.g., "CreateProcedureAsync").</param>
    /// <param name="name">The name supplied to the mutation (e.g., the new procedure name).</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Mutation: {Operation} called | Name={Name}")]
    public static partial void LogMutationCalledWithName(
        this ILogger logger,
        string operation,
        string name);
}