using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Variables;

public class VariableResolverTests
{
    private readonly VariableResolver _sut;

    public VariableResolverTests()
    {
        var mockLogger = new Mock<ILogger<VariableResolver>>();

        _sut = new VariableResolver(mockLogger.Object);
    }

    [Fact]
    public async Task InitializeContextAsync_CreatesContextWithDefaultValues()
    {
        // Arrange
        var procedureExecutionId = Guid.NewGuid();
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Id = Guid.NewGuid(),
            Name = "TestProcedure",
            RootNodeIds = new List<Guid>(),
            Variables = new List<VariableDefinition>
            {
                new() { Name = "speed", Type = new NumberType(), DefaultValue = 100.0 },
                new() { Name = "enabled", Type = new BooleanType(), DefaultValue = true }
            }
        };

        // Act
        var context = await _sut.InitializeContextAsync(procedureExecutionId, procedure);

        // Assert
        context.Should().NotBeNull();
        context.ProcedureExecutionId.Should().Be(procedureExecutionId);
        context.TryGetValue("speed", out var speedValue).Should().BeTrue();
        speedValue.Should().Be(100.0);
        context.TryGetValue("enabled", out var enabledValue).Should().BeTrue();
        enabledValue.Should().Be(true);
    }

    [Fact]
    public async Task InitializeContextAsync_MergesUserProvidedValues()
    {
        // Arrange
        var procedureExecutionId = Guid.NewGuid();
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Id = Guid.NewGuid(),
            Name = "TestProcedure",
            RootNodeIds = new List<Guid>(),
            Variables = new List<VariableDefinition>
            {
                new() { Name = "speed", Type = new NumberType(), DefaultValue = 100.0 },
                new() { Name = "enabled", Type = new BooleanType(), DefaultValue = true }
            }
        };
        var userValues = new Dictionary<string, object>
        {
            ["speed"] = 200.0
        };

        // Act
        var context = await _sut.InitializeContextAsync(procedureExecutionId, procedure, userValues);

        // Assert
        context.TryGetValue("speed", out var speedValue).Should().BeTrue();
        speedValue.Should().Be(200.0); // User value overrides default
        context.TryGetValue("enabled", out var enabledValue).Should().BeTrue();
        enabledValue.Should().Be(true); // Default value used
    }

    [Fact]
    public async Task InitializeContextAsync_ValidatesVariableDefinitionsExist()
    {
        // Arrange
        var procedureExecutionId = Guid.NewGuid();
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Id = Guid.NewGuid(),
            Name = "TestProcedure",
            RootNodeIds = new List<Guid>(),
            Variables = new List<VariableDefinition>()
        };

        // Act
        var context = await _sut.InitializeContextAsync(procedureExecutionId, procedure);

        // Assert
        context.Should().NotBeNull();
        context.GetAllValues().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveValueAsync_ReturnsTypedValue()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("count", 42, "System");

        // Act
        var result = await VariableResolver.ResolveValueAsync<int>(context, "count");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task ResolveValueAsync_ThrowsVariableNotFoundException_WhenMissing()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };

        // Act
        var act = async () => await VariableResolver.ResolveValueAsync<int>(context, "missing");

        // Assert
        await act.Should().ThrowAsync<VariableNotFoundException>()
            .WithMessage("Variable 'missing' not found in context.");
    }

    [Fact]
    public async Task UpdateValueAsync_UpdatesExistingValue()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("count", 10, "System");

        // Act
        await _sut.UpdateValueAsync(context, "count", 20, "User");

        // Assert
        context.TryGetValue("count", out var value).Should().BeTrue();
        value.Should().Be(20);
    }

    [Fact]
    public async Task UpdateValueAsync_UpdatesLastUpdatedUtc()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        var initialTime = context.LastUpdatedUtc;
        await Task.Delay(10); // Ensure time difference

        // Act
        await _sut.UpdateValueAsync(context, "count", 20, "User");

        // Assert
        context.LastUpdatedUtc.Should().BeAfter(initialTime);
    }

    [Fact]
    public async Task ResolveBindingAsync_ReadsFromVariable()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("speed", 150.0, "System");

        var binding = new VariableBinding
        {
            VariableName = "speed",
            Mode = BindingMode.Read
        };

        // Act
        var result = await _sut.ResolveBindingAsync(context, binding);

        // Assert
        result.Should().Be(150.0);
    }

    [Fact]
    public async Task ResolveBindingAsync_AppliesTransformExpression()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("speed", 100.0, "System");

        var binding = new VariableBinding
        {
            VariableName = "speed",
            Mode = BindingMode.Read,
            TransformExpression = "value * 2"
        };

        var mockEvaluator = new Mock<IExpressionEvaluator>();
        mockEvaluator.Setup(m => m.EvaluateAsync("value * 2", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(200.0);

        // Act
        var result = await _sut.ResolveBindingAsync(context, binding, mockEvaluator.Object);

        // Assert
        result.Should().Be(200.0);
        mockEvaluator.Verify(m => m.EvaluateAsync(
            "value * 2",
            It.Is<Dictionary<string, object?>>(d => d.ContainsKey("value") && d["value"]!.Equals(100.0))), Times.Once);
    }

    [Fact]
    public async Task TypeConversion_WorksForCompatibleTypes()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("count", 42, "System"); // int

        // Act
        var result = await VariableResolver.ResolveValueAsync<double>(context, "count"); // Convert to double

        // Assert
        result.Should().Be(42.0);
    }

    [Fact]
    public async Task InvalidTypeConversion_ThrowsVariableTypeMismatchException()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("message", "hello", "System"); // string

        // Act
        var act = async () =>
            await VariableResolver.ResolveValueAsync<int>(context, "message"); // Try to convert to int

        // Assert
        await act.Should().ThrowAsync<VariableTypeMismatchException>()
            .Where(ex => ex.VariableName == "message" &&
                         ex.ExpectedType == typeof(int) &&
                         ex.ActualType == typeof(string));
    }
}