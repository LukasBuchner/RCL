using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

/// <summary>
///     Represents the internal state for a skill that is actively being executed,
///     as managed by the orchestrator.
/// </summary>
public record SkillExecution : ISkillExecution
{
    /// <summary>
    ///     Gets or sets the actual time at which execution began.
    /// </summary>
    public double? ActualStartTime { get; set; }

    public double? ActualFinishTime { get; set; }
    public double? EstimatedDuration { get; set; }

    public required Guid Id { get; set; }
    public double PlannedStartTime { get; set; }
    public double PlannedFinishTime { get; set; }
    public double PlannedDuration { get; set; }

    /// <summary>
    ///     Gets the unique identifier for this execution instance.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    public required string Name { get; init; }

    /// <summary>
    ///     Gets the domain entity representing the actual skill to execute.
    /// </summary>
    public required Skill DomainSkill { get; init; }

    /// <summary>
    ///     Gets the domain entity representing the actual agent to execute.
    /// </summary>
    public required Agent DomainAgent { get; init; }

    public required IRuntimeAgent RuntimeAgent { get; init; }

    /// <summary>
    ///     Gets or sets the most recent progress update for this execution.
    /// </summary>
    public SkillExecutionProgress? LastProgress { get; set; }

    /// <summary>
    ///     Gets or sets the subscription used to observe progress updates.
    /// </summary>
    public IDisposable? ProgressSubscription { get; set; }

    /// <summary>
    ///     Gets or sets the subject used to issue adaptation requests
    ///     for adaptive task handling.
    /// </summary>
    public ISubject<double>? AdaptationRequestSubject { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this execution supports
    ///     adaptive behavior.
    /// </summary>
    public bool IsAdaptive { get; set; }
}