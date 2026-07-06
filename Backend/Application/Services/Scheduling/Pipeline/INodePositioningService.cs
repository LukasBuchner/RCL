using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Service responsible for applying calculated positions and heights to nodes.
/// </summary>
public interface INodePositioningService
{
    /// <summary>
    ///     Applies X/Y positions and heights to nodes based on timing information.
    /// </summary>
    /// <param name="nodes">The nodes to position.</param>
    /// <param name="detailedTimingInfo">
    ///     Optional timing information for X-axis positioning. If null, nodes are returned
    ///     unchanged.
    /// </param>
    /// <param name="parentToChildrenMapping">Parent-to-children mapping for hierarchy-based Y positioning.</param>
    /// <returns>A new list of nodes with updated positions and heights.</returns>
    /// <exception cref="ArgumentNullException">Thrown if nodes or parentToChildrenMapping is null.</exception>
    IReadOnlyList<Node> ApplyPositionsAndHeights(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, NodeTimingInfo>? detailedTimingInfo,
        IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping);
}