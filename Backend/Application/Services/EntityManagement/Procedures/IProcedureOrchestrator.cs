using FHOOE.Freydis.Application.Services.EntityManagement.Procedures.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Orchestrates procedure loading, unloading, and state management.
///     Manages which procedure is currently active and coordinates data loading.
/// </summary>
public interface IProcedureOrchestrator
{
    /// <summary>
    ///     Gets an observable that emits whenever the loaded procedure changes.
    ///     Emits the new procedure ID when a procedure is loaded, or null when unloaded.
    ///     This enables reactive components to respond automatically to procedure context changes.
    /// </summary>
    /// <remarks>
    ///     This observable is used by reactive pipelines to re-trigger filtering and updates
    ///     when the procedure context changes, ensuring that data streams remain synchronized
    ///     with the currently loaded procedure.
    /// </remarks>
    IObservable<Guid?> ProcedureChanges { get; }

    /// <summary>
    ///     Loads a procedure and marks it as the currently active procedure.
    ///     Automatically unloads any previously loaded procedure.
    /// </summary>
    /// <param name="procedureId">The unique identifier of the procedure to load.</param>
    /// <returns>The loaded procedure with its associated data.</returns>
    /// <exception cref="ProcedureNotFoundException">Thrown when the procedure doesn't exist.</exception>
    Task<Procedure> LoadProcedureAsync(Guid procedureId);

    /// <summary>
    ///     Unloads the currently active procedure, if any.
    /// </summary>
    Task UnloadCurrentProcedureAsync();

    /// <summary>
    ///     Retrieves the currently loaded procedure, if any.
    /// </summary>
    /// <returns>The loaded procedure or null if no procedure is currently loaded.</returns>
    Task<Procedure?> GetLoadedProcedureAsync();

    /// <summary>
    ///     Creates a new procedure with the specified details.
    /// </summary>
    /// <param name="name">The name of the procedure.</param>
    /// <param name="description">Optional description of the procedure.</param>
    /// <returns>The newly created procedure.</returns>
    Task<Procedure> CreateProcedureAsync(string name, string? description = null);

    /// <summary>
    ///     Deletes a procedure and all associated entities (nodes, edges, variables).
    ///     If the procedure is currently loaded, it will be unloaded first.
    /// </summary>
    /// <param name="procedureId">The unique identifier of the procedure to delete.</param>
    /// <returns>True if the procedure was deleted successfully, false otherwise.</returns>
    Task<bool> DeleteProcedureAsync(Guid procedureId);

    /// <summary>
    ///     Gets the ID of the currently loaded procedure, if any.
    /// </summary>
    /// <returns>The ID of the loaded procedure or null if none is loaded.</returns>
    Guid? GetLoadedProcedureId();

    /// <summary>
    ///     Gets the name of the currently loaded procedure, if any.
    /// </summary>
    /// <returns>The name of the loaded procedure or null if none is loaded.</returns>
    string? GetLoadedProcedureName();
}