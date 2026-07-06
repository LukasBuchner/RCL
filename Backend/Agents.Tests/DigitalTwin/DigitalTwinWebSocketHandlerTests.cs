using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Services;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Services;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.DigitalTwin;

/// <summary>
///     Tests for <see cref="DigitalTwinWebSocketHandler" />, verifying the full WebSocket lifecycle
///     including registration, message dispatch, ping/pong timeout detection, and disconnection cleanup.
/// </summary>
public class DigitalTwinWebSocketHandlerTests
{
    private static readonly Guid TestSkillId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    ///     Creates a <see cref="DigitalTwinWebSocketHandler" /> with the given configuration,
    ///     a mock <see cref="IAgentManager" />, and a single test skill definition.
    /// </summary>
    /// <param name="config">Configuration overrides. Uses fast defaults if <c>null</c>.</param>
    /// <returns>The handler, mock agent manager, and mock skill provider for assertions.</returns>
    private static (DigitalTwinWebSocketHandler handler, Mock<IAgentManager> agentManager,
        Mock<ISkillDefinitionProvider> skillProvider, Mock<IAgentLifecycleNotifier> lifecycleNotifier)
        CreateHandler(DigitalTwinAgentConfiguration? config = null)
    {
        config ??= new DigitalTwinAgentConfiguration
        {
            PingIntervalSeconds = 0.1,
            PongTimeoutSeconds = 0.5,
            NominalDurationSeconds = 1.0,
            EstimateTimeoutSeconds = 0.5,
            MaxConcurrentExecutions = 1,
            ReceiveBufferSize = 4096
        };

        var mockAgentManager = new Mock<IAgentManager>();
        mockAgentManager.Setup(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>())).Returns(true);
        mockAgentManager.Setup(m => m.StopAgentAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var mockSkillProvider = new Mock<ISkillDefinitionProvider>();
        mockSkillProvider
            .Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                [TestSkillId] = new()
                {
                    Id = TestSkillId,
                    Name = "Move To Position",
                    Description = "Move to a specific position",
                    Properties = []
                }
            });

        var mockLifecycleNotifier = new Mock<IAgentLifecycleNotifier>();
        mockLifecycleNotifier
            .Setup(n => n.OnAgentConnectedAsync(It.IsAny<IRuntimeAgent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockLifecycleNotifier
            .Setup(n => n.OnAgentDisconnectedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockPositionTagRepository = new Mock<IRepository<PositionTag>>();
        mockPositionTagRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([]);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<DigitalTwinWebSocketHandler>();

        var options = Options.Create(config);
        var handler = new DigitalTwinWebSocketHandler(
            mockAgentManager.Object, options, loggerFactory, logger, mockSkillProvider.Object,
            mockLifecycleNotifier.Object, mockPositionTagRepository.Object);

        return (handler, mockAgentManager, mockSkillProvider, mockLifecycleNotifier);
    }

    /// <summary>
    ///     Builds a JSON string for a <see cref="RegisterMessage" /> envelope.
    /// </summary>
    /// <param name="agentName">Name of the agent to register.</param>
    /// <param name="skillIds">Skill IDs to include in the registration.</param>
    /// <returns>A JSON-serialized <see cref="DigitalTwinEnvelope" /> string.</returns>
    private static string MakeRegisterJson(string agentName, params Guid[] skillIds)
    {
        return DigitalTwinMessages.Serialize(
            DigitalTwinMessages.MessageTypes.Register,
            new RegisterMessage { AgentName = agentName, AvailableSkillIds = skillIds.ToList() });
    }

    /// <summary>
    ///     Builds a JSON string for a <see cref="PongMessage" /> envelope.
    /// </summary>
    /// <returns>A JSON-serialized pong envelope string.</returns>
    private static string MakePongJson()
    {
        return DigitalTwinMessages.Serialize(
            DigitalTwinMessages.MessageTypes.Pong,
            new PongMessage { OriginalTimestampUtc = DateTime.UtcNow });
    }

    // ─── Registration ────────────────────────────────────────────────

    [Fact]
    public async Task ValidRegistration_RegistersAgentWithManager()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-01", TestSkillId));
        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        agentManager.Verify(
            m => m.RegisterAgent(It.Is<IRuntimeAgent>(a => a.Name == "Twin-01")),
            Times.Once);
    }

    [Fact]
    public async Task TwinDisconnects_UnregistersAgent()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-02", TestSkillId));
        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        agentManager.Verify(m => m.StopAgentAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task NonRegisterFirstMessage_ClosesWithoutRegistering()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        // Send a progress message instead of register
        var badJson = DigitalTwinMessages.Serialize(
            DigitalTwinMessages.MessageTypes.SkillProgress,
            new SkillProgressMessage
            {
                ExecutionId = Guid.NewGuid(), ProgressPercent = 0.5, StatusMessage = "wrong"
            });
        ws.EnqueueMessage(badJson);
        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Never);
    }

    [Fact]
    public async Task CloseBeforeRegistration_DoesNotRegister()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Never);
    }

    [Fact]
    public async Task InvalidJsonDuringRegistration_ClosesGracefully()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage("{not valid json!!");
        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Never);
    }

    // ─── Message dispatch ────────────────────────────────────────────

    [Fact]
    public async Task SkillProgressMessage_DispatchedWithoutError()
    {
        var (handler, _, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-Prog", TestSkillId));
        ws.EnqueueMessage(DigitalTwinMessages.Serialize(
            DigitalTwinMessages.MessageTypes.SkillProgress,
            new SkillProgressMessage
            {
                ExecutionId = Guid.NewGuid(), ProgressPercent = 0.5, StatusMessage = "halfway"
            }));
        ws.EnqueueClose();

        // Should not throw
        await handler.HandleConnectionAsync(ws, CancellationToken.None);
    }

    [Fact]
    public async Task UnknownMessageType_LogsAndContinues()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-Unk", TestSkillId));
        ws.EnqueueMessage(DigitalTwinMessages.Serialize("TotallyUnknownType", new { Foo = "bar" }));
        ws.EnqueueClose();

        // Should not throw — unknown type is logged and skipped
        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        // Agent should still have been registered and then unregistered on close
        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Once);
        agentManager.Verify(m => m.StopAgentAsync(It.IsAny<Guid>()), Times.Once);
    }

    // ─── Ping / Pong ─────────────────────────────────────────────────

    [Fact]
    public async Task PingSent_AfterInterval()
    {
        var (handler, _, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-Ping", TestSkillId));

        // Keep the connection alive with periodic pongs, then close
        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            ws.EnqueueClose();
        });

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        // Verify at least one Ping was sent
        var pingSent = ws.SentMessages.Any(msg =>
        {
            var json = Encoding.UTF8.GetString(msg);
            return json.Contains("\"Ping\"", StringComparison.OrdinalIgnoreCase);
        });
        Assert.True(pingSent, "Expected at least one Ping message to be sent");
    }

    [Fact]
    public async Task PongTimeout_DisconnectsAgent()
    {
        var config = new DigitalTwinAgentConfiguration
        {
            PingIntervalSeconds = 0.05,
            PongTimeoutSeconds = 0.15,
            NominalDurationSeconds = 1.0,
            EstimateTimeoutSeconds = 0.5,
            MaxConcurrentExecutions = 1,
            ReceiveBufferSize = 4096
        };
        var (handler, agentManager, _, _) = CreateHandler(config);
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-Timeout", TestSkillId));
        // No pongs enqueued — the receive loop will block until cancelled by the ping loop timeout

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleConnectionAsync(ws, cts.Token);

        // Agent should have been unregistered due to pong timeout
        agentManager.Verify(m => m.StopAgentAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task PongsArrivingOnTime_ConnectionStaysAlive()
    {
        var config = new DigitalTwinAgentConfiguration
        {
            PingIntervalSeconds = 0.05,
            PongTimeoutSeconds = 0.3,
            NominalDurationSeconds = 1.0,
            EstimateTimeoutSeconds = 0.5,
            MaxConcurrentExecutions = 1,
            ReceiveBufferSize = 4096
        };
        var (handler, agentManager, _, _) = CreateHandler(config);
        var ws = new ScriptedMockWebSocket();

        ws.EnqueueMessage(MakeRegisterJson("Twin-PongsOk", TestSkillId));

        // Feed pongs and then close gracefully
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < 4; i++)
            {
                await Task.Delay(60);
                ws.EnqueueMessage(MakePongJson());
            }

            await Task.Delay(60);
            ws.EnqueueClose();
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleConnectionAsync(ws, cts.Token);

        // Agent should have been registered and unregistered once (graceful close, not timeout)
        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Once);
        agentManager.Verify(m => m.StopAgentAsync(It.IsAny<Guid>()), Times.Once);
    }

    // ─── Concurrent connections ──────────────────────────────────────

    [Fact]
    public async Task MultipleConcurrentConnections_EachRegisteredSeparately()
    {
        var (handler, agentManager, _, _) = CreateHandler();

        var ws1 = new ScriptedMockWebSocket();
        ws1.EnqueueMessage(MakeRegisterJson("Twin-A", TestSkillId));
        ws1.EnqueueClose();

        var ws2 = new ScriptedMockWebSocket();
        ws2.EnqueueMessage(MakeRegisterJson("Twin-B", TestSkillId));
        ws2.EnqueueClose();

        await Task.WhenAll(
            handler.HandleConnectionAsync(ws1, CancellationToken.None),
            handler.HandleConnectionAsync(ws2, CancellationToken.None));

        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Exactly(2));
        agentManager.Verify(m => m.StopAgentAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }

    // ─── Skill resolution ────────────────────────────────────────────

    [Fact]
    public async Task UnknownSkillIds_RegistersWithResolvedSkillsOnly()
    {
        var (handler, agentManager, _, _) = CreateHandler();
        var ws = new ScriptedMockWebSocket();

        var unknownId = Guid.NewGuid();
        ws.EnqueueMessage(MakeRegisterJson("Twin-Skills", TestSkillId, unknownId));
        ws.EnqueueClose();

        await handler.HandleConnectionAsync(ws, CancellationToken.None);

        // Should still register the agent (with only the valid skill resolved)
        agentManager.Verify(m => m.RegisterAgent(It.IsAny<IRuntimeAgent>()), Times.Once);
    }

    // ─── ScriptedMockWebSocket ───────────────────────────────────────

    /// <summary>
    ///     Mock <see cref="WebSocket" /> that uses a queue of scripted incoming messages.
    ///     <see cref="ReceiveAsync" /> blocks via <see cref="SemaphoreSlim" /> until a message
    ///     is enqueued, simulating the real async receive pattern.
    ///     Outgoing messages sent via <see cref="SendAsync" /> are captured in <see cref="SentMessages" />.
    /// </summary>
    private class ScriptedMockWebSocket : WebSocket
    {
        private readonly ConcurrentQueue<ScriptedFrame> _incomingQueue = new();
        private readonly SemaphoreSlim _incomingSignal = new(0);
        private volatile WebSocketState _state = WebSocketState.Open;

        /// <summary>
        ///     All messages sent by the handler via <see cref="SendAsync" />.
        /// </summary>
        public ConcurrentBag<byte[]> SentMessages { get; } = [];

        public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed
            ? WebSocketCloseStatus.NormalClosure
            : null;

        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        /// <summary>
        ///     Enqueues a text message that <see cref="ReceiveAsync" /> will return.
        /// </summary>
        /// <param name="json">The JSON string to deliver.</param>
        public void EnqueueMessage(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            _incomingQueue.Enqueue(new ScriptedFrame(bytes, WebSocketMessageType.Text));
            _incomingSignal.Release();
        }

        /// <summary>
        ///     Enqueues a close frame that <see cref="ReceiveAsync" /> will return,
        ///     causing the handler to interpret the connection as closed.
        /// </summary>
        public void EnqueueClose()
        {
            _incomingQueue.Enqueue(new ScriptedFrame([], WebSocketMessageType.Close));
            _incomingSignal.Release();
        }

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _incomingSignal.Dispose();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await _incomingSignal.WaitAsync(cancellationToken);

            if (!_incomingQueue.TryDequeue(out var frame))
                throw new InvalidOperationException("Signal fired but queue was empty");

            if (frame.MessageType == WebSocketMessageType.Close)
            {
                _state = WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true,
                    WebSocketCloseStatus.NormalClosure, "");
            }

            var count = Math.Min(frame.Data.Length, buffer.Count);
            Array.Copy(frame.Data, 0, buffer.Array!, buffer.Offset, count);
            return new WebSocketReceiveResult(count, frame.MessageType, true);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            if (_state != WebSocketState.Open && _state != WebSocketState.CloseReceived)
                throw new WebSocketException("WebSocket is not open");

            SentMessages.Add(buffer.ToArray());
            return Task.CompletedTask;
        }

        /// <summary>
        ///     A single scripted WebSocket frame containing the raw bytes and message type.
        /// </summary>
        /// <param name="Data">The raw byte payload.</param>
        /// <param name="MessageType">The WebSocket message type (Text or Close).</param>
        private record ScriptedFrame(byte[] Data, WebSocketMessageType MessageType);
    }
}