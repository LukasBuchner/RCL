namespace FHOOE.Freydis.Application.Services.Expressions;

/// <summary>
///     Service for evaluating expressions safely using DynamicExpresso.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    ///     Evaluates an expression with provided variable context.
    /// </summary>
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object?> context);

    /// <summary>
    ///     Evaluates a boolean expression.
    /// </summary>
    Task<bool> EvaluateBooleanAsync(string expression, Dictionary<string, object?> context);

    /// <summary>
    ///     Validates expression syntax without evaluation.
    /// </summary>
    bool ValidateSyntax(string expression, out string? errorMessage);
}