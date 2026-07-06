using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Duration;

/// <ILogger
/// <ExecutionAwareDurationProvider>
///     >f="ExecutionAwareDurationProvider" />.
///     </summary>
public class ExecutionAwareDurationProviderTests
{
    private readonly Mock<ILogger<ExecutionAwareDurationProvider>> _mockLogger = new();
    private readonly Mock<ISkillDurationProvider> _mockPlanningProvider = new();
    private readonly DateTime _procedureStartTime = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Skill CreateTestSkill(Guid? id = null, string name = "TestSkill")
    {
        return new Skill
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = "Test Description",
            Properties = new List<TypedProperty>()
        };
    }

    private static Agent CreateTestAgent(Guid? id = null, string name = "TestAgent")
    {
        return new Agent
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            SkillIds = new List<Guid>(),
            RepresentativeColor = "#FF0000"
        };
    }

    [Fact]
    public async Task AnalyzeAsync_WithMatchingProgressData_UsesActualProgressData()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(5),
                CurrentTimeIntoExecution = 3.0,
                EstimatedTotalDuration = 12.0,
                MinAchievableDuration = 10.0,
                CompletedSuccessfully = false,
                Error = null,
                StatusMessage = ""
            }
        };

        // Planning provider is called to get domain objects
        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.PlannedDuration == 8.0 && // Different from progress data
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        Assert.Equal(12.0, result.PlannedDuration); // Uses EstimatedTotalDuration from progress, not planning provider

        // Should be adaptive since min/max are provided (now uses AdaptiveSkillExecution, not Planned)
        Assert.IsType<AdaptiveSkillExecution>(result);
        var adaptiveResult = (IAdaptiveSkillExecution)result;
        Assert.Equal(10.0, adaptiveResult.MinDuration);

        // Planning provider IS called to get domain objects
        _mockPlanningProvider.Verify(
            x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithoutExecutionId_FallsBackToPlanningProvider()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = null // No execution ID yet
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>();

        var testAgent = CreateTestAgent(agentId);
        var expectedPlannedExecution = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.PlannedDuration == 8.5 &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlannedExecution);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        Assert.Equal(8.5, result.PlannedDuration);

        // Should call the planning provider as fallback
        _mockPlanningProvider.Verify(
            x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithExecutionIdButNoProgressData_FallsBackToPlanningProvider()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        // Progress data exists but not for this execution ID
        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [Guid.NewGuid()] = new()
            {
                ExecutionId = Guid.NewGuid(),
                SkillId = Guid.NewGuid(),
                AgentId = Guid.NewGuid(),
                ActualStartTimeUtc = _procedureStartTime,
                CurrentTimeIntoExecution = 0,
                EstimatedTotalDuration = 5.0,
                StatusMessage = ""
            }
        };

        var testAgent = CreateTestAgent(agentId);
        var expectedPlannedExecution = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.PlannedDuration == 7.0 &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlannedExecution);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7.0, result.PlannedDuration);

        // Should call the planning provider as fallback
        _mockPlanningProvider.Verify(
            x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCompletedExecution_UsesActualDuration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(5),
                CurrentTimeIntoExecution = 8.5, // Completed after 8.5 seconds
                EstimatedTotalDuration = 10.0,
                MinAchievableDuration = 8.0,
                CompletedSuccessfully = true, // Execution completed
                StatusMessage = ""
            }
        };

        // Planning provider is called to get domain objects
        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        // When completed, should use CurrentTimeIntoExecution as the actual duration
        Assert.Equal(8.5, result.PlannedDuration);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonAdaptiveSkill_ReturnsPlannedSkillExecution()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(5),
                CurrentTimeIntoExecution = 3.0,
                EstimatedTotalDuration = 8.0,
                MinAchievableDuration = null, // Non-adaptive - no min
                CompletedSuccessfully = false,
                Error = null,
                StatusMessage = ""
            }
        };

        // Planning provider is called to get domain objects
        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        Assert.Equal(8.0, result.PlannedDuration);

        // Should be non-adaptive SkillExecution, NOT adaptive
        Assert.IsType<SkillExecution>(result);
        Assert.IsNotType<AdaptiveSkillExecution>(result);

        // Planning provider IS called to get domain objects
        _mockPlanningProvider.Verify(
            x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenPlanningProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(),
                AgentId = Guid.NewGuid(),
                ExecutionId = null
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>();

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPlannedSkillExecution?)null);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.Null(result);
    }

    #region BUG FIX TESTS - ISkillExecution with ActualStartTime

    [Fact]
    public async Task AnalyzeAsync_WithRunningSkill_ShouldCreateSkillExecutionWithActualStartTime()
    {
        // TEST FOR BUG FIX: Must create SkillExecution (not PlannedSkillExecution)
        // so that ActualStartTime is available to the scheduling algorithm

        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        // Skill started at 1.26s, currently 30s into execution, estimated 43.93s total
        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(1.26),
                CurrentTimeIntoExecution = 30.0,
                EstimatedTotalDuration = 43.93,
                CompletedSuccessfully = false,
                StatusMessage = ""
            }
        };

        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);

        // ✅ CRITICAL: Must implement ISkillExecution (not just IPlannedSkillExecution)
        Assert.IsAssignableFrom<ISkillExecution>(result);

        var skillExecution = result as ISkillExecution;
        Assert.NotNull(skillExecution);

        // ✅ CRITICAL: Must have ActualStartTime set
        Assert.NotNull(skillExecution.ActualStartTime);
        Assert.Equal(1.26, skillExecution.ActualStartTime.Value);

        // ✅ CRITICAL: Must be marked as running
        Assert.True(skillExecution.IsRunning);
        Assert.False(skillExecution.IsFinished);

        // ✅ Should use estimated duration from progress
        Assert.Equal(43.93, result.PlannedDuration);

        // ✅ No actual finish time yet (still running)
        Assert.Null(skillExecution.ActualFinishTime);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCompletedSkill_ShouldCreateSkillExecutionWithActualFinishTime()
    {
        // TEST FOR BUG FIX: Completed skills should have ActualFinishTime set

        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        // Skill started at 1.26s, completed after 43.93s
        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(1.26),
                CurrentTimeIntoExecution = 43.93,
                EstimatedTotalDuration = 43.93,
                CompletedSuccessfully = true,
                StatusMessage = ""
            }
        };

        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        var skillExecution = result as ISkillExecution;
        Assert.NotNull(skillExecution);

        // ✅ Should have actual start and finish times
        Assert.Equal(1.26, skillExecution.ActualStartTime!.Value);
        Assert.Equal(45.19, skillExecution.ActualFinishTime!.Value, 2); // 1.26 + 43.93 = 45.19

        // ✅ Should be marked as finished
        Assert.False(skillExecution.IsRunning);
        Assert.True(skillExecution.IsFinished);

        // ✅ Should use actual duration
        Assert.Equal(43.93, result.PlannedDuration);
    }

    [Fact]
    public async Task AnalyzeAsync_WithAdaptiveRunningSkill_ShouldCreateAdaptiveSkillExecutionWithActualTiming()
    {
        // TEST FOR BUG FIX: Adaptive skills must also have ActualStartTime

        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(2.5),
                CurrentTimeIntoExecution = 15.0,
                EstimatedTotalDuration = 30.0,
                MinAchievableDuration = 20.0,
                CompletedSuccessfully = false,
                StatusMessage = ""
            }
        };

        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Test Task" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);

        // ✅ Must be both adaptive AND have actual timing
        Assert.IsAssignableFrom<IAdaptiveSkillExecution>(result);
        Assert.IsAssignableFrom<ISkillExecution>(result);

        var adaptiveExecution = result as IAdaptiveSkillExecution;
        Assert.Equal(20.0, adaptiveExecution!.MinDuration);

        var skillExecution = result as ISkillExecution;
        Assert.Equal(2.5, skillExecution!.ActualStartTime);
        Assert.True(skillExecution.IsRunning);
    }

    [Fact]
    public async Task AnalyzeAsync_BugReproduction_SchedulerCanDetectRunningSkill()
    {
        // This test reproduces the exact bug scenario that caused Skill 2's start time
        // to be set to 43.93s (Skill 1's duration) instead of 45.19s (Skill 1's finish time)

        // Arrange: Skill 1 running at 1.26s with 43.93s duration
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skill 1",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(skillId, "Skill 1"),
                AgentId = agentId,
                ExecutionId = executionId
            }
        };

        var progressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            [executionId] = new()
            {
                ExecutionId = executionId,
                SkillId = skillId,
                AgentId = agentId,
                ActualStartTimeUtc = _procedureStartTime.AddSeconds(1.26),
                CurrentTimeIntoExecution = 30.0,
                EstimatedTotalDuration = 43.93,
                CompletedSuccessfully = false,
                StatusMessage = ""
            }
        };

        var testAgent = CreateTestAgent(agentId);
        var mockPlanningResult = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.Name == "Skill 1" &&
            e.DomainAgent == testAgent);

        _mockPlanningProvider
            .Setup(x => x.AnalyzeAsync(node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanningResult);

        var provider = new ExecutionAwareDurationProvider(
            _mockPlanningProvider.Object,
            _procedureStartTime,
            progressData,
            _mockLogger.Object);

        // Act
        var result = await provider.AnalyzeAsync(node);

        // Assert: The scheduler needs to be able to detect this skill is running
        Assert.NotNull(result);
        var skillExecution = result as ISkillExecution;
        Assert.NotNull(skillExecution);

        // ✅ CRITICAL: Scheduler checks `IsRunning` to determine if skill has started
        Assert.True(skillExecution.IsRunning);

        // ✅ CRITICAL: Scheduler uses `ActualStartTime` for running skills
        Assert.Equal(1.26, skillExecution.ActualStartTime);

        // ✅ CRITICAL: Scheduler calculates finish time as ActualStart + Duration
        var expectedFinishTime = 1.26 + 43.93; // = 45.19
        Assert.Equal(expectedFinishTime, skillExecution.EstimatedFinishTime!.Value, 2);

        // ✅ This ensures Skill 2 will start at 45.19s (Skill 1's finish), NOT 43.93s (duration)
    }

    #endregion
}