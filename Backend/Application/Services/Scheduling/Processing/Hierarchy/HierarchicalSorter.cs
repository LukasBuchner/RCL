using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Implementation of hierarchical sorting service.
/// </summary>
public class HierarchicalSorter : IHierarchicalSorter
{
    private readonly ILogger<HierarchicalSorter> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="HierarchicalSorter" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public HierarchicalSorter(ILogger<HierarchicalSorter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<TaskNode> SortTaskNodesHierarchically(IEnumerable<TaskNode> taskNodes)
    {
        ArgumentNullException.ThrowIfNull(taskNodes);

        var nodeList = taskNodes.ToList();
        var result = new List<TaskNode>();
        var processed = new HashSet<Guid>();

        // Build a set of all TaskNode IDs for efficient lookup
        var taskNodeIds = new HashSet<Guid>(nodeList.Select(tn => tn.Id));

        _logger.LogHierarchicalSortStarted(nodeList.Count);

        // Process nodes depth-first, ensuring children are processed before parents
        void ProcessNode(TaskNode node)
        {
            if (processed.Contains(node.Id))
            {
                _logger.LogNodeAlreadyProcessed(node.Id);
                return;
            }

            // First process all children of this node
            var children = nodeList.Where(tn => tn.ParentId == node.Id).ToList();
            _logger.LogProcessingChildren(children.Count, node.Id);

            foreach (var child in children) ProcessNode(child);

            // Then process this node
            result.Add(node);
            processed.Add(node.Id);
            _logger.LogNodeAddedToSortedResult(node.Id, result.Count);
        }

        // A TaskNode is considered a "root" for sorting purposes if:
        // 1. It has no parent (ParentId is null), OR
        // 2. Its parent is not a TaskNode (e.g., parent is a RouterNode)
        // This handles branch TaskNodes whose ParentId points to a RouterNode
        var rootNodes = nodeList.Where(tn =>
            !tn.ParentId.HasValue || !taskNodeIds.Contains(tn.ParentId.Value)).ToList();
        _logger.LogRootNodesForSorting(rootNodes.Count);

        foreach (var rootNode in rootNodes) ProcessNode(rootNode);

        // Handle any remaining nodes (truly orphaned nodes with invalid parent references)
        var remainingNodes = nodeList.Where(tn => !processed.Contains(tn.Id)).ToList();
        if (remainingNodes.Count > 0)
        {
            _logger.LogOrphanedNodesWarning(remainingNodes.Count);
            foreach (var node in remainingNodes) ProcessNode(node);
        }

        _logger.LogHierarchicalSortCompleted(nodeList.Count, result.Count);

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<TaskNode> SortTaskNodesHierarchically(IEnumerable<TaskNode> taskNodes,
        IReadOnlyList<Node> allNodes)
    {
        ArgumentNullException.ThrowIfNull(taskNodes);
        ArgumentNullException.ThrowIfNull(allNodes);

        var nodeList = taskNodes.ToList();
        if (nodeList.Count == 0)
            return new List<TaskNode>().AsReadOnly();

        // Build a lookup of all nodes by ID for hierarchy traversal through RouterNode boundaries
        var allNodesById = allNodes.ToDictionary(n => n.Id);

        // Calculate true hierarchy depth for each TaskNode by walking through all parent types
        int GetHierarchyDepth(TaskNode tn)
        {
            var depth = 0;
            var currentParentId = tn.ParentId;
            while (currentParentId.HasValue)
            {
                depth++;
                if (!allNodesById.TryGetValue(currentParentId.Value, out var parentNode))
                    break;
                currentParentId = parentNode.ParentId;
            }

            return depth;
        }

        // Sort by depth descending (deepest first), then by original order for stability
        var indexed = nodeList.Select((tn, i) => (Node: tn, Index: i, Depth: GetHierarchyDepth(tn))).ToList();
        var sorted = indexed.OrderByDescending(x => x.Depth).ThenBy(x => x.Index).Select(x => x.Node).ToList();

        var depthsSummary = string.Join(", ",
            indexed.OrderByDescending(x => x.Depth).ThenBy(x => x.Index)
                .Select(x => $"{x.Node.Task.Name}={x.Depth}"));

        _logger.LogHierarchicalSortWithDepthsCompleted(
            nodeList.Count, sorted.Count, depthsSummary);

        return sorted.AsReadOnly();
    }
}