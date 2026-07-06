using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Calculates node widths based on their duration, time-to-pixel scale, and hierarchy.
///     Container nodes are expanded so that they are at least as wide as their widest
///     descendant, preventing child overflow.
/// </summary>
public interface INodeWidthCalculator
{
    /// <summary>
    ///     Calculates widths for nodes based on their task durations, ensuring that
    ///     container nodes are at least as wide as their widest child. This prevents
    ///     nested nodes (e.g. a router inside a branch task) from overflowing their
    ///     parent visually.
    /// </summary>
    /// <param name="nodes">The nodes to calculate widths for.</param>
    /// <param name="parentToChildrenMapping">
    ///     Mapping from parent node IDs to their direct children.
    ///     Used to propagate the maximum child width up to ancestor containers.
    /// </param>
    /// <returns>Dictionary mapping node IDs to their calculated widths in pixels.</returns>
    IReadOnlyDictionary<Guid, double> CalculateNodeWidths(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping);
}