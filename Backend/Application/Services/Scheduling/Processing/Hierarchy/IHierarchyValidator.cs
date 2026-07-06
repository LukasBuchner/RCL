using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Service responsible for validating node hierarchy consistency.
/// </summary>
public interface IHierarchyValidator
{
    /// <summary>
    ///     Validates the consistency of hierarchy mappings.
    /// </summary>
    /// <param name="taskNodes">Task nodes in the hierarchy.</param>
    /// <param name="skillExecutionNodes">Skill execution nodes in the hierarchy.</param>
    /// <param name="taskToSkillMapping">Forward mapping from tasks to skills.</param>
    /// <param name="skillToTaskMapping">Reverse mapping from skills to tasks.</param>
    /// <returns>Validation result with success status and any error messages.</returns>
    HierarchyValidationResult ValidateConsistency(
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<SkillExecutionNode> skillExecutionNodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>> taskToSkillMapping,
        IReadOnlyDictionary<Guid, TaskNode> skillToTaskMapping);
}

/// <summary>
///     Result of hierarchy validation operation.
/// </summary>
public record HierarchyValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);