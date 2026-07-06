namespace FHOOE.Freydis.GraphQLServer.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for application startup
///     and validation operations. Covers <c>StartupValidationService</c> (PostgreSQL connectivity,
///     service registration, GraphQL schema, and application service validation) and
///     <c>AgentServiceExtensions</c> (factory registration during DI setup). All methods are
///     extension methods on <see cref="ILogger" /> and use the <see cref="LoggerMessageAttribute" />
///     source generator to eliminate boxing allocations and guard-check overhead (CA1848).
/// </summary>
public static partial class StartupLogger
{
    // ──────────────────────────────────────────────────
    //  StartupValidationService — Lifecycle
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the application startup validation process has begun.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting application validation...")]
    public static partial void LogStartingValidation(
        this ILogger logger);

    /// <summary>
    ///     Logs that all validation checks completed successfully, including the total elapsed time.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="duration">The total validation duration in milliseconds.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Application validation completed successfully in {Duration}ms")]
    public static partial void LogValidationCompleted(
        this ILogger logger,
        long duration);

    /// <summary>
    ///     Logs an unexpected error that occurred during the startup validation process,
    ///     outside the scope of any specific validation step.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The unexpected exception that occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unexpected error during startup validation")]
    public static partial void LogUnexpectedValidationError(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs that the startup validation completed with errors, including the number of
    ///     errors and a formatted summary of each individual error.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="errorCount">The number of validation errors that were detected.</param>
    /// <param name="formattedErrors">A formatted string containing all validation error messages.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Startup validation failed with {ErrorCount} error(s):\n{FormattedErrors}")]
    public static partial void LogValidationFailed(
        this ILogger logger,
        int errorCount,
        string formattedErrors);

    // ──────────────────────────────────────────────────
    //  StartupValidationService — PostgreSQL
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the PostgreSQL connection was validated successfully during startup.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "PostgreSQL connection validated successfully")]
    public static partial void LogPostgresValidated(
        this ILogger logger);

    // ──────────────────────────────────────────────────
    //  StartupValidationService — Service registration
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the result of the service registration validation step, including
    ///     the number of errors found during registration checks.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="errorCount">The number of service registration errors found.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Service registration validation completed with {ErrorCount} errors")]
    public static partial void LogServiceValidationCompleted(
        this ILogger logger,
        int errorCount);

    // ──────────────────────────────────────────────────
    //  StartupValidationService — GraphQL schema
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the GraphQL schema was validated successfully during startup.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GraphQL schema validated successfully")]
    public static partial void LogSchemaValidated(
        this ILogger logger);

    /// <summary>
    ///     Logs an error when the GraphQL schema validation step fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that caused the schema validation failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Detailed GraphQL schema validation error")]
    public static partial void LogSchemaValidationError(
        this ILogger logger,
        Exception exception);

    // ──────────────────────────────────────────────────
    //  StartupValidationService — Application services
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the agent application service was validated successfully, including
    ///     the number of agents found in the system.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentCount">The number of agents found during validation.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent service validated, found {AgentCount} agents")]
    public static partial void LogAgentServiceValidated(
        this ILogger logger,
        int agentCount);

    /// <summary>
    ///     Logs that the skill application service was validated successfully, including
    ///     the number of skills found in the system.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillCount">The number of skills found during validation.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skill service validated, found {SkillCount} skills")]
    public static partial void LogSkillServiceValidated(
        this ILogger logger,
        int skillCount);

    /// <summary>
    ///     Logs that the node application service was validated successfully, including
    ///     the number of nodes found in the system.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeCount">The number of nodes found during validation.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Node service validated, found {NodeCount} nodes")]
    public static partial void LogNodeServiceValidated(
        this ILogger logger,
        int nodeCount);

    // ──────────────────────────────────────────────────
    //  AgentServiceExtensions — Factory registration
    // ──────────────────────────────────────────────────
}