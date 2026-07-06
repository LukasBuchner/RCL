using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Service for managing variables within procedures.
///     Handles CRUD operations with validation for variable definitions.
///     Notifies the <see cref="IProcedureVariableChangeTracker" /> after each mutation
///     so that GraphQL subscribers receive real-time variable updates.
/// </summary>
public class ProcedureVariableService(
    IRepository<Procedure> procedureRepository,
    IProcedureVariableChangeTracker procedureVariableChangeTracker,
    ILogger<ProcedureVariableService> logger)
    : IProcedureVariableService
{
    private readonly ILogger<ProcedureVariableService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IRepository<Procedure> _procedureRepository =
        procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));

    private readonly IProcedureVariableChangeTracker _procedureVariableChangeTracker =
        procedureVariableChangeTracker ?? throw new ArgumentNullException(nameof(procedureVariableChangeTracker));

    /// <inheritdoc />
    public async Task<Procedure> AddVariableAsync(Guid procedureId, VariableDefinition variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        _logger.LogVariableAddStart(variable.Name, procedureId);

        var procedure = await GetProcedureOrThrowAsync(procedureId);

        // Validate: No duplicate variable names
        if (procedure.Variables.Any(v => v.Name == variable.Name))
            throw new VariableAlreadyExistsException(variable.Name, procedureId);

        // Create new list with added variable
        var updatedVariables = new List<VariableDefinition>(procedure.Variables) { variable };

        var updatedProcedure = procedure with
        {
            Variables = updatedVariables.AsReadOnly(),
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var success = await _procedureRepository.UpdateAsync(updatedProcedure);
        if (!success) throw new InvalidOperationException($"Failed to update procedure {procedureId}");

        _logger.LogVariableAddSuccess(variable.Name, procedureId);

        _procedureVariableChangeTracker.NotifyChanged(updatedProcedure.Variables);

        return updatedProcedure;
    }

    /// <inheritdoc />
    public async Task<Procedure> UpdateVariableAsync(
        Guid procedureId,
        string variableName,
        VariableDefinition updatedVariable)
    {
        ArgumentNullException.ThrowIfNull(updatedVariable);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);

        _logger.LogVariableUpdateStart(variableName, procedureId);

        var procedure = await GetProcedureOrThrowAsync(procedureId);

        // Find existing variable
        var existingIndex = procedure.Variables.ToList().FindIndex(v => v.Name == variableName);
        if (existingIndex == -1)
            throw new InvalidOperationException(
                $"Variable with name '{variableName}' not found in procedure {procedureId}");

        // If renaming, check for conflicts
        if (updatedVariable.Name != variableName)
            if (procedure.Variables.Any(v => v.Name == updatedVariable.Name))
                throw new VariableAlreadyExistsException(updatedVariable.Name, procedureId);

        // Update the variable
        var updatedVariables = procedure.Variables.ToList();
        updatedVariables[existingIndex] = updatedVariable;

        var updatedProcedure = procedure with
        {
            Variables = updatedVariables.AsReadOnly(),
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var success = await _procedureRepository.UpdateAsync(updatedProcedure);
        if (!success) throw new InvalidOperationException($"Failed to update procedure {procedureId}");

        _logger.LogVariableUpdateSuccess(variableName, procedureId);

        _procedureVariableChangeTracker.NotifyChanged(updatedProcedure.Variables);

        return updatedProcedure;
    }

    /// <inheritdoc />
    public async Task<Procedure> RemoveVariableAsync(Guid procedureId, string variableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);

        _logger.LogVariableRemoveStart(variableName, procedureId);

        var procedure = await GetProcedureOrThrowAsync(procedureId);

        // Find existing variable
        var existingVariable = procedure.Variables.FirstOrDefault(v => v.Name == variableName);
        if (existingVariable == null)
            throw new InvalidOperationException(
                $"Variable with name '{variableName}' not found in procedure {procedureId}");

        // Remove the variable
        var updatedVariables = procedure.Variables
            .Where(v => v.Name != variableName)
            .ToList();

        var updatedProcedure = procedure with
        {
            Variables = updatedVariables.AsReadOnly(),
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var success = await _procedureRepository.UpdateAsync(updatedProcedure);
        if (!success) throw new InvalidOperationException($"Failed to update procedure {procedureId}");

        _logger.LogVariableRemoveSuccess(variableName, procedureId);

        _procedureVariableChangeTracker.NotifyChanged(updatedProcedure.Variables);

        return updatedProcedure;
    }

    private async Task<Procedure> GetProcedureOrThrowAsync(Guid procedureId)
    {
        var procedure = await _procedureRepository.GetByIdAsync(procedureId);
        if (procedure == null) throw new InvalidOperationException($"Procedure with ID {procedureId} not found");
        return procedure;
    }
}