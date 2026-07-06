using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Operations;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Operations;

/// <summary>
///     Tests for Mutation class dependency edge operations using IProcedureContext
///     and the extracted IDependencyEdgeValidator.
/// </summary>
public class MutationProcedureContextTests
{
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<IDependencyEdgeValidator> _mockEdgeValidator = new();
    private readonly Mock<ILogger<Mutation>> _mockLogger = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();
    private readonly Mutation _sut = new();

    public MutationProcedureContextTests()
    {
        // By default the validator completes successfully (all rules pass).
        _mockEdgeValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
    }

    #region CreateDependencyEdgeAsync Tests

    [Fact]
    public async Task CreateDependencyEdgeAsync_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var edgeId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var input = new CreateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = edgeId,
                SourceId = sourceId,
                TargetId = targetId,
                SourceHandle = "output",
                TargetHandle = "input"
            }
        };

        var expectedEdge = new DependencyEdge
        {
            Id = edgeId,
            ProcedureId = procedureId,
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "output",
            TargetHandle = "input"
        };

        _mockEdgeService
            .Setup(x => x.CreateDependencyEdgeAsync(It.Is<DependencyEdge>(e =>
                e.Id == edgeId &&
                e.ProcedureId == procedureId)))
            .ReturnsAsync(expectedEdge);

        // Act
        var result = await _sut.CreateDependencyEdgeAsync(
            input,
            _mockEdgeService.Object,
            _mockEdgeValidator.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(edgeId, result!.DependencyEdge!.Id);
        Assert.Equal(procedureId, result.DependencyEdge.ProcedureId);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task CreateDependencyEdgeAsync_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var input = new CreateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = Guid.NewGuid(),
                SourceId = sourceId,
                TargetId = targetId
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.CreateDependencyEdgeAsync(
                input,
                _mockEdgeService.Object,
                _mockEdgeValidator.Object,
                _mockProcedureContext.Object,
                _mockLogger.Object));

        Assert.Contains("No procedure is currently loaded", ex.Message);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
        _mockEdgeService.Verify(x => x.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()), Times.Never);
    }

    #endregion

    #region UpdateDependencyEdgeAsync Tests

    [Fact]
    public async Task UpdateDependencyEdgeAsync_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var edgeId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var input = new UpdateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = edgeId,
                SourceId = sourceId,
                TargetId = targetId,
                SourceHandle = "output",
                TargetHandle = "input"
            }
        };

        _mockEdgeService
            .Setup(x => x.UpdateDependencyEdgeAsync(It.Is<DependencyEdge>(e =>
                e.Id == edgeId &&
                e.ProcedureId == procedureId)))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateDependencyEdgeAsync(
            input,
            _mockEdgeService.Object,
            _mockEdgeValidator.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Boolean);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task UpdateDependencyEdgeAsync_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var input = new UpdateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = Guid.NewGuid(),
                SourceId = sourceId,
                TargetId = targetId
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.UpdateDependencyEdgeAsync(
                input,
                _mockEdgeService.Object,
                _mockEdgeValidator.Object,
                _mockProcedureContext.Object,
                _mockLogger.Object));

        Assert.Contains("No procedure is currently loaded", ex.Message);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
        _mockEdgeService.Verify(x => x.UpdateDependencyEdgeAsync(It.IsAny<DependencyEdge>()), Times.Never);
    }

    #endregion
}