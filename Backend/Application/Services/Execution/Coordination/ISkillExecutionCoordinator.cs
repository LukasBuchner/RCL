using System.Reactive;
using FHOOE.Freydis.Domain.Entities.Common;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Coordination;

/// <summary>
///     Coordinates skill execution on agents and publishes execution events.
///     Bridges IRuntimeAgent execution with the event-driven execution system.
/// </summary>
public interface ISkillExecutionCoordinator
{
    /// <summary>
    ///     Executes a skill on its assigned agent and publishes Start/Finish events.
    /// </summary>
    /// <param name="skillNodeId">The ID of the SkillExecutionNode (used for event publishing and dependency tracking).</param>
    /// <param name="skill">The skill to execute.</param>
    /// <param name="agentId">The ID of the agent to execute the skill on.</param>
    /// <param name="variableContext">Optional variable context for resolving property bindings.</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution.</param>
    /// <returns>Observable stream of execution progress.</returns>
    IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid skillNodeId,
        Skill skill,
        Guid agentId,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Executes an adaptive skill with dynamic schedule updates and event-driven completion.
    /// </summary>
    /// <param name="skillNodeId">The ID of the SkillExecutionNode (used for event publishing and dependency tracking).</param>
    /// <param name="skill">The skill to execute.</param>
    /// <param name="agentId">The ID of the agent to execute the skill on.</param>
    /// <param name="initialDuration">Initial planned duration for the skill.</param>
    /// <param name="plannedFinishTimes">Observable stream of planned finish times for pacing the skill.</param>
    /// <param name="finishSignal">Observable that signals when the skill should complete successfully.</param>
    /// <param name="variableContext">Optional variable context for resolving property bindings.</param>
    /// <param name="cancellationToken">Cancellation token for error conditions only.</param>
    /// <returns>Observable stream of execution progress.</returns>
    IObservable<SkillExecutionProgress> ExecuteAdaptiveSkillAsync(
        Guid skillNodeId,
        Skill skill,
        Guid agentId,
        double initialDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken);
}

/// <summary>
///     Progress information for skill execution.
///     Mirrors the structure from IRuntimeAgent for compatibility.
/// </summary>
public record SkillExecutionProgress
{
    /// <summary>ID of the executing skill.</summary>
    public required Guid SkillId { get; init; }

    /// <summary>Progress percentage (0.0 to 1.0).</summary>
    public required double Progress { get; init; }

    /// <summary>Current status message.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Whether execution has completed.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>Whether execution failed.</summary>
    public bool IsFailed { get; init; }

    /// <summary>Error message if execution failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Skill output values (populated when execution completes).</summary>
    public Dictionary<string, object>? Outputs { get; init; }
}