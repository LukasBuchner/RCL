using HotChocolate.Types;

namespace FHOOE.Freydis.Domain.Entities.Procedure;

/// <summary>
///     Base class for selector expressions that evaluate to choose a branch.
///     Supports two types: SimpleVariableSelector for single variable lookups,
///     and ExpressionSelector for complex boolean expressions with multiple variables.
/// </summary>
[UnionType("SelectorExpression")]
public abstract record SelectorExpression
{
    /// <summary>
    ///     The expression string to evaluate.
    /// </summary>
    public required string Expression { get; init; }
}

/// <summary>
///     Simple selector that evaluates a single variable value.
///     Only adds the specified variable to the evaluation context.
///     Example: "quality_result" - directly uses variable value for matching.
/// </summary>
public record SimpleVariableSelector : SelectorExpression
{
    /// <summary>
    ///     Name of the variable to evaluate.
    ///     Derived from the Expression property for convenience.
    /// </summary>
    public string VariableName => Expression;
}

/// <summary>
///     Expression selector for boolean expressions referencing one or more variables.
///     Adds all variables to the evaluation context for expression evaluation.
///     Examples: "temperature > 100 and 50 > pressure"
/// </summary>
public record ExpressionSelector : SelectorExpression;