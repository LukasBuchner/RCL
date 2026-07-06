using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <inheritdoc />
public sealed class RouterBranchNavigator : IRouterBranchNavigator
{
    private IReadOnlyDictionary<Guid, Node>? _allNodesById;
    private IReadOnlyDictionary<Guid, RouterNode>? _routerNodes;
    private IReadOnlyDictionary<Guid, SkillExecutionNode>? _skillNodes;

    /// <inheritdoc />
    public void Initialize(
        IReadOnlyDictionary<Guid, Node> allNodesById,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes)
    {
        _allNodesById = allNodesById ?? throw new ArgumentNullException(nameof(allNodesById));
        _skillNodes = skillNodes ?? throw new ArgumentNullException(nameof(skillNodes));
        _routerNodes = routerNodes ?? throw new ArgumentNullException(nameof(routerNodes));
    }

    /// <inheritdoc />
    public List<Guid> FindDirectExecutableNodesInBranch(Guid branchTargetId)
    {
        EnsureInitialized();

        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(branchTargetId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            if (_skillNodes!.ContainsKey(currentId))
            {
                result.Add(currentId);
                continue; // Skills have no children
            }

            // Nested router — include it (wait for Router.Finish) but do NOT traverse its
            // children, since the nested router manages its own subtree.
            // IMPORTANT: only treat DESCENDANT routers as wait-targets, not the branch target
            // itself. If the selected target happens to be a router (external sequential router),
            // just traverse its children (which are typically none for external routers).
            if (_routerNodes!.ContainsKey(currentId) && currentId != branchTargetId)
            {
                result.Add(currentId);
                continue;
            }

            // TaskNode, branch target, or the branchTargetId itself — traverse children
            foreach (var (id, node) in _allNodesById!)
                if (node.ParentId == currentId)
                    queue.Enqueue(id);
        }

        return result;
    }

    /// <inheritdoc />
    public List<Guid> FindAllDescendantExecutableNodes(Guid branchTargetId)
    {
        EnsureInitialized();

        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(branchTargetId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            if (_skillNodes!.ContainsKey(currentId))
                result.Add(currentId);
            else if (_routerNodes!.ContainsKey(currentId))
                result.Add(currentId);

            // Traverse ALL children — including nested router subtrees
            foreach (var (id, node) in _allNodesById!)
                if (node.ParentId == currentId)
                    queue.Enqueue(id);
        }

        return result;
    }

    /// <inheritdoc />
    public RouterNode? FindAncestorRouter(Guid nodeId)
    {
        EnsureInitialized();
        if (!_allNodesById!.TryGetValue(nodeId, out var node)) return null;

        var currentParentId = node.ParentId;
        while (currentParentId.HasValue)
        {
            if (_routerNodes!.TryGetValue(currentParentId.Value, out var router))
                return router;

            if (_allNodesById.TryGetValue(currentParentId.Value, out var parentNode))
                currentParentId = parentNode.ParentId;
            else
                break;
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsNodeInSelectedBranch(Guid nodeId, Guid selectedTargetId)
    {
        if (nodeId == selectedTargetId) return true;

        EnsureInitialized();
        if (!_allNodesById!.TryGetValue(nodeId, out var node)) return false;

        var currentParentId = node.ParentId;
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == selectedTargetId)
                return true;

            if (_allNodesById.TryGetValue(currentParentId.Value, out var parentNode))
                currentParentId = parentNode.ParentId;
            else
                break;
        }

        return false;
    }

    private void EnsureInitialized()
    {
        if (_allNodesById == null || _skillNodes == null || _routerNodes == null)
            throw new InvalidOperationException(
                $"{nameof(RouterBranchNavigator)} has not been initialized. " +
                $"Call {nameof(Initialize)} before using navigation methods.");
    }
}