using FHOOE.Freydis.Domain.Entities.Common;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Domain.Common;

/// <summary>
///     Tests for TypedProperty using new TypedValue system.
///     Tests written FIRST following TDD methodology.
/// </summary>
public class TypedPropertyTests
{
    #region TypedProperty Collection Tests

    [Fact]
    public void PropertyList_Should_StoreMultipleProperties()
    {
        // Arrange & Act
        var properties = new List<TypedProperty>
        {
            new() { Name = "speed", Value = TypedValue.Number(100.0), Direction = PropertyDirection.Input },
            new() { Name = "enabled", Value = TypedValue.Boolean(true), Direction = PropertyDirection.Input },
            new() { Name = "name", Value = TypedValue.Text("Robot1"), Direction = PropertyDirection.Input }
        };

        // Assert
        properties.Should().HaveCount(3);
        properties[0].Name.Should().Be("speed");
        properties[1].Name.Should().Be("enabled");
        properties[2].Name.Should().Be("name");
    }

    #endregion

    #region Basic TypedProperty Tests

    [Fact]
    public void Create_Should_StoreNameAndValue()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "speed",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("speed");
        property.Value.Should().NotBeNull();
        property.Value.Type.Should().BeOfType<NumberType>();
        property.Value.Value.Should().Be(100.0);
    }

    [Fact]
    public void Create_Should_WorkWithBooleanValue()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "enabled",
            Value = TypedValue.Boolean(true),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("enabled");
        property.Value.Type.Should().BeOfType<BooleanType>();
        property.Value.Value.Should().Be(true);
    }

    [Fact]
    public void Create_Should_WorkWithStringValue()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "description",
            Value = TypedValue.Text("test description"),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("description");
        property.Value.Type.Should().BeOfType<StringType>();
        property.Value.Value.Should().Be("test description");
    }

    [Fact]
    public void Create_Should_WorkWithPositionValue()
    {
        // Arrange
        var position = new Position { X = 1.0, Y = 2.0, Z = 3.0 };

        // Act
        var property = new TypedProperty
        {
            Name = "target",
            Value = TypedValue.Position(position),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("target");
        property.Value.Type.Should().BeOfType<PositionType>();
        property.Value.Value.Should().Be(position);
    }

    [Fact]
    public void Create_Should_WorkWithPositionTagValue()
    {
        // Arrange
        var positionTag = new PositionTag
        {
            Tag = "Home",
            Position = new Position { X = 0, Y = 0, Z = 0 }
        };

        // Act
        var property = new TypedProperty
        {
            Name = "homeTag",
            Value = TypedValue.PositionTag(positionTag),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("homeTag");
        property.Value.Type.Should().BeOfType<PositionTagType>();
        property.Value.Value.Should().Be(positionTag);
    }

    [Fact]
    public void Create_Should_WorkWithSceneObjectValue()
    {
        // Arrange
        var sceneObject = new SceneObject
        {
            Name = "Robot",
            Position = new Position { X = 5, Y = 6, Z = 7 }
        };

        // Act
        var property = new TypedProperty
        {
            Name = "robot",
            Value = TypedValue.SceneObject(sceneObject),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("robot");
        property.Value.Type.Should().BeOfType<SceneObjectType>();
        property.Value.Value.Should().Be(sceneObject);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void RecordEquality_Should_WorkForSameNameAndValue()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "count",
            Value = TypedValue.Number(42.0),
            Direction = PropertyDirection.Input
        };
        var property2 = new TypedProperty
        {
            Name = "count",
            Value = TypedValue.Number(42.0),
            Direction = PropertyDirection.Input
        };

        // Act & Assert
        property1.Should().Be(property2);
    }

    [Fact]
    public void RecordEquality_Should_DifferForDifferentNames()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "count1",
            Value = TypedValue.Number(42.0),
            Direction = PropertyDirection.Input
        };
        var property2 = new TypedProperty
        {
            Name = "count2",
            Value = TypedValue.Number(42.0),
            Direction = PropertyDirection.Input
        };

        // Act & Assert
        property1.Should().NotBe(property2);
    }

    [Fact]
    public void RecordEquality_Should_DifferForDifferentValues()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "count",
            Value = TypedValue.Number(42.0),
            Direction = PropertyDirection.Input
        };
        var property2 = new TypedProperty
        {
            Name = "count",
            Value = TypedValue.Number(43.0),
            Direction = PropertyDirection.Input
        };

        // Act & Assert
        property1.Should().NotBe(property2);
    }

    #endregion

    #region Complex Value Tests

    [Fact]
    public void Create_Should_WorkWithEnumValue()
    {
        // Arrange
        var allowedValues = new List<string> { "Low", "Medium", "High" };

        // Act
        var property = new TypedProperty
        {
            Name = "priority",
            Value = TypedValue.Enum(allowedValues, "Medium"),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("priority");
        property.Value.Type.Should().BeOfType<EnumType>();
        var enumType = property.Value.Type as EnumType;
        enumType!.AllowedValues.Should().BeEquivalentTo(allowedValues);
        property.Value.Value.Should().Be("Medium");
    }

    [Fact]
    public void Create_Should_WorkWithListValue()
    {
        // Arrange
        var listValue = new List<object> { 1.0, 2.0, 3.0 };

        // Act
        var property = new TypedProperty
        {
            Name = "waypoints",
            Value = TypedValue.List(new NumberType(), listValue),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("waypoints");
        property.Value.Type.Should().BeOfType<ListType>();
        var listType = property.Value.Type as ListType;
        listType!.ElementType.Should().BeOfType<NumberType>();
        property.Value.Value.Should().BeEquivalentTo(listValue);
    }

    [Fact]
    public void Create_Should_AllowNullValue()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "optional",
            Value = new TypedValue
            {
                Type = new StringType(),
                Value = null
            },
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Name.Should().Be("optional");
        property.Value.Type.Should().BeOfType<StringType>();
        property.Value.Value.Should().BeNull();
    }

    #endregion

    #region Direction and Binding Tests

    [Fact]
    public void Create_Should_WorkWithInputDirection()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "inputValue",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input
        };

        // Assert
        property.Direction.Should().Be(PropertyDirection.Input);
    }

    [Fact]
    public void Create_Should_WorkWithOutputDirection()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "outputValue",
            Value = TypedValue.Number(200.0),
            Direction = PropertyDirection.Output
        };

        // Assert
        property.Direction.Should().Be(PropertyDirection.Output);
    }

    [Fact]
    public void Create_Should_WorkWithInputOutputDirection()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "ioValue",
            Value = TypedValue.Number(300.0),
            Direction = PropertyDirection.InputOutput
        };

        // Assert
        property.Direction.Should().Be(PropertyDirection.InputOutput);
    }

    [Fact]
    public void Create_Should_WorkWithVariableBinding()
    {
        // Arrange
        var binding = new VariableBinding
        {
            VariableName = "target_pos",
            Mode = BindingMode.Read
        };

        // Act
        var property = new TypedProperty
        {
            Name = "targetPosition",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = binding
        };

        // Assert
        property.Binding.Should().NotBeNull();
        property.Binding!.VariableName.Should().Be("target_pos");
        property.Binding.Mode.Should().Be(BindingMode.Read);
    }

    [Fact]
    public void Create_Should_AllowNullBinding()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "unboundProperty",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = null
        };

        // Assert
        property.Binding.Should().BeNull();
    }

    [Fact]
    public void Create_Should_WorkWithInputDirectionAndReadBinding()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "input",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = new VariableBinding
            {
                VariableName = "input_var",
                Mode = BindingMode.Read
            }
        };

        // Assert
        property.Direction.Should().Be(PropertyDirection.Input);
        property.Binding!.Mode.Should().Be(BindingMode.Read);
    }

    [Fact]
    public void Create_Should_WorkWithOutputDirectionAndWriteBinding()
    {
        // Arrange & Act
        var property = new TypedProperty
        {
            Name = "output",
            Value = TypedValue.Number(200.0),
            Direction = PropertyDirection.Output,
            Binding = new VariableBinding
            {
                VariableName = "output_var",
                Mode = BindingMode.Write
            }
        };

        // Assert
        property.Direction.Should().Be(PropertyDirection.Output);
        property.Binding!.Mode.Should().Be(BindingMode.Write);
    }

    [Fact]
    public void RecordEquality_Should_ConsiderDirection()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input
        };
        var property2 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Output
        };

        // Act & Assert
        property1.Should().NotBe(property2);
    }

    [Fact]
    public void RecordEquality_Should_ConsiderBinding()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = new VariableBinding
            {
                VariableName = "var1",
                Mode = BindingMode.Read
            }
        };
        var property2 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = new VariableBinding
            {
                VariableName = "var2",
                Mode = BindingMode.Read
            }
        };

        // Act & Assert
        property1.Should().NotBe(property2);
    }

    [Fact]
    public void RecordEquality_Should_WorkWithNullBindings()
    {
        // Arrange
        var property1 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = null
        };
        var property2 = new TypedProperty
        {
            Name = "value",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = null
        };

        // Act & Assert
        property1.Should().Be(property2);
    }

    #endregion
}