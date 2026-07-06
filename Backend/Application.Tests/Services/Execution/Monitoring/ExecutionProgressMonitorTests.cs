using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Monitoring;

/// <summary>
///     Unit tests for ExecutionProgressMonitor.
///     Tests progress calculation and completion detection logic.
/// </summary>
public class ExecutionProgressMonitorTests
{
    private readonly Mock<ISkillExecutionStateManager> _mockStateManager;
    private readonly ExecutionProgressMonitor _progressMonitor;

    public ExecutionProgressMonitorTests()
    {
        _mockStateManager = new Mock<ISkillExecutionStateManager>();
        _progressMonitor = new ExecutionProgressMonitor(_mockStateManager.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStateManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExecutionProgressMonitor(null!));
    }

    #endregion

    #region Helper Methods

    private static SkillExecutionState CreateStateWithStatus(
        ExecutionStatus status, double? lastProgressPercentage = null)
    {
        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill description",
                    Properties = new List<TypedProperty>()
                }
            }
        };

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = status,
            LastProgressPercentage = lastProgressPercentage
        };

        return state;
    }

    #endregion

    #region CalculateProgressPercentage Tests

    [Fact]
    public void CalculateProgressPercentage_WithNoSkills_ReturnsZero()
    {
        // Arrange
        _mockStateManager.Setup(m => m.GetAllStates()).Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert
        Assert.Equal(0.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_WithAllCompleted_ReturnsHundred()
    {
        // Arrange
        var states = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed))
            .Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert
        Assert.Equal(100.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_WithHalfCompleted_ReturnsFifty()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Running),
            CreateStateWithStatus(ExecutionStatus.Scheduled)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed))
            .Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert
        Assert.Equal(50.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_WithSomeFailedAndCompleted_ReturnsCorrectPercentage()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var failedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Failed),
            CreateStateWithStatus(ExecutionStatus.Running)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed))
            .Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(failedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert
        Assert.Equal(75.0, progress); // (2 completed + 1 failed) / 4 total = 75%
    }

    [Fact]
    public void CalculateProgressPercentage_SingleRunningSkillAtFiftyPercent_ReturnsFifty()
    {
        // Arrange: 1 skill Running at 50% progress
        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Running, 50.0)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: Should return 50%, not 0%
        Assert.Equal(50.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_RunningSkillsContributePartialProgress()
    {
        // Arrange: 1 Completed, 2 Running@50%, 1 NotStarted
        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Running, 50.0),
            CreateStateWithStatus(ExecutionStatus.Running, 50.0),
            CreateStateWithStatus(ExecutionStatus.NotStarted)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: (100 + 50 + 50 + 0) / 4 = 50%
        Assert.Equal(50.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_ParallelSkillsAtDifferentProgress_ReflectsWeightedAverage()
    {
        // Arrange: 1 Completed, 1 Running@30%, 1 Running@70%, 1 NotStarted
        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Running, 30.0),
            CreateStateWithStatus(ExecutionStatus.Running, 70.0),
            CreateStateWithStatus(ExecutionStatus.NotStarted)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: (100 + 30 + 70 + 0) / 4 = 50%
        Assert.Equal(50.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_UserScenario_MidParallelExecution_ReturnsFifty()
    {
        // Arrange: User's exact scenario at t≈100:
        // Grasp1=Completed, Hold=Running@50%, Weld=Running@50%, Grasp2=NotStarted
        var grasp1 = CreateStateWithStatus(ExecutionStatus.Completed);
        var hold = CreateStateWithStatus(ExecutionStatus.Running, 50.0);
        var weld = CreateStateWithStatus(ExecutionStatus.Running, 50.0);
        var grasp2 = CreateStateWithStatus(ExecutionStatus.NotStarted);

        var allStates = new List<SkillExecutionState> { grasp1, hold, weld, grasp2 }.AsReadOnly();
        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: (100 + 50 + 50 + 0) / 4 = 50%
        Assert.Equal(50.0, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_UserScenario_LastSkillHalfDone_ReturnsEightySevenPointFive()
    {
        // Arrange: t≈172.5: 3 Completed, Grasp2=Running@50%
        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Running, 50.0)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: (100 + 100 + 100 + 50) / 4 = 87.5%
        Assert.Equal(87.5, progress);
    }

    [Fact]
    public void CalculateProgressPercentage_RunningSkillWithNullProgress_TreatsAsZeroContribution()
    {
        // Arrange: 1 Running (null progress), 1 Completed
        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Running),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);

        // Act
        var progress = _progressMonitor.CalculateProgressPercentage();

        // Assert: (0 + 100) / 2 = 50% — Running with null progress contributes 0%
        Assert.Equal(50.0, progress);
    }

    #endregion

    #region IsExecutionComplete Tests

    [Fact]
    public void IsExecutionComplete_WithNoSkills_ReturnsTrue()
    {
        // Arrange
        _mockStateManager.Setup(m => m.GetAllStates()).Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void IsExecutionComplete_WithAllCompleted_ReturnsTrue()
    {
        // Arrange
        var states = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed)).Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void IsExecutionComplete_WithAllFailed_ReturnsTrue()
    {
        // Arrange
        var states = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Failed),
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(states);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void IsExecutionComplete_WithMixedCompletedAndFailed_ReturnsTrue()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var failedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed)).Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(failedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void IsExecutionComplete_WithSomeRunning_ReturnsFalse()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var failedStates = new List<SkillExecutionState>().AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Running)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed)).Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(failedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.False(isComplete);
    }

    [Fact]
    public void IsExecutionComplete_WithSomeScheduled_ReturnsFalse()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var failedStates = new List<SkillExecutionState>().AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Scheduled)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed)).Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(failedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isComplete = _progressMonitor.IsExecutionComplete();

        // Assert
        Assert.False(isComplete);
    }

    #endregion

    #region IsExecutionSuccessful Tests

    [Fact]
    public void IsExecutionSuccessful_WithNoSkills_ReturnsTrue()
    {
        // Arrange
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isSuccessful = _progressMonitor.IsExecutionSuccessful();

        // Assert
        Assert.True(isSuccessful);
    }

    [Fact]
    public void IsExecutionSuccessful_WithNoFailures_ReturnsTrue()
    {
        // Arrange
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var isSuccessful = _progressMonitor.IsExecutionSuccessful();

        // Assert
        Assert.True(isSuccessful);
    }

    [Fact]
    public void IsExecutionSuccessful_WithFailures_ReturnsFalse()
    {
        // Arrange
        var failedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(failedStates);

        // Act
        var isSuccessful = _progressMonitor.IsExecutionSuccessful();

        // Assert
        Assert.False(isSuccessful);
    }

    #endregion

    #region GetExecutionStatistics Tests

    [Fact]
    public void GetExecutionStatistics_WithNoSkills_ReturnsEmptyStatistics()
    {
        // Arrange
        _mockStateManager.Setup(m => m.GetAllStates()).Returns(new List<SkillExecutionState>().AsReadOnly());
        _mockStateManager.Setup(m => m.GetStatesByStatus(It.IsAny<ExecutionStatus>()))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var stats = _progressMonitor.GetExecutionStatistics();

        // Assert
        Assert.Equal(0, stats["Total"]);
        Assert.Equal(0, stats["Completed"]);
        Assert.Equal(0, stats["Failed"]);
        Assert.Equal(0, stats["Running"]);
    }

    [Fact]
    public void GetExecutionStatistics_WithMixedStates_ReturnsCorrectCounts()
    {
        // Arrange
        var completedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed)
        }.AsReadOnly();

        var failedStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Failed)
        }.AsReadOnly();

        var runningStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Running)
        }.AsReadOnly();

        var allStates = new List<SkillExecutionState>
        {
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Completed),
            CreateStateWithStatus(ExecutionStatus.Failed),
            CreateStateWithStatus(ExecutionStatus.Running)
        }.AsReadOnly();

        _mockStateManager.Setup(m => m.GetAllStates()).Returns(allStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Completed)).Returns(completedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Failed)).Returns(failedStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.Running)).Returns(runningStates);
        _mockStateManager.Setup(m => m.GetStatesByStatus(ExecutionStatus.NotSelected))
            .Returns(new List<SkillExecutionState>().AsReadOnly());

        // Act
        var stats = _progressMonitor.GetExecutionStatistics();

        // Assert
        Assert.Equal(4, stats["Total"]);
        Assert.Equal(2, stats["Completed"]);
        Assert.Equal(1, stats["Failed"]);
        Assert.Equal(1, stats["Running"]);
        Assert.Equal(0, stats["NotSelected"]);
    }

    #endregion
}