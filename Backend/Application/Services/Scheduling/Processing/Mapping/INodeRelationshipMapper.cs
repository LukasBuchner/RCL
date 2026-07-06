using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Service responsible for building node relationship mappings in the hierarchy.
/// </summary>
public interface INodeRelationshipMapper
{
    /// <summary>
    ///     Builds mapping from task nodes to their child skill execution nodes.
    /// </summary>
    /// <param name="taskNodes">Collection of task nodes to process.</param>
    /// <param name="skillExecutionNodes">Collection of skill execution nodes to map.</param>
    /// <returns>Dictionary mapping task node IDs to their child skill execution nodes.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> BuildTaskToSkillMapping(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes);

    /// <summary>
    ///     Builds reverse mapping from skill execution nodes to their parent task nodes.
    /// </summary>
    /// <param name="taskNodes">Collection of task nodes for lookup.</param>
    /// <param name="skillExecutionNodes">Collection of skill execution nodes to map.</param>
    /// <returns>Dictionary mapping skill execution node IDs to their parent task nodes.</returns>
    IReadOnlyDictionary<Guid, TaskNode> BuildSkillToTaskMapping(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes);

    /// <summary>
    ///     Builds general parent-to-children hierarchy mapping for position calculations.
    ///     Root nodes (with null ParentId) are stored with Guid.Empty as the key.
    /// </summary>
    /// <param name="nodes">All nodes to process.</param>
    /// <returns>Dictionary mapping parent IDs to their children nodes.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<Node>> BuildParentToChildrenMapping(IReadOnlyList<Node> nodes);
}