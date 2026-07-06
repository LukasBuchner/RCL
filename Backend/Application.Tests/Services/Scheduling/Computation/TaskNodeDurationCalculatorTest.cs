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
///     Unit test to verify TaskNodeDurationCalculator correctly calculates parent timing from children.
/// </summary>
public class TaskNodeDurationCalculatorTest
{
    /// <summary
    /// <IChildNodeCollector>
    ///     smart mock behaviors for all tests in this class
    ///     </summary>
    private void SetupSmartMockBehaviors(
        Mock<IChildNodeCollector> mockChildNodeCollector,
        Mock<ITimingAggregator> mockTimingAggregator,
        Mock<IHierarchicalSorter> mockHierarchicalSorter)
    {
        // Smart ChildNodeCollector behavior - dynamically finds actual children based on ParentId
        mockChildNodeCollector.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns<Guid, IReadOnlyList<Node>>((parentId, allNodes) =>
            {
                var skillChildren = allNodes.OfType<SkillExecutionNode>()
                    .Where(sn => sn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly();

                var taskChildren = allNodes.OfType<TaskNode>()
                    .Where(tn => tn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly();

                var routerChildren = allNodes.OfType<RouterNode>()
                    .Where(rn => rn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly();

                return (skillChildren, taskChildren, routerChildren);
            });

        mockChildNodeCollector.Setup(x => x.CollectChildSkillNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns<Guid, IReadOnlyList<Node>>((parentId, allNodes) =>
                allNodes.OfType<SkillExecutionNode>()
                    .Where(sn => sn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly());

        mockChildNodeCollector.Setup(x => x.CollectChildTaskNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns<Guid, IReadOnlyList<Node>>((parentId, allNodes) =>
                allNodes.OfType<TaskNode>()
                    .Where(tn => tn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly());

        // Smart HierarchicalSorter behavior - sorts by hierarchy depth (children before parents)
        mockHierarchicalSorter.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IEnumerable<TaskNode>>()))
            .Returns<IEnumerable<TaskNode>>(nodes =>
            {
                var taskList = nodes.ToList();
                // Simple hierarchical sort - nodes with parents come before nodes without parents
                return taskList.OrderBy(tn => tn.ParentId.HasValue ? 0 : 1).ToList().AsReadOnly();
            });

        // Smart TimingAggregator behavior - calculates actual span from provided timings
        mockTimingAggregator.Setup(x =>
                x.AggregateTimings(It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns<IEnumerable<(double Duration, double StartTime, double FinishTime)>>(timings =>
            {
                var timingList = timings.ToList();
                if (!timingList.Any())
                    return (0.0, 0.0, 0.0);

                var earliestStart = timingList.Min(t => t.StartTime);
                var latestFinish = timingList.Max(t => t.FinishTime);
                var spanDuration = latestFinish - earliestStart;

                return (spanDuration, earliestStart, latestFinish);
            });
    }

    [Fact]
    public void CalculateTaskNodeSchedules_TaskWithScheduledSkillChild_ShouldCalculateCorrectStartTime()
    {
        // Arrange
        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Setup smart mock behaviors
        SetupSmartMockBehaviors(mockChildNodeCollector, mockTimingAggregator, mockHierarchicalSorter);

        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = taskId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Parent Task",
                StartTime = 0, // Original start time
                Duration = 70,
                FinishTime = 70
            }
        };

        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = skillId,
            ParentId = taskId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Child Skill",
                StartTime = 0, // Original start time, but will be overridden by scheduled timing
                Duration = 70,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { taskNode, skillNode };

        // This represents the timing information from the scheduled execution graph
        // The skill was scheduled to start at time 70 (after some other task finished)
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillId] = (Duration: 70.0, StartTime: 70.0, FinishTime: 140.0)
        };

        // Act
        var result = calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Single(result); // Only the task node should have a calculated schedule
        Assert.True(result.ContainsKey(taskId));

        var taskSchedule = result[taskId];

        // The parent task should start when its child starts (70) and span to when child finishes (140)
        Assert.Equal(70.0, taskSchedule.StartTime); // Task should start when child starts
        Assert.Equal(140.0, taskSchedule.FinishTime); // Task should finish when child finishes  
        Assert.Equal(70.0, taskSchedule.Duration); // Duration should be child span (140-70=70)
    }

    [Fact]
    public void CalculateTaskNodeSchedules_TaskWithNoChildTimings_ShouldUseOriginalSchedule()
    {
        // Arrange
        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Setup smart mock behaviors
        SetupSmartMockBehaviors(mockChildNodeCollector, mockTimingAggregator, mockHierarchicalSorter);

        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var taskNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = taskId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Parent Task",
                StartTime = 10,
                Duration = 50,
                FinishTime = 60
            }
        };

        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = skillId,
            ParentId = taskId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Child Skill",
                StartTime = 0,
                Duration = 30,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { taskNode, skillNode };

        // No child timings provided (e.g. scheduling failed)
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(taskId));

        var taskSchedule = result[taskId];

        // Should fall back to original timing
        Assert.Equal(50.0, taskSchedule.Duration); // Original duration
        Assert.Equal(0.0, taskSchedule.StartTime); // Fallback to 0 start time
        Assert.Equal(50.0, taskSchedule.FinishTime); // Start + Duration
    }

    [Fact]
    public void CalculateTaskNodeSchedules_NestedTaskNodes_ShouldCalculateParentEnvelopeCorrectly()
    {
        // This test verifies the fix for the bug where parent TaskNodes were not considering child TaskNodes
        // Test scenario based on the bug report:
        // - t1 (parent TaskNode)
        //   - t2 (child TaskNode) with s1 (SkillExecutionNode child) finishing at 65
        //   - t3 (child TaskNode) with s2 (SkillExecutionNode child) finishing at 135
        // Expected: t1 should span from 0 to 135, not 0 to 70

        // Arrange
        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Setup smart mock behaviors
        SetupSmartMockBehaviors(mockChildNodeCollector, mockTimingAggregator, mockHierarchicalSorter);

        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        var t1Id = Guid.NewGuid();
        var t2Id = Guid.NewGuid();
        var t3Id = Guid.NewGuid();
        var s1Id = Guid.NewGuid();
        var s2Id = Guid.NewGuid();

        // Root TaskNode t1
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
                Duration = 70, // Incorrect original duration
                FinishTime = 70
            }
        };

        // Child TaskNode t2 (child of t1)
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

        // Child TaskNode t3 (child of t1)
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

        // SkillExecutionNode s1 (child of t2)
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
                    Name = "Move Object To",
                    Description = "Move an object to a specific location",
                    Properties = []
                }
            }
        };

        // SkillExecutionNode s2 (child of t3)
        var s2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = s2Id,
            ParentId = t3Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "s2",
                StartTime = 65,
                Duration = 70,
                FinishTime = 135,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { t1, t2, t3, s1, s2 };

        // Timing information from scheduled skill executions (only skills have scheduled timings initially)
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [s1Id] = (Duration: 65.0, StartTime: 0.0, FinishTime: 65.0),
            [s2Id] = (Duration: 70.0, StartTime: 65.0, FinishTime: 135.0)
        };

        // Act
        var result = calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Equal(3, result.Count); // Should have schedules for t1, t2, and t3

        // Verify t2 timing (should envelope s1)
        Assert.True(result.ContainsKey(t2Id));
        var t2Schedule = result[t2Id];
        Assert.Equal(0.0, t2Schedule.StartTime);
        Assert.Equal(65.0, t2Schedule.FinishTime);
        Assert.Equal(65.0, t2Schedule.Duration);

        // Verify t3 timing (should envelope s2)
        Assert.True(result.ContainsKey(t3Id));
        var t3Schedule = result[t3Id];
        Assert.Equal(65.0, t3Schedule.StartTime);
        Assert.Equal(135.0, t3Schedule.FinishTime);
        Assert.Equal(70.0, t3Schedule.Duration);

        // Verify t1 timing (should envelope both t2 and t3) - THIS IS THE KEY FIX
        Assert.True(result.ContainsKey(t1Id));
        var t1Schedule = result[t1Id];
        Assert.Equal(0.0, t1Schedule.StartTime); // Should start at earliest child start
        Assert.Equal(135.0, t1Schedule.FinishTime); // Should finish at latest child finish
        Assert.Equal(135.0, t1Schedule.Duration); // Should span the full range (135 - 0)
    }
}