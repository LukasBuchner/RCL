namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Represents the different types of dependencies between tasks.
/// </summary>
public enum DependencyType
{
    /// <summary>
    ///     Finish-to-Start: Task B cannot start until Task A finishes. (Sj >= Fi)
    /// </summary>
    FinishToStart, // FS

    /// <summary>
    ///     Start-to-Start: Task B cannot start until Task A starts. (Sj >= Si)
    /// </summary>
    StartToStart, // SS

    /// <summary>
    ///     Start-to-Finish: Task B cannot finish until Task A starts. (Fj >= Si)
    /// </summary>
    StartToFinish, // SF

    /// <summary>
    ///     Finish-to-Finish: Task B cannot finish until Task A finishes. (Fj >= Fi)
    /// </summary>
    FinishToFinish // FF
}

/// <summary>
///     Represents a dependency between two skill executions with a specific dependency type.
/// </summary>
public record Dependency
{
    /// <summary>
    ///     Unique identifier for the dependency.
    /// </summary>
    public required Guid Id { get; init; } = Guid.Empty;

    /// <summary>
    ///     The source task or skill execution in the dependency relationship.
    /// </summary>
    public required IPlannedSkillExecution Source { get; init; }

    /// <summary>
    ///     The target task or skill execution that depends on the source.
    /// </summary>
    public required IPlannedSkillExecution Target { get; init; }

    /// <summary>
    ///     The type of dependency between the source and target.
    /// </summary>
    public DependencyType Type { get; init; }

    /// <summary>
    ///     Returns a string representation of the dependency in the format:
    ///     "Dep: SourceId --(Type)--> TargetId"
    /// </summary>
    /// <returns>A string representing the dependency.</returns>
    public override string ToString()
    {
        return $"Dep: {Source.Id} --({Type})--> {Target.Id}";
    }
}