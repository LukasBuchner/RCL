using System.Text.RegularExpressions;
using FHOOE.Freydis.Application.Services.Branching.Support.Logging;
using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Branching;

/// <summary>
///     Service for selecting branches in RouterNodes based on selector expressions.
/// </summary>
public partial class BranchSelector : IBranchSelector
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<BranchSelector> _logger;

    /// <summary>
    ///     Initializes a new instance of the BranchSelector class.
    /// </summary>
    /// <param name="expressionEvaluator">Service for evaluating boolean expressions.</param>
    /// <param name="logger">Logger for branch selection operations.</param>
    public BranchSelector(
        IExpressionEvaluator expressionEvaluator,
        ILogger<BranchSelector> logger)
    {
        _expressionEvaluator = expressionEvaluator;
        _logger = logger;
    }

    /// <summary>
    ///     Selects exactly one branch from a RouterNode based on the selector expression and variable context.
    ///     Evaluates branches in priority order with short-circuit evaluation.
    /// </summary>
    /// <param name="router">The RouterNode to select a branch from.</param>
    /// <param name="context">The variable context for evaluating conditions.</param>
    /// <returns>The selected ConditionalBranch.</returns>
    /// <exception cref="InvalidOperationException">Thrown when router has no branches.</exception>
    /// <exception cref="AmbiguousBranchException">Thrown when multiple branches match with same priority.</exception>
    /// <exception cref="NoBranchMatchException">Thrown when no branches match and no default exists.</exception>
    public async Task<ConditionalBranch> SelectBranchAsync(
        RouterNode router,
        VariableContext context)
    {
        if (router.RouterTask.Branches == null || !router.RouterTask.Branches.Any())
            throw new InvalidOperationException(
                $"RouterNode '{router.RouterTask.Name}' has no branches");

        // Log router and selector details for debugging
        var selector = router.RouterTask.Selector;
        var selectorTypeName = selector?.GetType().Name ?? "(null)";
        var selectorExpression = (selector as SimpleVariableSelector)?.Expression ??
                                 (selector as ExpressionSelector)?.Expression ?? "(null)";
        _logger.LogBranchSelectionStarted(
            router.RouterTask.Name, router.Id,
            selectorTypeName,
            selectorExpression,
            router.RouterTask.Branches.Count);

        if (selector is SimpleVariableSelector simpleSelector)
            _logger.LogSimpleVariableSelectorDetails(
                simpleSelector.VariableName ?? "(null)", simpleSelector.Expression ?? "(null)");

        var orderedBranches = router.RouterTask.Branches
            .OrderBy(b => b.Priority)
            .ToList();

        // Evaluate non-default branches in priority order (short-circuit)
        var branchesByPriority = orderedBranches
            .Where(b => !b.IsDefaultBranch())
            .GroupBy(b => b.Priority)
            .OrderBy(g => g.Key);

        foreach (var priorityGroup in branchesByPriority)
        {
            var matchesInGroup = new List<ConditionalBranch>();

            foreach (var branch in priorityGroup)
            {
                var isMatch = await EvaluateBranchConditionAsync(
                    router.RouterTask.Selector,
                    branch,
                    context);

                if (isMatch) matchesInGroup.Add(branch);
            }

            // Check for ambiguity within same priority (FAIL FAST)
            if (matchesInGroup.Count > 1)
                throw new AmbiguousBranchException(matchesInGroup.Select(m => m.Name).ToList());

            // Return first match (short-circuit - don't evaluate lower priority branches)
            if (matchesInGroup.Count != 1) continue;
            _logger.LogBranchSelected(router.RouterTask.Name, matchesInGroup[0].Name);
            return matchesInGroup[0];
        }

        // Fall back to default branch
        var defaultBranch = orderedBranches.FirstOrDefault(b => b.IsDefaultBranch());
        if (defaultBranch == null) throw new NoBranchMatchException(router.RouterTask.Name);
        _logger.LogDefaultBranchSelected(router.RouterTask.Name, defaultBranch.Name);
        return defaultBranch;

        // No match and no default (FAIL FAST)
    }

    /// <summary>
    ///     Evaluates whether a branch condition matches based on the selector type and variable context.
    ///     For SimpleVariableSelector, only the specified variable is added to the evaluation context.
    ///     For ExpressionSelector, all variables are added to support complex expressions.
    /// </summary>
    /// <param name="selector">The selector expression defining how to evaluate.</param>
    /// <param name="branch">The branch whose condition to evaluate.</param>
    /// <param name="context">The variable context containing variable values.</param>
    /// <returns>True if the branch condition evaluates to true, false otherwise.</returns>
    /// <exception cref="VariableNotFoundException">Thrown when a required variable is not found in context.</exception>
    private async Task<bool> EvaluateBranchConditionAsync(
        SelectorExpression selector,
        ConditionalBranch branch,
        VariableContext context)
    {
        var selectorTypeName = selector.GetType().Name;
        var branchCondition = branch.Condition ?? "(null)";
        _logger.LogBranchConditionEvaluationStarted(
            branch.Name, branch.Priority, selectorTypeName, branchCondition);

        var evalContext = new Dictionary<string, object?>();

        // Build evaluation context based on selector type
        if (selector is SimpleVariableSelector simple)
        {
            // Get the variable name - either from selector or extract from condition as fallback
            var variableName = simple.VariableName;

            if (string.IsNullOrWhiteSpace(variableName))
            {
                // FALLBACK: Extract variable name from the branch condition
                variableName = ExtractVariableNameFromCondition(branch.Condition);

                if (string.IsNullOrWhiteSpace(variableName))
                {
                    _logger.LogEmptyVariableNameFallbackFailed(branch.Name, branch.Condition ?? "(null)");
                    return false;
                }

                _logger.LogVariableNameExtractedFromCondition(variableName, branch.Condition!);
            }

            _logger.LogVariableLookup(variableName, branch.Name);

            // Simple variable matching: read only the specified variable
            if (!context.TryGetValue(variableName, out var varValue))
            {
                var availableVars = string.Join(", ", context.GetAllValues().Select(v => v.Key));
                _logger.LogVariableNotFound(
                    variableName, branch.Name,
                    availableVars);
                throw new VariableNotFoundException(variableName);
            }

            // Prepare the evaluation context
            evalContext[variableName] = varValue;

            // Handle boolean-to-string comparison: if value is boolean and condition uses string literals,
            // convert boolean to lowercase string for comparison
            var conditionToEvaluate = branch.Condition!;
            if (varValue is bool boolValue && ConditionUsesStringComparison(branch.Condition))
            {
                var stringValue = boolValue.ToString().ToLowerInvariant();
                evalContext[variableName] = stringValue;

                _logger.LogBooleanToStringConversion(variableName, boolValue, stringValue);
            }

            var evalValue = evalContext[variableName];
            var evalValueTypeName = evalValue?.GetType().Name ?? "null";
            _logger.LogSimpleVariableEvaluation(
                variableName, evalValue, evalValueTypeName,
                conditionToEvaluate);

            var result = await _expressionEvaluator.EvaluateBooleanAsync(
                conditionToEvaluate,
                evalContext);

            _logger.LogSimpleVariableEvalResult(
                branch.Name, conditionToEvaluate, variableName, evalContext[variableName], result);

            return result;
        }

        // ExpressionSelector: add all variables to context for complex expressions
        var allValues = context.GetAllValues();
        var varsForLog = string.Join(", ", allValues.Select(v => $"{v.Key}='{v.Value.Value}'"));
        _logger.LogExpressionSelectorEvaluation(varsForLog, branch.Condition!);

        foreach (var (name, variableValue) in allValues) evalContext[name] = variableValue.Value;

        var expressionResult = await _expressionEvaluator.EvaluateBooleanAsync(
            branch.Condition!,
            evalContext);

        _logger.LogExpressionSelectorEvalResult(branch.Name, branch.Condition!, expressionResult);

        return expressionResult;
    }

    /// <summary>
    ///     Extracts the first variable name from a condition expression.
    ///     Looks for patterns like "VariableName == value" or "VariableName != value".
    /// </summary>
    /// <param name="condition">The condition expression to parse.</param>
    /// <returns>The extracted variable name, or null if not found.</returns>
    private static string? ExtractVariableNameFromCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return null;

        // Pattern to match: identifier followed by comparison operator
        // Examples: "QualityOK == true", "status != \"failed\"", "count > 10"
        var match = ComparisonExpressionRegex().Match(condition);

        if (match.Success)
            return match.Groups[1].Value;

        // Alternative: just take the first identifier-like word
        var identifierMatch = IdentifierRegex().Match(condition);
        return identifierMatch.Success ? identifierMatch.Groups[1].Value : null;
    }

    /// <summary>
    ///     Checks if a condition uses string comparison with boolean literals.
    ///     Detects patterns like: == "true", == "false", != "true", != "false"
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <returns>True if the condition compares against string boolean literals.</returns>
    private static bool ConditionUsesStringComparison(string? condition)
    {
        return !string.IsNullOrWhiteSpace(condition) &&
               // Check for string boolean literals: "true" or "false" (with quotes)
               StringBooleanLiteralRegex().IsMatch(condition);
    }

    [GeneratedRegex(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*(==|!=|>|<|>=|<=)")]
    private static partial Regex ComparisonExpressionRegex();

    [GeneratedRegex("([a-zA-Z_][a-zA-Z0-9_]*)")]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex("""
                    (==|!=)\s*"(true|false)"
                    """, RegexOptions.IgnoreCase)]
    private static partial Regex StringBooleanLiteralRegex();
}