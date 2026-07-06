using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Variables;

/// <summary>
///     Service for resolving and managing variables during procedure execution.
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    ///     Initializes a variable context for a procedure execution.
    /// </summary>
    Task<VariableContext> InitializeContextAsync(
        Guid procedureExecutionId,
        Procedure procedure,
        Dictionary<string, object>? userProvidedValues = null);

    /// <summary>
    ///     Updates a variable value in the context and persists to database.
    /// </summary>
    Task UpdateValueAsync(
        VariableContext context,
        string variableName,
        object value,
        string? updatedBy = null);

    /// <summary>
    ///     Resolves a variable binding (reads from variable, applies transform if specified).
    /// </summary>
    Task<object?> ResolveBindingAsync(
        VariableContext context,
        VariableBinding binding,
        IExpressionEvaluator? expressionEvaluator = null);
}