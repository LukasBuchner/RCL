using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Implementation of hierarchy validation service.
/// </summary>
public class HierarchyValidator : IHierarchyValidator
{
    private readonly ILogger<HierarchyValidator> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="HierarchyValidator" />.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and debugging.</param>
    public HierarchyValidator(ILogger<HierarchyValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public HierarchyValidationResult ValidateConsistency(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> taskToSkillMapping,
        IReadOnlyDictionary<Guid, TaskNode> skillToTaskMapping)
    {
        _logger.LogHierarchyValidationStarted(taskNodes.Count, skillExecutionNodes.Count);

        var validationErrors = new List<string>();
        var validationWarnings = new List<string>();

        // Validate task-to-skill mapping consistency
        ValidateTaskToSkillMappingConsistency(taskToSkillMapping, skillToTaskMapping, validationErrors,
            validationWarnings);

        // Validate skill-to-task mapping consistency
        ValidateSkillToTaskMappingConsistency(taskToSkillMapping, skillToTaskMapping, validationErrors,
            validationWarnings);

        // Check for circular references
        ValidateCircularReferences(taskNodes, validationErrors);

        if (validationErrors.Count > 0)
        {
            var errorMessage = string.Join("; ", validationErrors);
            _logger.LogHierarchyValidationErrors(validationErrors.Count, errorMessage);
        }

        if (validationWarnings.Count > 0)
        {
            var warningMessage = string.Join("; ", validationWarnings);
            _logger.LogHierarchyValidationWarnings(validationWarnings.Count, warningMessage);
        }

        if (validationErrors.Count == 0 && validationWarnings.Count == 0)
            _logger.LogHierarchyValidationPassed();

        return new HierarchyValidationResult(
            validationErrors.Count == 0,
            validationErrors.AsReadOnly(),
            validationWarnings.AsReadOnly());
    }

    private static void ValidateTaskToSkillMappingConsistency(
        IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> taskToSkillMapping,
        IReadOnlyDictionary<Guid, TaskNode> skillToTaskMapping,
        List<string> validationErrors,
        List<string> validationWarnings)
    {
        foreach (var (taskId, childSkills) in taskToSkillMapping)
            foreach (var childSkill in childSkills)
                if (skillToTaskMapping.TryGetValue(childSkill.Id, out var parentTask))
                {
                    if (parentTask.Id != taskId)
                        validationErrors.Add(
                            $"Inconsistent mapping: Skill {childSkill.Id} is child of task {taskId} but reverse mapping points to task {parentTask.Id}");
                }
                else
                {
                    validationWarnings.Add($"Skill {childSkill.Id} is child of task {taskId} but has no reverse mapping");
                }
    }

    private static void ValidateSkillToTaskMappingConsistency(
        IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> taskToSkillMapping,
        IReadOnlyDictionary<Guid, TaskNode> skillToTaskMapping,
        List<string> validationErrors,
        List<string> validationWarnings)
    {
        foreach (var (skillId, parentTask) in skillToTaskMapping)
            if (taskToSkillMapping.TryGetValue(parentTask.Id, out var childSkills))
            {
                if (!childSkills.Any(s => s.Id == skillId))
                    validationErrors.Add(
                        $"Inconsistent mapping: Skill {skillId} maps to parent {parentTask.Id} but parent doesn't list it as child");
            }
            else
            {
                validationWarnings.Add(
                    $"Skill {skillId} maps to parent {parentTask.Id} but parent has no children in forward mapping");
            }
    }

    private static void ValidateCircularReferences(IReadOnlyList<TaskNode> taskNodes, List<string> validationErrors)
    {
        var visitedNodes = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();

        foreach (var taskNode in taskNodes)
            if (!visitedNodes.Contains(taskNode.Id))
                CheckForCircularReferences(taskNode, visitedNodes, recursionStack, taskNodes, validationErrors);
    }

    private static void CheckForCircularReferences(
        Node currentNode,
        HashSet<Guid> visitedNodes,
        HashSet<Guid> recursionStack,
        IReadOnlyList<TaskNode> allNodes,
        List<string> validationErrors)
    {
        visitedNodes.Add(currentNode.Id);
        recursionStack.Add(currentNode.Id);

        if (currentNode.ParentId.HasValue)
        {
            var parentId = currentNode.ParentId.Value;

            if (recursionStack.Contains(parentId))
            {
                validationErrors.Add(
                    $"Circular reference detected: Node {currentNode.Id} has ancestor {parentId} which creates a cycle");
            }
            else if (!visitedNodes.Contains(parentId))
            {
                var parentNode = allNodes.FirstOrDefault(n => n.Id == parentId);
                if (parentNode != null)
                    CheckForCircularReferences(parentNode, visitedNodes, recursionStack, allNodes, validationErrors);
            }
        }

        recursionStack.Remove(currentNode.Id);
    }
}