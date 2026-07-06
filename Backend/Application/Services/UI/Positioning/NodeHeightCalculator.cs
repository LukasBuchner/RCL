using FHOOE.Freydis.Application.Services.UI.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Implementation of node height calculator.
///     Calculates node heights based on hierarchy and children using the position Y calculator.
/// </summary>
public class NodeHeightCalculator : INodeHeightCalculator
{
    private readonly ILogger<NodeHeightCalculator> _logger;
    private readonly INodePositionYCalculator _positionYCalculator;

    /// <summary>
    ///     Initialises a new instance of <see cref="NodeHeightCalculator" />.
    /// </summary>
    /// <param name="positionYCalculator">Calculator for Y-axis positioning and height calculation.</param>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public NodeHeightCalculator(
        INodePositionYCalculator positionYCalculator,
        ILogger<NodeHeightCalculator> logger)
    {
        _positionYCalculator = positionYCalculator ?? throw new ArgumentNullException(nameof(positionYCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, double> CalculateNodeHeights(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(parentToChildrenMapping);

        var nodeHeights = new Dictionary<Guid, double>();

        _logger.LogHeightCalculationStarted(nodes.Count, parentToChildrenMapping.Count);

        foreach (var node in nodes)
            if (parentToChildrenMapping.TryGetValue(node.Id, out var children) && children.Any())
            {
                // Container node - calculate height based on children
                // CalculateContainerHeight already accounts for RouterNode dropdown height
                var containerHeight =
                    _positionYCalculator.CalculateContainerHeight(node, children, parentToChildrenMapping);

                nodeHeights[node.Id] = containerHeight;

                var containerNodeType = node.GetType().Name;
                _logger.LogContainerHeightCalculated(
                    containerNodeType, node.Id, containerHeight, children.Count, node.ParentId);
            }
            else
            {
                // Leaf node - use base height
                var height = _positionYCalculator.BaseHeight;

                var leafNodeType = node.GetType().Name;
                _logger.LogLeafNodeHeight(node.Id, leafNodeType, height);

                // Add extra height for RouterNode dropdown if branches exist
                if (node is RouterNode routerNode)
                {
                    if (routerNode.RouterTask?.Branches?.Any() == true)
                    {
                        height += _positionYCalculator.RouterDropdownHeight;
                        _logger.LogLeafRouterNodeWithBranches(
                            node.Id, routerNode.RouterTask.Branches.Count, _positionYCalculator.BaseHeight,
                            _positionYCalculator.RouterDropdownHeight, height);
                    }
                    else
                    {
                        _logger.LogLeafRouterNodeNoBranches(node.Id, _positionYCalculator.BaseHeight);
                    }
                }

                nodeHeights[node.Id] = height;
            }

        _logger.LogHeightCalculationCompleted(nodeHeights.Count);
        return nodeHeights;
    }
}