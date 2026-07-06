using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Service responsible for processing node hierarchies.
///     Extracts common node processing logic to eliminate duplication between services.
/// </summary>
public interface INodeHierarchyProcessor
{
    /// <summary>
    ///     Processes the node hierarchy to prepare it for scheduling operations.
    ///     Handles both TaskNode and SkillExecutionNode hierarchies with their relationships.
    /// </summary>
    /// <param name="nodes">The input domain nodes to process.</param>
    /// <returns>Processed node hierarchy information ready for scheduling.</returns>
    NodeHierarchyInfo ProcessHierarchy(IReadOnlyList<Node> nodes);
}

/// <summary>
///     Result of node hierarchy processing containing organized node information and relationships.
///     Provides structured access to the node hierarchy for scheduling and positioning operations.
/// </summary>
public record NodeHierarchyInfo
{
    /// <summary>
    ///     All task nodes organized by their hierarchy level.
    /// </summary>
    public required IReadOnlyList<TaskNode> TaskNodes { get; init; }

    /// <summary>
    ///     All skill execution nodes with their parent relationships resolved.
    /// </summary>
    public required IReadOnlyList<SkillExecutionNode> SkillExecutionNodes { get; init; }

    /// <summary>
    ///     All router nodes (decision points for conditional branching).
    /// </summary>
    public required IReadOnlyList<RouterNode> RouterNodes { get; init; }

    /// <summary>
    ///     All nodes (task, skill execution, and router) for easy access.
    /// </summary>
    public IReadOnlyList<Node> AllNodes => TaskNodes
        .Concat(SkillExecutionNodes.Cast<Node>())
        .Concat(RouterNodes.Cast<Node>())
        .ToList()
        .AsReadOnly();

    /// <summary>
    ///     General hierarchy mapping for position calculations - maps parent node IDs to their children.
    ///     Key is Guid.Empty for top-level nodes.
    /// </summary>
    public required IReadOnlyDictionary<Guid, IReadOnlyList<Node>> ParentToChildrenMapping { get; init; }

    /// <summary>
    ///     Mapping of task nodes to their child skill execution nodes.
    /// </summary>
    public required IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> TaskToSkillMapping { get; init; }

    /// <summary>
    ///     Mapping of skill execution nodes to their parent task nodes.
    /// </summary>
    public required IReadOnlyDictionary<Guid, TaskNode> SkillToTaskMapping { get; init; }

    /// <summary>
    ///     Total count of nodes processed.
    /// </summary>
    public int TotalNodeCount => TaskNodes.Count + SkillExecutionNodes.Count + RouterNodes.Count;

    /// <summary>
    ///     Whether the hierarchy contains any skill execution nodes that require scheduling.
    /// </summary>
    public bool HasSkillExecutionNodes => SkillExecutionNodes.Count > 0;
}