using System.Reactive;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Agents;

/// <summary>
///     Represents the progress of a skill execution as reported by an agent.
/// </summary>
public record SkillExecutionProgress
{
    /// <summary>
    ///     Unique ID for this specific execution instance, matching the one provided by the orchestrator.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    ///     ID of the skill being executed.
    /// </summary>
    public required Guid SkillId { get; init; }

    /// <summary>
    ///     ID of the agent performing the execution.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    ///     The absolute UTC time when the agent started this skill execution.
    /// </summary>
    public required DateTime ActualStartTimeUtc { get; init; }

    /// <summary>
    ///     Time elapsed in seconds since the agent started this execution instance.
    /// </summary>
    public required double CurrentTimeIntoExecution { get; init; }

    /// <summary>
    ///     The agent's current best estimate of the total duration (in seconds) this skill will take,
    ///     from its start to its finish.
    /// </summary>
    public required double EstimatedTotalDuration { get; init; } // Agent's current best guess

    /// <summary>
    ///     A human-readable status message from the agent.
    ///     E.g. "Processing item 5/10", "Waiting for resource".
    /// </summary>
    public required string StatusMessage { get; init; } // e.g. "Processing item 5/10", "Waiting for resource"

    /// <summary>
    ///     Indicates if the agent considers the skill execution completed successfully.
    ///     If true, Error should be null.
    /// </summary>
    public bool CompletedSuccessfully { get; init; }

    /// <summary>
    ///     If an error occurred during execution, this property will contain the exception.
    ///     If non-null, IsCompletedSuccessfully should be false.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    ///     For adaptive agents: The minimum total duration (in seconds) the agent currently
    ///     believes it can achieve for this skill, given its current progress and capabilities.
    ///     Null if not applicable or not reported. The duration is unbounded above.
    /// </summary>
    public double? MinAchievableDuration { get; init; }

    /// <summary>
    ///     Output values produced by the skill execution.
    ///     Populated when the skill completes successfully.
    ///     Used for variable-driven branching and data flow between skills.
    /// </summary>
    public Dictionary<string, object>? Outputs { get; init; }
}

/// <summary>
///     Represents an agent's estimation for executing a specific skill.
/// </summary>
public record SkillExecutionEstimate
{
    /// <summary>
    ///     The skill for which the estimate is provided.
    /// </summary>
    public required Skill Skill { get; init; }

    /// <summary>
    ///     The agent providing the estimate.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    ///     Indicates if the agent can execute this skill adaptively.
    /// </summary>
    public required bool CanExecuteAdaptively { get; init; }

    /// <summary>
    ///     The agent's best estimate of the execution duration if executed non-adaptively,
    ///     or the nominal/expected duration if executed adaptively.
    /// </summary>
    public required double EstimatedNominalDuration { get; init; }

    /// <summary>
    ///     For adaptive execution: The absolute minimum duration the agent can achieve for this skill. Null otherwise.
    ///     This is the "capability" bound; the duration is unbounded above.
    /// </summary>
    public double? MinAdaptiveDuration { get; init; }
}

/// <summary>
///     Represents the health status of an agent.
/// </summary>
public record AgentHealthStatus
{
    /// <summary>
    ///     The unique identifier of the agent.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    ///     The name of the agent.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    ///     Indicates if the agent is healthy and operational.
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    ///     Indicates if the agent is currently available for new skill executions.
    /// </summary>
    public required bool IsAvailable { get; init; }

    /// <summary>
    ///     The number of skills currently being executed by this agent.
    /// </summary>
    public required int ActiveExecutions { get; init; }

    /// <summary>
    ///     The total number of skills executed by this agent since startup.
    /// </summary>
    public required int TotalExecutionsCompleted { get; init; }

    /// <summary>
    ///     The number of skills that failed during execution.
    /// </summary>
    public required int FailedExecutions { get; init; }

    /// <summary>
    ///     The last time the agent was seen or communicated.
    /// </summary>
    public required DateTime LastSeenUtc { get; init; }

    /// <summary>
    ///     The time when the agent was started or initialised.
    /// </summary>
    public required DateTime StartedUtc { get; init; }

    /// <summary>
    ///     Current CPU usage percentage (0-100).
    /// </summary>
    public double? CpuUsagePercent { get; init; }

    /// <summary>
    ///     Current memory usage in MB.
    /// </summary>
    public double? MemoryUsageMb { get; init; }

    /// <summary>
    ///     Average execution time for completed skills in seconds.
    /// </summary>
    public double? AverageExecutionTimeSeconds { get; init; }

    /// <summary>
    ///     Current status message from the agent.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    ///     Any error or warning messages.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Additional metadata about the agent's state.
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; init; }
}

/// <summary>
///     Defines the contract for an entity capable of executing skills.
/// </summary>
public interface IRuntimeAgent
{
    /// <summary>
    ///     Gets the unique identifier of this agent.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Gets the name of this agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Asynchronously retrieves the current health status of this agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current health status of the agent.</returns>
    Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously retrieves a list of skills this agent is capable of executing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="Skill" /> entities.</returns>
    Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously provides an estimation for executing a given skill.
    ///     This includes nominal duration and adaptive capabilities if applicable.
    ///     This is called *before* execution to aid in planning.
    /// </summary>
    /// <param name="skill">The skill to estimate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SkillExecutionEstimate" /> for the skill, or null if the agent cannot execute it.</returns>
    Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously determines if the agent can execute the given skill adaptively.
    /// </summary>
    /// <param name="skill">The skill to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if adaptive execution is supported for this skill, false otherwise.</returns>
    Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously executes a skill in a non-adaptive manner.
    /// </summary>
    /// <param name="executionId">A unique identifier for this execution instance.</param>
    /// <param name="skillToExecute">The skill to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the execution.</param>
    /// <returns>An observable stream of <see cref="SkillExecutionProgress" /> updates.</returns>
    IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid executionId,
        Skill skillToExecute,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Asynchronously executes a skill adaptively with event-driven completion.
    /// </summary>
    /// <param name="executionId">A unique identifier for this execution instance, provided by the orchestrator.</param>
    /// <param name="skillToExecute">The skill to execute.</param>
    /// <param name="initialTargetDuration">The initial target duration (in seconds) - agent's nominal pace.</param>
    /// <param name="plannedFinishTimes">
    ///     An observable stream of planned finish times (in seconds) from the scheduler.
    ///     The agent uses these to pace its work but does NOT auto-complete when reaching them.
    ///     These times update dynamically as re-scheduling occurs.
    /// </param>
    /// <param name="finishSignal">
    ///     An observable that signals when the agent should complete execution.
    ///     When this signal fires, the agent should finish successfully (not fail).
    ///     This is event-driven completion based on dependency constraints.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for error conditions and forced shutdown only.</param>
    /// <returns>
    ///     An observable stream of <see cref="SkillExecutionProgress" /> updates.
    ///     The observable completes when the finish signal fires or the execution is cancelled.
    ///     Progress reports include <see cref="SkillExecutionProgress.MinAchievableDuration" />.
    /// </returns>
    IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId,
        Skill skillToExecute,
        double initialTargetDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        CancellationToken cancellationToken);
}