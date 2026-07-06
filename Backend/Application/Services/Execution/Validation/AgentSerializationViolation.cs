namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Describes a single skill node that participates in a serialization violation for a given
///     agent \u2014 i.e. a skill that is assigned to an agent but lacks the required Finish-to-Start
///     ordering with at least one peer skill on the same agent.
/// </summary>
/// <param name="NodeId">
///     The unique identifier of the procedure graph node that contains this skill.
/// </param>
/// <param name="SkillName">
///     The human-readable name of the skill, used for diagnostic messages shown to the operator.
/// </param>
public record UnserializedSkill(Guid NodeId, string SkillName);

/// <summary>
///     A pair of skill node identifiers for which a required Finish-to-Start dependency is absent.
///     Both skills are assigned to the same physical agent; without an FS chain between them they
///     could be dispatched to the robot concurrently.
/// </summary>
/// <param name="SkillA">
///     The unique identifier of the first skill node in the unordered pair.
/// </param>
/// <param name="SkillB">
///     The unique identifier of the second skill node in the unordered pair.
///     The pair is unordered \u2014 (SkillA, SkillB) and (SkillB, SkillA) denote the same missing edge.
/// </param>
public record SkillPair(Guid SkillA, Guid SkillB);

/// <summary>
///     Aggregates all serialization constraint violations for a single physical agent within
///     a procedure graph. An agent is considered unserializable when two or more skills assigned
///     to it can execute concurrently because no Finish-to-Start reachability path exists between
///     every pair of those skills.
/// </summary>
public record AgentSerializationViolation
{
    /// <summary>
    ///     Unique identifier of the physical agent (robot) that has conflicting skill assignments.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    ///     Human-readable name of the agent, included so diagnostic output and client error
    ///     messages are self-contained without requiring an extra agent lookup.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    ///     The set of skill nodes assigned to this agent that are involved in at least one
    ///     missing FS dependency. Each entry identifies the node and provides its skill name
    ///     for operator-facing messages.
    /// </summary>
    public required IReadOnlyList<UnserializedSkill> UnserializedSkills { get; init; }

    /// <summary>
    ///     Every pair of skills on this agent for which no Finish-to-Start reachability path
    ///     exists in the dependency graph. Each <see cref="SkillPair" /> represents one missing
    ///     ordering constraint that would allow concurrent dispatch to the same robot.
    /// </summary>
    public required IReadOnlyList<SkillPair> MissingFsPairs { get; init; }
}