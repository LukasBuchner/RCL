using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Branching.Support.Logging;

/// <summary>
///     Provides structured logging for branch selection operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class BranchingLogger
{
    /// <summary>
    ///     Logs the start of a branch selection evaluation for a RouterNode, including
    ///     selector type and branch count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router task.</param>
    /// <param name="routerId">The unique identifier of the router node.</param>
    /// <param name="selectorType">The CLR type name of the selector expression.</param>
    /// <param name="expression">The expression string from the selector, or "(null)" if absent.</param>
    /// <param name="branchCount">The number of branches available for selection.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "SelectBranchAsync: Router='{RouterName}' (ID={RouterId}), SelectorType={SelectorType}, SelectorExpression='{Expression}', BranchCount={BranchCount}")]
    public static partial void LogBranchSelectionStarted(
        this ILogger logger,
        string routerName,
        Guid routerId,
        string selectorType,
        string expression,
        int branchCount);

    /// <summary>
    ///     Logs details of a SimpleVariableSelector including the variable name and expression.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The variable name from the selector, or "(null)" if absent.</param>
    /// <param name="expression">The expression string from the selector, or "(null)" if absent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "SimpleVariableSelector details: VariableName='{VariableName}', Expression='{Expression}'")]
    public static partial void LogSimpleVariableSelectorDetails(
        this ILogger logger,
        string variableName,
        string expression);

    /// <summary>
    ///     Logs that a specific conditional branch was selected by the router.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router task that made the selection.</param>
    /// <param name="branchName">The name of the selected branch.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Router '{RouterName}' selected branch '{BranchName}'")]
    public static partial void LogBranchSelected(
        this ILogger logger,
        string routerName,
        string branchName);

    /// <summary>
    ///     Logs that the router fell back to the default branch because no conditional branch matched.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router task that fell back to default.</param>
    /// <param name="branchName">The name of the default branch.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Router '{RouterName}' matched no conditional branch; falling back to default branch '{BranchName}'")]
    public static partial void LogDefaultBranchSelected(
        this ILogger logger,
        string routerName,
        string branchName);

    /// <summary>
    ///     Logs the start of a branch condition evaluation with the condition expression.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the branch being evaluated.</param>
    /// <param name="priority">The priority of the branch.</param>
    /// <param name="selectorType">The CLR type name of the selector expression.</param>
    /// <param name="condition">The condition expression being evaluated, or "(null)" if absent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Evaluating branch '{BranchName}' (Priority={Priority}) with selector type {SelectorType}, Condition='{Condition}'")]
    public static partial void LogBranchConditionEvaluationStarted(
        this ILogger logger,
        string branchName,
        int priority,
        string selectorType,
        string condition);

    /// <summary>
    ///     Logs a warning when a SimpleVariableSelector has an empty variable name and the
    ///     name could not be extracted from the condition expression.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the branch that could not be evaluated.</param>
    /// <param name="condition">The condition expression that was inspected, or "(null)" if absent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SimpleVariableSelector has empty VariableName and could not extract from condition for branch '{BranchName}'. Condition='{Condition}'. Skipping condition evaluation, treating as non-match.")]
    public static partial void LogEmptyVariableNameFallbackFailed(
        this ILogger logger,
        string branchName,
        string condition);

    /// <summary>
    ///     Logs that a variable name was extracted by parsing the condition expression because
    ///     the SimpleVariableSelector had an empty configured VariableName.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The variable name extracted from the condition.</param>
    /// <param name="condition">The condition expression from which the name was extracted.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SimpleVariableSelector had an empty configured VariableName; falling back to variable '{VariableName}' parsed from condition '{Condition}'")]
    public static partial void LogVariableNameExtractedFromCondition(
        this ILogger logger,
        string variableName,
        string condition);

    /// <summary>
    ///     Logs a variable lookup attempt for a SimpleVariableSelector evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being looked up.</param>
    /// <param name="branchName">The name of the branch the variable is being looked up for.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "SimpleVariableSelector: Looking up variable '{VariableName}' for branch '{BranchName}'")]
    public static partial void LogVariableLookup(
        this ILogger logger,
        string variableName,
        string branchName);

    /// <summary>
    ///     Logs a warning when a required variable is not found in the evaluation context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable that was not found.</param>
    /// <param name="branchName">The name of the branch requiring the variable.</param>
    /// <param name="availableVariables">Comma-separated list of available variable names.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Variable '{VariableName}' not found in context for branch '{BranchName}'. Available variables: [{AvailableVariables}]")]
    public static partial void LogVariableNotFound(
        this ILogger logger,
        string variableName,
        string branchName,
        string availableVariables);

    /// <summary>
    ///     Logs a boolean-to-string conversion performed for string comparison in a condition.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being converted.</param>
    /// <param name="boolValue">The original boolean value.</param>
    /// <param name="stringValue">The converted lowercase string value.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "[BRANCH EVAL] Boolean-to-string conversion: '{VariableName}' = {BoolValue} -> \"{StringValue}\" for string comparison")]
    public static partial void LogBooleanToStringConversion(
        this ILogger logger,
        string variableName,
        bool boolValue,
        string stringValue);

    /// <summary>
    ///     Logs the variable value and condition being evaluated for a SimpleVariableSelector.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being evaluated.</param>
    /// <param name="value">The current value of the variable.</param>
    /// <param name="valueType">The CLR type name of the variable value.</param>
    /// <param name="condition">The condition expression being evaluated.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "[BRANCH EVAL] Variable '{VariableName}' = '{Value}' (type: {ValueType}), evaluating condition: '{Condition}'")]
    public static partial void LogSimpleVariableEvaluation(
        this ILogger logger,
        string variableName,
        object? value,
        string valueType,
        string condition);

    /// <summary>
    ///     Logs the result of a SimpleVariableSelector branch condition evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the evaluated branch.</param>
    /// <param name="condition">The condition expression that was evaluated.</param>
    /// <param name="variableName">The name of the variable used in the evaluation.</param>
    /// <param name="value">The value of the variable during evaluation.</param>
    /// <param name="result">The boolean result of the evaluation.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "[BRANCH EVAL RESULT] Branch '{BranchName}' condition '{Condition}' with {VariableName}='{Value}' => {Result}")]
    public static partial void LogSimpleVariableEvalResult(
        this ILogger logger,
        string branchName,
        string condition,
        string variableName,
        object? value,
        bool result);

    /// <summary>
    ///     Logs the variables and condition being evaluated for an ExpressionSelector.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variables">A formatted string listing all variable name-value pairs.</param>
    /// <param name="condition">The condition expression being evaluated.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[BRANCH EVAL] ExpressionSelector: Variables [{Variables}], evaluating condition: '{Condition}'")]
    public static partial void LogExpressionSelectorEvaluation(
        this ILogger logger,
        string variables,
        string condition);

    /// <summary>
    ///     Logs the result of an ExpressionSelector branch condition evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the evaluated branch.</param>
    /// <param name="condition">The condition expression that was evaluated.</param>
    /// <param name="result">The boolean result of the evaluation.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[BRANCH EVAL RESULT] Branch '{BranchName}' condition '{Condition}' => {Result}")]
    public static partial void LogExpressionSelectorEvalResult(
        this ILogger logger,
        string branchName,
        string condition,
        bool result);
}