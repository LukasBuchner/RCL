namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Severity of an operator-facing execution advisory.
/// </summary>
public enum ExecutionAdvisorySeverity
{
    /// <summary>
    ///     An informational warning that a skill is running past its scheduled finish.
    /// </summary>
    Warning,

    /// <summary>
    ///     A more prominent warning that a skill has grossly overrun its scheduled finish.
    /// </summary>
    Escalated
}

/// <summary>
///     An advisory raised for the operator about an executing skill. Advisories are observability
///     only — they never gate, trigger, or alter execution. They are surfaced to the frontend and
///     logged, not consumed by the scheduling or triggering layers.
/// </summary>
public record ExecutionAdvisory
{
    /// <summary>
    ///     Identifier of the node/skill the advisory concerns.
    /// </summary>
    public required Guid SkillId { get; init; }

    /// <summary>
    ///     Human-readable name of the skill, for display.
    /// </summary>
    public required string SkillName { get; init; }

    /// <summary>
    ///     Seconds the skill has run past its scheduled finish at the time the advisory was raised.
    /// </summary>
    public required double OverrunSeconds { get; init; }

    /// <summary>
    ///     The scheduled finish time (procedure-relative seconds) the skill overran.
    /// </summary>
    public required double ScheduledFinishSeconds { get; init; }

    /// <summary>
    ///     Severity of the advisory.
    /// </summary>
    public required ExecutionAdvisorySeverity Severity { get; init; }

    /// <summary>
    ///     The advisory message, phrased as guidance rather than an assertion of failure.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     The UTC time the advisory was raised.
    /// </summary>
    public required DateTimeOffset RaisedAtUtc { get; init; }
}