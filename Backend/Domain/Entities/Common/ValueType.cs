using System.Text.Json.Serialization;

namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Base type descriptor for all value types in the system.
///     Replaces both PropertyType hierarchy and VariableType enum.
/// </summary>
public abstract record ValueType
{
    /// <summary>
    ///     CLR type that this value type represents.
    /// </summary>
    [JsonIgnore]
    public abstract Type ClrType { get; }

    /// <summary>
    ///     Human-readable name for this type.
    /// </summary>
    public abstract string TypeName { get; }
}

/// <summary>
///     Boolean value type.
/// </summary>
public record BooleanType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(bool);
    public override string TypeName => "Boolean";
}

/// <summary>
///     Numeric value type (double precision).
/// </summary>
public record NumberType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(double);
    public override string TypeName => "Number";
}

/// <summary>
///     String value type.
/// </summary>
public record StringType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(string);
    public override string TypeName => "String";
}

/// <summary>
///     3D position value type.
/// </summary>
public record PositionType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(Position);
    public override string TypeName => "Position";
}

/// <summary>
///     Position tag reference type.
/// </summary>
public record PositionTagType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(PositionTag);
    public override string TypeName => "PositionTag";
}

/// <summary>
///     Scene object reference type.
/// </summary>
public record SceneObjectType : ValueType
{
    [JsonIgnore] public override Type ClrType => typeof(SceneObject);
    public override string TypeName => "SceneObject";
}

/// <summary>
///     Enumeration type with predefined allowed values.
/// </summary>
public record EnumType : ValueType
{
    /// <summary>
    ///     List of allowed string values for this enum.
    /// </summary>
    public required List<string> AllowedValues { get; init; }

    [JsonIgnore] public override Type ClrType => typeof(string);
    public override string TypeName => "Enum";
}

/// <summary>
///     List type containing elements of a specific type.
/// </summary>
public record ListType : ValueType
{
    /// <summary>
    ///     Type of elements in the list.
    /// </summary>
    public required ValueType ElementType { get; init; }

    [JsonIgnore] public override Type ClrType => typeof(List<object>);
    public override string TypeName => $"List<{ElementType.TypeName}>";
}