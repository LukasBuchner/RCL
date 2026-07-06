using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FHOOE.Freydis.Domain.Entities.Variables;

/// <summary>
///     Runtime storage for variable values during procedure execution.
///     Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class VariableContext : IDisposable
{
    private readonly Subject<VariableValue> _changeSubject = new();
    private bool _disposed;
    private ConcurrentDictionary<string, VariableValue> _values = new();

    /// <summary>
    ///     Unique identifier for this context.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Identifier of the procedure execution this context belongs to.
    /// </summary>
    public Guid ProcedureExecutionId { get; set; }

    /// <summary>
    ///     Timestamp when the context was last updated (UTC).
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }

    /// <summary>
    ///     Observable stream of variable value changes.
    ///     Subscribe to receive notifications when variables are updated.
    /// </summary>
    public IObservable<VariableValue> Changes => _changeSubject.AsObservable();

    /// <summary>
    ///     Disposes the change subject, completing any active subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _changeSubject.OnCompleted();
        }
        catch (ObjectDisposedException)
        {
        }

        _changeSubject.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Gets a typed value from the context.
    /// </summary>
    /// <typeparam name="T">Type to cast the value to.</typeparam>
    /// <param name="name">Name of the variable.</param>
    /// <returns>The typed value, or default if not found.</returns>
    public T? GetValue<T>(string name)
    {
        if (_values.TryGetValue(name, out var value))
            return (T?)value.Value;
        return default;
    }

    /// <summary>
    ///     Sets a value in the context.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="updatedBy">Optional identifier of the entity setting the value.</param>
    public void SetValue(string name, object value, string? updatedBy = null)
    {
        var variableValue = new VariableValue
        {
            Name = name,
            Value = value,
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = updatedBy
        };

        _values[name] = variableValue;
        LastUpdatedUtc = DateTime.UtcNow;

        // Publish change notification
        _changeSubject.OnNext(variableValue);
    }

    /// <summary>
    ///     Tries to get a value from the context.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="value">The value if found.</param>
    /// <returns>True if the variable exists, false otherwise.</returns>
    public bool TryGetValue(string name, out object? value)
    {
        if (_values.TryGetValue(name, out var variableValue))
        {
            value = variableValue.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Gets all values in the context.
    /// </summary>
    /// <returns>Read-only dictionary of all variable values.</returns>
    public IReadOnlyDictionary<string, VariableValue> GetAllValues()
    {
        return _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}