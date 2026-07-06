using FHOOE.Freydis.Domain.Entities.Common;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Domain.Common;

/// <summary>
///     Tests for TypedValue.
///     Tests written FIRST following TDD methodology.
/// </summary>
public class TypedValueTests
{
    #region Constructor Tests

    [Fact]
    public void Create_Should_StoreTypeAndValue()
    {
        // Arrange
        var type = new NumberType();
        var value = 42.5;

        // Act
        var typedValue = new TypedValue
        {
            Type = type,
            Value = value
        };

        // Assert
        typedValue.Type.Should().Be(type);
        typedValue.Value.Should().Be(value);
    }

    [Fact]
    public void Create_Should_AllowNullValue()
    {
        // Arrange
        var type = new StringType();

        // Act
        var typedValue = new TypedValue
        {
            Type = type,
            Value = null
        };

        // Assert
        typedValue.Type.Should().Be(type);
        typedValue.Value.Should().BeNull();
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Boolean_Should_CreateTypedBooleanValue()
    {
        // Arrange & Act
        var typedValue = TypedValue.Boolean(true);

        // Assert
        typedValue.Type.Should().BeOfType<BooleanType>();
        typedValue.Value.Should().Be(true);
    }

    [Fact]
    public void Number_Should_CreateTypedNumberValue()
    {
        // Arrange & Act
        var typedValue = TypedValue.Number(123.45);

        // Assert
        typedValue.Type.Should().BeOfType<NumberType>();
        typedValue.Value.Should().Be(123.45);
    }

    [Fact]
    public void String_Should_CreateTypedStringValue()
    {
        // Arrange & Act
        var typedValue = TypedValue.Text("hello");

        // Assert
        typedValue.Type.Should().BeOfType<StringType>();
        typedValue.Value.Should().Be("hello");
    }

    [Fact]
    public void Position_Should_CreateTypedPositionValue()
    {
        // Arrange
        var position = new Position { X = 1.0, Y = 2.0, Z = 3.0 };

        // Act
        var typedValue = TypedValue.Position(position);

        // Assert
        typedValue.Type.Should().BeOfType<PositionType>();
        typedValue.Value.Should().Be(position);
    }

    [Fact]
    public void PositionTag_Should_CreateTypedPositionTagValue()
    {
        // Arrange
        var positionTag = new PositionTag
        {
            Tag = "Home",
            Position = new Position { X = 1.0, Y = 2.0, Z = 3.0 }
        };

        // Act
        var typedValue = TypedValue.PositionTag(positionTag);

        // Assert
        typedValue.Type.Should().BeOfType<PositionTagType>();
        typedValue.Value.Should().Be(positionTag);
    }

    [Fact]
    public void SceneObject_Should_CreateTypedSceneObjectValue()
    {
        // Arrange
        var sceneObject = new SceneObject
        {
            Name = "Robot",
            Position = new Position { X = 5.0, Y = 6.0, Z = 7.0 }
        };

        // Act
        var typedValue = TypedValue.SceneObject(sceneObject);

        // Assert
        typedValue.Type.Should().BeOfType<SceneObjectType>();
        typedValue.Value.Should().Be(sceneObject);
    }

    [Fact]
    public void Enum_Should_CreateTypedEnumValue()
    {
        // Arrange
        var allowedValues = new List<string> { "Red", "Green", "Blue" };

        // Act
        var typedValue = TypedValue.Enum(allowedValues, "Red");

        // Assert
        typedValue.Type.Should().BeOfType<EnumType>();
        var enumType = typedValue.Type as EnumType;
        enumType!.AllowedValues.Should().BeEquivalentTo(allowedValues);
        typedValue.Value.Should().Be("Red");
    }

    [Fact]
    public void List_Should_CreateTypedListValue()
    {
        // Arrange
        var elementType = new NumberType();
        var listValue = new List<object> { 1.0, 2.0, 3.0 };

        // Act
        var typedValue = TypedValue.List(elementType, listValue);

        // Assert
        typedValue.Type.Should().BeOfType<ListType>();
        var listType = typedValue.Type as ListType;
        listType!.ElementType.Should().Be(elementType);
        typedValue.Value.Should().BeEquivalentTo(listValue);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void RecordEquality_Should_WorkForSameTypeAndValue()
    {
        // Arrange
        var value1 = TypedValue.Number(42.0);
        var value2 = TypedValue.Number(42.0);

        // Act & Assert
        value1.Should().Be(value2);
    }

    [Fact]
    public void RecordEquality_Should_DifferForDifferentValues()
    {
        // Arrange
        var value1 = TypedValue.Number(42.0);
        var value2 = TypedValue.Number(43.0);

        // Act & Assert
        value1.Should().NotBe(value2);
    }

    [Fact]
    public void RecordEquality_Should_DifferForDifferentTypes()
    {
        // Arrange
        var value1 = TypedValue.Number(42.0);
        var value2 = TypedValue.Text("42.0");

        // Act & Assert
        value1.Should().NotBe(value2);
    }

    [Fact]
    public void RecordEquality_Should_WorkForNullValues()
    {
        // Arrange
        var value1 = new TypedValue { Type = new StringType(), Value = null };
        var value2 = new TypedValue { Type = new StringType(), Value = null };

        // Act & Assert
        value1.Should().Be(value2);
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public void Create_Should_WorkWithComplexPosition()
    {
        // Arrange & Act
        var position = new Position
        {
            X = 10.5,
            Y = 20.3,
            Z = 30.7,
            Alpha = 0.1,
            Beta = 0.2,
            Gamma = 0.3
        };
        var typedValue = TypedValue.Position(position);

        // Assert
        typedValue.Value.Should().Be(position);
        var storedPosition = typedValue.Value as Position;
        storedPosition!.X.Should().Be(10.5);
        storedPosition.Y.Should().Be(20.3);
        storedPosition.Z.Should().Be(30.7);
        storedPosition.Alpha.Should().Be(0.1);
    }

    [Fact]
    public void Create_Should_WorkWithNestedListTypes()
    {
        // Arrange
        var innerListType = new ListType { ElementType = new NumberType() };
        var outerListType = new ListType { ElementType = innerListType };
        var nestedList = new List<object>
        {
            new List<object> { 1.0, 2.0 },
            new List<object> { 3.0, 4.0 }
        };

        // Act
        var typedValue = new TypedValue
        {
            Type = outerListType,
            Value = nestedList
        };

        // Assert
        typedValue.Type.Should().Be(outerListType);
        typedValue.Type.TypeName.Should().Be("List<List<Number>>");
        typedValue.Value.Should().BeEquivalentTo(nestedList);
    }

    #endregion
}