using FHOOE.Freydis.Application.Services.UI.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.UI.Visibility;

public class NodeHidingService : INodeHidingService
{
    private readonly ILogger<NodeHidingService> _logger;

    public NodeHidingService(ILogger<NodeHidingService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Node>> ApplyHiddenStateAsync(
        IReadOnlyList<Node>? allNodes,
        IReadOnlyCollection<Guid> nodesToHide)
    {
        if (allNodes == null || allNodes.Count == 0)
            return Task.FromResult<IReadOnlyList<Node>>([]);

        var hiddenNodeIds = new HashSet<Guid>(nodesToHide);
        var updatedNodes = new List<Node>(allNodes.Count);

        foreach (var node in allNodes)
        {
            var shouldBeHidden = hiddenNodeIds.Contains(node.Id);

            // Only create new node instance if Hidden state needs to change
            if (node.Hidden != shouldBeHidden)
            {
                var updatedNode = node with { Hidden = shouldBeHidden };
                updatedNodes.Add(updatedNode);
            }
            else
            {
                updatedNodes.Add(node);
            }
        }

        if (nodesToHide.Count > 0)
            _logger.LogHiddenStateApplied(nodesToHide.Count, allNodes.Count - nodesToHide.Count);

        return Task.FromResult<IReadOnlyList<Node>>(updatedNodes);
    }
}