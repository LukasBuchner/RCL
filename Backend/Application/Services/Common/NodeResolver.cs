using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Common;

/// <summary>
///     Implements <see cref="INodeResolver" /> by traversing the containment hierarchy in a
///     <see cref="NodeHierarchyInfo" />. Stateless — safe to register as a singleton.
/// </summary>
public sealed class NodeResolver(ILogger<NodeResolver> logger) : INodeResolver
{
    private readonly ILogger<NodeResolver> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public IEnumerable<Guid> ResolveToExecutableIds(Guid nodeId, NodeHierarchyInfo hierarchy)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);

        var startNode = FindNode(nodeId, hierarchy);
        if (startNode is null)
        {
            _logger.LogSourceNodeNotFound(nodeId);
            return [];
        }

        var result = new List<Guid>();
        var visited = new HashSet<Guid>();
        CollectExecutableDescendants(startNode, hierarchy, result, visited);
        return result;
    }

    /// <inheritdoc />
    public IEnumerable<Guid> ResolveToFiringEndpointsIds(Guid nodeId, NodeHierarchyInfo hierarchy)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);

        var leaves = ResolveToExecutableIds(nodeId, hierarchy).ToList();
        if (leaves.Count > 0)
            return leaves;

        // No executable leaves: a leafless container present in the hierarchy is its own zero-extent firing
        // endpoint, so a dependency through it gates on the container's own Start/Finish. An id absent from
        // the hierarchy resolves to nothing.
        if (FindNode(nodeId, hierarchy) is null)
            return [];

        return [nodeId];
    }

    /// <summary>
    ///     Locates the hierarchy node carrying <paramref name="nodeId" />.
    /// </summary>
    /// <param name="nodeId">The node ID to look up.</param>
    /// <param name="hierarchy">The processed node hierarchy to search.</param>
    /// <returns>The matching <see cref="Node" />, or <c>null</c> when the ID is absent from the hierarchy.</returns>
    private static Node? FindNode(Guid nodeId, NodeHierarchyInfo hierarchy)
    {
        Node? node = hierarchy.SkillExecutionNodes.FirstOrDefault(s => s.Id == nodeId);
        node ??= hierarchy.RouterNodes.FirstOrDefault(r => r.Id == nodeId);
        node ??= hierarchy.TaskNodes.FirstOrDefault(t => t.Id == nodeId);
        return node;
    }

    /// <summary>
    ///     Walks the containment subtree of <paramref name="node" />, collecting the executable node IDs it
    ///     represents. A <see cref="SkillExecutionNode" /> or <see cref="RouterNode" /> contributes its own ID and
    ///     terminates the branch; a <see cref="TaskNode" /> contributes the union of its children's executable IDs,
    ///     recursing through nested task nodes to any depth.
    /// </summary>
    /// <param name="node">The node whose executable descendants are collected.</param>
    /// <param name="hierarchy">The processed node hierarchy supplying the parent-to-children mapping.</param>
    /// <param name="result">Accumulates the resolved executable IDs in encounter order, without duplicates.</param>
    /// <param name="visited">Tracks visited node IDs so a malformed parent-child cycle terminates the walk.</param>
    private static void CollectExecutableDescendants(
        Node node, NodeHierarchyInfo hierarchy, List<Guid> result, HashSet<Guid> visited)
    {
        if (!visited.Add(node.Id))
            return;

        switch (node)
        {
            case SkillExecutionNode:
            case RouterNode:
                result.Add(node.Id);
                return;

            case TaskNode:
                if (hierarchy.ParentToChildrenMapping.TryGetValue(node.Id, out var children))
                    foreach (var child in children)
                        CollectExecutableDescendants(child, hierarchy, result, visited);
                return;
        }
    }
}