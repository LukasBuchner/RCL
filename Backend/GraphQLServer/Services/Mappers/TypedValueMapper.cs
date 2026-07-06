using System.Globalization;
using System.Text.Json;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Types.OutputTypes;
using DomainBooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using DomainNumberType = FHOOE.Freydis.Domain.Entities.Common.NumberType;
using DomainStringType = FHOOE.Freydis.Domain.Entities.Common.StringType;
using DomainPositionType = FHOOE.Freydis.Domain.Entities.Common.PositionType;
using DomainPositionTagType = FHOOE.Freydis.Domain.Entities.Common.PositionTagType;
using DomainSceneObjectType = FHOOE.Freydis.Domain.Entities.Common.SceneObjectType;

namespace FHOOE.Freydis.GraphQLServer.Services.Mappers;

/// <summary>
///     Maps TypedValue domain objects to GraphQL PropertyValue union types.
///     Handles the conversion from the domain's TypedValue (type + value) to GraphQL wrapper objects.
/// </summary>
public partial class TypedValueMapper(ILogger<TypedValueMapper> logger) : ITypedValueMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public object MapToPropertyValue(TypedValue typedValue)
    {
        if (typedValue.Type == null)
        {
            LogNullType(logger);
            throw new InvalidOperationException(
                "TypedValue has null Type. This typically indicates a data integrity issue.");
        }

        if (typedValue.Value == null)
        {
            LogNullValue(logger, typedValue.Type.GetType().Name);

            throw new InvalidOperationException(
                $"TypedValue has null Value for type {typedValue.Type.GetType().Name}");
        }

        try
        {
            return typedValue.Type switch
            {
                DomainBooleanType boolType => new BooleanValue
                {
                    BoolValue = CoerceValue<bool>(typedValue.Value, v => v switch
                    {
                        bool b => b,
                        JsonElement je => je.GetBoolean(),
                        _ => Convert.ToBoolean(v, CultureInfo.InvariantCulture)
                    }),
                    Type = boolType
                },

                DomainNumberType numType => new NumberValue
                {
                    Value = CoerceValue<double>(typedValue.Value, v => v switch
                    {
                        JsonElement je => je.GetDouble(),
                        _ => Convert.ToDouble(v, CultureInfo.InvariantCulture)
                    }),
                    Type = numType
                },

                DomainStringType strType => new StringValue
                {
                    Value = CoerceValue<string>(typedValue.Value, v => v switch
                    {
                        string s => s,
                        JsonElement je => je.GetString() ?? "",
                        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
                    }),
                    Type = strType
                },

                DomainPositionType posType => new PositionValue
                {
                    Value = CoerceValue<Position>(typedValue.Value, v => v switch
                    {
                        Position p => p,
                        JsonElement je => JsonSerializer.Deserialize<Position>(je.GetRawText(), JsonOptions)!,
                        _ => (Position)v
                    }),
                    Type = posType
                },

                DomainPositionTagType posTagType => new PositionTagValue
                {
                    Value = CoerceValue<PositionTag>(typedValue.Value, v => v switch
                    {
                        PositionTag pt => pt,
                        JsonElement je => JsonSerializer.Deserialize<PositionTag>(je.GetRawText(), JsonOptions)!,
                        _ => (PositionTag)v
                    }),
                    Type = posTagType
                },

                DomainSceneObjectType soType => new SceneObjectValue
                {
                    Value = CoerceValue<SceneObject>(typedValue.Value, v => v switch
                    {
                        SceneObject so => so,
                        JsonElement je => JsonSerializer.Deserialize<SceneObject>(je.GetRawText(), JsonOptions)!,
                        _ => (SceneObject)v
                    }),
                    Type = soType
                },

                _ => throw new InvalidOperationException(
                    $"Unsupported ValueType: {typedValue.Type.GetType().Name}. " +
                    $"Only BooleanType, NumberType, StringType, PositionType, PositionTagType, " +
                    $"and SceneObjectType are currently supported for GraphQL property values.")
            };
        }
        catch (InvalidCastException ex)
        {
            LogTypeCastFailed(logger, ex,
                typedValue.Type.GetType().Name, typedValue.Value.GetType().Name);

            throw new InvalidOperationException(
                $"Type mismatch: TypedValue.Type is {typedValue.Type.GetType().Name} " +
                $"but Value is {typedValue.Value.GetType().Name} and could not be cast", ex);
        }
    }

    /// <summary>
    ///     Coerces a value to the expected type, handling JsonElement from deserialization.
    /// </summary>
    private static T CoerceValue<T>(object value, Func<object, T> converter)
    {
        if (value is T typed)
            return typed;

        return converter(value);
    }

    /// <summary>
    ///     Logs that a TypedValue has a null Type, indicating corrupted property data.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(LogLevel.Error, "TypedValue has null Type - this indicates corrupted property data")]
    private static partial void LogNullType(ILogger logger);

    /// <summary>
    ///     Logs that a TypedValue has a null Value for the specified type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="typeName">The name of the TypedValue's type.</param>
    [LoggerMessage(LogLevel.Error, "TypedValue has null Value for type {TypeName}")]
    private static partial void LogNullValue(ILogger logger, string typeName);

    /// <summary>
    ///     Logs that a TypedValue cast to the expected type failed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The InvalidCastException that occurred.</param>
    /// <param name="typeName">The expected type name.</param>
    /// <param name="valueType">The actual value type name.</param>
    [LoggerMessage(LogLevel.Error,
        "Failed to cast TypedValue.Value to expected type for {TypeName}. Value type: {ValueType}")]
    private static partial void LogTypeCastFailed(ILogger logger, Exception exception,
        string typeName, string valueType);
}