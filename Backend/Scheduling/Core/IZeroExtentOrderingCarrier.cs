namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Marks an <see cref="IPlannedSkillExecution" /> as a zero-extent ordering carrier: a synthetic node
///     materialised for a leafless container (an empty task, or a router whose selected branch carries no
///     executable work) solely to preserve dependency ordering through it, so a chain
///     <c>A → empty → B</c> still orders <c>B</c> after <c>A</c>.
/// </summary>
/// <remarks>
///     A carrier has zero planned duration (so <c>PlannedFinish = PlannedStart</c>) and carries no execution
///     state. In the schedule it is a first-class kind, distinct from finished, running, and not-yet-started
///     tasks: it is exempt from the current-time floor that applies to not-yet-started tasks, so its start is
///     bounded only by its dependency predecessors' finishes and the makespan objective. It therefore settles
///     at the maximum finish of its predecessors and never pushes an already-finished successor forward.
/// </remarks>
public interface IZeroExtentOrderingCarrier : IPlannedSkillExecution
{
}