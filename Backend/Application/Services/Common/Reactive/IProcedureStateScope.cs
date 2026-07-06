namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Allows the procedure lifecycle orchestrator to notify the unified state tracker
///     when the active procedure changes, so that the tracker can load or clear
///     procedure-scoped node and edge data accordingly.
/// </summary>
/// <remarks>
///     This interface is implemented by <see cref="ProcedureStateTracker" /> and consumed by
///     <see cref="FHOOE.Freydis.Application.Services.EntityManagement.Procedures.ProcedureOrchestrator" />.
///     Keeping it as a narrow, dedicated interface prevents a DI circular dependency that would
///     arise from injecting <c>IProcedureContext</c> directly into the tracker (since
///     <c>IProcedureContext</c> transitively depends on <c>INodeChangeTracker</c>, which is
///     implemented by the same tracker).
/// </remarks>
public interface IProcedureStateScope
{
    /// <summary>
    ///     Notifies the tracker that a procedure has been loaded and that scoped repository
    ///     data should be fetched for that procedure.  Any previously cached data for a
    ///     different procedure is replaced.
    /// </summary>
    /// <param name="procedureId">The identifier of the newly loaded procedure.</param>
    void OnProcedureLoaded(Guid procedureId);

    /// <summary>
    ///     Notifies the tracker that no procedure is currently loaded.
    ///     The tracker immediately emits <see cref="ProcedureState.Empty" /> and clears
    ///     all cached data so that stale cross-procedure data cannot linger.
    /// </summary>
    void OnProcedureUnloaded();
}