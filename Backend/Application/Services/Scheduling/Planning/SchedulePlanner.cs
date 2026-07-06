using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Scheduling;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
// SchedulingAnalyticsLogger provides LogSchedulePlan* methods
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;
using ISkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.ISkillExecution;

namespace FHOOE.Freydis.Application.Services.Scheduling.Planning;

/// <summary>
///     Default implementation of <see cref="ISchedulePlanner" />.
/// </summary>
public class SchedulePlanner : ISchedulePlanner
{
    private readonly ILogger<SchedulePlanner> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="SchedulePlanner" />.
    /// </summary>
    public SchedulePlanner(ILogger<SchedulePlanner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool Plan(IExecutionGraph graph, double currentTime = 0)
    {
        try
        {
            _logger.LogSchedulePlanStarted(graph.SkillExecutions.Count, currentTime);

            // Log input state of all skills before planning
            foreach (var skill in graph.SkillExecutions)
            {
                var se = skill as ISkillExecution;
                var adaptiveSe = skill as IAdaptivePlannedSkillExecution;

                _logger.LogSkillTiming(
                    "PLAN_INPUT",
                    skill.Id,
                    skill is IPlannedSkillExecution appSkill ? appSkill.Name : "Unknown",
                    skill is IPlannedSkillExecution appSkill2
                        ? appSkill2.DomainAgent.Id
                        : Guid.Empty,
                    skill is ISkillExecution appSkillExec
                        ? appSkillExec.ExecutionId
                        : null,
                    se?.IsFinished == true ? "FINISHED" : se?.IsRunning == true ? "RUNNING" : "NOT_STARTED",
                    adaptiveSe != null,
                    skill.PlannedStartTime,
                    skill.PlannedFinishTime,
                    skill.PlannedDuration,
                    se?.ActualStartTime,
                    se?.ActualFinishTime,
                    se?.EstimatedDuration,
                    currentTime,
                    adaptiveSe?.MinDuration,
                    "Before PlanSchedule");
            }

            graph.PlanSchedule(currentTime, false, _logger);

            // Log output state of all skills after planning
            foreach (var skill in graph.SkillExecutions)
            {
                var se = skill as ISkillExecution;
                var adaptiveSe = skill as IAdaptivePlannedSkillExecution;

                _logger.LogSkillTiming(
                    "PLAN_OUTPUT",
                    skill.Id,
                    skill is IPlannedSkillExecution appSkill ? appSkill.Name : "Unknown",
                    skill is IPlannedSkillExecution appSkill2
                        ? appSkill2.DomainAgent.Id
                        : Guid.Empty,
                    skill is ISkillExecution appSkillExec
                        ? appSkillExec.ExecutionId
                        : null,
                    se?.IsFinished == true ? "FINISHED" : se?.IsRunning == true ? "RUNNING" : "NOT_STARTED",
                    adaptiveSe != null,
                    skill.PlannedStartTime,
                    skill.PlannedFinishTime,
                    skill.PlannedDuration,
                    se?.ActualStartTime,
                    se?.ActualFinishTime,
                    se?.EstimatedDuration,
                    currentTime,
                    adaptiveSe?.MinDuration,
                    "After PlanSchedule");
            }

            _logger.LogSchedulePlanCompleted(currentTime);
            return true;
        }
        catch (ScheduleInfeasibleException ex)
        {
            _logger.LogSchedulePlanInfeasible(ex, currentTime, ex.Message);
            return false;
        }
        catch (ScheduleModelException ex)
        {
            _logger.LogSchedulePlanModelError(ex, currentTime, ex.Message);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogSchedulePlanFailed(ex, currentTime, ex.Message);
            return false;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogSchedulePlanInvalidInput(ex, currentTime, ex.Message);
            return false;
        }
    }
}