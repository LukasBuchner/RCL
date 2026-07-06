using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Domain.Procedure;

/// <summary>
///     Tests for the polymorphic SelectorExpression hierarchy.
/// </summary>
public class SelectorExpressionTests
{
    [Fact]
    public void SimpleVariableSelector_ShouldCreateWithVariableName()
    {
        // Arrange & Act
        var selector = new SimpleVariableSelector
        {
            Expression = "quality_result"
        };

        // Assert
        selector.Expression.Should().Be("quality_result");
        selector.VariableName.Should().Be("quality_result");
    }

    [Fact]
    public void SimpleVariableSelector_VariableName_ShouldMatchExpression()
    {
        // Arrange
        var selector = new SimpleVariableSelector
        {
            Expression = "temperature"
        };

        // Act & Assert
        selector.VariableName.Should().Be(selector.Expression);
    }

    [Fact]
    public void ConditionalSelector_ShouldCreateWithBooleanExpression()
    {
        // Arrange & Act
        var selector = new ExpressionSelector
        {
            Expression = "temperature > 100 && pressure < 50"
        };

        // Assert
        selector.Expression.Should().Be("temperature > 100 && pressure < 50");
    }

    [Fact]
    public void ConditionalSelector_ShouldStoreComplexCondition()
    {
        // Arrange & Act
        var selector = new ExpressionSelector
        {
            Expression = "quality == 'OK' || retry_count < 3"
        };

        // Assert
        selector.Expression.Should().Be("quality == 'OK' || retry_count < 3");
    }

    [Fact]
    public void ComplexSelector_ShouldCreateWithComplexExpression()
    {
        // Arrange & Act
        var selector = new ExpressionSelector
        {
            Expression = "Math.Max(temp1, temp2) > threshold"
        };

        // Assert
        selector.Expression.Should().Be("Math.Max(temp1, temp2) > threshold");
    }

    [Fact]
    public void ComplexSelector_ShouldStoreMultiVariableLogic()
    {
        // Arrange & Act
        var selector = new ExpressionSelector
        {
            Expression = "(weight * 2.2) + offset"
        };

        // Assert
        selector.Expression.Should().Be("(weight * 2.2) + offset");
    }

    [Fact]
    public void SimpleVariableSelector_ShouldHaveRecordEquality()
    {
        // Arrange
        var selector1 = new SimpleVariableSelector { Expression = "quality_result" };
        var selector2 = new SimpleVariableSelector { Expression = "quality_result" };
        var selector3 = new SimpleVariableSelector { Expression = "different_var" };

        // Act & Assert
        selector1.Should().Be(selector2);
        selector1.Should().NotBe(selector3);
    }

    [Fact]
    public void ConditionalSelector_ShouldHaveRecordEquality()
    {
        // Arrange
        var selector1 = new ExpressionSelector { Expression = "temp > 100" };
        var selector2 = new ExpressionSelector { Expression = "temp > 100" };
        var selector3 = new ExpressionSelector { Expression = "temp > 200" };

        // Act & Assert
        selector1.Should().Be(selector2);
        selector1.Should().NotBe(selector3);
    }

    [Fact]
    public void ComplexSelector_ShouldHaveRecordEquality()
    {
        // Arrange
        var selector1 = new ExpressionSelector { Expression = "Math.Max(a, b)" };
        var selector2 = new ExpressionSelector { Expression = "Math.Max(a, b)" };
        var selector3 = new ExpressionSelector { Expression = "Math.Min(a, b)" };

        // Act & Assert
        selector1.Should().Be(selector2);
        selector1.Should().NotBe(selector3);
    }

    [Fact]
    public void SelectorExpression_ShouldSupportPolymorphism_SimpleVariableSelector()
    {
        // Arrange & Act
        SelectorExpression selector = new SimpleVariableSelector
        {
            Expression = "quality_result"
        };

        // Assert
        selector.Should().BeOfType<SimpleVariableSelector>();
        selector.Expression.Should().Be("quality_result");
    }

    [Fact]
    public void SelectorExpression_ShouldSupportPolymorphism_ConditionalSelector()
    {
        // Arrange & Act
        SelectorExpression selector = new ExpressionSelector
        {
            Expression = "temp > 100"
        };

        // Assert
        selector.Should().BeOfType<ExpressionSelector>();
        selector.Expression.Should().Be("temp > 100");
    }

    [Fact]
    public void SelectorExpression_ShouldSupportPolymorphism_ComplexSelector()
    {
        // Arrange & Act
        SelectorExpression selector = new ExpressionSelector
        {
            Expression = "Math.Max(a, b)"
        };

        // Assert
        selector.Should().BeOfType<ExpressionSelector>();
        selector.Expression.Should().Be("Math.Max(a, b)");
    }

    [Fact]
    public void SelectorExpression_PolymorphicList_ShouldHoldAllTypes()
    {
        // Arrange & Act
        var selectors = new List<SelectorExpression>
        {
            new SimpleVariableSelector { Expression = "var1" },
            new ExpressionSelector { Expression = "var2 > 10" },
            new ExpressionSelector { Expression = "Math.Abs(var3)" }
        };

        // Assert
        selectors.Should().HaveCount(3);
        selectors[0].Should().BeOfType<SimpleVariableSelector>();
        selectors[1].Should().BeOfType<ExpressionSelector>();
        selectors[2].Should().BeOfType<ExpressionSelector>();
    }

    [Fact]
    public void DifferentSelectorTypes_WithSameExpression_ShouldNotBeEqual()
    {
        // Arrange
        var simple = new SimpleVariableSelector { Expression = "test" };
        var expression = new ExpressionSelector { Expression = "test" };

        // Act & Assert - SimpleVariableSelector and ExpressionSelector are different types
        simple.Should().NotBe(expression);
    }

    [Fact]
    public void SimpleVariableSelector_WithEmptyExpression_ShouldCreate()
    {
        // Arrange & Act
        var selector = new SimpleVariableSelector { Expression = "" };

        // Assert
        selector.Expression.Should().BeEmpty();
        selector.VariableName.Should().BeEmpty();
    }

    [Fact]
    public void ConditionalSelector_WithMultilineExpression_ShouldStore()
    {
        // Arrange
        var expression = """
                         temperature > 100 &&
                         pressure < 50 &&
                         quality == 'OK'
                         """;

        // Act
        var selector = new ExpressionSelector { Expression = expression };

        // Assert
        selector.Expression.Should().Be(expression);
    }
}