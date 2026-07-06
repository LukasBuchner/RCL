namespace FHOOE.Freydis.Agents.Utilities;

/// <summary>
///     Tracks execution statistics for an agent using lock-free atomic operations.
///     Thread-safe implementation using <see cref="Interlocked" /> for high-performance concurrent access.
/// </summary>
public sealed class AgentExecutionStatistics
{
    private int _activeExecutions;
    private int _failedExecutions;
    private int _totalExecutions;

    /// <summary>
    ///     Gets the number of skill executions currently in progress.
    /// </summary>
    public int ActiveExecutions => Interlocked.CompareExchange(ref _activeExecutions, 0, 0);

    /// <summary>
    ///     Gets the total number of skill executions completed since agent startup.
    /// </summary>
    public int TotalExecutions => Interlocked.CompareExchange(ref _totalExecutions, 0, 0);

    /// <summary>
    ///     Gets the number of skill executions that failed.
    /// </summary>
    public int FailedExecutions => Interlocked.CompareExchange(ref _failedExecutions, 0, 0);

    /// <summary>
    ///     Gets the success rate as a percentage (0-100) of completed executions.
    ///     Returns 100 if no executions have been attempted yet.
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = TotalExecutions;
            var failed = FailedExecutions;
            return total > 0 ? (double)(total - failed) / total * 100 : 100;
        }
    }

    /// <summary>
    ///     Atomically increments the count of active executions.
    /// </summary>
    public void IncrementActive()
    {
        Interlocked.Increment(ref _activeExecutions);
    }

    /// <summary>
    ///     Atomically decrements the count of active executions.
    /// </summary>
    public void DecrementActive()
    {
        Interlocked.Decrement(ref _activeExecutions);
    }

    /// <summary>
    ///     Atomically increments the total execution count.
    /// </summary>
    public void IncrementTotal()
    {
        Interlocked.Increment(ref _totalExecutions);
    }

    /// <summary>
    ///     Atomically increments the failed execution count.
    /// </summary>
    public void IncrementFailed()
    {
        Interlocked.Increment(ref _failedExecutions);
    }

    /// <summary>
    ///     Gets a consistent snapshot of all execution counters.
    /// </summary>
    /// <returns>A tuple containing (active, total, failed) execution counts.</returns>
    public (int active, int total, int failed) GetSnapshot()
    {
        return (ActiveExecutions, TotalExecutions, FailedExecutions);
    }
}