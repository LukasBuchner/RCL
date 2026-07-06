using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Common;

/// <summary>
///     Pure traversal helpers over the procedure node tree (<see cref="Node.ParentId" /> links),
///     defined once and shared by the services that walk node descendants so the traversal is not
///     re-implemented per call site. All walks are breadth-first with a visited guard, so a malformed
///     parent cycle terminates rather than loops forever.
/// </summary>
public static class HierarchyTraversal
{
    /// <summary>
    ///     Collects every descendant of <paramref name="rootNodeId" /> breadth-first, using
    ///     <paramref name="childrenOf" /> to enumerate the direct children of each visited node.
    ///     The root node itself is not included; each descendant is yielded once.
    /// </summary>
    /// <param name="rootNodeId">The id of the node whose descendants are collected.</param>
    /// <param name="childrenOf">Returns the direct children of the given node id.</param>
    /// <returns>The descendant nodes at every depth, in breadth-first order, without duplicates.</returns>
    public static List<Node> CollectDescendants(Guid rootNodeId, Func<Guid, IEnumerable<Node>> childrenOf)
    {
        var descendants = new List<Node>();
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootNodeId);

        while (queue.Count > 0)
        {
            var currentParentId = queue.Dequeue();
            foreach (var child in childrenOf(currentParentId))
                if (visited.Add(child.Id))
                {
                    descendants.Add(child);
                    queue.Enqueue(child.Id);
                }
        }

        return descendants;
    }

    /// <summary>
    ///     Collects every descendant of <paramref name="rootNodeId" /> breadth-first over the flat
    ///     <paramref name="allNodes" /> set. The root node itself is not included.
    /// </summary>
    /// <param name="rootNodeId">The id of the node whose descendants are collected.</param>
    /// <param name="allNodes">The collection of all nodes to search.</param>
    /// <returns>A flat list of all descendant nodes at every depth, in breadth-first order.</returns>
    public static List<Node> CollectDescendants(Guid rootNodeId, IReadOnlyList<Node> allNodes)
    {
        return CollectDescendants(rootNodeId, parentId => allNodes.Where(n => n.ParentId == parentId));
    }

    /// <summary>
    ///     Collects the ids of every descendant of <paramref name="rootNodeId" />, using a prebuilt
    ///     parent-to-children index to enumerate direct children.
    /// </summary>
    /// <param name="rootNodeId">The id of the node whose descendant ids are collected.</param>
    /// <param name="childIndex">Index mapping a parent node id to its direct child nodes.</param>
    /// <returns>The set of descendant node ids (the root is not included).</returns>
    public static HashSet<Guid> CollectDescendantIds(
        Guid rootNodeId, IReadOnlyDictionary<Guid, List<Node>> childIndex)
    {
        return CollectDescendants(rootNodeId, id => childIndex.TryGetValue(id, out var children) ? children : [])
            .Select(n => n.Id)
            .ToHashSet();
    }
}