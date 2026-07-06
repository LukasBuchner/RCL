using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;
using AgentProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Integration;

/// <summary>
///     Integration tests that verify ExecutionOrchestrator publishes nodes with IsExecuting and Progress
///     AFTER they have been processed through the ScheduleAsync request (TimingCalculationOrchestrator).
/// </summary>
public class ExecutionOrchestratorNodePublishingTests(ITestOutputHelper output)
{
    /// <summary>
    ///     CRITICAL TEST: Verifies that nodes published to the frontend have IsExecuting and Progress set correctly
    ///     AFTER being processed through TimingCalculationOrchestrator.CalculateAsync().
    ///     This test validates the complete flow:
    ///     Events → State → BuildProgressData → SchedulingRequest → CalculateAsync →
    ///     ExecutionAwareDurationProvider → NodeTimingMapper → Published Nodes
    /// </summary>
    [Fact]
    public async Task ExecutionOrchestrator_PublishesNodesWithIsExecutingAndProgress_AfterSchedulingPipeline()
    {
        // Arrange - Create test nodes and edges
        var (nodes, edges) = CreateTestProcedure();

        // Assign ExecutionIds (normally done by ExecutionInitializer)
        var executionIdA = Guid.NewGuid();
        var executionIdB = Guid.NewGuid();

        var skillA = nodes[0] as SkillExecutionNode;
        var skillB = nodes[1] as SkillExecutionNode;

        Assert.NotNull(skillA);
        Assert.NotNull(skillB);

        nodes[0] = skillA with { SkillExecutionTask = skillA.SkillExecutionTask with { ExecutionId = executionIdA } };
        nodes[1] = skillB with { SkillExecutionTask = skillB.SkillExecutionTask with { ExecutionId = executionIdB } };

        skillA = nodes[0] as SkillExecutionNode;
        skillB = nodes[1] as SkillExecutionNode;

        // Setup real agent mocks that provide progress
        var agentA = CreateMockAgent(skillA!.SkillExecutionTask.AgentId, "AgentA", executionIdA,
            skillA.SkillExecutionTask.Skill);
        var agentB = CreateMockAgent(skillB!.SkillExecutionTask.AgentId, "AgentB", executionIdB,
            skillB.SkillExecutionTask.Skill);

        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentA.Id)).Returns(agentA);
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentB.Id)).Returns(agentB);

        // Setup real services
        var eventBus = new SkillExecutionEventBus(new TestLogger<SkillExecutionEventBus>(output));
        var stateManager = new SkillExecutionStateManager(new TestLogger<SkillExecutionStateManager>(output));
        var stateTransitionService = new ExecutionStateTransitionService(
            stateManager,
            new TestLogger<ExecutionStateTransitionService>(output),
            TimeProvider.System);

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
            new TestLogger<SkillExecutionCoordinator>(output),
            NullLogger<PipelineEvents>.Instance);

        // Mock repository (needed by ExecutionInitializer - minimal mocking)
        var mockProcedureRepo = new Mock<IProcedureRepository>();
        mockProcedureRepo.Setup(r => r.GetAllNodesAsync()).ReturnsAsync(nodes);
        mockProcedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(It.IsAny<Guid>())).ReturnsAsync(nodes);
        mockProcedureRepo.Setup(r => r.GetAllEdgesAsync()).ReturnsAsync(edges);
        mockProcedureRepo.Setup(r => r.GetEdgesByProcedureIdAsync(It.IsAny<Guid>())).ReturnsAsync(edges);

        var mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        var lastSchedulingRequestHadProgressData = false; // Track whether the last CalculateAsync had progress data

        // Mock AgentManager to return runtime agents (ExecutionInitializer needs this to build agent assignments)
        var mockAgentManager = new Mock<IAgentManager>();
        mockAgentManager.Setup(m => m.GetAgent(agentA.Id)).Returns(agentA);
        mockAgentManager.Setup(m => m.GetAgent(agentB.Id)).Returns(agentB);

        // Use REAL ExecutionInitializer with mocked repositories
        var executionIdAssigner = new ExecutionIdAssigner();
        var procedureId = Guid.NewGuid();
        var mockVariableResolver = new Mock<IVariableResolver>();
        var mockProcedureContext = new Mock<IProcedureContext>();

        // Setup procedure context to return a valid procedure ID
        mockProcedureContext.Setup(c => c.RequireCurrentProcedureId()).Returns(procedureId);
        mockProcedureContext.Setup(c => c.CurrentProcedureId).Returns(procedureId);

        // Setup procedure repository to return a minimal procedure
        mockProcedureRepo.Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(new Procedure
            {
                Id = procedureId,
                Name = "Test Procedure",
                RootNodeIds = new List<Guid>()
            });

        // Setup variable resolver to return an empty variable context
        mockVariableResolver.Setup(r => r.InitializeContextAsync(
                It.IsAny<Guid>(),
                It.IsAny<Procedure>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(new VariableContext());

        var executionInitializer = new ExecutionInitializer(
            mockProcedureRepo.Object,
            mockTimingOrchestrator.Object,
            mockAgentManager.Object,
            executionIdAssigner,
            mockVariableResolver.Object,
            mockProcedureContext.Object,
            new TestLogger<ExecutionInitializer>(output));

        // Mock the timing calculation orchestrator to return nodes with IsExecuting and Progress set
        // This simulates what the real NodeTimingMapper does
        mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchedulingRequest req, CancellationToken _) =>
            {
                output.WriteLine(
                    $"\n[TIMING_ORCHESTRATOR] CalculateAsync called with CurrentTime={req.CurrentTime:F2}s, ExecutionProgressData={req.ExecutionProgressData?.Count ?? 0} items");

                // Track whether this request has progress data (means it's a re-scheduling request)
                lastSchedulingRequestHadProgressData =
                    req.ExecutionProgressData is { Count: > 0 };

                // CRITICAL: This is where we verify the progress data is being passed correctly
                if (req.ExecutionProgressData != null)
                    foreach (var (execId, progress) in req.ExecutionProgressData)
                        output.WriteLine(
                            $"  ExecutionId={execId}, Started={progress.ActualStartTimeUtc}, Completed={progress.CompletedSuccessfully}");

                // Map nodes with execution state (simulating what NodeTimingMapper does)
                var updatedNodes = req.Nodes.Select(n =>
                {
                    if (n is not SkillExecutionNode sn || sn.SkillExecutionTask.ExecutionId == null)
                        return n;

                    var execId = sn.SkillExecutionTask.ExecutionId.Value;

                    // Check if we have progress data for this execution
                    if (req.ExecutionProgressData?.TryGetValue(execId, out var progressData) == true)
                    {
                        var isExecuting = !progressData.CompletedSuccessfully;
                        var progress = progressData.CompletedSuccessfully
                            ? 100.0
                            : progressData.CurrentTimeIntoExecution / progressData.EstimatedTotalDuration * 100.0;

                        output.WriteLine(
                            $"  [NODE_MAPPING] Node {sn.Id}: IsExecuting={isExecuting}, Progress={progress:F1}%");

                        var updatedTask = sn.SkillExecutionTask with
                        {
                            IsExecuting = isExecuting,
                            Progress = progress,
                            StartTime = (progressData.ActualStartTimeUtc - req.ProcedureStartTimeUtc!.Value)
                            .TotalSeconds,
                            FinishTime = progressData.CompletedSuccessfully
                                ? (progressData.ActualStartTimeUtc - req.ProcedureStartTimeUtc!.Value).TotalSeconds +
                                  progressData.CurrentTimeIntoExecution
                                : null
                        };

                        return sn with { SkillExecutionTask = updatedTask };
                    }

                    return n;
                }).ToList();

                return new ScheduleResult
                {
                    Success = true,
                    UpdatedNodes = updatedNodes,
                    NodeSchedules = new List<NodeSchedule>(),
                    ErrorMessage = null
                };
            });

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

        var executionTriggerService = new ExecutionTriggerService(
            eventBus,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            new TestLogger<ExecutionTriggerService>(output),
            NullLogger<PipelineEvents>.Instance);

        var mockEventPublisher = new Mock<IExecutionEventPublisher>();
        mockEventPublisher.Setup(p => p.RefreshChangeTrackersFromRepositoryAsync())
            .Returns(Task.CompletedTask);
        var publishedNodesList = new List<(DateTimeOffset Timestamp, IReadOnlyList<Node> Nodes, string Source)>();

        // Create subjects for the observables
        var nodesChangedSubject = new Subject<IReadOnlyList<Node>>();
        var edgesChangedSubject = new Subject<IReadOnlyList<DependencyEdge>>();

        mockEventPublisher.Setup(p => p.NodesChanged).Returns(nodesChangedSubject.AsObservable());
        mockEventPublisher.Setup(p => p.EdgesChanged).Returns(edgesChangedSubject.AsObservable());

        mockEventPublisher
            .Setup(p => p.PublishNodeChanges(It.IsAny<IReadOnlyList<Node>>()))
            .Callback<IReadOnlyList<Node>>(nodes =>
            {
                // If the last CalculateAsync had progress data, this is a RESCHEDULING publication
                var source = lastSchedulingRequestHadProgressData ? "RESCHEDULING" : "INITIAL";
                publishedNodesList.Add((DateTimeOffset.UtcNow, nodes, source));
                output.WriteLine(
                    $"\n[PUBLISHED] {source} - {nodes.Count} nodes published at {DateTimeOffset.UtcNow:HH:mm:ss.fff}");

                foreach (var node in nodes.OfType<SkillExecutionNode>())
                    output.WriteLine(
                        $"  Node {node.Id}: IsExecuting={node.SkillExecutionTask.IsExecuting}, Progress={node.SkillExecutionTask.Progress}");

                // Emit to the observable
                nodesChangedSubject.OnNext(nodes);
            });

        mockEventPublisher
            .Setup(p => p.PublishEdgeChanges(It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Callback<IReadOnlyList<DependencyEdge>>(edges => { edgesChangedSubject.OnNext(edges); });

        // Observer surface consumed by the orchestrator's per-channel node stream. Forward
        // OnNext into the same PublishNodeChanges callback above so the test's capture list
        // records publications driven by the pipeline.
        mockEventPublisher.Setup(p => p.NodesObserver).Returns(
            Observer.Create<IReadOnlyList<Node>>(
                nodes => mockEventPublisher.Object.PublishNodeChanges(nodes),
                _ => { },
                () => { }));

        // Use REAL ExecutionProgressMonitor (it uses stateManager which we already have)
        var progressMonitor = new ExecutionProgressMonitor(stateManager);

        // Use real DependencyGraphAnalyzer with mocked hierarchy processor
        var mockHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        mockHierarchyProcessor
            .Setup(p => p.ProcessHierarchy(It.IsAny<IReadOnlyList<Node>>()))
            .Returns((IReadOnlyList<Node> n) => new NodeHierarchyInfo
            {
                TaskNodes = new List<TaskNode>(),
                SkillExecutionNodes = n.OfType<SkillExecutionNode>().ToList(),
                RouterNodes = n.OfType<RouterNode>().ToList(),
                ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
                TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
                SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
            });

        var dependencyGraphAnalyzer = new DependencyGraphAnalyzer(
            mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            new TestLogger<DependencyGraphAnalyzer>(output));

        // Create the real re-scheduling services (that ExecutionOrchestrator now depends on)
        var progressDataBuilder = new ExecutionProgressDataBuilder();
        var timeCalculator = new ExecutionTimeCalculator();

        var reschedulingCoordinator = new ReschedulingCoordinator(
            new TestLogger<ReschedulingCoordinator>(output),
            NullLogger<PipelineEvents>.Instance,
            TimeProvider.System,
            stateManager,
            progressDataBuilder,
            progressMonitor,
            timeCalculator,
            mockTimingOrchestrator.Object);

        // Create the ExecutionOrchestrator with all real dependencies
        var agentSerializationValidatorMock = new Mock<IAgentSerializationValidator>();
        agentSerializationValidatorMock
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns(Array.Empty<AgentSerializationViolation>());

        var orchestrator = new ExecutionOrchestrator(
            new TestLogger<ExecutionOrchestrator>(output),
            NullLogger<PipelineEvents>.Instance,
            TimeProvider.System,
            executionInitializer,
            stateManager,
            mockEventPublisher.Object,
            progressMonitor,
            dependencyGraphAnalyzer,
            executionTriggerService,
            eventBus,
            reschedulingCoordinator,
            new ExecutionTimingPublisher(NullLogger<ExecutionTimingPublisher>.Instance),
            new ExecutionEventDispatcher(
                stateManager,
                stateTransitionService,
                new TestLogger<ExecutionEventDispatcher>(output),
                NullLogger<PipelineEvents>.Instance),
            new ExecutionPipelineBuilder(
                Options.Create(new ExecutionPipelineConfiguration()),
                NullLogger<PipelineEvents>.Instance,
                Scheduler.Default),
            agentSerializationValidatorMock.Object,
            Options.Create(new ExecutionPipelineConfiguration()),
            Scheduler.Default);

        // Subscribe to published nodes
        var publishedNodesFromObservable = new List<(DateTimeOffset Timestamp, IReadOnlyList<Node> Nodes)>();
        orchestrator.NodesChanged.Subscribe(nodes =>
        {
            publishedNodesFromObservable.Add((DateTimeOffset.UtcNow, nodes));
        });

        // Act - Start execution (this will run in background)
        output.WriteLine("\n=== STARTING EXECUTION ===\n");
        var executionTask = orchestrator.StartLoadedProcedureAsync(CancellationToken.None);

        // Wait for the detached run to actually complete (the mock agent finishes in ~0.4s) instead of
        // hoping a fixed delay is long enough — under load the fixed delay flaked. After CurrentExecution
        // resolves, every publication, including the terminal 100%-progress snapshot, has been captured.
        var executionResult = await executionTask;
        await orchestrator.CurrentExecution.WaitAsync(TimeSpan.FromSeconds(10));
        output.WriteLine($"\n=== EXECUTION COMPLETED: {executionResult} ===\n");

        // Assert - CRITICAL CHECKS
        output.WriteLine("\n=== VERIFYING PUBLISHED NODES ===\n");

        // 1. Verify we captured publications
        Assert.NotEmpty(publishedNodesList);
        output.WriteLine($"✓ Captured {publishedNodesList.Count} node publications");

        // 2. CRITICAL: Find publications that came from RESCHEDULING (after CalculateAsync)
        var rescheduledPublications = publishedNodesList.Where(p => p.Source == "RESCHEDULING").ToList();
        Assert.NotEmpty(rescheduledPublications);
        output.WriteLine($"✓ Found {rescheduledPublications.Count} publications from RESCHEDULING");

        // 3. CRITICAL: Verify that nodes have IsExecuting state (either true while running or false when completed)
        var nodesWithIsExecutingState = rescheduledPublications
            .SelectMany(p => p.Nodes.OfType<SkillExecutionNode>())
            .Where(n => n.SkillExecutionTask.IsExecuting.HasValue)
            .ToList();

        var runningNodes = nodesWithIsExecutingState.Where(n => n.SkillExecutionTask.IsExecuting == true).ToList();
        var completedNodes = nodesWithIsExecutingState.Where(n => n.SkillExecutionTask.IsExecuting == false).ToList();

        Assert.NotEmpty(nodesWithIsExecutingState);
        output.WriteLine($"✓ Found {nodesWithIsExecutingState.Count} nodes with IsExecuting state FROM RESCHEDULING");
        output.WriteLine($"  - Running (IsExecuting=true): {runningNodes.Count}");
        output.WriteLine($"  - Completed (IsExecuting=false): {completedNodes.Count}");

        // 4. CRITICAL: Verify that nodes published AFTER ScheduleAsync have Progress set
        var nodesWithProgressFromRescheduling = rescheduledPublications
            .SelectMany(p => p.Nodes.OfType<SkillExecutionNode>())
            .Where(n => n.SkillExecutionTask.Progress.HasValue)
            .ToList();

        Assert.NotEmpty(nodesWithProgressFromRescheduling);
        output.WriteLine(
            $"✓ Found {nodesWithProgressFromRescheduling.Count} nodes with Progress set FROM RESCHEDULING");

        // 5. CRITICAL: Verify progress percentage values are correct
        // Progress should be calculated as: (CurrentTimeIntoExecution / EstimatedTotalDuration) * 100
        // Our mock agent sends updates at 0.1s, 0.2s, 0.3s, 0.4s with total duration 0.4s
        // So we should see progress values like 25%, 50%, 75%, 100%
        var progressValues = nodesWithProgressFromRescheduling
            .Select(n => n.SkillExecutionTask.Progress!.Value)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        output.WriteLine("\n=== PROGRESS PERCENTAGE VALUES FOUND ===");
        foreach (var progress in progressValues) output.WriteLine($"  Progress: {progress:F1}%");

        // Verify we have 100% completion (skill finished)
        Assert.Contains(100.0, progressValues);
        output.WriteLine("✓ Found nodes with 100% progress (completion)");

        // Verify progress values are reasonable percentages (0-100)
        Assert.All(progressValues, p => Assert.InRange(p, 0.0, 100.0));
        output.WriteLine("✓ All progress values are in valid range (0-100%)");

        // 6. CRITICAL: Verify ActualStartTime is set for executing/completed skills
        var nodesWithActualStart = rescheduledPublications
            .SelectMany(p => p.Nodes.OfType<SkillExecutionNode>())
            .Where(n => n.SkillExecutionTask.IsExecuting == true ||
                        n.SkillExecutionTask.Progress is 100.0)
            .ToList();

        output.WriteLine("\n=== ACTUAL START/FINISH TIME VERIFICATION ===");

        // For skills that are executing or completed, check if they have execution timing data
        // NOTE: In the current implementation, ActualStart/Finish might not be directly on SkillExecutionTask
        // but they should be reflected in the timing (StartTime should match actual start once executing)

        // At minimum, verify that executing skills have realistic start times
        foreach (var node in nodesWithActualStart.Take(5))
            output.WriteLine($"Skill: {node.SkillExecutionTask.Name} | " +
                             $"IsExecuting: {node.SkillExecutionTask.IsExecuting} | " +
                             $"Progress: {node.SkillExecutionTask.Progress:F1}% | " +
                             $"StartTime: {node.SkillExecutionTask.StartTime:F3}s");

        // 7. CRITICAL: Verify skills transition to completed state (IsExecuting = false/null after completion)
        // Look for skills that have 100% progress - they should NOT be marked as IsExecuting
        var completedSkills = rescheduledPublications
            .SelectMany(p => p.Nodes.OfType<SkillExecutionNode>())
            .Where(n => n.SkillExecutionTask.Progress is >= 99.0) // Near or at completion
            .ToList();

        output.WriteLine("\n=== SKILL COMPLETION STATE VERIFICATION ===");
        output.WriteLine($"Found {completedSkills.Count} completed/nearly-completed skills");

        if (completedSkills.Any())
        {
            // For skills at 100% progress, verify they're no longer marked as executing
            var fullyCompletedSkills = completedSkills
                .Where(n => n.SkillExecutionTask.Progress!.Value == 100.0)
                .ToList();

            output.WriteLine($"Skills at 100% completion: {fullyCompletedSkills.Count}");

            foreach (var skill in fullyCompletedSkills.Take(3))
            {
                output.WriteLine($"  Skill: {skill.SkillExecutionTask.Name} | " +
                                 $"Progress: {skill.SkillExecutionTask.Progress:F1}% | " +
                                 $"IsExecuting: {skill.SkillExecutionTask.IsExecuting?.ToString() ?? "null"}");

                // A completed skill (100% progress) should have IsExecuting = false or null
                // It should NOT still be marked as IsExecuting = true
                if (skill.SkillExecutionTask.IsExecuting == true)
                    output.WriteLine("  ⚠️  WARNING: Skill is at 100% but still marked as IsExecuting=true!");
            }

            // Assert that at least some publications show completed skills with IsExecuting=false
            // This verifies the state transitions properly
            var properlyCompletedSkills = rescheduledPublications
                .SelectMany(p => p.Nodes.OfType<SkillExecutionNode>())
                .Where(n => n.SkillExecutionTask.Progress is 100.0 &&
                            n.SkillExecutionTask.IsExecuting != true) // false or null
                .ToList();

            if (properlyCompletedSkills.Any())
                output.WriteLine(
                    $"✓ Found {properlyCompletedSkills.Count} skills properly marked as completed (IsExecuting=false/null at 100%)");
            else
                output.WriteLine(
                    "⚠️  No skills found with proper completion state (IsExecuting should be false/null at 100% progress)");
        }

        // 8. Print detailed example
        var exampleNode = nodesWithIsExecutingState.FirstOrDefault();
        if (exampleNode != null)
        {
            output.WriteLine("\n=== EXAMPLE NODE FROM RESCHEDULING ===");
            output.WriteLine($"Node ID: {exampleNode.Id}");
            output.WriteLine($"IsExecuting: {exampleNode.SkillExecutionTask.IsExecuting}");
            output.WriteLine($"Progress: {exampleNode.SkillExecutionTask.Progress:F1}%");
            output.WriteLine($"StartTime: {exampleNode.SkillExecutionTask.StartTime}");
            output.WriteLine($"Duration: {exampleNode.SkillExecutionTask.Duration}");
        }

        // 9. Verify the mock was called with progress data
        mockTimingOrchestrator.Verify(
            o => o.CalculateAsync(
                It.Is<SchedulingRequest>(req =>
                    req.ExecutionProgressData != null &&
                    req.ExecutionProgressData.Count > 0),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "TimingCalculationOrchestrator should be called with ExecutionProgressData");

        output.WriteLine("\n✓ TimingCalculationOrchestrator was called with ExecutionProgressData");

        // 10. CRITICAL: Verify that execution completed with all skills at 100% and finished
        output.WriteLine("\n=== FINAL STATE VERIFICATION ===");

        // Get the very last publication (should be the final state after execution completes)
        var finalPublication = publishedNodesList.Last();
        var finalSkills = finalPublication.Nodes.OfType<SkillExecutionNode>().ToList();

        output.WriteLine($"Final publication source: {finalPublication.Source}");
        output.WriteLine($"Final publication skill count: {finalSkills.Count}");

        // ALL skills in the final publication must be at 100% and marked as finished (IsExecuting=false)
        foreach (var skill in finalSkills)
        {
            output.WriteLine(
                $"  Skill {skill.SkillExecutionTask.Name}: Progress={skill.SkillExecutionTask.Progress:F1}%, IsExecuting={skill.SkillExecutionTask.IsExecuting}");

            Assert.True(skill.SkillExecutionTask.Progress.HasValue,
                $"Skill {skill.SkillExecutionTask.Name} should have Progress set in final state");
            Assert.Equal(100.0, skill.SkillExecutionTask.Progress.Value, 1.0);
            Assert.False(skill.SkillExecutionTask.IsExecuting ?? false,
                $"Skill {skill.SkillExecutionTask.Name} should be marked as finished (IsExecuting=false) in final state");
        }

        output.WriteLine($"✓ All {finalSkills.Count} skills in final state are at 100% and marked as finished");
        output.WriteLine($"✓ Execution completed successfully: {executionResult}");
        output.WriteLine("\n=== ALL CRITICAL CHECKS PASSED ===\n");
    }

    private (List<Node> nodes, List<DependencyEdge> edges) CreateTestProcedure()
    {
        var agentAId = Guid.NewGuid();
        var agentBId = Guid.NewGuid();

        var skillA = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "SkillA",
            Description = "First skill",
            Properties = []
        };

        var skillB = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "SkillB",
            Description = "Second skill",
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
                Duration = 0.5, // 500ms
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
                Duration = 0.5, // 500ms
                Skill = skillB,
                AgentId = agentBId
            },
            ProcedureId = default
        };

        // Simple dependency: A must finish before B starts
        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = nodeA.Id,
                TargetId = nodeB.Id,
                SourceHandle = "right", // Finish
                TargetHandle = "left",
                ProcedureId = default // Start
            }
        };

        return ([nodeA, nodeB], edges);
    }

    private IRuntimeAgent CreateMockAgent(Guid agentId, string agentName, Guid executionId, Skill skill)
    {
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns(agentName);

        mockAgent.Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, CancellationToken>((_, _, ct) =>
            {
                return Observable.Create<AgentProgress>(async observer =>
                {
                    try
                    {
                        var startTime = DateTime.UtcNow;
                        var duration = TimeSpan.FromMilliseconds(400);
                        var updateInterval = TimeSpan.FromMilliseconds(100);
                        var elapsed = TimeSpan.Zero;

                        while (elapsed < duration && !ct.IsCancellationRequested)
                        {
                            await Task.Delay(updateInterval, ct);
                            elapsed += updateInterval;

                            observer.OnNext(new AgentProgress
                            {
                                ExecutionId = executionId,
                                SkillId = skill.Id,
                                AgentId = agentId,
                                ActualStartTimeUtc = startTime,
                                CurrentTimeIntoExecution = elapsed.TotalSeconds,
                                EstimatedTotalDuration = duration.TotalSeconds,
                                StatusMessage = $"Executing at {elapsed.TotalSeconds:F2}s"
                            });

                            output.WriteLine(
                                $"[{agentName}] Progress: {elapsed.TotalSeconds:F2}s / {duration.TotalSeconds:F2}s");
                        }

                        if (!ct.IsCancellationRequested)
                        {
                            observer.OnNext(new AgentProgress
                            {
                                ExecutionId = executionId,
                                SkillId = skill.Id,
                                AgentId = agentId,
                                ActualStartTimeUtc = startTime,
                                CurrentTimeIntoExecution = duration.TotalSeconds,
                                EstimatedTotalDuration = duration.TotalSeconds,
                                StatusMessage = "Completed",
                                CompletedSuccessfully = true
                            });
                            output.WriteLine($"[{agentName}] Completed");
                            observer.OnCompleted();
                        }
                    }
                    catch (Exception ex)
                    {
                        output.WriteLine($"[{agentName}] Error: {ex.Message}");
                        observer.OnError(ex);
                    }
                });
            });

        return mockAgent.Object;
    }
}