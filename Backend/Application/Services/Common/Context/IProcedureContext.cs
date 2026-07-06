namespace FHOOE.Freydis.Application.Services.Common.Context;

/// <summary>
///     Provides access to the current procedure context, enabling procedure-scoped operations
///     and validation of procedure ownership across the application layer.
/// </summary>
public interface IProcedureContext
{
    /// <summary>
    ///     Gets the identifier of the currently loaded procedure, or null if no procedure is loaded.
    /// </summary>
    Guid? CurrentProcedureId { get; }

    /// <summary>
    ///     Gets an observable that emits whenever the loaded procedure changes.
    ///     Emits the new procedure ID when a procedure is loaded, or null when unloaded.
    ///     This enables reactive components to respond automatically to procedure context changes.
    /// </summary>
    /// <remarks>
    ///     Subscribers can use this observable with CombineLatest to re-trigger filtering
    ///     and other reactive operations when the procedure context changes.
    /// </remarks>
    IObservable<Guid?> ProcedureChanges { get; }

    /// <summary>
    ///     Gets the identifier of the currently loaded procedure, throwing an exception if no procedure is loaded.
    /// </summary>
    /// <returns>The identifier of the currently loaded procedure.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no procedure is currently loaded.</exception>
    Guid RequireCurrentProcedureId();

    /// <summary>
    ///     Validates that an entity belongs to the currently loaded procedure.
    /// </summary>
    /// <param name="entityProcedureId">The procedure identifier of the entity to validate.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no procedure is currently loaded or when the entity does not belong to the current procedure.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when the entity procedure ID is empty.</exception>
    void ValidateProcedureOwnership(Guid entityProcedureId);

    /// <summary>
    ///     Checks if the specified procedure identifier matches the currently loaded procedure.
    /// </summary>
    /// <param name="procedureId">The procedure identifier to check.</param>
    /// <returns>True if the specified procedure ID matches the current procedure; otherwise, false.</returns>
    bool IsCurrentProcedure(Guid procedureId);

    /// <summary>
    ///     Sets the current procedure context to the specified procedure identifier.
    /// </summary>
    /// <param name="procedureId">The procedure identifier to set as current.</param>
    /// <exception cref="ArgumentException">Thrown when the procedure ID is empty.</exception>
    void SetCurrentProcedureId(Guid procedureId);

    /// <summary>
    ///     Clears the current procedure context, setting it to null.
    /// </summary>
    void ClearCurrentProcedure();
}