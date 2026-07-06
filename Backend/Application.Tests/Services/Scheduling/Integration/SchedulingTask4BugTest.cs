using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Integration;

/// <summary>
///     Reproduces the specific "Task 4" hierarchy issue from production database data.
///     This test focuses on the core issue: TaskNode with ParentId having timing data.
/// </summary>
public class SchedulingTask4BugTest
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

    /// <summary>
    ///     Reproduces the production issue where a TaskNode child ("task 4") has timing data.
    ///     This violates the expected hierarchy where only SkillExecutionNodes should have explicit timing.
    /// </summary>
    [Fact]
    public void TaskNodeDurationCalculator_WithChildTaskNodeHavingTimingData_ShouldHandleGracefully()
    {
        // Arrange - Create the problematic scenario
        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Setup smart mock behaviors
        SetupSmartMockBehaviors(mockChildNodeCollector, mockTimingAggregator, mockHierarchicalSorter);

        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        // Create the problematic hierarchy from production
        var parentTaskQ = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = null, // Root node
            Position = new NodePosition { X = 0, Y = 330 },
            Task = new Task
            {
                Name = "q",
                StartTime = 0,
                Duration = 120,
                FinishTime = 120
            }
        };

        // Child TaskNode with timing data - THIS IS THE PROBLEM!
        var childTask4 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentTaskQ.Id, // Child TaskNode
            Position = new NodePosition { X = 0, Y = 150 },
            Task = new Task
            {
                Name = "4", // Problematic child TaskNode
                StartTime = 0, // This timing data is the issue
                Duration = 200, // TaskNode children shouldn't have explicit timing
                FinishTime = 200 // Only SkillExecutionNodes should
            }
        };

        // Valid SkillExecutionNode child
        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentTaskQ.Id,
            Position = new NodePosition { X = 205, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "sdf",
                StartTime = 205,
                Duration = 120,
                FinishTime = 325,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position",
                    Description = "Test skill",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { parentTaskQ, childTask4, skillNode };

        // Create child node timings including the problematic TaskNode
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            // Valid skill execution timing
            {
                skillNode.Id,
                (skillNode.SkillExecutionTask.Duration, skillNode.SkillExecutionTask.StartTime,
                    skillNode.SkillExecutionTask.FinishTime ?? 0.0)
            },

            // PROBLEMATIC: TaskNode with timing data
            { childTask4.Id, (childTask4.Task.Duration, childTask4.Task.StartTime, childTask4.Task.FinishTime ?? 0.0) }
        };

        // Act & Assert - Should not crash
        var exception = Record.Exception(() =>
        {
            var durations = calculator.CalculateAllTaskNodeDurations(allNodes.AsReadOnly(), childNodeTimings);

            // Verify the calculator handles this gracefully
            Assert.NotNull(durations);

            // The parent task should still get a calculated duration
            Assert.True(durations.ContainsKey(parentTaskQ.Id), "Parent task should have a calculated duration");

            // The child TaskNode should NOT appear in the results (it's not a container)
            Assert.False(durations.ContainsKey(childTask4.Id), "Child TaskNode should not have a calculated duration");

            // The duration should be based on the actual child timings
            var parentDuration = durations[parentTaskQ.Id];
            Assert.True(parentDuration > 0, "Parent duration should be positive");
        });

        Assert.Null(exception);
    }

    /// <summary>
    ///     Tests the corrected scenario where the problematic TaskNode is removed.
    ///     This should work correctly and demonstrate the expected behavior.
    /// </summary>
    [Fact]
    public void TaskNodeDurationCalculator_WithCorrectedHierarchy_ShouldCalculateCorrectly()
    {
        // Arrange - Create the corrected scenario
        var mockChildNodeCollector = new Mock<IChildNodeCollector>();
        var mockTimingAggregator = new Mock<ITimingAggregator>();
        var mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        var mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Setup smart mock behaviors
        SetupSmartMockBehaviors(mockChildNodeCollector, mockTimingAggregator, mockHierarchicalSorter);

        var calculator = new TaskNodeDurationCalculator(mockChildNodeCollector.Object, mockTimingAggregator.Object,
            mockHierarchicalSorter.Object, mockLogger.Object);

        // Create the corrected hierarchy (no problematic child TaskNode)
        var parentTaskQ = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = null, // Root node
            Position = new NodePosition { X = 205, Y = 330 },
            Task = new Task
            {
                Name = "q",
                StartTime = 205, // Corrected start time
                Duration = 120,
                FinishTime = 325 // Corrected finish time
            }
        };

        // Valid SkillExecutionNode child
        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentTaskQ.Id,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "sdf",
                StartTime = 205,
                Duration = 120,
                FinishTime = 325,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position",
                    Description = "Test skill",
                    Properties = []
                }
            }
        };

        var allNodes = new List<Node> { parentTaskQ, skillNode };

        // Create child node timings (only valid skill execution)
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            {
                skillNode.Id,
                (skillNode.SkillExecutionTask.Duration, skillNode.SkillExecutionTask.StartTime,
                    skillNode.SkillExecutionTask.FinishTime ?? 0.0)
            }
        };

        // Act
        var durations = calculator.CalculateAllTaskNodeDurations(allNodes.AsReadOnly(), childNodeTimings);

        // Assert - This should work correctly
        Assert.NotNull(durations);
        Assert.True(durations.ContainsKey(parentTaskQ.Id), "Parent task should have a calculated duration");

        var parentDuration = durations[parentTaskQ.Id];
        Assert.Equal(120.0, parentDuration); // Should match the skill duration
    }
}