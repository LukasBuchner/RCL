namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Represents a task with an adaptable duration bounded below by a minimum and unbounded above.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - LPSchedulingValidity.lean — adaptive tasks with sufficient (unbounded) headroom preserve LP feasibility
/// </remarks>
public record AdaptivePlannedSkill : IAdaptivePlannedSkillExecution
{
    /// <summary>
    ///     Indicates whether the task can meet its minimum duration constraint.
    /// </summary>
    public bool CanMeetDurationConstraints { get; set; }

    /// <summary>
    ///     Gets the unique identifier for the adaptive task.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    ///     Gets the minimum allowable duration for this task. The duration is unbounded above.
    /// </summary>
    public required double MinDuration { get; set; }

    /// <summary>
    ///     Gets or sets the target duration. Must be at least <see cref="MinDuration" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the target duration is below the minimum.</exception>
    public required double PlannedDuration
    {
        get;
        set
        {
            if (value < MinDuration)
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"Target duration must be at least {MinDuration}.");
            field = value;
        }
    }

    /// <summary>
    ///     Gets or sets the start time of the task in seconds. Must be non-negative.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when start time is negative.</exception>
    public double PlannedStartTime
    {
        get;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Start time must be positive.");
            field = value;
        }
    }

    /// <summary>
    ///     Gets or sets the finish time of the task in seconds.
    ///     If not explicitly set, it is calculated as StartTime + Duration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when finish time is negative.</exception>
    public double PlannedFinishTime
    {
        get => field != 0 ? field : PlannedStartTime + PlannedDuration;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Finish time must be positive.");
            field = value;
        }
    }

    /// <summary>
    ///     Returns a string representation of the adaptive task, including its ID, target duration, and minimum.
    /// </summary>
    /// <returns>A string describing the task.</returns>
    public override string ToString()
    {
        return $"Adaptive Task '{Id}' (Target: {PlannedDuration}s, Min: {MinDuration}s)";
    }
}