namespace FHOOE.Freydis.Application.Configuration;

/// <summary>
///     Configuration for the observability-only adaptive-skill overrun monitor. The monitor
///     watches how long an executing skill has run relative to its scheduled finish and raises
///     an advisory to the operator when it overruns. It is purely advisory: it never affects
///     scheduling, triggering, or orchestration.
/// </summary>
public class AdaptiveSkillMonitoringConfiguration
{
    /// <summary>
    ///     Whether the overrun monitor is active. Default is <see langword="true" />.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Seconds a skill may run past its scheduled finish before a warning advisory is raised.
    ///     Generous by design — this should flag "running far longer than the schedule expected",
    ///     not normal adaptive stretching. Default is 30 s.
    /// </summary>
    public double OverrunMarginSeconds { get; set; } = 30.0;

    /// <summary>
    ///     Seconds past the scheduled finish at which the advisory is escalated to a more prominent
    ///     warning. Default is 120 s.
    /// </summary>
    public double EscalateMarginSeconds { get; set; } = 120.0;
}