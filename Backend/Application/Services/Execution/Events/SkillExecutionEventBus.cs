using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Central event bus for skill execution events using Rx.NET Subject.
///     Thread-safe implementation for publishing and subscribing to execution events.
/// </summary>
public sealed class SkillExecutionEventBus : ISkillExecutionEventBus, IDisposable
{
    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly ILogger<SkillExecutionEventBus> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillExecutionEventBus" /> class.
    /// </summary>
    /// <param name="logger">Logger for event bus operations.</param>
    public SkillExecutionEventBus(ILogger<SkillExecutionEventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventSubject = new Subject<ExecutionEvent>();

        AllEvents = _eventSubject.AsObservable();
        StartEvents = AllEvents.Where(e => e.EventType == ExecutionEventType.Start);
        FinishEvents = AllEvents.Where(e => e.EventType == ExecutionEventType.Finish);
        FailedEvents = AllEvents.Where(e => e.EventType == ExecutionEventType.Failed);

        _logger.LogEventBusInitialized();
    }

    /// <summary>
    ///     Disposes the event bus and completes all subscriptions.
    /// </summary>
    public void Dispose()
    {
        _logger.LogEventBusDisposing();

        try
        {
            _eventSubject.OnCompleted();
        }
        catch (ObjectDisposedException)
        {
            // Subject already disposed, ignore
        }

        _eventSubject.Dispose();
    }

    /// <summary>
    ///     Observable stream of all execution events (Start, Finish, Failed, Progress, NotSelected).
    /// </summary>
    public IObservable<ExecutionEvent> AllEvents { get; }

    /// <summary>
    ///     Observable stream of only Start events.
    /// </summary>
    public IObservable<ExecutionEvent> StartEvents { get; }

    /// <summary>
    ///     Observable stream of only Finish events (successful completion).
    /// </summary>
    public IObservable<ExecutionEvent> FinishEvents { get; }

    /// <summary>
    ///     Observable stream of only Failed events (execution errors from agents).
    /// </summary>
    public IObservable<ExecutionEvent> FailedEvents { get; }

    /// <summary>
    ///     Publishes an execution event to all subscribers.
    /// </summary>
    /// <param name="executionEvent">The event to publish.</param>
    public void PublishEvent(ExecutionEvent executionEvent)
    {
        ArgumentNullException.ThrowIfNull(executionEvent);

        _logger.LogEventBusPublish(executionEvent.EventType, executionEvent.SkillId, executionEvent.Timestamp);

        _eventSubject.OnNext(executionEvent);
    }
}