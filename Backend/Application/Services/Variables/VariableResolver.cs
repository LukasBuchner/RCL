using System.Globalization;
using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Application.Services.Variables.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Variables;

/// <summary>
///     Service for resolving and managing variables during procedure execution.
///     Variable contexts are purely in-memory runtime state and are not persisted.
/// </summary>
public class VariableResolver(
    ILogger<VariableResolver> logger)
    : IVariableResolver
{
    public Task<VariableContext> InitializeContextAsync(
        Guid procedureExecutionId,
        Procedure procedure,
        Dictionary<string, object>? userProvidedValues = null)
    {
        // Validate user-provided values
        if (userProvidedValues != null)
            foreach (var kvp in userProvidedValues)
            {
                var variable = procedure.Variables.FirstOrDefault(v => v.Name == kvp.Key);
                if (variable == null)
                    throw new InvalidOperationException($"Variable '{kvp.Key}' is not defined in procedure");

                // Check if variable is read-only
                if (variable.IsReadOnly)
                    throw new InvalidOperationException($"Cannot override read-only variable '{kvp.Key}'");

                // Validate type compatibility
                var expectedType = variable.Type.ClrType;
                var actualType = kvp.Value.GetType();

                if (!IsTypeCompatible(expectedType, actualType))
                    throw new InvalidCastException(
                        $"Cannot convert {actualType.Name} to {expectedType.Name} for variable '{kvp.Key}'");
            }

        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = procedureExecutionId,
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Initialize with default values
        foreach (var variable in procedure.Variables)
        {
            var value = variable.DefaultValue;

            // Override with user-provided value if exists
            if (userProvidedValues?.TryGetValue(variable.Name, out var userValue) == true) value = userValue;

            // Check if variable has no value (required validation)
            if (value == null && variable.DefaultValue == null)
            {
                // If the variable has no default value and no user-provided value, it's required
                var hasUserValue = userProvidedValues?.ContainsKey(variable.Name) ?? false;
                if (!hasUserValue)
                    throw new InvalidOperationException(
                        $"Required variable '{variable.Name}' is missing and has no default value");
            }

            if (value != null) context.SetValue(variable.Name, value, "System");
        }

        logger.LogVariableContextInitialized(procedureExecutionId, procedure.Variables.Count);

        return Task.FromResult(context);
    }

    public static Task<T> ResolveValueAsync<T>(VariableContext context, string variableName)
    {
        if (!context.TryGetValue(variableName, out var value)) throw new VariableNotFoundException(variableName);

        if (value is T typedValue) return Task.FromResult(typedValue);

        // Try type conversion
        try
        {
            var converted = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture)!;
            return Task.FromResult(converted);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new VariableTypeMismatchException(
                variableName,
                typeof(T),
                value?.GetType() ?? typeof(object));
        }
    }

    public Task UpdateValueAsync(
        VariableContext context,
        string variableName,
        object value,
        string? updatedBy = null)
    {
        context.SetValue(variableName, value, updatedBy);

        logger.LogVariableUpdated(variableName, context.ProcedureExecutionId);

        return Task.CompletedTask;
    }

    public async Task<object?> ResolveBindingAsync(
        VariableContext context,
        VariableBinding binding,
        IExpressionEvaluator? expressionEvaluator = null)
    {
        // Read value from variable
        if (!context.TryGetValue(binding.VariableName, out var value))
            throw new VariableNotFoundException(binding.VariableName);

        // Apply transform if specified
        if (string.IsNullOrEmpty(binding.TransformExpression) || expressionEvaluator == null) return value;
        var transformContext = new Dictionary<string, object?>
        {
            ["value"] = value
        };

        value = await expressionEvaluator.EvaluateAsync(
            binding.TransformExpression,
            transformContext);

        return value;
    }

    private static bool IsTypeCompatible(Type expectedType, Type actualType)
    {
        // Direct match
        if (expectedType.IsAssignableFrom(actualType))
            return true;

        // Numeric type compatibility
        return IsNumericType(expectedType) && IsNumericType(actualType);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(decimal);
    }
}