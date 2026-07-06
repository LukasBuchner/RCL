using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Common.Support.Logging;

/// <summary>
///     Provides structured logging for platform-specific service operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class PlatformLogger
{
    /// <summary>
    ///     Logs that the Windows system timer resolution has been set to 1ms for accurate
    ///     sub-16ms Rx.NET timer operations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Windows timer resolution set to 1ms — sub-16ms Rx.NET timers will fire accurately")]
    public static partial void LogTimerResolutionSet(
        this ILogger logger);

    /// <summary>
    ///     Logs that the current platform does not require timer resolution adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="os">The operating system description string.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Non-Windows platform detected ({OS}); timer resolution adjustment not required")]
    public static partial void LogNonWindowsPlatform(
        this ILogger logger,
        string os);

    /// <summary>
    ///     Logs that the Windows system timer resolution has been restored to its default value.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Windows timer resolution restored to default")]
    public static partial void LogTimerResolutionRestored(
        this ILogger logger);
}