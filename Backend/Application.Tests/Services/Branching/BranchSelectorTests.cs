using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Branching;

public class BranchSelectorTests
{
    private readonly Mock<IExpressionEvaluator> _mockEvaluator;
    private readonly Mock<ILogger<BranchSelector>> _mockLogger;
    private readonly BranchSelector _sut;

    public BranchSelectorTests()
    {
        _mockEvaluator = new Mock<IExpressionEvaluator>();
        _mockLogger = new Mock<ILogger<BranchSelector>>();
        _sut = new BranchSelector(_mockEvaluator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SelectBranchAsync_SimpleVariableSelector_MatchesExactValue()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("mode", "fast", "System");

        var fastBranchId = Guid.NewGuid();
        var slowBranchId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router1",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "mode" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "FastBranch", Condition = "mode == \"fast\"", Priority = 1, TargetNodeId = fastBranchId
                    },
                    new()
                    {
                        Name = "SlowBranch", Condition = "mode == \"slow\"", Priority = 2, TargetNodeId = slowBranchId
                    }
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("mode == \"fast\"", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);
        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("mode == \"slow\"", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("FastBranch");
    }

    [Fact]
    public async Task SelectBranchAsync_ConditionalSelector_EvaluatesBoolean()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("temperature", 75.0, "System");

        var hotBranchId = Guid.NewGuid();
        var moderateBranchId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "TempRouter",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "temperature" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Hot", Condition = "temperature > 80", Priority = 1, TargetNodeId = hotBranchId },
                    new()
                    {
                        Name = "Moderate", Condition = "temperature > 60", Priority = 2, TargetNodeId = moderateBranchId
                    }
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("temperature > 80", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);
        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("temperature > 60", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("Moderate");
    }

    [Fact]
    public async Task SelectBranchAsync_ComplexSelector_EvaluatesComplexExpression()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("speed", 100.0, "System");
        context.SetValue("distance", 500.0, "System");

        var longDistanceId = Guid.NewGuid();
        var mediumDistanceId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "ComplexRouter",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "distance && speed" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "LongDistance", Condition = "distance > 1000 && speed > 50", Priority = 1,
                        TargetNodeId = longDistanceId
                    },
                    new()
                    {
                        Name = "MediumDistance", Condition = "distance > 100 && speed > 50", Priority = 2,
                        TargetNodeId = mediumDistanceId
                    }
                }
            }
        };

        _mockEvaluator.Setup(m =>
                m.EvaluateBooleanAsync("distance > 1000 && speed > 50", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);
        _mockEvaluator.Setup(m =>
                m.EvaluateBooleanAsync("distance > 100 && speed > 50", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("MediumDistance");
    }

    [Fact]
    public async Task SelectBranchAsync_ReturnsDefaultBranch_WhenNoMatch()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("count", 5, "System");

        var highCountId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "RouterWithDefault",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "count" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "HighCount", Condition = "count > 10", Priority = 1, TargetNodeId = highCountId },
                    new()
                    {
                        Name = "Default", Condition = null, Priority = 100, TargetNodeId = defaultId
                    } // Default branch
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("count > 10", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("Default");
    }

    [Fact]
    public async Task SelectBranchAsync_ThrowsNoBranchMatchException_WhenNoDefault()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("count", 5, "System");

        var highCountId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "RouterNoDefault",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "count" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "HighCount", Condition = "count > 10", Priority = 1, TargetNodeId = highCountId }
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("count > 10", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.SelectBranchAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<NoBranchMatchException>()
            .WithMessage("No branch matched in router 'RouterNoDefault' and no default branch exists.");
    }

    [Fact]
    public async Task SelectBranchAsync_ThrowsAmbiguousBranchException_OnMultipleMatches()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("value", 50, "System");

        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "AmbiguousRouter",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "value" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", Condition = "value > 30", Priority = 1, TargetNodeId = branch1Id },
                    new()
                    {
                        Name = "Branch2", Condition = "value < 100", Priority = 1, TargetNodeId = branch2Id
                    } // Same priority
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("value > 30", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);
        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("value < 100", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.SelectBranchAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<AmbiguousBranchException>()
            .Where(ex => ex.MatchingBranches.Contains("Branch1") && ex.MatchingBranches.Contains("Branch2"));
    }

    [Fact]
    public async Task SelectBranchAsync_EvaluatesByPriorityOrder()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("value", 50, "System");

        var lowPriorityId = Guid.NewGuid();
        var highPriorityId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "PriorityRouter",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "value" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "LowPriority", Condition = "value > 10", Priority = 10, TargetNodeId = lowPriorityId
                    },
                    new()
                    {
                        Name = "HighPriority", Condition = "value > 10", Priority = 1, TargetNodeId = highPriorityId
                    }
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("value > 10", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(true);

        // Act - Should evaluate high priority first and stop
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("HighPriority");
    }

    [Fact]
    public async Task SelectBranchAsync_DefaultBranchHasLowestPriority()
    {
        // Arrange
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = Guid.NewGuid()
        };
        context.SetValue("value", 5, "System");

        var regularId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();

        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "DefaultPriorityRouter",
                StartTime = 0,
                Duration = 1,
                Selector = new ExpressionSelector { Expression = "value" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Regular", Condition = "value > 10", Priority = 1, TargetNodeId = regularId },
                    new()
                    {
                        Name = "Default", Condition = null, Priority = 100, TargetNodeId = defaultId
                    } // Evaluated last
                }
            }
        };

        _mockEvaluator.Setup(m => m.EvaluateBooleanAsync("value > 10", It.IsAny<Dictionary<string, object?>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SelectBranchAsync(router, context);

        // Assert
        result.Name.Should().Be("Default");
    }
}