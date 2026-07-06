using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     Input type for variable binding data.
/// </summary>
public record VariableBindingInput
{
    /// <summary>
    ///     Name of the variable to bind to.
    /// </summary>
    public required string VariableName { get; set; }

    /// <summary>
    ///     Mode of binding operation (Read, Write, or ReadWrite).
    /// </summary>
    public required BindingMode Mode { get; set; }

    /// <summary>
    ///     Optional expression to transform the value during binding.
    /// </summary>
    public string? TransformExpression { get; set; }
}