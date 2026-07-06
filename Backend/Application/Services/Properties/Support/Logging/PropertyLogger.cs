using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Properties.Support.Logging;

/// <summary>
///     Provides structured logging for property binding operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class PropertyLogger
{
    /// <summary>
    ///     Logs a successful resolution of an input property binding from a variable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="propertyName">The name of the skill property that was resolved.</param>
    /// <param name="variableName">The name of the variable the property was bound to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Resolved input binding for property '{PropertyName}' from variable '{VariableName}'")]
    public static partial void LogInputBindingResolved(
        this ILogger logger,
        string propertyName,
        string variableName);

    /// <summary>
    ///     Logs a successful application of an output property binding to a variable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="propertyName">The name of the skill property that was applied.</param>
    /// <param name="variableName">The name of the variable the property was written to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applied output binding for property '{PropertyName}' to variable '{VariableName}'")]
    public static partial void LogOutputBindingApplied(
        this ILogger logger,
        string propertyName,
        string variableName);

    /// <summary>
    ///     Logs that a configured transform expression evaluated to null for an output property;
    ///     the un-transformed skill output value is written to the variable instead.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="propertyName">The name of the output property whose transform produced null.</param>
    /// <param name="variableName">The name of the variable receiving the un-transformed value.</param>
    /// <param name="transformExpression">The transform expression that evaluated to null.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Transform expression for output property '{PropertyName}' (variable '{VariableName}') evaluated to null; discarding transform and writing the un-transformed skill output value instead. Expression: '{TransformExpression}'")]
    public static partial void LogTransformEvaluatedToNull(
        this ILogger logger,
        string propertyName,
        string variableName,
        string transformExpression);
}