using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Service responsible for collecting child nodes from hierarchical structures.
/// </summary>
public interface IChildNodeCollector
{
    /// <summary>
    ///     Collects all skill execution nodes that are children of the specified parent.
    /// </summary>
    /// <param name="parentId">ID of the parent node.</param>
    /// <param name="allNodes">Collection of all nodes to search.</param>
    /// <returns>List of skill execution nodes that are children of the parent.</returns>
    IReadOnlyList<SkillExecutionNode> CollectChildSkillNodes(Guid parentId, IReadOnlyList<Node> allNodes);

    /// <summary>
    ///     Collects all task nodes that are children of the specified parent.
    /// </summary>
    /// <param name="parentId">ID of the parent node.</param>
    /// <param name="allNodes">Collection of all nodes to search.</param>
    /// <returns>List of task nodes that are children of the parent.</returns>
    IReadOnlyList<TaskNode> CollectChildTaskNodes(Guid parentId, IReadOnlyList<Node> allNodes);

    /// <summary>
    ///     Collects all router nodes that are children of the specified parent.
    /// </summary>
    /// <param name="parentId">ID of the parent node.</param>
    /// <param name="allNodes">Collection of all nodes to search.</param>
    /// <returns>List of router nodes that are children of the parent.</returns>
    IReadOnlyList<RouterNode> CollectChildRouterNodes(Guid parentId, IReadOnlyList<Node> allNodes);

    /// <summary>
    ///     Collects skill execution, task, and router nodes that are children of the specified parent.
    /// </summary>
    /// <param name="parentId">ID of the parent node.</param>
    /// <param name="allNodes">Collection of all nodes to search.</param>
    /// <returns>Tuple containing lists of child skill nodes, child task nodes, and child router nodes.</returns>
    (IReadOnlyList<SkillExecutionNode> SkillNodes, IReadOnlyList<TaskNode> TaskNodes, IReadOnlyList<RouterNode>
        RouterNodes) CollectAllChildNodes(
            Guid parentId, IReadOnlyList<Node> allNodes);
}