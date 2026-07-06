using ValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.Domain.Entities.Variables;

/// <summary>
///     Design-time declaration of a variable in a procedure.
/// </summary>
public record VariableDefinition
{
    /// <summary>
    ///     Name of the variable.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Type descriptor for this variable.
    ///     Uses unified ValueType instead of enum.
    /// </summary>
    public required ValueType Type { get; set; }

    /// <summary>
    ///     Default value for the variable.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    ///     Scope level where the variable is accessible.
    /// </summary>
    public VariableScope Scope { get; set; } = VariableScope.Procedure;

    /// <summary>
    ///     Source of the variable's value.
    /// </summary>
    public VariableSource Source { get; set; } = VariableSource.UserDefined;

    /// <summary>
    ///     Description of the variable's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Whether the variable is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}