using FHOOE.Freydis.Domain.Entities.Variables;
using DomainValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     GraphQL input type for creating or updating variable definitions.
/// </summary>
public record VariableDefinitionInput
{
    /// <summary>
    ///     Name of the variable.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Type descriptor for this variable.
    /// </summary>
    public required DomainValueType Type { get; init; }

    /// <summary>
    ///     Default value for the variable.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    ///     Scope level where the variable is accessible.
    /// </summary>
    public VariableScope Scope { get; init; } = VariableScope.Procedure;

    /// <summary>
    ///     Source of the variable's value.
    /// </summary>
    public VariableSource Source { get; init; } = VariableSource.UserDefined;

    /// <summary>
    ///     Description of the variable's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Whether the variable is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }
}