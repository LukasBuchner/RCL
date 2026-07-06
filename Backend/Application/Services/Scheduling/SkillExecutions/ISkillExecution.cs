using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;

namespace FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;

public interface ISkillExecution : Freydis.Scheduling.ISkillExecution, IPlannedSkillExecution
{
    /// <summary>
    ///     Gets the unique identifier for this execution instance.
    /// </summary>
    Guid ExecutionId { get; init; }

    /// <summary>
    ///     Gets or sets the most recent progress update for this execution.
    /// </summary>
    SkillExecutionProgress? LastProgress { get; set; }

    /// <summary>
    ///     Gets or sets the subscription used to observe progress updates.
    /// </summary>
    IDisposable? ProgressSubscription { get; set; }

    /// <summary>
    ///     Gets or sets the subject used to issue adaptation requests
    ///     for adaptive task handling.
    /// </summary>
    ISubject<double>? AdaptationRequestSubject { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this execution supports
    ///     adaptive behaviour.
    /// </summary>
    bool IsAdaptive { get; set; }
}