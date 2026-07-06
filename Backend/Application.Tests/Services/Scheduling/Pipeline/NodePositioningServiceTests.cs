using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

public class NodePositioningServiceTests
{
    private readonly Mock<INodeHeightCalculator> _mockHeightCalculator;
    private readonly Mock<INodeWidthCalculator> _mockWidthCalculator;
    private readonly Mock<INodePositionXCalculator> _mockXCalculator;
    private readonly Mock<INodePositionYCalculator> _mockYCalculator;
    private readonly INodePositioningService _service;

    public NodePositioningServiceTests()
    {
        _mockXCalculator = new Mock<INodePositionXCalculator>();
        _mockYCalculator = new Mock<INodePositionYCalculator>();
        _mockHeightCalculator = new Mock<INodeHeightCalculator>();
        _mockWidthCalculator = new Mock<INodeWidthCalculator>();
        var mockLogger = new Mock<ILogger<NodePositioningService>>();

        // Setup default return value for width calculator
        _mockWidthCalculator.Setup(x => x.CalculateNodeWidths(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        _service = new NodePositioningService(
            _mockXCalculator.Object,
            _mockYCalculator.Object,
            _mockHeightCalculator.Object,
            _mockWidthCalculator.Object,
            mockLogger.Object);
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>();
        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyPositionsAndHeights(null!, timingInfo, parentMapping));
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithNullParentMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var nodes = new List<Node>();
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyPositionsAndHeights(nodes, timingInfo, null!));
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithNullTimingInfo_ReturnsNodesUnchanged()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode = CreateSkillExecutionNode();
        var nodes = new List<Node> { taskNode, skillNode };
        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, null, parentMapping);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(taskNode, result);
        Assert.Contains(skillNode, result);

        // Verify no calculator methods were called
        _mockXCalculator.Verify(
            x => x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()),
            Times.Never);
        _mockYCalculator.Verify(
            x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()), Times.Never);
        _mockWidthCalculator.Verify(
            x => x.CalculateNodeWidths(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Never);
        _mockHeightCalculator.Verify(
            x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Never);
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithEmptyNodes_ReturnsEmptyList()
    {
        // Arrange
        var nodes = new List<Node>();
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100 } }
        };
        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(new Dictionary<Guid, double>());
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(new Dictionary<Guid, double>());
        _mockWidthCalculator.Setup(x => x.CalculateNodeWidths(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithTimingInfo_CalculatesXPositions()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode = CreateSkillExecutionNode();
        var nodes = new List<Node> { taskNode, skillNode };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                skillNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        var xPositions = new Dictionary<Guid, double>
        {
            { taskNode.Id, 150.5 },
            { skillNode.Id, 250.5 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(xPositions);
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(new Dictionary<Guid, double>());
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(2, result.Count);
        var resultTaskNode = result.OfType<TaskNode>().FirstOrDefault(n => n.Id == taskNode.Id);
        var resultSkillNode = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode.Id);

        Assert.NotNull(resultTaskNode);
        Assert.NotNull(resultSkillNode);
        Assert.Equal(150.5, resultTaskNode.Position.X);
        Assert.Equal(250.5, resultSkillNode.Position.X);

        _mockXCalculator.Verify(
            x => x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()),
            Times.Once);
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithHierarchy_CalculatesYPositions()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode = CreateSkillExecutionNode();
        skillNode.ParentId = taskNode.Id;

        var nodes = new List<Node> { taskNode, skillNode };
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                skillNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { taskNode.Id, new List<Node> { skillNode } }
        };

        var yPositions = new Dictionary<Guid, double>
        {
            { taskNode.Id, 100 },
            { skillNode.Id, 200 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(new Dictionary<Guid, double>());
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(yPositions);
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(2, result.Count);
        var resultTaskNode = result.OfType<TaskNode>().FirstOrDefault(n => n.Id == taskNode.Id);
        var resultSkillNode = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode.Id);

        Assert.NotNull(resultTaskNode);
        Assert.NotNull(resultSkillNode);
        Assert.Equal(100, resultTaskNode.Position.Y);
        Assert.Equal(200, resultSkillNode.Position.Y);

        _mockYCalculator.Verify(
            x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()), Times.Once);
    }

    [Fact]
    public void ApplyPositionsAndHeights_WithHierarchy_CalculatesContainerHeights()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode = CreateSkillExecutionNode();
        skillNode.ParentId = taskNode.Id;

        var nodes = new List<Node> { taskNode, skillNode };
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                skillNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { taskNode.Id, new List<Node> { skillNode } }
        };

        var heights = new Dictionary<Guid, double>
        {
            { taskNode.Id, 150 },
            { skillNode.Id, 50 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(new Dictionary<Guid, double>());
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(new Dictionary<Guid, double>());
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(heights);

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(2, result.Count);
        var resultTaskNode = result.OfType<TaskNode>().FirstOrDefault(n => n.Id == taskNode.Id);
        var resultSkillNode = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode.Id);

        Assert.NotNull(resultTaskNode);
        Assert.NotNull(resultSkillNode);
        Assert.Equal(150, resultTaskNode.Height);
        Assert.Equal(50, resultSkillNode.Height);

        _mockHeightCalculator.Verify(
            x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Once);
    }

    [Fact]
    public void ApplyPositionsAndHeights_PreservesNodesWithoutTimingInfo()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode1 = CreateSkillExecutionNode();
        var skillNode2 = CreateSkillExecutionNode();

        var nodes = new List<Node> { taskNode, skillNode1, skillNode2 };

        // Only provide timing info for some nodes
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                skillNode1.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            }
            // skillNode2 has no timing info
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        var xPositions = new Dictionary<Guid, double>
        {
            { taskNode.Id, 150 },
            { skillNode1.Id, 250 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(xPositions);
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(new Dictionary<Guid, double>());
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(3, result.Count);

        // Nodes with timing should have updated X positions
        var resultTaskNode = result.OfType<TaskNode>().FirstOrDefault(n => n.Id == taskNode.Id);
        var resultSkillNode1 = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode1.Id);
        Assert.NotNull(resultTaskNode);
        Assert.NotNull(resultSkillNode1);
        Assert.Equal(150, resultTaskNode.Position.X);
        Assert.Equal(250, resultSkillNode1.Position.X);

        // Node without timing should preserve original position
        var resultSkillNode2 = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode2.Id);
        Assert.NotNull(resultSkillNode2);
        Assert.Equal(skillNode2.Position.X, resultSkillNode2.Position.X);
    }

    [Fact]
    public void ApplyPositionsAndHeights_HandlesMixedTaskAndSkillNodes()
    {
        // Arrange
        var taskNode1 = CreateTaskNode();
        var taskNode2 = CreateTaskNode();
        var skillNode1 = CreateSkillExecutionNode();
        var skillNode2 = CreateSkillExecutionNode();

        skillNode1.ParentId = taskNode1.Id;
        skillNode2.ParentId = taskNode2.Id;

        var nodes = new List<Node> { taskNode1, taskNode2, skillNode1, skillNode2 };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode1.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                taskNode2.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 100, AbsoluteFinishTime = 200, Duration = 100, RelativeStartTime = 100 }
            },
            {
                skillNode1.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            },
            {
                skillNode2.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 110, AbsoluteFinishTime = 190, Duration = 80, RelativeStartTime = 110 }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { taskNode1.Id, new List<Node> { skillNode1 } },
            { taskNode2.Id, new List<Node> { skillNode2 } }
        };

        var xPositions = new Dictionary<Guid, double>
        {
            { taskNode1.Id, 50 },
            { taskNode2.Id, 150 },
            { skillNode1.Id, 50 },
            { skillNode2.Id, 150 }
        };

        var yPositions = new Dictionary<Guid, double>
        {
            { taskNode1.Id, 0 },
            { taskNode2.Id, 0 },
            { skillNode1.Id, 50 },
            { skillNode2.Id, 50 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(xPositions);
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(yPositions);
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(new Dictionary<Guid, double>());

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(2, result.OfType<TaskNode>().Count());
        Assert.Equal(2, result.OfType<SkillExecutionNode>().Count());

        // Verify positions are correctly applied
        foreach (var node in result)
        {
            if (xPositions.ContainsKey(node.Id)) Assert.Equal(xPositions[node.Id], node.Position.X);
            if (yPositions.ContainsKey(node.Id)) Assert.Equal(yPositions[node.Id], node.Position.Y);
        }
    }

    [Fact]
    public void ApplyPositionsAndHeights_UpdatesBothPositionAndHeightWhenApplicable()
    {
        // Arrange
        var taskNode = CreateTaskNode();
        var skillNode = CreateSkillExecutionNode();
        skillNode.ParentId = taskNode.Id;

        var nodes = new List<Node> { taskNode, skillNode };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                taskNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 0, AbsoluteFinishTime = 100, Duration = 100, RelativeStartTime = 0 }
            },
            {
                skillNode.Id,
                new NodeTimingInfo
                    { AbsoluteStartTime = 10, AbsoluteFinishTime = 90, Duration = 80, RelativeStartTime = 10 }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { taskNode.Id, new List<Node> { skillNode } }
        };

        var xPositions = new Dictionary<Guid, double>
        {
            { taskNode.Id, 100 },
            { skillNode.Id, 100 }
        };

        var yPositions = new Dictionary<Guid, double>
        {
            { taskNode.Id, 50 },
            { skillNode.Id, 100 }
        };

        var heights = new Dictionary<Guid, double>
        {
            { taskNode.Id, 120 },
            { skillNode.Id, 40 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(xPositions);
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(yPositions);
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(heights);

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Equal(2, result.Count);

        var resultTaskNode = result.OfType<TaskNode>().FirstOrDefault(n => n.Id == taskNode.Id);
        Assert.NotNull(resultTaskNode);
        Assert.Equal(100, resultTaskNode.Position.X);
        Assert.Equal(50, resultTaskNode.Position.Y);
        Assert.Equal(120, resultTaskNode.Height);

        var resultSkillNode = result.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == skillNode.Id);
        Assert.NotNull(resultSkillNode);
        Assert.Equal(100, resultSkillNode.Position.X);
        Assert.Equal(100, resultSkillNode.Position.Y);
        Assert.Equal(40, resultSkillNode.Height);
    }

    [Fact]
    public void ApplyPositionsAndHeights_RouterNode_UpdatesPositionAndHeight()
    {
        // Arrange
        var routerNode = CreateRouterNode();
        var nodes = new List<Node> { routerNode };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                routerNode.Id,
                new NodeTimingInfo
                {
                    AbsoluteStartTime = 50,
                    AbsoluteFinishTime = 100,
                    Duration = 50,
                    RelativeStartTime = 50
                }
            }
        };

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        var xPositions = new Dictionary<Guid, double>
        {
            { routerNode.Id, 300.0 }
        };

        var yPositions = new Dictionary<Guid, double>
        {
            { routerNode.Id, 150.0 }
        };

        var heights = new Dictionary<Guid, double>
        {
            { routerNode.Id, 80.0 }
        };

        _mockXCalculator.Setup(x =>
                x.CalculateXPositions(It.IsAny<IReadOnlyDictionary<Guid, (Node Node, NodeTimingInfo Timing)>>()))
            .Returns(xPositions);
        _mockYCalculator.Setup(x => x.CalculateYPositions(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>()))
            .Returns(yPositions);
        _mockHeightCalculator.Setup(x => x.CalculateNodeHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(heights);

        // Act
        var result = _service.ApplyPositionsAndHeights(nodes, timingInfo, parentMapping);

        // Assert
        Assert.Single(result);
        var resultRouterNode = result.OfType<RouterNode>().FirstOrDefault(n => n.Id == routerNode.Id);

        Assert.NotNull(resultRouterNode);
        Assert.Equal(300.0, resultRouterNode.Position.X);
        Assert.Equal(150.0, resultRouterNode.Position.Y);
        Assert.Equal(80.0, resultRouterNode.Height);
    }

    // Helper methods
    private static TaskNode CreateTaskNode()
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Height = 50,
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 100,
                FinishTime = 100
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode()
    {
        var skillId = Guid.NewGuid();
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Height = 40,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                StartTime = 0,
                Duration = 100,
                FinishTime = 100,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = skillId,
                    Name = "Test Skill",
                    Description = "Test Description",
                    Properties = new List<TypedProperty>()
                }
            }
        };
    }

    private static RouterNode CreateRouterNode()
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Height = 60,
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 50,
                FinishTime = 50,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Branch A",
                        Condition = "condition1",
                        TargetNodeId = Guid.NewGuid()
                    }
                }
            }
        };
    }
}