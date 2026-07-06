using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Computation;

/// <summary>
///     Test that verifies parent TaskNode timing is calculated correctly
///     both with and without dependency edges between children.
/// </summary>
public class EdgeDependencyTimingTest
{
    [Fact]
    public void ParentTaskNode_WithDependencyEdge_ShouldSpanFullSequentialDuration()
    {
        // When there's a dependency edge (t2 → t3), tasks run sequentially
        // Expected: t1 should span 0-135 (full sequential duration)

        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();
        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        var t1Id = Guid.NewGuid();
        var t2Id = Guid.NewGuid();
        var t3Id = Guid.NewGuid();
        var s1Id = Guid.NewGuid();
        var s2Id = Guid.NewGuid();

        var t1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t1Id,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "t1",
                StartTime = 0,
                Duration = 70, // Wrong initial value
                FinishTime = 70
            }
        };

        var t2 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t2Id,
            ParentId = t1Id,
            Position = new NodePosition { X = 0, Y = 40 },
            Task = new Task
            {
                Name = "t2",
                StartTime = 0,
                Duration = 65,
                FinishTime = 65
            }
        };

        var t3 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t3Id,
            ParentId = t1Id,
            Position = new NodePosition { X = 65, Y = 150 },
            Task = new Task
            {
                Name = "t3",
                StartTime = 65,
                Duration = 70,
                FinishTime = 135
            }
        };

        var s1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = s1Id,
            ParentId = t2Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "s1",
                StartTime = 0,
                Duration = 65,
                FinishTime = 65,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill1",
                    Description = "",
                    Properties = []
                }
            }
        };

        var s2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = s2Id,
            ParentId = t3Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "s2",
                StartTime = 65, // Starts after s1 due to dependency
                Duration = 70,
                FinishTime = 135,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill2",
                    Description = "",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { t1, t2, t3, s1, s2 };

        // Setup mock dependencies - return nodes in hierarchical order (deepest first)
        mockHierarchicalSorter.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns(new List<TaskNode> { t2, t3, t1 }); // Process children before parent

        mockChildNodeCollector.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns((Guid parentId, IReadOnlyList<Node> allNodes) =>
            {
                var childNodes = allNodes.Where(n => n.ParentId == parentId).ToList();
                var skillNodes = childNodes.OfType<SkillExecutionNode>().ToList().AsReadOnly();
                var taskNodes = childNodes.OfType<TaskNode>().ToList().AsReadOnly();
                var routerNodes = childNodes.OfType<RouterNode>().ToList().AsReadOnly();
                return (skillNodes, taskNodes, routerNodes);
            });

        mockTimingAggregator.Setup(x =>
                x.AggregateTimings(It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((IEnumerable<(double Duration, double StartTime, double FinishTime)> timings) =>
            {
                var timingList = timings.ToList();
                if (!timingList.Any()) return (0.0, 0.0, 0.0);
                var minStart = timingList.Min(t => t.StartTime);
                var maxFinish = timingList.Max(t => t.FinishTime);
                return (maxFinish - minStart, minStart, maxFinish);
            });

        // Skill timings with sequential execution (due to t2→t3 dependency)
        var skillTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [s1Id] = (65.0, 0.0, 65.0),
            [s2Id] = (70.0, 65.0, 135.0) // s2 starts after s1 finishes
        };

        // Act
        var schedules = calculator.CalculateTaskNodeSchedules(allNodes, skillTimings);

        // Assert
        Assert.Equal(3, schedules.Count);

        // t2 should span its skill s1
        Assert.True(schedules.ContainsKey(t2Id));
        Assert.Equal(0.0, schedules[t2Id].StartTime);
        Assert.Equal(65.0, schedules[t2Id].FinishTime);
        Assert.Equal(65.0, schedules[t2Id].Duration);

        // t3 should span its skill s2
        Assert.True(schedules.ContainsKey(t3Id));
        Assert.Equal(65.0, schedules[t3Id].StartTime);
        Assert.Equal(135.0, schedules[t3Id].FinishTime);
        Assert.Equal(70.0, schedules[t3Id].Duration);

        // CRITICAL: t1 should span both children sequentially
        Assert.True(schedules.ContainsKey(t1Id));
        Assert.Equal(0.0, schedules[t1Id].StartTime);
        Assert.Equal(135.0, schedules[t1Id].FinishTime); // Full sequential span
        Assert.Equal(135.0, schedules[t1Id].Duration);
    }

    [Fact]
    public void ParentTaskNode_WithoutDependencyEdge_ShouldSpanParallelDuration()
    {
        // When there's NO dependency edge, tasks run in parallel
        // Expected: t1 should span 0-70 (max of parallel durations)

        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();
        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        var t1Id = Guid.NewGuid();
        var t2Id = Guid.NewGuid();
        var t3Id = Guid.NewGuid();
        var s1Id = Guid.NewGuid();
        var s2Id = Guid.NewGuid();

        var t1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t1Id,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "t1",
                StartTime = 0,
                Duration = 135, // Wrong initial value
                FinishTime = 135
            }
        };

        var t2 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t2Id,
            ParentId = t1Id,
            Position = new NodePosition { X = 0, Y = 40 },
            Task = new Task
            {
                Name = "t2",
                StartTime = 0,
                Duration = 65,
                FinishTime = 65
            }
        };

        var t3 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = t3Id,
            ParentId = t1Id,
            Position = new NodePosition { X = 0, Y = 150 },
            Task = new Task
            {
                Name = "t3",
                StartTime = 0,
                Duration = 70,
                FinishTime = 70
            }
        };

        var s1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = s1Id,
            ParentId = t2Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "s1",
                StartTime = 0,
                Duration = 65,
                FinishTime = 65,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill1",
                    Description = "",
                    Properties = []
                }
            }
        };

        var s2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = s2Id,
            ParentId = t3Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "s2",
                StartTime = 0, // Runs in parallel with s1
                Duration = 70,
                FinishTime = 70,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill2",
                    Description = "",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { t1, t2, t3, s1, s2 };

        // Setup mock dependencies - return nodes in hierarchical order (deepest first)
        mockHierarchicalSorter.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns(new List<TaskNode> { t2, t3, t1 }); // Process children before parent

        mockChildNodeCollector.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns((Guid parentId, IReadOnlyList<Node> allNodes) =>
            {
                var childNodes = allNodes.Where(n => n.ParentId == parentId).ToList();
                var skillNodes = childNodes.OfType<SkillExecutionNode>().ToList().AsReadOnly();
                var taskNodes = childNodes.OfType<TaskNode>().ToList().AsReadOnly();
                var routerNodes = childNodes.OfType<RouterNode>().ToList().AsReadOnly();
                return (skillNodes, taskNodes, routerNodes);
            });

        mockTimingAggregator.Setup(x =>
                x.AggregateTimings(It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((IEnumerable<(double Duration, double StartTime, double FinishTime)> timings) =>
            {
                var timingList = timings.ToList();
                if (!timingList.Any()) return (0.0, 0.0, 0.0);
                var minStart = timingList.Min(t => t.StartTime);
                var maxFinish = timingList.Max(t => t.FinishTime);
                return (maxFinish - minStart, minStart, maxFinish);
            });

        // Skill timings with parallel execution (no dependency)
        var skillTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [s1Id] = (65.0, 0.0, 65.0),
            [s2Id] = (70.0, 0.0, 70.0) // s2 runs in parallel with s1
        };

        // Act
        var schedules = calculator.CalculateTaskNodeSchedules(allNodes, skillTimings);

        // Assert
        Assert.Equal(3, schedules.Count);

        // t2 should span its skill s1
        Assert.True(schedules.ContainsKey(t2Id));
        Assert.Equal(0.0, schedules[t2Id].StartTime);
        Assert.Equal(65.0, schedules[t2Id].FinishTime);
        Assert.Equal(65.0, schedules[t2Id].Duration);

        // t3 should span its skill s2
        Assert.True(schedules.ContainsKey(t3Id));
        Assert.Equal(0.0, schedules[t3Id].StartTime);
        Assert.Equal(70.0, schedules[t3Id].FinishTime);
        Assert.Equal(70.0, schedules[t3Id].Duration);

        // CRITICAL: t1 should span both children in parallel
        Assert.True(schedules.ContainsKey(t1Id));
        Assert.Equal(0.0, schedules[t1Id].StartTime);
        Assert.Equal(70.0, schedules[t1Id].FinishTime); // Max of parallel durations
        Assert.Equal(70.0, schedules[t1Id].Duration);
    }
}