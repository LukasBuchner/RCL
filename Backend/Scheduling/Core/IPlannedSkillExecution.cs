namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Common interface for all executable tasks/skills in the schedule.
/// </summary>
public interface IPlannedSkillExecution
{
    /// <summary>
    ///     Unique identifier for the task.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     The calculated start time relative to the schedule's T0.
    ///     Set during the planning phase (including SCC solving).
    /// </summary>
    double PlannedStartTime { get; set; }

    /// <summary>
    ///     The calculated finish time relative to the schedule's T0.
    ///     Set during the planning phase (including SCC solving).
    /// </summary>
    double PlannedFinishTime { get; set; }

    /// <summary>
    ///     The duration used for planning (either fixed or the result of solving for adaptive).
    ///     PlannedFinish = PlannedStart + PlannedDuration
    /// </summary>
    double PlannedDuration { get; set; }
}