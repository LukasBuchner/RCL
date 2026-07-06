using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Moq;
using ProcedureEntity = FHOOE.Freydis.Domain.Entities.Procedure.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Reactive;

/// <summary>
///     Verifies that <see cref="ProcedureOrchestrator" /> notifies
///     <see cref="IProcedureStateScope" /> and <see cref="IProcedureVariableChangeTracker" />
///     when procedures are loaded, unloaded, or deleted.
/// </summary>
public sealed class ProcedureOrchestratorNotificationTests
{
    private readonly ProcedureOrchestrator _orchestrator;
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly Mock<IProcedureRepository> _procedureRepo = new();
    private readonly Mock<IProcedureStateScope> _stateScope = new();
    private readonly Mock<IProcedureVariableChangeTracker> _tracker = new();

    public ProcedureOrchestratorNotificationTests()
    {
        _orchestrator = new ProcedureOrchestrator(
            _procedureRepo.Object,
            _stateScope.Object,
            _tracker.Object,
            new Mock<ILogger<ProcedureOrchestrator>>().Object);
    }

    [Fact]
    public async Task LoadProcedureAsync_NotifiesStateScopeAndVariableTracker()
    {
        // Arrange
        var procedure = new ProcedureEntity
        {
            Id = _procedureId,
            Name = "Test Procedure",
            RootNodeIds = [],
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);

        // Act
        await _orchestrator.LoadProcedureAsync(_procedureId);

        // Assert — state scope receives the scoped load trigger
        _stateScope.Verify(s => s.OnProcedureLoaded(_procedureId), Times.Once);

        // Assert — variable tracker receives the procedure's variables
        _tracker.Verify(
            t => t.NotifyChanged(It.IsAny<IReadOnlyList<VariableDefinition>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnloadCurrentProcedureAsync_NotifiesStateScopeAndVariableTracker()
    {
        // Arrange — load first
        var procedure = new ProcedureEntity
        {
            Id = _procedureId,
            Name = "Test Procedure",
            RootNodeIds = [],
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);
        await _orchestrator.LoadProcedureAsync(_procedureId);

        _stateScope.Invocations.Clear();
        _tracker.Invocations.Clear();

        // Act
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Assert
        _stateScope.Verify(s => s.OnProcedureUnloaded(), Times.Once);
        _tracker.Verify(t => t.NotifyUnloaded(), Times.Once);
    }

    [Fact]
    public async Task DeleteProcedureAsync_WhenLoaded_NotifiesStateScopeAndVariableTracker()
    {
        // Arrange — load first
        var procedure = new ProcedureEntity
        {
            Id = _procedureId,
            Name = "Test Procedure",
            RootNodeIds = [],
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);
        _procedureRepo.Setup(r => r.DeleteAsync(_procedureId)).ReturnsAsync(true);
        await _orchestrator.LoadProcedureAsync(_procedureId);

        _stateScope.Invocations.Clear();
        _tracker.Invocations.Clear();

        // Act
        await _orchestrator.DeleteProcedureAsync(_procedureId);

        // Assert
        _stateScope.Verify(s => s.OnProcedureUnloaded(), Times.Once);
        _tracker.Verify(t => t.NotifyUnloaded(), Times.Once);
    }

    [Fact]
    public async Task DeleteProcedureAsync_WhenNotLoaded_DoesNotNotifyStateScopeOrVariableTracker()
    {
        // Arrange — no procedure loaded
        var otherProcedureId = Guid.NewGuid();
        var procedure = new ProcedureEntity
        {
            Id = otherProcedureId,
            Name = "Other Procedure",
            RootNodeIds = [],
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
        _procedureRepo.Setup(r => r.GetByIdAsync(otherProcedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);
        _procedureRepo.Setup(r => r.DeleteAsync(otherProcedureId)).ReturnsAsync(true);

        // Act — delete a procedure that was never loaded
        await _orchestrator.DeleteProcedureAsync(otherProcedureId);

        // Assert — no scope or variable notifications for non-loaded procedures
        _stateScope.Verify(s => s.OnProcedureUnloaded(), Times.Never);
        _tracker.Verify(t => t.NotifyUnloaded(), Times.Never);
    }
}