using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Computation;

/// <summary>
///     Comprehensive unit tests for TaskNodeDurationCalculator.
///     Tests cover duration calculations, schedule calculations, edge cases, and error scenarios.
/// </summary>
public class TaskNodeDurationCalculatorTests
{
    private readonly TaskNodeDurationCalculator _calculator;
    private readonly Mock<IChildNodeCollector> _mockChildNodeCollector;
    private readonly Mock<IHierarchicalSorter> _mockHierarchicalSorter;
    private readonly Mock<ILogger<TaskNodeDurationCalculator>> _mockLogger;
    private readonly Mock<ITimingAggregator> _mockTimingAggregator;

    public TaskNodeDurationCalculatorTests()
    {
        _mockChildNodeCollector = new Mock<IChildNodeCollector>();
        _mockTimingAggregator = new Mock<ITimingAggregator>();
        _mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        _mockLogger = new Mock<ILogger<TaskNodeDurationCalculator>>();

        // Set up default mock behaviors
        SetupDefaultMockBehaviors();

        _calculator = new TaskNodeDurationCalculator(_mockChildNodeCollector.Object, _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object, _mockLogger.Object);
    }

    /// <summary>
    ///     Sets up default behaviors for mocks to prevent NullReferenceExceptions
    /// </summary>
    private void SetupDefaultMockBehaviors()
    {
        // Smart ChildNodeCollector behavior - dynamically finds actual children based on ParentId
        _mockChildNodeCollector.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
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

        _mockChildNodeCollector.Setup(x => x.CollectChildSkillNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns<Guid, IReadOnlyList<Node>>((parentId, allNodes) =>
                allNodes.OfType<SkillExecutionNode>()
                    .Where(sn => sn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly());

        _mockChildNodeCollector.Setup(x => x.CollectChildTaskNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns<Guid, IReadOnlyList<Node>>((parentId, allNodes) =>
                allNodes.OfType<TaskNode>()
                    .Where(tn => tn.ParentId == parentId)
                    .ToList()
                    .AsReadOnly());

        // Smart HierarchicalSorter behavior - sorts by hierarchy depth (children before parents)
        _mockHierarchicalSorter.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IEnumerable<TaskNode>>()))
            .Returns<IEnumerable<TaskNode>>(nodes =>
            {
                var taskList = nodes.ToList();
                // Simple hierarchical sort - nodes with parents come before nodes without parents
                return taskList.OrderBy(tn => tn.ParentId.HasValue ? 0 : 1).ToList().AsReadOnly();
            });

        // Smart TimingAggregator behavior - calculates actual span from provided timings
        _mockTimingAggregator.Setup(x =>
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

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_ShouldNotThrow()
    {
        // Act & Assert - Constructor called in setup, no exception should be thrown
        Assert.NotNull(_calculator);
    }

    [Fact]
    public void Constructor_WithNullChildNodeCollector_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TaskNodeDurationCalculator(null!, _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullTimingAggregator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TaskNodeDurationCalculator(_mockChildNodeCollector.Object, null!,
            _mockHierarchicalSorter.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHierarchicalSorter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TaskNodeDurationCalculator(_mockChildNodeCollector.Object,
            _mockTimingAggregator.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TaskNodeDurationCalculator(_mockChildNodeCollector.Object,
            _mockTimingAggregator.Object, _mockHierarchicalSorter.Object, null!));
    }

    #endregion

    #region CalculateTaskNodeDuration Tests

    [Fact]
    public void CalculateTaskNodeDuration_WithNullTaskNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        var allNodes = new List<Node>();
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateTaskNodeDuration(null!, allNodes, nodeTimings));
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithNullAllNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateTaskNodeDuration(taskNode, null!, nodeTimings));
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithNullNodeTimings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var allNodes = new List<Node>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateTaskNodeDuration(taskNode, allNodes, null!));
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithTaskNodeHavingNoChildren_ShouldReturnNull()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var allNodes = new List<Node> { taskNode }; // Only the task node itself
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act - smart mocks will handle the setup automatically
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithSingleChildSkillNode_ShouldReturnChildDuration()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skillNode = CreateSkillExecutionNode("Skill1", 30.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skillNode };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillNode.Id] = (30.0, 10.0, 40.0)
        };

        // Act - smart mocks will handle the setup automatically
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(30.0, result);
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithMultipleChildrenSequential_ShouldReturnSpanDuration()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 20.0, taskNode.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 15.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (20.0, 10.0, 30.0), // 10-30
            [skill2.Id] = (15.0, 30.0, 45.0) // 30-45 (sequential)
        };

        // Act - smart mocks will handle the setup automatically
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(35.0, result); // 45 - 10 = 35 (span from earliest start to latest finish)
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithMultipleChildrenOverlapping_ShouldReturnSpanDuration()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, taskNode.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 25.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0), // 10-40
            [skill2.Id] = (25.0, 20.0, 45.0) // 20-45 (overlapping)
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(35.0, result); // 45 - 10 = 35 (span from earliest start to latest finish)
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithChildrenHavingMissingTimings_ShouldIgnoreMissingChildren()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, taskNode.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 25.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0)
            // skill2 timing is missing
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(30.0, result); // Only consider skill1 that has timing info
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithNestedTaskChildren_ShouldIncludeNestedTasks()
    {
        // Arrange
        var parentTask = CreateTaskNode("ParentTask", 100.0);
        var childTask = CreateTaskNode("ChildTask", 50.0, parentTask.Id);
        var skillNode = CreateSkillExecutionNode("Skill1", 30.0, childTask.Id);
        var allNodes = new List<Node> { parentTask, childTask, skillNode };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [childTask.Id] = (40.0, 10.0, 50.0), // Child task timing
            [skillNode.Id] = (30.0, 15.0, 45.0) // Skill timing
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(parentTask, allNodes, nodeTimings);

        // Assert
        Assert.Equal(40.0, result); // 50 - 10 = 40 (span from child task start to finish)
    }

    #endregion

    #region CalculateAllTaskNodeDurations Tests

    [Fact]
    public void CalculateAllTaskNodeDurations_WithNullAllNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateAllTaskNodeDurations(null!, nodeTimings));
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithNullNodeTimings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var allNodes = new List<Node>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateAllTaskNodeDurations(allNodes, null!));
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithEmptyNodes_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var allNodes = new List<Node>();
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithOnlySkillNodes_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0);
        var skill2 = CreateSkillExecutionNode("Skill2", 25.0);
        var allNodes = new List<Node> { skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0),
            [skill2.Id] = (25.0, 20.0, 45.0)
        };

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithTaskNodesHavingChildren_ShouldReturnCalculatedDurations()
    {
        // Arrange
        var task1 = CreateTaskNode("Task1", 60.0);
        var task2 = CreateTaskNode("Task2", 80.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, task1.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 25.0, task1.Id);
        var skill3 = CreateSkillExecutionNode("Skill3", 40.0, task2.Id);

        var allNodes = new List<Node> { task1, task2, skill1, skill2, skill3 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0),
            [skill2.Id] = (25.0, 35.0, 60.0), // task1 span: 10-60 = 50
            [skill3.Id] = (40.0, 5.0, 45.0) // task2 span: 5-45 = 40
        };

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(50.0, result[task1.Id]); // 60 - 10 = 50
        Assert.Equal(40.0, result[task2.Id]); // 45 - 5 = 40
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithTaskNodesHavingNoChildren_ShouldExcludeThoseTasks()
    {
        // Arrange
        var taskWithChildren = CreateTaskNode("TaskWithChildren", 60.0);
        var taskWithoutChildren = CreateTaskNode("TaskWithoutChildren", 80.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, taskWithChildren.Id);

        var allNodes = new List<Node> { taskWithChildren, taskWithoutChildren, skill1 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0)
        };

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(30.0, result[taskWithChildren.Id]); // Calculated from children
        Assert.Equal(80.0, result[taskWithoutChildren.Id]); // Fallback to original duration
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithNestedTaskHierarchy_ShouldCalculateAllLevels()
    {
        // Arrange
        var grandParent = CreateTaskNode("GrandParent", 120.0);
        var parent1 = CreateTaskNode("Parent1", 60.0, grandParent.Id);
        var parent2 = CreateTaskNode("Parent2", 80.0, grandParent.Id);
        var child1 = CreateTaskNode("Child1", 40.0, parent1.Id);
        var skill1 = CreateSkillExecutionNode("Skill1", 20.0, child1.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 30.0, parent2.Id);

        var allNodes = new List<Node> { grandParent, parent1, parent2, child1, skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (20.0, 10.0, 30.0), // child1: 20, parent1: 20, grandParent considers parent1+parent2
            [skill2.Id] = (30.0, 35.0, 65.0), // parent2: 30, grandParent: 10-65 = 55
            [child1.Id] = (20.0, 10.0, 30.0), // calculated from skill1
            [parent1.Id] = (20.0, 10.0, 30.0), // calculated from child1
            [parent2.Id] = (30.0, 35.0, 65.0) // calculated from skill2
        };

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(20.0, result[child1.Id]); // From skill1
        Assert.Equal(20.0, result[parent1.Id]); // From child1
        Assert.Equal(30.0, result[parent2.Id]); // From skill2
        Assert.Equal(55.0, result[grandParent.Id]); // 65 - 10 = 55 (span from parent1 to parent2)
    }

    #endregion

    #region CalculateTaskNodeSchedules Tests

    [Fact]
    public void CalculateTaskNodeSchedules_WithNullAllNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateTaskNodeSchedules(null!, childNodeTimings));
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithNullChildNodeTimings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var allNodes = new List<Node>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateTaskNodeSchedules(allNodes, null!));
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithEmptyNodes_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var allNodes = new List<Node>();
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithTaskHavingOneChild_ShouldReturnChildSchedule()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skillNode = CreateSkillExecutionNode("Skill1", 30.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skillNode };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillNode.Id] = (30.0, 15.0, 45.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Single(result);
        var schedule = result[taskNode.Id];
        Assert.Equal(30.0, schedule.Duration);
        Assert.Equal(15.0, schedule.StartTime);
        Assert.Equal(45.0, schedule.FinishTime);
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithTaskHavingMultipleChildren_ShouldReturnSpanSchedule()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 20.0, taskNode.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 15.0, taskNode.Id);
        var skill3 = CreateSkillExecutionNode("Skill3", 25.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1, skill2, skill3 };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (20.0, 10.0, 30.0),
            [skill2.Id] = (15.0, 25.0, 40.0),
            [skill3.Id] = (25.0, 35.0, 60.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Single(result);
        var schedule = result[taskNode.Id];
        Assert.Equal(50.0, schedule.Duration); // 60 - 10 = 50
        Assert.Equal(10.0, schedule.StartTime); // Earliest start
        Assert.Equal(60.0, schedule.FinishTime); // Latest finish
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithMultipleTasksHavingChildren_ShouldReturnAllSchedules()
    {
        // Arrange
        var task1 = CreateTaskNode("Task1", 60.0);
        var task2 = CreateTaskNode("Task2", 80.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, task1.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 25.0, task2.Id);
        var allNodes = new List<Node> { task1, task2, skill1, skill2 };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0),
            [skill2.Id] = (25.0, 50.0, 75.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Equal(2, result.Count);

        var schedule1 = result[task1.Id];
        Assert.Equal(30.0, schedule1.Duration);
        Assert.Equal(10.0, schedule1.StartTime);
        Assert.Equal(40.0, schedule1.FinishTime);

        var schedule2 = result[task2.Id];
        Assert.Equal(25.0, schedule2.Duration);
        Assert.Equal(50.0, schedule2.StartTime);
        Assert.Equal(75.0, schedule2.FinishTime);
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithTasksHavingNoChildren_ShouldEmitZeroExtent()
    {
        // Arrange
        var taskWithChildren = CreateTaskNode("TaskWithChildren", 60.0);
        var taskWithoutChildren = CreateTaskNode("TaskWithoutChildren", 80.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 30.0, taskWithChildren.Id);
        var allNodes = new List<Node> { taskWithChildren, taskWithoutChildren, skill1 };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (30.0, 10.0, 40.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey(taskWithChildren.Id)); // Calculated from children
        Assert.True(result.ContainsKey(taskWithoutChildren.Id)); // Empty container present with zero extent
        Assert.Equal((0.0, 0.0, 0.0), result[taskWithoutChildren.Id]); // No children: zero scheduled extent
    }

    [Fact]
    public void CalculateTaskNodeSchedules_WithNestedHierarchy_ShouldCalculateAllLevels()
    {
        // Arrange
        var parentTask = CreateTaskNode("ParentTask", 100.0);
        var childTask = CreateTaskNode("ChildTask", 50.0, parentTask.Id);
        var skill1 = CreateSkillExecutionNode("Skill1", 20.0, childTask.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 30.0, parentTask.Id);
        var allNodes = new List<Node> { parentTask, childTask, skill1, skill2 };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (20.0, 10.0, 30.0),
            [skill2.Id] = (30.0, 40.0, 70.0),
            [childTask.Id] = (20.0, 10.0, 30.0) // Calculated from skill1
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        Assert.Equal(2, result.Count);

        var childSchedule = result[childTask.Id];
        Assert.Equal(20.0, childSchedule.Duration);
        Assert.Equal(10.0, childSchedule.StartTime);
        Assert.Equal(30.0, childSchedule.FinishTime);

        var parentSchedule = result[parentTask.Id];
        Assert.Equal(60.0, parentSchedule.Duration); // 70 - 10 = 60 (span from childTask to skill2)
        Assert.Equal(10.0, parentSchedule.StartTime);
        Assert.Equal(70.0, parentSchedule.FinishTime);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CalculateTaskNodeDuration_WithZeroDurationChildren_ShouldCalculateCorrectly()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 0.0, taskNode.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", 30.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1, skill2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (0.0, 10.0, 10.0), // Zero duration
            [skill2.Id] = (30.0, 20.0, 50.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(40.0, result); // 50 - 10 = 40 (includes zero-duration skill in span)
    }

    [Fact]
    public void CalculateTaskNodeDuration_WithNegativeTimings_ShouldCalculateCorrectly()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 60.0);
        var skill1 = CreateSkillExecutionNode("Skill1", 20.0, taskNode.Id);
        var allNodes = new List<Node> { taskNode, skill1 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill1.Id] = (20.0, -5.0, 15.0) // Negative start time
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(taskNode, allNodes, nodeTimings);

        // Assert
        Assert.Equal(20.0, result); // Duration should still be calculated correctly
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_WithLargeHierarchy_ShouldPerformEfficiently()
    {
        // Arrange - Create a large hierarchy to test performance
        var tasks = new List<TaskNode>();
        var skills = new List<SkillExecutionNode>();
        var allNodes = new List<Node>();
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        for (var i = 0; i < 100; i++)
        {
            var task = CreateTaskNode($"Task{i}", 60.0);
            tasks.Add(task);
            allNodes.Add(task);

            for (var j = 0; j < 5; j++)
            {
                var skill = CreateSkillExecutionNode($"Skill{i}_{j}", 10.0, task.Id);
                skills.Add(skill);
                allNodes.Add(skill);
                nodeTimings[skill.Id] = (10.0, j * 12.0, (j + 1) * 12.0);
            }
        }

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert
        Assert.Equal(100, result.Count); // All tasks should have calculated durations
        Assert.All(result.Values, duration => Assert.Equal(60.0, duration)); // Each task spans 0-60 (5 * 12)
    }

    #endregion

    #region Nested Router Duration Tests — RouterNode children invisible to TaskNodeDurationCalculator

    [Fact]
    public void CalculateTaskNodeDuration_TaskNodeWithRouterChild_MustIncludeRouterTiming()
    {
        // A TaskNode (branch task of an outer router) has a nested RouterNode as its child.
        // The RouterNode has timing data. TaskNodeDurationCalculator must include it.
        // Currently fails because ChildNodeCollector.CollectAllChildNodes uses .OfType<SkillExecutionNode>()
        // and .OfType<TaskNode>(), which excludes RouterNode.
        var parentTask = CreateTaskNode("BranchTask", 100.0);
        var nestedRouter = CreateRouterNode("NestedRouter", 20.0, parentTask.Id);
        var allNodes = new List<Node> { parentTask, nestedRouter };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [nestedRouter.Id] = (20.0, 5.0, 25.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(parentTask, allNodes, nodeTimings);

        // Assert — must include the router child's timing
        result.Should().NotBeNull("the TaskNode has a RouterNode child with timing data");
        result.Should().Be(20.0, "duration should be 25.0 - 5.0 = 20.0 from the router child's timing span");
    }

    [Fact]
    public void CalculateTaskNodeDuration_TaskNodeWithSkillAndRouterChildren_MustSpanBoth()
    {
        // A TaskNode contains both a SkillExecutionNode and a RouterNode.
        // The duration must span across both children's timing ranges.
        var parentTask = CreateTaskNode("BranchTask", 100.0);
        var skill = CreateSkillExecutionNode("Skill1", 15.0, parentTask.Id);
        var nestedRouter = CreateRouterNode("NestedRouter", 20.0, parentTask.Id);
        var allNodes = new List<Node> { parentTask, skill, nestedRouter };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skill.Id] = (15.0, 0.0, 15.0),
            [nestedRouter.Id] = (20.0, 15.0, 35.0) // Router starts after skill
        };

        // Act
        var result = _calculator.CalculateTaskNodeDuration(parentTask, allNodes, nodeTimings);

        // Assert — must span from earliest start (0.0) to latest finish (35.0)
        result.Should().Be(35.0,
            "duration should span from skill start (0.0) to router finish (35.0) = 35.0, " +
            "but currently router timing is invisible because ChildNodeCollector excludes RouterNode");
    }

    [Fact]
    public void CalculateTaskNodeSchedules_TaskNodeWithRouterChild_MustIncludeRouterInSchedule()
    {
        // CalculateTaskNodeSchedules must include RouterNode children when computing
        // the TaskNode's schedule (start, finish, duration).
        var parentTask = CreateTaskNode("BranchTask", 100.0);
        var nestedRouter = CreateRouterNode("NestedRouter", 25.0, parentTask.Id);
        var allNodes = new List<Node> { parentTask, nestedRouter };
        var childNodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [nestedRouter.Id] = (25.0, 10.0, 35.0)
        };

        // Act
        var result = _calculator.CalculateTaskNodeSchedules(allNodes, childNodeTimings);

        // Assert
        result.Should().ContainKey(parentTask.Id);
        var schedule = result[parentTask.Id];
        schedule.Duration.Should().Be(25.0, "duration comes from the router child's timing span");
        schedule.StartTime.Should().Be(10.0, "start time from the router child");
        schedule.FinishTime.Should().Be(35.0, "finish time from the router child");
    }

    [Fact]
    public void CalculateAllTaskNodeDurations_HierarchyWithNestedRouter_MustAccountForRouterDuration()
    {
        // Full hierarchy: GrandParentTask → BranchTask → NestedRouter
        // NestedRouter has known timing. BranchTask's duration should include the router.
        // GrandParentTask's duration should include BranchTask.
        var grandParent = CreateTaskNode("GrandParent", 200.0);
        var branchTask = CreateTaskNode("BranchTask", 100.0, grandParent.Id);
        var nestedRouter = CreateRouterNode("NestedRouter", 30.0, branchTask.Id);

        var allNodes = new List<Node> { grandParent, branchTask, nestedRouter };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [nestedRouter.Id] = (30.0, 5.0, 35.0)
        };

        // Act
        var result = _calculator.CalculateAllTaskNodeDurations(allNodes, nodeTimings);

        // Assert — BranchTask must include router child's timing
        result.Should().ContainKey(branchTask.Id);
        result[branchTask.Id].Should().Be(30.0,
            "BranchTask duration must be 35.0 - 5.0 = 30.0 from the nested router timing, " +
            "but currently the router child is invisible to ChildNodeCollector");
    }

    #endregion

    #region Test Helper Methods

    private TaskNode CreateTaskNode(string name, double duration, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = 0.0,
                FinishTime = duration
            },
            ProcedureId = default
        };
    }

    private SkillExecutionNode CreateSkillExecutionNode(string skillName, double duration, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = duration,
                StartTime = 0.0,
                FinishTime = duration,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = $"{skillName} description",
                    Properties =
                    [
                    ]
                }
            },
            ProcedureId = default
        };
    }

    private RouterNode CreateRouterNode(string name, double duration, Guid? parentId = null)
    {
        var branchTargetId = Guid.NewGuid();
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            ProcedureId = default,
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = duration,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", TargetNodeId = branchTargetId, Priority = 0 }
                }
            }
        };
    }

    #endregion
}