using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.UI.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Implementation of node width calculator.
///     Calculates node widths by scaling task durations using the TimeToPixelScale configuration.
///     Container nodes are expanded so that they are at least as wide as their widest
///     descendant, preventing child overflow in nested hierarchies.
/// </summary>
public class NodeWidthCalculator : INodeWidthCalculator
{
    private readonly ILogger<NodeWidthCalculator> _logger;
    private readonly double _timeToPixelScale;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeWidthCalculator" />.
    /// </summary>
    /// <param name="schedulingOptions">Configuration options for scheduling and positioning.</param>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public NodeWidthCalculator(
        IOptions<SchedulingConfiguration> schedulingOptions,
        ILogger<NodeWidthCalculator> logger)
    {
        ArgumentNullException.ThrowIfNull(schedulingOptions);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeToPixelScale = schedulingOptions.Value.Positioning.TimeToPixelScale;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, double> CalculateNodeWidths(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(parentToChildrenMapping);

        var nodeWidths = new Dictionary<Guid, double>();

        _logger.LogWidthCalculationStarted(nodes.Count, _timeToPixelScale);

        // First pass: calculate duration-based widths for every node
        foreach (var node in nodes)
        {
            var duration = GetNodeDuration(node);
            var width = duration * _timeToPixelScale;
            nodeWidths[node.Id] = width;

            var nodeTypeName = node.GetType().Name;
            _logger.LogNodeWidthCalculated(node.Id, nodeTypeName, duration, width);
        }

        // Second pass: propagate maximum child width upward so every container
        // is at least as wide as its widest descendant.
        foreach (var node in nodes)
        {
            if (!parentToChildrenMapping.ContainsKey(node.Id))
                continue; // leaf node — nothing to propagate

            var requiredWidth = CalculateRequiredContainerWidth(node.Id, nodeWidths, parentToChildrenMapping);
            if (!(requiredWidth > nodeWidths[node.Id])) continue;
            var containerTypeName = node.GetType().Name;
            _logger.LogContainerWidthExpanded(node.Id, containerTypeName, nodeWidths[node.Id], requiredWidth);
            nodeWidths[node.Id] = requiredWidth;
        }

        _logger.LogWidthCalculationCompleted(nodeWidths.Count);
        return nodeWidths;
    }

    /// <summary>
    ///     Recursively calculates the minimum width a container node needs so that
    ///     no descendant overflows it. Returns the maximum width found among the node
    ///     itself and all its descendants.
    /// </summary>
    /// <param name="nodeId">The node ID to calculate the required width for.</param>
    /// <param name="nodeWidths">Current duration-based widths for all nodes.</param>
    /// <param name="parentToChildrenMapping">Parent-to-children mapping for hierarchy traversal.</param>
    /// <returns>The minimum width required for the container to contain all descendants.</returns>
    private static double CalculateRequiredContainerWidth(
        Guid nodeId,
        Dictionary<Guid, double> nodeWidths,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        if (!nodeWidths.TryGetValue(nodeId, out var ownWidth))
            return 0.0;

        var maxWidth = ownWidth;

        if (!parentToChildrenMapping.TryGetValue(nodeId, out var children))
            return maxWidth;

        return children.Select(child => CalculateRequiredContainerWidth(child.Id, nodeWidths, parentToChildrenMapping))
            .Prepend(maxWidth).Max();
    }

    /// <summary>
    ///     Extracts the duration from a node based on its type.
    /// </summary>
    /// <param name="node">The node to get duration from.</param>
    /// <returns>The duration of the node's task.</returns>
    private static double GetNodeDuration(Node node)
    {
        return node switch
        {
            TaskNode taskNode => taskNode.Task.Duration,
            SkillExecutionNode skillNode => skillNode.SkillExecutionTask.Duration,
            RouterNode routerNode => routerNode.RouterTask.Duration,
            _ => 0.0
        };
    }
}