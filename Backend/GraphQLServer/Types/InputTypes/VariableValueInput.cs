namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     GraphQL input type for providing variable values at execution time.
/// </summary>
public record VariableValueInput
{
    /// <summary>
    ///     Name of the variable.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Value to assign to the variable.
    ///     Type should match the variable's declared type.
    /// </summary>
    public required object Value { get; init; }
}