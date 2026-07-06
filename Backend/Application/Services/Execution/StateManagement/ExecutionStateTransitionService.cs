using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Service responsible for managing state transitions of skill executions.
///     Centralizes all state transition logic to ensure consistency and proper sequencing.
/// </summary>
public class ExecutionStateTransitionService : IExecutionStateTransitionService
{
    private readonly ILogger<ExecutionStateTransitionService> _logger;
    private readonly ISkillExecutionStateManager _stateManager;
    private DateTimeOffset _executionStartTime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionStateTransitionService" /> class.
    /// </summary>
    /// <param name="stateManager">The state manager for skill executions.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public ExecutionStateTransitionService(
        ISkillExecutionStateManager stateManager,
        ILogger<ExecutionStateTransitionService> logger,
        TimeProvider timeProvider)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var timeProvider1 = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _executionStartTime = timeProvider1.GetUtcNow();
    }

    /// <inheritdoc />
    public void TransitionToRunning(Guid skillId, IRuntimeAgent agent, DateTimeOffset startTime)
    {
        ArgumentNullException.ThrowIfNull(agent, nameof(agent));

        var state = _stateManager.GetState(skillId);
        var fromState = state?.ExecutionStatus.ToString() ?? "UNKNOWN";
        var skillName = ResolveSkillName(state);
        var timestamp = (startTime - _executionStartTime).TotalSeconds;

        _stateManager.UpdateState(skillId, state =>
        {
            state.ExecutionStatus = ExecutionStatus.Running;
            state.StartedAt = startTime;
            state.AssignedAgent = agent;
        });

        _logger.LogStateTransition(
            skillId,
            skillName,
            null,
            fromState,
            "RUNNING",
            agent.Id,
            timestamp,
            "Agent started execution");
    }

    /// <inheritdoc />
    public void TransitionToCompleted(Guid skillId, DateTimeOffset completionTime)
    {
        var state = _stateManager.GetState(skillId);
        var fromState = state?.ExecutionStatus.ToString() ?? "UNKNOWN";
        var skillName = ResolveSkillName(state);
        var timestamp = (completionTime - _executionStartTime).TotalSeconds;

        _stateManager.UpdateState(skillId, state =>
        {
            state.ExecutionStatus = ExecutionStatus.Completed;
            state.CompletedAt = completionTime;
            state.Subscription?.Dispose();
        });

        var updatedState = _stateManager.GetState(skillId);
        _logger.LogStateTransition(
            skillId,
            skillName,
            null,
            fromState,
            "COMPLETED",
            updatedState?.AssignedAgent?.Id,
            timestamp,
            "Execution completed successfully");
    }

    /// <inheritdoc />
    public void TransitionToFailed(Guid skillId, string errorMessage, DateTimeOffset failureTime)
    {
        ArgumentNullException.ThrowIfNull(errorMessage, nameof(errorMessage));
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty or whitespace.", nameof(errorMessage));

        var state = _stateManager.GetState(skillId);
        var fromState = state?.ExecutionStatus.ToString() ?? "UNKNOWN";
        var skillName = ResolveSkillName(state);
        var timestamp = (failureTime - _executionStartTime).TotalSeconds;

        _stateManager.UpdateState(skillId, state =>
        {
            state.ExecutionStatus = ExecutionStatus.Failed;
            state.ErrorMessage = errorMessage;
            state.CompletedAt = failureTime;
            state.Subscription?.Dispose();
        });

        var updatedState = _stateManager.GetState(skillId);
        _logger.LogStateTransition(
            skillId,
            skillName,
            null,
            fromState,
            "FAILED",
            updatedState?.AssignedAgent?.Id,
            timestamp,
            errorMessage);
    }

    /// <inheritdoc />
    public void TransitionToNotSelected(Guid skillId, DateTimeOffset timestamp)
    {
        var state = _stateManager.GetState(skillId);
        var fromState = state?.ExecutionStatus.ToString() ?? "UNKNOWN";
        var skillName = ResolveSkillName(state);
        var relativeTimestamp = (timestamp - _executionStartTime).TotalSeconds;

        _stateManager.UpdateState(skillId, state =>
        {
            state.ExecutionStatus = ExecutionStatus.NotSelected;
            state.CompletedAt = timestamp;
            state.Subscription?.Dispose();
        });

        _logger.LogStateTransition(
            skillId,
            skillName,
            null,
            fromState,
            "NOT_SELECTED",
            null,
            relativeTimestamp,
            "Skill in non-selected router branch");
    }

    /// <inheritdoc />
    public void UpdateProgress(Guid skillId, double progressPercentage, SkillExecutionProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress, nameof(progress));

        progressPercentage = Math.Clamp(progressPercentage, 0.0, 100.0);

        _stateManager.UpdateState(skillId, state =>
        {
            state.LastProgressPercentage = progressPercentage;
            state.LastProgress = progress;
        });
    }

    /// <summary>
    ///     Sets the execution start time for timestamp calculations.
    ///     Should be called when execution starts.
    /// </summary>
    public void SetExecutionStartTime(DateTimeOffset startTime)
    {
        _executionStartTime = startTime;
    }

    /// <summary>
    ///     Resolves a human-readable skill name from the execution state.
    /// </summary>
    private static string ResolveSkillName(SkillExecutionState? state)
    {
        return (state?.SkillNode as SkillExecutionNode)?.SkillExecutionTask.Skill.Name ?? "Unknown";
    }
}