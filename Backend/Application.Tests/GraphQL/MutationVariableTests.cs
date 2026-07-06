using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DomainProcedure = FHOOE.Freydis.Domain.Entities.Procedure.Procedure;

namespace FHOOE.Freydis.Application.Tests.GraphQL;

/// <summary>
///     T<IRepository
///     <Procedure>
///         >perations on Procedures.
///         Following TDD: Write tests FIRS<ProcedureVariableService>ummary>
public class MutationVariableTests
{
    private readonly Mock<ILogger<ProcedureVariableService>> _loggerMock;
    private readonly Mock<IRepository<DomainProcedure>> _procedureRepositoryMock;
    private readonly ProcedureVariableService _service;

    public MutationVariableTests()
    {
        _procedureRepositoryMock = new Mock<IRepository<DomainProcedure>>();
        _loggerMock = new Mock<ILogger<ProcedureVariableService>>();
        _service = new ProcedureVariableService(
            _procedureRepositoryMock.Object,
            new Mock<IProcedureVariableChangeTracker>().Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private static DomainProcedure CreateTestProcedure(Guid id)
    {
        return new DomainProcedure
        {
            Id = id,
            Name = "Test Procedure",
            Description = "Test procedure for variable management",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = new List<Guid>().AsReadOnly(),
            Variables = new List<VariableDefinition>().AsReadOnly()
        };
    }

    #endregion

    #region Add Variable Tests

    [Fact]
    public async Task AddVariableToProcedure_ValidInput_AddsVariable()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        var variable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType(),
            DefaultValue = "pending",
            Description = "Current status of the procedure"
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        _procedureRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddVariableAsync(procedureId, variable);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().ContainSingle();
        result.Variables.First().Name.Should().Be("status");
        result.Variables.First().Type.Should().BeOfType<StringType>();
        result.Variables.First().DefaultValue.Should().Be("pending");

        _procedureRepositoryMock.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.Variables.Count == 1 &&
            p.Variables.First().Name == "status"
        )), Times.Once);
    }

    [Fact]
    public async Task AddVariableToProcedure_DuplicateName_ThrowsException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        // Add existing variable
        var existingVariable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType(),
            DefaultValue = "active"
        };

        var procedureWithVariable = procedure with
        {
            Variables = new List<VariableDefinition> { existingVariable }.AsReadOnly()
        };

        var duplicateVariable = new VariableDefinition
        {
            Name = "status", // Same name
            Type = new NumberType(),
            DefaultValue = 1.0
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedureWithVariable);

        // Act & Assert
        var act = async () => await _service.AddVariableAsync(procedureId, duplicateVariable);

        await act.Should().ThrowAsync<VariableAlreadyExistsException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task AddVariableToProcedure_ProcedureNotFound_ThrowsException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var variable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType()
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync((DomainProcedure?)null);

        // Act & Assert
        var act = async () => await _service.AddVariableAsync(procedureId, variable);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Update Variable Tests

    [Fact]
    public async Task UpdateProcedureVariable_ExistingVariable_UpdatesSuccessfully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        var existingVariable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType(),
            DefaultValue = "pending"
        };

        var procedureWithVariable = procedure with
        {
            Variables = new List<VariableDefinition> { existingVariable }.AsReadOnly()
        };

        var updatedVariable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType(),
            DefaultValue = "completed", // Changed
            Description = "Updated description"
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedureWithVariable);

        _procedureRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateVariableAsync(procedureId, "status", updatedVariable);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().ContainSingle();
        result.Variables.First().DefaultValue.Should().Be("completed");
        result.Variables.First().Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateProcedureVariable_NonExistentVariable_ThrowsException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        var updatedVariable = new VariableDefinition
        {
            Name = "nonexistent",
            Type = new StringType()
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        // Act & Assert
        var act = async () => await _service.UpdateVariableAsync(procedureId, "nonexistent", updatedVariable);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateProcedureVariable_RenameToExistingName_ThrowsException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        var variable1 = new VariableDefinition
        {
            Name = "status",
            Type = new StringType()
        };

        var variable2 = new VariableDefinition
        {
            Name = "quality",
            Type = new NumberType()
        };

        var procedureWithVariables = procedure with
        {
            Variables = new List<VariableDefinition> { variable1, variable2 }.AsReadOnly()
        };

        // Try to rename "status" to "quality" (which already exists)
        var updatedVariable = new VariableDefinition
        {
            Name = "quality",
            Type = new StringType()
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedureWithVariables);

        // Act & Assert
        var act = async () => await _service.UpdateVariableAsync(procedureId, "status", updatedVariable);

        await act.Should().ThrowAsync<VariableAlreadyExistsException>()
            .WithMessage("*already exists*");
    }

    #endregion

    #region Remove Variable Tests

    [Fact]
    public async Task RemoveProcedureVariable_ExistingVariable_RemovesSuccessfully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        var variable = new VariableDefinition
        {
            Name = "status",
            Type = new StringType()
        };

        var procedureWithVariable = procedure with
        {
            Variables = new List<VariableDefinition> { variable }.AsReadOnly()
        };

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedureWithVariable);

        _procedureRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RemoveVariableAsync(procedureId, "status");

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().BeEmpty();

        _procedureRepositoryMock.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.Variables.Count == 0
        )), Times.Once);
    }

    [Fact]
    public async Task RemoveProcedureVariable_NonExistentVariable_ThrowsException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateTestProcedure(procedureId);

        _procedureRepositoryMock
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        // Act & Assert
        var act = async () => await _service.RemoveVariableAsync(procedureId, "nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion
}