namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     GraphQL input type for selector expressions in router nodes using OneOf pattern.
///     Exactly one of the selector type properties must be set.
///     Supports two types: SimpleVariableSelector for single variable lookups,
///     and ExpressionSelector for complex boolean expressions with multiple variables.
/// </summary>
[OneOf]
public record SelectorExpressionInput
{
    /// <summary>
    ///     Simple selector that evaluates a single variable value.
    ///     Example: "qualityResult" - directly uses variable value for matching.
    /// </summary>
    public SimpleVariableSelectorInput? SimpleVariableSelector { get; set; }

    /// <summary>
    ///     Expression selector for boolean expressions referencing one or more variables.
    ///     Examples: "temperature > 100 && pressure < 50", "Math.Max(temp1, temp2) > threshold"
    /// </summary>
    public ExpressionSelectorInput? ExpressionSelector { get; set; }
}

/// <summary>
///     Simple selector input that evaluates a single variable value.
///     Example: "qualityResult" - directly uses variable value for matching.
/// </summary>
public record SimpleVariableSelectorInput
{
    /// <summary>
    ///     The expression string to evaluate.
    /// </summary>
    public required string Expression { get; init; }
}

/// <summary>
///     Expression selector input for boolean expressions referencing one or more variables.
///     Examples: "temperature > 100 && pressure < 50", "Math.Max(temp1, temp2) > threshold"
/// </summary>
public record ExpressionSelectorInput
{
    /// <summary>
    ///     The expression string to evaluate.
    /// </summary>
    public required string Expression { get; init; }
}