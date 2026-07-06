namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Represents a task with a fixed, predetermined duration.
/// </summary>
public record FixedDurationPlannedSkill : IPlannedSkillExecution
{
    /// <summary>
    ///     Gets the unique identifier for the task.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the duration of the task in seconds. Must be a non-negative value.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a negative duration is assigned.</exception>
    public required double PlannedDuration
    {
        get;
        set =>
            field = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), "Duration must be positive.");
    }

    /// <summary>
    ///     Gets or sets the start time of the task in seconds. Must be a non-negative value.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a negative start time is assigned.</exception>
    public double PlannedStartTime
    {
        get;
        set =>
            field = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), "Start time must be positive.");
    }

    /// <summary>
    ///     Gets or sets the finish time of the task in seconds.
    ///     If not explicitly set, it is computed as StartTime + Duration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a negative finish time is assigned.</exception>
    public double PlannedFinishTime
    {
        get => field != 0 ? field : PlannedStartTime + PlannedDuration;
        set =>
            field = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), "Finish time must be positive.");
    }

    /// <summary>
    ///     Returns a string representation of the task, including its ID and duration.
    /// </summary>
    /// <returns>A string describing the task.</returns>
    public override string ToString()
    {
        return $"Task '{Id}' (Duration: {PlannedDuration}s)";
    }
}