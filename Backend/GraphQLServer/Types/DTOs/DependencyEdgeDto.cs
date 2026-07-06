namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for DependencyEdge operations.
///     Represents dependency edge data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the dependency edge.</param>
/// <param name="SourceId">The ID of the source node.</param>
/// <param name="TargetId">The ID of the target node.</param>
/// <param name="SourceHandle">The source handle for the edge connection.</param>
/// <param name="TargetHandle">The target handle for the edge connection.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record DependencyEdgeDto(
    Guid Id,
    Guid SourceId,
    Guid TargetId,
    string? SourceHandle,
    string? TargetHandle);