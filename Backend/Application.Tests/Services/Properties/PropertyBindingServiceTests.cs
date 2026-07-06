using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Properties;

public class PropertyBindingServiceTests
{
    private readonly Mock<IExpressionEvaluator> _expressionEvaluatorMock;
    private readonly PropertyBindingService _sut;
    private readonly Mock<IVariableResolver> _variableResolverMock;

    public PropertyBindingServiceTests()
    {
        var loggerMock = new Mock<ILogger<PropertyBindingService>>();

        _variableResolverMock = new Mock<IVariableResolver>();
        _expressionEvaluatorMock = new Mock<IExpressionEvaluator>();
        _sut = new PropertyBindingService(
            _variableResolverMock.Object,
            _expressionEvaluatorMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task ResolveInputBindingsAsync_ReadsAllInputPropertiesFromVariables()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Read
                    }
                },
                new TypedProperty
                {
                    Name = "input2",
                    Value = new TypedValue { Type = new StringType(), Value = "" },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "var2",
                        Mode = BindingMode.Read
                    }
                }
            ]
        };

        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                It.Is<VariableBinding>(b => b.VariableName == "var1"),
                _expressionEvaluatorMock.Object))
            .ReturnsAsync(42);

        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                It.Is<VariableBinding>(b => b.VariableName == "var2"),
                _expressionEvaluatorMock.Object))
            .ReturnsAsync("hello");

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result.Should().HaveCount(2);
        result["input1"].Should().Be(42);
        result["input2"].Should().Be("hello");
    }

    [Fact]
    public async Task ResolveInputBindingsAsync_AppliesTransformExpressions()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Read,
                        TransformExpression = "value * 2"
                    }
                }
            ]
        };

        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                skill.Properties[0].Binding!,
                _expressionEvaluatorMock.Object))
            .ReturnsAsync(84); // Already transformed by VariableResolver

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result["input1"].Should().Be(84);
    }

    [Fact]
    public async Task ResolveInputBindingsAsync_HandlesPropertiesWithoutBindings()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 42 },
                    Direction = PropertyDirection.Input,
                    Binding = null // No binding
                },
                new TypedProperty
                {
                    Name = "input2",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "var2",
                        Mode = BindingMode.Read
                    }
                }
            ]
        };

        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                skill.Properties[1].Binding!,
                _expressionEvaluatorMock.Object))
            .ReturnsAsync(100);

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("input2");
        result.Should().NotContainKey("input1");
    }

    [Fact]
    public async Task ResolveInputBindingsAsync_ThrowsOnMissingVariable()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "nonexistent",
                        Mode = BindingMode.Read
                    }
                }
            ]
        };

        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                skill.Properties[0].Binding!,
                _expressionEvaluatorMock.Object))
            .ThrowsAsync(new KeyNotFoundException("Variable not found"));

        // Act & Assert
        await _sut.Invoking(s => s.ResolveInputBindingsAsync(skill, context))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ResolveInputBindingsAsync_HandlesTypeConversion()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Read
                    }
                }
            ]
        };

        // Variable returns int, should be accepted
        _variableResolverMock.Setup(r => r.ResolveBindingAsync(
                context,
                skill.Properties[0].Binding!,
                _expressionEvaluatorMock.Object))
            .ReturnsAsync(42);

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result["input1"].Should().Be(42);
    }

    [Fact]
    public async Task ApplyOutputBindingsAsync_WritesAllOutputPropertiesToVariables()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "output1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Write
                    }
                },
                new TypedProperty
                {
                    Name = "output2",
                    Value = new TypedValue { Type = new StringType(), Value = "" },
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "var2",
                        Mode = BindingMode.Write
                    }
                }
            ]
        };

        var skillOutputs = new Dictionary<string, object>
        {
            ["output1"] = 99,
            ["output2"] = "world"
        };

        // Act
        await _sut.ApplyOutputBindingsAsync(skill, skillOutputs, context);

        // Assert
        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var1",
            99,
            It.IsAny<string>()), Times.Once);

        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var2",
            "world",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutputBindingsAsync_AppliesTransformExpressions()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "output1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Write,
                        TransformExpression = "value * 2"
                    }
                }
            ]
        };

        var skillOutputs = new Dictionary<string, object>
        {
            ["output1"] = 50
        };

        _expressionEvaluatorMock.Setup(e => e.EvaluateAsync(
                "value * 2",
                It.Is<Dictionary<string, object?>>(d => (int)d["value"]! == 50)))
            .ReturnsAsync(100);

        // Act
        await _sut.ApplyOutputBindingsAsync(skill, skillOutputs, context);

        // Assert
        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var1",
            100,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutputBindingsAsync_HandlesPropertiesWithoutBindings()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "output1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Output,
                    Binding = null // No binding
                },
                new TypedProperty
                {
                    Name = "output2",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "var2",
                        Mode = BindingMode.Write
                    }
                }
            ]
        };

        var skillOutputs = new Dictionary<string, object>
        {
            ["output1"] = 42,
            ["output2"] = 99
        };

        // Act
        await _sut.ApplyOutputBindingsAsync(skill, skillOutputs, context);

        // Assert
        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var2",
            99,
            It.IsAny<string>()), Times.Once);

        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            It.Is<string>(s => s != "var2"),
            It.IsAny<object>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApplyOutputBindingsAsync_PersistsToContext()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "output1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.Write
                    }
                }
            ]
        };

        var skillOutputs = new Dictionary<string, object>
        {
            ["output1"] = 42
        };

        // Act
        await _sut.ApplyOutputBindingsAsync(skill, skillOutputs, context);

        // Assert - UpdateValueAsync persists to database
        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var1",
            42,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOutputBindingsAsync_HandlesInputOutputProperties()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "inout1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.InputOutput,
                    Binding = new VariableBinding
                    {
                        VariableName = "var1",
                        Mode = BindingMode.ReadWrite
                    }
                }
            ]
        };

        var skillOutputs = new Dictionary<string, object>
        {
            ["inout1"] = 42
        };

        // Act
        await _sut.ApplyOutputBindingsAsync(skill, skillOutputs, context);

        // Assert - InputOutput typedProperty should write back
        _variableResolverMock.Verify(r => r.UpdateValueAsync(
            context,
            "var1",
            42,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EmptySkillPropertiesReturnsEmptyDictionary()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties = []
        };

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NullBindingOnPropertyIsSkipped()
    {
        // Arrange
        var context = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties =
            [
                new TypedProperty
                {
                    Name = "input1",
                    Value = new TypedValue { Type = new NumberType(), Value = 0 },
                    Direction = PropertyDirection.Input,
                    Binding = null
                }
            ]
        };

        // Act
        var result = await _sut.ResolveInputBindingsAsync(skill, context);

        // Assert
        result.Should().BeEmpty();
        _variableResolverMock.Verify(r => r.ResolveBindingAsync(
            It.IsAny<VariableContext>(),
            It.IsAny<VariableBinding>(),
            It.IsAny<IExpressionEvaluator>()), Times.Never);
    }
}