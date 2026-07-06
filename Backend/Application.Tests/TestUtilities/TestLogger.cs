using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Tests.TestUtilities;

/// <summary>
///     Test logger implementation that captures log messages for verification.
///     This is the recommended approach for testing source-generated logging,
///     as it avoids Moq's incompatibility with source-generated delegate signatures.
/// </summary>
/// <typeparam name="T">The category type for the logger.</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logEntries = new();

    /// <summary>
    ///     Gets the captured log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> LogEntries => _logEntries.AsReadOnly();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logEntries.Add(new LogEntry(logLevel, eventId, message, exception));
    }

    /// <summary>
    ///     Clears all captured log entries.
    /// </summary>
    public void Clear()
    {
        _logEntries.Clear();
    }

    /// <summary>
    ///     Gets log entries matching the specified log level.
    /// </summary>
    public IEnumerable<LogEntry> GetEntriesByLevel(LogLevel level)
    {
        return _logEntries.Where(e => e.Level == level);
    }

    /// <summary>
    ///     Gets log entries containing the specified text.
    /// </summary>
    public IEnumerable<LogEntry> GetEntriesContaining(string text)
    {
        return _logEntries.Where(e => e.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Verifies that a log entry exists with the specified level and message content.
    /// </summary>
    public bool HasEntry(LogLevel level, string messageContent)
    {
        return _logEntries.Any(e =>
            e.Level == level && e.Message.Contains(messageContent, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
///     Represents a captured log entry.
/// </summary>
/// <param name="Level">The log level.</param>
/// <param name="EventId">The event ID.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The exception, if any.</param>
public record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);