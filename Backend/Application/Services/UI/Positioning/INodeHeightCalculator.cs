using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.UI.Positioning;

/// <summary>
///     Calculates node heights based on hierarchy and children.
///     Follows Single Responsibility Principle by focusing only on height calculation.
/// </summary>
public interface INodeHeightCalculator
{
    /// <summary>
    ///     Calculates heights for nodes based on their hierarchy and children.
    ///     Container nodes get calculated heights based on their children, while leaf nodes get base heights.
    /// </summary>
    /// <param name="nodes">The nodes to calculate heights for.</param>
    /// <param name="parentToChildrenMapping">Mapping of parent nodes to their children.</param>
    /// <returns>Dictionary mapping node IDs to their calculated heights.</returns>
    IReadOnlyDictionary<Guid, double> CalculateNodeHeights(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping);
}