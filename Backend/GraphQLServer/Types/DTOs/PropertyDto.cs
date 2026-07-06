using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for TypedProperty operations.
///     Represents property data in a format suitable for application layer processing.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Value">The typed value of the property.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record PropertyDto(
    string Name,
    TypedValue Value);