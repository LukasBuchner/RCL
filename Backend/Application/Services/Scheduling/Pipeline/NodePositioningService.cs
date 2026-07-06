using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Service responsible for applying calculated positions, widths, and heights to nodes.
/// </summary>
public class NodePositioningService(
    INodePositionXCalculator positionXCalculator,
    INodePositionYCalculator positionYCalculator,
    INodeHeightCalculator nodeHeightCalculator,
    INodeWidthCalculator nodeWidthCalculator,
    ILogger<NodePositioningService> logger)
    : INodePositioningService
{
    private readonly ILogger<NodePositioningService>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly INodeHeightCalculator _nodeHeightCalculator =
        nodeHeightCalculator ?? throw new ArgumentNullException(nameof(nodeHeightCalculator));

    private readonly INodeWidthCalculator _nodeWidthCalculator =
        nodeWidthCalculator ?? throw new ArgumentNullException(nameof(nodeWidthCalculator));

    private readonly INodePositionXCalculator _positionXCalculator =
        positionXCalculator ?? throw new ArgumentNullException(nameof(positionXCalculator));

    private readonly INodePositionYCalculator _positionYCalculator =
        positionYCalculator ?? throw new ArgumentNullException(nameof(positionYCalculator));

    /// <summary>
    ///     Applies X/Y positions, widths, and heights to nodes based on timing information.
    /// </summary>
    /// <param name="nodes">The nodes to position.</param>
    /// <param name="detailedTimingInfo">
    ///     Optional timing information for X-axis positioning. If null, nodes are returned
    ///     unchanged.
    /// </param>
    /// <param name="parentToChildrenMapping">Parent-to-children mapping for hierarchy-based Y positioning.</param>
    /// <returns>A new list of nodes with updated positions, widths, and heights.</returns>
    /// <exception cref="ArgumentNullException">Thrown if nodes or parentToChildrenMapping is null.</exception>
    public IReadOnlyList<Node> ApplyPositionsAndHeights(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? detailedTimingInfo,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(parentToChildrenMapping);

        // If no timing info provided, return nodes unchanged
        if (detailedTimingInfo == null)
        {
            _logger.LogNoTimingInformationProvided();
            return nodes;
        }

        // Create dictionary of nodes with timing for X positioning
        var nodesWithTiming = nodes
            .Where(n => detailedTimingInfo.ContainsKey(n.Id))
            .ToDictionary(
                n => n.Id,
                n => (Node: n, Timing: detailedTimingInfo[n.Id]));

        _logger.LogPositionCalculationStarted(nodesWithTiming.Count);

        // Calculate X positions based on timing
        var xPositions = _positionXCalculator.CalculateXPositions(nodesWithTiming);

        // Calculate Y positions based on hierarchy and timing
        var yPositions = _positionYCalculator.CalculateYPositions(nodes, parentToChildrenMapping, detailedTimingInfo);

        // Calculate widths based on durations, expanding containers to fit children
        var widths = _nodeWidthCalculator.CalculateNodeWidths(nodes, parentToChildrenMapping);

        // Calculate heights based on hierarchy
        var heights = _nodeHeightCalculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        _logger.LogPositionDimensionCounts(xPositions.Count, yPositions.Count, widths.Count, heights.Count);

        // Apply positions, widths, and heights to nodes
        var updatedNodes = nodes.Select(node =>
        {
            var updatedNode = node;

            // Apply X position if available
            if (xPositions.TryGetValue(node.Id, out var xPos))
            {
                var oldPos = updatedNode.Position;
                var newPosition = updatedNode.Position with { X = xPos };

                var xNodeType = node.GetType().Name;
                _logger.LogNodeXPositionUpdate(node.Id, xNodeType, node.ParentId, oldPos.X, xPos, oldPos.Y);

                updatedNode = updatedNode switch
                {
                    TaskNode tn => tn with { Position = newPosition },
                    SkillExecutionNode sen => sen with { Position = newPosition },
                    RouterNode rn => rn with { Position = newPosition },
                    _ => updatedNode
                };
            }

            // Apply Y position if available
            if (yPositions.TryGetValue(node.Id, out var yPos))
            {
                var newPosition = updatedNode.Position with { Y = yPos };
                updatedNode = updatedNode switch
                {
                    TaskNode tn => tn with { Position = newPosition },
                    SkillExecutionNode sen => sen with { Position = newPosition },
                    RouterNode rn => rn with { Position = newPosition },
                    _ => updatedNode
                };
            }

            // Apply width if available
            if (widths.TryGetValue(node.Id, out var width))
            {
                var wNodeType = node.GetType().Name;
                _logger.LogNodeWidthCalculated(node.Id, wNodeType, node.ParentId, width);

                updatedNode = updatedNode switch
                {
                    TaskNode tn => tn with { Width = width },
                    SkillExecutionNode sen => sen with { Width = width },
                    RouterNode rn => rn with { Width = width },
                    _ => updatedNode
                };
            }

            // Apply height if available
            if (heights.TryGetValue(node.Id, out var height))
            {
                var hNodeType = node.GetType().Name;
                _logger.LogNodeHeightCalculated(node.Id, hNodeType, node.ParentId, height);

                updatedNode = updatedNode switch
                {
                    TaskNode tn => tn with { Height = height },
                    SkillExecutionNode sen => sen with { Height = height },
                    RouterNode rn => rn with { Height = height },
                    _ => updatedNode
                };
            }

            // Set extent to "parent" for child nodes to constrain them within parent bounds
            if (node.ParentId.HasValue && updatedNode.Extent != "parent")
                updatedNode = updatedNode switch
                {
                    TaskNode tn => tn with { Extent = "parent" },
                    SkillExecutionNode sen => sen with { Extent = "parent" },
                    RouterNode rn => rn with { Extent = "parent" },
                    _ => updatedNode
                };

            return updatedNode;
        }).ToList();

        _logger.LogPositioningCompleted(updatedNodes.Count);
        return updatedNodes;
    }
}