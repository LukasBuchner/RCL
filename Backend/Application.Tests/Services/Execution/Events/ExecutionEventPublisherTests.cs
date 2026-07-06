using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Events;

/// <summary>
///     Unit tests for ExecutionEventPublisher.
///     Tests event publication and observable stream management.
/// </summary>
public class ExecutionEventPublisherTests
{
    private readonly ExecutionEventPublisher _eventPublisher;
    private readonly Mock<IDependencyEdgeChangeTracker> _mockEdgeChangeTracker;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;

    public ExecutionEventPublisherTests()
    {
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockEdgeChangeTracker = new Mock<IDependencyEdgeChangeTracker>();
        _eventPublisher = new ExecutionEventPublisher(
            _mockNodeChangeTracker.Object,
            _mockEdgeChangeTracker.Object,
            NullLogger<ExecutionEventPublisher>.Instance);
    }

    #region RefreshChangeTrackersFromRepositoryAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task RefreshChangeTrackersFromRepositoryAsync_RefreshesBothTrackers()
    {
        // Arrange
        _mockNodeChangeTracker.Setup(t => t.RefreshFromRepositoryAsync())
            .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockEdgeChangeTracker.Setup(t => t.RefreshFromRepositoryAsync())
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        await _eventPublisher.RefreshChangeTrackersFromRepositoryAsync();

        // Assert
        _mockNodeChangeTracker.Verify(t => t.RefreshFromRepositoryAsync(), Times.Once);
        _mockEdgeChangeTracker.Verify(t => t.RefreshFromRepositoryAsync(), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNodeChangeTracker_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExecutionEventPublisher(
                null!,
                _mockEdgeChangeTracker.Object,
                NullLogger<ExecutionEventPublisher>.Instance));
    }

    [Fact]
    public void Constructor_WithNullEdgeChangeTracker_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExecutionEventPublisher(
                _mockNodeChangeTracker.Object,
                null!,
                NullLogger<ExecutionEventPublisher>.Instance));
    }

    #endregion

    #region NodesChanged Observable Tests

    [Fact]
    public void NodesChanged_ReturnsObservableFromNodeChangeTracker()
    {
        // Arrange
        var expectedNodes = new List<Node> { CreateTaskNode("Test") }.AsReadOnly();
        var observable = Observable.Return(expectedNodes);
        _mockNodeChangeTracker.Setup(t => t.Nodes).Returns(observable);

        // Act
        var result = _eventPublisher.NodesChanged;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(observable, result);
    }

    [Fact]
    public void NodesChanged_PropagatesChangesFromTracker()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Node1") }.AsReadOnly();
        var nodeSubject = new BehaviorSubject<IReadOnlyList<Node>>(nodes);
        _mockNodeChangeTracker.Setup(t => t.Nodes).Returns(nodeSubject);

        // Act
        IReadOnlyList<Node>? receivedNodes = null;
        _eventPublisher.NodesChanged.Subscribe(n => receivedNodes = n);

        // Assert
        Assert.Equal(nodes, receivedNodes);
    }

    #endregion

    #region EdgesChanged Observable Tests

    [Fact]
    public void EdgesChanged_ReturnsObservableFromEdgeChangeTracker()
    {
        // Arrange
        var expectedEdges = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();
        var observable = Observable.Return(expectedEdges);
        _mockEdgeChangeTracker.Setup(t => t.Edges).Returns(observable);

        // Act
        var result = _eventPublisher.EdgesChanged;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(observable, result);
    }

    [Fact]
    public void EdgesChanged_PropagatesChangesFromTracker()
    {
        // Arrange
        var edges = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();
        var edgeSubject = new BehaviorSubject<IReadOnlyList<DependencyEdge>>(edges);
        _mockEdgeChangeTracker.Setup(t => t.Edges).Returns(edgeSubject);

        // Act
        IReadOnlyList<DependencyEdge>? receivedEdges = null;
        _eventPublisher.EdgesChanged.Subscribe(e => receivedEdges = e);

        // Assert
        Assert.Equal(edges, receivedEdges);
    }

    #endregion

    #region PublishNodeChanges Tests

    [Fact]
    public void PublishNodeChanges_WithValidNodes_UpdatesChangeTracker()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Test1"), CreateTaskNode("Test2") }.AsReadOnly();

        // Act
        _eventPublisher.PublishNodeChanges(nodes);

        // Assert
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(nodes), Times.Once);
    }

    [Fact]
    public void PublishNodeChanges_WithNullNodes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _eventPublisher.PublishNodeChanges(null!));
    }

    [Fact]
    public void PublishNodeChanges_WithEmptyList_UpdatesChangeTracker()
    {
        // Arrange
        var nodes = new List<Node>().AsReadOnly();

        // Act
        _eventPublisher.PublishNodeChanges(nodes);

        // Assert
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(nodes), Times.Once);
    }

    [Fact]
    public void PublishNodeChanges_CalledMultipleTimes_UpdatesEachTime()
    {
        // Arrange
        var nodes1 = new List<Node> { CreateTaskNode("Test1") }.AsReadOnly();
        var nodes2 = new List<Node> { CreateTaskNode("Test2") }.AsReadOnly();
        var nodes3 = new List<Node> { CreateTaskNode("Test3") }.AsReadOnly();

        // Act
        _eventPublisher.PublishNodeChanges(nodes1);
        _eventPublisher.PublishNodeChanges(nodes2);
        _eventPublisher.PublishNodeChanges(nodes3);

        // Assert
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(It.IsAny<IReadOnlyList<Node>>()), Times.Exactly(3));
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(nodes1), Times.Once);
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(nodes2), Times.Once);
        _mockNodeChangeTracker.Verify(t => t.UpdateEntities(nodes3), Times.Once);
    }

    #endregion

    #region PublishEdgeChanges Tests

    [Fact]
    public void PublishEdgeChanges_WithValidEdges_UpdatesChangeTracker()
    {
        // Arrange
        var edges = new List<DependencyEdge> { CreateDependencyEdge(), CreateDependencyEdge() }.AsReadOnly();

        // Act
        _eventPublisher.PublishEdgeChanges(edges);

        // Assert
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(edges), Times.Once);
    }

    [Fact]
    public void PublishEdgeChanges_WithNullEdges_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _eventPublisher.PublishEdgeChanges(null!));
    }

    [Fact]
    public void PublishEdgeChanges_WithEmptyList_UpdatesChangeTracker()
    {
        // Arrange
        var edges = new List<DependencyEdge>().AsReadOnly();

        // Act
        _eventPublisher.PublishEdgeChanges(edges);

        // Assert
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(edges), Times.Once);
    }

    [Fact]
    public void PublishEdgeChanges_CalledMultipleTimes_UpdatesEachTime()
    {
        // Arrange
        var edges1 = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();
        var edges2 = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();
        var edges3 = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();

        // Act
        _eventPublisher.PublishEdgeChanges(edges1);
        _eventPublisher.PublishEdgeChanges(edges2);
        _eventPublisher.PublishEdgeChanges(edges3);

        // Assert
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(It.IsAny<IReadOnlyList<DependencyEdge>>()),
            Times.Exactly(3));
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(edges1), Times.Once);
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(edges2), Times.Once);
        _mockEdgeChangeTracker.Verify(t => t.UpdateEntities(edges3), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PublishNodeChanges_TriggersNodesChangedObservable()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Test") }.AsReadOnly();
        var nodeSubject = new BehaviorSubject<IReadOnlyList<Node>>(new List<Node>().AsReadOnly());
        _mockNodeChangeTracker.Setup(t => t.Nodes).Returns(nodeSubject);
        _mockNodeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<Node>>()))
            .Callback<IReadOnlyList<Node>>(n => nodeSubject.OnNext(n));

        // Act
        IReadOnlyList<Node>? receivedNodes = null;
        _eventPublisher.NodesChanged.Skip(1).Subscribe(n => receivedNodes = n);
        _eventPublisher.PublishNodeChanges(nodes);

        // Assert
        Assert.Equal(nodes, receivedNodes);
    }

    [Fact]
    public void PublishEdgeChanges_TriggersEdgesChangedObservable()
    {
        // Arrange
        var edges = new List<DependencyEdge> { CreateDependencyEdge() }.AsReadOnly();
        var edgeSubject = new BehaviorSubject<IReadOnlyList<DependencyEdge>>(new List<DependencyEdge>().AsReadOnly());
        _mockEdgeChangeTracker.Setup(t => t.Edges).Returns(edgeSubject);
        _mockEdgeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Callback<IReadOnlyList<DependencyEdge>>(e => edgeSubject.OnNext(e));

        // Act
        IReadOnlyList<DependencyEdge>? receivedEdges = null;
        _eventPublisher.EdgesChanged.Skip(1).Subscribe(e => receivedEdges = e);
        _eventPublisher.PublishEdgeChanges(edges);

        // Assert
        Assert.Equal(edges, receivedEdges);
    }

    #endregion

    #region Helper Methods

    private static TaskNode CreateTaskNode(string name)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0
            },
            ProcedureId = default
        };
    }

    private static DependencyEdge CreateDependencyEdge()
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            ProcedureId = Guid.Empty
        };
    }

    #endregion
}