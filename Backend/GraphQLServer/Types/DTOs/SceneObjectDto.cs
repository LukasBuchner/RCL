using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for SceneObject operations.
///     Represents scene object data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the scene object.</param>
/// <param name="Name">The name of the scene object.</param>
/// <param name="Position">The position of the scene object.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record SceneObjectDto(
    Guid Id,
    string Name,
    Position Position);