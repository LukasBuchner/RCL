using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Tests.Domain.Variables;

/// <summary>
///     Tests for VariableValue record.
/// </summary>
public class VariableValueTests
{
    [Fact]
    public void Create_Should_SetAllRequiredProperties_When_ProvidedValidValues()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var variableValue = new VariableValue
        {
            Name = "testVar",
            Value = 42,
            LastUpdatedUtc = now
        };

        // Assert
        Assert.Equal("testVar", variableValue.Name);
        Assert.Equal(42, variableValue.Value);
        Assert.Equal(now, variableValue.LastUpdatedUtc);
    }

    [Fact]
    public void Create_Should_AllowNullForLastUpdatedBy_When_NotProvided()
    {
        // Arrange & Act
        var variableValue = new VariableValue
        {
            Name = "testVar",
            Value = "test",
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Assert
        Assert.Null(variableValue.LastUpdatedBy);
    }

    [Fact]
    public void Create_Should_SetLastUpdatedBy_When_Provided()
    {
        // Arrange & Act
        var variableValue = new VariableValue
        {
            Name = "counter",
            Value = 100,
            LastUpdatedUtc = DateTime.UtcNow,
            LastUpdatedBy = "SkillA"
        };

        // Assert
        Assert.Equal("SkillA", variableValue.LastUpdatedBy);
    }

    [Fact]
    public void Value_Should_AcceptAnyObjectType_When_Set()
    {
        // Arrange & Act
        var intValue = new VariableValue
        {
            Name = "int",
            Value = 42,
            LastUpdatedUtc = DateTime.UtcNow
        };

        var stringValue = new VariableValue
        {
            Name = "string",
            Value = "hello",
            LastUpdatedUtc = DateTime.UtcNow
        };

        var boolValue = new VariableValue
        {
            Name = "bool",
            Value = true,
            LastUpdatedUtc = DateTime.UtcNow
        };

        var objectValue = new VariableValue
        {
            Name = "object",
            Value = new { X = 1, Y = 2 },
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Assert
        Assert.IsType<int>(intValue.Value);
        Assert.IsType<string>(stringValue.Value);
        Assert.IsType<bool>(boolValue.Value);
        Assert.NotNull(objectValue.Value);
    }

    [Fact]
    public void RecordEquality_Should_WorkCorrectly_When_ComparingInstances()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var value1 = new VariableValue
        {
            Name = "var1",
            Value = 42,
            LastUpdatedUtc = now
        };

        var value2 = new VariableValue
        {
            Name = "var1",
            Value = 42,
            LastUpdatedUtc = now
        };

        var value3 = new VariableValue
        {
            Name = "var1",
            Value = 43,
            LastUpdatedUtc = now
        };

        // Act & Assert
        Assert.Equal(value1, value2);
        Assert.NotEqual(value1, value3);
    }

    [Fact]
    public void LastUpdatedUtc_Should_BeSetCorrectly_When_Created()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var variableValue = new VariableValue
        {
            Name = "test",
            Value = "value",
            LastUpdatedUtc = DateTime.UtcNow
        };

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(variableValue.LastUpdatedUtc >= beforeCreation);
        Assert.True(variableValue.LastUpdatedUtc <= afterCreation);
    }
}