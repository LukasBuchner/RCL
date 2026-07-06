using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using FluentAssertions;
using DomainStringType = FHOOE.Freydis.Domain.Entities.Common.StringType;
using DomainBooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using DomainEnumType = FHOOE.Freydis.Domain.Entities.Common.EnumType;

namespace FHOOE.Freydis.GraphQLServer.Tests.Mappers;

/// <summary>
///     Tests for mapping between domain variable entities and GraphQL DTOs.
///     Verifies bidirectional transformation and data integrity.
/// </summary>
public class VariableMappingTests
{
    [Fact]
    public void MapToVariableDefinitionDto_WithStringVariable_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "qualityResult",
            Type = new DomainStringType(),
            DefaultValue = "OK",
            Scope = VariableScope.Procedure,
            Source = VariableSource.SkillOutput,
            Description = "Quality inspection result",
            IsReadOnly = false
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("qualityResult");
        dto.Type.Should().NotBeNull();
        dto.Type.TypeName.Should().Be("String");
        dto.DefaultValue.Should().Be("OK");
        dto.Scope.Should().Be(VariableScope.Procedure);
        dto.Source.Should().Be(VariableSource.SkillOutput);
        dto.Description.Should().Be("Quality inspection result");
        dto.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void MapToVariableDefinitionDto_WithNumberVariable_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "temperature",
            Type = new NumberType(),
            DefaultValue = 25.5,
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = "Temperature threshold",
            IsReadOnly = false
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("temperature");
        dto.Type.TypeName.Should().Be("Number");
        dto.DefaultValue.Should().Be(25.5);
    }

    [Fact]
    public void MapToVariableDefinitionDto_WithBooleanVariable_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "isEnabled",
            Type = new DomainBooleanType(),
            DefaultValue = true,
            Scope = VariableScope.Task,
            Source = VariableSource.RuntimeComputed,
            Description = null,
            IsReadOnly = true
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("isEnabled");
        dto.Type.TypeName.Should().Be("Boolean");
        dto.DefaultValue.Should().Be(true);
        dto.Scope.Should().Be(VariableScope.Task);
        dto.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void MapToVariableDefinitionDto_WithPositionVariable_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "targetPosition",
            Type = new PositionType(),
            DefaultValue = new Position { X = 1.0, Y = 2.0, Z = 3.0 },
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = "Target position for robot",
            IsReadOnly = false
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("targetPosition");
        dto.Type.TypeName.Should().Be("Position");
        dto.DefaultValue.Should().BeOfType<Position>();
        var position = dto.DefaultValue as Position;
        position.Should().NotBeNull();
        position!.X.Should().Be(1.0);
        position.Y.Should().Be(2.0);
        position.Z.Should().Be(3.0);
    }

    [Fact]
    public void MapToVariableDefinitionDto_WithEnumVariable_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "operationMode",
            Type = new DomainEnumType { AllowedValues = ["AUTO", "MANUAL", "MAINTENANCE"] },
            DefaultValue = "AUTO",
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = "Operation mode selection",
            IsReadOnly = false
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("operationMode");
        dto.Type.TypeName.Should().Be("Enum");
        dto.Type.Should().BeOfType<DomainEnumType>();
        var enumType = dto.Type as DomainEnumType;
        enumType!.AllowedValues.Should().BeEquivalentTo("AUTO", "MANUAL", "MAINTENANCE");
        dto.DefaultValue.Should().Be("AUTO");
    }

    [Fact]
    public void MapToVariableDefinitionDto_WithNullDefaultValue_MapsCorrectly()
    {
        // Arrange
        var domainVariable = new VariableDefinition
        {
            Name = "optionalValue",
            Type = new DomainStringType(),
            DefaultValue = null,
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = null,
            IsReadOnly = false
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToVariableDefinitionDto(domainVariable);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("optionalValue");
        dto.DefaultValue.Should().BeNull();
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void MapFromVariableDefinitionInput_WithStringType_MapsCorrectly()
    {
        // Arrange
        var input = new VariableDefinitionInput
        {
            Name = "testVar",
            Type = new DomainStringType(),
            DefaultValue = "testValue",
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = "Test variable",
            IsReadOnly = false
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromVariableDefinitionInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Name.Should().Be("testVar");
        domain.Type.Should().BeOfType<DomainStringType>();
        domain.DefaultValue.Should().Be("testValue");
        domain.Scope.Should().Be(VariableScope.Procedure);
        domain.Source.Should().Be(VariableSource.UserDefined);
        domain.Description.Should().Be("Test variable");
        domain.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void MapFromVariableValueInput_WithStringValue_MapsCorrectly()
    {
        // Arrange
        var input = new VariableValueInput
        {
            Name = "qualityResult",
            Value = "PASS"
        };

        // Act
        var (name, value) = GraphQlDtoMapperService.MapFromVariableValueInput(input);

        // Assert
        name.Should().Be("qualityResult");
        value.Should().Be("PASS");
    }

    [Fact]
    public void MapFromVariableValueInput_WithNumberValue_MapsCorrectly()
    {
        // Arrange
        var input = new VariableValueInput
        {
            Name = "temperature",
            Value = 42.5
        };

        // Act
        var (name, value) = GraphQlDtoMapperService.MapFromVariableValueInput(input);

        // Assert
        name.Should().Be("temperature");
        value.Should().Be(42.5);
    }

    [Fact]
    public void MapFromVariableValueInput_WithBooleanValue_MapsCorrectly()
    {
        // Arrange
        var input = new VariableValueInput
        {
            Name = "isActive",
            Value = true
        };

        // Act
        var (name, value) = GraphQlDtoMapperService.MapFromVariableValueInput(input);

        // Assert
        name.Should().Be("isActive");
        value.Should().Be(true);
    }

    [Fact]
    public void MapFromVariableValueInput_WithPositionValue_MapsCorrectly()
    {
        // Arrange
        var position = new Position { X = 10.0, Y = 20.0, Z = 30.0 };
        var input = new VariableValueInput
        {
            Name = "targetPos",
            Value = position
        };

        // Act
        var (name, value) = GraphQlDtoMapperService.MapFromVariableValueInput(input);

        // Assert
        name.Should().Be("targetPos");
        value.Should().BeOfType<Position>();
        var pos = value as Position;
        pos!.X.Should().Be(10.0);
        pos.Y.Should().Be(20.0);
        pos.Z.Should().Be(30.0);
    }

    [Fact]
    public void MapVariableValueInputsToDictionary_WithMultipleVariables_MapsCorrectly()
    {
        // Arrange
        var inputs = new List<VariableValueInput>
        {
            new() { Name = "var1", Value = "value1" },
            new() { Name = "var2", Value = 42.0 },
            new() { Name = "var3", Value = true }
        };

        // Act
        var dictionary = GraphQlDtoMapperService.MapVariableValueInputsToDictionary(inputs);

        // Assert
        dictionary.Should().HaveCount(3);
        dictionary["var1"].Should().Be("value1");
        dictionary["var2"].Should().Be(42.0);
        dictionary["var3"].Should().Be(true);
    }

    [Fact]
    public void MapVariableValueInputsToDictionary_WithEmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var inputs = new List<VariableValueInput>();

        // Act
        var dictionary = GraphQlDtoMapperService.MapVariableValueInputsToDictionary(inputs);

        // Assert
        dictionary.Should().NotBeNull();
        dictionary.Should().BeEmpty();
    }

    [Fact]
    public void MapVariableValueInputsToDictionary_WithDuplicateNames_UsesLastValue()
    {
        // Arrange
        var inputs = new List<VariableValueInput>
        {
            new() { Name = "var1", Value = "first" },
            new() { Name = "var1", Value = "last" }
        };

        // Act
        var dictionary = GraphQlDtoMapperService.MapVariableValueInputsToDictionary(inputs);

        // Assert
        dictionary.Should().HaveCount(1);
        dictionary["var1"].Should().Be("last");
    }
}