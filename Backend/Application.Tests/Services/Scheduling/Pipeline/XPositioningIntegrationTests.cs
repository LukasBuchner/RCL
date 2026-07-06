using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

public class XPositioningIntegrationTests
{
    private readonly NodeHierarchyProcessor _hierarchyProcessor;
    private readonly NullLogger<NodeHierarchyProcessor> _logger;
    private readonly NodePositionXCalculator _positionCalculator;

    public XPositioningIntegrationTests()
    {
        var schedulingConfig = new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 100.0
            }
        };
        var options = Options.Create(schedulingConfig);
        _positionCalculator = new NodePositionXCalculator(options);
        _logger = new NullLogger<NodeHierarchyProcessor>();
        _hierarchyProcessor = new NodeHierarchyProcessor(
            Mock.Of<INodeRelationshipMapper>(),
            Mock.Of<IHierarchyValidator>(),
            _logger);
    }

    [Fact]
    public void IntegratedPipeline_SimpleParentChildHierarchy_ShouldPositionCorrectly()
    {
        // Arrange: Parent task with child skill execution
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId, null);
        var childNode = CreateSkillExecutionNode(childId, parentId);

        // Simulate timing calculation results
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 10.0,
                RelativeStartTime = 10.0, // Root node: relative equals absolute
                Duration = 8.0
            },
            [childId] = new()
            {
                AbsoluteStartTime = 12.0,
                RelativeStartTime = 2.0, // Child: relative to parent (12.0 - 10.0 = 2.0)
                Duration = 3.0
            }
        };

        // Prepare input for position calculator
        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [parentId] = (parentNode, timingInfo[parentId]),
            [childId] = (childNode, timingInfo[childId])
        };

        // Act: Calculate X positions
        var xPositions = _positionCalculator.CalculateXPositions(nodesWithTiming);

        // Assert: Verify positioning
        Assert.Equal(2, xPositions.Count);

        // Parent should be positioned based on its absolute start time (10.0)
        Assert.Equal(1000.0, xPositions[parentId]); // 10.0 * 100.0 scale

        // Child should be positioned based on its relative start time (2.0)
        Assert.Equal(200.0, xPositions[childId]); // 2.0 * 100.0 scale (relative to parent)
    }

    [Fact]
    public void IntegratedPipeline_MultiLevelHierarchy_ShouldPositionCorrectly()
    {
        // Arrange: Grandparent -> Parent -> Child hierarchy
        var grandparentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var grandparentNode = CreateTaskNode(grandparentId, null);
        var parentNode = CreateTaskNode(parentId, grandparentId);
        var childNode = CreateSkillExecutionNode(childId, parentId);

        // Simulate absolute timing: Grandparent(0-15), Parent(3-10), Child(5-8)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [grandparentId] = new()
            {
                AbsoluteStartTime = 0.0,
                RelativeStartTime = 0.0, // Root
                Duration = 15.0
            },
            [parentId] = new()
            {
                AbsoluteStartTime = 3.0,
                RelativeStartTime = 3.0, // 3.0 - 0.0 = 3.0 (relative to grandparent)
                Duration = 7.0
            },
            [childId] = new()
            {
                AbsoluteStartTime = 5.0,
                RelativeStartTime = 2.0, // 5.0 - 3.0 = 2.0 (relative to parent)
                Duration = 3.0
            }
        };

        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [grandparentId] = (grandparentNode, timingInfo[grandparentId]),
            [parentId] = (parentNode, timingInfo[parentId]),
            [childId] = (childNode, timingInfo[childId])
        };

        // Act
        var xPositions = _positionCalculator.CalculateXPositions(nodesWithTiming);

        // Assert
        Assert.Equal(3, xPositions.Count);
        Assert.Equal(0.0, xPositions[grandparentId]); // 0.0 * 100.0 = 0.0
        Assert.Equal(300.0, xPositions[parentId]); // 3.0 * 100.0 = 300.0 (relative to grandparent)
        Assert.Equal(200.0, xPositions[childId]); // 2.0 * 100.0 = 200.0 (relative to parent)
    }

    [Fact]
    public void IntegratedPipeline_SiblingNodes_ShouldPositionRelativeToSameParent()
    {
        // Arrange: Parent with two child skill executions
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId, null);
        var child1Node = CreateSkillExecutionNode(child1Id, parentId);
        var child2Node = CreateSkillExecutionNode(child2Id, parentId);

        // Parent starts at 5.0, Child1 at 7.0 (rel: 2.0), Child2 at 10.0 (rel: 5.0)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 5.0,
                RelativeStartTime = 5.0,
                Duration = 8.0
            },
            [child1Id] = new()
            {
                AbsoluteStartTime = 7.0,
                RelativeStartTime = 2.0, // 7.0 - 5.0 = 2.0
                Duration = 2.0
            },
            [child2Id] = new()
            {
                AbsoluteStartTime = 10.0,
                RelativeStartTime = 5.0, // 10.0 - 5.0 = 5.0
                Duration = 2.0
            }
        };

        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [parentId] = (parentNode, timingInfo[parentId]),
            [child1Id] = (child1Node, timingInfo[child1Id]),
            [child2Id] = (child2Node, timingInfo[child2Id])
        };

        // Act
        var xPositions = _positionCalculator.CalculateXPositions(nodesWithTiming);

        // Assert
        Assert.Equal(3, xPositions.Count);
        Assert.Equal(500.0, xPositions[parentId]); // 5.0 * 100.0 = 500.0
        Assert.Equal(200.0, xPositions[child1Id]); // 2.0 * 100.0 = 200.0 (relative to parent)
        Assert.Equal(500.0, xPositions[child2Id]); // 5.0 * 100.0 = 500.0 (relative to parent)
    }

    [Fact]
    public void IntegratedPipeline_CustomTimeScale_ShouldAffectAllPositions()
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

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId, null);
        var childNode = CreateSkillExecutionNode(childId, parentId);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 4.0,
                RelativeStartTime = 4.0,
                Duration = 6.0
            },
            [childId] = new()
            {
                AbsoluteStartTime = 6.0,
                RelativeStartTime = 2.0, // 6.0 - 4.0 = 2.0
                Duration = 3.0
            }
        };

        var nodesWithTiming = new Dictionary<Guid, (Node Node, NodeTimingInfo Timing)>
        {
            [parentId] = (parentNode, timingInfo[parentId]),
            [childId] = (childNode, timingInfo[childId])
        };

        // Act
        var xPositions = customCalculator.CalculateXPositions(nodesWithTiming);

        // Assert
        Assert.Equal(200.0, xPositions[parentId]); // 4.0 * 50.0 = 200.0
        Assert.Equal(100.0, xPositions[childId]); // 2.0 * 50.0 = 100.0
    }

    [Fact]
    public void TimingCalculationEngine_AdjustRelativeStartTimes_ShouldCorrectTimingInfo()
    {
        // This test simulates what the TimingCalculationEngine.AdjustRelativeStartTimesForHierarchy method should do
        // Arrange: Create nodes with INCORRECT RelativeStartTime (all absolute)
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var parentNode = CreateTaskNode(parentId, null);
        var child1Node = CreateSkillExecutionNode(child1Id, parentId);
        var child2Node = CreateSkillExecutionNode(child2Id, parentId);

        var nodes = new List<Node> { parentNode, child1Node, child2Node };

        // BEFORE: All RelativeStartTime values are INCORRECT (set to absolute)
        var incorrectTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 10.0,
                RelativeStartTime = 10.0, // ROOT: Should stay 10.0
                Duration = 8.0
            },
            [child1Id] = new()
            {
                AbsoluteStartTime = 12.0,
                RelativeStartTime = 12.0, // WRONG: Should be 2.0 (12.0 - 10.0)
                Duration = 3.0
            },
            [child2Id] = new()
            {
                AbsoluteStartTime = 15.0,
                RelativeStartTime = 15.0, // WRONG: Should be 5.0 (15.0 - 10.0)
                Duration = 2.0
            }
        };

        // Act: Apply the correction logic (simulating AdjustRelativeStartTimesForHierarchy)
        var correctedTimingInfo = ApplyRelativeTimingCorrection(incorrectTimingInfo, nodes);

        // Assert: RelativeStartTime should now be correct
        Assert.Equal(10.0, correctedTimingInfo[parentId].RelativeStartTime); // Root: unchanged
        Assert.Equal(2.0, correctedTimingInfo[child1Id].RelativeStartTime); // 12.0 - 10.0 = 2.0
        Assert.Equal(5.0, correctedTimingInfo[child2Id].RelativeStartTime); // 15.0 - 10.0 = 5.0

        // AbsoluteStartTime should remain unchanged
        Assert.Equal(10.0, correctedTimingInfo[parentId].AbsoluteStartTime);
        Assert.Equal(12.0, correctedTimingInfo[child1Id].AbsoluteStartTime);
        Assert.Equal(15.0, correctedTimingInfo[child2Id].AbsoluteStartTime);
    }

    /// <summary>
    ///     Helper method that simulates the TimingCalculationEngine.AdjustRelativeStartTimesForHierarchy logic.
    /// </summary>
    private Dictionary<Guid, NodeTimingInfo> ApplyRelativeTimingCorrection(
        Dictionary<Guid, NodeTimingInfo> originalTimingInfo,
        List<Node> nodes)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);
        var correctedTimingInfo = new Dictionary<Guid, NodeTimingInfo>();

        foreach (var (nodeId, timing) in originalTimingInfo)
        {
            if (!nodeDict.TryGetValue(nodeId, out var node))
            {
                correctedTimingInfo[nodeId] = timing;
                continue;
            }

            double newRelativeStartTime;

            if (node.ParentId.HasValue && originalTimingInfo.TryGetValue(node.ParentId.Value, out var parentTiming))
                // Child node: relative = absolute - parent absolute
                newRelativeStartTime = timing.AbsoluteStartTime - parentTiming.AbsoluteStartTime;
            else
                // Root node: relative = absolute
                newRelativeStartTime = timing.AbsoluteStartTime;

            correctedTimingInfo[nodeId] = timing with { RelativeStartTime = newRelativeStartTime };
        }

        return correctedTimingInfo;
    }

    private static TaskNode CreateTaskNode(Guid id, Guid? parentId)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = $"Task-{id:N}"[..8],
                StartTime = 0.0,
                Duration = 1.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(Guid id, Guid? parentId)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
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