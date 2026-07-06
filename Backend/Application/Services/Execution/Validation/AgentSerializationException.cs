namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Thrown when the pre-execution validation detects that one or more physical agents have
///     skills assigned to them that lack the Finish-to-Start dependency ordering required to
///     prevent concurrent dispatch to the same robot.
/// </summary>
/// <remarks>
///     Each entry in <see cref="Violations" /> corresponds to one agent and lists every skill
///     pair that is missing an FS chain, giving the operator precise information about which
///     edges must be added to the procedure graph before execution can proceed.
///     The <see cref="StructuredData" /> override projects the violations into anonymous objects
///     suitable for JSON serialization in a GraphQL error payload.
/// </remarks>
public sealed class AgentSerializationException : ExecutionPreConditionException
{
    /// <summary>
    ///     Initializes a new instance of <see cref="AgentSerializationException" /> with the
    ///     full list of serialization violations discovered during pre-execution validation.
    /// </summary>
    /// <param name="violations">
    ///     One <see cref="AgentSerializationViolation" /> per offending agent, each carrying
    ///     the agent identity, the involved skill nodes, and every missing FS pair.
    ///     Must not be <see langword="null" />.
    /// </param>
    public AgentSerializationException(IReadOnlyList<AgentSerializationViolation> violations)
        : base(BuildMessage(violations))
    {
        Violations = violations;
    }

    /// <summary>
    ///     All agent serialization violations discovered during validation, one record per
    ///     offending agent.
    /// </summary>
    public IReadOnlyList<AgentSerializationViolation> Violations { get; }

    /// <inheritdoc />
    public override string ErrorCode => "AGENT_SERIALIZATION_VIOLATION";

    /// <inheritdoc />
    /// <remarks>
    ///     Returns <see cref="Violations" /> projected to anonymous objects with the shape
    ///     <c>{ AgentId, AgentName, UnserializedSkills: [{ NodeId, SkillName }], MissingFsPairs: [{ SkillA, SkillB }] }</c>,
    ///     which is safe to serialize directly into a GraphQL error extensions payload.
    /// </remarks>
    public override object? StructuredData =>
        Violations.Select(v => new
        {
            v.AgentId,
            v.AgentName,
            UnserializedSkills = v.UnserializedSkills.Select(s => new { s.NodeId, s.SkillName }),
            MissingFsPairs = v.MissingFsPairs.Select(p => new { p.SkillA, p.SkillB })
        });

    private static string BuildMessage(IReadOnlyList<AgentSerializationViolation> violations)
    {
        var agentNames = string.Join(", ", violations.Select(v => $"\'{v.AgentName}\'"));
        return $"Agent serialization validation failed for {violations.Count} agent(s): {agentNames}. " +
               "All skills assigned to the same physical agent must be connected by a Finish-to-Start " +
               "dependency chain to prevent concurrent dispatch to the same robot.";
    }
}