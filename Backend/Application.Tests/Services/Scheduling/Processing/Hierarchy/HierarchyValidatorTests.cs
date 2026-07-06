using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Hierarchy;

/// <ILogger
/// <HierarchyValidator>
///     > for HierarchyValidator service.
///     </summary>
public class HierarchyValidatorTests
{
    private readonly Mock<ILogger<HierarchyValidator>> _mockLogger;
    private readonly HierarchyValidator _validator;

    public HierarchyValidatorTests()
    {
        _mockLogger = new Mock<ILogger<HierarchyValidator>>();
        _validator = new HierarchyValidator(_mockLogger.Object);
    }

    [Fact]
    public void ValidateConsistency_WithValidHierarchy_ReturnsSuccess()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = CreateTaskNode(taskId, "Task1");
        var skillNode = CreateSkillExecutionNode(skillId, "Skill1", taskId);

        var taskNodes = new List<TaskNode> { taskNode }.AsReadOnly();
        var skillNodes = new List<SkillExecutionNode> { skillNode }.AsReadOnly();

        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskId] = new List<SkillExecutionNode> { skillNode }.AsReadOnly()
        }.AsReadOnly();

        var skillToTaskMapping = new Dictionary<Guid, TaskNode>
        {
            [skillId] = taskNode
        }.AsReadOnly();

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ValidateConsistency_WithInconsistentTaskToSkillMapping_ReturnsErrors()
    {
        // Arrange
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode1 = CreateTaskNode(taskId1, "Task1");
        var taskNode2 = CreateTaskNode(taskId2, "Task2");
        var skillNode = CreateSkillExecutionNode(skillId, "Skill1", taskId1);

        var taskNodes = new List<TaskNode> { taskNode1, taskNode2 }.AsReadOnly();
        var skillNodes = new List<SkillExecutionNode> { skillNode }.AsReadOnly();

        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskId1] = new List<SkillExecutionNode> { skillNode }.AsReadOnly(),
            [taskId2] = new List<SkillExecutionNode>().AsReadOnly()
        }.AsReadOnly();

        var skillToTaskMapping = new Dictionary<Guid, TaskNode>
        {
            [skillId] = taskNode2 // Points to wrong task!
        }.AsReadOnly();

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count); // Should detect both sides of the inconsistency
        Assert.All(result.Errors, error => Assert.Contains("Inconsistent mapping", error));
    }

    [Fact]
    public void ValidateConsistency_WithMissingReverseMapping_ReturnsWarnings()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = CreateTaskNode(taskId, "Task1");
        var skillNode = CreateSkillExecutionNode(skillId, "Skill1", taskId);

        var taskNodes = new List<TaskNode> { taskNode }.AsReadOnly();
        var skillNodes = new List<SkillExecutionNode> { skillNode }.AsReadOnly();

        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskId] = new List<SkillExecutionNode> { skillNode }.AsReadOnly()
        }.AsReadOnly();

        var skillToTaskMapping = new Dictionary<Guid, TaskNode>().AsReadOnly(); // Empty!

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.True(result.IsValid); // Warnings don't make it invalid
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("has no reverse mapping", result.Warnings[0]);
    }

    [Fact]
    public void ValidateConsistency_WithCircularReference_ReturnsErrors()
    {
        // Arrange
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        var taskNode1 = CreateTaskNode(taskId1, "Task1", taskId2); // Points to task2
        var taskNode2 = CreateTaskNode(taskId2, "Task2", taskId1); // Points to task1 - circular!

        var taskNodes = new List<TaskNode> { taskNode1, taskNode2 }.AsReadOnly();
        var skillNodes = new List<SkillExecutionNode>().AsReadOnly();
        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>().AsReadOnly();
        var skillToTaskMapping = new Dictionary<Guid, TaskNode>().AsReadOnly();

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("Circular reference detected"));
    }

    [Fact]
    public void ValidateConsistency_WithSkillMappingInconsistency_ReturnsErrors()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = CreateTaskNode(taskId, "Task1");
        var skillNode = CreateSkillExecutionNode(skillId, "Skill1", taskId);

        var taskNodes = new List<TaskNode> { taskNode }.AsReadOnly();
        var skillNodes = new List<SkillExecutionNode> { skillNode }.AsReadOnly();

        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskId] = new List<SkillExecutionNode>().AsReadOnly() // Empty list!
        }.AsReadOnly();

        var skillToTaskMapping = new Dictionary<Guid, TaskNode>
        {
            [skillId] = taskNode // Skill points to task, but task doesn't list skill
        }.AsReadOnly();

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("parent doesn't list it as child", result.Errors[0]);
    }

    [Fact]
    public void ValidateConsistency_WithEmptyCollections_ReturnsSuccess()
    {
        // Arrange
        var taskNodes = new List<TaskNode>().AsReadOnly();
        var skillNodes = new List<SkillExecutionNode>().AsReadOnly();
        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>().AsReadOnly();
        var skillToTaskMapping = new Dictionary<Guid, TaskNode>().AsReadOnly();

        // Act
        var result = _validator.ValidateConsistency(taskNodes, skillNodes, taskToSkillMapping, skillToTaskMapping);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    private static TaskNode CreateTaskNode(Guid id, string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0,
                IsExecuting = false,
                Progress = 0.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(Guid id, string skillName, Guid? parentId)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = "Test skill",
                    Properties = []
                }
            }
        };
    }
}