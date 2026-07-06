namespace FHOOE.Freydis.Application.Services.Execution.Dependencies;

/// <summary>
///     Contains all event prerequisites for a single skill.
///     Represents when a skill can start and when it can finish based on dependency events.
/// </summary>
public record SkillEventPrerequisites
{
    /// <summary>The ID of the skill these prerequisites apply to.</summary>
    public required Guid SkillId { get; init; }

    /// <summary>
    ///     Events that must occur before this skill can start.
    ///     Empty list means the skill can start immediately.
    /// </summary>
    public required IReadOnlyList<EventPrerequisite> StartPrerequisites { get; init; }

    /// <summary>
    ///     Events that must occur before this skill can finish (for adaptive skills).
    ///     Empty list means the skill finishes based only on its own execution completion.
    /// </summary>
    public required IReadOnlyList<EventPrerequisite> FinishPrerequisites { get; init; }

    /// <summary>
    ///     Whether this skill has any prerequisites for starting.
    /// </summary>
    public bool HasStartPrerequisites => StartPrerequisites.Count > 0;

    /// <summary>
    ///     Whether this skill has any prerequisites for finishing (adaptive behavior).
    /// </summary>
    public bool HasFinishPrerequisites => FinishPrerequisites.Count > 0;

    /// <summary>
    ///     Whether this skill is adaptive (has finish prerequisites).
    /// </summary>
    public bool IsAdaptive => HasFinishPrerequisites;
}