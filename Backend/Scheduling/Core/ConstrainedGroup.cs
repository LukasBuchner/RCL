namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Represents a group of tasks connected by SS (Start-to-Start) and/or FF (Finish-to-Finish)
///     constraints that require coordinated duration optimization using LP solver.
///     <para>
///     Unlike true SCCs (strongly connected components), ConstrainedGroups do not form cycles
///     in the directed graph, but the SS/FF constraints create tight coupling between task
///     timings and durations that benefits from LP optimization.
///     </para>
/// </summary>
/// <remarks>
///     A ConstrainedGroup is formed when:
///     <list type="bullet">
///         <item>Two or more tasks are connected by SS or FF edges (treated as undirected graph)</item>
///         <item>At least one task in the group is adaptive (IAdaptivePlannedSkillExecution)</item>
///     </list>
///     Example patterns that form ConstrainedGroups:
///     <list type="bullet">
///         <item>A --SS,FF--&gt; B (parallel edges between 2 tasks)</item>
///         <item>A --SS--&gt; B, A --SS--&gt; C, C --FF--&gt; A, C --FF--&gt; B (complex 3-task pattern)</item>
///         <item>A --SS--&gt; B --FF--&gt; C (chain via SS and FF)</item>
///     </list>
/// </remarks>
public sealed class ConstrainedGroup : IExecutionGraph
{
    /// <summary>
    ///     All tasks in this constraint group that need coordinated scheduling.
    /// </summary>
    public required IReadOnlyList<IPlannedSkillExecution> SkillExecutions { get; set; }

    /// <summary>
    ///     All dependencies between tasks within this group.
    ///     Includes not just SS/FF edges (which define the group) but also any FS/SF edges
    ///     between the same tasks, as these must be respected during LP solving.
    /// </summary>
    public required IReadOnlyList<Dependency> Dependencies { get; set; }

    /// <summary>
    ///     Returns a string summary of the constrained group including task and dependency counts.
    /// </summary>
    /// <returns>A string describing the constrained group.</returns>
    public override string ToString()
    {
        return $"ConstrainedGroup with {SkillExecutions.Count} tasks and {Dependencies.Count} dependencies:\n" +
               string.Join("\n", SkillExecutions.Select(t => t.ToString())) + "\n" +
               string.Join("\n", Dependencies.Select(d => d.ToString()));
    }
}