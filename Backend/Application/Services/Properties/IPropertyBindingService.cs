using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Properties;

/// <summary>
///     Service for binding skill properties to procedure variables during execution.
///     Handles reading input values from variables and writing output values back to variables.
/// </summary>
public interface IPropertyBindingService
{
    /// <summary>
    ///     Resolves input property bindings before skill execution.
    ///     Reads values from variables and applies to properties.
    /// </summary>
    /// <param name="skill">Skill with properties to resolve.</param>
    /// <param name="context">Variable context containing runtime values.</param>
    /// <returns>Dictionary of resolved input values keyed by property name.</returns>
    Task<Dictionary<string, object>> ResolveInputBindingsAsync(
        Skill skill,
        VariableContext context);

    /// <summary>
    ///     Applies output property bindings after skill execution.
    ///     Writes values from skill outputs to variables.
    /// </summary>
    /// <param name="skill">Skill with properties to apply.</param>
    /// <param name="skillOutputs">Output values from skill execution.</param>
    /// <param name="context">Variable context to write to.</param>
    Task ApplyOutputBindingsAsync(
        Skill skill,
        Dictionary<string, object> skillOutputs,
        VariableContext context);
}