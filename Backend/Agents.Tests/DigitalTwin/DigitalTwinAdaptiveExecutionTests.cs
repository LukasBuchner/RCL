using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.DigitalTwin;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.DigitalTwin;

/// <summary>
///     Tests for <see cref="DigitalTwinRuntimeAgent.ExecuteSkillAdaptivelyAsync" />,
///     verifying the LSP fix: errors are delivered through Rx OnError instead of
///     synchronous exceptions, and adaptive Hold Position execution works correctly.
/// </summary>
public class DigitalTwinAdaptiveExecutionTests : IDisposable
{
    private readonly DigitalTwinRuntimeAgent _agent;
    private readonly DigitalTwinAgentConfiguration _config;
    private readonly Skill _holdPositionSkill;
    private readonly Mock<ILogger<DigitalTwinRuntimeAgent>> _mockLogger;
    private readonly MockWebSocket _mockWs;
    private readonly Skill _moveToPositionSkill;

    public DigitalTwinAdaptiveExecutionTests()
    {
        _mockLogger = new Mock<ILogger<DigitalTwinRuntimeAgent>>();
        _config = new DigitalTwinAgentConfiguration
        {
            NominalDurationSeconds = 5.0,
            MaxConcurrentExecutions = 2,
            EstimateTimeoutSeconds = 2.0,
            HoldMinDurationSeconds = 1.0
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

        _holdPositionSkill = new Skill
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

        _mockWs = new MockWebSocket(WebSocketState.Open);

        _agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(),
            "TestDigitalTwin",
            _mockWs,
            [_moveToPositionSkill, _holdPositionSkill],
            _config,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _agent.Dispose();
    }

    // ─── LSP: Observable.Throw instead of synchronous throw ─────────

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_ReturnsObservableNotThrows()
    {
        // The method should return an IObservable without throwing synchronously.
        // The error should only appear when subscribing (Rx OnError channel).
        var observable = _agent.ExecuteSkillAdaptivelyAsync(
            Guid.NewGuid(),
            _moveToPositionSkill,
            5.0,
            Observable.Empty<double>(),
            Observable.Empty<Unit>(),
            CancellationToken.None);

        // Key assertion: we got an observable back, no synchronous exception
        Assert.NotNull(observable);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_ErrorDeliveredViaOnError()
    {
        Exception? receivedError = null;

        var observable = _agent.ExecuteSkillAdaptivelyAsync(
            Guid.NewGuid(),
            _moveToPositionSkill,
            5.0,
            Observable.Empty<double>(),
            Observable.Empty<Unit>(),
            CancellationToken.None);

        observable.Subscribe(
            _ => { },
            error => receivedError = error,
            () => { });

        // Give Rx scheduler a moment
        Thread.Sleep(50);

        Assert.NotNull(receivedError);
        Assert.IsType<NotSupportedException>(receivedError);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_ErrorMessageContainsSkillName()
    {
        Exception? receivedError = null;

        _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _moveToPositionSkill,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(_ => { }, error => receivedError = error);

        Thread.Sleep(50);

        Assert.NotNull(receivedError);
        Assert.Contains("Move To Position", receivedError!.Message);
        Assert.Contains("adaptive execution", receivedError.Message);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_ErrorMessageContainsAgentName()
    {
        Exception? receivedError = null;

        _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _moveToPositionSkill,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(_ => { }, error => receivedError = error);

        Thread.Sleep(50);

        Assert.NotNull(receivedError);
        Assert.Contains("TestDigitalTwin", receivedError!.Message);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_NeverEmitsProgressValues()
    {
        var progressValues = new ConcurrentQueue<SkillExecutionProgress>();

        _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _moveToPositionSkill,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(
                p => progressValues.Enqueue(p),
                _ => { });

        Thread.Sleep(50);

        Assert.Empty(progressValues);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_DoesNotSendWebSocketCommand()
    {
        _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _moveToPositionSkill,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(_ => { }, _ => { });

        Thread.Sleep(50);

        // No command should be sent to Unity for unsupported adaptive skills
        Assert.Empty(_mockWs.SentMessages);
    }

    [Fact]
    public void AdaptiveExecution_NonHoldSkill_WaitThrowsNotSupported()
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

    [Fact]
    public void AdaptiveExecution_SkillWithNoDurationProperty_ReturnsError()
    {
        // A skill that has properties but none named "Duration" with NumberType
        var skillNoDuration = new Skill
        {
            Id = Guid.Parse("11111111-1111-4111-1111-111111111111"),
            Name = "Custom Skill",
            Description = "Has properties but no Duration",
            Properties =
            [
                new TypedProperty
                {
                    Name = "SomeOtherProp",
                    Value = TypedValue.Text("value"),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        var mockWs = new MockWebSocket(WebSocketState.Open);
        using var agent = new DigitalTwinRuntimeAgent(
            Guid.NewGuid(), "TestAgent", mockWs,
            [skillNoDuration], _config, _mockLogger.Object);

        Exception? receivedError = null;

        agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                skillNoDuration,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(_ => { }, error => receivedError = error);

        Thread.Sleep(50);

        Assert.NotNull(receivedError);
        Assert.IsType<NotSupportedException>(receivedError);
    }

    [Fact]
    public void AdaptiveExecution_UnknownSkillId_FallsBackToProvidedSkill()
    {
        // When the skill ID doesn't match any available skill,
        // the code falls back to the provided skill object.
        var unknownSkill = new Skill
        {
            Id = Guid.NewGuid(), // Not in the agent's available skills
            Name = "Unknown Skill",
            Description = "Not registered",
            Properties = []
        };

        Exception? receivedError = null;

        _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                unknownSkill,
                5.0,
                Observable.Empty<double>(),
                Observable.Empty<Unit>(),
                CancellationToken.None)
            .Subscribe(_ => { }, error => receivedError = error);

        Thread.Sleep(50);

        Assert.NotNull(receivedError);
        Assert.IsType<NotSupportedException>(receivedError);
        Assert.Contains("Unknown Skill", receivedError!.Message);
    }

    // ─── Adaptive Hold Position (happy path) ─────────────────────────

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_EmitsProgress()
    {
        var progressValues = new ConcurrentQueue<SkillExecutionProgress>();
        var finishSubject = new Subject<Unit>();

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                5.0,
                Observable.Empty<double>(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(p => progressValues.Enqueue(p));

        // Let the timer emit a few progress values
        await Task.Delay(350);

        // Signal finish
        // Te
        finishSubject.OnNext(Unit.Default);
        await Task.Delay(100);

        Assert.False(progressValues.IsEmpty);
        Assert.All(progressValues, p => Assert.Equal(_agent.Id, p.AgentId));
        // At least one progress tick should be a "Holding position" update (before the completion message)
        Assert.Contains(progressValues, p => p.StatusMessage.Contains("Holding position"));

        subscription.Dispose();
    }

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_ReportsMinAchievableDuration()
    {
        var progressValues = new ConcurrentQueue<SkillExecutionProgress>();
        var finishSubject = new Subject<Unit>();

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                5.0,
                Observable.Empty<double>(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(p => progressValues.Enqueue(p));

        await Task.Delay(250);
        finishSubject.OnNext(Unit.Default);
        await Task.Delay(100);

        Assert.False(progressValues.IsEmpty);
        var progress = progressValues.First();
        Assert.NotNull(progress.MinAchievableDuration);
        Assert.True(progress.MinAchievableDuration >= _config.HoldMinDurationSeconds);

        subscription.Dispose();
    }

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_ClampsToMinDuration()
    {
        // Request a duration below the minimum — should be clamped
        var progressValues = new ConcurrentQueue<SkillExecutionProgress>();
        var finishSubject = new Subject<Unit>();

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                0.1, // Well below HoldMinDurationSeconds (1.0)
                Observable.Empty<double>(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(p => progressValues.Enqueue(p));

        await Task.Delay(250);
        finishSubject.OnNext(Unit.Default);
        await Task.Delay(100);

        Assert.False(progressValues.IsEmpty);
        // EstimatedTotalDuration should be clamped to min
        Assert.True(progressValues.First().EstimatedTotalDuration >= _config.HoldMinDurationSeconds);

        subscription.Dispose();
    }

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_RespondsToPlannedFinishUpdates()
    {
        var progressValues = new ConcurrentQueue<SkillExecutionProgress>();
        var finishSubject = new Subject<Unit>();
        var plannedFinishSubject = new Subject<double>();

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                5.0,
                plannedFinishSubject.AsObservable(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(p => progressValues.Enqueue(p));

        await Task.Delay(200);

        // Update planned finish to a new value
        plannedFinishSubject.OnNext(15.0);
        await Task.Delay(200);

        // Progress should reflect the updated target
        var latestProgress = progressValues.Last();
        Assert.Equal(15.0, latestProgress.EstimatedTotalDuration, 1);

        finishSubject.OnNext(Unit.Default);
        await Task.Delay(100);

        subscription.Dispose();
    }

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_SendsWebSocketCommand()
    {
        var finishSubject = new Subject<Unit>();

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                5.0,
                Observable.Empty<double>(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(_ => { });

        await Task.Delay(100);

        // Should send execute command via WebSocket
        Assert.NotEmpty(_mockWs.SentMessages);

        finishSubject.OnNext(Unit.Default);
        await Task.Delay(50);

        subscription.Dispose();
    }

    [Fact]
    public async Task AdaptiveExecution_HoldPosition_StopsOnFinishSignal()
    {
        var finishSubject = new Subject<Unit>();
        var completed = false;

        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                Guid.NewGuid(),
                _holdPositionSkill,
                5.0,
                Observable.Empty<double>(),
                finishSubject.AsObservable(),
                CancellationToken.None)
            .Subscribe(
                _ => { },
                _ => { },
                () => completed = true);

        await Task.Delay(200);
        Assert.False(completed);

        // Signal finish
        finishSubject.OnNext(Unit.Default);
        await Task.Delay(200);

        Assert.True(completed);

        subscription.Dispose();
    }

    // ─── CanExecuteAdaptivelyAsync contract ──────────────────────────

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_MoveToPosition_ReturnsFalse()
    {
        var result = await _agent.CanExecuteAdaptivelyAsync(_moveToPositionSkill);

        Assert.False(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_UnknownSkill_ReturnsFalse()
    {
        var unknownSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown",
            Description = "Not registered",
            Properties = []
        };

        var result = await _agent.CanExecuteAdaptivelyAsync(unknownSkill);

        Assert.False(result);
    }

    // ─── Multiple concurrent adaptive executions ─────────────────────

    [Fact]
    public async Task AdaptiveExecution_TwoHoldPositions_TrackSeparately()
    {
        var exec1Progress = new ConcurrentQueue<SkillExecutionProgress>();
        var exec2Progress = new ConcurrentQueue<SkillExecutionProgress>();
        var finish1 = new Subject<Unit>();
        var finish2 = new Subject<Unit>();
        var exec1Id = Guid.NewGuid();
        var exec2Id = Guid.NewGuid();

        var sub1 = _agent.ExecuteSkillAdaptivelyAsync(
                exec1Id, _holdPositionSkill, 5.0,
                Observable.Empty<double>(), finish1, CancellationToken.None)
            .Subscribe(p => exec1Progress.Enqueue(p));

        var sub2 = _agent.ExecuteSkillAdaptivelyAsync(
                exec2Id, _holdPositionSkill, 10.0,
                Observable.Empty<double>(), finish2, CancellationToken.None)
            .Subscribe(p => exec2Progress.Enqueue(p));

        await Task.Delay(300);

        // Stop the timer-driven producers before asserting so the queues are no longer mutated
        // while the assertions enumerate them. finishSignal completes the inner observable,
        // and the subsequent delay lets the final progress emission flush.
        finish1.OnNext(Unit.Default);
        finish2.OnNext(Unit.Default);
        await Task.Delay(100);

        sub1.Dispose();
        sub2.Dispose();

        // Snapshot the queues once producers are quiescent
        var snap1 = exec1Progress.ToArray();
        var snap2 = exec2Progress.ToArray();

        Assert.NotEmpty(snap1);
        Assert.NotEmpty(snap2);

        // Each should have its own execution ID
        Assert.All(snap1, p => Assert.Equal(exec1Id, p.ExecutionId));
        Assert.All(snap2, p => Assert.Equal(exec2Id, p.ExecutionId));

        // Different target durations
        Assert.Equal(5.0, snap1[0].EstimatedTotalDuration, 1);
        Assert.Equal(10.0, snap2[0].EstimatedTotalDuration, 1);
    }

    /// <summary>
    ///     Minimal mock WebSocket for testing.
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