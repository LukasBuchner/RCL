using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;
using AgentProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Integration;

/// <summary>
///     Integration tests for adaptive skill execution with Start-to-Start and Finish-to-Finish dependencies.
///     Tests the complete flow from trigger to execution to cancellation with real implementations.
/// </summary>
public class AdaptiveSkillExecutionIntegrationTests(ITestOutputHelper output)
{
    private const int ExpectedSkillCount = 2;
    private readonly ILogger<DependencyGraphAnalyzer> _analyzerLogger = new TestLogger<DependencyGraphAnalyzer>(output);

    private readonly ILogger<SkillExecutionCoordinator> _coordinatorLogger =
        new TestLogger<SkillExecutionCoordinator>(output);

    private readonly ILogger<ExecutionTriggerService> _triggerLogger = new TestLogger<ExecutionTriggerService>(output);

    /// <summary>
    ///     Tests the complete adaptive execution flow WITH NODE PUBLISHING:
    ///     - Skill B (adaptive) starts first (no incoming start dependencies)
    ///     - Skill A starts when B starts (SS dependency: B->A)
    ///     - Skill B finishes when A finishes (FF dependency: A->B)
    ///     - Verifies nodes published to frontend have IsExecuting=true and Progress set correctly
    /// </summary>
    [Fact]
    public async Task AdaptiveExecution_PublishesNodesWithProgressAndIsExecuting()
    {
        // Arrange - Create test procedure with two skills and dependencies
        var (nodes, edges) = CreateTestProcedure();
        var skillA = nodes[0] as SkillExecutionNode;
        var skillB = nodes[1] as SkillExecutionNode;

        Assert.NotNull(skillA);
        Assert.NotNull(skillB);

        // Assign execution IDs (normally done by ExecutionIdAssigner)
        var executionIdA = Guid.NewGuid();
        var executionIdB = Guid.NewGuid();

        nodes[0] = skillA with
        {
            SkillExecutionTask = skillA.SkillExecutionTask with { ExecutionId = executionIdA }
        };
        nodes[1] = skillB with
        {
            SkillExecutionTask = skillB.SkillExecutionTask with { ExecutionId = executionIdB }
        };

        skillA = nodes[0] as SkillExecutionNode;
        skillB = nodes[1] as SkillExecutionNode;

        // Create mock agents that simulate execution with progress updates
        var agentA = CreateMockAgent(skillA!.SkillExecutionTask.AgentId, "AgentA", executionIdA);
        var agentB = CreateMockAgent(skillB!.SkillExecutionTask.AgentId, "AgentB", executionIdB);

        // Setup real services with minimal mocking
        var mockEventBusLogger = new TestLogger<SkillExecutionEventBus>(output);
        var eventBus = new SkillExecutionEventBus(mockEventBusLogger);
        var stateManager = new SkillExecutionStateManager(new TestLogger<SkillExecutionStateManager>(output));

        // Create mock agent provider that returns our mock agents
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentA.Id)).Returns(agentA);
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentB.Id)).Returns(agentB);

        // Create mock typedProperty binding service
        var mockPropertyBindingService = new Mock<IPropertyBindingService>();

        // Create mock scene entity resolver (returns skill unchanged)
        var mockSceneEntityResolver = new Mock<ISceneEntityResolver>();
        mockSceneEntityResolver.Setup(r => r.RefreshSceneEntityProperties(It.IsAny<Skill>())).Returns<Skill>(s => s);

        var coordinator = new SkillExecutionCoordinator(
            eventBus,
            mockAgentProvider.Object,
            mockPropertyBindingService.Object,
            mockSceneEntityResolver.Object,
            TimeProvider.System,
            _coordinatorLogger,
            NullLogger<PipelineEvents>.Instance);

        // Setup NodeHierarchyProcessor with dependencies
        var mockRelationshipMapper = new Mock<INodeRelationshipMapper>();
        mockRelationshipMapper
            .Setup(m => m.BuildParentToChildrenMapping(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(new Dictionary<Guid, IReadOnlyList<Node>>());
        mockRelationshipMapper
            .Setup(m => m.BuildTaskToSkillMapping(It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns(new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>());
        mockRelationshipMapper
            .Setup(m => m.BuildSkillToTaskMapping(It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns(new Dictionary<Guid, TaskNode>());

        var mockHierarchyValidator = new Mock<IHierarchyValidator>();
        mockHierarchyValidator
            .Setup(v => v.ValidateConsistency(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, TaskNode>>()))
            .Returns(new HierarchyValidationResult(true, new List<string>(), new List<string>()));

        var mockHierarchyLogger = new TestLogger<NodeHierarchyProcessor>(output);
        var hierarchyProcessor = new NodeHierarchyProcessor(
            mockRelationshipMapper.Object,
            mockHierarchyValidator.Object,
            mockHierarchyLogger);

        var analyzer = new DependencyGraphAnalyzer(
            hierarchyProcessor,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            _analyzerLogger);
        var dependencyGraph = analyzer.AnalyzeDependencies(nodes, edges);

        var mockRouterEvaluationService = new Mock<IRouterEvaluationService>();

        var skillTriggerHandler = new SkillTriggerHandler(
            eventBus,
            coordinator,
            mockAgentProvider.Object,
            NullLogger<SkillTriggerHandler>.Instance,
            NullLogger<PipelineEvents>.Instance);

        var branchNavigator = new RouterBranchNavigator();
        var routerTriggerHandler = new RouterTriggerHandler(
            eventBus,
            mockRouterEvaluationService.Object,
            branchNavigator,
            NullLogger<RouterTriggerHandler>.Instance);

        var triggerService = new ExecutionTriggerService(
            eventBus,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            _triggerLogger,
            NullLogger<PipelineEvents>.Instance);

        // Track PUBLISHED NODES for verification
        // ConcurrentBag provides thread-safety as defense-in-depth
        var publishedNodes = new ConcurrentBag<(DateTimeOffset Timestamp, IReadOnlyList<Node> Nodes)>();
        var completionTaskSource = new TaskCompletionSource<bool>();
        var finishEventCount = 0;

        // Initialize state manager with nodes and agent assignments
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillA.Id, agentA },
            { skillB.Id, agentB }
        };
        stateManager.Initialize(nodes, agentAssignments);

        // Subscribe to events and immediately update state and publish nodes
        // This ensures we capture every state transition without relying on periodic sampling
        // Use Synchronize() to serialize event processing and prevent concurrent access to shared state
        var eventSubscription = eventBus.AllEvents
            .Synchronize()
            .Subscribe(evt =>
            {
                output.WriteLine($"[EVENT] {evt.EventType} for skill {evt.SkillId} at {evt.Timestamp:HH:mm:ss.fff}");

                var node = nodes.OfType<SkillExecutionNode>().FirstOrDefault(n => n.Id == evt.SkillId);
                if (node != null)
                {
                    var agent = agentAssignments.GetValueOrDefault(node.Id);

                    // Simulate state transitions
                    switch (evt.EventType)
                    {
                        case ExecutionEventType.Start when agent != null:
                            stateManager.UpdateState(node.Id, state =>
                            {
                                state.ExecutionStatus = ExecutionStatus.Running;
                                state.StartedAt = evt.Timestamp;
                                state.AssignedAgent = agent;
                            });
                            break;
                        case ExecutionEventType.Finish:
                            stateManager.UpdateState(node.Id, state =>
                            {
                                state.ExecutionStatus = ExecutionStatus.Completed;
                                state.CompletedAt = evt.Timestamp;
                            });
                            break;
                        case ExecutionEventType.Progress when evt.ProgressData != null:
                            stateManager.UpdateState(node.Id, state =>
                            {
                                state.LastProgressPercentage = evt.ProgressPercentage * 100.0;
                                state.LastProgress = evt.ProgressData;
                            });
                            break;
                    }

                    // Immediately publish updated nodes after state change
                    var updatedNodes = nodes.Select(n =>
                    {
                        if (n is not SkillExecutionNode sn) return n;

                        var state = stateManager.GetState(sn.Id);
                        if (state == null) return n;

                        // Apply execution state to node (simulating what NodeTimingMapper does)
                        var isExecuting = state.ExecutionStatus == ExecutionStatus.Running;
                        var progress = state.ExecutionStatus == ExecutionStatus.Completed
                            ? 100.0
                            : state.LastProgressPercentage;

                        var updatedTask = sn.SkillExecutionTask with
                        {
                            IsExecuting = isExecuting,
                            Progress = progress
                        };

                        return sn with { SkillExecutionTask = updatedTask };
                    }).ToList().AsReadOnly();

                    publishedNodes.Add((DateTimeOffset.UtcNow, updatedNodes));
                }

                // Track finish events to know when execution is complete
                if (evt.EventType == ExecutionEventType.Finish)
                    // Interlocked.Increment provides atomic increment (defense-in-depth, though Synchronize() already serializes access)
                    if (Interlocked.Increment(ref finishEventCount) == ExpectedSkillCount)
                        completionTaskSource.TrySetResult(true);
            });

        // Act - Start execution
        output.WriteLine("\n=== STARTING EXECUTION ===\n");
        triggerService.Start(dependencyGraph, nodes, null);

        // Wait for execution to complete with timeout
        var completionTask = completionTaskSource.Task;
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        var completedTask = await Task.WhenAny(completionTask, timeoutTask);

        if (completedTask == timeoutTask)
            output.WriteLine("⚠ Timeout waiting for execution to complete");
        else
            output.WriteLine("✓ Execution completed successfully");

        // Give a small additional delay to ensure all events are processed
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Cleanup
        eventSubscription.Dispose();
        triggerService.StopMonitoring();

        // Assert - Verify PUBLISHED NODES have IsExecuting and Progress set
        output.WriteLine("\n=== VERIFYING PUBLISHED NODES ===\n");

        var nodeAId = nodes[0].Id;
        var nodeBId = nodes[1].Id;

        // 1. Verify we captured published node updates
        Assert.NotEmpty(publishedNodes);
        output.WriteLine($"✓ Captured {publishedNodes.Count} node publication events");

        // 2. Find published nodes where Skill A is executing (IsExecuting = true)
        var nodesWithAExecuting = publishedNodes
            .Where(pn => pn.Nodes.OfType<SkillExecutionNode>()
                .Any(n => n.Id == nodeAId && n.SkillExecutionTask.IsExecuting == true))
            .ToList();

        Assert.NotEmpty(nodesWithAExecuting);
        output.WriteLine($"✓ Found {nodesWithAExecuting.Count} publications where Skill A has IsExecuting=true");

        // 3. Find published nodes where Skill A has progress
        var nodesWithAProgress = publishedNodes
            .Where(pn => pn.Nodes.OfType<SkillExecutionNode>()
                .Any(n => n.Id == nodeAId && n.SkillExecutionTask.Progress.HasValue))
            .ToList();

        Assert.NotEmpty(nodesWithAProgress);
        output.WriteLine($"✓ Found {nodesWithAProgress.Count} publications where Skill A has Progress set");

        // 4. Verify Skill A completed (Progress = 100)
        var nodesWithACompleted = publishedNodes
            .Where(pn => pn.Nodes.OfType<SkillExecutionNode>()
                .Any(n => n.Id == nodeAId && n.SkillExecutionTask.Progress == 100.0))
            .ToList();

        Assert.NotEmpty(nodesWithACompleted);
        output.WriteLine($"✓ Found {nodesWithACompleted.Count} publications where Skill A has Progress=100");

        // 5. Verify Skill B was executing
        var nodesWithBExecuting = publishedNodes
            .Where(pn => pn.Nodes.OfType<SkillExecutionNode>()
                .Any(n => n.Id == nodeBId && n.SkillExecutionTask.IsExecuting == true))
            .ToList();

        Assert.NotEmpty(nodesWithBExecuting);
        output.WriteLine($"✓ Found {nodesWithBExecuting.Count} publications where Skill B has IsExecuting=true");

        // 6. Print sample of published node data
        output.WriteLine("\n=== SAMPLE PUBLISHED NODE DATA ===");
        var sampleNode = publishedNodes
            .SelectMany(pn => pn.Nodes.OfType<SkillExecutionNode>())
            .FirstOrDefault(n => n.Id == nodeAId && n.SkillExecutionTask.IsExecuting == true);

        if (sampleNode != null)
        {
            output.WriteLine("Skill A (while executing):");
            output.WriteLine($"  IsExecuting: {sampleNode.SkillExecutionTask.IsExecuting}");
            output.WriteLine($"  Progress: {sampleNode.SkillExecutionTask.Progress}");
            output.WriteLine($"  StartTime: {sampleNode.SkillExecutionTask.StartTime}");
            output.WriteLine($"  Duration: {sampleNode.SkillExecutionTask.Duration}");
        }

        output.WriteLine("\n=== TEST PASSED ===\n");
    }

    /// <summary>
    ///     Creates a test procedure with two skills:
    ///     - Skill A: Fixed duration skill
    ///     - Skill B: Adaptive skill (finishes when A finishes)
    ///     Dependencies:
    ///     - B Start -> A Start (SS)
    ///     - A Finish -> B Finish (FF)
    /// </summary>
    private (List<Node> nodes, List<DependencyEdge> edges) CreateTestProcedure()
    {
        var agentAId = Guid.NewGuid();
        var agentBId = Guid.NewGuid();

        var skillA = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "FixedDurationSkill",
            Description = "Skill A with fixed duration",
            Properties = []
        };

        var skillB = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "AdaptiveSkill",
            Description = "Skill B that adapts to A's completion",
            Properties = []
        };

        var nodeA = new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Task A",
                StartTime = 0,
                Duration = 1.0, // 1 second fixed duration
                Skill = skillA,
                AgentId = agentAId
            },
            ProcedureId = default
        };

        var nodeB = new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 100
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Task B",
                StartTime = 0,
                Duration = 5.0, // 5 seconds max (will be cut short by A's finish)
                Skill = skillB,
                AgentId = agentBId
            },
            ProcedureId = default
        };

        var edges = new List<DependencyEdge>
        {
            // B Start -> A Start (Start-to-Start)
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = nodeB.Id,
                TargetId = nodeA.Id,
                SourceHandle = "left", // Start
                TargetHandle = "left",
                ProcedureId = default // Start
            },
            // A Finish -> B Finish (Finish-to-Finish)
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = nodeA.Id,
                TargetId = nodeB.Id,
                SourceHandle = "right", // Finish
                TargetHandle = "right",
                ProcedureId = default // Finish
            }
        };

        return ([nodeA, nodeB], edges);
    }

    /// <summary>
    ///     Creates a mock agent that simulates real execution behavior with progress updates.
    /// </summary>
    private IRuntimeAgent CreateMockAgent(Guid agentId, string agentName, Guid expectedExecutionId)
    {
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns(agentName);

        // Simulate skill execution with progress updates (non-adaptive)
        mockAgent.Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, CancellationToken>((_, skill, ct) =>
            {
                return Observable.Create<AgentProgress>(async observer =>
                {
                    try
                    {
                        // Simulate execution with progress updates
                        var startTime = DateTime.UtcNow;
                        var duration = TimeSpan.FromMilliseconds(800); // ~800ms execution
                        var updateInterval = TimeSpan.FromMilliseconds(100);
                        var elapsed = TimeSpan.Zero;

                        while (elapsed < duration && !ct.IsCancellationRequested)
                        {
                            await Task.Delay(updateInterval, ct);
                            elapsed += updateInterval;

                            var progressPct = elapsed.TotalMilliseconds / duration.TotalMilliseconds;

                            observer.OnNext(new AgentProgress
                            {
                                ExecutionId = expectedExecutionId,
                                SkillId = skill.Id,
                                AgentId = agentId,
                                ActualStartTimeUtc = startTime,
                                CurrentTimeIntoExecution = elapsed.TotalSeconds,
                                EstimatedTotalDuration = duration.TotalSeconds,
                                StatusMessage = $"{agentName} executing at {progressPct:P0}"
                            });

                            output.WriteLine($"[{agentName}] Progress: {progressPct:P0}");
                        }

                        if (ct.IsCancellationRequested)
                        {
                            output.WriteLine($"[{agentName}] Execution cancelled");
                            observer.OnCompleted();
                        }
                        else
                        {
                            observer.OnNext(new AgentProgress
                            {
                                ExecutionId = expectedExecutionId,
                                SkillId = skill.Id,
                                AgentId = agentId,
                                ActualStartTimeUtc = startTime,
                                CurrentTimeIntoExecution = duration.TotalSeconds,
                                EstimatedTotalDuration = duration.TotalSeconds,
                                StatusMessage = $"{agentName} completed",
                                CompletedSuccessfully = true
                            });
                            output.WriteLine($"[{agentName}] Execution completed");
                            observer.OnCompleted();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        output.WriteLine($"[{agentName}] Execution cancelled (exception)");
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        output.WriteLine($"[{agentName}] Execution failed: {ex.Message}");
                        observer.OnError(ex);
                    }
                });
            });

        // Simulate adaptive skill execution with progress updates
        mockAgent.Setup(a => a.ExecuteSkillAdaptivelyAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<double>(),
                It.IsAny<IObservable<double>>(),
                It.IsAny<IObservable<Unit>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, double, IObservable<double>, IObservable<Unit>, CancellationToken>((_, skill,
                initialDuration, _, _, ct) =>
            {
                return Observable.Create<AgentProgress>(async observer =>
                {
                    try
                    {
                        // Simulate execution that can be cancelled externally
                        var startTime = DateTime.UtcNow;
                        var duration = TimeSpan.FromSeconds(initialDuration); // Use initial duration
                        var updateInterval = TimeSpan.FromMilliseconds(100);
                        var elapsed = TimeSpan.Zero;

                        while (!ct.IsCancellationRequested)
                        {
                            await Task.Delay(updateInterval, ct);
                            elapsed += updateInterval;

                            var progressPct = elapsed.TotalMilliseconds / duration.TotalMilliseconds;

                            observer.OnNext(new AgentProgress
                            {
                                ExecutionId = expectedExecutionId,
                                SkillId = skill.Id,
                                AgentId = agentId,
                                ActualStartTimeUtc = startTime,
                                CurrentTimeIntoExecution = elapsed.TotalSeconds,
                                EstimatedTotalDuration = duration.TotalSeconds,
                                StatusMessage = $"{agentName} executing adaptively at {progressPct:P0}",
                                MinAchievableDuration = initialDuration * 0.7 // Min 70% of nominal
                            });

                            output.WriteLine($"[{agentName}] Adaptive Progress: {progressPct:P0}");
                        }

                        output.WriteLine($"[{agentName}] Adaptive execution cancelled");
                        observer.OnCompleted();
                    }
                    catch (OperationCanceledException)
                    {
                        output.WriteLine($"[{agentName}] Adaptive execution cancelled (exception)");
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        output.WriteLine($"[{agentName}] Adaptive execution failed: {ex.Message}");
                        observer.OnError(ex);
                    }
                });
            });

        return mockAgent.Object;
    }
}

/// <summary>
///     Test logger that outputs to xUnit test output.
/// </summary>
internal class TestLogger<T>(ITestOutputHelper output) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var prefix = logLevel switch
        {
            LogLevel.Trace => "[TRC]",
            LogLevel.Debug => "[DBG]",
            LogLevel.Information => "[INF]",
            LogLevel.Warning => "[WRN]",
            LogLevel.Error => "[ERR]",
            LogLevel.Critical => "[CRT]",
            _ => "[LOG]"
        };

        output.WriteLine($"{prefix} {typeof(T).Name}: {message}");

        if (exception != null) output.WriteLine($"      Exception: {exception}");
    }
}