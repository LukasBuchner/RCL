using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;
using Node = FHOOE.Freydis.Domain.Entities.Procedure.Node;
using TaskNode = FHOOE.Freydis.Domain.Entities.Procedure.TaskNode;
using SkillExecutionNode = FHOOE.Freydis.Domain.Entities.Procedure.SkillExecutionNode;
using DependencyEdge = FHOOE.Freydis.Domain.Entities.Procedure.DependencyEdge;
using NodePosition = FHOOE.Freydis.Domain.Entities.Procedure.NodePosition;
using SkillExecutionTask = FHOOE.Freydis.Domain.Entities.Procedure.SkillExecutionTask;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Comprehensive unit tests for TimingCalculationOrchestrator
///     covering the main orchestration logic, error handling, cancellation, and all phases.
/// </summary>
public class TimingCalculationOrchestratorTests
{
    private readonly Mock<IDurationProviderFactory> _mockDurationProviderFactory;
    private readonly Mock<INodeHierarchyProcessor> _mockHierarchyProcessor;
    private readonly Mock<ILogger<TimingCalculationOrchestrator>> _mockLogger;
    private readonly Mock<INodePositioningService> _mockNodePositioningService;
    private readonly Mock<ISchedulingPhaseLogger> _mockSchedulingPhaseLogger;
    private readonly Mock<ITimingAnalyzer> _mockTimingAnalyzer;
    private readonly Mock<ITimingCalculationEngine> _mockTimingEngine;
    private readonly TimingCalculationOrchestrator _pipeline;
    private readonly IScheduleResultConverter _scheduleResultConverter;

    public TimingCalculationOrchestratorTests()
    {
        _mockHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        _mockTimingEngine = new Mock<ITimingCalculationEngine>();
        _mockDurationProviderFactory = new Mock<IDurationProviderFactory>();
        _mockNodePositioningService = new Mock<INodePositioningService>();
        _mockTimingAnalyzer = new Mock<ITimingAnalyzer>();
        _mockSchedulingPhaseLogger = new Mock<ISchedulingPhaseLogger>();
        _mockLogger = new Mock<ILogger<TimingCalculationOrchestrator>>();
        _scheduleResultConverter =
            new ScheduleResultConverter(new Mock<ILogger<ScheduleResultConverter>>().Object); // Use real implementation

        // Setup mock duration provider factory to return a planning provider
        var mockPlanningProvider = new Mock<ISkillDurationProvider>();
        // Note: We don't need to setup AnalyzeAsync for these tests as it's not called directly in constructor

        _mockDurationProviderFactory
            .Setup(f => f.CreateDurationProvider(It.IsAny<bool>(), It.IsAny<DateTime?>(),
                It.IsAny<IReadOnlyDictionary<Guid, SkillExecutionProgress>?>()))
            .Returns(mockPlanningProvider.Object);

        // Setup NodePositioningService to return input nodes unchanged
        _mockNodePositioningService
            .Setup(s => s.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns((IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, NodeTimingInfo> _,
                IReadOnlyDictionary<Guid, IReadOnlyList<Node>> _) => nodes);

        // Setup TimingAnalyzer with default values
        _mockTimingAnalyzer
            .Setup(a => a.CollectStatistics(It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>()))
            .Returns(new TimingStatistics
            {
                MinDuration = 0,
                MaxDuration = 0,
                AverageDuration = 0,
                SumDuration = 0,
                NodeCount = 0,
                EarliestStart = 0,
                LatestFinish = 0,
                TotalProcedureSpan = 0
            });

        _mockTimingAnalyzer
            .Setup(a => a.AnalyzeCriticalPath(It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyList<Node>>()))
            .Returns(new CriticalPathInfo { CriticalPathNodeIds = [], MaxParallelism = 1, PeakParallelismTime = 0 });

        _pipeline = new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act & Assert - Constructor called in setup, no exception should be thrown
        Assert.NotNull(_pipeline);
    }

    [Fact]
    public void Constructor_WithNullNodeHierarchyProcessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            null!,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullTimingCalculationEngine_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            null!,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullScheduleResultConverter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            null!,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullDurationProviderFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            null!,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullNodePositioningService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            null!,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullTimingAnalyzer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            null!,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullSchedulingPhaseLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            null!,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimingCalculationOrchestrator(
            _mockHierarchyProcessor.Object,
            _mockTimingEngine.Object,
            _scheduleResultConverter,
            _mockDurationProviderFactory.Object,
            _mockNodePositioningService.Object,
            _mockTimingAnalyzer.Object,
            _mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            null!
        ));
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task CalculateAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _pipeline.CalculateAsync(null!));
    }

    [Fact]
    public async Task CalculateAsync_WithEmptyNodeList_ShouldReturnEmptyResult()
    {
        // Arrange
        var request = CreateSchedulingRequest(
            new List<Node>(),
            new List<DependencyEdge>()
        );

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.UpdatedNodes);
        Assert.Empty(result.UpdatedNodes);

        // Verify no processing methods were called
        _mockHierarchyProcessor.Verify(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()), Times.Never);
        _mockTimingEngine.Verify(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
            It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task CalculateAsync_WithValidInputAndSuccessfulTiming_ShouldReturnCompleteResult()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes);

        SetupSuccessfulMocks(mockHierarchy, mockTimingResult);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Debug: Check what's in the result
        var nodeScheduleCount = result.NodeSchedules?.Count ?? 0;
        var updatedNodesCount = result.UpdatedNodes?.Count ?? 0;

        if (!result.Success) throw new Exception($"Pipeline failed: {result.ErrorMessage}");

        if (nodeScheduleCount == 0)
            throw new Exception(
                $"NodeSchedules is empty. Success={result.Success}, UpdatedNodes.Count={updatedNodesCount}, ErrorMessage={result.ErrorMessage}");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.NodeSchedules);
        Assert.NotEmpty(result.NodeSchedules!);
        Assert.NotNull(result.UpdatedNodes);
        Assert.NotEmpty(result.UpdatedNodes);

        // Verify all phases were executed
        VerifyAllPhasesExecuted(nodes, edges);
    }

    [Fact]
    public async Task CalculateAsync_WithNoSkillExecutionNodes_ShouldStillProcessSuccessfully()
    {
        // Arrange - only TaskNodes, no SkillExecutionNodes
        var nodes = new List<Node>
        {
            CreateTaskNode("Task1", Guid.NewGuid()),
            CreateTaskNode("Task2", Guid.NewGuid())
        };
        var edges = new List<DependencyEdge>();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes, false);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes);

        SetupSuccessfulMocks(mockHierarchy, mockTimingResult);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.NodeSchedules);

        // Verify hierarchy processing was still called
        _mockHierarchyProcessor.Verify(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_WithDetailedTimingDisabled_ShouldNotIncludeDetailedTiming()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges, includeDetailedTiming: false);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes, false);

        SetupSuccessfulMocks(mockHierarchy, mockTimingResult);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.NodeSchedules); // When detailed timing is disabled, NodeSchedules should be empty

        // Verify timing engine was called with correct options
        _mockTimingEngine.Verify(x => x.CalculateTimingsAsync(
            It.IsAny<NodeHierarchyInfo>(),
            It.IsAny<IReadOnlyList<DependencyEdge>>(),
            It.Is<TimingCalculationOptions>(opts => !opts.IncludeDetailedTiming),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CalculateAsync_WithTimingCalculationFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var failedTimingResult = CreateFailedTimingResult("Timing calculation failed");

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedTimingResult);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Timing calculation failed", result.ErrorMessage);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.UpdatedNodes);
    }

    [Fact]
    public async Task CalculateAsync_WithHierarchyProcessorException_ShouldReturnFailureResult()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Throws(new InvalidOperationException("Hierarchy processing failed"));

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Hierarchy processing failed", result.ErrorMessage!);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.UpdatedNodes);
    }

    [Fact]
    public async Task CalculateAsync_WithTimingEngineException_ShouldReturnFailureResult()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Timing engine failed"));

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Timing engine failed", result.ErrorMessage!);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.UpdatedNodes);
    }

    [Fact]
    public async Task CalculateAsync_WithPositionCalculatorException_ShouldReturnFailureResult()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTimingResult);

        _mockNodePositioningService.Setup(x =>
                x.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(),
                    It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                    It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Throws(new InvalidOperationException("Position calculation failed"));

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Position calculation failed", result.ErrorMessage!);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CalculateAsync_WithCancellationToken_ShouldPropagateCancellation()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);
        var cancellationToken = new CancellationToken(true); // Already cancelled

        var mockHierarchy = CreateMockHierarchy(nodes);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _pipeline.CalculateAsync(request, cancellationToken));
    }

    [Fact]
    public async Task CalculateAsync_WithCancellationDuringExecution_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);
        var cts = new CancellationTokenSource();

        var mockHierarchy = CreateMockHierarchy(nodes);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                cts.Cancel(); // Cancel during execution
                await Task.Delay(100, cts.Token); // This should throw
                return CreateSuccessfulTimingResult(nodes);
            });

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _pipeline.CalculateAsync(request, cts.Token));
    }

    #endregion

    #region Position Calculation Tests

    [Fact]
    public async Task CalculateAsync_WithValidTiming_ShouldCalculatePositions()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes);

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(mockHierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTimingResult);

        // Setup NodePositioningService to return nodes with updated positions
        var updatedNodes = nodes.Select(n => n switch
        {
            TaskNode tn => tn with { Position = new NodePosition { X = 100.0, Y = 200.0 } },
            SkillExecutionNode sn => sn with { Position = new NodePosition { X = 100.0, Y = 200.0 } },
            _ => n
        }).ToList();

        _mockNodePositioningService
            .Setup(s => s.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns(updatedNodes);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.True(result.Success);

        // Verify positioning service was called
        _mockNodePositioningService.Verify(
            x => x.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Once);

        // Verify updated nodes have the calculated positions
        Assert.All(result.UpdatedNodes!, node =>
        {
            Assert.Equal(100.0, node.Position.X);
            Assert.Equal(200.0, node.Position.Y);
        });
    }

    [Fact]
    public async Task CalculateAsync_WithNoDetailedTiming_ShouldStillCallPositioningService()
    {
        // Arrange
        var nodes = CreateTestNodes();
        var edges = CreateTestEdges();
        var request = CreateSchedulingRequest(nodes, edges, includeDetailedTiming: false);

        var mockHierarchy = CreateMockHierarchy(nodes);
        var mockTimingResult = CreateSuccessfulTimingResult(nodes, false);

        SetupSuccessfulMocks(mockHierarchy, mockTimingResult);

        // Act
        var result = await _pipeline.CalculateAsync(request);

        // Assert
        Assert.True(result.Success);

        // In the refactored implementation, positioning service is always called
        // It handles the null DetailedTimingInfo internally
        _mockNodePositioningService.Verify(
            x => x.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(), null,
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Once);
    }

    #endregion

    #region Test Helper Methods

    private SchedulingRequest CreateSchedulingRequest(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        bool strictMode = false,
        bool preserveOriginalTaskDurations = true,
        bool includeDetailedTiming = true)
    {
        return new SchedulingRequest
        {
            ProcedureId = Guid.NewGuid(),
            Nodes = nodes,
            Edges = edges,
            StrictMode = strictMode,
            PreserveOriginalTaskDurations = preserveOriginalTaskDurations,
            IncludeDetailedTiming = includeDetailedTiming
        };
    }

    private List<Node> CreateTestNodes()
    {
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();

        return
        [
            CreateTaskNode("Task1", taskId1),
            CreateTaskNode("Task2", taskId2),
            CreateSkillExecutionNode("Skill1", skillId1, taskId1),
            CreateSkillExecutionNode("Skill2", skillId2, taskId2)
        ];
    }

    private TaskNode CreateTaskNode(string name, Guid id, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = name,
                StartTime = 0,
                Duration = 60,
                FinishTime = 60
            }
        };
    }

    private SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid id, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                StartTime = 0,
                Duration = 60,
                FinishTime = 60,
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

    private List<DependencyEdge> CreateTestEdges()
    {
        return
        [
            new DependencyEdge
            {
                Id = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                SourceHandle = "output",
                TargetHandle = "input",
                ProcedureId = default
            }
        ];
    }

    private NodeHierarchyInfo CreateMockHierarchy(IReadOnlyList<Node> nodes, bool hasSkills = true)
    {
        var taskNodes = nodes.OfType<TaskNode>().ToList().AsReadOnly();
        var skillNodes = hasSkills
            ? nodes.OfType<SkillExecutionNode>().ToList().AsReadOnly()
            : new List<SkillExecutionNode>().AsReadOnly();

        var parentMapping = new Dictionary<Guid, IReadOnlyList<Node>>();
        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>();
        var skillToTaskMapping = new Dictionary<Guid, TaskNode>();

        return new NodeHierarchyInfo
        {
            TaskNodes = taskNodes,
            SkillExecutionNodes = skillNodes,
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = parentMapping.AsReadOnly(),
            TaskToSkillMapping = taskToSkillMapping.AsReadOnly(),
            SkillToTaskMapping = skillToTaskMapping.AsReadOnly()
        };
    }

    private TimingResult CreateSuccessfulTimingResult(IReadOnlyList<Node> nodes, bool includeDetailedTiming = true)
    {
        var durations = nodes.ToDictionary(n => n.Id, _ => 60.0);

        Dictionary<Guid, NodeTimingInfo>? detailedTiming = null;
        if (includeDetailedTiming)
            detailedTiming = nodes.ToDictionary(n => n.Id, n => new NodeTimingInfo
            {
                Duration = 60.0,
                AbsoluteStartTime = 0.0,
                AbsoluteFinishTime = 60.0,
                RelativeStartTime = 0.0,
                RelativeFinishTime = 60.0,
                NodeType = n is TaskNode ? NodeTimingType.Task : NodeTimingType.SkillExecution,
                IsCalculated = true
            });

        return new TimingResult
        {
            Durations = durations.AsReadOnly(),
            DetailedTimingInfo = detailedTiming?.AsReadOnly(),
            UpdatedNodes = nodes,
            Success = true,
            Statistics = new TimingCalculationStatistics()
        };
    }

    private TimingResult CreateFailedTimingResult(string errorMessage)
    {
        return new TimingResult
        {
            Durations = new Dictionary<Guid, double>().AsReadOnly(),
            UpdatedNodes = new List<Node>().AsReadOnly(),
            Success = false,
            ErrorMessage = errorMessage,
            Statistics = new TimingCalculationStatistics()
        };
    }

    private void SetupSuccessfulMocks(NodeHierarchyInfo hierarchy, TimingResult timingResult)
    {
        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(hierarchy);

        _mockTimingEngine.Setup(x => x.CalculateTimingsAsync(It.IsAny<NodeHierarchyInfo>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(), It.IsAny<TimingCalculationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(timingResult);

        // NodePositioningService setup is already done in constructor
        // TimingAnalyzer setup is already done in constructor
        // Note: _scheduleResultConverter is using real implementation, no setup needed
    }

    private void VerifyAllPhasesExecuted(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)
    {
        _mockHierarchyProcessor.Verify(x => x.ProcessHierarchy(
            It.Is<IReadOnlyList<Node>>(n => n.Count == nodes.Count)), Times.Once);

        _mockTimingEngine.Verify(x => x.CalculateTimingsAsync(
            It.IsAny<NodeHierarchyInfo>(),
            It.Is<IReadOnlyList<DependencyEdge>>(e => e.Count == edges.Count),
            It.IsAny<TimingCalculationOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockNodePositioningService.Verify(x => x.ApplyPositionsAndHeights(
            It.IsAny<IReadOnlyList<Node>>(),
            It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
            It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()), Times.Once);
    }

    #endregion
}