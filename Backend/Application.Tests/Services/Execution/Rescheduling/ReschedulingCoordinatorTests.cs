using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodeSchedule = FHOOE.Freydis.Application.Services.Scheduling.Models.NodeSchedule;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Rescheduling;

/// <summ
/// <ILogger
/// <ReschedulingCoordinator>
///     >ngCoordinator service.
///     </summary>
public class ReschedulingCoordinatorTests
{
    private readonly ReschedulingCoordinator _coordinator;
    private readonly IReadOnlyList<DependencyEdge> _currentEdges;
    private readonly IReadOnlyList<Node> _currentNodes;
    private readonly DateTimeOffset _executionStartTime;
    private readonly Mock<ILogger<ReschedulingCoordinator>> _mockLogger;
    private readonly Mock<IExecutionProgressDataBuilder> _mockProgressDataBuilder;
    private readonly Mock<IExecutionProgressMonitor> _mockProgressMonitor;
    private readonly Mock<ISkillExecutionStateManager> _mockStateManager;
    private readonly Mock<IExecutionTimeCalculator> _mockTimeCalculator;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;

    // Test context
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly TimeProvider _timeProvider;

    public ReschedulingCoordinatorTests()
    {
        _mockLogger = new Mock<ILogger<ReschedulingCoordinator>>();
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockStateManager = new Mock<ISkillExecutionStateManager>();
        _mockProgressDataBuilder = new Mock<IExecutionProgressDataBuilder>();
        _mockProgressMonitor = new Mock<IExecutionProgressMonitor>();
        _mockTimeCalculator = new Mock<IExecutionTimeCalculator>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _timeProvider = TimeProvider.System;

        // Set up test context
        _executionStartTime = _timeProvider.GetUtcNow().AddSeconds(-10);
        _currentNodes = new List<Node>
        {
            CreateSkillExecutionNode("Skill 1"),
            CreateSkillExecutionNode("Skill 2")
        }.AsReadOnly();
        _currentEdges = new List<DependencyEdge>().AsReadOnly();

        _coordinator = new ReschedulingCoordinator(
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance,
            _timeProvider,
            _mockStateManager.Object,
            _mockProgressDataBuilder.Object,
            _mockProgressMonitor.Object,
            _mockTimeCalculator.Object,
            _mockTimingOrchestrator.Object);

        // Initialize coordinator with test context
        _coordinator.Initialize(_procedureId, _currentNodes, _currentEdges, _executionStartTime);
    }

    [Fact]
    public async Task RescheduleAsync_WithSuccessfulScheduling_ReturnsSuccess()
    {
        // Arrange
        _timeProvider.GetUtcNow();
        var elapsedSeconds = 10.0;
        var states = new List<SkillExecutionState>();
        var progressData = new Dictionary<Guid, SkillExecutionProgress>();
        var updatedNodes = new List<Node> { CreateSkillExecutionNode("Updated Skill") }.AsReadOnly();

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(elapsedSeconds);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(states);

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(states, It.IsAny<DateTimeOffset>()))
            .Returns(progressData);

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = updatedNodes,
                NodeSchedules = new List<NodeSchedule>(),
                ErrorMessage = null
            });

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedNodes);
        Assert.Equal(updatedNodes.Count, result.UpdatedNodes.Count);
        Assert.Equal(elapsedSeconds, result.CurrentTime);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     Verifies the coordinator stamps <see cref="ReschedulingResult.IsExecutionComplete" />
    ///     to reflect whatever the progress monitor reports at the moment the schedule is computed.
    ///     Downstream completion detection in the execution pipeline reads this flag instead of
    ///     querying the monitor directly, so the stamp must be accurate.
    /// </summary>
    [Fact]
    public async Task RescheduleAsync_WhenProgressMonitorReportsComplete_StampsIsExecutionCompleteTrue()
    {
        // Arrange
        ArrangeSuccessfulScheduling();
        _mockProgressMonitor.Setup(m => m.IsExecutionComplete()).Returns(true);

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillFinished, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsExecutionComplete);
    }

    /// <summary>
    ///     Verifies <see cref="ReschedulingResult.IsExecutionComplete" /> is <c>false</c> while the
    ///     execution is still running, so the single-phase completion subscriber does not fire
    ///     prematurely.
    /// </summary>
    [Fact]
    public async Task RescheduleAsync_WhenProgressMonitorReportsIncomplete_StampsIsExecutionCompleteFalse()
    {
        // Arrange
        ArrangeSuccessfulScheduling();
        _mockProgressMonitor.Setup(m => m.IsExecutionComplete()).Returns(false);

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.IsExecutionComplete);
    }

    /// <summary>
    ///     Load-bearing invariant: when <see cref="ReschedulingResult.IsExecutionComplete" /> is
    ///     <c>true</c>, every <see cref="SkillExecutionNode" /> in <c>UpdatedNodes</c> carries a
    ///     non-null finish time. This is the coordinator's contract for the single-phase
    ///     completion collapse — the first complete-stamped result is also the authoritative
    ///     final snapshot. The end-to-end guarantee (that a real timing orchestrator produces
    ///     finish times for every terminal skill) is asserted by integration tests against
    ///     <see cref="ITimingCalculationOrchestrator" /> implementations; this unit test asserts
    ///     the coordinator does not destroy finish times on the way out.
    /// </summary>
    [Fact]
    public async Task RescheduleAsync_WhenComplete_EverySkillNodeHasFinishTime()
    {
        // Arrange — orchestrator returns a complete snapshot with finish times on every skill node.
        var completeNodes = new List<Node>
        {
            CreateSkillExecutionNode("Skill 1"),
            CreateSkillExecutionNode("Skill 2"),
            CreateSkillExecutionNode("Skill 3")
        }.AsReadOnly();

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);
        _mockStateManager.Setup(s => s.GetAllStates()).Returns(new List<SkillExecutionState>());
        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = completeNodes,
                NodeSchedules = new List<NodeSchedule>()
            });
        _mockProgressMonitor.Setup(m => m.IsExecutionComplete()).Returns(true);

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillFinished, CancellationToken.None);

        // Assert
        Assert.True(result.IsExecutionComplete);
        Assert.NotNull(result.UpdatedNodes);
        Assert.All(
            result.UpdatedNodes.OfType<SkillExecutionNode>(),
            skill => Assert.NotNull(skill.SkillExecutionTask.FinishTime));
    }

    /// <summary>
    ///     Shared mock setup for the three completion-stamp tests. Returns a successful schedule
    ///     with the current-node list so each test can focus on the monitor stub.
    /// </summary>
    private void ArrangeSuccessfulScheduling()
    {
        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);
        _mockStateManager.Setup(s => s.GetAllStates()).Returns(new List<SkillExecutionState>());
        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = _currentNodes,
                NodeSchedules = new List<NodeSchedule>()
            });
    }

    [Fact]
    public async Task RescheduleAsync_CallsTimingOrchestratorWithCorrectRequest()
    {
        // Arrange
        var elapsedSeconds = 10.0;
        var states = new List<SkillExecutionState>();
        var progressData = new Dictionary<Guid, SkillExecutionProgress>();
        SchedulingRequest? capturedRequest = null;

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(elapsedSeconds);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(states);

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(states, It.IsAny<DateTimeOffset>()))
            .Returns(progressData);

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ScheduleResult
            { Success = true, UpdatedNodes = _currentNodes, NodeSchedules = new List<NodeSchedule>() });

        // Act
        await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(_procedureId, capturedRequest.ProcedureId);
        Assert.Equal(_currentNodes, capturedRequest.Nodes);
        Assert.Equal(_currentEdges, capturedRequest.Edges);
        Assert.Equal(elapsedSeconds, capturedRequest.CurrentTime);
        Assert.False(capturedRequest.StrictMode);
        Assert.True(capturedRequest.IncludeDetailedTiming);
        Assert.False(capturedRequest.PreserveOriginalTaskDurations);
        Assert.Equal(_executionStartTime.UtcDateTime, capturedRequest.ProcedureStartTimeUtc);
        Assert.Equal(progressData, capturedRequest.ExecutionProgressData);
    }

    [Fact]
    public async Task RescheduleAsync_WithFailedScheduling_ReturnsFailure()
    {
        // Arrange
        var elapsedSeconds = 10.0;
        var errorMessage = "Scheduling failed";

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(elapsedSeconds);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(new List<SkillExecutionState>());

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = false,
                UpdatedNodes = null,
                NodeSchedules = new List<NodeSchedule>(),
                ErrorMessage = errorMessage
            });

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.UpdatedNodes);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task RescheduleAsync_WithException_ReturnsFailureWithErrorMessage()
    {
        // Arrange
        var exceptionMessage = "Unexpected error";

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.UpdatedNodes);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task RescheduleAsync_WithNullUpdatedNodes_ReturnsFailure()
    {
        // Arrange
        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(new List<SkillExecutionState>());

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = null, // Success but null nodes
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        var result = await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.UpdatedNodes);
    }

    [Fact]
    public async Task RescheduleAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var elapsedSeconds = 10.0;

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(elapsedSeconds);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(new List<SkillExecutionState>());

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = _currentNodes,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Re-scheduling remaining skills")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("RESCHEDULING") && v.ToString()!.Contains("UPDATING_NODES")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RescheduleAsync_LogsWarningOnFailure()
    {
        // Arrange
        var errorMessage = "Scheduling failed";

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(new List<SkillExecutionState>());

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Re-scheduling failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RescheduleAsync_UsesProgressDataFromStateManager()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);
        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-5)
        };
        var states = new List<SkillExecutionState> { state };

        var expectedProgress = new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 5.0,
            EstimatedTotalDuration = 10.0,
            StatusMessage = "Running"
        };
        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            { executionId, expectedProgress }
        };

        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(_executionStartTime, It.IsAny<DateTimeOffset>()))
            .Returns(10.0);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(states);

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(states, It.IsAny<DateTimeOffset>()))
            .Returns(progressData);

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = _currentNodes,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        await _coordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert
        _mockProgressDataBuilder.Verify(
            b => b.BuildProgressData(states, It.IsAny<DateTimeOffset>()),
            Times.Once);
    }

    [Fact]
    public async Task Initialize_StoresContextForRescheduling()
    {
        // Arrange
        var newCoordinator = new ReschedulingCoordinator(
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance,
            _timeProvider,
            _mockStateManager.Object,
            _mockProgressDataBuilder.Object,
            _mockProgressMonitor.Object,
            _mockTimeCalculator.Object,
            _mockTimingOrchestrator.Object);

        var procedureId = Guid.NewGuid();
        var nodes = new List<Node> { CreateSkillExecutionNode("Test") }.AsReadOnly();
        var edges = new List<DependencyEdge>().AsReadOnly();
        var startTime = _timeProvider.GetUtcNow();

        // Act
        newCoordinator.Initialize(procedureId, nodes, edges, startTime);

        // Assert - No direct way to verify, but we can test that reschedule uses these values
        _mockTimeCalculator
            .Setup(c => c.CalculateElapsedSeconds(startTime, It.IsAny<DateTimeOffset>()))
            .Returns(5.0);

        _mockStateManager
            .Setup(s => s.GetAllStates())
            .Returns(new List<SkillExecutionState>());

        _mockProgressDataBuilder
            .Setup(b => b.BuildProgressData(It.IsAny<IEnumerable<SkillExecutionState>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new Dictionary<Guid, SkillExecutionProgress>());

        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(
                It.Is<SchedulingRequest>(r =>
                    r.ProcedureId == procedureId &&
                    r.Nodes == nodes &&
                    r.Edges == edges),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            { Success = true, UpdatedNodes = nodes, NodeSchedules = new List<NodeSchedule>() });

        // Act
        await newCoordinator.RescheduleAsync(RescheduleReason.SkillStarted, CancellationToken.None);

        // Assert - The mock verifies the request had correct context
        _mockTimingOrchestrator.Verify(
            o => o.CalculateAsync(
                It.Is<SchedulingRequest>(r =>
                    r.ProcedureId == procedureId &&
                    r.Nodes == nodes &&
                    r.Edges == edges),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Helper methods
    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid? executionId = null,
        Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                ExecutionId = executionId,
                Skill = CreateSkill(skillName)
            }
        };
    }

    private static Skill CreateSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };
    }
}