using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Calculates the X position of nodes based on their relative start time.
///     Implements the Open/Closed principle - open for extension through interface, closed for modification.
/// </summary>
public class NodePositionXCalculator : INodePositionXCalculator
{
    private readonly PositioningConfiguration _positioningConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodePositionXCalculator" /> class.
    /// </summary>
    /// <param name="schedulingOptions">The scheduling configuration options.</param>
    public NodePositionXCalculator(IOptions<SchedulingConfiguration> schedulingOptions)
    {
        _positioningConfig = schedulingOptions?.Value?.Positioning ?? new PositioningConfiguration();
    }

    /// <summary>
    ///     Gets the scale factor for converting time units to pixels.
    ///     Configured through application settings.
    /// </summary>
    public double TimeToPixelScale => _positioningConfig.TimeToPixelScale;

    /// <summary>
    ///     Calculates the X position for a node based on its relative start time.
    /// </summary>
    public double CalculateXPosition(Node node, NodeTimingInfo timingInfo, Node? parentNode = null)
    {
        // X position is based on the relative start time
        // If the node has a parent, the position is relative to the parent's timeline
        // Otherwise, it's relative to the procedure start (0)
        return timingInfo.RelativeStartTime * TimeToPixelScale;
    }

    /// <summary>
    ///     Calculates X positions for multiple nodes.
    /// </summary>
    public IReadOnlyDictionary<Guid, double> CalculateXPositions(
        IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)> nodesWithTiming)
    {
        var xPositions = new Dictionary<Guid, double>();

        // Build parent lookup for hierarchical positioning

        foreach (var (nodeId, (node, timing)) in nodesWithTiming)
        {
            // Find parent node if exists
            Node? parentNode = null;
            if (node.ParentId.HasValue && nodesWithTiming.TryGetValue(node.ParentId.Value, out var parentInfo))
                parentNode = parentInfo.Node;

            xPositions[nodeId] = CalculateXPosition(node, timing, parentNode);
        }

        return xPositions;
    }
}