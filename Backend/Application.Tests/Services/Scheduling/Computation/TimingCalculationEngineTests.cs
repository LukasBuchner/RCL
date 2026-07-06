using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using Moq;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Computation;

/// <summary>
///     Comprehensive unit tests for TimingCalculation
///     <IExecutionGraphBuilder>
///         ver all major scenarios: no skills, execution failures, sch
///         <TimingCalculationEngine>
///             aths, and error
///             handling.
/// </summary>
public class TimingCalculationEngineTests
{
    private readonly TimingCalculationEngine _engine;
    private readonly Mock<IExecutionGraphBuilder> _mockExecutionGraphBuilder;
    private readonly Mock<ILogger<TimingCalculationEngine>> _mockLogger;
    private readonly Mock<INodeDurationAdjuster> _mockNodeDurationAdjuster;
    private readonly Mock<INodeResolver> _mockNodeResolver;
    private readonly Mock<INodeTimingMapper> _mockNodeTimingMapper;
    private readonly Mock<ISchedulePlanner> _mockSchedulePlanner;
    private readonly Mock<ITaskNodeDurationCalculator> _mockTaskNodeDurationCalculator;
    private readonly Mock<ITimingStatisticsCollector> _mockTimingStatisticsCollector;

    public TimingCalculationEngineTests()
    {
        _mockExecutionGraphBuilder = new Mock<IExecutionGraphBuilder>();
        _mockSchedulePlanner = new Mock<ISchedulePlanner>();
        _mockTaskNodeDurationCalculator = new Mock<ITaskNodeDurationCalculator>();
        _mockTimingStatisticsCollector = new Mock<ITimingStatisticsCollector>();
        _mockNodeDurationAdjuster = new Mock<INodeDurationAdjuster>();
        _mockNodeTimingMapper = new Mock<INodeTimingMapper>();
        _mockLogger = new Mock<ILogger<TimingCalculationEngine>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockNodeResolver = new Mock<INodeResolver>();
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(It.IsAny<Guid>(), It.IsAny<NodeHierarchyInfo>()))
            .Returns((Guid id, NodeHierarchyInfo _) => new[] { id });

        _engine = new TimingCalculationEngine(
            _mockExecutionGraphBuilder.Object,
            _mockSchedulePlanner.Object,
            _mockTaskNodeDurationCalculator.Object,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            _mockNodeResolver.Object,
            _mockLogger.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act & Assert - Constructor called in setup, no exception should be thrown
        Assert.NotNull(_engine);
    }

    [Fact]
    public void Constructor_WithNullExecutionGraphBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationEngine(
            null!,
            _mockSchedulePlanner.Object,
            _mockTaskNodeDurationCalculator.Object,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            _mockNodeResolver.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullSchedulePlanner_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationEngine(
            _mockExecutionGraphBuilder.Object,
            null!,
            _mockTaskNodeDurationCalculator.Object,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            _mockNodeResolver.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullTaskNodeDurationCalculator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationEngine(
            _mockExecutionGraphBuilder.Object,
            _mockSchedulePlanner.Object,
            null!,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            _mockNodeResolver.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationEngine(
            _mockExecutionGraphBuilder.Object,
            _mockSchedulePlanner.Object,
            _mockTaskNodeDurationCalculator.Object,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            _mockNodeResolver.Object,
            null!
        ));
    }

    [Fact]
    public void Constructor_WithNullNodeResolver_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationEngine(
            _mockExecutionGraphBuilder.Object,
            _mockSchedulePlanner.Object,
            _mockTaskNodeDurationCalculator.Object,
            _mockTimingStatisticsCollector.Object,
            _mockNodeDurationAdjuster.Object,
            _mockNodeTimingMapper.Object,
            null!,
            _mockLogger.Object
        ));
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithNullNodeHierarchy_ShouldThrowArgumentNullException()
    {
        // Arrange
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _engine.CalculateTimingsAsync(null!, edges, options));
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithNullEdges_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodeHierarchy = CreateMockNodeHierarchy();
        var options = CreateTimingCalculationOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _engine.CalculateTimingsAsync(nodeHierarchy, null!, options));
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodeHierarchy = CreateMockNodeHierarchy();
        var edges = new List<DependencyEdge>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _engine.CalculateTimingsAsync(nodeHierarchy, edges, null!));
    }

    #endregion

    #region No Skill Execution Nodes Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithNoSkillExecutionNodes_ShouldReturnSuccessWithOriginalDurations()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0, 10.0, 70.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, []);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.Durations);
        Assert.Equal(60.0, result.Durations[taskNodes[0].Id]);
        Assert.NotNull(result.DetailedTimingInfo);
        Assert.Single(result.DetailedTimingInfo);
        Assert.Equal(NodeTimingType.Original, result.DetailedTimingInfo[taskNodes[0].Id].NodeType);
        Assert.False(result.DetailedTimingInfo[taskNodes[0].Id].IsCalculated);

        // Verify no mocks were called since there are no skills
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()),
            Times.Never);
        _mockSchedulePlanner.Verify(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task
        CalculateTimingsAsync_WithNoSkillsAndDetailedTimingDisabled_ShouldReturnSuccessWithoutDetailedTiming()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0, 10.0, 70.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, []);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions(includeDetailedTiming: false);

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Durations);
        Assert.Null(result.DetailedTimingInfo); // Should be null when disabled
    }

    #endregion

    #region Execution Graph Building Failure Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithExecutionGraphBuildingFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync((IExecutionGraph?)null);

        SetupTaskNodeDurationCalculatorForFailure();

        // Setup statistics collector to return proper statistics
        _mockTimingStatisticsCollector.Setup(x => x.UpdateStatistics(
                It.IsAny<TimingCalculationStatistics>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>()))
            .Returns(new TimingCalculationStatistics
            {
                SkillExecutionNodesProcessed = 1,
                TaskNodesProcessed = 1,
                ExecutionGraphBuildTime = TimeSpan.FromMilliseconds(10),
                TotalTime = TimeSpan.FromMilliseconds(20)
            });

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to build execution graph", result.ErrorMessage!);
        Assert.True(result.Statistics.ExecutionGraphBuildTime > TimeSpan.Zero);

        // Verify execution graph builder was called
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()),
            Times.Once);

        // Verify scheduler was not called
        _mockSchedulePlanner.Verify(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithEmptyExecutionGraph_ShouldReturnSuccessResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        var mockExecutionGraph = new Mock<IExecutionGraph>();
        mockExecutionGraph.Setup(x => x.SkillExecutions).Returns(new List<IPlannedSkillExecution>().AsReadOnly());

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        SetupTaskNodeDurationCalculatorForFailure();

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success); // Empty execution graph is handled as success with fallback

        // Verify execution graph builder was called
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()),
            Times.Once);

        // Verify scheduler was not called for empty graph
        _mockSchedulePlanner.Verify(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()), Times.Never);
    }

    #endregion

    #region Schedule Planning Failure Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithSchedulePlanningFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        var mockExecutionGraph = CreateMockExecutionGraph();

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(false); // Planning fails

        SetupTaskNodeDurationCalculatorForFailure();

        // Setup statistics collector to return proper statistics
        _mockTimingStatisticsCollector.Setup(x => x.UpdateStatistics(
                It.IsAny<TimingCalculationStatistics>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>()))
            .Returns(new TimingCalculationStatistics
            {
                SkillExecutionNodesProcessed = 1,
                TaskNodesProcessed = 1,
                SchedulingTime = TimeSpan.FromMilliseconds(10),
                TotalTime = TimeSpan.FromMilliseconds(20)
            });

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to plan schedule", result.ErrorMessage!);
        Assert.True(result.Statistics.SchedulingTime > TimeSpan.Zero);

        // Verify both services were called
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()),
            Times.Once);
        _mockSchedulePlanner.Verify(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithSchedulePlanningFailureAndStrictMode_ShouldReturnFailureResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions(strictMode: true);

        var mockExecutionGraph = CreateMockExecutionGraph();

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(false);

        SetupTaskNodeDurationCalculatorForFailure();

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to plan schedule", result.ErrorMessage!);

        // Verify strict mode was passed to execution graph builder
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
            It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), true), Times.Once);
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithSuccessfulExecution_ShouldReturnCompleteResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        var mockExecutionGraph = CreateMockExecutionGraph();
        var mockSkillExecution = CreateMockSkillExecution(skillNodes[0].Id, 30.0, 10.0, 40.0);
        mockExecutionGraph.Setup(x => x.SkillExecutions).Returns(new[] { mockSkillExecution.Object }.AsReadOnly());

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(true);

        SetupTaskNodeDurationCalculatorForSuccess(taskNodes);

        // Setup statistics collector for successful execution
        _mockTimingStatisticsCollector.Setup(x => x.CreateStatistics())
            .Returns(new TimingCalculationStatistics());

        _mockTimingStatisticsCollector.Setup(x => x.UpdateStatistics(
                It.IsAny<TimingCalculationStatistics>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>()))
            .Returns(new TimingCalculationStatistics
            {
                SkillExecutionNodesProcessed = skillNodes.Count,
                TaskNodesProcessed = taskNodes.Count,
                ExecutionGraphBuildTime = TimeSpan.FromMilliseconds(50),
                SchedulingTime = TimeSpan.FromMilliseconds(100),
                DomainUpdateTime = TimeSpan.FromMilliseconds(25),
                TotalTime = TimeSpan.FromMilliseconds(200)
            });

        // Setup node duration adjuster
        _mockNodeDurationAdjuster.Setup(x => x.AdjustParentTaskDurations(
            It.IsAny<NodeHierarchyInfo>(),
            It.IsAny<Dictionary<Guid, double>>(),
            It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>(),
            It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()));

        // Setup node timing mapper
        _mockNodeTimingMapper.Setup(x => x.ApplyTimingToNode(
                It.IsAny<Node>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>(),
                It.IsAny<IReadOnlyDictionary<Guid, double>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IPlannedSkillExecution>?>()))
            .Returns((Node node, IReadOnlyDictionary<Guid, NodeTimingInfo>? _,
                IReadOnlyDictionary<Guid, double> _,
                IReadOnlyDictionary<Guid, IPlannedSkillExecution>? _) => node);

        _mockNodeTimingMapper.Setup(x => x.AdjustRelativeStartTimesForHierarchy(
            It.IsAny<Dictionary<Guid, NodeTimingInfo>>(),
            It.IsAny<IReadOnlyList<Node>>()));

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotEmpty(result.Durations);
        Assert.NotEmpty(result.UpdatedNodes);
        Assert.NotNull(result.DetailedTimingInfo);
        Assert.True(result.Statistics.TotalTime > TimeSpan.Zero);
        Assert.True(result.Statistics.ExecutionGraphBuildTime > TimeSpan.Zero);
        Assert.True(result.Statistics.SchedulingTime > TimeSpan.Zero);
        Assert.True(result.Statistics.DomainUpdateTime > TimeSpan.Zero);
        Assert.Equal(skillNodes.Count, result.Statistics.SkillExecutionNodesProcessed);
        Assert.Equal(taskNodes.Count, result.Statistics.TaskNodesProcessed);

        // Verify all services were called in sequence
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()),
            Times.Once);
        _mockSchedulePlanner.Verify(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()), Times.Once);
        _mockTaskNodeDurationCalculator.Verify(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task
        CalculateTimingsAsync_WithSuccessfulExecutionAndDetailedTimingDisabled_ShouldReturnResultWithoutDetailedTiming()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions(includeDetailedTiming: false);

        var mockExecutionGraph = CreateMockExecutionGraph();
        var mockSkillExecution = CreateMockSkillExecution(skillNodes[0].Id, 30.0, 10.0, 40.0);
        mockExecutionGraph.Setup(x => x.SkillExecutions).Returns(new[] { mockSkillExecution.Object }.AsReadOnly());

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(true);

        SetupTaskNodeDurationCalculatorForSuccess(taskNodes);

        // Setup statistics collector for successful execution
        _mockTimingStatisticsCollector.Setup(x => x.CreateStatistics())
            .Returns(new TimingCalculationStatistics());

        _mockTimingStatisticsCollector.Setup(x => x.UpdateStatistics(
                It.IsAny<TimingCalculationStatistics>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>()))
            .Returns(new TimingCalculationStatistics
            {
                SkillExecutionNodesProcessed = skillNodes.Count,
                TaskNodesProcessed = taskNodes.Count,
                ExecutionGraphBuildTime = TimeSpan.FromMilliseconds(50),
                SchedulingTime = TimeSpan.FromMilliseconds(100),
                DomainUpdateTime = TimeSpan.FromMilliseconds(25),
                TotalTime = TimeSpan.FromMilliseconds(200)
            });

        // Setup node duration adjuster
        _mockNodeDurationAdjuster.Setup(x => x.AdjustParentTaskDurations(
            It.IsAny<NodeHierarchyInfo>(),
            It.IsAny<Dictionary<Guid, double>>(),
            It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>(),
            It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()));

        // Setup node timing mapper
        _mockNodeTimingMapper.Setup(x => x.ApplyTimingToNode(
                It.IsAny<Node>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>?>(),
                It.IsAny<IReadOnlyDictionary<Guid, double>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IPlannedSkillExecution>?>()))
            .Returns((Node node, IReadOnlyDictionary<Guid, NodeTimingInfo>? _,
                IReadOnlyDictionary<Guid, double> _,
                IReadOnlyDictionary<Guid, IPlannedSkillExecution>? _) => node);

        _mockNodeTimingMapper.Setup(x => x.AdjustRelativeStartTimesForHierarchy(
            It.IsAny<Dictionary<Guid, NodeTimingInfo>>(),
            It.IsAny<IReadOnlyList<Node>>()));

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Durations);
        Assert.Null(result.DetailedTimingInfo); // Should be null when disabled
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithExecutionGraphBuilderException_ShouldReturnFailureResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Graph building failed"));

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Graph building failed", result.ErrorMessage!);
        Assert.Empty(result.Durations);
        Assert.Empty(result.UpdatedNodes);
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithSchedulePlannerException_ShouldReturnFailureResult()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();

        var mockExecutionGraph = CreateMockExecutionGraph();
        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Throws(new InvalidOperationException("Scheduling failed"));

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Scheduling failed", result.ErrorMessage!);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithCancellationToken_ShouldPropagateCancellation()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();
        var cancellationToken = new CancellationToken(true);

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _engine.CalculateTimingsAsync(nodeHierarchy, edges, options, cancellationToken));
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithCancellationDuringExecution_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions();
        var cts = new CancellationTokenSource();

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .Returns(async () =>
            {
                cts.Cancel();
                await Task.Delay(100, cts.Token);
                return CreateMockExecutionGraph().Object;
            });

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _engine.CalculateTimingsAsync(nodeHierarchy, edges, options, cts.Token));
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public async Task CalculateTimingsAsync_WithPreserveOriginalTaskDurations_ShouldPassOptionToCalculator()
    {
        // Arrange
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions(preserveOriginalTaskDurations: false);

        SetupSuccessfulExecution(taskNodes, skillNodes);

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);

        // Verify options were used appropriately
        _mockExecutionGraphBuilder.Verify(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), options.StrictMode),
            Times.Once);
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithDifferentProcedureId_ShouldIncludeInLogs()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var taskNodes = new List<TaskNode> { CreateTaskNode("Task1", 60.0) };
        var skillNodes = new List<SkillExecutionNode> { CreateSkillExecutionNode("Skill1", 30.0) };
        var nodeHierarchy = CreateMockNodeHierarchy(taskNodes, skillNodes);
        var edges = new List<DependencyEdge>();
        var options = CreateTimingCalculationOptions(procedureId);

        SetupSuccessfulExecution(taskNodes, skillNodes);

        // Act
        var result = await _engine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);

        // Verify logging includes the correct procedure ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(procedureId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Test Helper Methods

    private TimingCalculationOptions CreateTimingCalculationOptions(
        Guid? procedureId = null,
        bool strictMode = false,
        bool preserveOriginalTaskDurations = true,
        bool includeDetailedTiming = true)
    {
        // Create a mock duration provider for tests
        var mockDurationProvider = new Mock<ISkillDurationProvider>();

        return new TimingCalculationOptions
        {
            ProcedureId = procedureId ?? Guid.NewGuid(),
            StrictMode = strictMode,
            PreserveOriginalTaskDurations = preserveOriginalTaskDurations,
            IncludeDetailedTiming = includeDetailedTiming,
            DurationProvider = mockDurationProvider.Object
        };
    }

    private NodeHierarchyInfo CreateMockNodeHierarchy(
        List<TaskNode>? taskNodes = null,
        List<SkillExecutionNode>? skillNodes = null)
    {
        taskNodes ??= [];
        skillNodes ??= [];

        return new NodeHierarchyInfo
        {
            TaskNodes = taskNodes.AsReadOnly(),
            SkillExecutionNodes = skillNodes.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>().AsReadOnly(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>().AsReadOnly(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>().AsReadOnly()
        };
    }

    private TaskNode CreateTaskNode(string name, double duration, double startTime = 0.0, double? finishTime = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = name,
                Duration = duration,
                StartTime = startTime,
                FinishTime = finishTime ?? startTime + duration
            }
        };
    }

    private SkillExecutionNode CreateSkillExecutionNode(string skillName, double duration, double startTime = 0.0,
        double? finishTime = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = duration,
                StartTime = startTime,
                FinishTime = finishTime ?? startTime + duration,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = $"{skillName} description",
                    Properties = []
                }
            }
        };
    }

    private Mock<IExecutionGraph> CreateMockExecutionGraph()
    {
        var mock = new Mock<IExecutionGraph>();
        var mockSkillExecution = CreateMockSkillExecution(Guid.NewGuid(), 30.0, 0.0, 30.0);
        mock.Setup(x => x.SkillExecutions).Returns(new[] { mockSkillExecution.Object }.AsReadOnly());
        return mock;
    }

    private Mock<IPlannedSkillExecution> CreateMockSkillExecution(Guid id, double duration, double startTime,
        double finishTime)
    {
        var mock = new Mock<IPlannedSkillExecution>();
        mock.Setup(x => x.Id).Returns(id);
        mock.Setup(x => x.PlannedDuration).Returns(duration);
        mock.Setup(x => x.PlannedStartTime).Returns(startTime);
        mock.Setup(x => x.PlannedFinishTime).Returns(finishTime);
        return mock;
    }

    private void SetupTaskNodeDurationCalculatorForFailure()
    {
        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>()))
            .Returns(new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>().AsReadOnly());
    }

    private void SetupTaskNodeDurationCalculatorForSuccess(List<TaskNode> taskNodes)
    {
        var schedules = taskNodes.ToDictionary(tn => tn.Id, tn => (
            Duration: tn.Task.Duration + 10,
            tn.Task.StartTime,
            FinishTime: tn.Task.StartTime + tn.Task.Duration + 10
        ));

        _mockTaskNodeDurationCalculator
            .Setup(x => x.CalculateTaskNodeSchedules(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, (double Duration, double StartTime, double FinishTime)>>()))
            .Returns(schedules.AsReadOnly());
    }

    private void SetupSuccessfulExecution(List<TaskNode> taskNodes, List<SkillExecutionNode> skillNodes)
    {
        var mockExecutionGraph = CreateMockExecutionGraph();
        var skillExecutions = skillNodes.Select(skill =>
        {
            var mock = CreateMockSkillExecution(skill.Id, skill.SkillExecutionTask.Duration, 0.0,
                skill.SkillExecutionTask.Duration);
            return mock.Object;
        }).ToList();

        mockExecutionGraph.Setup(x => x.SkillExecutions).Returns(skillExecutions.AsReadOnly());

        _mockExecutionGraphBuilder.Setup(x => x.BuildAsync(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<ISkillDurationProvider>(), It.IsAny<bool>()))
            .ReturnsAsync(mockExecutionGraph.Object);

        _mockSchedulePlanner.Setup(x => x.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(true);

        SetupTaskNodeDurationCalculatorForSuccess(taskNodes);
    }

    #endregion
}