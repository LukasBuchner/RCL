using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FluentAssertions;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Context;

/// <summary>
///     Tests for ProcedureContext service that manages procedure scoping state via delegation to IProcedureOrchestrator.
/// </summary>
public class ProcedureContextTests
{
    private readonly Mock<IProcedureOrchestrator> _procedureOrchestratorMock;
    private readonly ProcedureContext _sut;

    public ProcedureContextTests()
    {
        _procedureOrchestratorMock = new Mock<IProcedureOrchestrator>();
        _sut = new ProcedureContext(_procedureOrchestratorMock.Object);
    }

    [Fact]
    public void CurrentProcedureId_WhenNoProcedureLoaded_ShouldReturnNull()
    {
        // Arrange
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        // Act
        var result = _sut.CurrentProcedureId;

        // Assert
        result.Should().BeNull("no procedure has been loaded yet");
    }

    [Fact]
    public void CurrentProcedureId_WhenProcedureLoaded_ShouldReturnCorrectId()
    {
        // Arrange
        var expectedProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(expectedProcedureId);

        // Act
        var result = _sut.CurrentProcedureId;

        // Assert
        result.Should().Be(expectedProcedureId, "the procedure ID was retrieved from the orchestrator");
    }

    [Fact]
    public void RequireCurrentProcedureId_WhenProcedureLoaded_ShouldReturnId()
    {
        // Arrange
        var expectedProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(expectedProcedureId);

        // Act
        var result = _sut.RequireCurrentProcedureId();

        // Assert
        result.Should().Be(expectedProcedureId, "the procedure ID was retrieved from the orchestrator");
    }

    [Fact]
    public void RequireCurrentProcedureId_WhenNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        // Act
        var act = () => _sut.RequireCurrentProcedureId();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*",
                "this operation requires an active procedure context");
    }

    [Fact]
    public void ValidateProcedureOwnership_WhenEntityBelongsToCurrentProcedure_ShouldNotThrow()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(procedureId);

        // Act
        var act = () => _sut.ValidateProcedureOwnership(procedureId);

        // Assert
        act.Should().NotThrow("the entity belongs to the current procedure");
    }

    [Fact]
    public void ValidateProcedureOwnership_WhenEntityBelongsToDifferentProcedure_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var currentProcedureId = Guid.NewGuid();
        var differentProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(currentProcedureId);

        // Act
        var act = () => _sut.ValidateProcedureOwnership(differentProcedureId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*",
                "cross-procedure access should be prevented");
    }

    [Fact]
    public void ValidateProcedureOwnership_WhenNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var entityProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        // Act
        var act = () => _sut.ValidateProcedureOwnership(entityProcedureId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*",
                "validation requires an active procedure context");
    }

    [Fact]
    public void IsCurrentProcedure_WhenProcedureIdMatches_ShouldReturnTrue()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(procedureId);

        // Act
        var result = _sut.IsCurrentProcedure(procedureId);

        // Assert
        result.Should().BeTrue("the procedure ID matches the current context");
    }

    [Fact]
    public void IsCurrentProcedure_WhenProcedureIdDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange
        var currentProcedureId = Guid.NewGuid();
        var differentProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(currentProcedureId);

        // Act
        var result = _sut.IsCurrentProcedure(differentProcedureId);

        // Assert
        result.Should().BeFalse("the procedure ID does not match the current context");
    }

    [Fact]
    public void IsCurrentProcedure_WhenNoProcedureLoaded_ShouldReturnFalse()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns((Guid?)null);

        // Act
        var result = _sut.IsCurrentProcedure(procedureId);

        // Assert
        result.Should().BeFalse("no procedure is currently loaded");
    }

    [Fact]
    public void SetCurrentProcedureId_ShouldThrowNotSupportedException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();

        // Act
        var act = () => _sut.SetCurrentProcedureId(procedureId);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*not supported*Use IProcedureOrchestrator.LoadProcedureAsync()*",
                "setting procedure ID directly is not allowed; must use orchestrator");
    }

    [Fact]
    public void ClearCurrentProcedure_ShouldThrowNotSupportedException()
    {
        // Act
        var act = () => _sut.ClearCurrentProcedure();

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*not supported*Use IProcedureOrchestrator.UnloadCurrentProcedureAsync()*",
                "clearing procedure directly is not allowed; must use orchestrator");
    }

    [Fact]
    public void ValidateProcedureOwnership_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var currentProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(currentProcedureId);

        // Act
        var act = () => _sut.ValidateProcedureOwnership(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*",
                "empty GUIDs are not valid procedure identifiers");
    }

    [Fact]
    public void IsCurrentProcedure_WithEmptyGuid_ShouldReturnFalse()
    {
        // Arrange
        var currentProcedureId = Guid.NewGuid();
        _procedureOrchestratorMock.Setup(x => x.GetLoadedProcedureId()).Returns(currentProcedureId);

        // Act
        var result = _sut.IsCurrentProcedure(Guid.Empty);

        // Assert
        result.Should().BeFalse("empty GUIDs should never match a valid procedure");
    }

    [Fact]
    public void Constructor_WhenProcedureOrchestratorIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ProcedureContext(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*procedureOrchestrator*",
                "the orchestrator dependency is required");
    }
}