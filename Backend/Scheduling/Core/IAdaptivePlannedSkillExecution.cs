namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Interface extending ISkillExecution for tasks with adaptable durations.
///     The duration is bounded below by a minimum and is unbounded above.
/// </summary>
public interface IAdaptivePlannedSkillExecution : IPlannedSkillExecution
{
    /// <summary>
    ///     The minimum allowable duration for this task. The duration is unbounded above.
    /// </summary>
    double MinDuration { get; set; }
}