using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.OutputTypes;
using Microsoft.Extensions.Logging.Abstractions;
using BooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using EnumType = FHOOE.Freydis.Domain.Entities.Common.EnumType;
using StringType = FHOOE.Freydis.Domain.Entities.Common.StringType;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Mappers;

public class TypedValueMapperTests
{
    private readonly ITypedValueMapper _mapper = new TypedValueMapper(NullLogger<TypedValueMapper>.Instance);

    [Fact]
    public void MapToPropertyValue_WithBooleanType_ReturnsBooleanValue()
    {
        // Arrange
        var typedValue = TypedValue.Boolean(true);

        // Act
        var result = _mapper.MapToPropertyValue(typedValue);

        // Assert
        var boolValue = Assert.IsType<BooleanValue>(result);
        Assert.True(boolValue.BoolValue);
        Assert.IsType<BooleanType>(boolValue.Type);
    }

    [Fact]
    public void MapToPropertyValue_WithNumberType_ReturnsNumberValue()
    {
        // Arrange
        var typedValue = TypedValue.Number(42.5);

        // Act
        var result = _mapper.MapToPropertyValue(typedValue);

        // Assert
        var numValue = Assert.IsType<NumberValue>(result);
        Assert.Equal(42.5, numValue.Value);
        Assert.IsType<NumberType>(numValue.Type);
    }

    [Fact]
    public void MapToPropertyValue_WithStringType_ReturnsStringValue()
    {
        // Arrange
        var typedValue = TypedValue.Text("test");

        // Act
        var result = _mapper.MapToPropertyValue(typedValue);

        // Assert
        var strValue = Assert.IsType<StringValue>(result);
        Assert.Equal("test", strValue.Value);
        Assert.IsType<StringType>(strValue.Type);
    }

    [Fact]
    public void MapToPropertyValue_WithPositionType_ReturnsPositionValue()
    {
        // Arrange
        var position = new Position
        {
            X = 1.0,
            Y = 2.0,
            Z = 3.0,
            Alpha = 0.1,
            Beta = 0.2,
            Gamma = 0.3
        };
        var typedValue = TypedValue.Position(position);

        // Act
        var result = _mapper.MapToPropertyValue(typedValue);

        // Assert
        var posValue = Assert.IsType<PositionValue>(result);
        Assert.Equal(position, posValue.Value);
        Assert.IsType<PositionType>(posValue.Type);
    }

    [Fact]
    public void MapToPropertyValue_WithNullValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var typedValue = new TypedValue
        {
            Type = new BooleanType(),
            Value = null
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _mapper.MapToPropertyValue(typedValue));
    }

    [Fact]
    public void MapToPropertyValue_WithUnsupportedType_ThrowsInvalidOperationException()
    {
        // Arrange
        var unsupportedType = new EnumType { AllowedValues = ["A", "B"] };
        var typedValue = new TypedValue
        {
            Type = unsupportedType,
            Value = "A"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _mapper.MapToPropertyValue(typedValue));
        Assert.Contains("EnumType", ex.Message);
    }
}