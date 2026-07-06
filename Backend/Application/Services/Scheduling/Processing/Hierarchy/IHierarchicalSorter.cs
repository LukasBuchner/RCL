using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Service responsible for sorting hierarchical structures in proper dependency order.
/// </summary>
public interface IHierarchicalSorter
{
    /// <summary>
    ///     Sorts task nodes in hierarchical order so that child nodes are processed before their parents.
    ///     Uses topological sorting to ensure correct dependency order.
    ///     This overload only considers TaskNode-to-TaskNode parent relationships.
    /// </summary>
    /// <param name="taskNodes">Collection of task nodes to sort.</param>
    /// <returns>Task nodes ordered for correct hierarchical processing (children before parents).</returns>
    IReadOnlyList<TaskNode> SortTaskNodesHierarchically(IEnumerable<TaskNode> taskNodes);

    /// <summary>
    ///     Sorts task nodes in hierarchical order, using the full node list to resolve
    ///     containment boundaries through RouterNodes. This enables correct depth ordering
    ///     when TaskNodes span multiple router nesting levels (e.g., Router1 → BT1 → Router2 → BT2).
    /// </summary>
    /// <param name="taskNodes">Collection of task nodes to sort.</param>
    /// <param name="allNodes">All nodes including RouterNodes, used to resolve the full containment hierarchy.</param>
    /// <returns>Task nodes ordered for correct hierarchical processing (deepest first).</returns>
    IReadOnlyList<TaskNode> SortTaskNodesHierarchically(IEnumerable<TaskNode> taskNodes, IReadOnlyList<Node> allNodes);
}