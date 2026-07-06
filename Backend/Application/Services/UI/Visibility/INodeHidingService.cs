using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.UI.Visibility;

/// <summary>
/// Marks nodes as hidden in the UI when they are excluded from active execution paths.
/// </summary>
public interface INodeHidingService
{
    /// <summary>
    /// Marks specified nodes as hidden and all other nodes as visible.
    /// </summary>
    /// <param name="allNodes">All nodes in the procedure</param>
    /// <param name="nodesToHide">IDs of nodes that should be hidden</param>
    /// <returns>Updated list of nodes with Hidden property set correctly</returns>
    Task<IReadOnlyList<Node>> ApplyHiddenStateAsync(
        IReadOnlyList<Node> allNodes,
        IReadOnlyCollection<Guid> nodesToHide);
}