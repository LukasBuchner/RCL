namespace FHOOE.Freydis.Scheduling.Core;

public interface IExecutionGraph
{
    /// <summary>
    ///     All tasks in the schedule, including both fixed and adaptive durations.
    /// </summary>
    IReadOnlyList<IPlannedSkillExecution> SkillExecutions { get; set; }

    /// <summary>
    ///     All dependencies between tasks in the schedule.
    /// </summary>
    IReadOnlyList<Dependency> Dependencies { get; set; }
}