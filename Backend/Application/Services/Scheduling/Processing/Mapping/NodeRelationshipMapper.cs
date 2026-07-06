using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Maps relationships between nodes in the domain model for scheduling operations.
/// </summary>
public class NodeRelationshipMapper : INodeRelationshipMapper
{
    private readonly ILogger<NodeRelationshipMapper> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeRelationshipMapper" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public NodeRelationshipMapper(ILogger<NodeRelationshipMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> BuildTaskToSkillMapping(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes)
    {
        _logger.LogTaskToSkillMappingStart(taskNodes.Count, skillExecutionNodes.Count);

        var mapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>();
        var totalMappedSkillNodes = 0;

        foreach (var taskNode in taskNodes)
        {
            var childSkillNodes = skillExecutionNodes
                .Where(skill => skill.ParentId == taskNode.Id)
                .ToList()
                .AsReadOnly();

            mapping[taskNode.Id] = childSkillNodes;
            totalMappedSkillNodes += childSkillNodes.Count;

            _logger.LogTaskToSkillRelationship(taskNode.Id, childSkillNodes.Count);
        }

        var unmappedSkillNodes = skillExecutionNodes.Count - totalMappedSkillNodes;
        _logger.LogTaskToSkillMappingComplete(mapping.Count, totalMappedSkillNodes, unmappedSkillNodes);

        if (unmappedSkillNodes <= 0) return mapping.AsReadOnly();

        var orphanedSkills = skillExecutionNodes
            .Where(s => !s.ParentId.HasValue || taskNodes.All(t => t.Id != s.ParentId.Value)).ToList();
        var orphanedIds = string.Join(", ", orphanedSkills.Select(s => s.Id.ToString()));
        _logger.LogOrphanedSkillNodes(orphanedSkills.Count, orphanedIds);

        return mapping.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, TaskNode> BuildSkillToTaskMapping(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes)
    {
        _logger.LogSkillToTaskMappingStart(skillExecutionNodes.Count, taskNodes.Count);

        var mapping = new Dictionary<Guid, TaskNode>();
        var taskNodeLookup = taskNodes.ToDictionary(task => task.Id);

        _logger.LogTaskLookupCreated(taskNodeLookup.Count);

        var skillsWithParents = 0;
        var skillsWithoutParents = 0;
        var skillsWithInvalidParents = 0;

        foreach (var skillNode in skillExecutionNodes)
            if (skillNode.ParentId.HasValue)
            {
                if (taskNodeLookup.TryGetValue(skillNode.ParentId.Value, out var parentTask))
                {
                    mapping[skillNode.Id] = parentTask;
                    skillsWithParents++;
                    _logger.LogSkillToTaskRelationship(skillNode.Id, parentTask.Id);
                }
                else
                {
                    skillsWithInvalidParents++;
                    _logger.LogSkillWithInvalidParent(skillNode.Id, skillNode.ParentId.Value);
                }
            }
            else
            {
                skillsWithoutParents++;
                _logger.LogSkillWithoutParent(skillNode.Id);
            }

        _logger.LogSkillToTaskMappingComplete(skillsWithParents, skillsWithoutParents, skillsWithInvalidParents);

        if (skillsWithInvalidParents > 0)
            _logger.LogInvalidParentReferencesWarning(skillsWithInvalidParents);

        return mapping.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, IReadOnlyList<Node>> BuildParentToChildrenMapping(IReadOnlyList<Node> nodes)
    {
        _logger.LogParentToChildrenMappingStart(nodes.Count);

        var result = new Dictionary<Guid, IReadOnlyList<Node>>();
        var lookup = nodes.ToLookup(n => n.ParentId ?? Guid.Empty);

        _logger.LogNodeLookupGroupsCreated(lookup.Count);

        var rootNodeCount = 0;
        var totalChildRelationships = 0;

        foreach (var group in lookup)
        {
            var childNodes = group.ToList().AsReadOnly();
            result[group.Key] = childNodes;

            if (group.Key == Guid.Empty)
            {
                rootNodeCount = childNodes.Count;
                _logger.LogRootNodesFound(rootNodeCount);

                if (!_logger.IsEnabled(LogLevel.Trace)) continue;

                foreach (var rootNode in childNodes)
                {
                    var rootNodeTypeName = rootNode.GetType().Name;
                    _logger.LogRootNodeDetail(rootNode.Id, rootNodeTypeName);
                }
            }
            else
            {
                totalChildRelationships += childNodes.Count;
                _logger.LogParentChildRelationship(group.Key, childNodes.Count);
            }
        }

        var parentGroups = lookup.Count - (rootNodeCount > 0 ? 1 : 0);
        _logger.LogParentToChildrenMappingComplete(rootNodeCount, totalChildRelationships, parentGroups);

        return result.AsReadOnly();
    }
}