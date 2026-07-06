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
///     Integration tests using real components wired together.
///     Only database repositories are mocked. Everything else is the real implementation:
///     ProcedureOrchestrator, ProcedureContext, ProcedureStateTracker,
///     ExecutionEventPublisher, NodeApplicationService, DependencyEdgeApplicationService.
/// </summary>
public sealed class RealPipelineIntegrationTests : IDisposable
{
    private readonly List<DependencyEdge> _allEdges;

    // Test data
    private readonly List<Node> _allNodes;
    private readonly DependencyEdgeApplicationService _edgeService;
    private readonly ExecutionEventPublisher _executionEventPublisher;

    // Real components
    private readonly ProcedureStateTracker _tracker;
    private readonly NodeApplicationService _nodeService;
    private readonly ProcedureContext _procedureContext;
    private readonly Guid _procedureIdA = Guid.NewGuid();
    private readonly Guid _procedureIdB = Guid.NewGuid();
    private readonly ProcedureOrchestrator _procedureOrchestrator;

    // Mocked DB layer
    private readonly Mock<IProcedureRepository> _procedureRepo;

    public RealPipelineIntegrationTests()
    {
        // Build test data for two procedures
        var (nodesA, edgesA) = BuildHierarchy(_procedureIdA, "A");
        var (nodesB, edgesB) = BuildHierarchy(_procedureIdB, "B");
        _allNodes = [.. nodesA, .. nodesB];
        _allEdges = [.. edgesA, .. edgesB];

        // Mock DB repository – the only mock in the pipeline
        _procedureRepo = new Mock<IProcedureRepository>();
        SetupProcedureRepo();

        // Scoped repository queries the unified tracker issues on procedure load and refresh.
        _procedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureIdA))
            .ReturnsAsync(_allNodes.Where(n => n.ProcedureId == _procedureIdA).ToList());
        _procedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureIdB))
            .ReturnsAsync(_allNodes.Where(n => n.ProcedureId == _procedureIdB).ToList());
        _procedureRepo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureIdA))
            .ReturnsAsync(_allEdges.Where(e => e.ProcedureId == _procedureIdA).ToList());
        _procedureRepo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureIdB))
            .ReturnsAsync(_allEdges.Where(e => e.ProcedureId == _procedureIdB).ToList());

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

        // Real ProcedureContext (delegates to real ProcedureOrchestrator)
        _procedureContext = new ProcedureContext(_procedureOrchestrator);

        // Real ExecutionEventPublisher (publishes to the unified tracker)
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

    #region Procedure Load / Unload

    [Fact]
    public async Task NoProcedureLoaded_QueryReturnsEmpty()
    {
        // No procedure loaded – ProcedureContext.CurrentProcedureId is null
        var nodes = await _nodeService.GetAllNodesAsync();
        var edges = await _edgeService.GetAllDependencyEdgesAsync();

        nodes.Should().BeEmpty();
        edges.Should().BeEmpty();
    }

    [Fact]
    public async Task NoProcedureLoaded_SubscriptionReturnsEmpty()
    {
        var subNodes = await _nodeService.Nodes.FirstAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        subNodes.Should().BeEmpty();
        subEdges.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadProcedure_QueryReturnsOnlyThatProceduresNodes()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodes = await _nodeService.GetAllNodesAsync();

        nodes.Should().NotBeEmpty();
        nodes.Should().OnlyContain(n => n.ProcedureId == _procedureIdA);
    }

    [Fact]
    public async Task LoadProcedure_SubscriptionReturnsOnlyThatProceduresNodes()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var subNodes = await _nodeService.Nodes.FirstAsync();

        subNodes.Should().NotBeEmpty();
        subNodes.Should().OnlyContain(n => n.ProcedureId == _procedureIdA);
    }

    [Fact]
    public async Task LoadProcedure_QueryAndSubscriptionReturnIdenticalNodes()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var queryNodes = await _nodeService.GetAllNodesAsync();
        var subNodes = await _nodeService.Nodes.FirstAsync();

        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subNodes.Select(n => n.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task LoadProcedure_QueryAndSubscriptionReturnIdenticalEdges()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subEdges.Select(e => e.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task LoadProcedure_AllEdgeEndpointsExistInFilteredNodeResults()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodes = await _nodeService.GetAllNodesAsync();
        var edges = await _edgeService.GetAllDependencyEdgesAsync();

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();

        edges.Should().AllSatisfy(e =>
        {
            nodeIds.Should().Contain(e.SourceId);
            nodeIds.Should().Contain(e.TargetId);
        });
    }

    [Fact]
    public async Task LoadProcedure_ParentChildHierarchyIsComplete()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodes = await _nodeService.GetAllNodesAsync();
        var nodeIds = nodes.Select(n => n.Id).ToHashSet();

        var childNodes = nodes.Where(n => n.ParentId.HasValue).ToList();
        childNodes.Should().NotBeEmpty();
        childNodes.Should().AllSatisfy(n =>
            nodeIds.Should().Contain(n.ParentId!.Value,
                $"node '{GetNodeName(n)}' parent must be present in the filtered results"));
    }

    [Fact]
    public async Task LoadProcedure_GetNodeByIdAsync_FindsNodeFromLoadedProcedure()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodesA = _allNodes.Where(n => n.ProcedureId == _procedureIdA).ToList();
        var targetNode = nodesA.First();

        var result = await _nodeService.GetNodeByIdAsync(targetNode.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(targetNode.Id);
    }

    [Fact]
    public async Task LoadProcedure_GetNodeByIdAsync_RejectsNodeFromOtherProcedure()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodeFromB = _allNodes.First(n => n.ProcedureId == _procedureIdB);

        // A node from another procedure is outside the loaded procedure's scope, so the
        // unified tracker never holds it and the lookup returns null.
        var result = await _nodeService.GetNodeByIdAsync(nodeFromB.Id);

        result.Should().BeNull(
            "a node from another procedure is not visible while a different procedure is loaded");
    }

    [Fact]
    public async Task LoadProcedure_GetNodesByParentIdAsync_ReturnsFilteredChildren()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var routerA = _allNodes.OfType<RouterNode>()
            .Single(n => n.ProcedureId == _procedureIdA);

        var children = await _nodeService.GetNodesByParentIdAsync(routerA.Id);

        children.Should().NotBeEmpty();
        children.Should().OnlyContain(n => n.ParentId == routerA.Id);
        children.Should().OnlyContain(n => n.ProcedureId == _procedureIdA);
    }

    [Fact]
    public async Task UnloadProcedure_QueryAndSubscriptionReturnEmpty()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Verify we have data
        var nodesBeforeUnload = await _nodeService.GetAllNodesAsync();
        nodesBeforeUnload.Should().NotBeEmpty();

        await _procedureOrchestrator.UnloadCurrentProcedureAsync();

        var queryNodes = await _nodeService.GetAllNodesAsync();
        var subNodes = await _nodeService.Nodes.FirstAsync();
        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        queryNodes.Should().BeEmpty();
        subNodes.Should().BeEmpty();
        queryEdges.Should().BeEmpty();
        subEdges.Should().BeEmpty();
    }

    #endregion

    #region Procedure Switching

    [Fact]
    public async Task SwitchProcedure_QueryReturnsSwitchedProceduresNodes()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodesA = await _nodeService.GetAllNodesAsync();
        nodesA.Should().OnlyContain(n => n.ProcedureId == _procedureIdA);

        // Switch to procedure B
        await LoadProcedureAndDrainAsync(_procedureIdB);

        var nodesB = await _nodeService.GetAllNodesAsync();
        nodesB.Should().OnlyContain(n => n.ProcedureId == _procedureIdB);
    }

    [Fact]
    public async Task SwitchProcedure_SubscriptionEmitsSwitchedData()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var emissions = new List<IReadOnlyList<Node>>();
        using var subscription = _nodeService.Nodes.Subscribe(n => emissions.Add(n));
        await Task.Yield();

        // Switch to procedure B
        await LoadProcedureAndDrainAsync(_procedureIdB);
        await Task.Yield();

        var latestEmission = emissions.Last();
        latestEmission.Should().OnlyContain(n => n.ProcedureId == _procedureIdB);
    }

    [Fact]
    public async Task SwitchProcedure_QueryAndSubscriptionMatchAfterSwitch()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);
        await LoadProcedureAndDrainAsync(_procedureIdB);

        var queryNodes = await _nodeService.GetAllNodesAsync();
        var subNodes = await _nodeService.Nodes.FirstAsync();
        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subNodes.Select(n => n.Id).OrderBy(id => id));
        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subEdges.Select(e => e.Id).OrderBy(id => id));
    }

    #endregion

    #region Execution Simulation via ExecutionEventPublisher

    [Fact]
    public async Task ExecutionPublish_QuerySeesModifiedNodeTiming()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Modify a node's timing to simulate execution progress
        var modifiedNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA
                                           && se.SkillExecutionTask.Name == "A_SkillA1")
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 10.0, Duration = 3.0 }
                };
            return n;
        }).ToList();

        _executionEventPublisher.PublishNodeChanges(modifiedNodes);

        var queryResult = await _nodeService.GetAllNodesAsync();
        var modifiedNode = queryResult.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "A_SkillA1");

        modifiedNode.SkillExecutionTask.StartTime.Should().Be(10.0);
        modifiedNode.SkillExecutionTask.Duration.Should().Be(3.0);
    }

    [Fact]
    public async Task ExecutionPublish_SubscriptionSeesModifiedNodeTiming()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var modifiedNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA
                                           && se.SkillExecutionTask.Name == "A_SkillA1")
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 10.0, Duration = 3.0 }
                };
            return n;
        }).ToList();

        _executionEventPublisher.PublishNodeChanges(modifiedNodes);

        var subResult = await _nodeService.Nodes.FirstAsync();
        var modifiedNode = subResult.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "A_SkillA1");

        modifiedNode.SkillExecutionTask.StartTime.Should().Be(10.0);
        modifiedNode.SkillExecutionTask.Duration.Should().Be(3.0);
    }

    [Fact]
    public async Task ExecutionPublish_QueryAndSubscriptionSeeIdenticalModifiedState()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Simulate execution: modify timing of all nodes in procedure A
        var modifiedNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA)
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with
                    {
                        StartTime = se.SkillExecutionTask.StartTime + 5.0
                    }
                };
            return n;
        }).ToList();

        _executionEventPublisher.PublishNodeChanges(modifiedNodes);

        var queryNodes = await _nodeService.GetAllNodesAsync();
        var subNodes = await _nodeService.Nodes.FirstAsync();

        // Verify both paths return the same node set
        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subNodes.Select(n => n.Id).OrderBy(id => id));

        // Verify both paths see the same timing modifications
        foreach (var queryNode in queryNodes.OfType<SkillExecutionNode>())
        {
            var subNode = subNodes.OfType<SkillExecutionNode>()
                .Single(n => n.Id == queryNode.Id);
            queryNode.SkillExecutionTask.StartTime
                .Should().Be(subNode.SkillExecutionTask.StartTime);
            queryNode.SkillExecutionTask.Duration
                .Should().Be(subNode.SkillExecutionTask.Duration);
        }
    }

    [Fact]
    public async Task ExecutionPublish_EdgeUpdateVisibleInBothPaths()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Add a new edge during execution
        var newEdge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureIdA,
            SourceId = _allNodes.First(n => n.ProcedureId == _procedureIdA).Id,
            TargetId = _allNodes.Last(n => n.ProcedureId == _procedureIdA).Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var updatedEdges = _allEdges.Append(newEdge).ToList();

        _executionEventPublisher.PublishEdgeChanges(updatedEdges);

        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        queryEdges.Should().Contain(e => e.Id == newEdge.Id);
        subEdges.Should().Contain(e => e.Id == newEdge.Id);

        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subEdges.Select(e => e.Id).OrderBy(id => id));
    }

    #endregion

    #region Post-Execution Refresh

    [Fact]
    public async Task RefreshFromRepository_QueryRevertsToPersistedState()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Simulate execution: push modified data
        var modifiedNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA
                                           && se.SkillExecutionTask.Name == "A_SkillA1")
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 99.0 }
                };
            return n;
        }).ToList();
        _executionEventPublisher.PublishNodeChanges(modifiedNodes);

        // Verify the modification is visible
        var queryBefore = await _nodeService.GetAllNodesAsync();
        queryBefore.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "A_SkillA1")
            .SkillExecutionTask.StartTime.Should().Be(99.0);

        // Refresh from repository (mock returns the original _allNodes)
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Now the query should return the original persisted state
        var queryAfter = await _nodeService.GetAllNodesAsync();
        queryAfter.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "A_SkillA1")
            .SkillExecutionTask.StartTime.Should().Be(0.0,
                "after refresh, data should match repository (original timing)");
    }

    [Fact]
    public async Task RefreshFromRepository_SubscriptionRevertsToPersistedState()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Push modified data
        var modifiedNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA
                                           && se.SkillExecutionTask.Name == "A_SkillA1")
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 99.0 }
                };
            return n;
        }).ToList();
        _executionEventPublisher.PublishNodeChanges(modifiedNodes);

        // Refresh
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        var subResult = await _nodeService.Nodes.FirstAsync();
        subResult.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "A_SkillA1")
            .SkillExecutionTask.StartTime.Should().Be(0.0);
    }

    [Fact]
    public async Task RefreshFromRepository_QueryAndSubscriptionMatchAfterRefresh()
    {
        await LoadProcedureAndDrainAsync(_procedureIdA);

        // Push modified data
        _executionEventPublisher.PublishNodeChanges(
            _allNodes.Select(n =>
            {
                if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA)
                    return se with
                    {
                        SkillExecutionTask = se.SkillExecutionTask with { StartTime = 999.0 }
                    };
                return n;
            }).ToList());

        _executionEventPublisher.PublishEdgeChanges(new List<DependencyEdge>()); // clear edges

        // Refresh restores from mock repos
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        var queryNodes = await _nodeService.GetAllNodesAsync();
        var subNodes = await _nodeService.Nodes.FirstAsync();
        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subNodes.Select(n => n.Id).OrderBy(id => id));

        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(subEdges.Select(e => e.Id).OrderBy(id => id));

        // Edges should be restored (not empty)
        queryEdges.Where(e => e.ProcedureId == _procedureIdA)
            .Should().NotBeEmpty();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task FullLifecycle_Load_Execute_Refresh_Switch_Unload()
    {
        // Phase 1: Load procedure A
        await LoadProcedureAndDrainAsync(_procedureIdA);

        var nodesA = await _nodeService.GetAllNodesAsync();
        nodesA.Should().OnlyContain(n => n.ProcedureId == _procedureIdA);

        // Phase 2: Simulate execution – modify timing
        var executionNodes = _allNodes.Select(n =>
        {
            if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA)
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 50.0 }
                };
            return n;
        }).ToList();
        _executionEventPublisher.PublishNodeChanges(executionNodes);

        var duringExecution = await _nodeService.GetAllNodesAsync();
        duringExecution.OfType<SkillExecutionNode>()
            .Should().OnlyContain(se => se.SkillExecutionTask.StartTime == 50.0);

        // Phase 3: Execution ends – refresh from repository
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        var afterRefresh = await _nodeService.GetAllNodesAsync();
        afterRefresh.OfType<SkillExecutionNode>()
            .Where(se => se.ProcedureId == _procedureIdA)
            .Should().OnlyContain(se => se.SkillExecutionTask.StartTime == 0.0,
                "repository holds the original timing");

        // Phase 4: Switch to procedure B
        await LoadProcedureAndDrainAsync(_procedureIdB);

        var nodesB = await _nodeService.GetAllNodesAsync();
        nodesB.Should().OnlyContain(n => n.ProcedureId == _procedureIdB);

        // Phase 5: Unload
        await _procedureOrchestrator.UnloadCurrentProcedureAsync();

        var nodesAfterUnload = await _nodeService.GetAllNodesAsync();
        nodesAfterUnload.Should().BeEmpty();

        // Subscription should match at each step
        var subNodesAfterUnload = await _nodeService.Nodes.FirstAsync();
        subNodesAfterUnload.Should().BeEmpty();
    }

    [Fact]
    public async Task FullLifecycle_SubscriptionTracksAllPhases()
    {
        var nodeEmissions = new List<IReadOnlyList<Node>>();
        using var subscription = _nodeService.Nodes.Subscribe(n => nodeEmissions.Add(n));
        await Task.Yield();

        // Initial state: no procedure loaded → empty
        nodeEmissions.Last().Should().BeEmpty();

        // Load procedure A
        await LoadProcedureAndDrainAsync(_procedureIdA);
        await Task.Yield();

        nodeEmissions.Last().Should().OnlyContain(n => n.ProcedureId == _procedureIdA);
        var countAfterLoad = nodeEmissions.Last().Count;
        countAfterLoad.Should().BeGreaterThan(0);

        // Execution: push modified data
        _executionEventPublisher.PublishNodeChanges(
            _allNodes.Select(n =>
            {
                if (n is SkillExecutionNode se && se.ProcedureId == _procedureIdA)
                    return se with
                    {
                        SkillExecutionTask = se.SkillExecutionTask with { Duration = 999.0 }
                    };
                return n;
            }).ToList());
        await Task.Yield();

        nodeEmissions.Last().OfType<SkillExecutionNode>()
            .Where(n => n.ProcedureId == _procedureIdA)
            .Should().OnlyContain(n => n.SkillExecutionTask.Duration == 999.0);

        // Refresh restores original
        await _executionEventPublisher.RefreshChangeTrackersFromRepositoryAsync();
        await Task.Yield();

        nodeEmissions.Last().OfType<SkillExecutionNode>()
            .Where(n => n.ProcedureId == _procedureIdA)
            .Should().OnlyContain(n => n.SkillExecutionTask.Duration == 0.5,
                "repository holds original duration");

        // Switch procedure
        await LoadProcedureAndDrainAsync(_procedureIdB);
        await Task.Yield();

        nodeEmissions.Last().Should().OnlyContain(n => n.ProcedureId == _procedureIdB);

        // Unload
        await _procedureOrchestrator.UnloadCurrentProcedureAsync();
        await Task.Yield();

        nodeEmissions.Last().Should().BeEmpty();

        // We should have received multiple emissions throughout the lifecycle
        nodeEmissions.Count.Should().BeGreaterThan(3);
    }

    #endregion

    #region Helpers

    private void SetupProcedureRepo()
    {
        var procedureA = new Procedure
        {
            Id = _procedureIdA,
            Name = "ProcedureA",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = Array.Empty<Guid>()
        };
        var procedureB = new Procedure
        {
            Id = _procedureIdB,
            Name = "ProcedureB",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = Array.Empty<Guid>()
        };

        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureIdA)).ReturnsAsync(procedureA);
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureIdB)).ReturnsAsync(procedureB);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<Procedure>()))
            .ReturnsAsync(true);
    }

    /// <summary>
    ///     Loads a procedure through the orchestrator and waits for the unified tracker's
    ///     fire-and-forget scoped load to complete, so subsequent query and subscription
    ///     assertions observe the loaded state deterministically.
    /// </summary>
    private async Task LoadProcedureAndDrainAsync(Guid procedureId)
    {
        await _procedureOrchestrator.LoadProcedureAsync(procedureId);
        for (var i = 0; i < 500 && !_tracker.IsInitialized; i++)
            await Task.Delay(1);
    }

    private static (List<Node> Nodes, List<DependencyEdge> Edges) BuildHierarchy(
        Guid procedureId, string prefix)
    {
        var agentId = Guid.NewGuid();
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = $"{prefix}_TestSkill",
            Description = "Test",
            Properties = []
        };

        var routerNodeId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var skillA1Id = Guid.NewGuid();
        var skillA2Id = Guid.NewGuid();
        var standaloneId = Guid.NewGuid();

        var pos = new NodePosition { X = 0, Y = 0 };

        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = procedureId,
            Position = pos,
            RouterTask = new RouterTask
            {
                Name = $"{prefix}_Decision",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "branch_var" },
                Branches =
                [
                    new ConditionalBranch
                    {
                        Name = "BranchA",
                        Condition = "value == 'A'",
                        Priority = 1,
                        TargetNodeId = branchAId
                    }
                ]
            }
        };

        var branchANode = new TaskNode
        {
            Id = branchAId,
            ProcedureId = procedureId,
            ParentId = routerNodeId,
            Position = pos,
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = $"{prefix}_BranchA",
                StartTime = 0,
                Duration = 1.0
            }
        };

        var skillA1 = new SkillExecutionNode
        {
            Id = skillA1Id,
            ProcedureId = procedureId,
            ParentId = branchAId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"{prefix}_SkillA1",
                StartTime = 0,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var skillA2 = new SkillExecutionNode
        {
            Id = skillA2Id,
            ProcedureId = procedureId,
            ParentId = branchAId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"{prefix}_SkillA2",
                StartTime = 0,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var standalone = new SkillExecutionNode
        {
            Id = standaloneId,
            ProcedureId = procedureId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"{prefix}_Standalone",
                StartTime = 0,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var nodes = new List<Node> { routerNode, branchANode, skillA1, skillA2, standalone };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = procedureId,
                SourceId = skillA1Id,
                TargetId = skillA2Id,
                SourceHandle = "right",
                TargetHandle = "left"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = procedureId,
                SourceId = routerNodeId,
                TargetId = standaloneId,
                SourceHandle = "right",
                TargetHandle = "left"
            }
        };

        return (nodes, edges);
    }

    private static string GetNodeName(Node node)
    {
        return node switch
        {
            RouterNode rn => rn.RouterTask.Name,
            TaskNode tn => tn.Task.Name,
            SkillExecutionNode se => se.SkillExecutionTask.Name,
            _ => node.Id.ToString()
        };
    }

    #endregion
}