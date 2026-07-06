using FHOOE.Freydis.Agents.Agents;

namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Represents the type of execution event that can trigger dependencies.
/// </summary>
public enum ExecutionEventType
{
    /// <summary>Skill started executing (transition to Running state).</summary>
    Start,

    /// <summary>Skill finished executing (transition to Completed state).</summary>
    Finish,

    /// <summary>Skill progress update (intermediate state during execution).</summary>
    Progress,

    /// <summary>Skill execution failed with an error (transition to Failed state).</summary>
    Failed,

    /// <summary>Skill was not selected for execution because it resides in a non-selected router branch.</summary>
    NotSelected
}

/// <summary>
///     Represents an execution event for a specific skill.
/// </summary>
public record ExecutionEvent
{
    /// <summary>The ID of the skill that triggered this event.</summary>
    public required Guid SkillId { get; init; }

    /// <summary>The type of event (Start, Finish, or Progress).</summary>
    public required ExecutionEventType EventType { get; init; }

    /// <summary>When the event occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Progress percentage (0.0 to 1.0) for Progress events.</summary>
    public double? ProgressPercentage { get; init; }

    /// <summary>Detailed progress information from the agent (for Progress events).</summary>
    public SkillExecutionProgress? ProgressData { get; init; }

    /// <summary>Error message describing why the skill failed (for Failed events).</summary>
    public string? ErrorMessage { get; init; }
}