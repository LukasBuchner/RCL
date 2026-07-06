using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

/// <summary>
///     Represents a planned skill execution for orchestration (non-adaptive).
/// </summary>
public record PlannedSkillExecution : IPlannedSkillExecution
{
    /// <summary>Name for easier debugging.</summary>
    public required string Name { get; init; }

    /// <summary>Reference to the original domain skill.</summary>
    public required Skill DomainSkill { get; init; }

    public required Agent DomainAgent { get; init; }

    /// <summary>Reference to the Agent selected for the execution.</summary>
    public required IRuntimeAgent RuntimeAgent { get; init; }

    /// <summary>Unique identifier for the node/skill in the procedure.</summary>
    public Guid Id { get; init; }

    /// <summary>Planned start time in seconds from procedure start.</summary>
    public double PlannedStartTime { get; set; } = double.NaN;

    /// <summary>Planned finish time in seconds from procedure start.</summary>
    public double PlannedFinishTime { get; set; } = double.NaN;

    /// <summary>Planned duration of execution.</summary>
    public double PlannedDuration { get; set; }
}