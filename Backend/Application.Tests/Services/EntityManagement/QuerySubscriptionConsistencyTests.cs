using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement;

/// <summary>
///     Verifies that query methods and subscription observables return identical data
///     by using a real ProcedureStateTracker so both code paths read from the same
///     underlying BehaviorSubject.
/// </summary>
public sealed class QuerySubscriptionConsistencyTests : IAsyncLifetime
{
    private readonly ProcedureStateTracker _tracker;
    private readonly IDependencyEdgeChangeTracker _edgeChangeTracker;
    private readonly DependencyEdgeApplicationService _edgeService;

    private readonly INodeChangeTracker _nodeChangeTracker;

    private readonly NodeApplicationService _nodeService;
    private readonly Guid _otherProcedureId = Guid.NewGuid();

    private readonly BehaviorSubject<Guid?> _procedureChanges;
    private readonly Guid _procedureId = Guid.NewGuid();

    public QuerySubscriptionConsistencyTests()
    {
        // The unified tracker loads scoped data on procedure load; this file drives data directly
        // through UpdateEntities, so the scoped repository queries return empty.
        var procedureRepo = new Mock<IProcedureRepository>();
        procedureRepo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId)).ReturnsAsync(new List<Node>());
        procedureRepo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId)).ReturnsAsync(new List<DependencyEdge>());

        _tracker = new ProcedureStateTracker(
            procedureRepo.Object,
            new Mock<ILogger<ProcedureStateTracker>>().Object);
        _nodeChangeTracker = _tracker;
        _edgeChangeTracker = _tracker;

        _procedureChanges = new BehaviorSubject<Guid?>(_procedureId);
        var mockProcedureContext = new Mock<IProcedureContext>();
        mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId);
        mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(_procedureId);
        mockProcedureContext.Setup(x => x.ProcedureChanges).Returns(_procedureChanges);

        var schedulingOptions = Options.Create(new SchedulingConfiguration
        {
            Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
        });

        _nodeService = new NodeApplicationService(
            procedureRepo.Object,
            new Mock<ICrudSchedulingOrchestrator>().Object,
            _tracker,
            mockProcedureContext.Object,
            new Mock<IProcedureVariableService>().Object,
            schedulingOptions,
            new Mock<ILogger<NodeApplicationService>>().Object);

        _edgeService = new DependencyEdgeApplicationService(
            new Mock<ICrudSchedulingOrchestrator>().Object,
            _tracker,
            mockProcedureContext.Object,
            new Mock<ILogger<DependencyEdgeApplicationService>>().Object);
    }

    /// <summary>
    ///     Loads the procedure so the tracker accepts writes, then drains the fire-and-forget
    ///     scoped (empty) load so its emission lands before any test body pumps data via
    ///     UpdateEntities — otherwise a late empty emission would clobber the pumped state.
    /// </summary>
    public async Task InitializeAsync()
    {
        ((IProcedureStateScope)_tracker).OnProcedureLoaded(_procedureId);
        for (var i = 0; i < 500 && !_tracker.IsInitialized; i++)
            await Task.Delay(1);
    }

    public Task DisposeAsync()
    {
        _tracker.Dispose();
        _procedureChanges.Dispose();
        return Task.CompletedTask;
    }

    #region Hierarchy Builder

    private (List<Node> Nodes, List<DependencyEdge> Edges) BuildHierarchy(Guid procedureId)
    {
        var agentId = Guid.NewGuid();
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
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
                Name = "Decision",
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
                Name = "BranchA",
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
                Name = "SkillA1",
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
                Name = "SkillA2",
                StartTime = 0.5,
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
                Name = "Standalone",
                StartTime = 1.0,
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

    #endregion

    #region Node Query vs Subscription

    [Fact]
    public async Task Nodes_QueryAndSubscriptionReturnSameData_AfterInitialLoad()
    {
        // Arrange – pump a hierarchy into the real change tracker
        var (nodes, _) = BuildHierarchy(_procedureId);
        _nodeChangeTracker.UpdateEntities(nodes);

        // Act – query path
        var queryResult = await _nodeService.GetAllNodesAsync();

        // Act – subscription path (emits immediately because BehaviorSubject + StartWith)
        var subscriptionResult = await _nodeService.Nodes.FirstAsync();

        // Assert – both return the same set of node IDs
        var queryIds = queryResult.Select(n => n.Id).OrderBy(id => id).ToList();
        var subIds = subscriptionResult.Select(n => n.Id).OrderBy(id => id).ToList();

        subIds.Should().BeEquivalentTo(queryIds,
            "subscription and query must return the same nodes from the same BehaviorSubject");
    }

    [Fact]
    public async Task Nodes_QueryAndSubscriptionReturnSameData_AfterUpdate()
    {
        // Arrange – load initial data
        var (nodes, _) = BuildHierarchy(_procedureId);
        _nodeChangeTracker.UpdateEntities(nodes);

        // Capture subscription emissions
        var emissions = new List<IReadOnlyList<Node>>();
        using var subscription = _nodeService.Nodes.Subscribe(n => emissions.Add(n));

        // Act – update: remove the standalone node (simulate execution change)
        var updatedNodes = nodes.Where(n =>
        {
            if (n is SkillExecutionNode se) return se.SkillExecutionTask.Name != "Standalone";
            return true;
        }).ToList();
        _nodeChangeTracker.UpdateEntities(updatedNodes);

        // Allow the observable pipeline to process
        await Task.Yield();

        // Query after update
        var queryResult = await _nodeService.GetAllNodesAsync();

        // The latest subscription emission
        var latestSubscription = emissions.Last();

        // Assert
        queryResult.Should().HaveCount(4, "standalone was removed");
        latestSubscription.Should().HaveCount(4, "subscription should see the same removal");

        var queryIds = queryResult.Select(n => n.Id).OrderBy(id => id).ToList();
        var subIds = latestSubscription.Select(n => n.Id).OrderBy(id => id).ToList();
        subIds.Should().BeEquivalentTo(queryIds);
    }

    [Fact]
    public async Task Nodes_QueryAndSubscriptionBothApplyProcedureFilter()
    {
        // Arrange – mix nodes from two procedures
        var (ourNodes, _) = BuildHierarchy(_procedureId);
        var foreignNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _otherProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Foreign",
                StartTime = 0,
                Duration = 1
            }
        };
        var allNodes = ourNodes.Append(foreignNode).ToList();
        _nodeChangeTracker.UpdateEntities(allNodes);

        // Act
        var queryResult = await _nodeService.GetAllNodesAsync();
        var subscriptionResult = await _nodeService.Nodes.FirstAsync();

        // Assert – both should filter to only our procedure's nodes
        queryResult.Should().HaveCount(5);
        subscriptionResult.Should().HaveCount(5);

        queryResult.Should().NotContain(n => n.ProcedureId == _otherProcedureId);
        subscriptionResult.Should().NotContain(n => n.ProcedureId == _otherProcedureId);

        queryResult.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                subscriptionResult.Select(n => n.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task Nodes_QueryAndSubscriptionBothReturnEmpty_WhenNoDataPumped()
    {
        // Act – no UpdateEntities called, the change tracker starts with empty from repository mock
        var queryResult = await _nodeService.GetAllNodesAsync();
        var subscriptionResult = await _nodeService.Nodes.FirstAsync();

        // Assert
        queryResult.Should().BeEmpty();
        subscriptionResult.Should().BeEmpty();
    }

    [Fact]
    public async Task Nodes_SubscriptionReceivesMultipleUpdates_AllMatchingQueries()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);

        var emissions = new List<IReadOnlyList<Node>>();
        using var subscription = _nodeService.Nodes.Subscribe(n => emissions.Add(n));

        // Push initial empty (already emitted from BehaviorSubject)
        await Task.Yield();

        // Push first update
        _nodeChangeTracker.UpdateEntities(nodes);
        await Task.Yield();

        var queryAfterFirst = await _nodeService.GetAllNodesAsync();
        queryAfterFirst.Should().HaveCount(5);
        emissions.Last().Should().HaveCount(5);

        // Push second update – add one more node
        var extraNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Extra",
                StartTime = 0,
                Duration = 1
            }
        };
        _nodeChangeTracker.UpdateEntities(nodes.Append(extraNode).ToList());
        await Task.Yield();

        var queryAfterSecond = await _nodeService.GetAllNodesAsync();
        queryAfterSecond.Should().HaveCount(6);
        emissions.Last().Should().HaveCount(6);

        queryAfterSecond.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                emissions.Last().Select(n => n.Id).OrderBy(id => id));
    }

    #endregion

    #region Edge Query vs Subscription

    [Fact]
    public async Task Edges_QueryAndSubscriptionReturnSameData_AfterInitialLoad()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        _edgeChangeTracker.UpdateEntities(edges);

        var queryResult = await _edgeService.GetAllDependencyEdgesAsync();
        var subscriptionResult = await _edgeService.DependencyEdges.FirstAsync();

        var queryIds = queryResult.Select(e => e.Id).OrderBy(id => id).ToList();
        var subIds = subscriptionResult.Select(e => e.Id).OrderBy(id => id).ToList();

        subIds.Should().BeEquivalentTo(queryIds,
            "subscription and query must return the same edges from the same BehaviorSubject");
    }

    [Fact]
    public async Task Edges_QueryAndSubscriptionReturnSameData_AfterUpdate()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        _edgeChangeTracker.UpdateEntities(edges);

        var emissions = new List<IReadOnlyList<DependencyEdge>>();
        using var subscription = _edgeService.DependencyEdges.Subscribe(e => emissions.Add(e));

        // Remove one edge
        var updatedEdges = edges.Take(1).ToList();
        _edgeChangeTracker.UpdateEntities(updatedEdges);
        await Task.Yield();

        var queryResult = await _edgeService.GetAllDependencyEdgesAsync();
        var latestSubscription = emissions.Last();

        queryResult.Should().HaveCount(1);
        latestSubscription.Should().HaveCount(1);

        queryResult.Single().Id.Should().Be(latestSubscription.Single().Id);
    }

    [Fact]
    public async Task Edges_QueryAndSubscriptionBothApplyProcedureFilter()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        var foreignEdge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _otherProcedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        _edgeChangeTracker.UpdateEntities(edges.Append(foreignEdge).ToList());

        var queryResult = await _edgeService.GetAllDependencyEdgesAsync();
        var subscriptionResult = await _edgeService.DependencyEdges.FirstAsync();

        queryResult.Should().HaveCount(2);
        subscriptionResult.Should().HaveCount(2);

        queryResult.Should().NotContain(e => e.ProcedureId == _otherProcedureId);
        subscriptionResult.Should().NotContain(e => e.ProcedureId == _otherProcedureId);

        queryResult.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                subscriptionResult.Select(e => e.Id).OrderBy(id => id));
    }

    #endregion

    #region Combined Node + Edge Consistency

    [Fact]
    public async Task NodesAndEdges_QueryAndSubscriptionReturnConsistentSnapshot()
    {
        var (nodes, edges) = BuildHierarchy(_procedureId);
        _nodeChangeTracker.UpdateEntities(nodes);
        _edgeChangeTracker.UpdateEntities(edges);

        // Query path
        var queryNodes = await _nodeService.GetAllNodesAsync();
        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();

        // Subscription path
        var subNodes = await _nodeService.Nodes.FirstAsync();
        var subEdges = await _edgeService.DependencyEdges.FirstAsync();

        // Both paths should agree on nodes
        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                subNodes.Select(n => n.Id).OrderBy(id => id));

        // Both paths should agree on edges
        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                subEdges.Select(e => e.Id).OrderBy(id => id));

        // Cross-check: all edge endpoints should reference nodes returned by both paths
        var nodeIds = queryNodes.Select(n => n.Id).ToHashSet();
        queryEdges.Should().AllSatisfy(e =>
        {
            nodeIds.Should().Contain(e.SourceId);
            nodeIds.Should().Contain(e.TargetId);
        });
    }

    [Fact]
    public async Task NodesAndEdges_BothPathsReflectSimultaneousUpdate()
    {
        // Start with initial data
        var (nodes, edges) = BuildHierarchy(_procedureId);
        _nodeChangeTracker.UpdateEntities(nodes);
        _edgeChangeTracker.UpdateEntities(edges);

        // Collect subscription emissions
        var nodeEmissions = new List<IReadOnlyList<Node>>();
        var edgeEmissions = new List<IReadOnlyList<DependencyEdge>>();
        using var nodeSub = _nodeService.Nodes.Subscribe(n => nodeEmissions.Add(n));
        using var edgeSub = _edgeService.DependencyEdges.Subscribe(e => edgeEmissions.Add(e));
        await Task.Yield();

        // Simulate an execution update: modify a node's timing and push both
        var modifiedNodes = nodes.Select(n =>
        {
            if (n is SkillExecutionNode { SkillExecutionTask.Name: "SkillA1" } se)
                return se with
                {
                    SkillExecutionTask = se.SkillExecutionTask with { StartTime = 0.1, Duration = 0.3 }
                };
            return n;
        }).ToList();

        _nodeChangeTracker.UpdateEntities(modifiedNodes);
        _edgeChangeTracker.UpdateEntities(edges); // edges unchanged but re-pushed
        await Task.Yield();

        // Query after update
        var queryNodes = await _nodeService.GetAllNodesAsync();
        var queryEdges = await _edgeService.GetAllDependencyEdgesAsync();

        // Latest subscription emission
        var latestSubNodes = nodeEmissions.Last();
        var latestSubEdges = edgeEmissions.Last();

        // Node IDs should match
        queryNodes.Select(n => n.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                latestSubNodes.Select(n => n.Id).OrderBy(id => id));

        // The modified timing should be visible in both paths
        var querySkillA1 = queryNodes.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "SkillA1");
        var subSkillA1 = latestSubNodes.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "SkillA1");

        querySkillA1.SkillExecutionTask.StartTime.Should().Be(0.1);
        subSkillA1.SkillExecutionTask.StartTime.Should().Be(0.1);
        querySkillA1.SkillExecutionTask.Duration.Should().Be(0.3);
        subSkillA1.SkillExecutionTask.Duration.Should().Be(0.3);

        // Edge IDs should match
        queryEdges.Select(e => e.Id).OrderBy(id => id)
            .Should().BeEquivalentTo(
                latestSubEdges.Select(e => e.Id).OrderBy(id => id));
    }

    #endregion
}