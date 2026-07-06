namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for RouterNode operations.
///     Represents router node data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the router node.</param>
/// <param name="Position">The position of the node in the UI.</param>
/// <param name="ParentId">The ID of the parent node, if any.</param>
/// <param name="Extent">The extent property of the node.</param>
/// <param name="Width">The width of the node.</param>
/// <param name="Height">The height of the node.</param>
/// <param name="Selectable">Whether the node is selectable.</param>
/// <param name="Selected">Whether the node is currently selected.</param>
/// <param name="Draggable">Whether the node is draggable.</param>
/// <param name="Dragging">Whether the node is currently being dragged.</param>
/// <param name="Hidden">Whether the node is hidden.</param>
/// <param name="RouterTask">The router task associated with this node.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record RouterNodeDto(
    Guid Id,
    NodePositionDto Position,
    Guid? ParentId,
    string? Extent,
    double? Width,
    double? Height,
    bool? Selectable,
    bool? Selected,
    bool? Draggable,
    bool? Dragging,
    bool? Hidden,
    RouterTaskDto RouterTask);