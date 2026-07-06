using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Agents.DigitalTwin;

/// <summary>
///     Runtime agent implementation that bridges a WebSocket connection to a Unity Digital Twin
///     and the Freydis <see cref="IRuntimeAgent" /> interface. Receives skill execution commands
///     from the orchestrator, serializes them as JSON, and sends them over WebSocket. Incoming
///     progress and completion messages from the Twin are translated into
///     <see cref="IObservable{SkillExecutionProgress}" /> streams consumed by the execution pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Each connected Digital Twin instance gets its own <see cref="DigitalTwinRuntimeAgent" />.
///         The agent holds the WebSocket reference and manages a map of active executions,
///         each backed by a <see cref="Subject" /> that emits progress
///         updates as WebSocket messages arrive.
///     </para>
///     <para>
///         Duration estimation is delegated to the Twin via a request/response query over WebSocket.
///         If the Twin does not respond within the configured timeout, a nominal fallback is used.
///     </para>
///     <para>
///         Adaptive execution is not supported; <see cref="ExecuteSkillAdaptivelyAsync" /> throws
///         <see cref="NotSupportedException" />.
///     </para>
/// </remarks>
public class DigitalTwinRuntimeAgent : IRuntimeAgent, IDisposable
{
    private readonly ConcurrentDictionary<Guid, ActiveExecution> _activeExecutions = new();
    private readonly List<Skill> _availableSkills;
    private readonly DigitalTwinAgentConfiguration _configuration;
    private readonly Lock _healthLock = new();
    private readonly ILogger<DigitalTwinRuntimeAgent> _logger;

    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<EstimateDurationResponse>> _pendingEstimates =
        new();

    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private volatile int _activeExecutionCount;
    private bool _disposed;
    private volatile int _failedExecutions;
    private HealthStatusMessage? _lastHealthStatus;
    private DateTime _lastSeenUtc = DateTime.UtcNow;
    private volatile int _totalExecutions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DigitalTwinRuntimeAgent" /> class.
    /// </summary>
    /// <param name="id">Unique identifier for this agent.</param>
    /// <param name="name">Display name for this agent, as reported by the Twin's registration message.</param>
    /// <param name="webSocket">The active WebSocket connection to the Digital Twin.</param>
    /// <param name="availableSkills">Skills the Twin reported as available during registration.</param>
    /// <param name="configuration">Configuration options for this agent (timeouts, nominal duration, etc.).</param>
    /// <param name="logger">Logger instance.</param>
    public DigitalTwinRuntimeAgent(
        Guid id,
        string name,
        WebSocket webSocket,
        IEnumerable<Skill> availableSkills,
        DigitalTwinAgentConfiguration configuration,
        ILogger<DigitalTwinRuntimeAgent> logger)
    {
        Id = id;
        Name = name;
        WebSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _availableSkills = availableSkills.ToList();
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the WebSocket connection to the Digital Twin.
    ///     Used by the <see cref="Services.DigitalTwinWebSocketHandler" /> to send messages.
    /// </summary>
    internal WebSocket WebSocket { get; }

    /// <summary>
    ///     Indicates whether the WebSocket connection to the Digital Twin is still open.
    /// </summary>
    public bool IsConnected => WebSocket.State == WebSocketState.Open;

    /// <summary>
    ///     Gets the UTC timestamp of the last received message (pong, progress, health, etc.)
    ///     from the Digital Twin. Read under <see cref="_healthLock" /> for thread safety.
    ///     Used by the <see cref="Services.DigitalTwinWebSocketHandler" /> to detect stale connections.
    /// </summary>
    internal DateTime LastSeenUtc
    {
        get
        {
            lock (_healthLock)
            {
                return _lastSeenUtc;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        HandleDisconnection();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        HealthStatusMessage? healthSnapshot;
        lock (_healthLock)
        {
            _lastSeenUtc = DateTime.UtcNow;
            healthSnapshot = _lastHealthStatus;
        }

        var uptime = DateTime.UtcNow - _startedUtc;

        var status = new AgentHealthStatus
        {
            AgentId = Id,
            AgentName = Name,
            IsHealthy = IsConnected,
            IsAvailable = IsConnected && _activeExecutionCount < _configuration.MaxConcurrentExecutions,
            ActiveExecutions = _activeExecutionCount,
            TotalExecutionsCompleted = _totalExecutions,
            FailedExecutions = _failedExecutions,
            LastSeenUtc = _lastSeenUtc,
            StartedUtc = _startedUtc,
            CpuUsagePercent = healthSnapshot?.CpuPercent,
            MemoryUsageMb = healthSnapshot?.MemoryMb,
            AverageExecutionTimeSeconds = _totalExecutions > 0 ? uptime.TotalSeconds / _totalExecutions : null,
            StatusMessage = IsConnected
                ? _activeExecutionCount > 0
                    ? $"Executing {_activeExecutionCount} skill(s)"
                    : "Idle - ready for work"
                : "Disconnected",
            ErrorMessage = IsConnected ? null : "WebSocket connection lost",
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["ConnectionState"] = WebSocket.State.ToString(),
                ["UptimeSeconds"] = uptime.TotalSeconds,
                ["Fps"] = healthSnapshot?.Fps ?? 0
            }
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Skill>>(_availableSkills.AsReadOnly());
    }

    /// <summary>
    ///     Provides a duration estimate for the given skill. Sends an <see cref="EstimateDurationQuery" />
    ///     to the Digital Twin and waits for an <see cref="EstimateDurationResponse" />.
    ///     Falls back to the configured nominal duration if the Twin does not respond within the timeout.
    ///     Uses the agent's own skill definition (from skills-config.json) rather than the passed domain
    ///     skill, which may be a stale embedded copy from the procedure node.
    /// </summary>
    /// <param name="skill">The skill to estimate (matched by ID against the agent's own definitions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A <see cref="SkillExecutionEstimate" /> with duration information,
    ///     or <c>null</c> if the agent does not support this skill.
    /// </returns>
    public async Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill,
        CancellationToken cancellationToken = default)
    {
        if (_availableSkills.All(s => s.Id != skill.Id))
        {
            _logger.LogSkillNotAvailable(Name, skill.Name, skill.Id);
            return null;
        }

        // Resolve from the agent's own skill list to ensure properties are up-to-date
        // (the passed domain skill may be a stale embedded copy from the procedure node)
        var resolvedSkill = _availableSkills.FirstOrDefault(s => s.Id == skill.Id) ?? skill;

        // Check for a Duration property (Hold Position skill)
        var durationProperty = resolvedSkill.Properties.FirstOrDefault(p =>
            p is { Name: "Duration", Value.Type: NumberType });

        if (durationProperty?.Value.Value is double durationSeconds and > 0)
        {
            _logger.LogHoldPositionEstimate(
                skill.Name, durationSeconds,
                _configuration.HoldMinDurationSeconds);

            return new SkillExecutionEstimate
            {
                Skill = skill,
                AgentId = Id,
                CanExecuteAdaptively = true,
                EstimatedNominalDuration = durationSeconds,
                MinAdaptiveDuration = _configuration.HoldMinDurationSeconds
            };
        }

        // Extract position parameters from the skill for the query
        var positionProperty = resolvedSkill.Properties.FirstOrDefault(p =>
            p.Value.Type is PositionType);

        if (positionProperty?.Value.Value is Position position && IsConnected)
        {
            var query = new EstimateDurationQuery
            {
                SkillName = skill.Name,
                Parameters = new SkillParameters
                {
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z,
                    Alpha = position.Alpha,
                    Beta = position.Beta,
                    Gamma = position.Gamma
                }
            };

            var tcs = new TaskCompletionSource<EstimateDurationResponse>();
            _pendingEstimates[query.QueryId] = tcs;

            try
            {
                await SendMessageAsync(DigitalTwinMessages.MessageTypes.EstimateDurationQuery, query,
                    cancellationToken);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.EstimateTimeoutSeconds));

                var response = await tcs.Task.WaitAsync(timeoutCts.Token);

                _logger.LogDurationEstimateReceived(skill.Name, response.EstimatedDurationSeconds);

                return new SkillExecutionEstimate
                {
                    Skill = skill,
                    AgentId = Id,
                    CanExecuteAdaptively = false,
                    EstimatedNominalDuration = response.EstimatedDurationSeconds,
                    MinAdaptiveDuration = response.MinDuration
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogEstimateTimeout(skill.Name, Name, _configuration.NominalDurationSeconds);
            }
            finally
            {
                _pendingEstimates.TryRemove(query.QueryId, out _);
            }
        }

        // Fallback to configured nominal duration
        return new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = Id,
            CanExecuteAdaptively = false,
            EstimatedNominalDuration = _configuration.NominalDurationSeconds
        };
    }

    /// <summary>
    ///     Determines whether the given skill can be executed adaptively.
    ///     Hold Position skills support adaptive execution (the robot holds until the
    ///     orchestrator's finish signal fires), while movement skills do not.
    ///     Uses the agent's own skill definition (from skills-config.json) rather than the
    ///     passed domain skill, which may be a stale embedded copy from the procedure node.
    /// </summary>
    /// <param name="skill">The skill to check (matched by ID against the agent's own definitions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the skill is a Hold Position skill; <c>false</c> otherwise.</returns>
    public Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        var resolvedSkill = _availableSkills.FirstOrDefault(s => s.Id == skill.Id) ?? skill;
        var hasDuration = resolvedSkill.Properties.Any(p =>
            p is { Name: "Duration", Value.Type: NumberType });
        return Task.FromResult(hasDuration);
    }

    /// <summary>
    ///     Executes a skill by sending an <see cref="ExecuteSkillCommand" /> to the Digital Twin
    ///     over WebSocket and returning an observable that emits <see cref="SkillExecutionProgress" />
    ///     updates as the Twin reports convergence progress.
    /// </summary>
    /// <param name="executionId">Unique identifier for this execution instance, provided by the orchestrator.</param>
    /// <param name="skillToExecute">The skill to execute, including its property values (e.g., target position).</param>
    /// <param name="cancellationToken">Token for cancelling the execution. When triggered, a
    ///     <see cref="CancelSkillCommand" /> is sent to the Twin.</param>
    /// <returns>
    ///     An observable stream of <see cref="SkillExecutionProgress" /> updates.
    ///     The stream completes when the Twin sends a <see cref="SkillCompletedMessage" />.
    /// </returns>
    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid executionId,
        Skill skillToExecute,
        CancellationToken cancellationToken)
    {
        return Observable.Defer(() =>
        {
            var subject = new Subject<SkillExecutionProgress>();
            var startTimeUtc = DateTime.UtcNow;

            var execution = new ActiveExecution
            {
                ExecutionId = executionId,
                Skill = skillToExecute,
                Subject = subject,
                StartTimeUtc = startTimeUtc,
                CancellationToken = cancellationToken
            };

            if (!_activeExecutions.TryAdd(executionId, execution))
            {
                _logger.LogDuplicateExecution(executionId, Name);
                return Observable.Throw<SkillExecutionProgress>(
                    new InvalidOperationException($"Execution {executionId} already in progress"));
            }

            Interlocked.Increment(ref _activeExecutionCount);

            // Extract position from skill properties — supports both direct Position
            // values (Move To Position) and PositionTag values (Move To Position Tag),
            // where the tag's embedded Position is used as the target coordinates.
            var parameters = new SkillParameters();
            var positionProperty = skillToExecute.Properties.FirstOrDefault(p =>
                p.Value.Type is PositionType);

            if (positionProperty?.Value.Value is Position pos)
            {
                parameters.X = pos.X;
                parameters.Y = pos.Y;
                parameters.Z = pos.Z;
                parameters.Alpha = pos.Alpha;
                parameters.Beta = pos.Beta;
                parameters.Gamma = pos.Gamma;
                _logger.LogSkillPositionResolved(
                    skillToExecute.Name, pos.X, pos.Y, pos.Z, pos.Alpha, pos.Beta, pos.Gamma);
            }
            else
            {
                var tagProperty = skillToExecute.Properties.FirstOrDefault(p =>
                    p.Value.Type is PositionTagType);
                if (tagProperty?.Value.Value is PositionTag tag)
                {
                    parameters.X = tag.Position.X;
                    parameters.Y = tag.Position.Y;
                    parameters.Z = tag.Position.Z;
                    parameters.Alpha = tag.Position.Alpha;
                    parameters.Beta = tag.Position.Beta;
                    parameters.Gamma = tag.Position.Gamma;
                    _logger.LogSkillPositionTagResolved(
                        skillToExecute.Name, tag.Tag, tag.Id,
                        tag.Position.X, tag.Position.Y, tag.Position.Z,
                        tag.Position.Alpha, tag.Position.Beta, tag.Position.Gamma);
                }
                else
                {
                    _logger.LogSkillNoPositionProperty(skillToExecute.Name);
                }
            }

            // Extract duration (Hold Position skill)
            var durationProperty = skillToExecute.Properties.FirstOrDefault(p =>
                p is { Name: "Duration", Value.Type: NumberType });

            if (durationProperty?.Value.Value is double duration and > 0) parameters.Duration = duration;

            var command = new ExecuteSkillCommand
            {
                ExecutionId = executionId,
                SkillName = skillToExecute.Name,
                Parameters = parameters
            };

            // Send the command asynchronously
            _ = SendCommandAndHandleCancellationAsync(command, execution, cancellationToken);

            // The Subject is intentionally not Disposed here. Disposing while HandleDisconnection
            // is concurrently iterating _activeExecutions races with its OnNext/OnCompleted calls
            // on the same subject, which would raise ObjectDisposedException on the WebSocket
            // reader thread and crash the test host. Subject<T> has no unmanaged resources; it is
            // reclaimed by the GC once all subscribers have unsubscribed.
            return subject.AsObservable()
                .Finally(() =>
                {
                    _activeExecutions.TryRemove(executionId, out _);
                    Interlocked.Decrement(ref _activeExecutionCount);
                    Interlocked.Increment(ref _totalExecutions);
                });
        });
    }

    /// <summary>
    ///     Executes a Hold Position skill adaptively. Sends the hold command to the Digital Twin,
    ///     then manages timing on the backend side. The hold completes when the orchestrator's
    ///     <paramref name="finishSignal" /> fires (FF dependency satisfied) or when the execution
    ///     is cancelled; there is no upper-duration bound.
    ///     Movement skills are not supported and throw <see cref="NotSupportedException" />.
    /// </summary>
    /// <param name="executionId">Unique execution instance identifier from the orchestrator.</param>
    /// <param name="skillToExecute">The Hold Position skill to execute.</param>
    /// <param name="initialTargetDuration">Initial target duration from the scheduler.</param>
    /// <param name="plannedFinishTimes">Observable of updated planned finish times from the orchestrator.</param>
    /// <param name="finishSignal">Observable that fires once when finish prerequisites are met.</param>
    /// <param name="cancellationToken">Token for cancelling the execution.</param>
    /// <returns>An observable stream of <see cref="SkillExecutionProgress" /> updates.</returns>
    public IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId,
        Skill skillToExecute,
        double initialTargetDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        CancellationToken cancellationToken)
    {
        // Resolve from the agent's own skill list to ensure properties are up-to-date
        var resolvedSkill = _availableSkills.FirstOrDefault(s => s.Id == skillToExecute.Id) ?? skillToExecute;
        var durationProperty = resolvedSkill.Properties.FirstOrDefault(p =>
            p is { Name: "Duration", Value.Type: NumberType });

        if (durationProperty == null)
            return Observable.Throw<SkillExecutionProgress>(new NotSupportedException(
                $"Agent '{Name}' (DigitalTwin) does not support adaptive execution for '{skillToExecute.Name}'. " +
                "Only Hold Position skills support adaptive execution on Digital Twin agents."));

        return Observable.Defer(() =>
        {
            var startTimeUtc = DateTime.UtcNow;
            Interlocked.Increment(ref _activeExecutionCount);

            var minDuration = _configuration.HoldMinDurationSeconds;
            var currentTargetDuration = Math.Max(initialTargetDuration, minDuration);

            // Send the hold command to Unity. The hold runs until the orchestrator's finish
            // signal fires (or cancellation); the duration sent is the agent's current target.
            var parameters = new SkillParameters { Duration = currentTargetDuration };
            var command = new ExecuteSkillCommand
            {
                ExecutionId = executionId,
                SkillName = skillToExecute.Name,
                Parameters = parameters
            };

            // Register an active execution so Unity failure messages (drift) are captured
            var failureSubject = new Subject<SkillExecutionProgress>();
            var execution = new ActiveExecution
            {
                ExecutionId = executionId,
                Skill = skillToExecute,
                Subject = failureSubject,
                StartTimeUtc = startTimeUtc,
                CancellationToken = cancellationToken
            };
            _activeExecutions.TryAdd(executionId, execution);

            _ = SendCommandAndHandleCancellationAsync(command, execution, cancellationToken);

            // Track finish signal
            var finishSignalFired = false;
            var finishSignalObs = finishSignal
                .Take(1)
                .Do(_ =>
                {
                    finishSignalFired = true;
                    _logger.LogHoldFinishSignalReceived(Name, executionId);
                })
                .Publish()
                .RefCount();

            // Bound planned finish times below by the agent's minimum capability;
            // there is no upper bound — the hold duration is unbounded above.
            var clampedTarget = plannedFinishTimes
                .Select(pf => Math.Max(pf, minDuration))
                .Do(target => _logger.LogHoldPlannedFinishUpdated(Name, target))
                .StartWith(currentTargetDuration);

            // Time-based progress stream (backend-driven, like dummy agent). Completion is
            // driven solely by the finish signal (or cancellation); there is no upper-duration timeout.
            var progressStream = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
                .TakeUntil(finishSignalObs)
                .WithLatestFrom(clampedTarget, (_, target) =>
                {
                    var elapsed = (DateTime.UtcNow - startTimeUtc).TotalSeconds;

                    return new SkillExecutionProgress
                    {
                        ExecutionId = executionId,
                        SkillId = skillToExecute.Id,
                        AgentId = Id,
                        ActualStartTimeUtc = startTimeUtc,
                        CurrentTimeIntoExecution = elapsed,
                        EstimatedTotalDuration = target,
                        StatusMessage =
                            $"Holding position: {elapsed:F1}s / {target:F1}s (min achievable: {minDuration:F1}s)",
                        MinAchievableDuration = minDuration
                    };
                });

            // Monitor for Unity-side failures (drift detection) — merge with progress
            var failureStream = failureSubject.AsObservable()
                .Where(p => p.Error != null);

            // Main stream: progress until finishSignal/maxDuration, interrupted by Unity failure
            var executionStream = progressStream
                .TakeUntil(failureStream)
                .Concat(Observable.Defer(() =>
                {
                    var elapsed = (DateTime.UtcNow - startTimeUtc).TotalSeconds;

                    // Tell Unity to stop holding
                    _ = SendMessageAsync(
                            DigitalTwinMessages.MessageTypes.CancelSkillCommand,
                            new CancelSkillCommand { ExecutionId = executionId })
                        .ContinueWith(
                            t => _logger.LogCancelHoldCommandFailed(t.Exception!.InnerException!, executionId),
                            TaskContinuationOptions.OnlyOnFaulted);

                    Interlocked.Increment(ref _totalExecutions);

                    return Observable.Return(new SkillExecutionProgress
                    {
                        ExecutionId = executionId,
                        SkillId = skillToExecute.Id,
                        AgentId = Id,
                        ActualStartTimeUtc = startTimeUtc,
                        CurrentTimeIntoExecution = elapsed,
                        EstimatedTotalDuration = elapsed,
                        StatusMessage = finishSignalFired
                            ? "Hold completed via finish signal"
                            : "Hold ended",
                        CompletedSuccessfully = true,
                        MinAchievableDuration = minDuration
                    });
                }))
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Finally(() =>
                {
                    // The failure Subject is intentionally not Disposed: HandleDisconnection
                    // may iterate _activeExecutions concurrently and call OnNext/OnCompleted on
                    // this subject. Disposing here raced against that code path and crashed the
                    // test host with ObjectDisposedException. Subject<T> holds no unmanaged
                    // resources and is reclaimed by the GC.
                    _activeExecutions.TryRemove(executionId, out _);
                    Interlocked.Decrement(ref _activeExecutionCount);
                });

            return executionStream;
        });
    }

    /// <summary>
    ///     Handles an incoming <see cref="SkillProgressMessage" /> from the Digital Twin,
    ///     translating it into a <see cref="SkillExecutionProgress" /> and pushing it to
    ///     the corresponding execution's observable subject.
    /// </summary>
    /// <param name="message">The progress message received over WebSocket.</param>
    internal void HandleProgressMessage(SkillProgressMessage message)
    {
        if (!_activeExecutions.TryGetValue(message.ExecutionId, out var execution))
        {
            _logger.LogProgressForUnknownExecution(message.ExecutionId);
            return;
        }

        UpdateLastSeen();

        var elapsed = (DateTime.UtcNow - execution.StartTimeUtc).TotalSeconds;
        var estimatedTotal = message.ProgressPercent > 0.001
            ? elapsed / message.ProgressPercent
            : _configuration.NominalDurationSeconds;

        var progress = new SkillExecutionProgress
        {
            ExecutionId = message.ExecutionId,
            SkillId = execution.Skill.Id,
            AgentId = Id,
            ActualStartTimeUtc = execution.StartTimeUtc,
            CurrentTimeIntoExecution = elapsed,
            EstimatedTotalDuration = estimatedTotal,
            StatusMessage = message.StatusMessage
        };

        execution.Subject.OnNext(progress);
    }

    /// <summary>
    ///     Handles an incoming <see cref="SkillCompletedMessage" /> from the Digital Twin.
    ///     Emits a final <see cref="SkillExecutionProgress" /> and completes the observable.
    /// </summary>
    /// <param name="message">The completion message received over WebSocket.</param>
    internal void HandleCompletedMessage(SkillCompletedMessage message)
    {
        if (!_activeExecutions.TryGetValue(message.ExecutionId, out var execution))
        {
            _logger.LogCompletionForUnknownExecution(message.ExecutionId);
            return;
        }

        UpdateLastSeen();

        var elapsed = (DateTime.UtcNow - execution.StartTimeUtc).TotalSeconds;

        if (message.Success)
        {
            var finalProgress = new SkillExecutionProgress
            {
                ExecutionId = message.ExecutionId,
                SkillId = execution.Skill.Id,
                AgentId = Id,
                ActualStartTimeUtc = execution.StartTimeUtc,
                CurrentTimeIntoExecution = elapsed,
                EstimatedTotalDuration = elapsed,
                StatusMessage = $"Completed after {elapsed:F1}s",
                CompletedSuccessfully = true
            };

            execution.Subject.OnNext(finalProgress);
            execution.Subject.OnCompleted();
        }
        else
        {
            Interlocked.Increment(ref _failedExecutions);

            var errorProgress = new SkillExecutionProgress
            {
                ExecutionId = message.ExecutionId,
                SkillId = execution.Skill.Id,
                AgentId = Id,
                ActualStartTimeUtc = execution.StartTimeUtc,
                CurrentTimeIntoExecution = elapsed,
                EstimatedTotalDuration = elapsed,
                StatusMessage = $"Failed: {message.ErrorMessage}",
                Error = new InvalidOperationException(message.ErrorMessage ?? "Execution failed")
            };

            execution.Subject.OnNext(errorProgress);
            execution.Subject.OnCompleted();
        }
    }

    /// <summary>
    ///     Handles an incoming <see cref="EstimateDurationResponse" /> from the Digital Twin,
    ///     completing the corresponding pending <see cref="TaskCompletionSource{T}" />.
    /// </summary>
    /// <param name="response">The duration estimation response received over WebSocket.</param>
    internal void HandleEstimateDurationResponse(EstimateDurationResponse response)
    {
        UpdateLastSeen();

        if (_pendingEstimates.TryRemove(response.QueryId, out var tcs))
            tcs.TrySetResult(response);
        else
            _logger.LogEstimateForUnknownQuery(response.QueryId);
    }

    /// <summary>
    ///     Handles an incoming <see cref="HealthStatusMessage" /> from the Digital Twin,
    ///     caching it for subsequent <see cref="GetHealthStatusAsync" /> calls.
    /// </summary>
    /// <param name="message">The health status message received over WebSocket.</param>
    internal void HandleHealthStatus(HealthStatusMessage message)
    {
        lock (_healthLock)
        {
            _lastHealthStatus = message;
            _lastSeenUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Handles an incoming <see cref="PongMessage" /> from the Digital Twin.
    ///     Updates the last-seen timestamp for health monitoring.
    /// </summary>
    /// <param name="message">The pong message received over WebSocket.</param>
    internal void HandlePong(PongMessage message)
    {
        UpdateLastSeen();
        var rtt = (DateTime.UtcNow - message.OriginalTimestampUtc).TotalMilliseconds;
        _logger.LogPongRtt(Name, rtt);
    }

    /// <summary>
    ///     Called when the WebSocket connection is lost. Cancels all active executions
    ///     with an error and cleans up internal state.
    /// </summary>
    internal void HandleDisconnection()
    {
        _logger.LogAgentDisconnected(Name);

        // Snapshot the active executions before iterating so a concurrent Finally callback
        // (e.g. a subscriber unsubscribing during disconnect) cannot mutate the dictionary
        // while this loop runs. Using ToArray() on a ConcurrentDictionary produces a safe
        // point-in-time copy; values still in flight are drained by their own Finally blocks.
        foreach (var kvp in _activeExecutions.ToArray())
        {
            var execution = kvp.Value;
            var elapsed = (DateTime.UtcNow - execution.StartTimeUtc).TotalSeconds;

            Interlocked.Increment(ref _failedExecutions);

            var errorProgress = new SkillExecutionProgress
            {
                ExecutionId = execution.ExecutionId,
                SkillId = execution.Skill.Id,
                AgentId = Id,
                ActualStartTimeUtc = execution.StartTimeUtc,
                CurrentTimeIntoExecution = elapsed,
                EstimatedTotalDuration = elapsed,
                StatusMessage = "Digital Twin disconnected",
                Error = new InvalidOperationException("WebSocket connection lost during execution")
            };

            // Defence-in-depth: an execution's own observable pipeline may already have
            // completed the Subject (via TakeUntil / finishSignal) in between the snapshot and
            // this call. Swallow ObjectDisposedException so disconnect cleanup cannot crash the
            // WebSocket reader thread — which previously aborted the test host with
            // "host process exited unexpectedly".
            try
            {
                execution.Subject.OnNext(errorProgress);
                execution.Subject.OnCompleted();
            }
            catch (ObjectDisposedException)
            {
                // Subject was finalized by its own Finally handler concurrently; nothing to do.
            }
        }

        _activeExecutions.Clear();

        // Cancel all pending estimates
        foreach (var kvp in _pendingEstimates) kvp.Value.TrySetCanceled();

        _pendingEstimates.Clear();
    }

    /// <summary>
    ///     Sends a typed message to the Digital Twin over WebSocket.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="messageType">The message type discriminator string.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal async Task SendMessageAsync<T>(string messageType, T payload,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogCannotSendNotConnected(messageType);
            return;
        }

        var json = DigitalTwinMessages.Serialize(messageType, payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await WebSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);

        _logger.LogMessageSent(messageType, Name);
    }

    /// <summary>
    ///     Sends the execute command and registers a cancellation callback to send
    ///     a <see cref="CancelSkillCommand" /> if the token fires.
    /// </summary>
    private async Task SendCommandAndHandleCancellationAsync(
        ExecuteSkillCommand command,
        ActiveExecution execution,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendMessageAsync(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command, cancellationToken);

            // Register cancellation to send CancelSkillCommand
            cancellationToken.Register(async void () =>
            {
                _logger.LogSendingCancelCommand(command.ExecutionId);
                try
                {
                    await SendMessageAsync(
                        DigitalTwinMessages.MessageTypes.CancelSkillCommand,
                        new CancelSkillCommand { ExecutionId = command.ExecutionId });
                }
                catch (Exception ex)
                {
                    _logger.LogCancelCommandFailed(ex, command.ExecutionId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogSendExecuteCommandFailed(ex, command.ExecutionId);

            Interlocked.Increment(ref _failedExecutions);

            var errorProgress = new SkillExecutionProgress
            {
                ExecutionId = execution.ExecutionId,
                SkillId = execution.Skill.Id,
                AgentId = Id,
                ActualStartTimeUtc = execution.StartTimeUtc,
                CurrentTimeIntoExecution = 0,
                EstimatedTotalDuration = 0,
                StatusMessage = $"Failed to send command: {ex.Message}",
                Error = ex
            };

            execution.Subject.OnNext(errorProgress);
            execution.Subject.OnCompleted();
        }
    }

    /// <summary>
    ///     Updates the last-seen UTC timestamp in a thread-safe manner.
    /// </summary>
    private void UpdateLastSeen()
    {
        lock (_healthLock)
        {
            _lastSeenUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Tracks the state of a single in-progress skill execution,
    ///     including its Rx subject for pushing progress updates.
    /// </summary>
    internal class ActiveExecution
    {
        /// <summary>Unique execution instance identifier.</summary>
        public required Guid ExecutionId { get; init; }

        /// <summary>The skill being executed.</summary>
        public required Skill Skill { get; init; }

        /// <summary>Subject for pushing progress updates to subscribers.</summary>
        public required Subject<SkillExecutionProgress> Subject { get; init; }

        /// <summary>UTC time when execution started.</summary>
        public required DateTime StartTimeUtc { get; init; }

        /// <summary>Cancellation token for this execution.</summary>
        public required CancellationToken CancellationToken { get; init; }
    }
}