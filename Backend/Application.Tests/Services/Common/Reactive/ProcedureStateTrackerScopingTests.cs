using FluentAssertions;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Reactive;

/// <summary>
///     Verifies the procedure-scoping behavior of <see cref="ProcedureStateTracker" />.
///     Ensures that repository calls are scoped to the loaded procedure and that the
///     BehaviorSubject emits empty state when no procedure is loaded.
/// </summary>
public sealed class ProcedureStateTrackerScopingTests
{
    private readonly Mock<IProcedureRepository> _repo = new();
    private readonly Mock<ILogger<ProcedureStateTracker>> _logger = new();
    private readonly Guid _procedureId = Guid.NewGuid();

    /// <summary>
    ///     Creates a fresh tracker instance backed by the shared mock repository.
    /// </summary>
    private ProcedureStateTracker CreateTracker()
    {
        return new ProcedureStateTracker(_repo.Object, _logger.Object);
    }

    /// <summary>
    ///     Builds a minimal valid <see cref="TaskNode" /> for use in tests that need a non-null
    ///     <see cref="Node" /> but do not care about its specific type or task properties.
    /// </summary>
    /// <param name="procedureId">The procedure the node belongs to.</param>
    private static TaskNode CreateTestNode(Guid procedureId)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "test", StartTime = 0, Duration = 1 }
        };
    }

    #region Initial state

    [Fact]
    public void InitialState_IsEmpty_AndDoesNotQueryRepository()
    {
        // Act
        using var tracker = CreateTracker();

        // Assert — no repository call on construction
        _repo.Verify(r => r.GetAllNodesAsync(), Times.Never);
        _repo.Verify(r => r.GetAllEdgesAsync(), Times.Never);
        _repo.Verify(r => r.GetNodesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);
        _repo.Verify(r => r.GetEdgesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);

        var state = tracker.GetCurrentState();
        state.Nodes.Should().BeEmpty();
        state.Edges.Should().BeEmpty();
    }

    [Fact]
    public void NoLoadedProcedure_EmitsEmptyNodesObservable()
    {
        // Arrange
        using var tracker = CreateTracker();
        INodeChangeTracker nodeTracker = tracker;

        // Act
        var nodes = nodeTracker.Nodes.FirstAsync().Wait();

        // Assert
        nodes.Should().BeEmpty("no procedure is loaded so the node stream must be empty");
    }

    [Fact]
    public void NoLoadedProcedure_EmitsEmptyEdgesObservable()
    {
        // Arrange
        using var tracker = CreateTracker();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        // Act
        var edges = edgeTracker.Edges.FirstAsync().Wait();

        // Assert
        edges.Should().BeEmpty("no procedure is loaded so the edge stream must be empty");
    }

    #endregion

    #region OnProcedureLoaded — scoped repository calls

    [Fact]
    public async Task OnProcedureLoaded_CallsScopedNodeQuery_NotGlobalQuery()
    {
        // Arrange
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();

        // Act
        tracker.OnProcedureLoaded(_procedureId);

        // Allow the fire-and-forget task to complete
        await Task.Delay(50);

        // Assert — scoped query issued
        _repo.Verify(r => r.GetNodesByProcedureIdAsync(_procedureId), Times.Once);

        // Assert — global unscoped query never issued
        _repo.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    [Fact]
    public async Task OnProcedureLoaded_CallsScopedEdgeQuery_NotGlobalQuery()
    {
        // Arrange
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();

        // Act
        tracker.OnProcedureLoaded(_procedureId);

        // Allow the fire-and-forget task to complete
        await Task.Delay(50);

        // Assert — scoped query issued
        _repo.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureId), Times.Once);

        // Assert — global unscoped query never issued
        _repo.Verify(r => r.GetAllEdgesAsync(), Times.Never);
    }

    [Fact]
    public async Task OnProcedureLoaded_PopulatesNodesFromRepository()
    {
        // Arrange
        var node = CreateTestNode(_procedureId);
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([node]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();
        INodeChangeTracker nodeTracker = tracker;

        // Collect the first non-empty emission after load
        var received = nodeTracker.Nodes
            .Where(n => n.Count > 0)
            .FirstAsync()
            .GetAwaiter();

        // Act
        tracker.OnProcedureLoaded(_procedureId);

        var nodes = await received;

        // Assert
        nodes.Should().ContainSingle(n => n.Id == node.Id);
    }

    [Fact]
    public async Task OnProcedureLoaded_PopulatesEdgesFromRepository()
    {
        // Arrange
        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([edge]);

        using var tracker = CreateTracker();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        var received = edgeTracker.Edges
            .Where(e => e.Count > 0)
            .FirstAsync()
            .GetAwaiter();

        // Act
        tracker.OnProcedureLoaded(_procedureId);

        var edges = await received;

        // Assert
        edges.Should().ContainSingle(e => e.Id == edge.Id);
    }

    [Fact]
    public async Task OnProcedureLoaded_SetsIsInitializedTrue()
    {
        // Arrange
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();

        tracker.IsInitialized.Should().BeFalse("tracker starts uninitialized");

        // Act
        tracker.OnProcedureLoaded(_procedureId);
        await Task.Delay(50);

        // Assert
        tracker.IsInitialized.Should().BeTrue();
    }

    #endregion

    #region OnProcedureUnloaded — state cleared

    [Fact]
    public async Task OnProcedureUnloaded_ClearsBehaviorSubject()
    {
        // Arrange — load a procedure first so the subject is non-empty
        var node = CreateTestNode(_procedureId);
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([node]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();

        // Wait for initial load to populate
        tracker.OnProcedureLoaded(_procedureId);
        await Task.Delay(50);

        tracker.GetCurrentState().Nodes.Should().NotBeEmpty("pre-condition: state should be populated after load");

        // Act
        tracker.OnProcedureUnloaded();

        // Assert — state is immediately empty
        var state = tracker.GetCurrentState();
        state.Nodes.Should().BeEmpty();
        state.Edges.Should().BeEmpty();
    }

    [Fact]
    public void OnProcedureUnloaded_EmitsEmptyNodeObservable()
    {
        // Arrange
        using var tracker = CreateTracker();
        INodeChangeTracker nodeTracker = tracker;

        IReadOnlyList<Node>? received = null;
        nodeTracker.Nodes.Subscribe(n => received = n);

        // Act
        tracker.OnProcedureUnloaded();

        // Assert
        received.Should().NotBeNull();
        received!.Should().BeEmpty("unload must push empty nodes to all subscribers");
    }

    [Fact]
    public void OnProcedureUnloaded_EmitsEmptyEdgeObservable()
    {
        // Arrange
        using var tracker = CreateTracker();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        IReadOnlyList<DependencyEdge>? received = null;
        edgeTracker.Edges.Subscribe(e => received = e);

        // Act
        tracker.OnProcedureUnloaded();

        // Assert
        received.Should().NotBeNull();
        received!.Should().BeEmpty("unload must push empty edges to all subscribers");
    }

    [Fact]
    public void OnProcedureUnloaded_SetsIsInitializedFalse()
    {
        // Arrange
        using var tracker = CreateTracker();

        // Act
        tracker.OnProcedureUnloaded();

        // Assert
        tracker.IsInitialized.Should().BeFalse();
    }

    #endregion

    #region RefreshFromRepositoryAsync — respects loaded procedure scope

    [Fact]
    public async Task RefreshNodes_WhenNoProcedureLoaded_DoesNotQueryRepository()
    {
        // Arrange
        using var tracker = CreateTracker();
        INodeChangeTracker nodeTracker = tracker;

        // Act
        await nodeTracker.RefreshFromRepositoryAsync();

        // Assert
        _repo.Verify(r => r.GetAllNodesAsync(), Times.Never);
        _repo.Verify(r => r.GetNodesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RefreshEdges_WhenNoProcedureLoaded_DoesNotQueryRepository()
    {
        // Arrange
        using var tracker = CreateTracker();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        // Act
        await edgeTracker.RefreshFromRepositoryAsync();

        // Assert
        _repo.Verify(r => r.GetAllEdgesAsync(), Times.Never);
        _repo.Verify(r => r.GetEdgesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RefreshNodes_WhenProcedureLoaded_UsesScoped_NotGlobalQuery()
    {
        // Arrange
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        using var tracker = CreateTracker();
        INodeChangeTracker nodeTracker = tracker;

        tracker.OnProcedureLoaded(_procedureId);
        await Task.Delay(50);

        _repo.Invocations.Clear();
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_procedureId))
            .ReturnsAsync([]);

        // Act
        await nodeTracker.RefreshFromRepositoryAsync();

        // Assert
        _repo.Verify(r => r.GetNodesByProcedureIdAsync(_procedureId), Times.Once);
        _repo.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    #endregion
}