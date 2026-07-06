using FHOOE.Freydis.Domain.Entities.Common;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Domain.Common;

public class VariableBindingTests
{
    [Fact]
    public void VariableBinding_CanBeCreated_WithRequiredFields()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "target_position",
            Mode = BindingMode.Read
        };

        // Assert
        binding.VariableName.Should().Be("target_position");
        binding.Mode.Should().Be(BindingMode.Read);
        binding.TransformExpression.Should().BeNull();
    }

    [Fact]
    public void VariableBinding_CanBeCreated_WithTransformExpression()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "distance",
            Mode = BindingMode.Read,
            TransformExpression = "value / 1000"
        };

        // Assert
        binding.VariableName.Should().Be("distance");
        binding.Mode.Should().Be(BindingMode.Read);
        binding.TransformExpression.Should().Be("value / 1000");
    }

    [Fact]
    public void VariableBinding_CanBeCreated_WithReadMode()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "input_var",
            Mode = BindingMode.Read
        };

        // Assert
        binding.Mode.Should().Be(BindingMode.Read);
    }

    [Fact]
    public void VariableBinding_CanBeCreated_WithWriteMode()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "output_var",
            Mode = BindingMode.Write
        };

        // Assert
        binding.Mode.Should().Be(BindingMode.Write);
    }

    [Fact]
    public void VariableBinding_CanBeCreated_WithReadWriteMode()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "io_var",
            Mode = BindingMode.ReadWrite
        };

        // Assert
        binding.Mode.Should().Be(BindingMode.ReadWrite);
    }

    [Fact]
    public void VariableBinding_TransformExpression_CanBeNull()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read,
            TransformExpression = null
        };

        // Assert
        binding.TransformExpression.Should().BeNull();
    }

    [Fact]
    public void VariableBinding_TransformExpression_CanBeEmpty()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "var2",
            Mode = BindingMode.Read,
            TransformExpression = ""
        };

        // Assert
        binding.TransformExpression.Should().BeEmpty();
    }

    [Fact]
    public void VariableBinding_TransformExpression_CanBeComplexExpression()
    {
        // Arrange & Act
        var binding = new VariableBinding
        {
            VariableName = "temperature",
            Mode = BindingMode.Read,
            TransformExpression = "(value - 32) * 5 / 9"
        };

        // Assert
        binding.TransformExpression.Should().Be("(value - 32) * 5 / 9");
    }

    [Fact]
    public void VariableBinding_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var binding1 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read,
            TransformExpression = "value * 2"
        };

        var binding2 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read,
            TransformExpression = "value * 2"
        };

        // Act & Assert
        binding1.Should().Be(binding2);
        (binding1 == binding2).Should().BeTrue();
    }

    [Fact]
    public void VariableBinding_RecordEquality_DifferentVariableName_AreNotEqual()
    {
        // Arrange
        var binding1 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read
        };

        var binding2 = new VariableBinding
        {
            VariableName = "var2",
            Mode = BindingMode.Read
        };

        // Act & Assert
        binding1.Should().NotBe(binding2);
    }

    [Fact]
    public void VariableBinding_RecordEquality_DifferentMode_AreNotEqual()
    {
        // Arrange
        var binding1 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read
        };

        var binding2 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Write
        };

        // Act & Assert
        binding1.Should().NotBe(binding2);
    }

    [Fact]
    public void VariableBinding_RecordEquality_DifferentTransformExpression_AreNotEqual()
    {
        // Arrange
        var binding1 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read,
            TransformExpression = "value * 2"
        };

        var binding2 = new VariableBinding
        {
            VariableName = "var1",
            Mode = BindingMode.Read,
            TransformExpression = "value * 3"
        };

        // Act & Assert
        binding1.Should().NotBe(binding2);
    }
}