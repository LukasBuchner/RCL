using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Services;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Agents.Agents.DigitalTwin.Services;

/// <summary>
///     Handles incoming WebSocket connections from Unity Digital Twin instances.
///     For each connection, this handler:
///     <list type="number">
///         <item>Upgrades the HTTP connection to a WebSocket</item>
///         <item>Waits for a <see cref="RegisterMessage" /> from the Twin</item>
///         <item>Creates a <see cref="DigitalTwinRuntimeAgent" /> wrapping the connection</item>
///         <item>Registers the agent with <see cref="IAgentManager" /></item>
///         <item>Enters a message receive loop, dispatching messages to the agent</item>
///         <item>On disconnect: unregisters the agent and cancels in-progress executions</item>
///     </list>
/// </summary>
/// <remarks>
///     <para>
///         Multiple Digital Twin instances can connect simultaneously; each gets its own agent.
///         The handler is registered as a singleton and mapped to the <c>/ws/digital-twin</c> endpoint.
///     </para>
///     <para>
///         A background keepalive ping loop runs for each connection to detect disconnections.
///     </para>
/// </remarks>
public class DigitalTwinWebSocketHandler
{
    private readonly IAgentManager _agentManager;
    private readonly DigitalTwinAgentConfiguration _configuration;
    private readonly IAgentLifecycleNotifier _lifecycleNotifier;
    private readonly ILogger<DigitalTwinWebSocketHandler> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRepository<PositionTag> _positionTagRepository;
    private readonly ISkillDefinitionProvider _skillDefinitionProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DigitalTwinWebSocketHandler" /> class.
    /// </summary>
    /// <param name="agentManager">The agent manager to register/unregister Digital Twin agents with.</param>
    /// <param name="configuration">Configuration options for Digital Twin connections.</param>
    /// <param name="loggerFactory">Factory for creating per-agent logger instances.</param>
    /// <param name="logger">Logger for handler-level events.</param>
    /// <param name="skillDefinitionProvider">Provider for resolving skill definitions by ID.</param>
    /// <param name="lifecycleNotifier">
    ///     Notifier for domain-level registration of dynamically connected agents.
    ///     Ensures agents appear in GraphQL queries and have their skills synchronized.
    /// </param>
    /// <param name="positionTagRepository">
    ///     Repository for loading position tags from the database. Used during skill resolution
    ///     to populate default values of PositionTag-typed skill properties.
    /// </param>
    public DigitalTwinWebSocketHandler(
        IAgentManager agentManager,
        IOptions<DigitalTwinAgentConfiguration> configuration,
        ILoggerFactory loggerFactory,
        ILogger<DigitalTwinWebSocketHandler> logger,
        ISkillDefinitionProvider skillDefinitionProvider,
        IAgentLifecycleNotifier lifecycleNotifier,
        IRepository<PositionTag> positionTagRepository)
    {
        _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _skillDefinitionProvider =
            skillDefinitionProvider ?? throw new ArgumentNullException(nameof(skillDefinitionProvider));
        _lifecycleNotifier = lifecycleNotifier ?? throw new ArgumentNullException(nameof(lifecycleNotifier));
        _positionTagRepository =
            positionTagRepository ?? throw new ArgumentNullException(nameof(positionTagRepository));
    }

    /// <summary>
    ///     Handles a WebSocket connection from a Digital Twin.
    ///     This method blocks until the WebSocket closes or an error occurs.
    /// </summary>
    /// <param name="webSocket">The accepted WebSocket connection.</param>
    /// <param name="cancellationToken">Token signalling application shutdown.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    public async Task HandleConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        _logger.LogConnectionEstablished();

        DigitalTwinRuntimeAgent? agent = null;

        try
        {
            // Wait for registration message
            var registerMessage = await WaitForRegistrationAsync(webSocket, cancellationToken);
            if (registerMessage == null)
            {
                _logger.LogConnectionClosedBeforeRegistration();
                return;
            }

            _logger.LogTwinRegistered(registerMessage.AgentName, registerMessage.AvailableSkillIds.Count);

            // Resolve skills from skill definition provider
            var skills = await ResolveSkillsAsync(registerMessage.AvailableSkillIds);

            // Use the Twin's stable ID if provided; otherwise derive a deterministic ID from the name
            var agentId = registerMessage.AgentId
                          ?? GenerateDeterministicId(registerMessage.AgentName);
            var agentLogger = _loggerFactory.CreateLogger<DigitalTwinRuntimeAgent>();
            agent = new DigitalTwinRuntimeAgent(
                agentId,
                registerMessage.AgentName,
                webSocket,
                skills,
                _configuration,
                agentLogger);

            // With stable IDs, a reconnecting Twin may still have a stale in-memory entry.
            // Remove it first so the new agent instance takes over.
            var existingAgent = _agentManager.GetAgent(agentId);
            if (existingAgent != null)
            {
                _logger.LogReplacingStaleAgent(existingAgent.Name, existingAgent.Id);
                await _agentManager.StopAgentAsync(agentId);
            }

            _agentManager.RegisterAgent(agent);
            _logger.LogAgentRegistered(agent.Name, agent.Id, skills.Count);

            // Register in the persistent domain model so the agent appears in GraphQL queries
            await _lifecycleNotifier.OnAgentConnectedAsync(agent, cancellationToken);

            // Start ping loop and receive loop concurrently.
            // If either exits (receive closes, ping timeout, or error), cancel the other.
            using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var pingTask = RunPingLoopAsync(agent, connectionCts.Token);
            var receiveTask = ReceiveMessagesAsync(agent, connectionCts.Token);

            await Task.WhenAny(receiveTask, pingTask);
            await connectionCts.CancelAsync();

            // Await both for clean completion (swallow expected cancellation)
            try
            {
                await Task.WhenAll(receiveTask, pingTask);
            }
            catch (OperationCanceledException)
            {
                /* expected — the other task was cancelled */
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWebSocketError(ex, agent?.Name ?? "unknown");
        }
        catch (OperationCanceledException)
        {
            _logger.LogConnectionCancelledShutdown();
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedConnectionError(ex);
        }
        finally
        {
            if (agent != null)
            {
                agent.HandleDisconnection();
                await _agentManager.StopAgentAsync(agent.Id);

                // Remove from persistent domain model
                try
                {
                    await _lifecycleNotifier.OnAgentDisconnectedAsync(agent.Id, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDomainRecordRemovalFailed(ex, agent.Name, agent.Id);
                }

                _logger.LogAgentUnregistered(agent.Name, agent.Id);
                agent.Dispose();
            }
        }
    }

    /// <summary>
    ///     Waits for the first message on the WebSocket and expects it to be a <see cref="RegisterMessage" />.
    ///     Returns <c>null</c> if the connection closes before a valid registration is received.
    /// </summary>
    /// <param name="webSocket">The WebSocket to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed registration message, or <c>null</c> if invalid or connection closed.</returns>
    private async Task<RegisterMessage?> WaitForRegistrationAsync(
        WebSocket webSocket, CancellationToken cancellationToken)
    {
        var json = await ReceiveStringAsync(webSocket, cancellationToken);
        if (json == null) return null;

        var envelope = DigitalTwinMessages.Deserialize(json);
        if (envelope == null)
        {
            _logger.LogInvalidJsonDuringRegistration();
            return null;
        }

        if (envelope.Type != DigitalTwinMessages.MessageTypes.Register)
        {
            _logger.LogUnexpectedMessageDuringRegistration(envelope.Type);
            return null;
        }

        return DigitalTwinMessages.ExtractPayload<RegisterMessage>(envelope);
    }

    /// <summary>
    ///     Resolves skill definition IDs to <see cref="Skill" /> domain entities
    ///     using the skill definition provider.
    /// </summary>
    /// <param name="skillIds">The skill definition IDs reported by the Twin.</param>
    /// <returns>A list of resolved <see cref="Skill" /> entities.</returns>
    private async Task<List<Skill>> ResolveSkillsAsync(List<Guid> skillIds)
    {
        var allDefinitions = await _skillDefinitionProvider.GetSkillDefinitionsAsync();

        // Load position tags so PositionTag-typed property defaults can be resolved
        var allTags = await _positionTagRepository.GetAllAsync();
        var positionTags = allTags.ToDictionary(t => t.Id);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogPositionTagsLoaded(
                positionTags.Count,
                string.Join(", ", positionTags.Select(t =>
                    $"{t.Key} '{t.Value.Tag}' ({t.Value.Position.X:F3},{t.Value.Position.Y:F3},{t.Value.Position.Z:F3} rot {t.Value.Position.Alpha:F1},{t.Value.Position.Beta:F1},{t.Value.Position.Gamma:F1})")));

        var skills = new List<Skill>();

        foreach (var skillId in skillIds)
            if (allDefinitions.TryGetValue(skillId, out var definition))
            {
                var skill = new Skill
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Description = definition.Description,
                    Properties = definition.Properties
                        .Select(p => p.ToProperty(positionTags))
                        .ToList()
                };
                skills.Add(skill);
                _logger.LogSkillResolved(skill.Name, skill.Id);
            }
            else
            {
                _logger.LogUnknownSkillDefinition(skillId);
            }

        return skills;
    }

    /// <summary>
    ///     Main message receive loop. Reads messages from the WebSocket and dispatches
    ///     them to the appropriate handler on the <see cref="DigitalTwinRuntimeAgent" />.
    /// </summary>
    /// <param name="agent">The agent to dispatch messages to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ReceiveMessagesAsync(
        DigitalTwinRuntimeAgent agent, CancellationToken cancellationToken)
    {
        while (agent.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            var json = await ReceiveStringAsync(agent.WebSocket, cancellationToken);
            if (json == null) break; // Connection closed

            try
            {
                var envelope = DigitalTwinMessages.Deserialize(json);
                if (envelope == null)
                {
                    _logger.LogInvalidJsonFromTwin(agent.Name);
                    continue;
                }

                DispatchMessage(agent, envelope);
            }
            catch (Exception ex)
            {
                _logger.LogMessageProcessingError(ex, agent.Name);
            }
        }
    }

    /// <summary>
    ///     Dispatches a deserialized envelope to the correct handler method on the agent
    ///     based on the message type discriminator.
    /// </summary>
    /// <param name="agent">The agent to dispatch to.</param>
    /// <param name="envelope">The deserialized message envelope.</param>
    private void DispatchMessage(DigitalTwinRuntimeAgent agent, DigitalTwinEnvelope envelope)
    {
        switch (envelope.Type)
        {
            case DigitalTwinMessages.MessageTypes.SkillProgress:
                var progress = DigitalTwinMessages.ExtractPayload<SkillProgressMessage>(envelope);
                if (progress != null) agent.HandleProgressMessage(progress);
                break;

            case DigitalTwinMessages.MessageTypes.SkillCompleted:
                var completed = DigitalTwinMessages.ExtractPayload<SkillCompletedMessage>(envelope);
                if (completed != null) agent.HandleCompletedMessage(completed);
                break;

            case DigitalTwinMessages.MessageTypes.Pong:
                var pong = DigitalTwinMessages.ExtractPayload<PongMessage>(envelope);
                if (pong != null) agent.HandlePong(pong);
                break;

            case DigitalTwinMessages.MessageTypes.HealthStatus:
                var health = DigitalTwinMessages.ExtractPayload<HealthStatusMessage>(envelope);
                if (health != null) agent.HandleHealthStatus(health);
                break;

            case DigitalTwinMessages.MessageTypes.EstimateDurationResponse:
                var estimate = DigitalTwinMessages.ExtractPayload<EstimateDurationResponse>(envelope);
                if (estimate != null) agent.HandleEstimateDurationResponse(estimate);
                break;

            default:
                _logger.LogUnknownMessageType(envelope.Type, agent.Name);
                break;
        }
    }

    /// <summary>
    ///     Periodically sends <see cref="PingCommand" /> messages to the Digital Twin
    ///     to keep the connection alive and detect disconnections.
    /// </summary>
    /// <param name="agent">The agent to ping.</param>
    /// <param name="cancellationToken">Cancellation token to stop the loop.</param>
    private async Task RunPingLoopAsync(DigitalTwinRuntimeAgent agent, CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(_configuration.PingIntervalSeconds);
        var pongTimeout = TimeSpan.FromSeconds(_configuration.PongTimeoutSeconds);

        while (!cancellationToken.IsCancellationRequested && agent.IsConnected)
            try
            {
                await Task.Delay(interval, cancellationToken);

                // Check whether the Twin has gone silent (no pong, progress, or health messages)
                var timeSinceLastSeen = DateTime.UtcNow - agent.LastSeenUtc;
                if (timeSinceLastSeen > pongTimeout)
                {
                    _logger.LogPongTimeout(agent.Name, timeSinceLastSeen.TotalSeconds, pongTimeout.TotalSeconds);
                    break;
                }

                await agent.SendMessageAsync(
                    DigitalTwinMessages.MessageTypes.Ping,
                    new PingCommand(),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogPingFailed(ex, agent.Name);
                break;
            }
    }

    /// <summary>
    ///     Reads a complete text message from the WebSocket, handling multi-frame messages.
    /// </summary>
    /// <param name="webSocket">The WebSocket to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     The received string, or <c>null</c> if the connection was closed.
    /// </returns>
    private async Task<string?> ReceiveStringAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[_configuration.ReceiveBufferSize];
        var messageBuilder = new StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        } while (!result.EndOfMessage);

        return messageBuilder.ToString();
    }

    /// <summary>
    ///     Generates a deterministic <see cref="Guid" /> from an agent name using a version-5 UUID
    ///     approach (SHA-256 hash truncated to 16 bytes). This ensures that a Digital Twin
    ///     reconnecting with the same name reuses the same agent ID, preserving workflow references.
    /// </summary>
    /// <param name="agentName">The agent name to derive the ID from.</param>
    /// <returns>A stable, deterministic GUID.</returns>
    private static Guid GenerateDeterministicId(string agentName)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"DigitalTwin:{agentName}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}