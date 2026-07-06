using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.DigitalTwin;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.DigitalTwin;

/// <summary>
///     Tests for <see cref="DigitalTwinRuntimeAgent" />, verifying that it correctly bridges
///     WebSocket messages to Rx.NET observables and implements <see cref="IRuntimeAgent" />.
/// </summary>
public class DigitalTwinRuntimeAgentTests : IDisposable
{
    private readonly DigitalTwinRuntimeAgent _agent;
    private readonly DigitalTwinAgentConfiguration _config;
    private readonly Mock<ILogger<DigitalTwinRuntimeAgent>> _mockLogger;
    private readonly Skill _moveToPositionSkill;

    public DigitalTwinRuntimeAgentTests()
    {
        _mockLogger = new Mock<ILogger<DigitalTwinRuntimeAgent>>();
        _config = new DigitalTwinAgentConfiguration
        {
            NominalDurationSeconds = 5.0,
            MaxConcurrentExecutions = 1,
            EstimateTimeoutSeconds = 2.0
        };

        _moveToPositionSkill = new Skill
        {
            Id = Guid.Parse("456789ab-cdef-4ef0-0123-456789abcdef"),
            Name = "Move To Position",
            Description = "Move to a specific position",
            Properties =
            [
                new TypedProperty
                {
                    Name = "TargetPosition",
                    Value = TypedValue.Position(new Position
                    {
                        X = 0.5, Y = 0.3, Z = 0.8,
                        Alpha = 0, Beta = 0, Gamma = 0
                    }),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        // Create a mock WebSocket that reports Open state
        var mockWs = new MockWebSocket(WebSocketState.Open);

        _agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(),
            "TestDigitalTwin",
            mockWs,
            [_moveToPositionSkill],
            _config,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _agent.Dispose();
    }

    // ─── GetAvailableSkillsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAvailableSkillsAsync_ReturnsRegisteredSkills()
    {
        var skills = await _agent.GetAvailableSkillsAsync();

        Assert.Single(skills);
        Assert.Equal("Move To Position", skills[0].Name);
    }

    // ─── GetExecutionEstimateAsync ───────────────────────────────────

    [Fact]
    public async Task GetExecutionEstimateAsync_UnknownSkill_ReturnsNull()
    {
        var unknownSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown",
            Description = "Not available",
            Properties = []
        };

        var estimate = await _agent.GetExecutionEstimateAsync(unknownSkill);

        Assert.Null(estimate);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_FallsBackToNominal()
    {
        // Create agent with a closed WebSocket to force fallback
        var closedWs = new MockWebSocket(WebSocketState.Closed);
        using var agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(), "ClosedAgent", closedWs,
            [_moveToPositionSkill], _config, _mockLogger.Object);

        var estimate = await agent.GetExecutionEstimateAsync(_moveToPositionSkill);

        Assert.NotNull(estimate);
        Assert.Equal(_config.NominalDurationSeconds, estimate!.EstimatedNominalDuration);
        Assert.False(estimate.CanExecuteAdaptively);
    }

    // ─── CanExecuteAdaptivelyAsync ───────────────────────────────────

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_AlwaysReturnsFalse()
    {
        var result = await _agent.CanExecuteAdaptivelyAsync(_moveToPositionSkill);

        Assert.False(result);
    }

    // ─── ExecuteSkillAdaptivelyAsync ─────────────────────────────────

    [Fact]
    public void ExecuteSkillAdaptivelyAsync_EmitsNotSupportedError()
    {
        var observable = _agent.ExecuteSkillAdaptivelyAsync(
            Guid.NewGuid(),
            _moveToPositionSkill,
            5.0,
            Observable.Empty<double>(),
            Observable.Empty<Unit>(),
            CancellationToken.None);

        Assert.Throws<NotSupportedException>(() => observable.Wait());
    }

    // ─── HandleProgressMessage ───────────────────────────────────────

    [Fact]
    public async Task HandleProgressMessage_EmitsProgressOnActiveExecution()
    {
        // Arrange — start an execution to create an active execution entry
        var executionId = Guid.NewGuid();
        var progressValues = new List<SkillExecutionProgress>();

        var observable = _agent.ExecuteSkillAsync(executionId, _moveToPositionSkill, CancellationToken.None);
        var subscription = observable.Subscribe(p => progressValues.Add(p));

        // Allow async command send to complete
        await Task.Delay(50);

        // Act — simulate Twin sending progress
        _agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = executionId,
            ProgressPercent = 0.5,
            StatusMessage = "Halfway there"
        });

        await Task.Delay(50);

        // Assert
        Assert.True(progressValues.Count >= 1);
        var lastProgress = progressValues.Last();
        Assert.Equal(executionId, lastProgress.ExecutionId);
        Assert.Equal("Halfway there", lastProgress.StatusMessage);

        subscription.Dispose();
    }

    [Fact]
    public void HandleProgressMessage_UnknownExecution_DoesNotThrow()
    {
        // Should log a warning but not throw
        _agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = Guid.NewGuid(),
            ProgressPercent = 0.5,
            StatusMessage = "Unknown"
        });
    }

    // ─── HandleCompletedMessage ──────────────────────────────────────

    [Fact]
    public async Task HandleCompletedMessage_Success_CompletesObservable()
    {
        var executionId = Guid.NewGuid();
        var completed = false;
        SkillExecutionProgress? lastProgress = null;

        var observable = _agent.ExecuteSkillAsync(executionId, _moveToPositionSkill, CancellationToken.None);
        var subscription = observable.Subscribe(
            p => lastProgress = p,
            () => completed = true);

        await Task.Delay(50);

        // Act
        _agent.HandleCompletedMessage(new SkillCompletedMessage
        {
            ExecutionId = executionId,
            Success = true
        });

        await Task.Delay(50);

        // Assert
        Assert.True(completed);
        Assert.NotNull(lastProgress);
        Assert.True(lastProgress!.CompletedSuccessfully);

        subscription.Dispose();
    }

    [Fact]
    public async Task HandleCompletedMessage_Failure_EmitsErrorAndCompletes()
    {
        var executionId = Guid.NewGuid();
        var completed = false;
        SkillExecutionProgress? lastProgress = null;

        var observable = _agent.ExecuteSkillAsync(executionId, _moveToPositionSkill, CancellationToken.None);
        var subscription = observable.Subscribe(
            p => lastProgress = p,
            () => completed = true);

        await Task.Delay(50);

        _agent.HandleCompletedMessage(new SkillCompletedMessage
        {
            ExecutionId = executionId,
            Success = false,
            ErrorMessage = "Timeout"
        });

        await Task.Delay(50);

        Assert.True(completed);
        Assert.NotNull(lastProgress);
        Assert.NotNull(lastProgress!.Error);
        Assert.Contains("Timeout", lastProgress.Error!.Message);

        subscription.Dispose();
    }

    // ─── HandleEstimateDurationResponse ──────────────────────────────

    [Fact]
    public void HandleEstimateDurationResponse_UnknownQuery_DoesNotThrow()
    {
        _agent.HandleEstimateDurationResponse(new EstimateDurationResponse
        {
            QueryId = Guid.NewGuid(),
            EstimatedDurationSeconds = 3.0
        });
    }

    // ─── HandleHealthStatus ──────────────────────────────────────────

    [Fact]
    public async Task HandleHealthStatus_CachesForGetHealthStatusAsync()
    {
        _agent.HandleHealthStatus(new HealthStatusMessage
        {
            CpuPercent = 55.0,
            MemoryMb = 2048,
            UptimeSeconds = 120,
            Fps = 90
        });

        var health = await _agent.GetHealthStatusAsync();

        Assert.Equal(55.0, health.CpuUsagePercent);
        Assert.Equal(2048, health.MemoryUsageMb);
    }

    // ─── GetHealthStatusAsync ────────────────────────────────────────

    [Fact]
    public async Task GetHealthStatusAsync_ReportsConnected_WhenWebSocketOpen()
    {
        var health = await _agent.GetHealthStatusAsync();

        Assert.True(health.IsHealthy);
        Assert.Contains("Idle", health.StatusMessage!);
        Assert.Null(health.ErrorMessage);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReportsDisconnected_WhenWebSocketClosed()
    {
        var closedWs = new MockWebSocket(WebSocketState.Closed);
        using var agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(), "ClosedAgent", closedWs,
            [_moveToPositionSkill], _config, _mockLogger.Object);

        var health = await agent.GetHealthStatusAsync();

        Assert.False(health.IsHealthy);
        Assert.Contains("Disconnected", health.StatusMessage!);
        Assert.NotNull(health.ErrorMessage);
    }

    // ─── HandleDisconnection ─────────────────────────────────────────

    [Fact]
    public async Task HandleDisconnection_CancelsActiveExecutions()
    {
        var executionId = Guid.NewGuid();
        SkillExecutionProgress? lastProgress = null;
        var completed = false;

        var observable = _agent.ExecuteSkillAsync(executionId, _moveToPositionSkill, CancellationToken.None);
        var subscription = observable.Subscribe(
            p => lastProgress = p,
            () => completed = true);

        await Task.Delay(50);

        // Act
        _agent.HandleDisconnection();
        await Task.Delay(50);

        // Assert
        Assert.True(completed);
        Assert.NotNull(lastProgress);
        Assert.NotNull(lastProgress!.Error);
        Assert.Contains("connection lost", lastProgress.Error!.Message);

        subscription.Dispose();
    }

    // ─── Concurrent Executions ───────────────────────────────────────

    [Fact]
    public async Task ExecuteSkillAsync_ConcurrentExecutions_TracksSeparately()
    {
        var exec1 = Guid.NewGuid();
        var exec2 = Guid.NewGuid();
        var progress1 = new List<SkillExecutionProgress>();
        var progress2 = new List<SkillExecutionProgress>();

        var sub1 = _agent.ExecuteSkillAsync(exec1, _moveToPositionSkill, CancellationToken.None)
            .Subscribe(p => progress1.Add(p));
        var sub2 = _agent.ExecuteSkillAsync(exec2, _moveToPositionSkill, CancellationToken.None)
            .Subscribe(p => progress2.Add(p));

        await Task.Delay(50);

        // Send progress only to exec1
        _agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = exec1,
            ProgressPercent = 0.5,
            StatusMessage = "Exec1 progress"
        });

        await Task.Delay(50);

        Assert.True(progress1.Count >= 1);
        Assert.Empty(progress2); // exec2 should not receive exec1's progress

        sub1.Dispose();
        sub2.Dispose();
    }

    // ─── Hold Position execution ─────────────────────────────────────

    [Fact]
    public async Task ExecuteSkillAsync_HoldPosition_SendsDurationInParameters()
    {
        // Arrange — Hold Position skill with Duration property
        var holdSkill = new Skill
        {
            Id = Guid.Parse("738e2beb-74af-436d-8836-7a843bf19f61"),
            Name = "Hold Position",
            Description = "Hold current pose",
            Properties =
            [
                new TypedProperty
                {
                    Name = "Duration",
                    Value = TypedValue.Number(10.0),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        var mockWs = new MockWebSocket(WebSocketState.Open);
        using var agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(), "HoldAgent", mockWs,
            [holdSkill], _config, _mockLogger.Object);

        var executionId = Guid.NewGuid();

        // Act
        var observable = agent.ExecuteSkillAsync(executionId, holdSkill, CancellationToken.None);
        var subscription = observable.Subscribe(_ => { });
        await Task.Delay(50);

        // Assert — verify the sent command contains Duration
        Assert.NotEmpty(mockWs.SentMessages);
        var sentJson = Encoding.UTF8.GetString(mockWs.SentMessages[0]);
        Assert.Contains("\"duration\"", sentJson);
        Assert.Contains("10", sentJson);

        subscription.Dispose();
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_HoldPosition_ReturnsDuration()
    {
        // Arrange — Hold Position skill with Duration property
        var holdSkill = new Skill
        {
            Id = Guid.Parse("738e2beb-74af-436d-8836-7a843bf19f61"),
            Name = "Hold Position",
            Description = "Hold current pose",
            Properties =
            [
                new TypedProperty
                {
                    Name = "Duration",
                    Value = TypedValue.Number(7.5),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        var mockWs = new MockWebSocket(WebSocketState.Open);
        using var agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(), "HoldAgent", mockWs,
            [holdSkill], _config, _mockLogger.Object);

        // Act
        var estimate = await agent.GetExecutionEstimateAsync(holdSkill);

        // Assert — should return Duration as nominal, adaptive enabled, no WebSocket query
        Assert.NotNull(estimate);
        Assert.Equal(7.5, estimate!.EstimatedNominalDuration);
        Assert.True(estimate.CanExecuteAdaptively);
        Assert.Equal(_config.HoldMinDurationSeconds, estimate.MinAdaptiveDuration);
        Assert.Empty(mockWs.SentMessages); // No WebSocket query for hold skills
    }

    /// <summary>
    ///     Minimal mock WebSocket for testing. Only provides <see cref="State" /> and
    ///     stubs <see cref="SendAsync" /> to capture sent messages.
    /// </summary>
    private class MockWebSocket : WebSocket
    {
        private readonly WebSocketState _state;

        public MockWebSocket(WebSocketState state)
        {
            _state = state;
        }

        public List<byte[]> SentMessages { get; } = [];

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            // Block indefinitely (tests don't call this)
            return new TaskCompletionSource<WebSocketReceiveResult>().Task;
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_state != WebSocketState.Open)
                throw new WebSocketException("WebSocket is not open");

            SentMessages.Add(buffer.ToArray());
            return Task.CompletedTask;
        }
    }
}