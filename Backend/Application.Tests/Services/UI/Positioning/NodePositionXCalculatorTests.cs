using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Options;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Tests for NodePositionXCalculator focusing on parent-child relative positioning.
/// </summary>
public class NodePositionXCalculatorTests
{
    private const double DefaultTimeToPixelScale = 100.0;
    private readonly NodePositionXCalculator _calculator;

    public NodePositionXCalculatorTests()
    {
        var schedulingConfig = new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = DefaultTimeToPixelScale
            }
        };
        var options = Options.Create(schedulingConfig);
        _calculator = new NodePositionXCalculator(options);
    }

    [Fact]
    public void CalculateXPosition_NodeWithoutParent_ShouldUseAbsoluteStartTime()
    {
        // Arrange
        var node = CreateTaskNode(Guid.NewGuid(), null, new NodePosition { X = 0, Y = 0 });
        var timing = new NodeTimingInfo
        {
            AbsoluteStartTime = 5.0,
            RelativeStartTime = 5.0, // Should equal absolute for root nodes
            Duration = 3.0
        };

        // Act
        var xPosition = _calculator.CalculateXPosition(node, timing);

        // Assert
        Assert.Equal(500.0, xPosition); // 5.0 * 100.0 scale
    }

    [Fact]
    public void CalculateXPosition_NodeWithParent_ShouldUseRelativeStartTime()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childNode = CreateTaskNode(Guid.NewGuid(), parentId, new NodePosition { X = 0, Y = 0 });

        // Child starts 2 seconds after its parent starts (parent starts at 5.0, child at 7.0)
        var childTiming = new NodeTimingInfo
        {
            AbsoluteStartTime = 7.0,
            RelativeStartTime = 2.0, // Relative to parent's start time
            Duration = 3.0
        };

        // Act
        var xPosition = _calculator.CalculateXPosition(childNode, childTiming);

        // Assert
        Assert.Equal(200.0, xPosition); // 2.0 * 100.0 scale
    }

    [Fact]
    public void CalculateXPosition_NodeWithZeroRelativeStartTime_ShouldReturnZero()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childNode = CreateTaskNode(Guid.NewGuid(), parentId, new NodePosition { X = 0, Y = 0 });

        // Child starts at the same time as its parent
        var childTiming = new NodeTimingInfo
        {
            AbsoluteStartTime = 5.0,
            RelativeStartTime = 0.0, // Starts exactly when parent starts
            Duration = 3.0
        };

        // Act
        var xPosition = _calculator.CalculateXPosition(childNode, childTiming);

        // Assert
        Assert.Equal(0.0, xPosition);
    }

    [Fact]
    public void CalculateXPositions_MultipleNodesWithHierarchy_ShouldCalculateCorrectRelativePositions()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId, null, new NodePosition { X = 0, Y = 0 });
        var child1Node = CreateSkillExecutionNode(child1Id, parentId, new NodePosition { X = 0, Y = 0 });
        var child2Node = CreateSkillExecutionNode(child2Id, parentId, new NodePosition { X = 0, Y = 0 });

        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [parentId] = (parentNode, new NodeTimingInfo
            {
                AbsoluteStartTime = 10.0,
                RelativeStartTime = 10.0, // Root node
                Duration = 8.0
            }),
            [child1Id] = (child1Node, new NodeTimingInfo
            {
                AbsoluteStartTime = 12.0,
                RelativeStartTime = 2.0, // 2 seconds after parent starts
                Duration = 3.0
            }),
            [child2Id] = (child2Node, new NodeTimingInfo
            {
                AbsoluteStartTime = 15.0,
                RelativeStartTime = 5.0, // 5 seconds after parent starts
                Duration = 2.0
            })
        };

        // Act
        var xPositions = _calculator.CalculateXPositions(nodesWithTiming);

        // Assert
        Assert.Equal(3, xPositions.Count);
        Assert.Equal(1000.0, xPositions[parentId]); // 10.0 * 100.0
        Assert.Equal(200.0, xPositions[child1Id]); // 2.0 * 100.0 (relative to parent)
        Assert.Equal(500.0, xPositions[child2Id]); // 5.0 * 100.0 (relative to parent)
    }

    [Fact]
    public void TimeToPixelScale_CustomScale_ShouldAffectCalculation()
    {
        // Arrange
        var customScaleConfig = new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 50.0 // Custom scale
            }
        };
        var customOptions = Options.Create(customScaleConfig);
        var customCalculator = new NodePositionXCalculator(customOptions);

        var node = CreateTaskNode(Guid.NewGuid(), null, new NodePosition { X = 0, Y = 0 });
        var timing = new NodeTimingInfo
        {
            AbsoluteStartTime = 4.0,
            RelativeStartTime = 4.0,
            Duration = 2.0
        };

        // Act
        var xPosition = customCalculator.CalculateXPosition(node, timing);

        // Assert
        Assert.Equal(200.0, xPosition); // 4.0 * 50.0 scale
    }

    [Fact]
    public void CalculateXPositions_EmptyInput_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var emptyInput = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>();

        // Act
        var xPositions = _calculator.CalculateXPositions(emptyInput);

        // Assert
        Assert.Empty(xPositions);
    }

    private static TaskNode CreateTaskNode(Guid id, Guid? parentId, NodePosition position)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = position,
            Task = new Task
            {
                Name = $"Task-{id:N}"[..8],
                StartTime = 0.0,
                Duration = 1.0
            }
        };
    }

    [Fact]
    public void CalculateXPositions_TaskNodeParentWithScheduledChild_ShouldPositionParentAndChildCorrectly()
    {
        // Arrange - Reproducing the user's scenario:
        // Task "3" should be at X=70 (based on scheduled child start time)
        // Skill "2323" should be at X=0 (relative to parent)
        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = CreateTaskNode(taskId, null, new NodePosition { X = 0, Y = 110 });
        var skillNode = CreateSkillExecutionNode(skillId, taskId, new NodePosition { X = 70, Y = 40 });

        // The skill execution was scheduled to start at time 70 (after first task finished)
        // The parent task should inherit this position
        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [taskId] = (taskNode, new NodeTimingInfo
            {
                AbsoluteStartTime = 70.0, // Task should start when its child starts
                RelativeStartTime = 70.0, // Root node, so relative = absolute
                Duration = 70.0
            }),
            [skillId] = (skillNode, new NodeTimingInfo
            {
                AbsoluteStartTime = 70.0, // Scheduled after first task
                RelativeStartTime = 0.0, // Should be 0 relative to parent (starts when parent starts)
                Duration = 70.0
            })
        };

        // Act
        var xPositions = _calculator.CalculateXPositions(nodesWithTiming);

        // Assert
        Assert.Equal(2, xPositions.Count);

        // Parent task should be positioned at scheduled time (70 * scale = 7000)
        Assert.Equal(7000.0, xPositions[taskId]); // 70.0 * 100.0 scale

        // Child skill should be positioned relative to parent (0 * scale = 0)
        Assert.Equal(0.0, xPositions[skillId]); // 0.0 * 100.0 scale
    }

    private static SkillExecutionNode CreateSkillExecutionNode(Guid id, Guid? parentId, NodePosition position)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = position,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"Skill-{id:N}"[..8],
                StartTime = 0.0,
                Duration = 1.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill for positioning",
                    Properties = []
                }
            }
        };
    }
}