using FluentAssertions;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Edges;

/// <summary>
///     Tests for <see cref="DependencyEdgeApplicationService" /> procedure scoping and ownership validation.
///     Ensures that edge operations are properly isolated to the currently loaded procedure.
/// </summary>
public class DependencyEdgeApplicationServiceProcedureScopingTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _crudOrchestrator = new();
    private readonly Mock<IDependencyEdgeChangeTracker> _edgeChangeTracker = new();
    private readonly Mock<IProcedureContext> _procedureContext = new();
    private readonly Mock<ILogger<DependencyEdgeApplicationService>> _logger = new();
    private readonly DependencyEdgeApplicationService _sut;

    private readonly Guid _procedureAId = Guid.NewGuid();
    private readonly Guid _procedureBId = Guid.NewGuid();

    public DependencyEdgeApplicationServiceProcedureScopingTests()
    {
        // Wire ProcedureChanges observable so DependencyEdges property can subscribe
        _procedureContext.Setup(c => c.ProcedureChanges)
            .Returns(Observable.Never<Guid?>());

        _sut = new DependencyEdgeApplicationService(
            _crudOrchestrator.Object,
            _edgeChangeTracker.Object,
            _procedureContext.Object,
            _logger.Object);
    }

    #region CreateDependencyEdgeAsync tests

    [Fact]
    public async Task CreateDependencyEdgeAsync_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns((Guid?)null);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var act = async () => await _sut.CreateDependencyEdgeAsync(edge);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");
    }

    [Fact]
    public async Task CreateDependencyEdgeAsync_WithCorrectProcedure_ShouldValidateAndCreate()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _crudOrchestrator.Setup(o => o.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()))
            .ReturnsAsync((DependencyEdge e) => e);

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var result = await _sut.CreateDependencyEdgeAsync(edge);

        // Assert
        result.Should().Be(edge);
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(_procedureAId), Times.Once);
        _crudOrchestrator.Verify(o => o.CreateDependencyEdgeAsync(edge), Times.Once);
    }

    [Fact]
    public async Task CreateDependencyEdgeAsync_WithDifferentProcedure_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(_procedureBId))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureBId}' does not belong to the current procedure '{_procedureAId}'."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var act = async () => await _sut.CreateDependencyEdgeAsync(edge);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _crudOrchestrator.Verify(o => o.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()), Times.Never);
    }

    #endregion

    #region UpdateDependencyEdgeAsync tests

    [Fact]
    public async Task UpdateDependencyEdgeAsync_WithCorrectProcedure_ShouldSucceed()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _crudOrchestrator.Setup(o => o.UpdateDependencyEdgeAsync(It.IsAny<DependencyEdge>()))
            .ReturnsAsync(true);

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var result = await _sut.UpdateDependencyEdgeAsync(edge);

        // Assert
        result.Should().BeTrue();
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(_procedureAId), Times.Once);
        _crudOrchestrator.Verify(o => o.UpdateDependencyEdgeAsync(edge), Times.Once);
    }

    [Fact]
    public async Task UpdateDependencyEdgeAsync_WithDifferentProcedure_ShouldThrowException()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(_procedureBId))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureBId}' does not belong to the current procedure '{_procedureAId}'."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var act = async () => await _sut.UpdateDependencyEdgeAsync(edge);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _crudOrchestrator.Verify(o => o.UpdateDependencyEdgeAsync(It.IsAny<DependencyEdge>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDependencyEdgeAsync_WithoutLoadedProcedure_ShouldThrowException()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns((Guid?)null);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };

        // Act
        var act = async () => await _sut.UpdateDependencyEdgeAsync(edge);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");

        _crudOrchestrator.Verify(o => o.UpdateDependencyEdgeAsync(It.IsAny<DependencyEdge>()), Times.Never);
    }

    #endregion

    #region DependencyEdges observable tests

    [Fact]
    public void DependencyEdges_Observable_FiltersToCurrentProcedure()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);

        var edgeA1 = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };
        var edgeA2 = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };
        var edgeB = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        var allEdges = new List<DependencyEdge> { edgeA1, edgeA2, edgeB };
        _edgeChangeTracker.Setup(t => t.Edges)
            .Returns(Observable.Return<IReadOnlyList<DependencyEdge>>(allEdges));

        // Act — DependencyEdges is a hot stream (CombineLatest with ProcedureChanges
        // which never completes), so take only the first emission to avoid blocking.
        var result = _sut.DependencyEdges.Timeout(TimeSpan.FromSeconds(2)).FirstAsync().Wait();

        // Assert
        result.Should().HaveCount(2, "only edges from Procedure A should be returned");
        result.Should().Contain(edgeA1);
        result.Should().Contain(edgeA2);
        result.Should().NotContain(edgeB, "edges from other procedures should be filtered out");
    }

    [Fact]
    public void DependencyEdges_Observable_ReturnsEmptyWhenNoProcedureLoaded()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns((Guid?)null);

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(), ProcedureId = _procedureAId,
                SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
            },
            new()
            {
                Id = Guid.NewGuid(), ProcedureId = _procedureBId,
                SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
            }
        };

        _edgeChangeTracker.Setup(t => t.Edges)
            .Returns(Observable.Return<IReadOnlyList<DependencyEdge>>(edges));

        // Act — DependencyEdges is a hot stream (CombineLatest with ProcedureChanges
        // which never completes), so take only the first emission to avoid blocking.
        var result = _sut.DependencyEdges.Timeout(TimeSpan.FromSeconds(2)).FirstAsync().Wait();

        // Assert
        result.Should().BeEmpty("no procedure is loaded, so no edges should be visible");
    }

    #endregion

    #region GetAllDependencyEdgesAsync tests

    [Fact]
    public async Task GetAllDependencyEdgesAsync_WithProcedureLoaded_ShouldReturnOnlyCurrentProcedureEdges()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);

        var edgeA1 = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };
        var edgeA2 = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };
        var edgeB = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edgeA1, edgeA2, edgeB }.AsReadOnly());

        // Act
        var result = await _sut.GetAllDependencyEdgesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(edgeA1);
        result.Should().Contain(edgeA2);
        result.Should().OnlyContain(e => e.ProcedureId == _procedureAId);
    }

    [Fact]
    public async Task GetAllDependencyEdgesAsync_WithNoProcedureLoaded_ShouldReturnEmpty()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns((Guid?)null);

        // Act
        var result = await _sut.GetAllDependencyEdgesAsync();

        // Assert
        result.Should().BeEmpty("when no procedure is loaded the service returns an empty list");
    }

    #endregion

    #region DeleteDependencyEdgeAsync tests

    [Fact]
    public async Task DeleteDependencyEdgeAsync_WithCorrectProcedure_ShouldValidateAndDelete()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edge }.AsReadOnly());
        _crudOrchestrator.Setup(o => o.DeleteDependencyEdgeAsync(edge.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteDependencyEdgeAsync(edge.Id);

        // Assert
        result.Should().BeTrue();
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(_procedureAId), Times.Once);
        _crudOrchestrator.Verify(o => o.DeleteDependencyEdgeAsync(edge.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_WithDifferentProcedure_ShouldThrowAndNotDelete()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(_procedureBId))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureBId}' does not belong to the current procedure '{_procedureAId}'."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edge }.AsReadOnly());

        // Act
        var act = async () => await _sut.DeleteDependencyEdgeAsync(edge.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _crudOrchestrator.Verify(o => o.DeleteDependencyEdgeAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_WhenEdgeNotFound_ShouldReturnFalseAndNotDelete()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge>().AsReadOnly());

        // Act
        var result = await _sut.DeleteDependencyEdgeAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse("edge does not exist in the tracker");
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(It.IsAny<Guid>()), Times.Never);
        _crudOrchestrator.Verify(o => o.DeleteDependencyEdgeAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region GetDependencyEdgeByIdAsync tests

    [Fact]
    public async Task GetDependencyEdgeByIdAsync_WithCorrectProcedure_ShouldReturnEdge()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureAId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edge }.AsReadOnly());

        // Act
        var result = await _sut.GetDependencyEdgeByIdAsync(edge.Id);

        // Assert
        result.Should().Be(edge);
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(_procedureAId), Times.Once);
    }

    [Fact]
    public async Task GetDependencyEdgeByIdAsync_WithDifferentProcedure_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _procedureContext
            .Setup(c => c.ValidateProcedureOwnership(_procedureBId))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureBId}' does not belong to the current procedure '{_procedureAId}'."));

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(), ProcedureId = _procedureBId,
            SourceId = Guid.NewGuid(), TargetId = Guid.NewGuid()
        };

        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edge }.AsReadOnly());

        // Act
        var act = async () => await _sut.GetDependencyEdgeByIdAsync(edge.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");
    }

    [Fact]
    public async Task GetDependencyEdgeByIdAsync_WhenEdgeNotFound_ShouldReturnNull()
    {
        // Arrange
        _procedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureAId);
        _edgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge>().AsReadOnly());

        // Act
        var result = await _sut.GetDependencyEdgeByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull("edge does not exist in the tracker");
        _procedureContext.Verify(c => c.ValidateProcedureOwnership(It.IsAny<Guid>()), Times.Never);
    }

    #endregion
}