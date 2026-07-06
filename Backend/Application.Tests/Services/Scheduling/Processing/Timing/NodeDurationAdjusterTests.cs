using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Timing;

/// <summary>
///     Unit tests for NodeDurationAdjuster.
/// </summary>
public class NodeDurationAdjusterTests
{
    private readonly NodeDurationAdjuster _adjuster;
    private readonly Mock<IChildNodeCollector> _mockChildNodeCollector;
    private readonly Mock<IHierarchicalSorter> _mockHierarchicalSorter;
    private readonly Mock<ILogger<NodeDurationAdjuster>> _mockLogger;
    private readonly Mock<IRouterNodeDurationCalculator> _mockRouterNodeDurationCalculator;
    private readonly Mock<ITaskNodeDurationCalculator> _mockTaskNodeDurationCalculator;
    private readonly Mock<ITimingAggregator> _mockTimingAggregator;

    public NodeDurationAdjusterTests()
    {
        _mockTaskNodeDurationCalculator = new Mock<ITaskNodeDurationCalculator>();
        _mockRouterNodeDurationCalculator = new Mock<IRouterNodeDurationCalculator>();
        _mockChildNodeCollector = new Mock<IChildNodeCollector>();
        _mockTimingAggregator = new Mock<ITimingAggregator>();
        _mockHierarchicalSorter = new Mock<IHierarchicalSorter>();
        _mockLogger = new Mock<ILogger<NodeDurationAdjuster>>();

        // Default setup for router node calculator to return empty dictionary
        _mockRouterNodeDurationCalculator
            .Setup(x => x.CalculateRouterNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>(),
                It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()))
            .Returns(new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>());

        _adjuster = new NodeDurationAdjuster(
            _mockTaskNodeDurationCalculator.Object,
            _mockRouterNodeDurationCalculator.Object,
            _mockChildNodeCollector.Object,
            _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDurationCalculator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeDurationAdjuster(
            null!,
            _mockRouterNodeDurationCalculator.Object,
            _mockChildNodeCollector.Object,
            _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullChildNodeCollector_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeDurationAdjuster(
            _mockTaskNodeDurationCalculator.Object,
            _mockRouterNodeDurationCalculator.Object,
            null!,
            _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullTimingAggregator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeDurationAdjuster(
            _mockTaskNodeDurationCalculator.Object,
            _mockRouterNodeDurationCalculator.Object,
            _mockChildNodeCollector.Object,
            null!,
            _mockHierarchicalSorter.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullHierarchicalSorter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeDurationAdjuster(
            _mockTaskNodeDurationCalculator.Object,
            _mockRouterNodeDurationCalculator.Object,
            _mockChildNodeCollector.Object,
            _mockTimingAggregator.Object,
            null!,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeDurationAdjuster(
            _mockTaskNodeDurationCalculator.Object,
            _mockRouterNodeDurationCalculator.Object,
            _mockChildNodeCollector.Object,
            _mockTimingAggregator.Object,
            _mockHierarchicalSorter.Object,
            null!));
    }

    [Fact]
    public void AdjustParentTaskDurations_WhenCalculatorProvidesResults_ShouldUseCalculatorValues()
    {
        // Arrange
        var taskNode = CreateTaskNode(Guid.NewGuid(), "Task1", 100);
        var skillNode = CreateSkillExecutionNode(Guid.NewGuid(), taskNode.Id, 50);

        var nodeHierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { taskNode }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillNode }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                [taskNode.Id] = new List<Node> { skillNode }.AsReadOnly()
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                [taskNode.Id] = new List<SkillExecutionNode> { skillNode }.AsReadOnly()
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                [skillNode.Id] = taskNode
            }
        };

        var durations = new Dictionary<Guid, double> { [taskNode.Id] = 100 };
        var skillTimings = new Dictionary<Guid, (double, double, double)>
        {
            [skillNode.Id] = (50, 0, 50)
        };

        var calculatorResults = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [taskNode.Id] = (75, 0, 75) // Calculator says it should be 75
        };

        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>()))
            .Returns(calculatorResults);

        // Act
        _adjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillTimings);

        // Assert
        Assert.Equal(75, durations[taskNode.Id]); // Should use calculator's value
    }

    [Fact]
    public void AdjustParentTaskDurations_WhenCalculatorReturnsEmpty_ShouldFallbackToLegacy()
    {
        // Arrange
        var taskNode = CreateTaskNode(Guid.NewGuid(), "Task1", 100);
        var skillNode1 = CreateSkillExecutionNode(Guid.NewGuid(), taskNode.Id, 30);
        var skillNode2 = CreateSkillExecutionNode(Guid.NewGuid(), taskNode.Id, 40);

        var nodeHierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { taskNode }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillNode1, skillNode2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                [taskNode.Id] = new List<Node> { skillNode1, skillNode2 }.AsReadOnly()
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                [taskNode.Id] = new List<SkillExecutionNode> { skillNode1, skillNode2 }.AsReadOnly()
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                [skillNode1.Id] = taskNode,
                [skillNode2.Id] = taskNode
            }
        };

        var durations = new Dictionary<Guid, double> { [taskNode.Id] = 100 };
        var skillTimings = new Dictionary<Guid, (double, double, double)>
        {
            [skillNode1.Id] = (30, 0, 30),
            [skillNode2.Id] = (40, 20, 60) // Overlaps with first, total span is 60
        };

        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>()))
            .Returns(new Dictionary<Guid, (double, double, double)>()); // Empty results

        // Setup hierarchical sorter to return the task nodes
        _mockHierarchicalSorter
            .Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns(nodeHierarchy.TaskNodes.ToList());

        // Setup timing aggregator for the fallback calculation
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((60.0, 0.0, 60.0)); // Span from skill timings

        // Act
        _adjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillTimings);

        // Assert
        Assert.Equal(60, durations[taskNode.Id]); // Should be adjusted to span of children (0-60)
    }

    [Fact]
    public void CalculateRequiredDurationForChildren_WithNoChildren_ShouldReturnNull()
    {
        // Arrange
        var children = new List<Node>();
        var durations = new Dictionary<Guid, double>();
        var skillTimings = new Dictionary<Guid, (double, double, double)>();

        // Act
        var result = _adjuster.CalculateRequiredDurationForChildren(children, durations, skillTimings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateRequiredDurationForChildren_WithSkillChildren_ShouldCalculateSpan()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode(Guid.NewGuid(), null, 30);
        var skillNode2 = CreateSkillExecutionNode(Guid.NewGuid(), null, 40);

        var children = new List<Node> { skillNode1, skillNode2 };
        var durations = new Dictionary<Guid, double>();
        var skillTimings = new Dictionary<Guid, (double, double, double)>
        {
            [skillNode1.Id] = (30, 10, 40),
            [skillNode2.Id] = (40, 30, 70)
        };

        // Setup TimingAggregator to return expected aggregated timing
        // From earliest start (10) to latest finish (70) = duration 60
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((60.0, 10.0, 70.0));

        // Act
        var result = _adjuster.CalculateRequiredDurationForChildren(children, durations, skillTimings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60, result.Value); // Span from 10 to 70 = 60
    }

    [Fact]
    public void CalculateRequiredDurationForChildren_WithTaskChildren_ShouldUseUpdatedDurations()
    {
        // Arrange
        var taskNode1 = CreateTaskNode(Guid.NewGuid(), "Task1", 50);
        var taskNode2 = CreateTaskNode(Guid.NewGuid(), "Task2", 30);
        taskNode2.Task = taskNode2.Task with { StartTime = 20 };

        var children = new List<Node> { taskNode1, taskNode2 };
        var durations = new Dictionary<Guid, double>
        {
            [taskNode1.Id] = 60, // Updated duration
            [taskNode2.Id] = 40 // Updated duration
        };
        var skillTimings = new Dictionary<Guid, (double, double, double)>();

        // Setup TimingAggregator to return expected aggregated timing
        // Task1: start 0, finish 60; Task2: start 20, finish 60
        // Span from earliest start (0) to latest finish (60) = duration 60
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((60.0, 0.0, 60.0));

        // Act
        var result = _adjuster.CalculateRequiredDurationForChildren(children, durations, skillTimings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60, result.Value); // Span from 0 to 60 (task1) and 20 to 60 (task2)
    }

    [Fact]
    public void CalculateRequiredDurationForChildren_WithMixedChildren_ShouldCalculateCorrectSpan()
    {
        // Arrange
        var taskNode = CreateTaskNode(Guid.NewGuid(), "Task1", 30);
        taskNode.Task = taskNode.Task with { StartTime = 5 };
        var skillNode = CreateSkillExecutionNode(Guid.NewGuid(), null, 25);

        var children = new List<Node> { taskNode, skillNode };
        var durations = new Dictionary<Guid, double>
        {
            [taskNode.Id] = 40 // Updated duration
        };
        var skillTimings = new Dictionary<Guid, (double, double, double)>
        {
            [skillNode.Id] = (25, 15, 40)
        };

        // Setup TimingAggregator to return expected aggregated timing
        // Task: start 5, finish 45 (5+40); Skill: start 15, finish 40
        // Span from earliest start (5) to latest finish (45) = duration 40
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((40.0, 5.0, 45.0));

        // Act
        var result = _adjuster.CalculateRequiredDurationForChildren(children, durations, skillTimings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(40, result.Value); // Span from 5 to 45 (task) and 15 to 40 (skill) = 5 to 45 = 40
    }

    [Fact]
    public void CalculateRequiredDurationForChildren_WithRouterNodeChildren_ShouldCalculateCorrectSpan()
    {
        // Arrange - This test demonstrates the bug where RouterNode is not handled
        var routerNode = CreateRouterNode(Guid.NewGuid(), "Router1", 10);
        routerNode.RouterTask = routerNode.RouterTask with { StartTime = 5 };
        var skillNode = CreateSkillExecutionNode(Guid.NewGuid(), null, 25);

        var children = new List<Node> { routerNode, skillNode };
        var durations = new Dictionary<Guid, double>
        {
            [routerNode.Id] = 15 // Updated duration for RouterNode
        };
        var skillTimings = new Dictionary<Guid, (double, double, double)>
        {
            [skillNode.Id] = (25, 20, 45)
        };

        // Setup TimingAggregator to return expected aggregated timing
        // Router: start 5, finish 20 (5+15); Skill: start 20, finish 45
        // Span from earliest start (5) to latest finish (45) = duration 40
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((40.0, 5.0, 45.0));

        // Act
        var result = _adjuster.CalculateRequiredDurationForChildren(children, durations, skillTimings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(40, result.Value); // Span from 5 to 20 (router) and 20 to 45 (skill) = 5 to 45 = 40
    }

    private static TaskNode CreateTaskNode(Guid id, string name, double duration)
    {
        return new TaskNode
        {
            Id = id,
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration
            },
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 100,
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(Guid id, Guid? parentId, double duration)
    {
        return new SkillExecutionNode
        {
            Id = id,
            ParentId = parentId,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"Skill_{id}",
                Duration = duration,
                StartTime = 0,
                FinishTime = duration,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = $"Skill_{id}",
                    Description = "Test skill",
                    Properties =
                    [
                    ]
                }
            },
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 50,
            ProcedureId = default
        };
    }

    private static RouterNode CreateRouterNode(Guid id, string name, double duration)
    {
        return new RouterNode
        {
            Id = id,
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration,
                Selector = new SimpleVariableSelector
                {
                    Expression = "test_condition"
                },
                Branches = new List<ConditionalBranch>()
            },
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 100,
            ProcedureId = default
        };
    }

    private static RouterNode CreateRouterNodeWithBranches(
        Guid id, string name, double duration, Guid branchTargetId,
        Guid? selectedBranchTargetNodeId = null)
    {
        return new RouterNode
        {
            Id = id,
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration,
                Selector = new SimpleVariableSelector { Expression = "test_condition" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", TargetNodeId = branchTargetId, Priority = 0 }
                },
                SelectedBranchTargetNodeId = selectedBranchTargetNodeId
            },
            Position = new NodePosition { X = 0, Y = 0 },
            Height = 100,
            ProcedureId = default
        };
    }

    #region Nested Router — Pipeline single-pass ordering tests

    [Fact]
    public void AdjustParentTaskDurations_TaskNodeWithRouterChild_RouterTimingNotFedBackToTaskNode()
    {
        // Pipeline: TaskNodeDurationCalculator runs first, then RouterNodeDurationCalculator.
        // BranchTask contains NestedRouter. TaskNodeDurationCalculator can't see the router child
        // (ChildNodeCollector bug), so BranchTask gets null/original duration.
        // RouterNodeDurationCalculator then computes NestedRouter's duration = 30.
        // But BranchTask's duration is NEVER updated with this information.
        //
        // Expected: BranchTask duration should include the nested router's 30s.
        // Actual: BranchTask duration stays at its original or is missing.

        var outerRouterId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var nestedRouterId = Guid.NewGuid();
        var innerBranchTaskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var outerRouter = CreateRouterNodeWithBranches(outerRouterId, "OuterRouter", 5.0,
            branchTaskId, branchTaskId);
        var branchTask = CreateTaskNode(branchTaskId, "BranchTask", 10.0);
        branchTask.ParentId = outerRouterId;
        var nestedRouter = CreateRouterNodeWithBranches(nestedRouterId, "NestedRouter", 3.0,
            innerBranchTaskId, innerBranchTaskId);
        nestedRouter.ParentId = branchTaskId;
        var innerBranchTask = CreateTaskNode(innerBranchTaskId, "InnerBranchTask", 10.0);
        innerBranchTask.ParentId = nestedRouterId;
        var skill = CreateSkillExecutionNode(skillId, innerBranchTaskId, 30.0);

        var nodeHierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask, innerBranchTask }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { outerRouter, nestedRouter }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { outerRouterId, new List<Node> { branchTask }.AsReadOnly() },
                { branchTaskId, new List<Node> { nestedRouter }.AsReadOnly() },
                { nestedRouterId, new List<Node> { innerBranchTask }.AsReadOnly() },
                { innerBranchTaskId, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { innerBranchTaskId, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, innerBranchTask }
            }
        };

        var durations = new Dictionary<Guid, double>
        {
            { outerRouterId, 5.0 },
            { branchTaskId, 10.0 },
            { nestedRouterId, 3.0 },
            { innerBranchTaskId, 10.0 }
        };

        var skillTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillId] = (30.0, 0.0, 30.0)
        };

        // TaskNodeDurationCalculator: InnerBranchTask gets skill timing.
        // BranchTask has nestedRouter as child — ChildNodeCollector now includes RouterNode children,
        // so BranchTask correctly includes the nested router's timing.
        var taskNodeSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [innerBranchTaskId] = (30.0, 0.0, 30.0), // Correct: computed from skill
            [branchTaskId] = (30.0, 0.0, 30.0) // Correct: includes nested router child timing
        };

        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>()))
            .Returns(taskNodeSchedules);

        // RouterNodeDurationCalculator: gets combinedTimings = skillTimings + taskNodeSchedules
        // NestedRouter selects innerBranchTask → gets (30, 0, 30)
        // OuterRouter selects branchTask → gets (30, 0, 30) — correct with the fix
        var routerNodeSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [nestedRouterId] = (30.0, 0.0, 30.0), // Correct: from innerBranchTask
            [outerRouterId] = (30.0, 0.0, 30.0) // Correct: from branchTask's corrected timing
        };

        _mockRouterNodeDurationCalculator
            .Setup(x => x.CalculateRouterNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>(),
                It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()))
            .Returns(routerNodeSchedules);

        // Act
        _adjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillTimings);

        // Assert — BranchTask correctly spans the nested router child (duration 30)
        durations[branchTaskId].Should().Be(30.0,
            "BranchTask must span its nested router child (duration 30)");
    }

    [Fact]
    public void AdjustParentTaskDurations_RouterTimingNeverFedBackToCombinedTimings()
    {
        // The pipeline in AdjustParentTaskDurations (lines 87-98):
        //   1. combinedTimings = skillTimings + taskNodeSchedules
        //   2. RouterNodeDurationCalculator.CalculateRouterNodeSchedules(allNodes, combinedTimings)
        //
        // Router results are applied to `durations` dict but NEVER added to `combinedTimings`.
        // So if TaskNode A depends on Router B's timing (e.g. A is Router B's parent),
        // A's timing was already computed in step 1 without B's result.
        //
        // This test verifies that the pipeline is single-pass and router results don't flow back.

        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router = CreateRouterNodeWithBranches(routerId, "Router1", 5.0,
            branchTaskId, branchTaskId);
        var branchTask = CreateTaskNode(branchTaskId, "BranchTask", 10.0);
        branchTask.ParentId = routerId;
        var skill = CreateSkillExecutionNode(skillId, branchTaskId, 20.0);

        var nodeHierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { routerId, new List<Node> { branchTask }.AsReadOnly() },
                { branchTaskId, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTaskId, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, branchTask }
            }
        };

        var durations = new Dictionary<Guid, double>
        {
            { routerId, 5.0 },
            { branchTaskId, 10.0 }
        };

        var skillTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillId] = (20.0, 0.0, 20.0)
        };

        // TaskNodeDurationCalculator correctly computes BranchTask
        var taskNodeSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchTaskId] = (20.0, 0.0, 20.0)
        };
        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>()))
            .Returns(taskNodeSchedules);

        // RouterNodeDurationCalculator correctly computes Router1 from BranchTask
        var routerSchedules = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [routerId] = (20.0, 0.0, 20.0)
        };
        _mockRouterNodeDurationCalculator
            .Setup(x => x.CalculateRouterNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>(),
                It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()))
            .Returns(routerSchedules);

        // Act
        _adjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillTimings);

        // Assert — simple case works: Router gets its duration from BranchTask
        durations[routerId].Should().Be(20.0, "Router1 correctly gets BranchTask timing (20)");
        durations[branchTaskId].Should().Be(20.0, "BranchTask correctly computed from skill (20)");
    }

    [Fact]
    public void AdjustParentTaskDurations_LegacyFallback_RouterDurationMustAlsoBeAdjusted()
    {
        // When TaskNodeDurationCalculator returns empty, the legacy fallback runs.
        // BranchTask (child of Router1) gets correctly adjusted to 30 from skill timing.
        // Router1 must ALSO be adjusted to span its BranchTask (30), not stay at original (5).

        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router = CreateRouterNodeWithBranches(routerId, "Router1", 5.0,
            branchTaskId);
        var branchTask = CreateTaskNode(branchTaskId, "BranchTask", 10.0);
        branchTask.ParentId = routerId;
        var skill = CreateSkillExecutionNode(skillId, branchTaskId, 30.0);

        var nodeHierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { routerId, new List<Node> { branchTask }.AsReadOnly() },
                { branchTaskId, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        var durations = new Dictionary<Guid, double>
        {
            { routerId, 5.0 },
            { branchTaskId, 10.0 }
        };

        var skillTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [skillId] = (30.0, 0.0, 30.0)
        };

        // TaskNodeDurationCalculator returns empty → triggers legacy path
        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double, double, double)>>()))
            .Returns(new Dictionary<Guid, (double, double, double)>());

        _mockHierarchicalSorter
            .Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IEnumerable<TaskNode>>()))
            .Returns<IEnumerable<TaskNode>>(nodes => nodes.ToList().AsReadOnly());

        // TimingAggregator: skill span = 30
        _mockTimingAggregator.Setup(m => m.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((30.0, 0.0, 30.0));

        // Act
        _adjuster.AdjustParentTaskDurations(nodeHierarchy, durations, skillTimings);

        // Assert — BranchTask gets adjusted
        durations[branchTaskId].Should().Be(30.0, "BranchTask duration adjusted from skill timing");

        // Router1 must also be adjusted to span its branch task
        durations[routerId].Should().Be(30.0,
            "Router1 must be adjusted to span its BranchTask (30), not stay at original (5). " +
            "The legacy fallback only iterates TaskNodes, so RouterNode parents are never adjusted");
    }

    #endregion
}