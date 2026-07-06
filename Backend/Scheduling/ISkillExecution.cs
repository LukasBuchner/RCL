using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling;

/// <summary>
///     Extends <see cref="IPlannedSkillExecution" /> with run‑time information collected
///     while the skill is executing.
///     If the skill is not running yet, the Planned StartTime and FinishTime can still be changed.
/// </summary>
public interface ISkillExecution : IPlannedSkillExecution
{
    /// <summary>
    ///     Indicates whether the skill is currently running.
    /// </summary>
    bool IsRunning => ActualStartTime.HasValue && !ActualFinishTime.HasValue;

    /// <summary>
    ///     Indicates whether the skill has completed running.
    /// </summary>
    bool IsFinished => ActualStartTime.HasValue && ActualFinishTime.HasValue;

    /// <summary>
    ///     Actual start time relative to <c>T₀</c>, if the skill has started.
    /// </summary>
    double? ActualStartTime { get; set; }

    /// <summary>
    ///     Estimated finish time — <c>ActualStart + EstimatedDuration</c>, if calculable.
    /// </summary>
    double? EstimatedFinishTime => ActualStartTime.HasValue
        ? ActualStartTime + PlannedDuration
        : null;

    /// <summary>
    ///     Actual finish time relative to <c>T₀</c>, if the skill has completed.
    /// </summary>
    double? ActualFinishTime { get; set; }

    /// <summary>
    ///     Expected total duration based on the current progress, if available.
    /// </summary>
    double? EstimatedDuration { get; set; }

    /// <summary>
    ///     Actual duration — <c>ActualFinish - ActualStart</c>, if the skill has completed.
    /// </summary>
    double? ActualDuration => ActualStartTime.HasValue && ActualFinishTime.HasValue
        ? ActualFinishTime - ActualStartTime
        : null;
}