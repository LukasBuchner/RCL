using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Domain.Variables;

/// <summary>
///     Tests for VariableDefinition using new ValueType system.
///     Updated to use ValueType instead of VariableType enum.
/// </summary>
public class VariableDefinitionTests
{
    [Fact]
    public void Create_Should_SetAllRequiredProperties_When_ProvidedValidValues()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "testVar",
            Type = new NumberType()
        };

        // Assert
        definition.Name.Should().Be("testVar");
        definition.Type.Should().BeOfType<NumberType>();
    }

    [Fact]
    public void Create_Should_SetDefaultValues_When_OptionalPropertiesNotProvided()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "testVar",
            Type = new StringType()
        };

        // Assert
        definition.DefaultValue.Should().BeNull();
        definition.Scope.Should().Be(VariableScope.Procedure);
        definition.Source.Should().Be(VariableSource.UserDefined);
        definition.Description.Should().BeNull();
        definition.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_SetOptionalProperties_When_Provided()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "counter",
            Type = new NumberType(),
            DefaultValue = 42,
            Scope = VariableScope.Task,
            Source = VariableSource.SkillOutput,
            Description = "A counter variable",
            IsReadOnly = true
        };

        // Assert
        definition.DefaultValue.Should().Be(42);
        definition.Scope.Should().Be(VariableScope.Task);
        definition.Source.Should().Be(VariableSource.SkillOutput);
        definition.Description.Should().Be("A counter variable");
        definition.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void ReadOnlyFlag_Should_WorkCorrectly_When_SetToTrue()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "constant",
            Type = new StringType(),
            IsReadOnly = true
        };

        // Assert
        definition.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_Should_WorkCorrectly_When_ComparingInstances()
    {
        // Arrange
        var definition1 = new VariableDefinition
        {
            Name = "var1",
            Type = new BooleanType(),
            DefaultValue = true
        };

        var definition2 = new VariableDefinition
        {
            Name = "var1",
            Type = new BooleanType(),
            DefaultValue = true
        };

        var definition3 = new VariableDefinition
        {
            Name = "var2",
            Type = new BooleanType(),
            DefaultValue = true
        };

        // Act & Assert
        definition1.Should().Be(definition2);
        definition1.Should().NotBe(definition3);
    }

    #region New Tests for ValueType System

    [Fact]
    public void Create_Should_WorkWithBooleanType()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "isEnabled",
            Type = new BooleanType()
        };

        // Assert
        definition.Type.Should().BeOfType<BooleanType>();
        definition.Type.TypeName.Should().Be("Boolean");
    }

    [Fact]
    public void Create_Should_WorkWithPositionType()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "targetPosition",
            Type = new PositionType()
        };

        // Assert
        definition.Type.Should().BeOfType<PositionType>();
        definition.Type.TypeName.Should().Be("Position");
    }

    [Fact]
    public void Create_Should_WorkWithEnumType()
    {
        // Arrange
        var allowedValues = new List<string> { "Low", "Medium", "High" };

        // Act
        var definition = new VariableDefinition
        {
            Name = "priority",
            Type = new EnumType { AllowedValues = allowedValues }
        };

        // Assert
        definition.Type.Should().BeOfType<EnumType>();
        var enumType = definition.Type as EnumType;
        enumType!.AllowedValues.Should().BeEquivalentTo(allowedValues);
    }

    [Fact]
    public void Create_Should_WorkWithListType()
    {
        // Arrange & Act
        var definition = new VariableDefinition
        {
            Name = "waypoints",
            Type = new ListType { ElementType = new PositionType() }
        };

        // Assert
        definition.Type.Should().BeOfType<ListType>();
        var listType = definition.Type as ListType;
        listType!.ElementType.Should().BeOfType<PositionType>();
        definition.Type.TypeName.Should().Be("List<Position>");
    }

    [Fact]
    public void Create_Should_WorkWithNestedListType()
    {
        // Arrange
        var innerList = new ListType { ElementType = new NumberType() };

        // Act
        var definition = new VariableDefinition
        {
            Name = "matrix",
            Type = new ListType { ElementType = innerList }
        };

        // Assert
        definition.Type.Should().BeOfType<ListType>();
        definition.Type.TypeName.Should().Be("List<List<Number>>");
    }

    #endregion
}