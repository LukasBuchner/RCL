using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

public interface IPlannedSkillExecution : Freydis.Scheduling.Core.IPlannedSkillExecution
{
    /// <summary>Name for easier debugging.</summary>
    string Name { get; init; }

    /// <summary>Reference to the original domain skill.</summary>
    Skill DomainSkill { get; init; }

    /// Reference to the original domain agent.
    Agent DomainAgent { get; init; }

    /// <summary>Reference to the Agent selected for the execution.</summary>
    IRuntimeAgent RuntimeAgent { get; init; }
}