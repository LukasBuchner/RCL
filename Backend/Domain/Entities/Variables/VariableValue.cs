namespace FHOOE.Freydis.Domain.Entities.Variables;

/// <summary>
///     Runtime value holder for a variable.
/// </summary>
public record VariableValue
{
    /// <summary>
    ///     Name of the variable.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Current value of the variable.
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    ///     Timestamp when the value was last updated (UTC).
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }

    /// <summary>
    ///     Identifier of the entity that last updated the value.
    /// </summary>
    public string? LastUpdatedBy { get; set; }
}