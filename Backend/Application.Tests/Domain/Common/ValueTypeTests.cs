using FHOOE.Freydis.Domain.Entities.Common;
using FluentAssertions;
using ValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.Application.Tests.Domain.Common;

/// <summary>
///     Tests for the unified ValueType system.
///     Tests written FIRST following TDD methodology.
/// </summary>
public class ValueTypeTests
{
    #region Polymorphism Tests

    [Fact]
    public void AllTypes_Should_BeAssignableToValueType()
    {
        // Arrange & Act
        ValueType[] types =
        [
            new BooleanType(),
            new NumberType(),
            new StringType(),
            new PositionType(),
            new PositionTagType(),
            new SceneObjectType(),
            new EnumType { AllowedValues = ["A"] },
            new ListType { ElementType = new NumberType() }
        ];

        // Assert
        types.Should().AllBeAssignableTo<ValueType>();
    }

    #endregion

    #region Basic Type Tests

    [Fact]
    public void BooleanType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var boolType = new BooleanType();

        // Assert
        boolType.ClrType.Should().Be(typeof(bool));
    }

    [Fact]
    public void BooleanType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var boolType = new BooleanType();

        // Assert
        boolType.TypeName.Should().Be("Boolean");
    }

    [Fact]
    public void NumberType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var numberType = new NumberType();

        // Assert
        numberType.ClrType.Should().Be(typeof(double));
    }

    [Fact]
    public void NumberType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var numberType = new NumberType();

        // Assert
        numberType.TypeName.Should().Be("Number");
    }

    [Fact]
    public void StringType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var stringType = new StringType();

        // Assert
        stringType.ClrType.Should().Be(typeof(string));
    }

    [Fact]
    public void StringType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var stringType = new StringType();

        // Assert
        stringType.TypeName.Should().Be("String");
    }

    [Fact]
    public void PositionType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var positionType = new PositionType();

        // Assert
        positionType.ClrType.Should().Be(typeof(Position));
    }

    [Fact]
    public void PositionType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var positionType = new PositionType();

        // Assert
        positionType.TypeName.Should().Be("Position");
    }

    [Fact]
    public void PositionTagType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var positionTagType = new PositionTagType();

        // Assert
        positionTagType.ClrType.Should().Be(typeof(PositionTag));
    }

    [Fact]
    public void PositionTagType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var positionTagType = new PositionTagType();

        // Assert
        positionTagType.TypeName.Should().Be("PositionTag");
    }

    [Fact]
    public void SceneObjectType_Should_HaveCorrectClrType()
    {
        // Arrange & Act
        var sceneObjectType = new SceneObjectType();

        // Assert
        sceneObjectType.ClrType.Should().Be(typeof(SceneObject));
    }

    [Fact]
    public void SceneObjectType_Should_HaveCorrectTypeName()
    {
        // Arrange & Act
        var sceneObjectType = new SceneObjectType();

        // Assert
        sceneObjectType.TypeName.Should().Be("SceneObject");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void BooleanType_Should_BeEqualToAnotherBooleanType()
    {
        // Arrange
        var type1 = new BooleanType();
        var type2 = new BooleanType();

        // Act & Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void NumberType_Should_BeEqualToAnotherNumberType()
    {
        // Arrange
        var type1 = new NumberType();
        var type2 = new NumberType();

        // Act & Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void StringType_Should_NotBeEqualToNumberType()
    {
        // Arrange
        ValueType stringType = new StringType();
        ValueType numberType = new NumberType();

        // Act & Assert
        stringType.Should().NotBe(numberType);
    }

    #endregion

    #region EnumType Tests

    [Fact]
    public void EnumType_Should_StoreAllowedValues()
    {
        // Arrange
        var allowedValues = new List<string> { "Red", "Green", "Blue" };

        // Act
        var enumType = new EnumType { AllowedValues = allowedValues };

        // Assert
        enumType.AllowedValues.Should().BeEquivalentTo(allowedValues);
    }

    [Fact]
    public void EnumType_Should_HaveStringClrType()
    {
        // Arrange & Act
        var enumType = new EnumType { AllowedValues = ["A", "B"] };

        // Assert
        enumType.ClrType.Should().Be(typeof(string));
    }

    [Fact]
    public void EnumType_Should_HaveEnumTypeName()
    {
        // Arrange & Act
        var enumType = new EnumType { AllowedValues = ["A", "B"] };

        // Assert
        enumType.TypeName.Should().Be("Enum");
    }

    [Fact]
    public void EnumType_Should_BeEqualWhenAllowedValuesMatch()
    {
        // Arrange
        var allowedValues = new List<string> { "A", "B", "C" };
        var type1 = new EnumType { AllowedValues = allowedValues };
        var type2 = new EnumType { AllowedValues = allowedValues };

        // Act & Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void EnumType_Should_NotBeEqualWhenAllowedValuesDiffer()
    {
        // Arrange
        var type1 = new EnumType { AllowedValues = ["A", "B"] };
        var type2 = new EnumType { AllowedValues = ["A", "C"] };

        // Act & Assert
        type1.Should().NotBe(type2);
    }

    #endregion

    #region ListType Tests

    [Fact]
    public void ListType_Should_StoreElementType()
    {
        // Arrange
        var elementType = new NumberType();

        // Act
        var listType = new ListType { ElementType = elementType };

        // Assert
        listType.ElementType.Should().Be(elementType);
    }

    [Fact]
    public void ListType_Should_HaveListClrType()
    {
        // Arrange & Act
        var listType = new ListType { ElementType = new StringType() };

        // Assert
        listType.ClrType.Should().Be(typeof(List<object>));
    }

    [Fact]
    public void ListType_Should_IncludeElementTypeInTypeName()
    {
        // Arrange & Act
        var listType = new ListType { ElementType = new NumberType() };

        // Assert
        listType.TypeName.Should().Be("List<Number>");
    }

    [Fact]
    public void ListType_Should_SupportNestedLists()
    {
        // Arrange
        var innerList = new ListType { ElementType = new StringType() };

        // Act
        var outerList = new ListType { ElementType = innerList };

        // Assert
        outerList.ElementType.Should().Be(innerList);
        outerList.TypeName.Should().Be("List<List<String>>");
    }

    [Fact]
    public void ListType_Should_BeEqualWhenElementTypesMatch()
    {
        // Arrange
        var type1 = new ListType { ElementType = new BooleanType() };
        var type2 = new ListType { ElementType = new BooleanType() };

        // Act & Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void ListType_Should_NotBeEqualWhenElementTypesDiffer()
    {
        // Arrange
        var type1 = new ListType { ElementType = new BooleanType() };
        var type2 = new ListType { ElementType = new NumberType() };

        // Act & Assert
        type1.Should().NotBe(type2);
    }

    [Fact]
    public void ListType_Should_SupportComplexElementTypes()
    {
        // Arrange & Act
        var listOfPositions = new ListType { ElementType = new PositionType() };
        var listOfSceneObjects = new ListType { ElementType = new SceneObjectType() };

        // Assert
        listOfPositions.TypeName.Should().Be("List<Position>");
        listOfSceneObjects.TypeName.Should().Be("List<SceneObject>");
    }

    #endregion
}