namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Composite result of all pre-execution validation checks run against a procedure graph.
///     Each property corresponds to one category of structural constraint; a result with no
///     violations in any category permits execution to proceed.
/// </summary>
/// <remarks>
///     Additional validator result fields should be added here as new validation rules are
///     introduced, keeping <see cref="HasViolations" /> as the single aggregated gate.
/// </remarks>
public record ProcedureValidationResult
{
    /// <summary>
    ///     Violations produced by the agent serialization validator. Each entry describes one
    ///     physical agent whose assigned skills lack the Finish-to-Start ordering required to
    ///     prevent concurrent robot dispatch.
    ///     Defaults to an empty list when no violations are found.
    /// </summary>
    public IReadOnlyList<AgentSerializationViolation> AgentSerializationViolations { get; init; }
        = [];

    // Future validator result fields go here, following the same pattern.

    /// <summary>
    ///     <see langword="true" /> when any validation category contains at least one violation,
    ///     indicating that execution must not proceed; <see langword="false" /> when all checks
    ///     passed and the procedure graph is structurally sound.
    /// </summary>
    public bool HasViolations => AgentSerializationViolations.Count > 0;
}