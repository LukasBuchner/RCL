namespace FHOOE.Freydis.Application.Services.Execution.Dependencies;

/// <summary>
///     Represents the event-based dependency graph for a procedure execution.
///     Maps each skill to its event prerequisites for starting and finishing.
/// </summary>
public record DependencyGraph
{
    /// <summary>
    ///     Map of SkillId to its event prerequisites.
    /// </summary>
    public required IReadOnlyDictionary<Guid, SkillEventPrerequisites> Prerequisites { get; init; }

    /// <summary>
    ///     Gets the prerequisites for a specific skill, or null if not found.
    /// </summary>
    /// <param name="skillId">The ID of the skill.</param>
    /// <returns>The prerequisites for the skill, or null if not in the graph.</returns>
    public SkillEventPrerequisites? GetPrerequisites(Guid skillId)
    {
        return Prerequisites.GetValueOrDefault(skillId);
    }

    /// <summary>
    ///     Whether the graph contains prerequisites for the specified skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill.</param>
    /// <returns>True if the skill is in the graph, false otherwise.</returns>
    public bool ContainsSkill(Guid skillId)
    {
        return Prerequisites.ContainsKey(skillId);
    }

    /// <summary>
    ///     Gets all skills that can start immediately (no start prerequisites).
    /// </summary>
    public IEnumerable<Guid> GetImmediateStartSkills()
    {
        return Prerequisites.Values
            .Where(p => !p.HasStartPrerequisites)
            .Select(p => p.SkillId);
    }

    /// <summary>
    ///     Gets all adaptive skills (skills with finish prerequisites).
    /// </summary>
    public IEnumerable<Guid> GetAdaptiveSkills()
    {
        return Prerequisites.Values
            .Where(p => p.IsAdaptive)
            .Select(p => p.SkillId);
    }
}