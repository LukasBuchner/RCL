namespace FHOOE.Freydis.GraphQLServer.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for GraphQL diagnostic
///     and error-handling services. Covers both <c>GraphQlDiagnosticEventListener</c> (request
///     lifecycle, resolver timing, validation, syntax errors) and <c>GraphQlErrorFilter</c>
///     (error classification, warning details). All methods are extension methods on
///     <see cref="ILogger" /> and use the <see cref="LoggerMessageAttribute" /> source generator
///     to eliminate boxing allocations and guard-check overhead (CA1848).
/// </summary>
public static partial class GraphQlServiceLogger
{
    // ──────────────────────────────────────────────────
    //  GraphQlDiagnosticEventListener — Request lifecycle
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a GraphQL request has started, including the operation type and request identifier.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operationType">The GraphQL operation type (e.g., "query", "mutation", "subscription").</param>
    /// <param name="requestId">The unique request identifier extracted from context data.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL Request Started: {OperationType} | RequestId: {RequestId}")]
    public static partial void LogRequestStarted(
        this ILogger logger,
        string? operationType,
        object? requestId);

    /// <summary>
    ///     Logs that a GraphQL request encountered an unhandled exception during execution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred during request execution.</param>
    /// <param name="message">The exception message describing the failure.</param>
    /// <param name="requestId">The unique request identifier extracted from context data.</param>
    /// <param name="query">The raw GraphQL query document that caused the error.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "GraphQL Request Error: {Message} | RequestId: {RequestId} | Query: {Query}")]
    public static partial void LogRequestError(
        this ILogger logger,
        Exception exception,
        string message,
        object? requestId,
        string? query);

    /// <summary>
    ///     Logs that a GraphQL field resolver produced an error, including the field name,
    ///     error path, and the exception type if available.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="fieldName">The name of the field whose resolver produced the error.</param>
    /// <param name="message">The error message from the resolver.</param>
    /// <param name="path">The GraphQL path where the error occurred.</param>
    /// <param name="exceptionType">The type name of the exception, or "No Exception" when absent.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "GraphQL Resolver Error in {FieldName}: {Message} | Path: {Path} | Exception: {ExceptionType}")]
    public static partial void LogResolverError(
        this ILogger logger,
        string fieldName,
        string message,
        string path,
        string exceptionType);

    /// <summary>
    ///     Logs a GraphQL validation error reported during query validation, including
    ///     the error code and path within the query.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="message">The validation error message.</param>
    /// <param name="code">The validation error code.</param>
    /// <param name="path">The path in the query where the validation error occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "GraphQL Validation Error: {Message} | Code: {Code} | Path: {Path}")]
    public static partial void LogValidationError(
        this ILogger logger,
        string message,
        string? code,
        string path);

    /// <summary>
    ///     Logs a GraphQL syntax error encountered during query parsing, including source
    ///     location information and the offending query text.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="message">The syntax error message.</param>
    /// <param name="locations">The formatted location string (e.g., "Line 3, Column 5").</param>
    /// <param name="query">The raw GraphQL query document that contained the syntax error.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "GraphQL Syntax Error: {Message} | Locations: {Locations} | Query: {Query}")]
    public static partial void LogSyntaxError(
        this ILogger logger,
        string message,
        string locations,
        string? query);

    // ──────────────────────────────────────────────────
    //  GraphQlDiagnosticEventListener — Resolver timing
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a field resolver has started execution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="fieldName">The fully qualified field name (e.g., "Query.agents").</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Resolver Started: {FieldName}")]
    public static partial void LogResolverStarted(
        this ILogger logger,
        string fieldName);

    /// <summary>
    ///     Logs that a GraphQL request completed successfully, including its operation type
    ///     and total execution duration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="operationType">The GraphQL operation type that completed.</param>
    /// <param name="duration">The total execution time in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "GraphQL Request Completed: {OperationType} | Duration: {Duration}ms")]
    public static partial void LogRequestCompleted(
        this ILogger logger,
        string operationType,
        long duration);

    /// <summary>
    ///     Logs a warning that a field resolver took longer than the slow-resolver threshold,
    ///     indicating potential performance issues.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="fieldName">The fully qualified field name of the slow resolver.</param>
    /// <param name="duration">The resolver execution time in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Slow Resolver: {FieldName} | Duration: {Duration}ms")]
    public static partial void LogSlowResolver(
        this ILogger logger,
        string fieldName,
        long duration);

    /// <summary>
    ///     Logs that a field resolver completed within normal timing thresholds.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="fieldName">The fully qualified field name of the completed resolver.</param>
    /// <param name="duration">The resolver execution time in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Resolver Completed: {FieldName} | Duration: {Duration}ms")]
    public static partial void LogResolverCompleted(
        this ILogger logger,
        string fieldName,
        long duration);

    // ──────────────────────────────────────────────────
    //  GraphQlErrorFilter — Error classification
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs a GraphQL error that has an associated exception, including the error code,
    ///     path, location, and extension data.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception associated with the GraphQL error.</param>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    /// <param name="path">The formatted path where the error occurred.</param>
    /// <param name="locations">The formatted location string for the error.</param>
    /// <param name="extensions">The error extension data.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "GraphQL Error: {Message} | Code: {Code} | Path: {Path} | Locations: {Locations} | Extensions: {Extensions}")]
    public static partial void LogGraphQlErrorWithException(
        this ILogger logger,
        Exception exception,
        string message,
        string? code,
        string path,
        string locations,
        object? extensions);

    /// <summary>
    ///     Logs a GraphQL warning for errors that have no associated exception, including the
    ///     error code, path, location, and extension data.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="code">The error code.</param>
    /// <param name="path">The formatted path where the warning originated.</param>
    /// <param name="locations">The formatted location string for the warning.</param>
    /// <param name="extensions">The error extension data.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "GraphQL Warning: {Message} | Code: {Code} | Path: {Path} | Locations: {Locations} | Extensions: {Extensions}")]
    public static partial void LogGraphQlWarning(
        this ILogger logger,
        string message,
        string? code,
        string path,
        string locations,
        object? extensions);

    /// <summary>
    ///     Logs the full error details object at debug level for diagnostic purposes.
    ///     Callers should guard with <c>IsEnabled(LogLevel.Debug)</c> to avoid constructing
    ///     the anonymous error info object when debug logging is disabled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="errorInfo">The full error details object for diagnostic inspection.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Full GraphQL Error Details: {ErrorInfo}")]
    public static partial void LogGraphQlErrorDetails(
        this ILogger logger,
        object errorInfo);
}