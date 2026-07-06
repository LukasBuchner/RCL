using FluentAssertions;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Reactive;

/// <summary>
///     Verifies the trust-boundary hardening of <see cref="ProcedureStateTracker" />.
///     The two public <c>UpdateEntities</c> overloads must never publish entities whose
///     <c>ProcedureId</c> does not match the currently loaded procedure, and must reject
///     updates entirely when no procedure is loaded. Violations must be surfaced as
///     structured warnings so upstream callers that leak cross-procedure data can be located.
/// </summary>
public sealed class ProcedureStateTrackerCrossProcedureGuardTests
{
    private readonly Mock<IProcedureRepository> _repo = new();
    private readonly Mock<ILogger<ProcedureStateTracker>> _logger = new();

    private readonly Guid _loadedProcedureId = Guid.NewGuid();
    private readonly Guid _otherProcedureId = Guid.NewGuid();

    /// <summary>
    ///     Enables Warning-level logging on the mock so that <c>[LoggerMessage]</c>
    ///     source-generated extension methods invoke the underlying <c>Log</c> path
    ///     and can be verified.
    /// </summary>
    public ProcedureStateTrackerCrossProcedureGuardTests()
    {
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    /// <summary>
    ///     Creates a fresh tracker backed by the shared mock repository, pre-loaded with
    ///     <see cref="_loadedProcedureId" /> so that subsequent <c>UpdateEntities</c> calls
    ///     are validated against a concrete active procedure.
    /// </summary>
    private async Task<ProcedureStateTracker> CreateLoadedTrackerAsync()
    {
        _repo.Setup(r => r.GetNodesByProcedureIdAsync(_loadedProcedureId))
            .ReturnsAsync(new List<Node>());
        _repo.Setup(r => r.GetEdgesByProcedureIdAsync(_loadedProcedureId))
            .ReturnsAsync(new List<DependencyEdge>());

        var tracker = new ProcedureStateTracker(_repo.Object, _logger.Object);
        tracker.OnProcedureLoaded(_loadedProcedureId);
        await Task.Delay(50);
        _logger.Invocations.Clear();
        return tracker;
    }

    private static TaskNode CreateNode(Guid procedureId)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "test", StartTime = 0, Duration = 1 }
        };
    }

    private static DependencyEdge CreateEdge(Guid procedureId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
    }

    private void VerifyLogged(LogLevel level, string messageFragment, Times times)
    {
        _logger.Verify(l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(messageFragment)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    #region Nodes — cross-procedure guard

    [Fact]
    public async Task UpdateNodes_AllMatchingProcedure_PublishesUnchanged()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        INodeChangeTracker nodeTracker = tracker;
        var nodes = new List<Node> { CreateNode(_loadedProcedureId), CreateNode(_loadedProcedureId) };

        nodeTracker.UpdateEntities(nodes);

        tracker.GetCurrentState().Nodes.Should().HaveCount(2);
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Never());
    }

    [Fact]
    public async Task UpdateNodes_WithForeignEntitiesMixed_DropsForeignAndLogsWarning()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        INodeChangeTracker nodeTracker = tracker;

        var keep = CreateNode(_loadedProcedureId);
        var drop1 = CreateNode(_otherProcedureId);
        var drop2 = CreateNode(_otherProcedureId);
        var nodes = new List<Node> { keep, drop1, drop2 };

        nodeTracker.UpdateEntities(nodes);

        var published = tracker.GetCurrentState().Nodes;
        published.Should().ContainSingle(n => n.Id == keep.Id);
        published.Should().NotContain(n => n.Id == drop1.Id || n.Id == drop2.Id);
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Once());
    }

    [Fact]
    public async Task UpdateNodes_AllForeign_PublishesEmptyAndLogsWarning()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        INodeChangeTracker nodeTracker = tracker;

        var nodes = new List<Node> { CreateNode(_otherProcedureId), CreateNode(_otherProcedureId) };

        nodeTracker.UpdateEntities(nodes);

        tracker.GetCurrentState().Nodes.Should().BeEmpty();
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Once());
    }

    [Fact]
    public void UpdateNodes_NoProcedureLoaded_RejectsAndLogsWarning()
    {
        var tracker = new ProcedureStateTracker(_repo.Object, _logger.Object);
        INodeChangeTracker nodeTracker = tracker;
        var nodes = new List<Node> { CreateNode(_otherProcedureId) };

        nodeTracker.UpdateEntities(nodes);

        tracker.GetCurrentState().Nodes.Should().BeEmpty("update must be rejected when no procedure is loaded");
        VerifyLogged(LogLevel.Warning, "Rejected", Times.Once());
    }

    [Fact]
    public void UpdateNodes_NoProcedureLoaded_EmptyList_StaysSilent()
    {
        var tracker = new ProcedureStateTracker(_repo.Object, _logger.Object);
        INodeChangeTracker nodeTracker = tracker;

        nodeTracker.UpdateEntities(new List<Node>());

        VerifyLogged(LogLevel.Warning, "Rejected", Times.Never());
    }

    #endregion

    #region Edges — cross-procedure guard

    [Fact]
    public async Task UpdateEdges_AllMatchingProcedure_PublishesUnchanged()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        IDependencyEdgeChangeTracker edgeTracker = tracker;
        var edges = new List<DependencyEdge> { CreateEdge(_loadedProcedureId), CreateEdge(_loadedProcedureId) };

        edgeTracker.UpdateEntities(edges);

        tracker.GetCurrentState().Edges.Should().HaveCount(2);
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Never());
    }

    [Fact]
    public async Task UpdateEdges_WithForeignEntitiesMixed_DropsForeignAndLogsWarning()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        var keep = CreateEdge(_loadedProcedureId);
        var drop = CreateEdge(_otherProcedureId);
        var edges = new List<DependencyEdge> { keep, drop };

        edgeTracker.UpdateEntities(edges);

        var published = tracker.GetCurrentState().Edges;
        published.Should().ContainSingle(e => e.Id == keep.Id);
        published.Should().NotContain(e => e.Id == drop.Id);
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Once());
    }

    [Fact]
    public async Task UpdateEdges_AllForeign_PublishesEmptyAndLogsWarning()
    {
        using var tracker = await CreateLoadedTrackerAsync();
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        var edges = new List<DependencyEdge> { CreateEdge(_otherProcedureId) };

        edgeTracker.UpdateEntities(edges);

        tracker.GetCurrentState().Edges.Should().BeEmpty();
        VerifyLogged(LogLevel.Warning, "Dropped", Times.Once());
    }

    [Fact]
    public void UpdateEdges_NoProcedureLoaded_RejectsAndLogsWarning()
    {
        var tracker = new ProcedureStateTracker(_repo.Object, _logger.Object);
        IDependencyEdgeChangeTracker edgeTracker = tracker;
        var edges = new List<DependencyEdge> { CreateEdge(_otherProcedureId) };

        edgeTracker.UpdateEntities(edges);

        tracker.GetCurrentState().Edges.Should().BeEmpty();
        VerifyLogged(LogLevel.Warning, "Rejected", Times.Once());
    }

    [Fact]
    public void UpdateEdges_NoProcedureLoaded_EmptyList_StaysSilent()
    {
        var tracker = new ProcedureStateTracker(_repo.Object, _logger.Object);
        IDependencyEdgeChangeTracker edgeTracker = tracker;

        edgeTracker.UpdateEntities(new List<DependencyEdge>());

        VerifyLogged(LogLevel.Warning, "Rejected", Times.Never());
    }

    #endregion
}