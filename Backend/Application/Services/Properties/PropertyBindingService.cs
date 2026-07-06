using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Properties.Support.Logging;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Properties;

/// <summary>
///     Service for binding skill properties to procedure variables during execution.
///     Handles reading input values from variables and writing output values back to variables.
/// </summary>
public class PropertyBindingService : IPropertyBindingService
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<PropertyBindingService> _logger;
    private readonly IVariableResolver _variableResolver;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PropertyBindingService" /> class.
    /// </summary>
    public PropertyBindingService(
        IVariableResolver variableResolver,
        IExpressionEvaluator expressionEvaluator,
        ILogger<PropertyBindingService> logger)
    {
        _variableResolver = variableResolver ?? throw new ArgumentNullException(nameof(variableResolver));
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> ResolveInputBindingsAsync(
        Skill skill,
        VariableContext context)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(context);

        var inputs = new Dictionary<string, object>();

        foreach (var property in skill.Properties)
            // Only process Input and InputOutput properties with bindings
            if ((property.Direction == PropertyDirection.Input ||
                 property.Direction == PropertyDirection.InputOutput) &&
                property.Binding != null &&
                (property.Binding.Mode == BindingMode.Read ||
                 property.Binding.Mode == BindingMode.ReadWrite))
            {
                var value = await _variableResolver.ResolveBindingAsync(
                    context,
                    property.Binding,
                    _expressionEvaluator);

                if (value != null)
                {
                    inputs[property.Name] = value;

                    _logger.LogInputBindingResolved(property.Name, property.Binding.VariableName);
                }
            }

        return inputs;
    }

    /// <inheritdoc />
    public async Task ApplyOutputBindingsAsync(
        Skill skill,
        Dictionary<string, object> skillOutputs,
        VariableContext context)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(skillOutputs);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var property in skill.Properties)
            // Only process Output and InputOutput properties with bindings
            if ((property.Direction == PropertyDirection.Output ||
                 property.Direction == PropertyDirection.InputOutput) &&
                property.Binding != null &&
                (property.Binding.Mode == BindingMode.Write ||
                 property.Binding.Mode == BindingMode.ReadWrite))
                if (skillOutputs.TryGetValue(property.Name, out var value))
                {
                    // Apply transform if specified
                    if (!string.IsNullOrEmpty(property.Binding.TransformExpression))
                    {
                        var transformContext = new Dictionary<string, object?> { ["value"] = value };
                        var transformedValue = await _expressionEvaluator.EvaluateAsync(
                            property.Binding.TransformExpression,
                            transformContext);

                        if (transformedValue != null)
                            value = transformedValue;
                        else
                            _logger.LogTransformEvaluatedToNull(
                                property.Name, property.Binding.VariableName,
                                property.Binding.TransformExpression);
                    }

                    // Write to variable
                    await _variableResolver.UpdateValueAsync(
                        context,
                        property.Binding.VariableName,
                        value,
                        $"Skill:{skill.Name}");

                    _logger.LogOutputBindingApplied(property.Name, property.Binding.VariableName);
                }
    }
}