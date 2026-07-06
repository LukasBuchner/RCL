using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Navigates the node hierarchy within router branches.
///     Provides traversal methods for finding executable nodes, ancestor routers,
///     and checking branch membership — extracted from <see cref="ExecutionTriggerService" />
///     to isolate hierarchy traversal logic from execution orchestration.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - NodeHierarchy.lean — ancestor traversal, closest ancestor router, branch subtree membership
///     - RouterBranchConsistency.lean — IsSelectedBranch consistency with stored selections
/// </remarks>
public interface IRouterBranchNavigator
{
    /// <summary>
    ///     Initializes the navigator with the node collections for the current execution.
    /// </summary>
    /// <param name="allNodesById">All nodes in the procedure, keyed by ID.</param>
    /// <param name="skillNodes">Skill execution nodes, keyed by ID.</param>
    /// <param name="routerNodes">Router nodes, keyed by ID.</param>
    void Initialize(
        IReadOnlyDictionary<Guid, Node> allNodesById,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes);

    /// <summary>
    ///     Finds executable nodes that are direct children of the given branch, stopping at
    ///     nested router boundaries. When a nested <see cref="RouterNode" /> is encountered,
    ///     its ID is included in the result but its children are <b>not</b> traversed — the
    ///     nested router manages its own subtree and publishes its own Finish event.
    /// </summary>
    /// <param name="branchTargetId">The ID of the node that is the target of the selected branch.</param>
    /// <returns>
    ///     List of executable node IDs (skills and nested routers) whose Finish events
    ///     the parent router must observe before publishing its own Finish.
    /// </returns>
    List<Guid> FindDirectExecutableNodesInBranch(Guid branchTargetId);

    /// <summary>
    ///     Finds all descendant executable nodes (skills and nested routers) in the given
    ///     branch, traversing through all levels including nested router subtrees.
    ///     Used for NotSelected publishing: every executable node in a non-selected branch
    ///     must receive a NotSelected event so the state manager marks it as terminal.
    /// </summary>
    /// <param name="branchTargetId">The ID of the node that is the root of the non-selected branch.</param>
    /// <returns>List of all executable node IDs (skills and nested routers) in the branch subtree.</returns>
    List<Guid> FindAllDescendantExecutableNodes(Guid branchTargetId);

    /// <summary>
    ///     Finds the closest ancestor <see cref="RouterNode" /> for a node by traversing
    ///     the parent hierarchy. Returns null if the node is not inside a router.
    /// </summary>
    /// <param name="nodeId">The node whose ancestor router to find.</param>
    /// <returns>The closest ancestor router, or null if none exists.</returns>
    RouterNode? FindAncestorRouter(Guid nodeId);

    /// <summary>
    ///     Checks if a node is the selected target or is a descendant (child/grandchild)
    ///     of the selected target by traversing up the parent chain.
    /// </summary>
    /// <param name="nodeId">The node to check.</param>
    /// <param name="selectedTargetId">The ID of the selected branch target.</param>
    /// <returns>True if the node is the target or a descendant of it.</returns>
    bool IsNodeInSelectedBranch(Guid nodeId, Guid selectedTargetId);
}