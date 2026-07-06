using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

namespace FHOOE.Freydis.Application.Services.Common.Context;

/// <summary>
///     Provides access to the current procedure context by delegating to IProcedureOrchestrator.
///     Validates procedure ownership and ensures operations are scoped to the loaded procedure.
/// </summary>
public class ProcedureContext : IProcedureContext
{
    private readonly IProcedureOrchestrator _procedureOrchestrator;

    /// <summary>
    ///     Initializes a new instance of the ProcedureContext class.
    /// </summary>
    /// <param name="procedureOrchestrator">The procedure orchestrator for accessing loaded procedure state.</param>
    public ProcedureContext(IProcedureOrchestrator procedureOrchestrator)
    {
        _procedureOrchestrator =
            procedureOrchestrator ?? throw new ArgumentNullException(nameof(procedureOrchestrator));
    }

    /// <inheritdoc />
    public Guid? CurrentProcedureId => _procedureOrchestrator.GetLoadedProcedureId();

    /// <inheritdoc />
    public Guid RequireCurrentProcedureId()
    {
        return CurrentProcedureId
               ?? throw new InvalidOperationException(
                   "No procedure is currently loaded. Load a procedure before performing this operation.");
    }

    /// <inheritdoc />
    public void ValidateProcedureOwnership(Guid entityProcedureId)
    {
        if (entityProcedureId == Guid.Empty)
            throw new ArgumentException(
                "Entity procedure ID cannot be empty.",
                nameof(entityProcedureId));

        var currentId = RequireCurrentProcedureId();

        if (entityProcedureId != currentId)
            throw new InvalidOperationException(
                $"Entity with procedure ID '{entityProcedureId}' does not belong to the current procedure '{currentId}'. " +
                "Cannot access entities from other procedures.");
    }

    /// <inheritdoc />
    public bool IsCurrentProcedure(Guid procedureId)
    {
        if (procedureId == Guid.Empty) return false;

        var currentProcedureId = _procedureOrchestrator.GetLoadedProcedureId();
        return currentProcedureId == procedureId;
    }

    /// <inheritdoc />
    public void SetCurrentProcedureId(Guid procedureId)
    {
        throw new NotSupportedException(
            "Setting the procedure ID directly is not supported. Use IProcedureOrchestrator.LoadProcedureAsync() instead.");
    }

    /// <inheritdoc />
    public void ClearCurrentProcedure()
    {
        throw new NotSupportedException(
            "Clearing the procedure directly is not supported. Use IProcedureOrchestrator.UnloadCurrentProcedureAsync() instead.");
    }

    /// <inheritdoc />
    public IObservable<Guid?> ProcedureChanges => _procedureOrchestrator.ProcedureChanges;
}