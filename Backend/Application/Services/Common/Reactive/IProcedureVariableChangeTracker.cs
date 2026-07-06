using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Tracks changes to the variable definitions of the currently loaded procedure
///     and exposes them as an observable stream.
///     Emits the current list of <see cref="VariableDefinition" /> whenever variables are
///     added, updated, or removed, or when a procedure is loaded.
///     Emits an empty list when no procedure is loaded.
/// </summary>
public interface IProcedureVariableChangeTracker
{
    /// <summary>
    ///     Gets an observable stream that emits the current variable definitions
    ///     of the loaded procedure whenever they change.
    ///     Emits an empty list when no procedure is loaded.
    /// </summary>
    IObservable<IReadOnlyList<VariableDefinition>> Variables { get; }

    /// <summary>
    ///     Notifies subscribers that the variable definitions have changed.
    ///     Called after variable additions, updates, removals, or when a procedure is loaded.
    /// </summary>
    /// <param name="variables">The current list of variable definitions for the loaded procedure.</param>
    void NotifyChanged(IReadOnlyList<VariableDefinition> variables);

    /// <summary>
    ///     Notifies subscribers that no procedure is currently loaded,
    ///     clearing the variable list to an empty collection.
    /// </summary>
    void NotifyUnloaded();
}