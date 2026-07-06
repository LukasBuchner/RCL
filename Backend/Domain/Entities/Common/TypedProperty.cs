namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     TypedProperty definition with typed value, direction, and optional variable binding.
/// </summary>
public record TypedProperty
{
    /// <summary>
    ///     Name of the property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Typed value of the property.
    /// </summary>
    public required TypedValue Value { get; set; }

    /// <summary>
    ///     Direction of data flow for this property.
    /// </summary>
    public required PropertyDirection Direction { get; init; }

    /// <summary>
    ///     Optional variable binding. Null means property is not bound to a variable.
    /// </summary>
    public VariableBinding? Binding { get; init; }
}