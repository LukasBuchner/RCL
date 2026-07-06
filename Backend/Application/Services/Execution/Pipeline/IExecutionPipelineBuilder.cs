using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Triggering;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Builds the Rx.NET reschedule pipeline that transforms reschedule requests into a
///     multicast stream of <see cref="ReschedulingResult" /> values. The caller owns
///     subscriber lifetime (composition of completion detection, frontend publishing,
///     agent planned-finish updates, etc.) and is responsible for invoking
///     <see cref="IConnectableObservable{T}.Connect" /> to start the pipeline.
/// </summary>
public interface IExecutionPipelineBuilder
{
    /// <summary>
    ///     Constructs the reactive reschedule pipeline:
    ///     <c>rescheduleRequests</c> → <c>Sample</c> → <c>ConcurrentMapLatest</c> → <c>Publish</c>.
    ///     The returned observable is cold until the caller invokes
    ///     <see cref="IConnectableObservable{T}.Connect" />; all subscribers attached before
    ///     Connect receive the same sequence of results.
    /// </summary>
    /// <param name="rescheduleRequests">Source of reschedule request reasons.</param>
    /// <param name="executionTriggerService">Trigger service consulted for router selections each cycle.</param>
    /// <param name="reschedulingCoordinator">Coordinator that performs the actual reschedule computation.</param>
    /// <returns>
    ///     A connectable observable that emits the multicast stream of reschedule results;
    ///     the caller disposes the IDisposable returned by <c>Connect</c> to tear down the pipeline.
    /// </returns>
    IConnectableObservable<ReschedulingResult> Build(
        IObservable<RescheduleReason> rescheduleRequests,
        IExecutionTriggerService executionTriggerService,
        IReschedulingCoordinator reschedulingCoordinator);
}