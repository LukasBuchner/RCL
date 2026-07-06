using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Execution.Dependencies;

/// <summary>
///     Represents which event type (Start or Finish) is required from a dependency.
/// </summary>
public enum EventTriggerType
{
    /// <summary>Triggered when the dependency skill starts.</summary>
    Start,

    /// <summary>Triggered when the dependency skill finishes.</summary>
    Finish
}

/// <summary>
///     Represents a single event prerequisite for a skill to execute.
///     Maps DependencyEdge handles to event types.
/// </summary>
public record EventPrerequisite
{
    /// <summary>The ID of the skill that must trigger the event.</summary>
    public required Guid DependencySkillId { get; init; }

    /// <summary>The type of event required (Start or Finish).</summary>
    public required EventTriggerType RequiredEventType { get; init; }

    /// <summary>
    ///     The dependency type (FS/SS/SF/FF) derived from source and target handles.
    ///     Shared with the scheduling layer's <see cref="DependencyType" />.
    /// </summary>
    public DependencyType DependencyType { get; init; } = DependencyType.FinishToStart;
}