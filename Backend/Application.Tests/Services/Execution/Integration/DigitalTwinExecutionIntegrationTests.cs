using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.DigitalTwin;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;
using CoordinatorProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Integration;

/// <summary>
///     Integration tests verifying that a real <see cref="DigitalTwinRuntimeAgent" /> correctly
///     bridges WebSocket progress messages through the execution pipeline into
///     <see cref="ISkillExecutionEventBus" /> Start/Progress/Finish events.
///     Uses a real agent (not a mock <see cref="IRuntimeAgent" />) so the full
///     Subject-backed observable bridge is exercised.
/// </summary>
public class DigitalTwinExecutionIntegrationTests(ITestOutputHelper output)
{
    /// <summary>
    ///     Verifies the full pipeline: DigitalTwinRuntimeAgent.ExecuteSkillAsync returns an observable,
    ///     the coordinator subscribes, and externally dispatched progress/completion messages flow
    ///     through to the event bus as Start -> Progress -> Finish events.
    /// </summary>
    [Fact]
    public async Task DigitalTwinProgress_FlowsThroughPipeline_ToEventBus()
    {
        // Arrange — real Digital Twin agent with mock WebSocket
        var agentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var skillNodeId = Guid.NewGuid();

        var skill = new Skill
        {
            Id = skillId,
            Name = "Move To Position",
            Description = "Test skill",
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

        var config = new DigitalTwinAgentConfiguration
        {
            NominalDurationSeconds = 5.0,
            MaxConcurrentExecutions = 1,
            EstimateTimeoutSeconds = 2.0
        };

        var mockWs = new CaptureWebSocket();
        var agentLogger = new TestLogger<DigitalTwinRuntimeAgent>(output);

        var agent = new DigitalTwinRuntimeAgent(
            agentId, "DigitalTwin-Test", mockWs, [skill], config, agentLogger);

        // Real event bus
        var eventBus = new SkillExecutionEventBus(
            new TestLogger<SkillExecutionEventBus>(output));

        // Mock agent provider returns our real DT agent
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(agent);

        // Mock typedProperty binding (no variable context in this test)
        var mockPropertyBinding = new Mock<IPropertyBindingService>();

        // Create mock scene entity resolver (returns skill unchanged)
        var mockSceneEntityResolver = new Mock<ISceneEntityResolver>();
        mockSceneEntityResolver.Setup(r => r.RefreshSceneEntityProperties(It.IsAny<Skill>())).Returns<Skill>(s => s);

        // Real coordinator
        var coordinator = new SkillExecutionCoordinator(
            eventBus,
            mockAgentProvider.Object,
            mockPropertyBinding.Object,
            mockSceneEntityResolver.Object,
            TimeProvider.System,
            new TestLogger<SkillExecutionCoordinator>(output),
            NullLogger<PipelineEvents>.Instance);

        // Collect events
        var events = new ConcurrentBag<ExecutionEvent>();
        var finishReceived = new TaskCompletionSource<bool>();

        using var subscription = eventBus.AllEvents
            .Synchronize()
            .Subscribe(evt =>
            {
                output.WriteLine(
                    $"[EVENT] {evt.EventType} for skill {evt.SkillId} (progress={evt.ProgressPercentage:P0})");
                events.Add(evt);

                if (evt.EventType == ExecutionEventType.Finish)
                    finishReceived.TrySetResult(true);
            });

        // Act — start execution via coordinator (returns observable)
        output.WriteLine("=== Starting execution via coordinator ===");

        var progressValues = new ConcurrentBag<CoordinatorProgress>();
        var observableCompleted = new TaskCompletionSource<bool>();

        var executionObservable = coordinator.ExecuteSkillAsync(
            skillNodeId, skill, agentId, null, CancellationToken.None);

        using var execSub = executionObservable.Subscribe(
            p =>
            {
                progressValues.Add(p);
                output.WriteLine($"[OBSERVABLE] Progress: {p.StatusMessage}");
            },
            ex => output.WriteLine($"[OBSERVABLE] Error: {ex.Message}"),
            () =>
            {
                output.WriteLine("[OBSERVABLE] Completed");
                observableCompleted.TrySetResult(true);
            });

        // Allow the ExecuteSkillCommand to be sent over WebSocket
        await Task.Delay(100);

        // Verify the agent sent an ExecuteSkillCommand over the mock WebSocket
        Assert.NotEmpty(mockWs.SentMessages);
        var sentJson = Encoding.UTF8.GetString(mockWs.SentMessages[0]);
        Assert.Contains("ExecuteSkillCommand", sentJson);
        output.WriteLine($"[WS] Agent sent: {sentJson[..Math.Min(120, sentJson.Length)]}...");

        // Find the executionId from the sent command
        var sentEnvelope = DigitalTwinMessages.Deserialize(sentJson);
        var sentCommand = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(sentEnvelope!);
        var executionId = sentCommand!.ExecutionId;

        output.WriteLine($"[WS] ExecutionId assigned: {executionId}");

        // Simulate Twin sending progress messages
        output.WriteLine("=== Simulating Twin progress messages ===");

        agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = executionId,
            ProgressPercent = 0.25,
            StatusMessage = "Moving to target (25%)"
        });
        await Task.Delay(50);

        agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = executionId,
            ProgressPercent = 0.50,
            StatusMessage = "Moving to target (50%)"
        });
        await Task.Delay(50);

        agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = executionId,
            ProgressPercent = 0.75,
            StatusMessage = "Moving to target (75%)"
        });
        await Task.Delay(50);

        // Simulate Twin sending completion
        output.WriteLine("=== Simulating Twin completion ===");
        agent.HandleCompletedMessage(new SkillCompletedMessage
        {
            ExecutionId = executionId,
            Success = true
        });

        // Wait for observable and finish event with timeout
        var timeout = Task.Delay(TimeSpan.FromSeconds(5));
        var completed = await Task.WhenAny(finishReceived.Task, timeout);
        Assert.NotEqual(timeout, completed);

        await Task.WhenAny(observableCompleted.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        await Task.Delay(100); // allow final events to propagate

        // Assert — observable emitted progress values
        output.WriteLine($"\n=== RESULTS: {progressValues.Count} observable emissions, {events.Count} events ===");

        Assert.True(progressValues.Count >= 4,
            $"Expected at least 4 observable emissions (3 progress + 1 completion), got {progressValues.Count}");

        // Assert — event bus received Start, Progress, and Finish events
        var startEvents = events.Where(e => e.EventType == ExecutionEventType.Start).ToList();
        var progressEvents = events.Where(e => e.EventType == ExecutionEventType.Progress).ToList();
        var finishEvents = events.Where(e => e.EventType == ExecutionEventType.Finish).ToList();

        Assert.Single(startEvents);
        Assert.NotEmpty(progressEvents);
        Assert.Single(finishEvents);

        output.WriteLine($"Start events:    {startEvents.Count}");
        output.WriteLine($"Progress events: {progressEvents.Count}");
        output.WriteLine($"Finish events:   {finishEvents.Count}");

        // Assert — events reference the correct skill node
        Assert.All(events, e => Assert.Equal(skillNodeId, e.SkillId));

        // Assert — Start comes before Finish
        Assert.True(startEvents[0].Timestamp < finishEvents[0].Timestamp);

        output.WriteLine("\n=== TEST PASSED ===");

        agent.Dispose();
    }

    /// <summary>
    ///     Verifies that a failed Digital Twin execution (SkillCompleted with success=false)
    ///     publishes Start and Progress events but NOT a Finish event.
    ///     The coordinator intentionally withholds Finish on failure so downstream
    ///     dependency-triggered skills are not incorrectly started.
    /// </summary>
    [Fact]
    public async Task DigitalTwinFailure_PublishesStartAndProgress_ButNoFinish()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillNodeId = Guid.NewGuid();

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Test skill",
            Properties = []
        };

        var config = new DigitalTwinAgentConfiguration { NominalDurationSeconds = 5.0 };
        var mockWs = new CaptureWebSocket();

        var agent = new DigitalTwinRuntimeAgent(
            agentId, "DigitalTwin-Fail", mockWs, [skill], config,
            new TestLogger<DigitalTwinRuntimeAgent>(output));

        var eventBus = new SkillExecutionEventBus(new TestLogger<SkillExecutionEventBus>(output));
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(agent);

        var mockSceneEntityResolver2 = new Mock<ISceneEntityResolver>();
        mockSceneEntityResolver2.Setup(r => r.RefreshSceneEntityProperties(It.IsAny<Skill>())).Returns<Skill>(s => s);

        var coordinator = new SkillExecutionCoordinator(
            eventBus, mockAgentProvider.Object,
            new Mock<IPropertyBindingService>().Object,
            mockSceneEntityResolver2.Object,
            TimeProvider.System,
            new TestLogger<SkillExecutionCoordinator>(output),
            NullLogger<PipelineEvents>.Instance);

        var events = new ConcurrentBag<ExecutionEvent>();

        using var subscription = eventBus.AllEvents
            .Synchronize()
            .Subscribe(evt =>
            {
                output.WriteLine($"[EVENT] {evt.EventType} for skill {evt.SkillId}");
                events.Add(evt);
            });

        // Act
        var execObs = coordinator.ExecuteSkillAsync(
            skillNodeId, skill, agentId, null, CancellationToken.None);

        var observableCompleted = new TaskCompletionSource<bool>();
        CoordinatorProgress? failedProgress = null;

        using var execSub = execObs.Subscribe(
            p => failedProgress = p,
            _ => observableCompleted.TrySetResult(true),
            () => observableCompleted.TrySetResult(true));

        await Task.Delay(100);

        // Extract executionId from the sent command
        var sentJson = Encoding.UTF8.GetString(mockWs.SentMessages[0]);
        var sentEnvelope = DigitalTwinMessages.Deserialize(sentJson);
        var executionId = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(sentEnvelope!)!.ExecutionId;

        // Send one progress then fail
        agent.HandleProgressMessage(new SkillProgressMessage
        {
            ExecutionId = executionId, ProgressPercent = 0.10, StatusMessage = "Starting"
        });
        await Task.Delay(50);

        agent.HandleCompletedMessage(new SkillCompletedMessage
        {
            ExecutionId = executionId, Success = false, ErrorMessage = "Joint limit reached"
        });

        // Wait for observable to complete
        await Task.WhenAny(observableCompleted.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await Task.Delay(100);

        // Assert — Start event published (first progress triggered it)
        var startEvents = events.Where(e => e.EventType == ExecutionEventType.Start).ToList();
        Assert.Single(startEvents);

        // Assert — Progress event published for the initial 10% update
        var progressEvents = events.Where(e => e.EventType == ExecutionEventType.Progress).ToList();
        Assert.Single(progressEvents);

        // Assert — NO Finish event on failure (coordinator withholds it)
        var finishEvents = events.Where(e => e.EventType == ExecutionEventType.Finish).ToList();
        Assert.Empty(finishEvents);

        // Assert — the observable emitted a progress item with IsFailed=true
        Assert.NotNull(failedProgress);
        Assert.True(failedProgress!.IsFailed);
        Assert.Contains("Joint limit", failedProgress.ErrorMessage!);

        output.WriteLine("=== TEST PASSED ===");

        agent.Dispose();
    }

    /// <summary>
    ///     Minimal mock WebSocket for integration tests. Reports <see cref="WebSocketState.Open" />
    ///     and captures all messages sent via <see cref="SendAsync" />.
    /// </summary>
    private class CaptureWebSocket : WebSocket
    {
        public List<byte[]> SentMessages { get; } = [];
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
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

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return new TaskCompletionSource<WebSocketReceiveResult>().Task;
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            SentMessages.Add(buffer.ToArray());
            return Task.CompletedTask;
        }
    }
}