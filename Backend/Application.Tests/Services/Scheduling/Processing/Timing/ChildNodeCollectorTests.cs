using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Timing;

/// <summary>
///     Unit tests for ChildNodeCollector service.
/// </summary>
public class ChildNodeCollectorTests
{
    private readonly ChildNodeCollector _collector;

    public ChildNodeCollectorTests()
    {
        var mockLogger = new Mock<ILogger<ChildNodeCollector>>();

        _collector = new ChildNodeCollector(mockLogger.Object);
    }

    [Fact]
    public void CollectChildSkillNodes_WithValidParent_ReturnsCorrectChildren()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId1 = Guid.NewGuid();
        var childId2 = Guid.NewGuid();
        var nonChildId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, "Parent"),
            CreateSkillExecutionNode(childId1, "Child1", parentId),
            CreateSkillExecutionNode(childId2, "Child2", parentId),
            CreateSkillExecutionNode(nonChildId, "NonChild", Guid.NewGuid())
        }.AsReadOnly();

        // Act
        var result = _collector.CollectChildSkillNodes(parentId, nodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, n => n.Id == childId1);
        Assert.Contains(result, n => n.Id == childId2);
        Assert.DoesNotContain(result, n => n.Id == nonChildId);
    }

    [Fact]
    public void CollectChildTaskNodes_WithValidParent_ReturnsCorrectChildren()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId1 = Guid.NewGuid();
        var childId2 = Guid.NewGuid();
        var nonChildId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, "Parent"),
            CreateTaskNode(childId1, "Child1", parentId),
            CreateTaskNode(childId2, "Child2", parentId),
            CreateTaskNode(nonChildId, "NonChild", Guid.NewGuid())
        }.AsReadOnly();

        // Act
        var result = _collector.CollectChildTaskNodes(parentId, nodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, n => n.Id == childId1);
        Assert.Contains(result, n => n.Id == childId2);
        Assert.DoesNotContain(result, n => n.Id == nonChildId);
    }

    [Fact]
    public void CollectAllChildNodes_WithMixedChildren_ReturnsCorrectSeparation()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, "Parent"),
            CreateSkillExecutionNode(skillId1, "Skill1", parentId),
            CreateSkillExecutionNode(skillId2, "Skill2", parentId),
            CreateTaskNode(taskId1, "ChildTask", parentId)
        }.AsReadOnly();

        // Act
        var (skillNodes, taskNodes, routerNodes) = _collector.CollectAllChildNodes(parentId, nodes);

        // Assert
        Assert.Equal(2, skillNodes.Count);
        Assert.Single(taskNodes);
        Assert.Empty(routerNodes);
        Assert.Contains(skillNodes, n => n.Id == skillId1);
        Assert.Contains(skillNodes, n => n.Id == skillId2);
        Assert.Contains(taskNodes, n => n.Id == taskId1);
    }

    [Fact]
    public void CollectChildSkillNodes_WithNoChildren_ReturnsEmptyList()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, "Parent"),
            CreateSkillExecutionNode(Guid.NewGuid(), "NonChild", Guid.NewGuid())
        }.AsReadOnly();

        // Act
        var result = _collector.CollectChildSkillNodes(parentId, nodes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CollectChildSkillNodes_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _collector.CollectChildSkillNodes(parentId, null!));
    }

    [Fact]
    public void CollectAllChildNodes_WithEmptyNodeList_ReturnsEmptyLists()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var nodes = new List<Node>().AsReadOnly();

        // Act
        var (skillNodes, taskNodes, routerNodes) = _collector.CollectAllChildNodes(parentId, nodes);

        // Assert
        Assert.Empty(skillNodes);
        Assert.Empty(taskNodes);
        Assert.Empty(routerNodes);
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

    private static RouterNode CreateRouterNode(Guid id, Guid branchTargetId, Guid? parentId = null)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = $"Router_{id.ToString()[..4]}",
                StartTime = 0,
                Duration = 5.0,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", TargetNodeId = branchTargetId, Priority = 0 }
                }
            }
        };
    }

    #region Nested Router Tests — RouterNode children are invisible to ChildNodeCollector

    [Fact]
    public void CollectAllChildNodes_TaskNodeWithRouterChild_MustIncludeRouterNode()
    {
        // A TaskNode (branch task of an outer router) contains a nested RouterNode as child.
        // ChildNodeCollector must find this RouterNode child so parent durations are correct.
        var taskId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var routerBranchTargetId = Guid.NewGuid();

        var taskNode = CreateTaskNode(taskId, "BranchTask");
        var routerChild = CreateRouterNode(routerId, routerBranchTargetId, taskId);

        var nodes = new List<Node> { taskNode, routerChild }.AsReadOnly();

        // Act
        var (skillNodes, taskNodes, routerNodes) = _collector.CollectAllChildNodes(taskId, nodes);

        // Assert — RouterNode child must be returned via the routerNodes component
        var allFoundIds = skillNodes.Select(n => n.Id)
            .Concat(taskNodes.Select(n => n.Id))
            .Concat(routerNodes.Select(n => n.Id))
            .ToList();
        allFoundIds.Should().Contain(routerId,
            "RouterNode child must be found by CollectAllChildNodes, otherwise the parent " +
            "TaskNode's duration will be underestimated because the nested router's timing is invisible");
    }

    [Fact]
    public void CollectChildSkillNodes_DoesNotReturnRouterNodes()
    {
        // Verify that CollectChildSkillNodes does not return RouterNode children
        // (it only returns SkillExecutionNode type).
        var parentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTargetId = Guid.NewGuid();

        var parentTask = CreateTaskNode(parentId, "Parent");
        var routerChild = CreateRouterNode(routerId, branchTargetId, parentId);

        var nodes = new List<Node> { parentTask, routerChild }.AsReadOnly();

        // Act
        var result = _collector.CollectChildSkillNodes(parentId, nodes);

        // Assert
        result.Should().BeEmpty("CollectChildSkillNodes uses .OfType<SkillExecutionNode>() which excludes RouterNode");
    }

    [Fact]
    public void CollectChildTaskNodes_DoesNotReturnRouterNodes()
    {
        // Verify that CollectChildTaskNodes does not return RouterNode children
        // (it only returns TaskNode type).
        var parentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTargetId = Guid.NewGuid();

        var parentTask = CreateTaskNode(parentId, "Parent");
        var routerChild = CreateRouterNode(routerId, branchTargetId, parentId);

        var nodes = new List<Node> { parentTask, routerChild }.AsReadOnly();

        // Act
        var result = _collector.CollectChildTaskNodes(parentId, nodes);

        // Assert
        result.Should().BeEmpty("CollectChildTaskNodes uses .OfType<TaskNode>() which excludes RouterNode");
    }

    [Fact]
    public void CollectAllChildNodes_TaskNodeWithMixedChildrenIncludingRouter_MustFindAll()
    {
        // A TaskNode has a SkillExecutionNode child, a TaskNode child, AND a RouterNode child.
        // All three must be found for accurate duration calculation.
        var parentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var childTaskId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var routerBranchTargetId = Guid.NewGuid();

        var parentTask = CreateTaskNode(parentId, "Parent");
        var skillChild = CreateSkillExecutionNode(skillId, "Skill1", parentId);
        var taskChild = CreateTaskNode(childTaskId, "ChildTask", parentId);
        var routerChild = CreateRouterNode(routerId, routerBranchTargetId, parentId);

        var nodes = new List<Node> { parentTask, skillChild, taskChild, routerChild }.AsReadOnly();

        // Act
        var (skillNodes, taskNodes, routerNodes) = _collector.CollectAllChildNodes(parentId, nodes);

        // Assert — all three child types are found
        skillNodes.Should().HaveCount(1);
        taskNodes.Should().HaveCount(1);
        routerNodes.Should().HaveCount(1);

        // The total children found must include the router
        var totalChildrenFound = skillNodes.Count + taskNodes.Count + routerNodes.Count;
        totalChildrenFound.Should().Be(3,
            "All three child types (SkillExecutionNode, TaskNode, RouterNode) must be found");
    }

    #endregion
}