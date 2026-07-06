namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Represents a graph of tasks and their dependencies for scheduling.
/// </summary>
public record ExecutionGraph : IExecutionGraph
{
    /// <summary>
    ///     All tasks in the schedule, including both fixed and adaptive durations.
    /// </summary>
    public required IReadOnlyList<IPlannedSkillExecution> SkillExecutions { get; set; }

    /// <summary>
    ///     All dependencies between tasks in the schedule.
    /// </summary>
    public required IReadOnlyList<Dependency> Dependencies { get; set; }

    /// <summary>
    ///     Returns a string summary of the task graph including counts and details.
    /// </summary>
    /// <returns>A string describing the graph.</returns>
    public override string ToString()
    {
        return $"TaskGraph with {SkillExecutions.Count} tasks and {Dependencies.Count} dependencies:\n" +
               string.Join("\n", SkillExecutions.Select(t => t.ToString())) + "\n" +
               string.Join("\n", Dependencies.Select(d => d.ToString()));
    }
}