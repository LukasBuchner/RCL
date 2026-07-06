namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

public record AdaptiveSkillExecution : SkillExecution, IAdaptiveSkillExecution
{
    public double MinDuration { get; set; }
}