namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Classifies an SCC (Strongly Connected Component) according to its size and the kinds of tasks it contains.
///     Cycle feasibility is enforced upstream by event-level acyclicity validation in
///     <c>ExecutionGraphExtensions.ValidateModel</c>, so this classification is purely descriptive.
/// </summary>
public enum StronglyConnectedComponentKind
{
    /// <summary>
    ///     The SCC has exactly one task, so there is no cycle to resolve.
    /// </summary>
    Trivial,

    /// <summary>
    ///     The SCC contains two or more tasks and at least one implements
    ///     <see cref="IAdaptivePlannedSkillExecution" />. The planner solves the cycle
    ///     by picking duration values within each adaptive task's bounds that satisfy
    ///     all dependency constraints.
    /// </summary>
    AdaptiveCycle,

    /// <summary>
    ///     The SCC contains two or more fixed-duration tasks connected by SS/FF/SF
    ///     coupling. No FS edge participates in any cycle (event-level acyclicity
    ///     guarantees that), so the group is solvable by constraint propagation.
    /// </summary>
    FixedCoupledGroup
}

/// <summary>
///     Represents a strongly connected component in the task dependency graph,
///     along with its classification.
/// </summary>
/// <param name="Tasks">The list of tasks that form the strongly connected component.</param>
/// <param name="Kind">The classification of the component based on its structure and task types.</param>
public readonly record struct StronglyConnectedComponentInfo(
    IReadOnlyList<IPlannedSkillExecution> Tasks,
    StronglyConnectedComponentKind Kind);

/// <summary>
///     Provides extension methods for analyzing strongly connected components of tasks.
/// </summary>
public static class StronglyConnectedComponentExtensions
{
    /// <summary>
    ///     Classifies a strongly connected component (SCC) based on its size and whether
    ///     it contains any adaptive skill execution. The classifier is purely descriptive;
    ///     event-level acyclicity is enforced upstream by
    ///     <c>ExecutionGraphExtensions.ValidateModel</c>, so cycles reaching this point
    ///     are guaranteed solvable.
    /// </summary>
    /// <param name="connectedComponent">The SCC to classify.</param>
    /// <returns>
    ///     A <see cref="StronglyConnectedComponentInfo" /> instance that contains the original tasks
    ///     and the computed <see cref="StronglyConnectedComponentKind" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectedComponent" /> is null.</exception>
    public static StronglyConnectedComponentInfo ClassifyStronglyConnectedComponent(
        this IStronglyConnectedComponent connectedComponent)
    {
        ArgumentNullException.ThrowIfNull(connectedComponent);

        var tasks = connectedComponent.SkillExecutions;

        if (tasks.Count == 1)
            return new StronglyConnectedComponentInfo(tasks, StronglyConnectedComponentKind.Trivial);

        var containsAdaptive = tasks.Any(t => t is IAdaptivePlannedSkillExecution);
        var kind = containsAdaptive
            ? StronglyConnectedComponentKind.AdaptiveCycle
            : StronglyConnectedComponentKind.FixedCoupledGroup;

        return new StronglyConnectedComponentInfo(tasks, kind);
    }
}