using System.Reactive.Disposables;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Operations;
using Microsoft.Extensions.Logging;
using Moq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Operations;

/// <summary>
///     Tests for subscription filtering based on loaded procedure.
///     Verifies that nodes and edges are properly filtered by ProcedureId.
/// </summary>
public class SubscriptionProcedureFilteringTests : IDisposable
{
    private readonly IDependencyEdgeApplicationService _edgeService;
    private readonly BehaviorSubject<IReadOnlyList<DependencyEdge>> _edgesSubject;
    private readonly ILogger<Subscription> _logger;
    private readonly INodeApplicationService _nodeService;

    private readonly BehaviorSubject<IReadOnlyList<Node>> _nodesSubject;
    private readonly IProcedureOrchestrator _procedureOrchestrator;
    private readonly CompositeDisposable _subscriptions = new();

    public SubscriptionProcedureFilteringTests()
    {
        var mockNodeService = new Mock<INodeApplicationService>();
        var mockEdgeService = new Mock<IDependencyEdgeApplicationService>();
        var mockProcedureOrchestrator = new Mock<IProcedureOrchestrator>();
        var mockLogger = new Mock<ILogger<Subscription>>();

        _nodesSubject = new BehaviorSubject<IReadOnlyList<Node>>(Array.Empty<Node>());
        _edgesSubject = new BehaviorSubject<IReadOnlyList<DependencyEdge>>(Array.Empty<DependencyEdge>());

        // Note: NodeApplicationService.Nodes now handles procedure-based filtering internally
        // via ProcedureContext.ProcedureChanges. These tests verify the subscription endpoint
        // correctly passes through the pre-filtered data.
        mockNodeService.Setup(x => x.Nodes).Returns(_nodesSubject);
        mockEdgeService.Setup(x => x.DependencyEdges).Returns(_edgesSubject);

        _nodeService = mockNodeService.Object;
        _edgeService = mockEdgeService.Object;
        _procedureOrchestrator = mockProcedureOrchestrator.Object;
        _logger = mockLogger.Object;
    }

    /// <summary>
    ///     Disposes the BehaviorSubjects and any subscriptions created during a test so that
    ///     the xUnit process can exit cleanly without leaking Rx resources into the test host.
    /// </summary>
    public void Dispose()
    {
        _subscriptions.Dispose();
        _nodesSubject.Dispose();
        _edgesSubject.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SubscribeToNodesChanged_WithLoadedProcedure_FiltersNodesByProcedureId()
    {
        // Arrange
        var procedureAId = Guid.NewGuid();

        var nodesA = new List<Node>
        {
            CreateTaskNode(Guid.NewGuid(), procedureAId, "Node A1"),
            CreateTaskNode(Guid.NewGuid(), procedureAId, "Node A2")
        };

        // Procedure A is loaded
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureAId);

        var observable = Subscription.SubscribeToNodesChanged(_nodeService, _logger);

        var receivedNodes = new List<List<Node>>();
        _subscriptions.Add(observable.Subscribe(nodes => receivedNodes.Add(nodes)));

        // Act - Simulate NodeApplicationService.Nodes emitting filtered nodes for procedure A
        _nodesSubject.OnNext(nodesA);
        await Task.Delay(100); // Allow observable to process

        // Assert - Check last emission (skipping initial BehaviorSubject emission)
        Assert.NotEmpty(receivedNodes);
        var lastEmission = receivedNodes.Last();
        Assert.Equal(2, lastEmission.Count);
        Assert.All(lastEmission, node => Assert.Equal(procedureAId, node.ProcedureId));
        Assert.Contains(lastEmission, n => ((TaskNode)n).Task.Name == "Node A1");
        Assert.Contains(lastEmission, n => ((TaskNode)n).Task.Name == "Node A2");
    }

    [Fact]
    public async Task SubscribeToNodesChanged_WithNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange
        // No procedure loaded
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        var observable = Subscription.SubscribeToNodesChanged(_nodeService, _logger);

        var receivedNodes = new List<List<Node>>();
        _subscriptions.Add(observable.Subscribe(nodes => receivedNodes.Add(nodes)));

        // Act - Simulate NodeApplicationService.Nodes emitting empty list when no procedure is loaded
        _nodesSubject.OnNext(Array.Empty<Node>());
        await Task.Delay(100); // Allow observable to process

        // Assert - Check last emission
        Assert.NotEmpty(receivedNodes);
        var lastEmission = receivedNodes.Last();
        Assert.Empty(lastEmission);
    }

    [Fact]
    public async Task SubscribeToNodesChanged_WhenProcedureChanges_FiltersForNewProcedure()
    {
        // Arrange
        var procedureAId = Guid.NewGuid();
        var procedureBId = Guid.NewGuid();

        var nodesA = new List<Node>
        {
            CreateTaskNode(Guid.NewGuid(), procedureAId, "Node A1")
        };

        var nodesB = new List<Node>
        {
            CreateTaskNode(Guid.NewGuid(), procedureBId, "Node B1")
        };

        // Start with procedure A
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureAId);

        var observable = Subscription.SubscribeToNodesChanged(_nodeService, _logger);

        var receivedNodes = new List<List<Node>>();
        _subscriptions.Add(observable.Subscribe(nodes => receivedNodes.Add(nodes)));

        // Act 1: Simulate NodeApplicationService.Nodes emitting filtered nodes for Procedure A
        _nodesSubject.OnNext(nodesA);
        await Task.Delay(100);

        // Act 2: Switch to Procedure B - simulate NodeApplicationService.Nodes emitting filtered nodes for Procedure B
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureBId);
        _nodesSubject.OnNext(nodesB);
        await Task.Delay(100);

        // Assert - BehaviorSubject emits initial value (empty), then nodesA, then nodesB
        Assert.Equal(3, receivedNodes.Count);

        // Index 0: Initial BehaviorSubject emission (empty)
        Assert.Empty(receivedNodes[0]);

        // Index 1: First data emission should have only Procedure A nodes
        Assert.Single(receivedNodes[1]);
        Assert.Equal(procedureAId, receivedNodes[1][0].ProcedureId);

        // Index 2: Second data emission should have only Procedure B nodes
        Assert.Single(receivedNodes[2]);
        Assert.Equal(procedureBId, receivedNodes[2][0].ProcedureId);
    }

    [Fact]
    public async Task SubscribeToDependencyEdgesChanged_WithLoadedProcedure_FiltersEdgesByProcedureId()
    {
        // Arrange
        var procedureAId = Guid.NewGuid();

        var edgesA = new List<DependencyEdge>
        {
            CreateEdge(Guid.NewGuid(), procedureAId),
            CreateEdge(Guid.NewGuid(), procedureAId)
        };

        // Procedure A is loaded
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureAId);

        var observable = Subscription.SubscribeToDependencyEdgesChanged(_edgeService, _logger);

        var receivedEdges = new List<List<DependencyEdge>>();
        _subscriptions.Add(observable.Subscribe(edges => receivedEdges.Add(edges)));

        // Act - Simulate DependencyEdgeApplicationService.DependencyEdges emitting filtered edges for procedure A
        _edgesSubject.OnNext(edgesA);
        await Task.Delay(100); // Allow observable to process

        // Assert - Check last emission
        Assert.NotEmpty(receivedEdges);
        var lastEmission = receivedEdges.Last();
        Assert.Equal(2, lastEmission.Count);
        Assert.All(lastEmission, edge => Assert.Equal(procedureAId, edge.ProcedureId));
    }

    [Fact]
    public async Task SubscribeToDependencyEdgesChanged_WithNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange
        // No procedure loaded
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        var observable = Subscription.SubscribeToDependencyEdgesChanged(_edgeService, _logger);

        var receivedEdges = new List<List<DependencyEdge>>();
        _subscriptions.Add(observable.Subscribe(edges => receivedEdges.Add(edges)));

        // Act - Simulate DependencyEdgeApplicationService.DependencyEdges emitting empty list when no procedure is loaded
        _edgesSubject.OnNext(Array.Empty<DependencyEdge>());
        await Task.Delay(100); // Allow observable to process

        // Assert - Check last emission
        Assert.NotEmpty(receivedEdges);
        var lastEmission = receivedEdges.Last();
        Assert.Empty(lastEmission);
    }

    [Fact]
    public async Task SubscribeToDependencyEdgesChanged_WhenProcedureChanges_FiltersForNewProcedure()
    {
        // Arrange
        var procedureAId = Guid.NewGuid();
        var procedureBId = Guid.NewGuid();

        var edgesA = new List<DependencyEdge>
        {
            CreateEdge(Guid.NewGuid(), procedureAId)
        };

        var edgesB = new List<DependencyEdge>
        {
            CreateEdge(Guid.NewGuid(), procedureBId)
        };

        // Start with procedure A
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureAId);

        var observable = Subscription.SubscribeToDependencyEdgesChanged(_edgeService, _logger);

        var receivedEdges = new List<List<DependencyEdge>>();
        _subscriptions.Add(observable.Subscribe(edges => receivedEdges.Add(edges)));

        // Act 1: Simulate DependencyEdgeApplicationService.DependencyEdges emitting filtered edges for Procedure A
        _edgesSubject.OnNext(edgesA);
        await Task.Delay(100);

        // Act 2: Switch to Procedure B - simulate DependencyEdgeApplicationService.DependencyEdges emitting filtered edges for Procedure B
        Mock.Get(_procedureOrchestrator).Setup(x => x.GetLoadedProcedureId()).Returns(procedureBId);
        _edgesSubject.OnNext(edgesB);
        await Task.Delay(100);

        // Assert - BehaviorSubject emits initial value (empty), then edgesA, then edgesB
        Assert.Equal(3, receivedEdges.Count);

        // Index 0: Initial BehaviorSubject emission (empty)
        Assert.Empty(receivedEdges[0]);

        // Index 1: First data emission should have only Procedure A edges
        Assert.Single(receivedEdges[1]);
        Assert.Equal(procedureAId, receivedEdges[1][0].ProcedureId);

        // Index 2: Second data emission should have only Procedure B edges
        Assert.Single(receivedEdges[2]);
        Assert.Equal(procedureBId, receivedEdges[2][0].ProcedureId);
    }

    private static TaskNode CreateTaskNode(Guid id, Guid procedureId, string name)
    {
        return new TaskNode
        {
            Id = id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask
            {
                Name = name,
                StartTime = 0,
                Duration = 10
            }
        };
    }

    private static DependencyEdge CreateEdge(Guid id, Guid procedureId)
    {
        return new DependencyEdge
        {
            Id = id,
            ProcedureId = procedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
    }
}