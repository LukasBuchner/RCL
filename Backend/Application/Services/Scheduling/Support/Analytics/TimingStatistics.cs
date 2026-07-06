namespace FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

/// <summary>
///     Statistical information about timing calculations for a procedure.
/// </summary>
public record TimingStatistics
{
    /// <summary>
    ///     Minimum duration across all nodes.
    /// </summary>
    public required double MinDuration { get; init; }

    /// <summary>
    ///     Maximum duration across all nodes.
    /// </summary>
    public required double MaxDuration { get; init; }

    /// <summary>
    ///     Average duration across all nodes.
    /// </summary>
    public required double AverageDuration { get; init; }

    /// <summary>
    ///     Sum of all node durations.
    /// </summary>
    public required double SumDuration { get; init; }

    /// <summary>
    ///     Total number of nodes analyzed.
    /// </summary>
    public required int NodeCount { get; init; }

    /// <summary>
    ///     Earliest start time across all nodes.
    /// </summary>
    public required double EarliestStart { get; init; }

    /// <summary>
    ///     Latest finish time across all nodes.
    /// </summary>
    public required double LatestFinish { get; init; }

    /// <summary>
    ///     Total timespan from earliest start to latest finish.
    /// </summary>
    public required double TotalProcedureSpan { get; init; }
}

/// <summary>
///     Information about the critical path and parallelism in a procedure.
/// </summary>
public record CriticalPathInfo
{
    /// <summary>
    ///     IDs of nodes on the critical path (finishing at the latest finish time).
    /// </summary>
    public required IReadOnlyList<Guid> CriticalPathNodeIds { get; init; }

    /// <summary>
    ///     Maximum number of tasks executing in parallel at any point.
    /// </summary>
    public required int MaxParallelism { get; init; }

    /// <summary>
    ///     Time at which maximum parallelism occurs.
    /// </summary>
    public required double PeakParallelismTime { get; init; }
}