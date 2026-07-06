using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.StateManagement;

/// <summary>
///     Unit tests for ExecutionStateTransitionService.
///     Tests all state transition scenarios to ensure proper state management.
/// </summary>
public class ExecutionStateTransitionServiceTests
{
    private readonly Mock<ILogger<ExecutionStateTransitionService>> _mockLogger;
    private readonly Mock<ISkillExecutionStateManager> _mockStateManager;
    private readonly TimeProvider _timeProvider;
    private readonly ExecutionStateTransitionService _transitionService;

    public ExecutionStateTransitionServiceTests()
    {
        _mockStateManager = new Mock<ISkillExecutionStateManager>();
        _mockLogger = new Mock<ILogger<ExecutionStateTransitionService>>();
        _timeProvider = TimeProvider.System;
        _transitionService = new ExecutionStateTransitionService(
            _mockStateManager.Object,
            _mockLogger.Object,
            _timeProvider);
    }

    #region TransitionToRunning Tests

    [Fact]
    public void TransitionToRunning_WithValidParameters_UpdatesStateCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agent = CreateMockAgent("TestAgent");
        var startTime = _timeProvider.GetUtcNow();

        // Act
        _transitionService.TransitionToRunning(skillId, agent, startTime);

        // Assert
        _mockStateManager.Verify(m => m.UpdateState(skillId, It.Is<Action<SkillExecutionState>>(action =>
            VerifyRunningStateUpdate(action, agent, startTime)
        )), Times.Once);
    }

    [Fact]
    public void TransitionToRunning_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var startTime = _timeProvider.GetUtcNow();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitionService.TransitionToRunning(skillId, null!, startTime));
    }

    [Fact]
    public void TransitionToRunning_SetsExecutionStatusToRunning()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agent = CreateMockAgent("TestAgent");
        var startTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToRunning(skillId, agent, startTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(ExecutionStatus.Running, capturedState.ExecutionStatus);
    }

    [Fact]
    public void TransitionToRunning_SetsStartedAtTime()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agent = CreateMockAgent("TestAgent");
        var startTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToRunning(skillId, agent, startTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(startTime, capturedState.StartedAt);
    }

    [Fact]
    public void TransitionToRunning_AssignsAgent()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agent = CreateMockAgent("TestAgent");
        var startTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToRunning(skillId, agent, startTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(agent, capturedState.AssignedAgent);
    }

    #endregion

    #region TransitionToCompleted Tests

    [Fact]
    public void TransitionToCompleted_WithValidParameters_UpdatesStateCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var completionTime = _timeProvider.GetUtcNow();

        // Act
        _transitionService.TransitionToCompleted(skillId, completionTime);

        // Assert
        _mockStateManager.Verify(m => m.UpdateState(skillId, It.Is<Action<SkillExecutionState>>(action =>
            VerifyCompletedStateUpdate(action, completionTime)
        )), Times.Once);
    }

    [Fact]
    public void TransitionToCompleted_SetsExecutionStatusToCompleted()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var completionTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToCompleted(skillId, completionTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(ExecutionStatus.Completed, capturedState.ExecutionStatus);
    }

    [Fact]
    public void TransitionToCompleted_SetsCompletedAtTime()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var completionTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToCompleted(skillId, completionTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(completionTime, capturedState.CompletedAt);
    }

    [Fact]
    public void TransitionToCompleted_DisposesSubscription()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var completionTime = _timeProvider.GetUtcNow();
        var mockSubscription = new Mock<IDisposable>();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                capturedState.Subscription = mockSubscription.Object;
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToCompleted(skillId, completionTime);

        // Assert
        mockSubscription.Verify(s => s.Dispose(), Times.Once);
    }

    #endregion

    #region TransitionToFailed Tests

    [Fact]
    public void TransitionToFailed_WithValidParameters_UpdatesStateCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var errorMessage = "Test error";
        var failureTime = _timeProvider.GetUtcNow();

        // Act
        _transitionService.TransitionToFailed(skillId, errorMessage, failureTime);

        // Assert
        _mockStateManager.Verify(m => m.UpdateState(skillId, It.Is<Action<SkillExecutionState>>(action =>
            VerifyFailedStateUpdate(action, errorMessage, failureTime)
        )), Times.Once);
    }

    [Fact]
    public void TransitionToFailed_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var failureTime = _timeProvider.GetUtcNow();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitionService.TransitionToFailed(skillId, null!, failureTime));
    }

    [Fact]
    public void TransitionToFailed_WithEmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var failureTime = _timeProvider.GetUtcNow();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _transitionService.TransitionToFailed(skillId, "", failureTime));
    }

    [Fact]
    public void TransitionToFailed_SetsExecutionStatusToFailed()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var errorMessage = "Test error";
        var failureTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToFailed(skillId, errorMessage, failureTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(ExecutionStatus.Failed, capturedState.ExecutionStatus);
    }

    [Fact]
    public void TransitionToFailed_SetsErrorMessage()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var errorMessage = "Test error";
        var failureTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToFailed(skillId, errorMessage, failureTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(errorMessage, capturedState.ErrorMessage);
    }

    [Fact]
    public void TransitionToFailed_SetsCompletedAtTime()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var errorMessage = "Test error";
        var failureTime = _timeProvider.GetUtcNow();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToFailed(skillId, errorMessage, failureTime);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(failureTime, capturedState.CompletedAt);
    }

    [Fact]
    public void TransitionToFailed_DisposesSubscription()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var errorMessage = "Test error";
        var failureTime = _timeProvider.GetUtcNow();
        var mockSubscription = new Mock<IDisposable>();
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                capturedState.Subscription = mockSubscription.Object;
                action(capturedState);
            });

        // Act
        _transitionService.TransitionToFailed(skillId, errorMessage, failureTime);

        // Assert
        mockSubscription.Verify(s => s.Dispose(), Times.Once);
    }

    #endregion

    #region UpdateProgress Tests

    [Fact]
    public void UpdateProgress_WithValidParameters_UpdatesStateCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progressPercentage = 75.5;
        var progress = CreateSkillExecutionProgress(5.0, 10.0);

        // Act
        _transitionService.UpdateProgress(skillId, progressPercentage, progress);

        // Assert
        _mockStateManager.Verify(m => m.UpdateState(skillId, It.Is<Action<SkillExecutionState>>(action =>
            VerifyProgressUpdate(action, progressPercentage, progress)
        )), Times.Once);
    }

    [Fact]
    public void UpdateProgress_WithNullProgress_ThrowsArgumentNullException()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progressPercentage = 50.0;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitionService.UpdateProgress(skillId, progressPercentage, null!));
    }

    [Fact]
    public void UpdateProgress_SetsProgressPercentage()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progressPercentage = 42.0;
        var progress = CreateSkillExecutionProgress(2.1, 5.0);
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.UpdateProgress(skillId, progressPercentage, progress);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(progressPercentage, capturedState.LastProgressPercentage);
    }

    [Fact]
    public void UpdateProgress_SetsLastProgress()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progressPercentage = 60.0;
        var progress = CreateSkillExecutionProgress(3.0, 5.0);
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act
        _transitionService.UpdateProgress(skillId, progressPercentage, progress);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(progress, capturedState.LastProgress);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(101.0, 100.0)]
    [InlineData(150.0, 100.0)]
    public void UpdateProgress_WithOutOfRangePercentage_ClampsToValidRange(
        double inputPercentage, double expectedClamped)
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progress = CreateSkillExecutionProgress(5.0, 10.0);
        SkillExecutionState? capturedState = null;

        _mockStateManager.Setup(m => m.UpdateState(skillId, It.IsAny<Action<SkillExecutionState>>()))
            .Callback<Guid, Action<SkillExecutionState>>((_, action) =>
            {
                capturedState = CreateSkillExecutionState();
                action(capturedState);
            });

        // Act — no longer throws, clamps instead
        _transitionService.UpdateProgress(skillId, inputPercentage, progress);

        // Assert
        Assert.NotNull(capturedState);
        Assert.Equal(expectedClamped, capturedState.LastProgressPercentage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    public void UpdateProgress_WithValidPercentage_DoesNotThrow(double validPercentage)
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var progress = CreateSkillExecutionProgress(validPercentage / 20.0, 5.0);

        // Act & Assert - should not throw
        _transitionService.UpdateProgress(skillId, validPercentage, progress);
    }

    #endregion

    #region Helper Methods

    private static IRuntimeAgent CreateMockAgent(string name)
    {
        var mock = new Mock<IRuntimeAgent>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.Id).Returns(Guid.NewGuid());
        return mock.Object;
    }

    private static SkillExecutionState CreateSkillExecutionState()
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

        return new SkillExecutionState(skillNode);
    }

    private static SkillExecutionProgress CreateSkillExecutionProgress(double currentTime, double totalDuration)
    {
        return new SkillExecutionProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            ActualStartTimeUtc = DateTime.UtcNow,
            StatusMessage = "Test progress",
            CurrentTimeIntoExecution = currentTime,
            EstimatedTotalDuration = totalDuration
        };
    }

    private static bool VerifyRunningStateUpdate(Action<SkillExecutionState> action, IRuntimeAgent expectedAgent,
        DateTimeOffset expectedStartTime)
    {
        var state = CreateSkillExecutionState();
        action(state);
        return state.ExecutionStatus == ExecutionStatus.Running &&
               state.AssignedAgent == expectedAgent &&
               state.StartedAt == expectedStartTime;
    }

    private static bool VerifyCompletedStateUpdate(Action<SkillExecutionState> action,
        DateTimeOffset expectedCompletionTime)
    {
        var state = CreateSkillExecutionState();
        action(state);
        return state.ExecutionStatus == ExecutionStatus.Completed &&
               state.CompletedAt == expectedCompletionTime;
    }

    private static bool VerifyFailedStateUpdate(Action<SkillExecutionState> action, string expectedErrorMessage,
        DateTimeOffset expectedFailureTime)
    {
        var state = CreateSkillExecutionState();
        action(state);
        return state.ExecutionStatus == ExecutionStatus.Failed &&
               state.ErrorMessage == expectedErrorMessage &&
               state.CompletedAt == expectedFailureTime;
    }

    private static bool VerifyProgressUpdate(Action<SkillExecutionState> action, double expectedPercentage,
        SkillExecutionProgress expectedProgress)
    {
        var state = CreateSkillExecutionState();
        action(state);
        return Math.Abs((state.LastProgressPercentage ?? 0.0) - expectedPercentage) < 0.001 &&
               state.LastProgress == expectedProgress;
    }

    #endregion
}