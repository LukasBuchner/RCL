namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Service responsible for monitoring and calculating execution progress.
///     Provides centralized logic for determining completion status and overall progress.
/// </summary>
public interface IExecutionProgressMonitor
{
    /// <summary>
    ///     Calculates the overall execution progress as a percentage.
    /// </summary>
    /// <returns>Progress percentage (0-100).</returns>
    double CalculateProgressPercentage();

    /// <summary>
    ///     Checks if all skill executions have completed (either successfully or with failure).
    /// </summary>
    /// <returns>True if execution is complete, false otherwise.</returns>
    bool IsExecutionComplete();

    /// <summary>
    ///     Checks if execution completed successfully (all skills completed without failures).
    /// </summary>
    /// <returns>True if all skills completed successfully, false if any failed or execution is incomplete.</returns>
    bool IsExecutionSuccessful();

    /// <summary>
    ///     Gets the count of skills in each execution status.
    /// </summary>
    /// <returns>Dictionary with status counts.</returns>
    Dictionary<string, int> GetExecutionStatistics();
}