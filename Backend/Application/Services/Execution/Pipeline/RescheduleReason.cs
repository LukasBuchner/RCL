namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Reason for requesting a reschedule in the execution pipeline.
///     Used for diagnostic logging — the pipeline does not branch on the reason.
/// </summary>
public enum RescheduleReason
{
    SkillStarted,
    SkillFinished,
    SkillFailed,
    SkillNotSelected,
    ProgressUpdate,
    RouterEvaluated
}