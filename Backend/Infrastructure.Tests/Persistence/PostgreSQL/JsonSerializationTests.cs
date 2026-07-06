using System.Text.Json;
using System.Text.Json.Serialization;
using DomainValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.PostgreSQL;

/// <summary>
///     Tests for JSON serialization of domain entities using the same
///     <see cref="JsonSerializerOptions" /> as <see cref="GenericPostgresRepository{T}" />.
///     Verifies that all polymorphic types, value types, and complex objects
///     serialize and deserialize correctly for PostgreSQL JSONB storage.
/// </summary>
public class JsonSerializationTests
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TypeHierarchyJsonConverter()
        }
    };

    [Fact]
    public void VariableDefinition_WithDefaultValue_RoundTrips_Correctly()
    {
        // Arrange
        var variableDef = new VariableDefinition
        {
            Name = "testVar",
            Type = new NumberType(),
            DefaultValue = 42.0,
            Scope = VariableScope.Procedure,
            Source = VariableSource.UserDefined,
            Description = "Test variable",
            IsReadOnly = false
        };

        // Act - Serialize to JSON
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);

        // Act - Deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("testVar");
        deserialized.Type.Should().BeOfType<NumberType>();
        deserialized.Scope.Should().Be(VariableScope.Procedure);
        deserialized.Source.Should().Be(VariableSource.UserDefined);
        deserialized.Description.Should().Be("Test variable");
        deserialized.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void VariableDefinition_WithNullDefaultValue_RoundTrips_Correctly()
    {
        // Arrange
        var variableDef = new VariableDefinition
        {
            Name = "nullVar",
            Type = new StringType(),
            DefaultValue = null,
            Scope = VariableScope.Task,
            Source = VariableSource.SkillOutput
        };

        // Act
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("nullVar");
        deserialized.Type.Should().BeOfType<StringType>();
        deserialized.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void VariableDefinition_WithComplexDefaultValue_RoundTrips_Correctly()
    {
        // Arrange
        var position = new Position { X = 1.0, Y = 2.0, Z = 3.0, Alpha = 0, Beta = 0, Gamma = 0 };
        var variableDef = new VariableDefinition
        {
            Name = "posVar",
            Type = new PositionType(),
            DefaultValue = position
        };

        // Act
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("posVar");
        deserialized.Type.Should().BeOfType<PositionType>();

        var defaultValue = deserialized.DefaultValue;
        defaultValue.Should().NotBeNull();

        // JSON deserialization returns a JsonElement for object-typed properties
        // Verify the position values through the JsonElement
        var jsonElement = defaultValue.Should().BeAssignableTo<JsonElement>().Subject;
        jsonElement.GetProperty("x").GetDouble().Should().Be(1.0);
        jsonElement.GetProperty("y").GetDouble().Should().Be(2.0);
        jsonElement.GetProperty("z").GetDouble().Should().Be(3.0);
    }

    [Fact]
    public void VariableContext_WithValues_RoundTrips_Correctly()
    {
        // Arrange
        var context = new VariableContext
        {
            ProcedureExecutionId = Guid.NewGuid(),
            LastUpdatedUtc = DateTime.UtcNow
        };
        context.SetValue("var1", "stringValue");
        context.SetValue("var2", 42.0);
        context.SetValue("var3", true);

        // Act
        var json = JsonSerializer.Serialize(context, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableContext>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ProcedureExecutionId.Should().Be(context.ProcedureExecutionId);
    }

    [Theory]
    [InlineData(typeof(BooleanType))]
    [InlineData(typeof(NumberType))]
    [InlineData(typeof(StringType))]
    [InlineData(typeof(PositionType))]
    [InlineData(typeof(PositionTagType))]
    [InlineData(typeof(SceneObjectType))]
    public void ValueType_Polymorphic_Serialization_Works(Type valueTypeType)
    {
        // Arrange
        var valueType = (DomainValueType)Activator.CreateInstance(valueTypeType)!;
        var variableDef = new VariableDefinition
        {
            Name = "test",
            Type = valueType
        };

        // Act
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType(valueTypeType);
        deserialized.Type.TypeName.Should().Be(valueType.TypeName);
    }

    [Fact]
    public void EnumType_WithAllowedValues_RoundTrips_Correctly()
    {
        // Arrange
        var enumType = new EnumType
        {
            AllowedValues = ["Good", "Bad", "Uncertain"]
        };
        var variableDef = new VariableDefinition
        {
            Name = "quality",
            Type = enumType
        };

        // Act
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<EnumType>();
        var deserializedEnum = (EnumType)deserialized.Type;
        deserializedEnum.AllowedValues.Should().BeEquivalentTo("Good", "Bad", "Uncertain");
    }

    [Fact]
    public void ListType_WithElementType_RoundTrips_Correctly()
    {
        // Arrange
        var listType = new ListType
        {
            ElementType = new NumberType()
        };
        var variableDef = new VariableDefinition
        {
            Name = "numbers",
            Type = listType
        };

        // Act
        var json = JsonSerializer.Serialize(variableDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableDefinition>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<ListType>();
        var deserializedList = (ListType)deserialized.Type;
        deserializedList.ElementType.Should().BeOfType<NumberType>();
        deserializedList.TypeName.Should().Be("List<Number>");
    }

    [Theory]
    [InlineData(typeof(SimpleVariableSelector), "quality_result")]
    [InlineData(typeof(ExpressionSelector), "temperature > 100")]
    [InlineData(typeof(ExpressionSelector), "Math.Max(temp1, temp2) > threshold")]
    public void SelectorExpression_Polymorphic_Serialization_Works(Type selectorType, string expression)
    {
        // Arrange
        var selector = (SelectorExpression)Activator.CreateInstance(selectorType)!;
        selector = selector with { Expression = expression };

        // Act
        var json = JsonSerializer.Serialize(selector, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SelectorExpression>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().BeOfType(selectorType);
        deserialized!.Expression.Should().Be(expression);
    }

    [Fact]
    public void ConditionalBranch_Serializes_Without_TargetNode()
    {
        // Arrange
        var branch = new ConditionalBranch
        {
            Name = "GoodBranch",
            Condition = "Good",
            Priority = 1,
            TargetNodeId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(branch, JsonOptions);

        // Assert - JSON should contain expected fields but NOT targetNode
        json.Should().Contain("name");
        json.Should().Contain("condition");
        json.Should().Contain("targetNodeId");
        // TargetNode should NOT be serialized ([JsonIgnore])
        // Use exact key match to avoid matching "targetNodeId"
        json.Should().NotContain("\"targetNode\"");
    }

    [Fact]
    public void RouterNode_WithSelectorAndBranches_RoundTrips_Correctly()
    {
        // Arrange
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            RouterTask = new RouterTask
            {
                Name = "QualityRouter",
                Description = "Routes based on quality",
                StartTime = 0,
                Duration = 5,
                Selector = new SimpleVariableSelector { Expression = "quality_result" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Good", Condition = "Good", Priority = 1, TargetNodeId = Guid.NewGuid() },
                    new() { Name = "Bad", Condition = "Bad", Priority = 2, TargetNodeId = Guid.NewGuid() },
                    new() { Name = "Default", Condition = null, Priority = 999, TargetNodeId = Guid.NewGuid() }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(routerNode, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RouterNode>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(routerNode.Id);
        deserialized.RouterTask.Name.Should().Be("QualityRouter");
        deserialized.RouterTask.Selector.Should().BeOfType<SimpleVariableSelector>();
        deserialized.RouterTask.Selector!.Expression.Should().Be("quality_result");
        deserialized.RouterTask.Branches.Should().HaveCount(3);
        deserialized.RouterTask.Branches![0].Name.Should().Be("Good");
        deserialized.RouterTask.Branches![2].IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void Property_WithDirectionAndBinding_RoundTrips_Correctly()
    {
        // Arrange
        var property = new TypedProperty
        {
            Name = "target_position",
            Value = TypedValue.Number(100.0),
            Direction = PropertyDirection.Input,
            Binding = new VariableBinding
            {
                VariableName = "current_position",
                Mode = BindingMode.ReadWrite,
                TransformExpression = "value * 2"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(property, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedProperty>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("target_position");
        deserialized.Value.Should().NotBeNull();
        deserialized.Value.Type.Should().BeOfType<NumberType>();
        deserialized.Direction.Should().Be(PropertyDirection.Input);
        deserialized.Binding.Should().NotBeNull();
        deserialized.Binding!.VariableName.Should().Be("current_position");
        deserialized.Binding.Mode.Should().Be(BindingMode.ReadWrite);
        deserialized.Binding.TransformExpression.Should().Be("value * 2");
    }

    [Fact]
    public void TypedValue_Serializes_Correctly()
    {
        // Arrange
        var typedValue = TypedValue.Number(42.5);

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<NumberType>();
        deserialized.Value.Should().Be(42.5);
    }

    [Fact]
    public void VariableValue_Serializes_Correctly()
    {
        // Arrange
        var variableValue = new VariableValue
        {
            Name = "testVar",
            Value = "testValue",
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = "TestAgent"
        };

        // Act
        var json = JsonSerializer.Serialize(variableValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<VariableValue>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("testVar");
        deserialized.LastUpdatedBy.Should().Be("TestAgent");
    }

    [Fact]
    public void TypedValue_WithPosition_Serializes_And_Deserializes()
    {
        // Arrange
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

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<PositionType>();
        deserialized.Value.Should().NotBeNull();
        deserialized.Value.Should().BeOfType<Position>();
        var deserializedPos = (Position)deserialized.Value!;
        deserializedPos.X.Should().Be(10.5);
        deserializedPos.Y.Should().Be(20.3);
        deserializedPos.Z.Should().Be(30.7);
        deserializedPos.Alpha.Should().Be(0.1);
        deserializedPos.Beta.Should().Be(0.2);
        deserializedPos.Gamma.Should().Be(0.3);
    }

    [Fact]
    public void TypedValue_WithPositionTag_Serializes_And_Deserializes()
    {
        // Arrange
        var positionTag = new PositionTag
        {
            Id = Guid.NewGuid(),
            Tag = "HomePosition",
            Position = new Position { X = 1.0, Y = 2.0, Z = 3.0, Alpha = 0, Beta = 0, Gamma = 0 }
        };
        var typedValue = TypedValue.PositionTag(positionTag);

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<PositionTagType>();
        deserialized.Value.Should().NotBeNull();
        deserialized.Value.Should().BeOfType<PositionTag>();
        var deserializedTag = (PositionTag)deserialized.Value!;
        deserializedTag.Id.Should().Be(positionTag.Id);
        deserializedTag.Tag.Should().Be("HomePosition");
        deserializedTag.Position.X.Should().Be(1.0);
        deserializedTag.Position.Y.Should().Be(2.0);
        deserializedTag.Position.Z.Should().Be(3.0);
    }

    [Fact]
    public void Property_WithPositionValue_Serializes_And_Deserializes()
    {
        // Arrange
        var position = new Position { X = 100.0, Y = 200.0, Z = 300.0, Alpha = 1.5, Beta = 2.5, Gamma = 3.5 };
        var property = new TypedProperty
        {
            Name = "target_position",
            Value = TypedValue.Position(position),
            Direction = PropertyDirection.Input
        };

        // Act
        var json = JsonSerializer.Serialize(property, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedProperty>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("target_position");
        deserialized.Direction.Should().Be(PropertyDirection.Input);
        deserialized.Value.Should().NotBeNull();
        deserialized.Value.Type.Should().BeOfType<PositionType>();
    }

    [Fact]
    public void Property_WithPositionTagValue_Serializes_And_Deserializes()
    {
        // Arrange
        var positionTag = new PositionTag
        {
            Id = Guid.NewGuid(),
            Tag = "PickupLocation",
            Position = new Position { X = 50.0, Y = 60.0, Z = 70.0, Alpha = 0, Beta = 0, Gamma = 0 }
        };
        var property = new TypedProperty
        {
            Name = "pickup_tag",
            Value = TypedValue.PositionTag(positionTag),
            Direction = PropertyDirection.Output
        };

        // Act
        var json = JsonSerializer.Serialize(property, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedProperty>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("pickup_tag");
        deserialized.Direction.Should().Be(PropertyDirection.Output);
        deserialized.Value.Should().NotBeNull();
        deserialized.Value.Type.Should().BeOfType<PositionTagType>();
    }

    [Fact]
    public void Skill_WithPositionProperty_Serializes_And_Deserializes()
    {
        // Arrange
        var position = new Position { X = 10.0, Y = 20.0, Z = 30.0, Alpha = 0, Beta = 0, Gamma = 0 };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "MoveTo",
            Description = "Moves the robot to a target position",
            Properties =
            [
                new TypedProperty
                {
                    Name = "target_position",
                    Value = TypedValue.Position(position),
                    Direction = PropertyDirection.Input
                },

                new TypedProperty
                {
                    Name = "speed",
                    Value = TypedValue.Number(100.0),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(skill, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Skill>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(skill.Id);
        deserialized.Name.Should().Be("MoveTo");
        deserialized.Properties.Should().HaveCount(2);

        // Verify Position property
        var positionProperty = deserialized.Properties.First(p => p.Name == "target_position");
        positionProperty.Value.Type.Should().BeOfType<PositionType>();

        // Verify Number property still works
        var speedProperty = deserialized.Properties.First(p => p.Name == "speed");
        speedProperty.Value.Type.Should().BeOfType<NumberType>();
    }

    [Fact]
    public void Skill_WithPositionTagProperty_Serializes_And_Deserializes()
    {
        // Arrange
        var positionTag = new PositionTag
        {
            Id = Guid.NewGuid(),
            Tag = "SafeZone",
            Position = new Position { X = 100.0, Y = 200.0, Z = 300.0, Alpha = 0, Beta = 0, Gamma = 0 }
        };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "MoveToTag",
            Description = "Moves the robot to a tagged position",
            Properties =
            [
                new TypedProperty
                {
                    Name = "target_tag",
                    Value = TypedValue.PositionTag(positionTag),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(skill, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Skill>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(skill.Id);
        deserialized.Name.Should().Be("MoveToTag");
        deserialized.Properties.Should().HaveCount(1);

        // Verify PositionTag property — Value must be PositionTag, NOT JsonElement
        var tagProperty = deserialized.Properties.First();
        tagProperty.Value.Type.Should().BeOfType<PositionTagType>();
        tagProperty.Value.Value.Should().BeOfType<PositionTag>();
        var deserializedTag = (PositionTag)tagProperty.Value.Value!;
        deserializedTag.Id.Should().Be(positionTag.Id);
        deserializedTag.Tag.Should().Be("SafeZone");
        deserializedTag.Position.X.Should().Be(100.0);
    }

    [Fact]
    public void TypedValue_WithSceneObject_Deserializes_To_Correct_Type()
    {
        // Arrange
        var sceneObject = new SceneObject
        {
            Id = Guid.NewGuid(),
            Name = "WorkPiece",
            Position = new Position { X = 5.0, Y = 10.0, Z = 15.0, Alpha = 0, Beta = 0, Gamma = 0 }
        };
        var typedValue = TypedValue.SceneObject(sceneObject);

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert — Value must be SceneObject, NOT JsonElement
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<SceneObjectType>();
        deserialized.Value.Should().BeOfType<SceneObject>();
        var deserializedObj = (SceneObject)deserialized.Value!;
        deserializedObj.Id.Should().Be(sceneObject.Id);
        deserializedObj.Name.Should().Be("WorkPiece");
    }

    [Fact]
    public void TypedValue_WithBoolean_Deserializes_To_Correct_Type()
    {
        // Arrange
        var typedValue = TypedValue.Boolean(true);

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert — Value must be bool, NOT JsonElement
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<BooleanType>();
        deserialized.Value.Should().BeOfType<bool>();
        deserialized.Value.Should().Be(true);
    }

    [Fact]
    public void TypedValue_WithString_Deserializes_To_Correct_Type()
    {
        // Arrange
        var typedValue = TypedValue.Text("test value");

        // Act
        var json = JsonSerializer.Serialize(typedValue, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TypedValue>(json, JsonOptions);

        // Assert — Value must be string, NOT JsonElement
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().BeOfType<StringType>();
        deserialized.Value.Should().BeOfType<string>();
        deserialized.Value.Should().Be("test value");
    }

    /// <summary>
    ///     Reproduces the exact TargetTag scenario: a Skill with a PositionTag property
    ///     stored in PostgreSQL JSONB must have its Value correctly deserialized as PositionTag,
    ///     not as JsonElement. This failed before the TypedValueJsonConverter was added.
    /// </summary>
    [Fact]
    public void Skill_TargetTag_Property_Value_Deserializes_As_PositionTag_Not_JsonElement()
    {
        // Arrange — matches skills-config.json "Move Object To Tag" skill
        var positionTag = new PositionTag
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            Tag = "DefaultTag",
            Position = new Position { X = 0, Y = 0, Z = 0, Alpha = 0, Beta = 0, Gamma = 0 }
        };
        var skill = new Skill
        {
            Id = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc"),
            Name = "Move Object To Tag",
            Description = "Move an object to a predefined location tag",
            Properties =
            [
                new TypedProperty
                {
                    Name = "TargetTag",
                    Value = TypedValue.PositionTag(positionTag),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        // Act — simulate PostgreSQL JSONB round-trip
        var json = JsonSerializer.Serialize(skill, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Skill>(json, JsonOptions);

        // Assert — this is the exact assertion that would have FAILED before the fix
        deserialized.Should().NotBeNull();
        var targetTagProp = deserialized!.Properties.First(p => p.Name == "TargetTag");
        targetTagProp.Value.Type.Should().BeOfType<PositionTagType>();

        // CRITICAL: Value must be PositionTag, not JsonElement
        targetTagProp.Value.Value.Should().NotBeNull();
        targetTagProp.Value.Value.Should().BeOfType<PositionTag>(
            "TypedValue.Value must deserialize as PositionTag, not as JsonElement. " +
            "Without the TypedValueJsonConverter, object? deserializes as JsonElement.");

        var tag = (PositionTag)targetTagProp.Value.Value!;
        tag.Id.Should().Be(Guid.Parse("a0000000-0000-0000-0000-000000000001"));
        tag.Tag.Should().Be("DefaultTag");
    }
}