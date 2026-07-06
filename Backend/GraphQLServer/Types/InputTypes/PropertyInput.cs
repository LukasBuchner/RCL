using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     Input type for property data.
/// </summary>
public record PropertyInput
{
    /// <summary>
    ///     Name of the property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Direction of data flow for the property.
    /// </summary>
    public required PropertyDirection Direction { get; set; }

    /// <summary>
    ///     Optional variable binding for this property.
    /// </summary>
    public VariableBindingInput? Binding { get; set; }

    /// <summary>
    ///     Typed value of the property.
    /// </summary>
    [GraphQLName("value")]
    public required PropertyTypeInput PropertyType { get; set; }
}