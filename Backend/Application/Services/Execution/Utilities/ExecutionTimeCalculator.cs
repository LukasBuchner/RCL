namespace FHOOE.Freydis.Application.Services.Execution.Utilities;

/// <summary>
///     Calculates execution time metrics.
/// </summary>
/// <remarks>
///     This service provides time calculation utilities for execution monitoring.
///     It's a simple, stateless service that performs time calculations.
/// </remarks>
public class ExecutionTimeCalculator : IExecutionTimeCalculator
{
    /// <inheritdoc />
    public double CalculateElapsedSeconds(DateTimeOffset startTime, DateTimeOffset currentTime)
    {
        var elapsed = currentTime - startTime;
        return elapsed.TotalSeconds;
    }
}