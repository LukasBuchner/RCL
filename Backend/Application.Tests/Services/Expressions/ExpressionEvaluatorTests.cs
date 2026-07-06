using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Expressions;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _sut;

    public ExpressionEvaluatorTests()
    {
        _sut = new ExpressionEvaluator();
    }

    [Fact]
    public async Task EvaluateAsync_SimpleVariableReference()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["x"] = 42
        };

        // Act
        var result = await _sut.EvaluateAsync("x", context);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task EvaluateAsync_ArithmeticExpression()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["a"] = 10,
            ["b"] = 5
        };

        // Act
        var result = await _sut.EvaluateAsync("a + b * 2", context);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public async Task EvaluateAsync_BooleanExpression()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["x"] = 10,
            ["y"] = 20
        };

        // Act
        var result = await _sut.EvaluateAsync("x < y", context);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleVariables()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["speed"] = 100.0,
            ["multiplier"] = 1.5,
            ["offset"] = 10.0
        };

        // Act
        var result = await _sut.EvaluateAsync("speed * multiplier + offset", context);

        // Assert
        result.Should().Be(160.0);
    }

    [Fact]
    public async Task EvaluateAsync_StringOperations()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        };

        // Act
        var result = await _sut.EvaluateAsync("firstName + \" \" + lastName", context);

        // Assert
        result.Should().Be("John Doe");
    }

    [Fact]
    public async Task EvaluateAsync_MathFunctions()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["x"] = -5.7
        };

        // Act
        var result = await _sut.EvaluateAsync("abs(x)", context);

        // Assert
        result.Should().Be(5.7);
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsOnSyntaxError()
    {
        // Arrange
        var context = new Dictionary<string, object?>();

        // Act
        var act = async () => await _sut.EvaluateAsync("invalid syntax +++", context);

        // Assert
        await act.Should().ThrowAsync<ExpressionEvaluationException>()
            .WithMessage("Failed to evaluate expression 'invalid syntax +++':*");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsOnRuntimeError()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["x"] = 10
        };

        // Act - Try to use undefined variable
        var act = async () => await _sut.EvaluateAsync("x + undefinedVar", context);

        // Assert
        await act.Should().ThrowAsync<ExpressionEvaluationException>();
    }

    [Fact]
    public async Task EvaluateBooleanAsync_ReturnsBooleanValue()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["count"] = 5
        };

        // Act
        var result = await _sut.EvaluateBooleanAsync("count > 3", context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateBooleanAsync_ThrowsWhenNotBoolean()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["x"] = 10
        };

        // Act
        var act = async () => await _sut.EvaluateBooleanAsync("x + 5", context);

        // Assert
        await act.Should().ThrowAsync<ExpressionEvaluationException>()
            .WithMessage("*did not return boolean value*");
    }

    [Fact]
    public void ValidateSyntax_ReturnsTrue_ForValidExpression()
    {
        // Act
        var isValid = _sut.ValidateSyntax("x + y * 2", out var errorMessage);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateSyntax_ReturnsFalse_ForInvalidExpression()
    {
        // Act - Use an expression with truly invalid syntax (incomplete expression)
        var isValid = _sut.ValidateSyntax("5 +", out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
    }
}