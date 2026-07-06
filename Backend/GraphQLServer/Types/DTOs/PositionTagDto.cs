using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for PositionTag operations.
///     Represents position tag data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the position tag.</param>
/// <param name="Tag">The tag name of the position tag.</param>
/// <param name="Position">The position coordinates.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record PositionTagDto(
    Guid Id,
    string Tag,
    Position Position);