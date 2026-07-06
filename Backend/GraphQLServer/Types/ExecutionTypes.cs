using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;

namespace FHOOE.Freydis.GraphQLServer.Types;

/// <summary>
///     GraphQL type representing an execution event (Start or Finish).
/// </summary>
public class ExecutionEventDto
{
    /// <summary>
    ///     The ID of the skill that triggered this event.
    /// </summary>
    public Guid SkillId { get; set; }

    /// <summary>
    ///     The type of event (Start or Finish).
    /// </summary>
    public ExecutionEventType EventType { get; set; }

    /// <summary>
    ///     When the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    ///     Error message describing why the skill failed. Only set for <see cref="ExecutionEventType.Failed"/> events.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Creates a DTO from an ExecutionEvent.
    /// </summary>
    public static ExecutionEventDto FromExecutionEvent(ExecutionEvent executionEvent)
    {
        return new ExecutionEventDto
        {
            SkillId = executionEvent.SkillId,
            EventType = executionEvent.EventType,
            Timestamp = executionEvent.Timestamp,
            ErrorMessage = executionEvent.ErrorMessage
        };
    }
}

/// <summary>
///     GraphQL type representing the complete execution state of a skill.
/// </summary>
public class SkillExecutionStateDto
{
    /// <summary>
    ///     The ID of the skill being executed.
    /// </summary>
    public Guid SkillId { get; set; }

    /// <summary>
    ///     The current execution status.
    /// </summary>
    public ExecutionStatus ExecutionStatus { get; set; }

    /// <summary>
    ///     The ID of the assigned agent, if any.
    /// </summary>
    public Guid? AssignedAgentId { get; set; }

    /// <summary>
    ///     The time when execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    ///     The time when execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    ///     The time when this state was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     The error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     The last progress percentage reported (0-100).
    /// </summary>
    public double? LastProgressPercentage { get; set; }

    /// <summary>
    ///     Creates a DTO from a SkillExecutionState.
    /// </summary>
    public static SkillExecutionStateDto FromSkillExecutionState(SkillExecutionState state)
    {
        return new SkillExecutionStateDto
        {
            SkillId = state.SkillNode.Id,
            ExecutionStatus = state.ExecutionStatus,
            AssignedAgentId = state.AssignedAgent?.Id,
            StartedAt = state.StartedAt,
            CompletedAt = state.CompletedAt,
            CreatedAt = state.CreatedAt,
            ErrorMessage = state.ErrorMessage,
            LastProgressPercentage = state.LastProgressPercentage
        };
    }
}

/// <summary>
///     GraphQL type representing the complete execution state of all skills.
/// </summary>
public class ExecutionStateDto
{
    /// <summary>
    ///     All skill execution states.
    /// </summary>
    public List<SkillExecutionStateDto> Skills { get; set; } = [];

    /// <summary>
    ///     The total number of skills.
    /// </summary>
    public int TotalSkills => Skills.Count;

    /// <summary>
    ///     The number of skills that are running.
    /// </summary>
    public int RunningCount => Skills.Count(s => s.ExecutionStatus == ExecutionStatus.Running);

    /// <summary>
    ///     The number of skills that are completed.
    /// </summary>
    public int CompletedCount => Skills.Count(s => s.ExecutionStatus == ExecutionStatus.Completed);

    /// <summary>
    ///     The number of skills that have failed.
    /// </summary>
    public int FailedCount => Skills.Count(s => s.ExecutionStatus == ExecutionStatus.Failed);

    /// <summary>
    ///     The number of skills that have not started.
    /// </summary>
    public int NotStartedCount => Skills.Count(s => s.ExecutionStatus == ExecutionStatus.NotStarted);

    /// <summary>
    ///     The number of skills that are scheduled.
    /// </summary>
    public int ScheduledCount => Skills.Count(s => s.ExecutionStatus == ExecutionStatus.Scheduled);

    /// <summary>
    ///     Creates a DTO from a collection of SkillExecutionStates.
    /// </summary>
    public static ExecutionStateDto FromSkillExecutionStates(IReadOnlyCollection<SkillExecutionState> states)
    {
        return new ExecutionStateDto
        {
            Skills = states.Select(SkillExecutionStateDto.FromSkillExecutionState).ToList()
        };
    }
}

/// <summary>
///     GraphQL type representing aggregated execution timing information.
/// </summary>
public class ExecutionTimingDto
{
    /// <summary>
    ///     Wall-clock UTC time when the execution started.
    /// </summary>
    public DateTimeOffset StartTimeUtc { get; set; }

    /// <summary>
    ///     Elapsed seconds since the execution started.
    /// </summary>
    public double CurrentTimeSeconds { get; set; }

    /// <summary>
    ///     Estimated total duration of the procedure in seconds.
    /// </summary>
    public double EstimatedTotalDurationSeconds { get; set; }

    /// <summary>
    ///     Estimated wall-clock UTC time when the execution will finish.
    /// </summary>
    public DateTimeOffset EstimatedEndTimeUtc { get; set; }

    /// <summary>
    ///     Overall execution progress as a percentage (0-100).
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    ///     Whether the execution is currently running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    ///     Creates a DTO from an ExecutionTimingInfo.
    /// </summary>
    public static ExecutionTimingDto FromExecutionTimingInfo(ExecutionTimingInfo info)
    {
        return new ExecutionTimingDto
        {
            StartTimeUtc = info.StartTimeUtc,
            CurrentTimeSeconds = info.CurrentTimeSeconds,
            EstimatedTotalDurationSeconds = info.EstimatedTotalDurationSeconds,
            EstimatedEndTimeUtc = info.EstimatedEndTimeUtc,
            ProgressPercentage = info.ProgressPercentage,
            IsRunning = info.IsRunning
        };
    }
}

/// <summary>
///     GraphQL type representing an operator-facing execution advisory (e.g. a skill running past
///     its scheduled finish). Advisory only — it never reflects a control-plane state change.
/// </summary>
public class ExecutionAdvisoryDto
{
    /// <summary>Identifier of the node/skill the advisory concerns.</summary>
    public Guid SkillId { get; set; }

    /// <summary>Human-readable skill name.</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>Seconds the skill has run past its scheduled finish.</summary>
    public double OverrunSeconds { get; set; }

    /// <summary>Scheduled finish time the skill overran, in procedure-relative seconds.</summary>
    public double ScheduledFinishSeconds { get; set; }

    /// <summary>Severity of the advisory.</summary>
    public ExecutionAdvisorySeverity Severity { get; set; }

    /// <summary>Operator-facing message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Wall-clock UTC time the advisory was raised.</summary>
    public DateTimeOffset RaisedAtUtc { get; set; }

    /// <summary>
    ///     Creates a DTO from an <see cref="ExecutionAdvisory" />.
    /// </summary>
    /// <param name="advisory">The advisory to project.</param>
    /// <returns>The GraphQL DTO.</returns>
    public static ExecutionAdvisoryDto FromExecutionAdvisory(ExecutionAdvisory advisory)
    {
        return new ExecutionAdvisoryDto
        {
            SkillId = advisory.SkillId,
            SkillName = advisory.SkillName,
            OverrunSeconds = advisory.OverrunSeconds,
            ScheduledFinishSeconds = advisory.ScheduledFinishSeconds,
            Severity = advisory.Severity,
            Message = advisory.Message,
            RaisedAtUtc = advisory.RaisedAtUtc
        };
    }
}