using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Variables.Support.Logging;

/// <summary>
///     Provides structured logging for variable resolution and management operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class VariableLogger
{
    /// <summary>
    ///     Logs the successful initialization of a variable context for a procedure execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="executionId">The unique identifier of the procedure execution.</param>
    /// <param name="count">The number of variables initialized in the context.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initialized variable context for execution {ExecutionId} with {Count} variables")]
    public static partial void LogVariableContextInitialized(
        this ILogger logger,
        Guid executionId,
        int count);

    /// <summary>
    ///     Logs a successful update of a variable value within an execution context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable that was updated.</param>
    /// <param name="executionId">The unique identifier of the procedure execution context.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updated variable '{VariableName}' in context {ExecutionId}")]
    public static partial void LogVariableUpdated(
        this ILogger logger,
        string variableName,
        Guid executionId);
}