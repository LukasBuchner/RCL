using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Utilities;

/// <summary>
///     Tests to ensure ExecutionId is properly propagated throughout the system.
/// </summary>
public class ExecutionIdPropagationTests
{
    private readonly ExecutionIdAssigner _assigner = new();

    #region Test: ExecutionId Immutability

    [Fact]
    public void AssignExecutionIds_ShouldNotModifyOriginalNode()
    {
        // Nodes are records, so this should create new instances
        // Arrange
        var originalNode = CreateSkillExecutionNode();
        var nodes = new List<Node> { originalNode };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Null(originalNode.SkillExecutionTask.ExecutionId); // Original unchanged

        var newNode = result[0] as SkillExecutionNode;
        Assert.NotNull(newNode);
        Assert.NotNull(newNode.SkillExecutionTask.ExecutionId);

        Assert.NotSame(originalNode, newNode); // Different instances
    }

    #endregion

    #region Test: ExecutionId Stability

    [Fact]
    public void AssignExecutionIds_CalledTwice_ShouldGenerateDifferentExecutionIds()
    {
        // ExecutionId should be regenerated each time (for retry scenarios)
        // Arrange
        var node = CreateSkillExecutionNode();
        var nodes = new List<Node> { node };

        // Act
        var result1 = _assigner.AssignExecutionIds(nodes);
        var result2 = _assigner.AssignExecutionIds(result1);

        // Assert
        var skill1 = result1[0] as SkillExecutionNode;
        var skill2 = result2[0] as SkillExecutionNode;

        Assert.NotEqual(skill1!.SkillExecutionTask.ExecutionId,
            skill2!.SkillExecutionTask.ExecutionId);
    }

    #endregion

    #region Test: ExecutionId Usage in Progress Tracking

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public void AssignExecutionIds_WithNNodes_ShouldAssignNExecutionIds(int nodeCount)
    {
        // Test scalability
        // Arrange
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(_ => CreateSkillExecutionNode() as Node)
            .ToList();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var skillNodes = result.OfType<SkillExecutionNode>().ToList();
        Assert.Equal(nodeCount, skillNodes.Count);

        var executionIds = skillNodes
            .Select(n => n.SkillExecutionTask.ExecutionId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        Assert.Equal(nodeCount, executionIds.Count);
        Assert.Equal(nodeCount, executionIds.Distinct().Count());
    }

    #endregion

    #region Test: ExecutionId Format

    [Fact]
    public void AssignExecutionIds_ShouldGenerateValidGuids()
    {
        // Arrange
        var node = CreateSkillExecutionNode();
        var nodes = new List<Node> { node };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var skillNode = result[0] as SkillExecutionNode;
        var executionId = skillNode!.SkillExecutionTask.ExecutionId!.Value;

        Assert.NotEqual(Guid.Empty, executionId);
        Assert.Equal(36, executionId.ToString().Length); // Standard GUID format
    }

    #endregion

    #region Helper Methods

    private static SkillExecutionNode CreateSkillExecutionNode(Guid? id = null, Guid? executionId = null)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties = new List<TypedProperty>()
        };

        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Task",
                StartTime = 0,
                Duration = 10,
                Skill = skill,
                AgentId = Guid.NewGuid(),
                ExecutionId = executionId
            }
        };
    }

    private static TaskNode CreateTaskNode(Guid? id = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "ParentTask",
                StartTime = 0,
                Duration = 100
            }
        };
    }

    #endregion

    #region Test: ExecutionId Assignment

    [Fact]
    public void AssignExecutionIds_WithSkillExecutionNode_ShouldAssignNewExecutionId()
    {
        // Arrange
        var node = CreateSkillExecutionNode();
        var nodes = new List<Node> { node };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Single(result);
        var skillNode = result[0] as SkillExecutionNode;
        Assert.NotNull(skillNode);
        Assert.NotNull(skillNode.SkillExecutionTask.ExecutionId);
        Assert.NotEqual(Guid.Empty, skillNode.SkillExecutionTask.ExecutionId.Value);
    }

    [Fact]
    public void AssignExecutionIds_WithMultipleSkillNodes_ShouldAssignUniqueExecutionIds()
    {
        // Arrange
        var node1 = CreateSkillExecutionNode();
        var node2 = CreateSkillExecutionNode();
        var node3 = CreateSkillExecutionNode();
        var nodes = new List<Node> { node1, node2, node3 };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var skillNodes = result.OfType<SkillExecutionNode>().ToList();
        Assert.Equal(3, skillNodes.Count);

        var executionIds = skillNodes.Select(n => n.SkillExecutionTask.ExecutionId).ToList();

        // All should have ExecutionIds
        Assert.All(executionIds, id => Assert.NotNull(id));

        // All should be unique
        var uniqueIds = executionIds.Distinct().Count();
        Assert.Equal(3, uniqueIds);
    }

    [Fact]
    public void AssignExecutionIds_WithTaskNode_ShouldReturnUnchanged()
    {
        // Task nodes don't get ExecutionIds (they're not executable)
        // Arrange
        var taskNode = CreateTaskNode();
        var nodes = new List<Node> { taskNode };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Single(result);
        Assert.IsType<TaskNode>(result[0]);
        Assert.Same(taskNode, result[0]);
    }

    [Fact]
    public void AssignExecutionIds_WithMixedNodes_ShouldOnlyAssignToSkillNodes()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode1 = CreateSkillExecutionNode();
        var skillNode2 = CreateSkillExecutionNode();
        var nodes = new List<Node> { taskNode, skillNode1, skillNode2 };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Equal(3, result.Count);

        var taskNodes = result.OfType<TaskNode>().ToList();
        var skillNodes = result.OfType<SkillExecutionNode>().ToList();

        Assert.Single(taskNodes);
        Assert.Equal(2, skillNodes.Count);

        // SkillNodes should have ExecutionIds
        Assert.All(skillNodes, node => Assert.NotNull(node.SkillExecutionTask.ExecutionId));
    }

    [Fact]
    public void AssignExecutionIds_WhenExecutionIdAlreadyExists_ShouldOverwrite()
    {
        // Arrange
        var existingExecutionId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(executionId: existingExecutionId);
        var nodes = new List<Node> { node };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        var skillNode = result[0] as SkillExecutionNode;
        Assert.NotNull(skillNode);
        Assert.NotNull(skillNode.SkillExecutionTask.ExecutionId);

        // Should have a NEW ExecutionId (different from existing)
        Assert.NotEqual(existingExecutionId, skillNode.SkillExecutionTask.ExecutionId.Value);
    }

    [Fact]
    public void AssignExecutionIds_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void AssignExecutionIds_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _assigner.AssignExecutionIds(null!));
    }

    #endregion

    #region Test: Return Value Properties

    [Fact]
    public void AssignExecutionIds_ShouldReturnReadOnlyList()
    {
        // Arrange
        var nodes = new List<Node> { CreateSkillExecutionNode() };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Node>>(result);
    }

    [Fact]
    public void AssignExecutionIds_ShouldPreserveNodeOrder()
    {
        // Arrange
        var node1 = CreateSkillExecutionNode(Guid.NewGuid());
        var node2 = CreateTaskNode(Guid.NewGuid());
        var node3 = CreateSkillExecutionNode(Guid.NewGuid());
        var nodes = new List<Node> { node1, node2, node3 };

        // Act
        var result = _assigner.AssignExecutionIds(nodes);

        // Assert
        Assert.Equal(node1.Id, result[0].Id);
        Assert.Equal(node2.Id, result[1].Id);
        Assert.Equal(node3.Id, result[2].Id);
    }

    #endregion
}