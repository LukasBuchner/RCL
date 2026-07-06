using FHOOE.Freydis.Domain.Entities.Common;
using DomainBooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using DomainNumberType = FHOOE.Freydis.Domain.Entities.Common.NumberType;
using DomainStringType = FHOOE.Freydis.Domain.Entities.Common.StringType;
using DomainPositionType = FHOOE.Freydis.Domain.Entities.Common.PositionType;
using DomainPositionTagType = FHOOE.Freydis.Domain.Entities.Common.PositionTagType;
using DomainSceneObjectType = FHOOE.Freydis.Domain.Entities.Common.SceneObjectType;

namespace FHOOE.Freydis.GraphQLServer.Types.OutputTypes;

/// <summary>
///     GraphQL output types for property values.
///     These types wrap TypedValue contents to expose both type descriptor and actual value.
/// </summary>
/// <summary>
///     GraphQL wrapper for boolean property values.
///     Exposes both the actual boolean value and its type descriptor.
/// </summary>
public record BooleanValue
{
    [GraphQLName("boolValue")] public required bool BoolValue { get; init; }

    [GraphQLName("type")] public required DomainBooleanType Type { get; init; }
}

/// <summary>
///     GraphQL wrapper for number property values.
///     Exposes both the actual numeric value and its type descriptor.
/// </summary>
public record NumberValue
{
    [GraphQLName("numberValue")] public required double Value { get; init; }

    [GraphQLName("type")] public required DomainNumberType Type { get; init; }
}

/// <summary>
///     GraphQL wrapper for string property values.
///     Exposes both the actual string value and its type descriptor.
/// </summary>
public record StringValue
{
    [GraphQLName("stringValue")] public required string Value { get; init; }

    [GraphQLName("type")] public required DomainStringType Type { get; init; }
}

/// <summary>
///     GraphQL wrapper for position property values.
///     Exposes both the actual position value and its type descriptor.
/// </summary>
public record PositionValue
{
    [GraphQLName("positionValue")] public required Position Value { get; init; }

    [GraphQLName("type")] public required DomainPositionType Type { get; init; }
}

/// <summary>
///     GraphQL wrapper for position tag property values.
///     Exposes both the actual position tag reference and its type descriptor.
/// </summary>
public record PositionTagValue
{
    [GraphQLName("positionTagValue")] public required PositionTag Value { get; init; }

    [GraphQLName("type")] public required DomainPositionTagType Type { get; init; }
}

/// <summary>
///     GraphQL wrapper for scene object property values.
///     Exposes both the actual scene object reference and its type descriptor.
/// </summary>
public record SceneObjectValue
{
    [GraphQLName("sceneObjectValue")] public required SceneObject Value { get; init; }

    [GraphQLName("type")] public required DomainSceneObjectType Type { get; init; }
}