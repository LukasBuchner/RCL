using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Tests for NodeTimingMapper to ensure correct mapping of timing information
///     from execution results back to domain nodes.
/// </summary>
public class NodeTimingMapperTests
{
    private readonly Mock<ILogger<NodeTimingMapper>> _loggerMock;
    private readonly NodeTimingMapper _mapper;

    public NodeTimingMapperTests()
    {
        _loggerMock = new Mock<ILogger<NodeTimingMapper>>();
        _mapper = new NodeTimingMapper(_loggerMock.Object);
    }

    #region Test: Multiple Nodes

    [Fact]
    public void ApplyTimingToNode_WithMultipleSkillNodes_ShouldApplyIndependently()
    {
        // Testing that each node gets its own timing independently
        // Arrange
        var node1 = CreateSkillExecutionNode(Guid.NewGuid());
        var node2 = CreateSkillExecutionNode(Guid.NewGuid(), 20, 15);

        var timingInfo1 = new Dictionary<Guid, NodeTimingInfo>
        {
            [node1.Id] = CreateTimingInfo(50, 30)
        };

        var timingInfo2 = new Dictionary<Guid, NodeTimingInfo>
        {
            [node2.Id] = CreateTimingInfo(100, 45)
        };

        var durations = new Dictionary<Guid, double>
        {
            [node1.Id] = 30,
            [node2.Id] = 45
        };

        // Act
        var result1 = _mapper.ApplyTimingToNode(node1, timingInfo1, durations);
        var result2 = _mapper.ApplyTimingToNode(node2, timingInfo2, durations);

        // Assert
        var skill1 = (SkillExecutionNode)result1;
        var skill2 = (SkillExecutionNode)result2;

        Assert.Equal(50.0, skill1.SkillExecutionTask.StartTime);
        Assert.Equal(30.0, skill1.SkillExecutionTask.Duration);

        Assert.Equal(100.0, skill2.SkillExecutionTask.StartTime);
        Assert.Equal(45.0, skill2.SkillExecutionTask.Duration);
    }

    #endregion

    #region Test: Immutability

    [Fact]
    public void ApplyTimingToNode_ShouldNotModifyOriginalNode()
    {
        // Nodes are records (immutable), so applying timing should return a new instance
        // Arrange
        var nodeId = Guid.NewGuid();
        var originalNode = CreateSkillExecutionNode(
            nodeId,
            10,
            20,
            30
        );

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(100, 50)
        };

        var durations = new Dictionary<Guid, double> { [nodeId] = 50 };

        // Act
        var result = _mapper.ApplyTimingToNode(originalNode, timingInfo, durations);

        // Assert
        // Original should be unchanged
        Assert.Equal(10.0, originalNode.SkillExecutionTask.StartTime);
        Assert.Equal(20.0, originalNode.SkillExecutionTask.Duration);
        Assert.Equal(30.0, originalNode.SkillExecutionTask.FinishTime);

        // Result should have new values
        var resultNode = (SkillExecutionNode)result;
        Assert.Equal(100.0, resultNode.SkillExecutionTask.StartTime);
        Assert.Equal(50.0, resultNode.SkillExecutionTask.Duration);
        Assert.Equal(150.0, resultNode.SkillExecutionTask.FinishTime);

        // Should be different instances
        Assert.NotSame(originalNode, result);
    }

    #endregion

    #region Helper Methods

    private static SkillExecutionNode CreateSkillExecutionNode(
        Guid? id = null,
        double startTime = 0,
        double duration = 10,
        double? finishTime = null)
    {
        var nodeId = id ?? Guid.NewGuid();
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };

        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "TestTask",
                StartTime = startTime,
                Duration = duration,
                FinishTime = finishTime ?? startTime + duration,
                Skill = skill,
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static TaskNode CreateTaskNode(
        Guid? id = null,
        double startTime = 0,
        double duration = 10)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "TestTask",
                StartTime = startTime,
                Duration = duration,
                FinishTime = startTime + duration
            }
        };
    }

    private static NodeTimingInfo CreateTimingInfo(
        double absoluteStartTime,
        double duration,
        NodeTimingType nodeType = NodeTimingType.SkillExecution)
    {
        return new NodeTimingInfo
        {
            AbsoluteStartTime = absoluteStartTime,
            AbsoluteFinishTime = absoluteStartTime + duration,
            Duration = duration,
            RelativeStartTime = absoluteStartTime,
            RelativeFinishTime = absoluteStartTime + duration,
            NodeType = nodeType
        };
    }

    #endregion

    #region Test: Basic Timing Application

    [Fact]
    public void ApplyTimingToNode_WithSkillExecutionNode_ShouldUpdateTimingFields()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(
            nodeId,
            0,
            10,
            10
        );

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(50, 30)
        };

        var durations = new Dictionary<Guid, double>
        {
            [nodeId] = 30
        };

        // Act
        var result = _mapper.ApplyTimingToNode(node, timingInfo, durations);

        // Assert
        Assert.IsType<SkillExecutionNode>(result);
        var skillNode = (SkillExecutionNode)result;
        Assert.Equal(50.0, skillNode.SkillExecutionTask.StartTime);
        Assert.Equal(30.0, skillNode.SkillExecutionTask.Duration);
        Assert.Equal(80.0, skillNode.SkillExecutionTask.FinishTime);
    }

    [Fact]
    public void ApplyTimingToNode_WithTaskNode_ShouldUpdateTimingFields()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateTaskNode(
            nodeId
        );

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(100, 50, NodeTimingType.Task)
        };

        var durations = new Dictionary<Guid, double>
        {
            [nodeId] = 50
        };

        // Act
        var result = _mapper.ApplyTimingToNode(node, timingInfo, durations);

        // Assert
        Assert.IsType<TaskNode>(result);
        var taskNode = (TaskNode)result;
        Assert.Equal(100.0, taskNode.Task.StartTime);
        Assert.Equal(50.0, taskNode.Task.Duration);
        Assert.Equal(150.0, taskNode.Task.FinishTime);
    }

    [Fact]
    public void ApplyTimingToNode_WithoutTimingInfo_ShouldApplyDurationOnly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(
            nodeId,
            20,
            10,
            30
        );

        // No timing info, but we have duration
        var durations = new Dictionary<Guid, double>
        {
            [nodeId] = 40
        };

        // Act
        var result = _mapper.ApplyTimingToNode(node, null, durations);

        // Assert
        var skillNode = (SkillExecutionNode)result;
        Assert.Equal(20.0, skillNode.SkillExecutionTask.StartTime); // Preserved
        Assert.Equal(40.0, skillNode.SkillExecutionTask.Duration); // Updated
        Assert.Equal(60.0, skillNode.SkillExecutionTask.FinishTime); // Recalculated
    }

    [Fact]
    public void ApplyTimingToNode_WithNoData_ShouldReturnUnchangedNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(
            nodeId,
            15,
            25,
            40
        );

        var durations = new Dictionary<Guid, double>(); // Empty

        // Act
        var result = _mapper.ApplyTimingToNode(node, null, durations);

        // Assert
        var skillNode = (SkillExecutionNode)result;
        Assert.Equal(15.0, skillNode.SkillExecutionTask.StartTime);
        Assert.Equal(25.0, skillNode.SkillExecutionTask.Duration);
        Assert.Equal(40.0, skillNode.SkillExecutionTask.FinishTime);
    }

    #endregion

    #region Test: Timing Consistency

    [Fact]
    public void ApplyTimingToNode_ShouldMaintainFinishTimeConsistency()
    {
        // FinishTime should always equal StartTime + Duration
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(nodeId);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(75, 42)
        };

        var durations = new Dictionary<Guid, double> { [nodeId] = 42 };

        // Act
        var result = _mapper.ApplyTimingToNode(node, timingInfo, durations);

        // Assert
        var skillNode = (SkillExecutionNode)result;
        var expectedFinish = skillNode.SkillExecutionTask.StartTime + skillNode.SkillExecutionTask.Duration;
        Assert.Equal(expectedFinish, skillNode.SkillExecutionTask.FinishTime);
    }

    [Fact]
    public void ApplyTimingToNode_WithVerySmallDuration_ShouldHandleCorrectly()
    {
        // Test with sub-second precision
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(nodeId);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(10.5, 0.123)
        };

        var durations = new Dictionary<Guid, double> { [nodeId] = 0.123 };

        // Act
        var result = _mapper.ApplyTimingToNode(node, timingInfo, durations);

        // Assert
        var skillNode = (SkillExecutionNode)result;
        Assert.Equal(10.5, skillNode.SkillExecutionTask.StartTime);
        Assert.Equal(0.123, skillNode.SkillExecutionTask.Duration);
        Assert.Equal(10.623, skillNode.SkillExecutionTask.FinishTime!.Value, 6);
    }

    [Fact]
    public void ApplyTimingToNode_WithLargeDuration_ShouldHandleCorrectly()
    {
        // Test with very long durations (hours)
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(nodeId);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = CreateTimingInfo(0, 7200) // 2 hours
        };

        var durations = new Dictionary<Guid, double> { [nodeId] = 7200 };

        // Act
        var result = _mapper.ApplyTimingToNode(node, timingInfo, durations);

        // Assert
        var skillNode = (SkillExecutionNode)result;
        Assert.Equal(0.0, skillNode.SkillExecutionTask.StartTime);
        Assert.Equal(7200.0, skillNode.SkillExecutionTask.Duration);
        Assert.Equal(7200.0, skillNode.SkillExecutionTask.FinishTime);
    }

    #endregion

    #region Test: Relative Time Adjustments

    [Fact]
    public void AdjustRelativeStartTimesForHierarchy_WithRootNode_ShouldSetRelativeEqualToAbsolute()
    {
        // Root nodes (no parent): RelativeStartTime = AbsoluteStartTime
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(nodeId);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = new()
            {
                AbsoluteStartTime = 50,
                AbsoluteFinishTime = 100,
                Duration = 50,
                RelativeStartTime = 999, // Wrong value, should be corrected
                RelativeFinishTime = 999,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var nodes = new List<Node> { node };

        // Act
        _mapper.AdjustRelativeStartTimesForHierarchy(timingInfo, nodes);

        // Assert
        Assert.Equal(50.0, timingInfo[nodeId].RelativeStartTime);
        Assert.Equal(100.0, timingInfo[nodeId].RelativeFinishTime);
    }

    [Fact]
    public void AdjustRelativeStartTimesForHierarchy_WithChildNode_ShouldCalculateRelativeToParent()
    {
        // Child nodes: RelativeStartTime = AbsoluteStartTime - ParentAbsoluteStartTime
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId);
        var childNode = CreateSkillExecutionNode(childId);
        childNode = childNode with { ParentId = parentId };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 100,
                AbsoluteFinishTime = 200,
                Duration = 100,
                RelativeStartTime = 100,
                RelativeFinishTime = 200,
                NodeType = NodeTimingType.Task
            },
            [childId] = new()
            {
                AbsoluteStartTime = 125,
                AbsoluteFinishTime = 175,
                Duration = 50,
                RelativeStartTime = 999, // Should be corrected
                RelativeFinishTime = 999,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var nodes = new List<Node> { parentNode, childNode };

        // Act
        _mapper.AdjustRelativeStartTimesForHierarchy(timingInfo, nodes);

        // Assert
        // Child's relative start = 125 - 100 = 25
        Assert.Equal(25.0, timingInfo[childId].RelativeStartTime);
        Assert.Equal(75.0, timingInfo[childId].RelativeFinishTime); // 25 + 50
    }

    [Fact]
    public void AdjustRelativeStartTimesForHierarchy_WithNestedHierarchy_ShouldHandleDeepNesting()
    {
        // Test: GrandParent -> Parent -> Child
        // Arrange
        var grandParentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var grandParentNode = CreateTaskNode(grandParentId);
        var parentNode = CreateTaskNode(parentId);
        parentNode = parentNode with { ParentId = grandParentId };
        var childNode = CreateSkillExecutionNode(childId);
        childNode = childNode with { ParentId = parentId };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [grandParentId] = CreateTimingInfo(0, 300, NodeTimingType.Task),
            [parentId] = CreateTimingInfo(50, 200, NodeTimingType.Task),
            [childId] = CreateTimingInfo(100, 50)
        };

        var nodes = new List<Node> { grandParentNode, parentNode, childNode };

        // Act
        _mapper.AdjustRelativeStartTimesForHierarchy(timingInfo, nodes);

        // Assert
        // GrandParent (root): Relative = 0
        Assert.Equal(0.0, timingInfo[grandParentId].RelativeStartTime);

        // Parent (relative to GrandParent): 50 - 0 = 50
        Assert.Equal(50.0, timingInfo[parentId].RelativeStartTime);

        // Child (relative to Parent): 100 - 50 = 50
        Assert.Equal(50.0, timingInfo[childId].RelativeStartTime);
    }

    #endregion

    #region Test: Edge Cases

    [Fact]
    public void ApplyTimingToNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        var durations = new Dictionary<Guid, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.ApplyTimingToNode(null!, null, durations));
    }

    [Fact]
    public void ApplyTimingToNode_WithNullDurations_ShouldThrowArgumentNullException()
    {
        // Arrange
        var node = CreateSkillExecutionNode();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.ApplyTimingToNode(node, null, null!));
    }

    [Fact]
    public void AdjustRelativeStartTimesForHierarchy_WithNullTimingInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.AdjustRelativeStartTimesForHierarchy(null!, nodes));
    }

    [Fact]
    public void AdjustRelativeStartTimesForHierarchy_WithNullNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.AdjustRelativeStartTimesForHierarchy(timingInfo, null!));
    }

    #endregion
}