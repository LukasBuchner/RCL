namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Reactive tracker that continuously validates a procedure graph and exposes a stream of
///     <see cref="ProcedureValidationResult" /> to power soft-warning UX.
/// </summary>
/// <remarks>
///     <para>
///         The tracker maintains a single internal subscription that uses
///         <c>CombineLatest(nodes, edges)</c> to re-run all validators whenever either collection
///         changes.  The pipeline is throttled by one second and de-duplicated with
///         <c>DistinctUntilChanged</c>, so subscribers receive at most one emission per
///         meaningful state change rather than one per keystroke.
///     </para>
///     <para>
///         This tracker is <strong>UX-only and not safety-critical</strong>.  The results it
///         surfaces may be up to 1 second stale relative to the current graph state.  The hard
///         execution gate is enforced inline by
///         <see cref="IAgentSerializationValidator.Validate" /> inside the
///         <c>ExecutionOrchestrator</c>, independently of this tracker.
///     </para>
///     <para>
///         Adding a new validation rule requires only two changes: implement the rule in a new
///         <c>IXxxValidator</c> and add its result field to <see cref="ProcedureValidationResult" />.
///         No new tracker or subscription is needed.
///     </para>
/// </remarks>
public interface IProcedureValidationTracker
{
    /// <summary>
    ///     Hot observable that emits a new <see cref="ProcedureValidationResult" /> whenever
    ///     the validation outcome changes.  Backed by a <c>BehaviorSubject</c>, so late
    ///     subscribers immediately receive the latest known result.
    /// </summary>
    IObservable<ProcedureValidationResult> ValidationResults { get; }

    /// <summary>
    ///     Synchronous snapshot of the most recently computed validation result.
    ///     Useful for request-time checks that do not need to subscribe to the full stream.
    /// </summary>
    ProcedureValidationResult CurrentResult { get; }
}