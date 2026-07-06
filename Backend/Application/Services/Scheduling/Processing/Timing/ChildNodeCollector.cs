using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;

/// <summary>
///     Implementation of child node collection service.
/// </summary>
public class ChildNodeCollector : IChildNodeCollector
{
    private readonly ILogger<ChildNodeCollector> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="ChildNodeCollector" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public ChildNodeCollector(ILogger<ChildNodeCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<SkillExecutionNode> CollectChildSkillNodes(Guid parentId, IReadOnlyList<Node> allNodes)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var childSkillNodes = allNodes
            .OfType<SkillExecutionNode>()
            .Where(sn => sn.ParentId == parentId)
            .ToList()
            .AsReadOnly();

        _logger.LogSkillChildNodesFound(childSkillNodes.Count, parentId);

        return childSkillNodes;
    }

    /// <inheritdoc />
    public IReadOnlyList<TaskNode> CollectChildTaskNodes(Guid parentId, IReadOnlyList<Node> allNodes)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var childTaskNodes = allNodes
            .OfType<TaskNode>()
            .Where(tn => tn.ParentId == parentId)
            .ToList()
            .AsReadOnly();

        _logger.LogTaskChildNodesFound(childTaskNodes.Count, parentId);

        return childTaskNodes;
    }

    /// <inheritdoc />
    public IReadOnlyList<RouterNode> CollectChildRouterNodes(Guid parentId, IReadOnlyList<Node> allNodes)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var childRouterNodes = allNodes
            .OfType<RouterNode>()
            .Where(rn => rn.ParentId == parentId)
            .ToList()
            .AsReadOnly();

        _logger.LogRouterChildNodesFound(childRouterNodes.Count, parentId);

        return childRouterNodes;
    }

    /// <inheritdoc />
    public (IReadOnlyList<SkillExecutionNode> SkillNodes, IReadOnlyList<TaskNode> TaskNodes, IReadOnlyList<RouterNode>
        RouterNodes) CollectAllChildNodes(
            Guid parentId, IReadOnlyList<Node> allNodes)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var skillNodes = CollectChildSkillNodes(parentId, allNodes);
        var taskNodes = CollectChildTaskNodes(parentId, allNodes);
        var routerNodes = CollectChildRouterNodes(parentId, allNodes);

        _logger.LogAllChildNodesFound(skillNodes.Count, taskNodes.Count, routerNodes.Count, parentId);

        return (skillNodes, taskNodes, routerNodes);
    }
}