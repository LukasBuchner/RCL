using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Moq;
using ProcedureEntity = FHOOE.Freydis.Domain.Entities.Procedure.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Reactive;

/// <summary>
///     Verifies that <see cref="ProcedureVariableService" /> notifies the
///     <see cref="IProcedureVariableChangeTracker" /> after each variable mutation.
/// </summary>
public sealed class ProcedureVariableServiceNotificationTests
{
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly Mock<IRepository<ProcedureEntity>> _procedureRepo = new();
    private readonly ProcedureVariableService _service;
    private readonly Mock<IProcedureVariableChangeTracker> _tracker = new();

    public ProcedureVariableServiceNotificationTests()
    {
        _service = new ProcedureVariableService(
            _procedureRepo.Object,
            _tracker.Object,
            new Mock<ILogger<ProcedureVariableService>>().Object);
    }

    private ProcedureEntity CreateProcedure(params VariableDefinition[] variables)
    {
        return new ProcedureEntity
        {
            Id = _procedureId,
            Name = "Test",
            RootNodeIds = [],
            Variables = variables.ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task AddVariableAsync_NotifiesTracker()
    {
        // Arrange
        var procedure = CreateProcedure();
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);

        var variable = new VariableDefinition
        {
            Name = "new_var",
            Type = new NumberType(),
            Source = VariableSource.SkillOutput
        };

        // Act
        await _service.AddVariableAsync(_procedureId, variable);

        // Assert
        _tracker.Verify(
            t => t.NotifyChanged(It.Is<IReadOnlyList<VariableDefinition>>(vars =>
                vars.Any(v => v.Name == "new_var"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateVariableAsync_NotifiesTracker()
    {
        // Arrange
        var existingVar = new VariableDefinition
        {
            Name = "existing_var",
            Type = new StringType()
        };
        var procedure = CreateProcedure(existingVar);
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);

        var updatedVar = existingVar with { Description = "Updated description" };

        // Act
        await _service.UpdateVariableAsync(_procedureId, "existing_var", updatedVar);

        // Assert
        _tracker.Verify(
            t => t.NotifyChanged(It.Is<IReadOnlyList<VariableDefinition>>(vars =>
                vars.Any(v => v.Name == "existing_var"))),
            Times.Once);
    }

    [Fact]
    public async Task RemoveVariableAsync_NotifiesTracker()
    {
        // Arrange
        var existingVar = new VariableDefinition
        {
            Name = "to_remove",
            Type = new BooleanType()
        };
        var procedure = CreateProcedure(existingVar);
        _procedureRepo.Setup(r => r.GetByIdAsync(_procedureId)).ReturnsAsync(procedure);
        _procedureRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcedureEntity>())).ReturnsAsync(true);

        // Act
        await _service.RemoveVariableAsync(_procedureId, "to_remove");

        // Assert
        _tracker.Verify(
            t => t.NotifyChanged(It.Is<IReadOnlyList<VariableDefinition>>(vars =>
                vars.All(v => v.Name != "to_remove"))),
            Times.Once);
    }
}