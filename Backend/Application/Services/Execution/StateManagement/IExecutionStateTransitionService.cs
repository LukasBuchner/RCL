using FHOOE.Freydis.Agents.Agents;

namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Service responsible for managing state transitions of skill executions.
///     Centralizes all state transition logic to ensure consistency and proper sequencing.
/// </summary>
public interface IExecutionStateTransitionService
{
    /// <summary>
    ///     Transitions a skill execution to the Running state.
    /// </summary>
    /// <param name="skillId">The ID of the skill execution node.</param>
    /// <param name="agent">The assigned runtime agent.</param>
    /// <param name="startTime">The time when execution started.</param>
    void TransitionToRunning(Guid skillId, IRuntimeAgent agent, DateTimeOffset startTime);

    /// <summary>
    ///     Transitions a skill execution to the Completed state.
    /// </summary>
    /// <param name="skillId">The ID of the skill execution node.</param>
    /// <param name="completionTime">The time when execution completed.</param>
    void TransitionToCompleted(Guid skillId, DateTimeOffset completionTime);

    /// <summary>
    ///     Transitions a skill execution to the Failed state.
    /// </summary>
    /// <param name="skillId">The ID of the skill execution node.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="failureTime">The time when the failure occurred.</param>
    void TransitionToFailed(Guid skillId, string errorMessage, DateTimeOffset failureTime);

    /// <summary>
    ///     Transitions a skill execution to the NotSelected state.
    ///     Used when a skill resides in a non-selected router branch and will never execute.
    /// </summary>
    /// <param name="skillId">The ID of the skill execution node.</param>
    /// <param name="timestamp">The time when the not-selected determination was made.</param>
    void TransitionToNotSelected(Guid skillId, DateTimeOffset timestamp);

    /// <summary>
    ///     Updates the execution progress of a running skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill execution node.</param>
    /// <param name="progressPercentage">The progress percentage (0-100).</param>
    /// <param name="progress">The detailed progress information from the agent.</param>
    void UpdateProgress(Guid skillId, double progressPercentage, SkillExecutionProgress progress);
}