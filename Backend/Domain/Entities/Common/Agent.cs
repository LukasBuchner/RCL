namespace FHOOE.Freydis.Domain.Entities.Common;

public record Agent
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public List<Guid> SkillIds { get; set; } = [];

    public required string RepresentativeColor { get; set; }

    /// <summary>
    ///     Gets or sets the current operational state of the agent.
    /// </summary>
    public AgentState State { get; set; } = AgentState.Registered;

    /// <summary>
    ///     Gets or sets the timestamp when the agent was last seen active.
    /// </summary>
    public DateTime? LastSeenUtc { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata about the agent.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}