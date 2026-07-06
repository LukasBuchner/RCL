namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Central event bus for skill execution events.
///     Provides IObservable streams for Start, Finish, and Failed events.
/// </summary>
public interface ISkillExecutionEventBus
{
    /// <summary>
    ///     Observable stream of all execution events (Start, Finish, Failed, Progress, NotSelected).
    /// </summary>
    IObservable<ExecutionEvent> AllEvents { get; }

    /// <summary>
    ///     Observable stream of only Start events.
    /// </summary>
    IObservable<ExecutionEvent> StartEvents { get; }

    /// <summary>
    ///     Observable stream of only Finish events (successful completion).
    /// </summary>
    IObservable<ExecutionEvent> FinishEvents { get; }

    /// <summary>
    ///     Observable stream of only Failed events (execution errors from agents).
    /// </summary>
    IObservable<ExecutionEvent> FailedEvents { get; }

    /// <summary>
    ///     Publishes an execution event to all subscribers.
    /// </summary>
    /// <param name="executionEvent">The event to publish.</param>
    void PublishEvent(ExecutionEvent executionEvent);
}