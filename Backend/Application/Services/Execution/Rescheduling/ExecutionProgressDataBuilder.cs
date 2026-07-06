using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Rescheduling;

/// <summary>
///     Builds SkillExecutionProgress data from execution state for re-scheduling.
/// </summary>
/// <remarks>
///     This service transforms execution state into progress data for the timing calculation orchestrator.
///     It handles both progress data reported by agents and creates synthetic progress for skills
///     that haven't reported yet. The service also ensures that CompletedSuccessfully flag is
///     correctly synchronized with the orchestrator's execution status.
/// </remarks>
public class ExecutionProgressDataBuilder : IExecutionProgressDataBuilder
{
    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, SkillExecutionProgress> BuildProgressData(
        IEnumerable<SkillExecutionState> states,
        DateTimeOffset currentTime)
    {
        var progressData = new Dictionary<Guid, SkillExecutionProgress>();

        foreach (var state in states)
        {
            // Skip nodes without execution IDs (not started yet)
            if (state.SkillNode is not SkillExecutionNode skillNode ||
                skillNode.SkillExecutionTask.ExecutionId == null)
                continue;

            var executionId = skillNode.SkillExecutionTask.ExecutionId.Value;

            // If we have LastProgress from agent, use it (most accurate)
            if (state.LastProgress != null)
            {
                // CRITICAL: Update CompletedSuccessfully based on current ExecutionStatus
                // The LastProgress might have been set before the Finish event arrived
                var updatedProgress = state.LastProgress with
                {
                    CompletedSuccessfully = state.ExecutionStatus == ExecutionStatus.Completed
                };
                progressData[executionId] = updatedProgress;
            }
            // Otherwise, if the skill has started, create progress data from state
            else if (state.StartedAt.HasValue)
            {
                // Calculate elapsed time
                var elapsed = state.CompletedAt.HasValue
                    ? (state.CompletedAt.Value - state.StartedAt.Value).TotalSeconds
                    : (currentTime - state.StartedAt.Value).TotalSeconds;

                // Get estimated duration from the node
                var estimatedDuration = skillNode.SkillExecutionTask.Duration;

                var syntheticProgress = new SkillExecutionProgress
                {
                    ExecutionId = executionId,
                    SkillId = skillNode.SkillExecutionTask.Skill.Id,
                    AgentId = skillNode.SkillExecutionTask.AgentId,
                    ActualStartTimeUtc = state.StartedAt.Value.UtcDateTime,
                    CurrentTimeIntoExecution = elapsed,
                    EstimatedTotalDuration = estimatedDuration,
                    StatusMessage = state.ExecutionStatus.ToString(),
                    CompletedSuccessfully = state.ExecutionStatus == ExecutionStatus.Completed,
                    Error = state.ErrorMessage != null ? new InvalidOperationException(state.ErrorMessage) : null
                };

                progressData[executionId] = syntheticProgress;
            }
        }

        return progressData;
    }
}