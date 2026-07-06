namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Represents a value with its associated type descriptor.
///     Replaces PropertyType hierarchy.
/// </summary>
public record TypedValue
{
    /// <summary>
    ///     Type descriptor for this value.
    /// </summary>
    public required ValueType Type { get; init; }

    /// <summary>
    ///     The actual value (can be null).
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    ///     Creates a typed boolean value.
    /// </summary>
    public static TypedValue Boolean(bool value)
    {
        return new TypedValue
        {
            Type = new BooleanType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed number value.
    /// </summary>
    public static TypedValue Number(double value)
    {
        return new TypedValue
        {
            Type = new NumberType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed string value.
    /// </summary>
    public static TypedValue Text(string value)
    {
        return new TypedValue
        {
            Type = new StringType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed position value.
    /// </summary>
    public static TypedValue Position(Position value)
    {
        return new TypedValue
        {
            Type = new PositionType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed position tag value.
    /// </summary>
    public static TypedValue PositionTag(PositionTag value)
    {
        return new TypedValue
        {
            Type = new PositionTagType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed scene object value.
    /// </summary>
    public static TypedValue SceneObject(SceneObject value)
    {
        return new TypedValue
        {
            Type = new SceneObjectType(),
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed enum value.
    /// </summary>
    public static TypedValue Enum(List<string> allowedValues, string value)
    {
        return new TypedValue
        {
            Type = new EnumType { AllowedValues = allowedValues },
            Value = value
        };
    }

    /// <summary>
    ///     Creates a typed list value.
    /// </summary>
    public static TypedValue List(ValueType elementType, List<object> value)
    {
        return new TypedValue
        {
            Type = new ListType { ElementType = elementType },
            Value = value
        };
    }
}