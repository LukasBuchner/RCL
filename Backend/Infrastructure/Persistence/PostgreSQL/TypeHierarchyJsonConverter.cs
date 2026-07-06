using System.Text.Json;
using System.Text.Json.Serialization;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using ValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     A JsonConverterFactory that handles polymorphic type hierarchies used in the domain model.
///     Supports: Node, ValueType, SelectorExpression, and Task hierarchies.
/// </summary>
public class TypeHierarchyJsonConverter : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(Node),
        typeof(ValueType),
        typeof(SelectorExpression),
        typeof(Task),
        typeof(TypedValue)
    ];

    public override bool CanConvert(Type typeToConvert)
    {
        return SupportedTypes.Contains(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Node))
            return new NodeJsonConverter();
        if (typeToConvert == typeof(ValueType))
            return new ValueTypeJsonConverter();
        if (typeToConvert == typeof(SelectorExpression))
            return new SelectorExpressionJsonConverter();
        if (typeToConvert == typeof(Task))
            return new TaskJsonConverter();
        if (typeToConvert == typeof(TypedValue))
            return new TypedValueJsonConverter();

        return null;
    }
}

/// <summary>
///     JSON converter for the Node type hierarchy (TaskNode, SkillExecutionNode, RouterNode).
/// </summary>
internal sealed class NodeJsonConverter : JsonConverter<Node>
{
    private const string Discriminator = "$type";

    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(Discriminator, out var typeProperty))
            throw new JsonException($"Missing '{Discriminator}' discriminator property for Node deserialization.");

        var typeName = typeProperty.GetString();
        var rawJson = root.GetRawText();

        // Remove the factory converter to avoid infinite recursion
        var innerOptions = CreateOptionsWithout<Node>(options);

        return typeName switch
        {
            nameof(TaskNode) => JsonSerializer.Deserialize<TaskNode>(rawJson, innerOptions),
            nameof(SkillExecutionNode) => JsonSerializer.Deserialize<SkillExecutionNode>(rawJson, innerOptions),
            nameof(RouterNode) => JsonSerializer.Deserialize<RouterNode>(rawJson, innerOptions),
            _ => throw new JsonException($"Unknown Node type: {typeName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        var innerOptions = CreateOptionsWithout<Node>(options);

        writer.WriteStartObject();
        writer.WriteString(Discriminator, value.GetType().Name);

        // Serialize the concrete type and merge properties
        var json = value switch
        {
            TaskNode tn => JsonSerializer.SerializeToElement(tn, innerOptions),
            SkillExecutionNode sen => JsonSerializer.SerializeToElement(sen, innerOptions),
            RouterNode rn => JsonSerializer.SerializeToElement(rn, innerOptions),
            _ => throw new JsonException($"Unknown Node type: {value.GetType().Name}")
        };

        foreach (var prop in json.EnumerateObject())
            prop.WriteTo(writer);

        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CreateOptionsWithout<T>(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        var convertersToRemove = newOptions.Converters
            .Where(c => c is TypeHierarchyJsonConverter || c is NodeJsonConverter)
            .ToList();
        foreach (var converter in convertersToRemove)
            newOptions.Converters.Remove(converter);
        return newOptions;
    }
}

/// <summary>
///     JSON converter for the ValueType hierarchy (BooleanType, NumberType, StringType, etc.).
/// </summary>
internal sealed class ValueTypeJsonConverter : JsonConverter<ValueType>
{
    private const string Discriminator = "$type";

    public override ValueType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(Discriminator, out var typeProperty))
            throw new JsonException($"Missing '{Discriminator}' discriminator property for ValueType deserialization.");

        var typeName = typeProperty.GetString();
        var rawJson = root.GetRawText();

        // Use original options — concrete types won't trigger this converter,
        // but nested ValueType properties (e.g. ListType.ElementType) will.
        return typeName switch
        {
            nameof(BooleanType) => JsonSerializer.Deserialize<BooleanType>(rawJson, options),
            nameof(NumberType) => JsonSerializer.Deserialize<NumberType>(rawJson, options),
            nameof(StringType) => JsonSerializer.Deserialize<StringType>(rawJson, options),
            nameof(PositionType) => JsonSerializer.Deserialize<PositionType>(rawJson, options),
            nameof(PositionTagType) => JsonSerializer.Deserialize<PositionTagType>(rawJson, options),
            nameof(SceneObjectType) => JsonSerializer.Deserialize<SceneObjectType>(rawJson, options),
            nameof(EnumType) => JsonSerializer.Deserialize<EnumType>(rawJson, options),
            nameof(ListType) => JsonSerializer.Deserialize<ListType>(rawJson, options),
            _ => throw new JsonException($"Unknown ValueType: {typeName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ValueType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(Discriminator, value.GetType().Name);

        // Use original options — concrete types won't trigger this converter,
        // but nested ValueType properties (e.g. ListType.ElementType) will.
        var json = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        foreach (var prop in json.EnumerateObject())
            prop.WriteTo(writer);

        writer.WriteEndObject();
    }
}

/// <summary>
///     JSON converter for the SelectorExpression hierarchy (SimpleVariableSelector, ExpressionSelector).
/// </summary>
internal sealed class SelectorExpressionJsonConverter : JsonConverter<SelectorExpression>
{
    private const string Discriminator = "$type";

    public override SelectorExpression? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(Discriminator, out var typeProperty))
            throw new JsonException(
                $"Missing '{Discriminator}' discriminator property for SelectorExpression deserialization.");

        var typeName = typeProperty.GetString();
        var rawJson = root.GetRawText();

        var innerOptions = CreateOptionsWithout(options);

        return typeName switch
        {
            nameof(SimpleVariableSelector) => JsonSerializer.Deserialize<SimpleVariableSelector>(rawJson,
                innerOptions),
            nameof(ExpressionSelector) => JsonSerializer.Deserialize<ExpressionSelector>(rawJson, innerOptions),
            _ => throw new JsonException($"Unknown SelectorExpression type: {typeName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, SelectorExpression value, JsonSerializerOptions options)
    {
        var innerOptions = CreateOptionsWithout(options);

        writer.WriteStartObject();
        writer.WriteString(Discriminator, value.GetType().Name);

        var json = JsonSerializer.SerializeToElement(value, value.GetType(), innerOptions);
        foreach (var prop in json.EnumerateObject())
            prop.WriteTo(writer);

        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CreateOptionsWithout(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        var convertersToRemove = newOptions.Converters
            .Where(c => c is TypeHierarchyJsonConverter || c is SelectorExpressionJsonConverter)
            .ToList();
        foreach (var converter in convertersToRemove)
            newOptions.Converters.Remove(converter);
        return newOptions;
    }
}

/// <summary>
///     JSON converter for TypedValue that properly deserializes the Value property
///     based on the Type discriminator. Without this, object? Value deserializes as JsonElement.
/// </summary>
internal sealed class TypedValueJsonConverter : JsonConverter<TypedValue>
{
    public override TypedValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Deserialize the Type field using ValueTypeJsonConverter
        if (!root.TryGetProperty("type", out var typeElement))
            throw new JsonException("Missing 'type' property for TypedValue deserialization.");

        var valueType = JsonSerializer.Deserialize<ValueType>(typeElement.GetRawText(), options);
        if (valueType is null)
            throw new JsonException("Failed to deserialize ValueType from TypedValue.");

        // Deserialize the Value field based on the resolved type
        object? value = null;
        if (root.TryGetProperty("value", out var valueElement) && valueElement.ValueKind != JsonValueKind.Null)
        {
            var valueJson = valueElement.GetRawText();
            value = valueType switch
            {
                BooleanType => valueElement.GetBoolean(),
                NumberType => valueElement.GetDouble(),
                StringType => valueElement.GetString(),
                PositionType => JsonSerializer.Deserialize<Position>(valueJson, options),
                PositionTagType => JsonSerializer.Deserialize<PositionTag>(valueJson, options),
                SceneObjectType => JsonSerializer.Deserialize<SceneObject>(valueJson, options),
                EnumType => valueElement.GetString(),
                ListType => JsonSerializer.Deserialize<List<object>>(valueJson, options),
                _ => throw new JsonException($"Unsupported ValueType for deserialization: {valueType.GetType().Name}")
            };
        }

        return new TypedValue
        {
            Type = valueType,
            Value = value
        };
    }

    public override void Write(Utf8JsonWriter writer, TypedValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        JsonSerializer.Serialize(writer, value.Type, options);

        writer.WritePropertyName("value");
        if (value.Value is null)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);

        writer.WriteEndObject();
    }
}

/// <summary>
///     JSON converter for the Task hierarchy (Task, SkillExecutionTask, RouterTask).
/// </summary>
internal sealed class TaskJsonConverter : JsonConverter<Task>
{
    private const string Discriminator = "$type";

    public override Task? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(Discriminator, out var typeProperty))
            throw new JsonException($"Missing '{Discriminator}' discriminator property for Task deserialization.");

        var typeName = typeProperty.GetString();
        var rawJson = root.GetRawText();

        var innerOptions = CreateOptionsWithout(options);

        return typeName switch
        {
            nameof(Task) => JsonSerializer.Deserialize<Task>(rawJson, innerOptions),
            nameof(SkillExecutionTask) => JsonSerializer.Deserialize<SkillExecutionTask>(rawJson, innerOptions),
            nameof(RouterTask) => JsonSerializer.Deserialize<RouterTask>(rawJson, innerOptions),
            _ => throw new JsonException($"Unknown Task type: {typeName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Task value, JsonSerializerOptions options)
    {
        var innerOptions = CreateOptionsWithout(options);

        writer.WriteStartObject();
        writer.WriteString(Discriminator, value.GetType().Name);

        var json = JsonSerializer.SerializeToElement(value, value.GetType(), innerOptions);
        foreach (var prop in json.EnumerateObject())
            prop.WriteTo(writer);

        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CreateOptionsWithout(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        var convertersToRemove = newOptions.Converters
            .Where(c => c is TypeHierarchyJsonConverter || c is TaskJsonConverter)
            .ToList();
        foreach (var converter in convertersToRemove)
            newOptions.Converters.Remove(converter);
        return newOptions;
    }
}