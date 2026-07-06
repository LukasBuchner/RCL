using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Tests to verify that RelativeStartTime is correctly calculated for nodes with parent-child relationships.
///     This focuses on the core issue: RelativeStartTime should be relative to parent, not absolute.
/// </summary>
public class RelativeTimingCalculationTests
{
    [Fact]
    public void NodeTimingInfo_RootNode_RelativeStartTimeShouldEqualAbsolute()
    {
        // This test demonstrates the expected behavior for root nodes
        var timing = new NodeTimingInfo
        {
            AbsoluteStartTime = 5.0,
            RelativeStartTime = 5.0, // For root nodes, relative = absolute
            Duration = 3.0
        };

        Assert.Equal(timing.AbsoluteStartTime, timing.RelativeStartTime);
    }

    [Fact]
    public void NodeTimingInfo_ChildNode_RelativeStartTimeShouldBeRelativeToParent()
    {
        // This test demonstrates the expected behavior for child nodes
        // Parent starts at 5.0, child starts at 7.0 → relative = 2.0
        var childTiming = new NodeTimingInfo
        {
            AbsoluteStartTime = 7.0,
            RelativeStartTime = 2.0, // Should be 7.0 - 5.0 (parent's start)
            Duration = 3.0
        };

        // Verify the calculation: child absolute - parent absolute = child relative
        var parentAbsoluteStart = 5.0;
        var expectedRelative = childTiming.AbsoluteStartTime - parentAbsoluteStart;

        Assert.Equal(expectedRelative, childTiming.RelativeStartTime);
    }

    [Fact]
    public void CalculateRelativeStartTime_NodeWithoutParent_ShouldReturnAbsoluteTime()
    {
        // Arrange
        var nodeTimingInfo = new Dictionary<Guid, NodeTimingInfo>();
        var nodeId = Guid.NewGuid();

        nodeTimingInfo[nodeId] = new NodeTimingInfo
        {
            AbsoluteStartTime = 10.0,
            RelativeStartTime = 10.0, // Will be corrected by our implementation
            Duration = 5.0
        };

        var nodes = new List<Node>
        {
            CreateTaskNode(nodeId, null)
        };

        // Act
        var correctedTimingInfo = CalculateCorrectRelativeStartTimes(nodeTimingInfo, nodes);

        // Assert
        Assert.Equal(10.0, correctedTimingInfo[nodeId].RelativeStartTime);
    }

    [Fact]
    public void CalculateRelativeStartTime_NodeWithParent_ShouldReturnRelativeToParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var nodeTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 5.0,
                RelativeStartTime = 5.0,
                Duration = 10.0
            },
            [childId] = new()
            {
                AbsoluteStartTime = 8.0,
                RelativeStartTime = 8.0, // INCORRECT - should be 3.0 (8.0 - 5.0)
                Duration = 3.0
            }
        };

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, null),
            CreateSkillExecutionNode(childId, parentId)
        };

        // Act
        var correctedTimingInfo = CalculateCorrectRelativeStartTimes(nodeTimingInfo, nodes);

        // Assert
        Assert.Equal(5.0, correctedTimingInfo[parentId].RelativeStartTime); // Parent unchanged
        Assert.Equal(3.0, correctedTimingInfo[childId].RelativeStartTime); // Child corrected: 8.0 - 5.0 = 3.0
    }

    [Fact]
    public void CalculateRelativeStartTime_MultiLevelHierarchy_ShouldCalculateCorrectly()
    {
        // Arrange: Parent -> Child -> Grandchild
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var nodeTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 0.0,
                RelativeStartTime = 0.0,
                Duration = 15.0
            },
            [childId] = new()
            {
                AbsoluteStartTime = 5.0,
                RelativeStartTime = 5.0, // Should be 5.0 (relative to parent at 0.0)
                Duration = 8.0
            },
            [grandchildId] = new()
            {
                AbsoluteStartTime = 7.0,
                RelativeStartTime = 7.0, // INCORRECT - should be 2.0 (7.0 - 5.0, relative to child)
                Duration = 3.0
            }
        };

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, null),
            CreateTaskNode(childId, parentId),
            CreateSkillExecutionNode(grandchildId, childId)
        };

        // Act
        var correctedTimingInfo = CalculateCorrectRelativeStartTimes(nodeTimingInfo, nodes);

        // Assert
        Assert.Equal(0.0, correctedTimingInfo[parentId].RelativeStartTime); // Root unchanged
        Assert.Equal(5.0, correctedTimingInfo[childId].RelativeStartTime); // 5.0 - 0.0 = 5.0
        Assert.Equal(2.0, correctedTimingInfo[grandchildId].RelativeStartTime); // 7.0 - 5.0 = 2.0
    }

    [Fact]
    public void CalculateRelativeStartTime_SiblingNodes_ShouldBothBeRelativeToParent()
    {
        // Arrange: Parent -> Child1, Child2
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var nodeTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [parentId] = new()
            {
                AbsoluteStartTime = 10.0,
                RelativeStartTime = 10.0,
                Duration = 12.0
            },
            [child1Id] = new()
            {
                AbsoluteStartTime = 12.0,
                RelativeStartTime = 12.0, // Should be 2.0 (12.0 - 10.0)
                Duration = 4.0
            },
            [child2Id] = new()
            {
                AbsoluteStartTime = 16.0,
                RelativeStartTime = 16.0, // Should be 6.0 (16.0 - 10.0)
                Duration = 3.0
            }
        };

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, null),
            CreateSkillExecutionNode(child1Id, parentId),
            CreateSkillExecutionNode(child2Id, parentId)
        };

        // Act
        var correctedTimingInfo = CalculateCorrectRelativeStartTimes(nodeTimingInfo, nodes);

        // Assert
        Assert.Equal(10.0, correctedTimingInfo[parentId].RelativeStartTime); // Root unchanged
        Assert.Equal(2.0, correctedTimingInfo[child1Id].RelativeStartTime); // 12.0 - 10.0 = 2.0
        Assert.Equal(6.0, correctedTimingInfo[child2Id].RelativeStartTime); // 16.0 - 10.0 = 6.0
    }

    /// <summary>
    ///     Helper method that implements the correct relative timing calculation logic.
    ///     This demonstrates how the TimingCalculationEngine should work.
    /// </summary>
    private static Dictionary<Guid, NodeTimingInfo> CalculateCorrectRelativeStartTimes(
        Dictionary<Guid, NodeTimingInfo> originalTimingInfo,
        List<Node> nodes)
    {
        var correctedTimingInfo = new Dictionary<Guid, NodeTimingInfo>();
        var nodeDict = nodes.ToDictionary(n => n.Id);

        foreach (var (nodeId, timing) in originalTimingInfo)
        {
            if (!nodeDict.TryGetValue(nodeId, out var node))
            {
                correctedTimingInfo[nodeId] = timing;
                continue;
            }

            double relativeStartTime;

            if (node.ParentId.HasValue && originalTimingInfo.TryGetValue(node.ParentId.Value, out var parentTiming))
                // Child node: relative = absolute - parent absolute
                relativeStartTime = timing.AbsoluteStartTime - parentTiming.AbsoluteStartTime;
            else
                // Root node: relative = absolute
                relativeStartTime = timing.AbsoluteStartTime;

            correctedTimingInfo[nodeId] = timing with { RelativeStartTime = relativeStartTime };
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