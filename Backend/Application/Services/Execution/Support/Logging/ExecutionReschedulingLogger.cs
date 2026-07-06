using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides high-performance source-generated logging for rescheduling coordination
///     operations during procedure execution.
/// </summary>
public static partial class ExecutionReschedulingLogger
{
    /// <summary>
    ///     Logs the start of a rescheduling operation with the reason and current execution time.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">The reason for rescheduling.</param>
    /// <param name="currentTime">The current execution time in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Re-scheduling remaining skills: {Reason} at time {CurrentTime:F1}s")]
    public static partial void LogReschedulingStarted(
        this ILogger logger,
        string reason,
        double currentTime);

    /// <summary>
    ///     Logs that a rescheduling attempt failed with an error message from the schedule result.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorMessage">The error message from the failed schedule computation.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Re-scheduling failed: {ErrorMessage}")]
    public static partial void LogReschedulingFailed(
        this ILogger logger,
        string? errorMessage);

    /// <summary>
    ///     Logs that an exception occurred during the rescheduling operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to re-schedule remaining skills")]
    public static partial void LogReschedulingException(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs the application of a live router selection to a router node for UI display.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router node.</param>
    /// <param name="oldTarget">The previous selected target node ID, or "null" if none.</param>
    /// <param name="newTarget">The new selected target node ID.</param>
    /// <param name="branchName">The name of the selected branch.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Applying live router selection to '{RouterName}': {OldTarget} -> {NewTarget} (Branch: '{BranchName}')")]
    public static partial void LogRouterSelectionApplied(
        this ILogger logger,
        string routerName,
        string oldTarget,
        Guid newTarget,
        string branchName);

    /// <summary>
    ///     Logs that the live router selection targets a node ID that does not match any branch
    ///     in the router's branch list, so the existing branch name is used as a fallback.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router node.</param>
    /// <param name="selectedTargetId">The target node ID from the live selection that had no matching branch.</param>
    /// <param name="fallbackBranchName">The pre-existing branch name used as the fallback display value.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Live router selection for '{RouterName}' targets node {SelectedTargetId} which matches no branch in the router's branch list; falling back to existing branch name '{FallbackBranchName}'")]
    public static partial void LogRouterBranchLookupMiss(
        this ILogger logger,
        string routerName,
        Guid selectedTargetId,
        string? fallbackBranchName);
}