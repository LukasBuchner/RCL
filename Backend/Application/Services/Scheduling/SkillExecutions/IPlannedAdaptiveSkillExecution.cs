using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

public interface IPlannedAdaptiveSkillExecution
{
    /// <summary>Minimum allowed duration for adaptive execution. The duration is unbounded above.</summary>
    double MinDuration { get; init; }

    /// <summary>Planned duration may be adjusted by solver.</summary>
    double PlannedDuration { get; set; }

    /// <summary>Indicates if the agent can meet duration constraints.</summary>
    bool CanMeetDurationConstraints { get; set; }

    /// <summary>Name for easier debugging.</summary>
    string Name { get; init; }

    /// <summary>Reference to the original domain skill.</summary>
    Skill DomainSkill { get; init; }

    /// <summary>Reference to the Agent selected for the execution.</summary>
    IRuntimeAgent RuntimeAgent { get; init; }

    /// <summary>Unique identifier for the node/skill in the procedure.</summary>
    Guid Id { get; init; }

    /// <summary>Planned start time in seconds from procedure start.</summary>
    double PlannedStartTime { get; set; }

    /// <summary>Planned finish time in seconds from procedure start.</summary>
    double PlannedFinishTime { get; set; }
}