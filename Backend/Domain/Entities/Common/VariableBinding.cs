namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Binds a property to a procedure variable for data flow.
/// </summary>
public record VariableBinding
{
    /// <summary>
    ///     Name of the variable to bind to.
    /// </summary>
    public required string VariableName { get; init; }

    /// <summary>
    ///     Mode of binding operation (Read, Write, or ReadWrite).
    /// </summary>
    public required BindingMode Mode { get; init; }

    /// <summary>
    ///     Optional expression to transform the value during binding.
    ///     Evaluation happens in Application layer (Phase 3).
    ///     Examples: "value / 1000", "value.ToUpper()", "value > 100"
    /// </summary>
    public string? TransformExpression { get; init; }
}