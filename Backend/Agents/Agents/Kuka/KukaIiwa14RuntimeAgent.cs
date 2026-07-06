using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Agents.Kuka.Services;
using FHOOE.Freydis.Agents.Services.OpcUa;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Agents.Utilities;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace FHOOE.Freydis.Agents.Agents.Kuka;

/// <summary>
///     Runtime agent for KUKA iiwa 14 robot connected via OPC UA.
///     Provides skill execution capabilities, health monitoring, and real-time data access.
///     This agent does not support adaptive execution.
/// </summary>
public sealed class KukaIiwa14RuntimeAgent(
    Guid id,
    string name,
    IOpcUaConnectionManager connectionManager,
    KukaIiwa14DataReader dataReader,
    AgentExecutionStatistics executionStats,
    SkillPropertyExtractor propertyExtractor,
    IEnumerable<Skill> availableSkills,
    ILogger<KukaIiwa14RuntimeAgent> logger)
    : IRuntimeAgent, IAsyncDisposable, IDisposable
{
    private readonly List<Skill> _availableSkills =
        availableSkills.ToList() ?? throw new ArgumentNullException(nameof(availableSkills));

    private readonly IOpcUaConnectionManager _connectionManager =
        connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));

    private readonly KukaIiwa14DataReader _dataReader =
        dataReader ?? throw new ArgumentNullException(nameof(dataReader));

    private readonly AgentExecutionStatistics _executionStats =
        executionStats ?? throw new ArgumentNullException(nameof(executionStats));

    private readonly ILogger<KukaIiwa14RuntimeAgent>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly SkillPropertyExtractor _propertyExtractor =
        propertyExtractor ?? throw new ArgumentNullException(nameof(propertyExtractor));

    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private bool _disposed;

    private long _lastSeenUtcTicks = DateTime.UtcNow.Ticks;

    /// <summary>
    ///     Asynchronously disposes resources used by this agent, including the OPC UA connection.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _connectionManager.DisposeAsync();
        _disposed = true;
    }

    /// <summary>
    ///     Disposes resources used by this agent, including the OPC UA connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _connectionManager.Dispose();
        _disposed = true;
    }

    /// <summary>
    ///     Gets the unique identifier of this agent.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    ///     Gets the name of this agent.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Asynchronously retrieves the current health status of this agent.
    ///     Includes connection state, execution statistics, uptime, and availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current health status of the agent.</returns>
    public Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UpdateLastSeenUtc();

        var isConnected = _connectionManager.IsConnected;
        var uptime = DateTime.UtcNow - _startedUtc;
        var (activeExecutions, totalExecutions, failedExecutions) = _executionStats.GetSnapshot();
        var averageExecutionTime = totalExecutions > 0 ? uptime.TotalSeconds / totalExecutions : 0;

        var healthStatus = new AgentHealthStatus
        {
            AgentId = Id,
            AgentName = Name,
            IsHealthy = isConnected,
            IsAvailable = isConnected && activeExecutions < 1,
            ActiveExecutions = activeExecutions,
            TotalExecutionsCompleted = totalExecutions,
            FailedExecutions = failedExecutions,
            LastSeenUtc = GetLastSeenUtc(),
            StartedUtc = _startedUtc,
            AverageExecutionTimeSeconds = averageExecutionTime,
            StatusMessage = !isConnected
                ? "Disconnected from OPC UA server"
                : activeExecutions > 0
                    ? $"Executing {activeExecutions} skill(s)"
                    : "Idle - ready for work",
            ErrorMessage = !isConnected ? "OPC UA connection not established" : null,
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["SuccessRate"] = _executionStats.SuccessRate,
                ["UptimeHours"] = uptime.TotalHours,
                ["AvailableSkillsCount"] = _availableSkills.Count,
                ["MaxConcurrentExecutions"] = 1,
                ["OpcUaConnected"] = isConnected
            }
        };

        return Task.FromResult(healthStatus);
    }

    /// <summary>
    ///     Asynchronously retrieves a list of skills this agent is capable of executing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="Skill" /> entities this agent can execute.</returns>
    public Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UpdateLastSeenUtc();
        return Task.FromResult<IReadOnlyList<Skill>>(_availableSkills.AsReadOnly());
    }

    /// <summary>
    ///     Asynchronously provides an execution time estimate for a given skill.
    ///     Queries the OPC UA server for the estimated time to reach the skill's target pose.
    /// </summary>
    /// <param name="skill">The skill to estimate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A <see cref="SkillExecutionEstimate" /> for the skill, or null if estimation fails or the agent cannot execute
    ///     it.
    /// </returns>
    public Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(
        Skill skill,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(skill);
        UpdateLastSeenUtc();

        _logger.LogEstimatingExecution(Name, skill.Name, skill.Id);

        if (_availableSkills.All(s => s.Id != skill.Id))
        {
            _logger.LogAgentMissingSkill(Name, skill.Name, skill.Id);
            return Task.FromResult<SkillExecutionEstimate?>(null);
        }

        if (_connectionManager.Session == null || !_connectionManager.IsConnected)
        {
            _logger.LogCannotEstimateNotConnected();
            return Task.FromResult<SkillExecutionEstimate?>(null);
        }

        var pose = _propertyExtractor.ExtractPose(skill);
        var estimatedDuration = _dataReader.CallGetExecutionEstimateMethod(
            _connectionManager.Session!,
            pose.x, pose.y, pose.z,
            pose.alpha, pose.beta, pose.gamma);

        if (estimatedDuration == null) return Task.FromResult<SkillExecutionEstimate?>(null);

        _logger.LogExecutionEstimateReceived(Name, skill.Name, estimatedDuration.Value);

        var estimate = new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = Id,
            CanExecuteAdaptively = false,
            EstimatedNominalDuration = estimatedDuration.Value,
            MinAdaptiveDuration = null
        };

        return Task.FromResult<SkillExecutionEstimate?>(estimate);
    }

    /// <summary>
    ///     Asynchronously determines if the agent can execute the given skill adaptively.
    ///     KUKA iiwa 14 agent does not support adaptive execution.
    /// </summary>
    /// <param name="skill">The skill to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Always returns false for KUKA iiwa 14 agent.</returns>
    public Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(skill);
        UpdateLastSeenUtc();

        return Task.FromResult(false);
    }

    /// <summary>
    ///     Asynchronously executes a skill by calling the Python OPC UA server's ExecutePathAsync method.
    ///     Monitors execution progress via OPC UA ExecutionState variables.
    /// </summary>
    /// <param name="executionId">A unique identifier for this execution instance.</param>
    /// <param name="skillToExecute">The skill to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the execution.</param>
    /// <returns>An observable stream of <see cref="SkillExecutionProgress" /> updates.</returns>
    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid executionId,
        Skill skillToExecute,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(skillToExecute);

        return Observable.Create<SkillExecutionProgress>(async observer =>
        {
            try
            {
                _executionStats.IncrementActive();
                UpdateLastSeenUtc();

                if (_connectionManager.Session == null || !_connectionManager.IsConnected)
                {
                    _logger.LogCannotExecuteNotConnected();
                    observer.OnError(new InvalidOperationException("OPC UA session is not connected"));
                    return;
                }

                // Extract pose from skill
                var pose = _propertyExtractor.ExtractPose(skillToExecute);

                // Find the robot node and ExecutePathAsync method
                var robotNode = await FindRobotNodeAsync(_connectionManager.Session, cancellationToken);
                if (robotNode == null)
                {
                    _logger.LogRobotNodeNotFoundInObjects();
                    observer.OnError(new InvalidOperationException("Robot node not found"));
                    return;
                }

                var methodNode = await FindMethodNodeAsync(
                    _connectionManager.Session,
                    robotNode,
                    "ExecutePathAsync",
                    cancellationToken);

                if (methodNode == null)
                {
                    _logger.LogExecutePathAsyncMethodNotFound();
                    observer.OnError(new InvalidOperationException("ExecutePathAsync method not found"));
                    return;
                }

                _logger.LogExecutingSkillViaPose(
                    skillToExecute.Name, skillToExecute.Id, pose.x, pose.y, pose.z, pose.alpha, pose.beta, pose.gamma);

                // Capture start time before calling execution
                var actualStartTimeUtc = DateTime.UtcNow;

                // Call ExecutePathAsync method
                var inputArguments = new object[] { pose.x, pose.y, pose.z, pose.alpha, pose.beta, pose.gamma };
                var outputArguments =
                    await _connectionManager.Session.CallAsync(robotNode, methodNode, cancellationToken,
                        inputArguments);

                if (outputArguments == null || outputArguments.Count == 0)
                {
                    _logger.LogExecutePathAsyncNoOutput();
                    observer.OnError(new InvalidOperationException("Method call failed"));
                    return;
                }

                var success = Convert.ToBoolean(outputArguments[0], CultureInfo.InvariantCulture);
                if (!success)
                {
                    _logger.LogExecutePathAsyncAlreadyInProgress();
                    observer.OnError(new InvalidOperationException("Execution already in progress on robot"));
                    return;
                }

                // Monitor execution progress by reading ExecutionState variables
                await MonitorExecutionProgressAsync(
                    _connectionManager.Session,
                    executionId,
                    skillToExecute,
                    actualStartTimeUtc,
                    observer,
                    cancellationToken);

                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                // Re-throw fatal exceptions that indicate process-level failures
                if (ex is OutOfMemoryException || ex is AccessViolationException) throw;

                _logger.LogSkillExecutionError(ex, skillToExecute.Name, skillToExecute.Id);
                _executionStats.IncrementFailed();
                observer.OnError(ex);
            }
            finally
            {
                _executionStats.DecrementActive();
            }
        });
    }

    /// <summary>
    ///     Asynchronously executes a skill adaptively.
    ///     Not supported by KUKA iiwa 14 agent.
    /// </summary>
    /// <param name="executionId">A unique identifier for this execution instance.</param>
    /// <param name="skillToExecute">The skill to execute.</param>
    /// <param name="initialTargetDuration">The initial target duration in seconds.</param>
    /// <param name="plannedFinishTimes">An observable stream of planned finish times.</param>
    /// <param name="finishSignal">An observable that signals when to complete execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An observable that immediately errors with <see cref="NotSupportedException" />.</returns>
    public IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId,
        Skill skillToExecute,
        double initialTargetDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        CancellationToken cancellationToken)
    {
        return Observable.Throw<SkillExecutionProgress>(new NotSupportedException(
            "ExecuteSkillAdaptivelyAsync not supported for KUKA iiwa 14 agent. " +
            "This robot does not support adaptive execution."));
    }

    /// <summary>
    ///     Asynchronously connects to the OPC UA server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _connectionManager.ConnectAsync(cancellationToken);
        UpdateLastSeenUtc();
    }

    /// <summary>
    ///     Asynchronously disconnects from the OPC UA server.
    /// </summary>
    /// <returns>A task representing the asynchronous disconnection operation.</returns>
    public async Task DisconnectAsync()
    {
        await _connectionManager.DisconnectAsync();
    }

    /// <summary>
    ///     Asynchronously reads all 7 joint angle values from the robot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of 7 joint values in radians, or null if the connection is not established or reading fails.</returns>
    public async Task<double[]?> ReadJointValuesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connectionManager is { Session: not null, IsConnected: true })
            return await _dataReader.ReadJointValuesAsync(_connectionManager.Session, cancellationToken);

        _logger.LogCannotReadJointValuesNotConnected();
        return null;
    }

    /// <summary>
    ///     Asynchronously reads all 7 joint torque values from the robot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of 7 torque values in Nm, or null if the connection is not established or reading fails.</returns>
    public async Task<double[]?> ReadTorqueValuesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connectionManager is { Session: not null, IsConnected: true })
            return await _dataReader.ReadTorqueValuesAsync(_connectionManager.Session, cancellationToken);

        _logger.LogCannotReadTorqueValuesNotConnected();
        return null;
    }

    /// <summary>
    ///     Finds the robot node by browsing the Objects folder.
    /// </summary>
    private async Task<NodeId?> FindRobotNodeAsync(ISession session, CancellationToken cancellationToken)
    {
        const string robotNodePath = "KUKA iiwa 14";

        try
        {
            _logger.LogBrowsingForRobotNode(robotNodePath);

            var nodeToBrowse = new BrowseDescription
            {
                NodeId = ObjectIds.ObjectsFolder,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Object,
                ResultMask = (uint)BrowseResultMask.All
            };

            var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };

            BrowseResultCollection? results = null;
            await Task.Run(() => session.Browse(null, null, 0, nodesToBrowse, out results, out _), cancellationToken);

            if (results == null || results.Count == 0)
            {
                _logger.LogBrowseNoResultsForObjectsFolder();
                return null;
            }

            var browseResult = results[0];
            if (StatusCode.IsBad(browseResult.StatusCode))
            {
                _logger.LogBrowseFailedWithStatus(browseResult.StatusCode);
                return null;
            }

            foreach (var reference in browseResult.References)
                if (reference.BrowseName.Name == robotNodePath)
                {
                    var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                    _logger.LogFoundRobotNode(robotNodePath, nodeId);
                    return nodeId;
                }

            _logger.LogRobotNodeNotFound(robotNodePath);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogBrowseForRobotNodeCancelled();
            return null;
        }
        catch (ServiceResultException ex)
        {
            _logger.LogOpcUaServiceErrorBrowsingRobotNode(ex);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedErrorBrowsingRobotNode(ex);
            return null;
        }
    }

    /// <summary>
    ///     Finds a method node by browsing the children of a parent node.
    /// </summary>
    private async Task<NodeId?> FindMethodNodeAsync(
        ISession session,
        NodeId parentNodeId,
        string methodName,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogBrowsingForMethod(parentNodeId, methodName);

            var nodeToBrowse = new BrowseDescription
            {
                NodeId = parentNodeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Method,
                ResultMask = (uint)BrowseResultMask.All
            };

            var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };

            BrowseResultCollection? results = null;
            await Task.Run(() => session.Browse(null, null, 0, nodesToBrowse, out results, out _), cancellationToken);

            if (results == null || results.Count == 0)
            {
                _logger.LogBrowseNoResultsForNode(parentNodeId);
                return null;
            }

            var browseResult = results[0];
            if (StatusCode.IsBad(browseResult.StatusCode))
            {
                _logger.LogBrowseFailedWithStatus(browseResult.StatusCode);
                return null;
            }

            foreach (var reference in browseResult.References)
                if (reference.BrowseName.Name == methodName)
                {
                    var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                    _logger.LogFoundMethod(methodName, nodeId);
                    return nodeId;
                }

            _logger.LogMethodNotFound(methodName);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogBrowseForMethodCancelled(methodName);
            return null;
        }
        catch (ServiceResultException ex)
        {
            _logger.LogOpcUaServiceErrorBrowsingMethod(ex, methodName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedErrorBrowsingMethod(ex, methodName);
            return null;
        }
    }

    /// <summary>
    ///     Monitors execution progress by polling ExecutionState OPC UA variables.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="skill">The skill being executed.</param>
    /// <param name="actualStartTimeUtc">The UTC time when execution started.</param>
    /// <param name="observer">The observer to receive progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task MonitorExecutionProgressAsync(
        ISession session,
        Guid executionId,
        Skill skill,
        DateTime actualStartTimeUtc,
        IObserver<SkillExecutionProgress> observer,
        CancellationToken cancellationToken)
    {
        const int pollingIntervalMs = 100; // Poll every 100 ms
        const string statusNodePath = "KUKA iiwa 14.ExecutionState.Status";
        const string progressNodePath = "KUKA iiwa 14.ExecutionState.ProgressPercent";
        const string elapsedNodePath = "KUKA iiwa 14.ExecutionState.ElapsedTime";
        const string remainingNodePath = "KUKA iiwa 14.ExecutionState.RemainingTime";

        try
        {
            // Create node IDs for reading execution state
            var statusNodeId = new NodeId($"ns=2;s={statusNodePath}");
            var progressNodeId = new NodeId($"ns=2;s={progressNodePath}");
            var elapsedNodeId = new NodeId($"ns=2;s={elapsedNodePath}");
            var remainingNodeId = new NodeId($"ns=2;s={remainingNodePath}");

            var nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId { NodeId = statusNodeId, AttributeId = Attributes.Value },
                new ReadValueId { NodeId = progressNodeId, AttributeId = Attributes.Value },
                new ReadValueId { NodeId = elapsedNodeId, AttributeId = Attributes.Value },
                new ReadValueId { NodeId = remainingNodeId, AttributeId = Attributes.Value }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                DataValueCollection? results = null;

                await Task.Run(() =>
                        session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out results, out _),
                    cancellationToken);

                if (results is { Count: 4 })
                {
                    var status = StatusCode.IsGood(results[0].StatusCode) ? results[0].Value.ToString() : "unknown";
                    var progressPercent = StatusCode.IsGood(results[1].StatusCode)
                        ? Convert.ToDouble(results[1].Value, CultureInfo.InvariantCulture)
                        : 0.0;
                    var elapsedTime = StatusCode.IsGood(results[2].StatusCode)
                        ? Convert.ToDouble(results[2].Value, CultureInfo.InvariantCulture)
                        : 0.0;
                    var remainingTime = StatusCode.IsGood(results[3].StatusCode)
                        ? Convert.ToDouble(results[3].Value, CultureInfo.InvariantCulture)
                        : 0.0;

                    if (!StatusCode.IsGood(results[0].StatusCode) || !StatusCode.IsGood(results[1].StatusCode)
                                                                  || !StatusCode.IsGood(results[2].StatusCode) ||
                                                                  !StatusCode.IsGood(results[3].StatusCode))
                        _logger.LogExecutionStateReadFailed(executionId, results[0].StatusCode, results[1].StatusCode);

                    // Determine completion status and error
                    var isCompleted = status == "completed";
                    var isError = status == "error" || status == "cancelled";
                    Exception? error = isError
                        ? new InvalidOperationException($"Execution ended with status: {status}")
                        : null;

                    // Publish progress update with the correct SkillExecutionProgress structure
                    var progress = new SkillExecutionProgress
                    {
                        ExecutionId = executionId,
                        SkillId = skill.Id,
                        AgentId = Id,
                        ActualStartTimeUtc = actualStartTimeUtc,
                        CurrentTimeIntoExecution = elapsedTime,
                        EstimatedTotalDuration = elapsedTime + remainingTime,
                        StatusMessage = $"{status}: {progressPercent:F1}% complete",
                        CompletedSuccessfully = isCompleted,
                        Error = error,
                        MinAchievableDuration = null
                    };

                    observer.OnNext(progress);

                    // Check if execution completed
                    if (status == "completed" || status == "error" || status == "cancelled")
                    {
                        if (status == "completed")
                        {
                            _executionStats.IncrementTotal();
                            _logger.LogSkillExecutionCompleted(skill.Name, elapsedTime);
                        }
                        else
                        {
                            _executionStats.IncrementFailed();
                            _logger.LogSkillExecutionEndedWithStatus(skill.Name, status);
                        }

                        break;
                    }
                }

                await Task.Delay(pollingIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogExecutionMonitoringCancelled(skill.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogExecutionMonitoringError(ex, skill.Name);
            throw;
        }
    }

    /// <summary>
    ///     Gets the last seen timestamp using lock-free atomic read.
    /// </summary>
    /// <returns>The last time this agent was accessed.</returns>
    private DateTime GetLastSeenUtc()
    {
        return new DateTime(Interlocked.Read(ref _lastSeenUtcTicks), DateTimeKind.Utc);
    }

    /// <summary>
    ///     Updates the last seen timestamp using lock-free atomic write.
    /// </summary>
    private void UpdateLastSeenUtc()
    {
        Interlocked.Exchange(ref _lastSeenUtcTicks, DateTime.UtcNow.Ticks);
    }
}