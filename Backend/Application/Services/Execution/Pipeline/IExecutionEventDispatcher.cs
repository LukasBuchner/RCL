using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Dispatches execution events from the event bus, performing state transitions
///     and requesting reschedules. Extracted from <see cref="ExecutionOrchestrator"/>
///     to isolate event-handling logic from pipeline setup and lifecycle management.
/// </summary>
public interface IExecutionEventDispatcher
{
    /// <summary>
    ///     Handles an execution event by performing the appropriate state transition and
    ///     forwarding a reschedule request to the supplied observer. Reschedule observers
    ///     are expected to tolerate calls made after their stream has completed:
    ///     <see cref="System.Reactive.Subjects.Subject{T}" /> silently drops OnNext after
    ///     OnCompleted, which is the intended termination protocol for the reschedule
    ///     pipeline — no external "is-cleaning-up" guard is required.
    /// </summary>
    /// <param name="executionEvent">The execution event to handle.</param>
    /// <param name="currentNodes">The current list of nodes for node lookup.</param>
    /// <param name="executionStartTime">The start time of the current execution.</param>
    /// <param name="rescheduleRequests">Observer to push reschedule requests to, or <c>null</c> when no pipeline is active.</param>
    void HandleExecutionEvent(
        ExecutionEvent executionEvent,
        IReadOnlyList<Node> currentNodes,
        DateTimeOffset executionStartTime,
        IObserver<RescheduleReason>? rescheduleRequests);
}