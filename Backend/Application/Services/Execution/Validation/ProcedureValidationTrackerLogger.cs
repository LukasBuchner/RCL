using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Source-generated, high-performance log methods for <see cref="ProcedureValidationTracker" />.
///     All messages are structured so that log aggregation tools can filter and alert on
///     individual fields without parsing message text.
/// </summary>
public static partial class ProcedureValidationTrackerLogger
{
    /// <summary>
    ///     Emitted each time the reactive pipeline produces a new validation result, indicating
    ///     how many agent serialization violations are currently present.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="violationCount">
    ///     The number of <see cref="AgentSerializationViolation" /> entries in the latest result.
    ///     Zero means the procedure graph is structurally sound with respect to agent serialization.
    /// </param>
    [LoggerMessage(
        EventId = 5010,
        Level = LogLevel.Information,
        Message = "PROCEDURE_VALIDATION | UPDATED | AgentSerializationViolationCount={ViolationCount}")]
    public static partial void LogProcedureValidationUpdated(
        this ILogger logger,
        int violationCount);

    /// <summary>
    ///     Emitted when an unhandled exception propagates out of the reactive pipeline,
    ///     terminating the subscription.  After this event, no further validation results
    ///     are emitted until the tracker is recreated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception that caused the pipeline to terminate.</param>
    [LoggerMessage(
        EventId = 5011,
        Level = LogLevel.Error,
        Message = "PROCEDURE_VALIDATION | PIPELINE_ERROR | ProcedureValidationTracker reactive pipeline terminated")]
    public static partial void LogProcedureValidationTrackerError(
        this ILogger logger,
        Exception ex);
}