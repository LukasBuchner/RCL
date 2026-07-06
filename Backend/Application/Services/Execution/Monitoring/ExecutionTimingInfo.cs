namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Aggregated timing information for the currently running execution,
///     surfaced to the frontend via a GraphQL subscription.
/// </summary>
public sealed record ExecutionTimingInfo
{
    /// <summary>
    ///     Wall-clock UTC time when the execution started.
    /// </summary>
    public required DateTimeOffset StartTimeUtc { get; init; }

    /// <summary>
    ///     Elapsed seconds since the execution started (scheduler-relative time).
    /// </summary>
    public required double CurrentTimeSeconds { get; init; }

    /// <summary>
    ///     Maximum FinishTime across all scheduled nodes, representing the estimated
    ///     total duration (in seconds) of the procedure.
    /// </summary>
    public required double EstimatedTotalDurationSeconds { get; init; }

    /// <summary>
    ///     Estimated wall-clock UTC time when the execution will finish.
    /// </summary>
    public DateTimeOffset EstimatedEndTimeUtc => StartTimeUtc.AddSeconds(EstimatedTotalDurationSeconds);

    /// <summary>
    ///     Overall execution progress as a percentage (0-100).
    /// </summary>
    public required double ProgressPercentage { get; init; }

    /// <summary>
    ///     Whether the execution is currently running.
    /// </summary>
    public required bool IsRunning { get; init; }
}