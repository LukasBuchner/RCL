namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for NodePosition operations.
///     Represents node position data in a format suitable for application layer processing.
/// </summary>
/// <param name="X">The X coordinate of the node position.</param>
/// <param name="Y">The Y coordinate of the node position.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record NodePositionDto(
    double X,
    double Y);