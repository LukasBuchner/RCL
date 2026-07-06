using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

/// <summary>
///     Represents a planned adaptive skill execution for orchestration.
///     Implements both application and scheduling domain interfaces for OR-Tools compatibility.
/// </summary>
public record PlannedAdaptiveSkillExecution : PlannedSkillExecution, IPlannedAdaptiveSkillExecution,
    IAdaptivePlannedSkillExecution
{
    // Explicit interface implementation for OR-Tools scheduling compatibility
    /// <summary>Minimum duration getter/setter for OR-Tools scheduling interface.</summary>
    double IAdaptivePlannedSkillExecution.MinDuration
    {
        get => MinDuration;
        set
        {
            /* MinDuration is init-only, but OR-Tools shouldn't need to set it */
        }
    }

    /// <summary>Minimum allowed duration for adaptive execution. The duration is unbounded above.</summary>
    public double MinDuration { get; init; }

    /// <summary>Indicates if the agent can meet duration constraints.</summary>
    public bool CanMeetDurationConstraints { get; set; } = true;
}