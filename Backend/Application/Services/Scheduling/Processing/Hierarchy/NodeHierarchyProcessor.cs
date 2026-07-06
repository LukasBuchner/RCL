using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Implementation of node hierarchy processing.
///     Extracts common node processing logic that was duplicated across multiple services.
/// </summary>
public class NodeHierarchyProcessor : INodeHierarchyProcessor
{
    private readonly IHierarchyValidator _hierarchyValidator;
    private readonly ILogger<NodeHierarchyProcessor> _logger;
    private readonly INodeRelationshipMapper _relationshipMapper;

    /// <summary>
    ///     Initializes a new instance of <see cref="NodeHierarchyProcessor" />.
    /// </summary>
    /// <param name="relationshipMapper">Service for building node relationship mappings.</param>
    /// <param name="hierarchyValidator">Service for validating hierarchy consistency.</param>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public NodeHierarchyProcessor(
        INodeRelationshipMapper relationshipMapper,
        IHierarchyValidator hierarchyValidator,
        ILogger<NodeHierarchyProcessor> logger)
    {
        _relationshipMapper = relationshipMapper ?? throw new ArgumentNullException(nameof(relationshipMapper));
        _hierarchyValidator = hierarchyValidator ?? throw new ArgumentNullException(nameof(hierarchyValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <inheritdoc />
    public NodeHierarchyInfo ProcessHierarchy(IReadOnlyList<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        _logger.LogHierarchyProcessingStarted(nodes.Count);

        if (nodes.Count == 0)
        {
            _logger.LogEmptyNodeListHierarchy();
            return new NodeHierarchyInfo
            {
                TaskNodes = Array.Empty<TaskNode>().AsReadOnly(),
                SkillExecutionNodes = Array.Empty<SkillExecutionNode>().AsReadOnly(),
                RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
                ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>().AsReadOnly(),
                TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>().AsReadOnly(),
                SkillToTaskMapping = new Dictionary<Guid, TaskNode>().AsReadOnly()
            };
        }

        // Separate nodes by type
        var taskNodes = nodes.OfType<TaskNode>().ToList().AsReadOnly();
        var skillExecutionNodes = nodes.OfType<SkillExecutionNode>().ToList().AsReadOnly();
        var routerNodes = nodes.OfType<RouterNode>().ToList().AsReadOnly();
        var otherNodes = nodes.Where(n => n is not TaskNode and not SkillExecutionNode and not RouterNode).ToList();

        _logger.LogNodeTypeAnalysis(taskNodes.Count, skillExecutionNodes.Count, routerNodes.Count, otherNodes.Count);

        if (otherNodes.Count > 0)
        {
            var unexpectedTypeNames = string.Join(", ", otherNodes.Select(n => n.GetType().Name).Distinct());
            _logger.LogUnexpectedNodeTypes(otherNodes.Count, unexpectedTypeNames);
        }

        // Log node details
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            foreach (var taskNode in taskNodes)
                _logger.LogTaskNodeDetail(taskNode.Id, taskNode.Task?.Name ?? "Unknown", taskNode.ParentId);

            foreach (var skillNode in skillExecutionNodes)
                _logger.LogSkillExecutionNodeDetail(
                    skillNode.Id, skillNode.ParentId, skillNode.SkillExecutionTask.Skill.Name,
                    skillNode.SkillExecutionTask.AgentId);

            foreach (var routerNode in routerNodes)
                _logger.LogRouterNodeDetail(
                    routerNode.Id, routerNode.RouterTask?.Name ?? "Unknown", routerNode.ParentId,
                    routerNode.RouterTask?.Branches?.Count ?? 0);
        }

        // Build mappings using extracted service
        _logger.LogBuildingRelationshipMappings();
        var taskToSkillMapping = _relationshipMapper.BuildTaskToSkillMapping(taskNodes, skillExecutionNodes);
        var skillToTaskMapping = _relationshipMapper.BuildSkillToTaskMapping(taskNodes, skillExecutionNodes);
        var parentToChildrenMapping = _relationshipMapper.BuildParentToChildrenMapping(nodes);

        // Validate hierarchy consistency using extracted service
        _logger.LogValidatingHierarchy();
        var validationResult =
            _hierarchyValidator.ValidateConsistency(taskNodes, skillExecutionNodes, taskToSkillMapping,
                skillToTaskMapping);

        if (!validationResult.IsValid)
            _logger.LogHierarchyValidationFailed(validationResult.Errors.Count);

        var result = new NodeHierarchyInfo
        {
            TaskNodes = taskNodes,
            SkillExecutionNodes = skillExecutionNodes,
            RouterNodes = routerNodes,
            ParentToChildrenMapping = parentToChildrenMapping,
            TaskToSkillMapping = taskToSkillMapping,
            SkillToTaskMapping = skillToTaskMapping
        };

        // Log comprehensive statistics
        var orphanedSkillNodes =
            skillExecutionNodes.Count(s => !s.ParentId.HasValue || !taskToSkillMapping.ContainsKey(s.ParentId.Value));
        var tasksWithChildren = taskToSkillMapping.Count(kvp => kvp.Value.Count > 0);
        var tasksWithoutChildren = taskNodes.Count - tasksWithChildren;

        _logger.LogHierarchyProcessingCompleted(
            result.TaskNodes.Count, tasksWithChildren, tasksWithoutChildren, result.SkillExecutionNodes.Count,
            orphanedSkillNodes, result.RouterNodes.Count, result.ParentToChildrenMapping.Count);

        if (orphanedSkillNodes > 0)
            _logger.LogOrphanedSkillNodesFound(orphanedSkillNodes);

        return result;
    }
}