using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Service for managing variables within procedures.
/// </summary>
public interface IProcedureVariableService
{
    /// <summary>
    ///     Adds a new variable to a procedure.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure.</param>
    /// <param name="variable">The variable definition to add.</param>
    /// <returns>The updated procedure.</returns>
    /// <exception cref="InvalidOperationException">If the procedure is not found.</exception>
    /// <exception cref="Variables.Exceptions.VariableAlreadyExistsException">If a variable with the same name already exists.</exception>
    Task<Procedure> AddVariableAsync(Guid procedureId, VariableDefinition variable);

    /// <summary>
    ///     Updates an existing variable in a procedure.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure.</param>
    /// <param name="variableName">The name of the variable to update.</param>
    /// <param name="updatedVariable">The updated variable definition.</param>
    /// <returns>The updated procedure.</returns>
    /// <exception cref="InvalidOperationException">If procedure or variable not found, or rename conflicts.</exception>
    Task<Procedure> UpdateVariableAsync(Guid procedureId, string variableName, VariableDefinition updatedVariable);

    /// <summary>
    ///     Removes a variable from a procedure.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure.</param>
    /// <param name="variableName">The name of the variable to remove.</param>
    /// <returns>The updated procedure.</returns>
    /// <exception cref="InvalidOperationException">If procedure or variable not found.</exception>
    Task<Procedure> RemoveVariableAsync(Guid procedureId, string variableName);
}