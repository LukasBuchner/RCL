using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Calculates the Y position of nodes for vertical distribution.
///     Implements the Open/Closed principle - open for extension through interface, closed for modification.
///     Nodes are positioned based on timing information when available, with a graceful fallback to ID-based ordering.
/// </summary>
public class NodePositionYCalculator : INodePositionYCalculator
{
    private readonly PositioningConfiguration _positioningConfig;

    /// <summary>
    ///     Initialises a new instance of the <see cref="NodePositionYCalculator" /> class.
    /// </summary>
    /// <param name="schedulingOptions">The scheduling configuration options.</param>
    public NodePositionYCalculator(IOptions<SchedulingConfiguration> schedulingOptions)
    {
        _positioningConfig = schedulingOptions.Value.Positioning;
    }

    /// <summary>
    ///     Gets the spacing between sibling nodes.
    /// </summary>
    public double SiblingSpacing => _positioningConfig.SiblingSpacing;

    /// <summary>
    ///     Gets the vertical offset for child nodes relative to their parent.
    /// </summary>
    public double ChildVerticalOffset => _positioningConfig.BaseYOffset;

    /// <summary>
    ///     Gets the top padding inside container nodes before the first child.
    /// </summary>
    public double ContainerTopPadding => _positioningConfig.ContainerTopPadding;

    /// <summary>
    ///     Gets the bottom padding inside container nodes after the last child.
    /// </summary>
    public double ContainerBottomPadding => _positioningConfig.ContainerBottomPadding;

    /// <summary>
    ///     Gets the base height for leaf nodes (nodes without children).
    /// </summary>
    public double BaseHeight => _positioningConfig.BaseHeight;

    /// <summary>
    ///     Gets the additional height required for RouterNode dropdown selector.
    ///     Only applied when the RouterNode has branches.
    /// </summary>
    public double RouterDropdownHeight => _positioningConfig.RouterDropdownHeight;

    /// <summary>
    ///     Calculates Y positions for multiple nodes with hierarchical consideration, time-based sorting, and cascading
    ///     container heights.
    ///     Root nodes are sorted by AbsoluteStartTime, sibling nodes by RelativeStartTime.
    ///     Falls back to ID-based sorting when timing information is unavailable.
    /// </summary>
    /// <param name="nodes">All nodes to calculate positions for.</param>
    /// <param name="nodeHierarchy">Dictionary mapping parent node IDs to their children.</param>
    /// <param name="timingInfo">Optional timing information for time-based sorting. When null, it falls back to ID-based sorting.</param>
    /// <returns>Dictionary mapping node IDs to their calculated Y positions.</returns>
    public IReadOnlyDictionary<Guid, double> CalculateYPositions(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> nodeHierarchy,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo = null)
    {
        var yPositions = new Dictionary<Guid, double>();

        // Get root nodes by detecting nodes that have no parent in the node list
        var rootNodesUnsorted = nodes.Where(n => n.ParentId == null || nodes.All(parent => parent.Id != n.ParentId))
            .ToList();

        // Sort root nodes by timing information
        var rootNodes = SortRootNodes(rootNodesUnsorted, timingInfo);

        // Position root nodes with cascading container heights
        var currentY = ChildVerticalOffset; // Start at base offset

        foreach (var rootNode in rootNodes)
        {
            yPositions[rootNode.Id] = currentY;

            // Update the node's position for child calculations
            rootNode.Position.Y = currentY;

            // Position all children of this root node
            PositionChildrenRecursively(rootNode, nodeHierarchy, yPositions, timingInfo);

            // Calculate this container's height to determine where the next sibling should be placed
            if (nodeHierarchy.TryGetValue(rootNode.Id, out var children) && children.Count > 0)
            {
                var containerHeight = CalculateContainerHeight(rootNode, children, nodeHierarchy);
                currentY += containerHeight + SiblingSpacing; // Add spacing between root containers
            }
            else
            {
                // For nodes without children, add node height (considers RouterNode dropdown) plus sibling spacing
                currentY += GetNodeHeight(rootNode) + SiblingSpacing;
            }
        }

        return yPositions;
    }

    /// <summary>
    ///     Calculates the height of a container node based on its children.
    /// </summary>
    public double CalculateContainerHeight(Node containerNode, IReadOnlyList<Node> children)
    {
        if (children.Count == 0)
            // Empty container has a minimum height of top padding plus bottom padding
            return ContainerTopPadding + ContainerBottomPadding;

        // For React Flow: calculate the maximum Y extent needed to contain all children
        // Simulate the relative positioning to find the furthest child (simple version without a hierarchy)
        var childY = ContainerTopPadding; // Start position for children
        if (containerNode is RouterNode { RouterTask.Branches.Count: > 0 })
            childY += RouterDropdownHeight; // Account for dropdown at top of RouterNode

        double maxChildExtent = 0;

        foreach (var child in children)
        {
            // Use GetNodeHeight to account for node-specific heights (e.g. RouterNode dropdown)
            var childHeight = GetNodeHeight(child);

            // Calculate the bottom extent of this child
            var childBottomExtent = childY + childHeight;
            maxChildExtent = Math.Max(maxChildExtent, childBottomExtent);

            // Move to the next sibling position
            childY += childHeight + SiblingSpacing;
        }

        // Container height = furthest child extent plus bottom padding
        return maxChildExtent + ContainerBottomPadding;
    }

    /// <summary>
    ///     Calculates the height of a container node based on its children with hierarchy support.
    ///     For React Flow compatibility, calculates the height needed to contain all children at their relative positions.
    /// </summary>
    public double CalculateContainerHeight(Node containerNode, IReadOnlyList<Node> children,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> nodeHierarchy)
    {
        if (children.Count == 0) return ContainerTopPadding + ContainerBottomPadding;

        // For React Flow: calculate the maximum Y extent needed to contain all children
        // Simulate the relative positioning to find the furthest child
        var childY = ContainerTopPadding; // Start position for children
        if (containerNode is RouterNode { RouterTask.Branches.Count: > 0 })
            childY += RouterDropdownHeight; // Account for dropdown at top of RouterNode

        double maxChildExtent = 0;

        foreach (var child in children)
        {
            // Use GetNodeHeight for leaf nodes to account for node-specific heights (e.g. RouterNode dropdown)
            var childHeight = GetNodeHeight(child);

            // If this child is also a container, calculate its height recursively
            if (nodeHierarchy.TryGetValue(child.Id, out var grandChildren) && grandChildren.Count > 0)
                // For container children, use their calculated container height
                childHeight = CalculateContainerHeight(child, grandChildren, nodeHierarchy);

            // Calculate the bottom extent of this child (its relative Y position plus its height)
            var childBottomExtent = childY + childHeight;
            maxChildExtent = Math.Max(maxChildExtent, childBottomExtent);

            // Move to the next sibling position (same logic as PositionChildrenRecursively)
            childY += childHeight + SiblingSpacing;
        }

        // Container height = furthest child extent plus bottom padding
        return maxChildExtent + ContainerBottomPadding;
    }

    /// <summary>
    ///     Calculates the height of a node based on its type.
    ///     RouterNode receives additional height for its dropdown selector only when branches
    ///     are configured; a RouterNode without branches has no dropdown and uses the base height.
    /// </summary>
    /// <param name="node">The node to calculate height for.</param>
    /// <returns>The height of the node in pixels.</returns>
    private double GetNodeHeight(Node node)
    {
        return node is RouterNode { RouterTask.Branches.Count: > 0 }
            ? BaseHeight + RouterDropdownHeight
            : BaseHeight;
    }

    /// <summary>
    ///     Recursively positions children of a given parent node with time-based sorting.
    ///     For React Flow compatibility, children are positioned relative to their parent (parent = origin).
    ///     Children are sorted by RelativeStartTime when timing information is available.
    /// </summary>
    /// <param name="parentNode">The parent node whose children are being positioned.</param>
    /// <param name="nodeHierarchy">Dictionary mapping parent node IDs to their children.</param>
    /// <param name="yPositions">Dictionary to store calculated Y positions.</param>
    /// <param name="timingInfo">Optional timing information for time-based sorting.</param>
    private void PositionChildrenRecursively(
        Node parentNode,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> nodeHierarchy,
        Dictionary<Guid, double> yPositions,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo)
    {
        if (!nodeHierarchy.TryGetValue(parentNode.Id, out var childrenUnsorted) || childrenUnsorted.Count == 0)
            return;

        // Sort children by timing information
        var children = SortChildren(childrenUnsorted, timingInfo);

        // For React Flow: children start at a relative position ContainerTopPadding from parent origin (0,0)
        // For RouterNodes with branches, add the dropdown height to account for the selector UI at the top
        var childY = ContainerTopPadding;
        if (parentNode is RouterNode { RouterTask.Branches.Count: > 0 })
            childY += RouterDropdownHeight;

        foreach (var child in children)
        {
            // Store relative position (relative to parent, not absolute)
            yPositions[child.Id] = childY;

            // Update the child's position for further calculations
            child.Position.Y = childY;

            // If this child has its own children, position them recursively
            PositionChildrenRecursively(child, nodeHierarchy, yPositions, timingInfo);

            // Determine height for next sibling (still relative to parent)
            if (nodeHierarchy.TryGetValue(child.Id, out var grandChildren) && grandChildren.Count > 0)
            {
                // Child is a container - use its calculated height plus sibling spacing
                var containerHeight = CalculateContainerHeight(child, grandChildren, nodeHierarchy);
                childY += containerHeight + SiblingSpacing;
            }
            else
            {
                // Child is a leaf node - use node height (considers RouterNode dropdown) plus sibling spacing
                childY += GetNodeHeight(child) + SiblingSpacing;
            }
        }
    }

    /// <summary>
    ///     Sorts root nodes by AbsoluteStartTime if timing information is available.
    ///     Falls back to sorting by ID for deterministic ordering when timing is unavailable.
    /// </summary>
    /// <param name="rootNodes">The root nodes to sort.</param>
    /// <param name="timingInfo">Optional timing information for time-based sorting.</param>
    /// <returns>Sorted list of root nodes.</returns>
    private static List<Node> SortRootNodes(
        IReadOnlyList<Node> rootNodes,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo)
    {
        if (timingInfo == null || timingInfo.Count == 0) return rootNodes.OrderBy(n => n.Id).ToList();

        return rootNodes
            .OrderBy(n => timingInfo.TryGetValue(n.Id, out var timing) ? timing.AbsoluteStartTime : double.MaxValue)
            .ThenBy(n => n.Id)
            .ToList();
    }

    /// <summary>
    ///     Sorts sibling nodes by RelativeStartTime if timing information is available.
    ///     Falls back to sorting by ID for deterministic ordering when timing is unavailable.
    /// </summary>
    /// <param name="children">The sibling nodes to sort.</param>
    /// <param name="timingInfo">Optional timing information for time-based sorting.</param>
    /// <returns>Sorted list of sibling nodes.</returns>
    private static List<Node> SortChildren(
        IReadOnlyList<Node> children,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? timingInfo)
    {
        if (timingInfo == null || timingInfo.Count == 0) return children.OrderBy(n => n.Id).ToList();

        return children
            .OrderBy(n => timingInfo.TryGetValue(n.Id, out var timing) ? timing.RelativeStartTime : double.MaxValue)
            .ThenBy(n => n.Id)
            .ToList();
    }
}