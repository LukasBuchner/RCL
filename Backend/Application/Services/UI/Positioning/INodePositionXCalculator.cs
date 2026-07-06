using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Interface for calculating the X position of nodes based on their relative start time.
///     Follows Single Responsibility Principle by focusing only on X-axis positioning.
/// </summary>
public interface INodePositionXCalculator
{
    /// <summary>
    ///     Gets the scale factor for converting time units to pixels.
    ///     Configured through application settings.
    /// </summary>
    double TimeToPixelScale { get; }

    /// <summary>
    ///     Calculates the X position for a node based on its relative start time.
    /// </summary>
    /// <param name="node">The node to calculate position for.</param>
    /// <param name="timingInfo">The timing information containing relative start time.</param>
    /// <param name="parentNode">Optional parent node for hierarchical positioning.</param>
    /// <returns>The calculated X position in pixels.</returns>
    double CalculateXPosition(Node node, NodeTimingInfo timingInfo, Node? parentNode = null);

    /// <summary>
    ///     Calculates X positions for multiple nodes.
    /// </summary>
    /// <param name="nodesWithTiming">Dictionary of nodes with their timing information.</param>
    /// <returns>Dictionary mapping node IDs to their calculated X positions.</returns>
    IReadOnlyDictionary<Guid, double> CalculateXPositions(
        IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)> nodesWithTiming);
}