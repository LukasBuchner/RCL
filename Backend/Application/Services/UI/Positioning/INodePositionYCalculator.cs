using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Interface for calculating the Y position of nodes for vertical distribution.
///     Follows Single Responsibility Principle by focusing only on Y-axis positioning.
///     Nodes are positioned based on timing information when available, with graceful fallback to ID-based ordering.
/// </summary>
public interface INodePositionYCalculator
{
    /// <summary>
    ///     Gets the vertical offset for child nodes relative to their parent.
    ///     Configured through application settings.
    /// </summary>
    double ChildVerticalOffset { get; }

    /// <summary>
    ///     Gets the top padding inside container nodes before the first child.
    ///     Configured through application settings.
    /// </summary>
    double ContainerTopPadding { get; }

    /// <summary>
    ///     Gets the bottom padding inside container nodes after the last child.
    ///     Configured through application settings.
    /// </summary>
    double ContainerBottomPadding { get; }

    /// <summary>
    ///     Gets the base height for leaf nodes (nodes without children).
    ///     Configured through application settings.
    /// </summary>
    double BaseHeight { get; }

    /// <summary>
    ///     Gets the additional height required for RouterNode dropdown selector.
    ///     Only applied when the RouterNode has branches.
    ///     Configured through application settings.
    /// </summary>
    double RouterDropdownHeight { get; }

    /// <summary>
    ///     Calculates Y positions for multiple nodes with hierarchical consideration and time-based sorting.
    ///     Root nodes are sorted by AbsoluteStartTime, sibling nodes by RelativeStartTime.
    ///     Falls back to ID-based sorting when timing information is unavailable.
    /// </summary>
    /// <param name="nodes">All nodes to calculate positions for.</param>
    /// <param name="nodeHierarchy">Dictionary mapping parent node IDs to their children.</param>
    /// <param name="timingInfo">Optional timing information for time-based sorting. When null, falls back to ID-based sorting.</param>
    /// <returns>Dictionary mapping node IDs to their calculated Y positions.</returns>
    IReadOnlyDictionary<Guid, double> CalculateYPositions(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> nodeHierarchy,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo = null);

    /// <summary>
    ///     Calculates the height of a container node based on its children.
    /// </summary>
    /// <param name="containerNode">The container node.</param>
    /// <param name="children">Direct children of the container.</param>
    /// <returns>The calculated height including padding.</returns>
    double CalculateContainerHeight(Node containerNode, IReadOnlyList<Node> children);

    /// <summary>
    ///     Calculates the height of a container node based on its children with hierarchy support.
    /// </summary>
    /// <param name="containerNode">The container node.</param>
    /// <param name="children">Direct children of the container.</param>
    /// <param name="nodeHierarchy">The full node hierarchy for recursive calculations.</param>
    /// <returns>The calculated height including padding.</returns>
    double CalculateContainerHeight(Node containerNode, IReadOnlyList<Node> children,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> nodeHierarchy);
}