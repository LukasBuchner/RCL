using System.Reactive.Linq;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement;

/// <summary>
///     Integration tests that verify the fix for a race condition where TaskNodes and their child
///     SkillExecutionNodes disappear from queries after execution completes.
///     The root cause was that an Rx.NET Sample timer callback could fire after cleanup started,
///     calling <see cref="ExecutionEventPublisher.PublishNodeChanges" /> with stale data and
///     overwriting the BehaviorSubject after <c>RefreshChangeTrackersFromRepositoryAsync</c>
///     had already restored correct data from the database.
///     The fix adds cleanup-phase guards to publish methods, preserves all excluded nodes
///     during rescheduling merge-back, and adds retry logic to the repository refresh.
/// </summary>
public sealed class ExecutionNodeDisappearanceTests : IDisposable
{
    private readonly List<DependencyEdge> _allEdges;

    // Test data — mirrors the user's real scenario:
    //   3 standalone SkillExecutionNodes (no parent)
    //   1 TaskNode with 2 child SkillExecutionNodes
    private readonly List<Node> _allNodes;
    private readonly Guid _childSkillAId = Guid.NewGuid();
    private readonly Guid _childSkillBId = Guid.NewGuid();
    private readonly DependencyEdgeApplicationService _edgeService;
    private readonly ExecutionEventPublisher _executionEventPublisher;

    // Real components
    private readonly ProcedureStateTracker _tracker;
    private readonly NodeApplicationService _nodeService;
    private readonly ProcedureContext _procedureContext;
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly ProcedureOrchestrator _procedureOrchestrator;

    // Mocked DB layer
    private readonly Mock<IProcedureRepository> _procedureRepo;
    private readonly Guid _standaloneSkill1Id = Guid.NewGuid();
    private readonly Guid _standaloneSkill2Id = Guid.NewGuid();
    private readonly Guid _standaloneSkill3Id = Guid.NewGuid();

    // Known IDs for assertions
    private readonly Guid _taskNodeId = Guid.NewGuid();

    public ExecutionNodeDisappearanceTests()
    {
        var (nodes, edges) = BuildScenario();
        _allNodes = nodes;
        _allEdges = edges;

        // Mock DB repository
        _procedureRepo = new Mock<IProcedureRepository>();
        SetupProcedureRepo();

        // Scoped repository queries the unified tracker issues on procedure load and refresh.
        // All scenario nodes/edges belong to the single test procedure.
        _procedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId)).ReturnsAsync(_allNodes);
        _procedureRepo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId)).ReturnsAsync(_allEdges);

        // Real unified tracker — ProcedureOrchestrator drives its scoped load/unload via IProcedureStateScope.
        _tracker = new ProcedureStateTracker(
            _procedureRepo.Object,
            NullLogger<ProcedureStateTracker>.Instance);

        // Real ProcedureOrchestrator (drives the tracker's scoped load and variable notifications)
        _procedureOrchestrator = new ProcedureOrchestrator(
            _procedureRepo.Object,
            _tracker,
            _tracker,
            new Mock<ILogger<ProcedureOrchestrator>>().Object);

        // Real ProcedureContext
        _procedureContext = new ProcedureContext(_procedureOrchestrator);

        // Real ExecutionEventPublisher
        _executionEventPublisher = new ExecutionEventPublisher(
            _tracker,
            _tracker,
            NullLogger<ExecutionEventPublisher>.Instance);

        // Real application services
        _nodeService = new NodeApplicationService(
            _procedureRepo.Object,
            new Mock<ICrudSchedulingOrchestrator>().Object,
            _tracker,
            _procedureContext,
            new Mock<IProcedureVariableService>().Object,
            Options.Create(new SchedulingConfiguration
            {
                Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
            }),
            new Mock<ILogger<NodeApplicationService>>().Object);

        _edgeService = new DependencyEdgeApplicationService(
            new Mock<ICrudSchedulingOrchestrator>().Object,
            _tracker,
            _procedureContext,
            new Mock<ILogger<DependencyEdgeApplicationService>>().Object);
    }

    public void Dispose()
    {
        _tracker.Dispose();
        _procedureOrchestrator.Dispose();
    }

    #region Node Disappearance Race Condition Fix

    /// <summary>
    ///     Verifies that a partial publish during execution (by design, the BehaviorSubject does
    ///     a full replacement) is followed by a successful refresh that restores all nodes.
    ///     The execution pipeline may publish only scheduled SkillExecutionNodes, temporarily
    ///     reducing the visible node count. The cleanup refresh must restore all 6 nodes.
    /// </summary>
    [Fact]
    public async Task PartialPublishDuringExecution_FollowedByRefresh_RestoresAllNodes()
    {
        // Arrange: load procedure — all 6 nodes should be visible
        await LoadProcedureAndDrainAsync(_procedureId);

        var nodesBefore = await _nodeService.GetAllNodesAsync();
        nodesBefore.Should().HaveCount(6, "all 6 nodes (3 standalone + 1 TaskNode + 2 children) should be present");

        // Act: simulate what the execution pipeline does — publish only standalone
        // SkillExecutionNodes (the scheduling pipeline only returns scheduled nodes)
        var standaloneOnlyNodes = _allNodes
            .Where(n => n is SkillExecutionNode && n.ParentId == null)
            .ToList();
        standaloneOnlyNodes.Should().HaveCount(3, "only 3 standalone SkillExecutionNodes in the publish");

        _executionEventPublisher.PublishNodeChanges(standaloneOnlyNodes);

        // During execution, only the published subset is visible (full replacement by design)
        var nodesDuringExecution = await _nodeService.GetAllNodesAsync();
        nodesDuringExecution.Should().HaveCount(3,
            "during execution, the BehaviorSubject reflects the latest publish (full replacement by design)");

        // Act: simulate cleanup refresh (as the finally block in ExecutionOrchestrator does)
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert: all 6 nodes restored after cleanup refresh
        var nodesAfterRefresh = await _nodeService.GetAllNodesAsync();
        nodesAfterRefresh.Should().HaveCount(6,
            "after cleanup refresh from repository, all 6 nodes must be restored");
        nodesAfterRefresh.Should().Contain(n => n.Id == _taskNodeId,
            "TaskNode must be restored after cleanup refresh");
        nodesAfterRefresh.Should().Contain(n => n.Id == _childSkillAId,
            "child SkillA must be restored after cleanup refresh");
        nodesAfterRefresh.Should().Contain(n => n.Id == _childSkillBId,
            "child SkillB must be restored after cleanup refresh");
    }

    /// <summary>
    ///     Verifies the same partial-publish-then-refresh behavior via the reactive subscription path.
    ///     The subscription emits the partial set during execution, then the full set after refresh.
    /// </summary>
    [Fact]
    public async Task PartialPublishDuringExecution_SubscriptionRestoresAllNodesAfterRefresh()
    {
        // Arrange
        await LoadProcedureAndDrainAsync(_procedureId);

        var subNodesBefore = await _nodeService.Nodes.FirstAsync();
        subNodesBefore.Should().HaveCount(6);

        // Act: publish only standalone SkillExecutionNodes (execution phase)
        var standaloneOnlyNodes = _allNodes
            .Where(n => n is SkillExecutionNode && n.ParentId == null)
            .ToList();
        _executionEventPublisher.PublishNodeChanges(standaloneOnlyNodes);

        // During execution, subscription reflects the partial set
        var subNodesDuringExecution = await _nodeService.Nodes.FirstAsync();
        subNodesDuringExecution.Should().HaveCount(3,
            "during execution, subscription reflects the latest publish (full replacement by design)");

        // Act: simulate cleanup refresh
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert: subscription restores all 6 nodes after refresh
        var subNodesAfterRefresh = await _nodeService.Nodes.FirstAsync();
        subNodesAfterRefresh.Should().HaveCount(6,
            "after cleanup refresh, subscription must emit all 6 nodes");
    }

    /// <summary>
    ///     Verifies that parent-child relationships are fully restored after cleanup refresh,
    ///     even when execution temporarily publishes only top-level SkillExecutionNodes.
    /// </summary>
    [Fact]
    public async Task PartialPublishDuringExecution_ParentChildRelationshipsRestoredAfterRefresh()
    {
        // Arrange
        await LoadProcedureAndDrainAsync(_procedureId);

        // Act: simulate the scheduling pipeline modifying timing on only the
        // top-level SkillExecutionNodes and omitting the TaskNode hierarchy entirely
        var executionNodes = _allNodes
            .OfType<SkillExecutionNode>()
            .Where(n => n.ParentId == null)
            .Select(n => (Node)(n with
            {
                SkillExecutionTask = n.SkillExecutionTask with { StartTime = 50.0 }
            }))
            .ToList();

        _executionEventPublisher.PublishNodeChanges(executionNodes);

        // During execution, parent-child nodes are temporarily absent
        var nodesDuringExecution = await _nodeService.GetAllNodesAsync();
        nodesDuringExecution.Should().HaveCount(3,
            "during execution, only the 3 published standalone nodes are visible");

        // Act: simulate cleanup refresh
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert: parent-child relationships must be fully restored
        var nodesAfter = await _nodeService.GetAllNodesAsync();
        var nodeIds = nodesAfter.Select(n => n.Id).ToHashSet();
        var childNodes = nodesAfter.Where(n => n.ParentId.HasValue).ToList();

        nodesAfter.Should().HaveCount(6,
            "all nodes must be restored after cleanup refresh");
        childNodes.Should().HaveCount(2,
            "both child SkillExecutionNodes must be restored");
        childNodes.Should().AllSatisfy(n =>
            nodeIds.Should().Contain(n.ParentId!.Value,
                $"child node's parent (TaskNode) must be present after cleanup refresh"));
    }

    /// <summary>
    ///     Verifies that RefreshChangeTrackersFromRepositoryAsync correctly restores all nodes
    ///     after a partial publish has occurred. This simulates the finally block in ExecutionOrchestrator.
    /// </summary>
    [Fact]
    public async Task RefreshAfterIncompletePublish_RestoresAllNodes()
    {
        // Arrange: load procedure and verify all nodes
        await LoadProcedureAndDrainAsync(_procedureId);

        var nodesBefore = await _nodeService.GetAllNodesAsync();
        nodesBefore.Should().HaveCount(6);

        // Act: publish incomplete set (simulating execution pipeline)
        var standaloneOnlyNodes = _allNodes
            .Where(n => n is SkillExecutionNode && n.ParentId == null)
            .ToList();
        _executionEventPublisher.PublishNodeChanges(standaloneOnlyNodes);

        // Act: refresh from repository (as the finally block in ExecutionOrchestrator does)
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert: all 6 nodes should be restored
        var nodesAfterRefresh = await _nodeService.GetAllNodesAsync();
        nodesAfterRefresh.Should().HaveCount(6,
            "after refreshing from repository, all 6 nodes should be restored");
        nodesAfterRefresh.Should().Contain(n => n.Id == _taskNodeId,
            "TaskNode should be restored after refresh");
        nodesAfterRefresh.Should().Contain(n => n.Id == _childSkillAId,
            "child SkillA should be restored after refresh");
        nodesAfterRefresh.Should().Contain(n => n.Id == _childSkillBId,
            "child SkillB should be restored after refresh");
    }

    /// <summary>
    ///     Verifies that the retry mechanism in RefreshFromRepositoryAsync recovers from
    ///     transient DB failures. The first call fails, but the retry succeeds and restores
    ///     all nodes. This validates the exponential backoff retry logic.
    /// </summary>
    [Fact]
    public async Task RefreshWithTransientFailure_RetriesAndRecovers()
    {
        // Arrange: load procedure
        await LoadProcedureAndDrainAsync(_procedureId);

        // Publish incomplete set to simulate execution
        var standaloneOnlyNodes = _allNodes
            .Where(n => n is SkillExecutionNode && n.ParentId == null)
            .ToList();
        _executionEventPublisher.PublishNodeChanges(standaloneOnlyNodes);

        // Set up repository to fail on the first call but succeed on the second (retry)
        var callCount = 0;
        _procedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 1)
                    throw new InvalidOperationException("Simulated transient DB failure");
                return Task.FromResult(_allNodes);
            });

        // Act: refresh — first attempt fails, retry succeeds
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert: all 6 nodes restored thanks to retry
        var nodesAfterRefresh = await _nodeService.GetAllNodesAsync();
        nodesAfterRefresh.Should().HaveCount(6,
            "retry mechanism should recover from transient failure and restore all 6 nodes");
        callCount.Should().Be(2,
            "the scoped node query should be called twice: first attempt fails, second attempt succeeds");
    }

    /// <summary>
    ///     End-to-end lifecycle test: load procedure, simulate execution publish with subset,
    ///     then cleanup refresh. Verifies that the final state after cleanup contains all nodes,
    ///     which is the critical invariant — nodes must never permanently disappear.
    /// </summary>
    [Fact]
    public async Task FullExecutionLifecycle_FinalStateAfterCleanupHasAllNodes()
    {
        // Arrange
        await LoadProcedureAndDrainAsync(_procedureId);

        var emissions = new List<IReadOnlyList<Node>>();
        using var subscription = _nodeService.Nodes.Subscribe(n => emissions.Add(n));
        await Task.Yield();

        // Phase 1: all nodes visible
        emissions.Last().Should().HaveCount(6,
            "initially all 6 nodes should be visible via subscription");

        // Phase 2: execution starts — scheduling pipeline publishes only scheduled nodes
        var scheduledNodes = _allNodes
            .OfType<SkillExecutionNode>()
            .Where(n => n.ParentId == null)
            .Select(n => (Node)(n with
            {
                SkillExecutionTask = n.SkillExecutionTask with { StartTime = 10.0 }
            }))
            .ToList();

        _executionEventPublisher.PublishNodeChanges(scheduledNodes);
        await Task.Yield();

        // During execution, only the scheduled subset is visible (by design)
        emissions.Last().Should().HaveCount(3,
            "during execution, only scheduled nodes are published (full replacement by design)");

        // Phase 3: execution completes — cleanup refresh from repository
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();
        await Task.Yield();

        // Phase 3 assertion: all nodes restored — this is the critical invariant
        emissions.Last().Should().HaveCount(6,
            "after execution cleanup refresh, all 6 nodes must be present");

        // The final emission must have all nodes — this is the fix guarantee
        var finalEmission = emissions.Last();
        finalEmission.Should().Contain(n => n.Id == _taskNodeId,
            "TaskNode must be present in final state");
        finalEmission.Should().Contain(n => n.Id == _childSkillAId,
            "child SkillA must be present in final state");
        finalEmission.Should().Contain(n => n.Id == _childSkillBId,
            "child SkillB must be present in final state");
    }

    #endregion

    #region Helpers

    /// <summary>
    ///     Loads the procedure through the orchestrator and waits for the unified tracker's
    ///     fire-and-forget scoped load to complete before assertions read the loaded state.
    /// </summary>
    private async Task LoadProcedureAndDrainAsync(Guid procedureId)
    {
        await _procedureOrchestrator.LoadProcedureAsync(procedureId);
        for (var i = 0; i < 500 && !_tracker.IsInitialized; i++)
            await Task.Delay(1);
    }

    /// <summary>
    ///     Builds a node graph matching the user's reported scenario:
    ///     3 standalone SkillExecutionNodes (no parent) + 1 TaskNode with 2 child SkillExecutionNodes.
    ///     After execution, only the 3 standalone nodes remain visible.
    /// </summary>
    private (List<Node> Nodes, List<DependencyEdge> Edges) BuildScenario()
    {
        var agentId = Guid.NewGuid();
        var skill1 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a specific location",
            Properties = []
        };
        var skill2 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To Tag",
            Description = "Move an object to a predefined location tag",
            Properties = []
        };
        var skill3 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Move an object to a specific position in 3D space",
            Properties = []
        };

        var pos = new NodePosition { X = 0, Y = 0 };

        // 3 standalone SkillExecutionNodes (no parent) — these survive execution
        var standalone1 = new SkillExecutionNode
        {
            Id = _standaloneSkill1Id,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 140, Y = 230 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "sd",
                StartTime = 70,
                Duration = 65,
                Skill = skill1,
                AgentId = agentId
            }
        };

        var standalone2 = new SkillExecutionNode
        {
            Id = _standaloneSkill2Id,
            ProcedureId = _procedureId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "sdf",
                StartTime = 0,
                Duration = 70,
                Skill = skill2,
                AgentId = agentId
            }
        };

        var standalone3 = new SkillExecutionNode
        {
            Id = _standaloneSkill3Id,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 140, Y = 290 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "asdfdssdfsdf",
                StartTime = 70,
                Duration = 120,
                Skill = skill3,
                AgentId = agentId
            }
        };

        // 1 TaskNode parent — this disappears after execution
        var taskNode = new TaskNode
        {
            Id = _taskNodeId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 60 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Tasky",
                StartTime = 0,
                Duration = 145
            }
        };

        // 2 child SkillExecutionNodes inside the TaskNode — these also disappear
        var childSkillA = new SkillExecutionNode
        {
            Id = _childSkillAId,
            ProcedureId = _procedureId,
            ParentId = _taskNodeId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skillya",
                StartTime = 0,
                Duration = 65,
                Skill = skill1,
                AgentId = agentId
            }
        };

        var childSkillB = new SkillExecutionNode
        {
            Id = _childSkillBId,
            ProcedureId = _procedureId,
            ParentId = _taskNodeId,
            Extent = "parent",
            Position = new NodePosition { X = 130, Y = 100 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "sdf",
                StartTime = 65,
                Duration = 80,
                Skill = skill3,
                AgentId = agentId
            }
        };

        var nodes = new List<Node>
        {
            standalone1, standalone2, standalone3,
            taskNode, childSkillA, childSkillB
        };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = _procedureId,
                SourceId = _standaloneSkill2Id,
                TargetId = _standaloneSkill1Id,
                SourceHandle = "right",
                TargetHandle = "left"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = _procedureId,
                SourceId = _standaloneSkill2Id,
                TargetId = _standaloneSkill3Id,
                SourceHandle = "right",
                TargetHandle = "left"
            }
        };

        return (nodes, edges);
    }

    /// <summary>
    ///     Sets up the procedure repository mock with a single test procedure.
    /// </summary>
    private void SetupProcedureRepo()
    {
        var procedure = new Procedure
        {
            Id = _procedureId,
            Name = "Procedure 1",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = Array.Empty<Guid>()
        };

        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync(true);
    }

    #endregion
}