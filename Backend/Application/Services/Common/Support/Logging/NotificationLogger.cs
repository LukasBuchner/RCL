using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Common.Support.Logging;

/// <summary>
///     Provides structured logging for entity notification service operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class NotificationLogger
{
    /// <summary>
    ///     Logs a notification that entity changes have been dispatched to subscribers.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the entity being notified about.</param>
    /// <param name="count">The number of entities included in the change notification.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notifying {EntityType} changes: {Count} entities")]
    public static partial void LogEntityChangesNotified(
        this ILogger logger,
        string entityType,
        int count);

    /// <summary>
    ///     Logs the successful loading of initial entity data for a new subscription.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The CLR type name of the entity being loaded.</param>
    /// <param name="count">The number of entities loaded from the repository.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded initial {EntityType} data for subscription: {Count} entities")]
    public static partial void LogInitialDataLoadedForSubscription(
        this ILogger logger,
        string entityType,
        int count);

    /// <summary>
    ///     Logs a failure to load initial entity data for a new subscription.
    ///     The subscription continues with an empty initial dataset.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the load failure.</param>
    /// <param name="entityType">The CLR type name of the entity that failed to load.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load initial {EntityType} data for subscription")]
    public static partial void LogInitialDataLoadFailedForSubscription(
        this ILogger logger,
        Exception exception,
        string entityType);
}