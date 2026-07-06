using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Utilities;

/// <summary>
///     Unit tests for ExecutionIdAssigner service.
/// </summary>
public class ExecutionIdAssignerTests
{
    private readonly ExecutionIdAssigner _assigner = new();

    [Fact]
    public void AssignExecutionIds_WithSkillExecutionNodes_AssignsUniqueIds()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var nodes = new List<Node> { skillNode1, skillNode2 }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Equal(2, result.Count);
        var resultSkill1 = (SkillExecutionNode)result[0];
        var resultSkill2 = (SkillExecutionNode)result[1];

        Assert.NotNull(resultSkill1.SkillExecutionTask.ExecutionId);
        Assert.NotNull(resultSkill2.SkillExecutionTask.ExecutionId);
        Assert.NotEqual(resultSkill1.SkillExecutionTask.ExecutionId, resultSkill2.SkillExecutionTask.ExecutionId);
    }

    [Fact]
    public void AssignExecutionIds_WithMixedNodes_OnlyAssignsToSkillNodes()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task 1");
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Equal(2, result.Count);
        var resultTask = (TaskNode)result[0];
        var resultSkill = (SkillExecutionNode)result[1];

        Assert.NotNull(resultSkill.SkillExecutionTask.ExecutionId);
        // TaskNode doesn't have ExecutionId - just verify it's still a TaskNode
        Assert.IsType<TaskNode>(resultTask);
    }

    [Fact]
    public void AssignExecutionIds_WithNoSkillNodes_ReturnsUnchangedNodes()
    {
        // Arrange
        var taskNode1 = CreateTaskNode("Task 1");
        var taskNode2 = CreateTaskNode("Task 2");
        var nodes = new List<Node> { taskNode1, taskNode2 }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, node => Assert.IsType<TaskNode>(node));
    }

    [Fact]
    public void AssignExecutionIds_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var nodes = new List<Node>().AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AssignExecutionIds_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _assigner.AssignExecutionIds(null!));
    }

    [Fact]
    public void AssignExecutionIds_CreatesNewNodeInstances_DoesNotModifyOriginal()
    {
        // Arrange
        var originalSkillNode = CreateSkillExecutionNode("Skill 1");
        var originalExecutionId = originalSkillNode.SkillExecutionTask.ExecutionId;
        var nodes = new List<Node> { originalSkillNode }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var resultSkillNode = (SkillExecutionNode)result[0];
        Assert.NotEqual(originalExecutionId, resultSkillNode.SkillExecutionTask.ExecutionId);
        Assert.Equal(originalExecutionId, originalSkillNode.SkillExecutionTask.ExecutionId); // Original unchanged
    }

    [Fact]
    public void AssignExecutionIds_WithMultipleSkillNodes_AssignsDistinctIds()
    {
        // Arrange
        var nodes = Enumerable.Range(0, 10)
            .Select(i => CreateSkillExecutionNode($"Skill {i}"))
            .Cast<Node>()
            .ToList()
            .AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var executionIds = result
            .OfType<SkillExecutionNode>()
            .Select(n => n.SkillExecutionTask.ExecutionId)
            .ToList();

        Assert.Equal(10, executionIds.Count);
        Assert.Equal(executionIds.Count, executionIds.Distinct().Count()); // All unique
    }

    [Fact]
    public void AssignExecutionIds_PreservesNodeProperties()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalParentId = Guid.NewGuid();
        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = originalId,
            ParentId = originalParentId,
            Position = new NodePosition { X = 100, Y = 200 },
            Height = 75,
            Width = 150,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                StartTime = 10,
                Duration = 100,
                FinishTime = 110,
                Skill = CreateSkill("Test Skill"),
                AgentId = Guid.NewGuid()
            }
        };
        var nodes = new List<Node> { skillNode }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var resultSkillNode = (SkillExecutionNode)result[0];
        Assert.Equal(originalId, resultSkillNode.Id);
        Assert.Equal(originalParentId, resultSkillNode.ParentId);
        Assert.Equal(100, resultSkillNode.Position.X);
        Assert.Equal(200, resultSkillNode.Position.Y);
        Assert.Equal(75, resultSkillNode.Height);
        Assert.Equal(150, resultSkillNode.Width);
        Assert.Equal("Test Skill", resultSkillNode.SkillExecutionTask.Name);
    }

    [Fact]
    public void AssignExecutionIds_ReturnsReadOnlyList()
    {
        // Arrange
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Node>>(result);
    }

    // Helper methods
    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                Skill = CreateSkill(skillName)
            }
        };
    }

    private static Skill CreateSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };
    }
}