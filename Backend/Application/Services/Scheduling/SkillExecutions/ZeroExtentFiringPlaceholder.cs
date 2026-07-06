namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

/// <summary>
///     A zero-extent firing endpoint materialized for a leafless container (an empty task, or a router
///     whose selected branch carries no executable work) so that a dependency chain through it
///     (<c>A → empty → B</c>) is preserved in the LP rather than dropped. The container is carried as a
///     real, zero-duration node: it gets Start/Finish/Duration LP variables like any task, and because its
///     <see cref="PlannedDuration" /> is <c>0</c> the LP pins <c>Finish = Start</c>, making it an inert
///     ordering carrier (Start of the successor is bounded by Finish of the predecessor through it).
/// </summary>
/// <remarks>
///     <para>
///         This type implements the Core marker
///         <see cref="FHOOE.Freydis.Scheduling.Core.IZeroExtentOrderingCarrier" /> (and thereby the base
///         <see cref="FHOOE.Freydis.Scheduling.Core.IPlannedSkillExecution" />) and deliberately not the
///         application-level <c>IPlannedSkillExecution</c>, <c>ISkillExecution</c>, or
///         <c>IAdaptivePlannedSkillExecution</c>. The scheduler classifies it as a first-class ordering
///         carrier via the marker; and because it lacks the richer contracts, every consumer that must ignore
///         the placeholder still filters it out, so the placeholder is excluded from
///     </para>
///     <list type="bullet">
///         <item><description>the LP duration arm — it is not an <c>ISkillExecution</c>, so its effective
///         duration is its own <see cref="PlannedDuration" /> (<c>0</c>), giving <c>Finish = Start</c>;</description></item>
///         <item><description>timeline display — it is neither <c>PlannedSkillExecution</c> nor an application
///         <c>IPlannedSkillExecution</c>, so the schedule extraction and Part A positioning skip it;</description></item>
///         <item><description>completion tracking and agent-serialization — both operate over the procedure's
///         <c>SkillExecutionNode</c>s, never over this placeholder.</description></item>
///     </list>
/// </remarks>
public sealed class ZeroExtentFiringPlaceholder : FHOOE.Freydis.Scheduling.Core.IZeroExtentOrderingCarrier
{
    /// <summary>The leafless container's node ID; the placeholder shares it so edges resolve to it.</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable name for diagnostics only; not used in scheduling.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The LP-assigned start time. Equal to <see cref="PlannedFinishTime" /> (zero extent).</summary>
    public double PlannedStartTime { get; set; }

    /// <summary>The LP-assigned finish time. Equal to <see cref="PlannedStartTime" /> (zero extent).</summary>
    public double PlannedFinishTime { get; set; }

    /// <summary>Always <c>0</c>: the container carries no executable work, so the LP pins Finish = Start.</summary>
    public double PlannedDuration { get; set; }
}