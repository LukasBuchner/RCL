using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Routing;

/// <summary>
///     Tests for RouterEvaluationService.
///     Validates router evaluation logic, branch selection, and error handling.
///     Router selections are kept in-memory only during execution to prevent timing contamination.
/// </summary>
public class RouterEvaluationServiceTests
{
    private readonly Mock<IBranchSelector> _mockBranchSelector;
    private readonly Mock<ILogger<RouterEvaluationService>> _mockLogger;
    private readonly RouterEvaluationService _service;

    public RouterEvaluationServiceTests()
    {
        _mockBranchSelector = new Mock<IBranchSelector>();
        _mockLogger = new Mock<ILogger<RouterEvaluationService>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _service = new RouterEvaluationService(
            _mockBranchSelector.Object,
            _mockLogger.Object);
    }

    #region EvaluateRouterAsync Tests

    [Fact]
    public async Task EvaluateRouterAsync_SimpleRouter_ReturnsSelectedBranchTargetId()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var router = CreateRouter("QualityCheck", "quality_result");
        var branch = CreateBranch("OK", "quality_result == 'OK'", targetNodeId);
        var context = new VariableContext();
        context.SetValue("quality_result", "OK");

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(branch);

        // Act
        var result = await _service.EvaluateRouterAsync(router, context);

        // Assert
        result.Should().Be(targetNodeId);
        _mockBranchSelector.Verify(x => x.SelectBranchAsync(router, context), Times.Once);
    }

    [Fact]
    public async Task EvaluateRouterAsync_RouterSelectsOKBranch_ReturnsOKTargetId()
    {
        // Arrange
        var okTargetId = Guid.NewGuid();
        var notOkTargetId = Guid.NewGuid();
        var router = CreateRouterWithBranches(
            "QualityCheck",
            "quality_result",
            [
                CreateBranch("OK", "quality_result == 'OK'", okTargetId),
                CreateBranch("NotOK", "quality_result == 'NotOK'", notOkTargetId, 1)
            ]);

        var context = new VariableContext();
        context.SetValue("quality_result", "OK");

        var okBranch = router.RouterTask.Branches!.First(b => b.Name == "OK");
        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(okBranch);

        // Act
        var result = await _service.EvaluateRouterAsync(router, context);

        // Assert
        result.Should().Be(okTargetId);
    }

    [Fact]
    public async Task EvaluateRouterAsync_RouterSelectsNotOKBranch_ReturnsNotOKTargetId()
    {
        // Arrange
        var okTargetId = Guid.NewGuid();
        var notOkTargetId = Guid.NewGuid();
        var router = CreateRouterWithBranches(
            "QualityCheck",
            "quality_result",
            [
                CreateBranch("OK", "quality_result == 'OK'", okTargetId),
                CreateBranch("NotOK", "quality_result == 'NotOK'", notOkTargetId, 1)
            ]);

        var context = new VariableContext();
        context.SetValue("quality_result", "NotOK");

        var notOkBranch = router.RouterTask.Branches!.First(b => b.Name == "NotOK");
        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(notOkBranch);

        // Act
        var result = await _service.EvaluateRouterAsync(router, context);

        // Assert
        result.Should().Be(notOkTargetId);
    }

    [Fact]
    public async Task EvaluateRouterAsync_RouterWithDefaultBranch_ReturnsDefaultWhenNoMatch()
    {
        // Arrange
        var okTargetId = Guid.NewGuid();
        var defaultTargetId = Guid.NewGuid();
        var router = CreateRouterWithBranches(
            "QualityCheck",
            "quality_result",
            [
                CreateBranch("OK", "quality_result == 'OK'", okTargetId),
                CreateBranch("Default", null, defaultTargetId, 99) // Default branch
            ]);

        var context = new VariableContext();
        context.SetValue("quality_result", "Unknown");

        var defaultBranch = router.RouterTask.Branches!.First(b => b.Name == "Default");
        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(defaultBranch);

        // Act
        var result = await _service.EvaluateRouterAsync(router, context);

        // Assert
        result.Should().Be(defaultTargetId);
    }

    [Fact]
    public async Task EvaluateRouterAsync_NestedRouter_EvaluatesCorrectly()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var router = CreateRouter("OuterRouter", "temperature");
        var branch = CreateBranch("High", "temperature > 100", targetNodeId);
        var context = new VariableContext();
        context.SetValue("temperature", 150);

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(branch);

        // Act
        var result = await _service.EvaluateRouterAsync(router, context);

        // Assert
        result.Should().Be(targetNodeId);
    }

    [Fact]
    public async Task EvaluateRouterAsync_MissingVariable_ThrowsVariableNotFoundException()
    {
        // Arrange
        var router = CreateRouter("QualityCheck", "quality_result");
        var context = new VariableContext(); // Empty - no variables

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new VariableNotFoundException("quality_result"));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<VariableNotFoundException>()
            .WithMessage("*quality_result*");
    }

    [Fact]
    public async Task EvaluateRouterAsync_AmbiguousBranches_ThrowsAmbiguousBranchException()
    {
        // Arrange
        var router = CreateRouterWithBranches(
            "AmbiguousRouter",
            "value",
            [
                CreateBranch("Branch1", "value > 10", Guid.NewGuid()),
                CreateBranch("Branch2", "value < 100", Guid.NewGuid()) // Same priority
            ]);

        var context = new VariableContext();
        context.SetValue("value", 50); // Matches both branches

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new AmbiguousBranchException(["Branch1", "Branch2"]));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<AmbiguousBranchException>();
    }

    [Fact]
    public async Task EvaluateRouterAsync_NoBranchMatch_ThrowsNoBranchMatchException()
    {
        // Arrange
        var router = CreateRouter("QualityCheck", "quality_result");
        var context = new VariableContext();
        context.SetValue("quality_result", "Unknown");

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new NoBranchMatchException("QualityCheck"));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<NoBranchMatchException>()
            .WithMessage("*QualityCheck*");
    }

    [Fact]
    public async Task EvaluateRouterAsync_LogsSelectedBranch()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var router = CreateRouter("QualityCheck", "quality_result");
        var branch = CreateBranch("OK", "quality_result == 'OK'", targetNodeId);
        var context = new VariableContext();
        context.SetValue("quality_result", "OK");

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(branch);

        // Act
        await _service.EvaluateRouterAsync(router, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("QualityCheck") && v.ToString()!.Contains("OK")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateRouterAsync_VariableContextNull_ThrowsArgumentNullException()
    {
        // Arrange
        var router = CreateRouter("QualityCheck", "quality_result");

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task EvaluateRouterAsync_NullSelector_ThrowsInvalidOperationException()
    {
        // Arrange - RouterNode without properly configured selector (should be validated by BranchSelector)
        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "InvalidRouter",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>()
            }
        };
        var context = new VariableContext();

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new InvalidOperationException("Router is not a valid router"));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a valid router*");
    }

    [Fact]
    public async Task EvaluateRouterAsync_NullBranches_ThrowsInvalidOperationException()
    {
        // Arrange - This test is no longer valid since RouterNode always has RouterTask with Branches
        // The validation happens in BranchSelector when branches list is empty
        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "InvalidRouter",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality_result" },
                Branches = new List<ConditionalBranch>() // Empty branches
            }
        };
        var context = new VariableContext();

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new InvalidOperationException("Router is not a valid router"));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a valid router*");
    }

    [Fact]
    public async Task EvaluateRouterAsync_EmptyBranches_ThrowsInvalidOperationException()
    {
        // Arrange - RouterNode with empty branches (validation happens in BranchSelector)
        var router = new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "EmptyRouter",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality_result" },
                Branches = new List<ConditionalBranch>() // Empty list
            }
        };
        var context = new VariableContext();

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ThrowsAsync(new InvalidOperationException("Router is not a valid router"));

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a valid router*");
    }

    [Fact]
    public async Task EvaluateRouterAsync_BranchWithNoTargetId_ThrowsInvalidOperationException()
    {
        // Arrange
        var router = CreateRouter("QualityCheck", "quality_result");
        var branch = CreateBranch("OK", "quality_result == 'OK'", null); // No target
        var context = new VariableContext();
        context.SetValue("quality_result", "OK");

        _mockBranchSelector
            .Setup(x => x.SelectBranchAsync(router, context))
            .ReturnsAsync(branch);

        // Act
        Func<Task> act = async () => await _service.EvaluateRouterAsync(router, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion


    #region Helper Methods

    private static RouterNode CreateRouter(string name, string variableName)
    {
        var targetNodeId = Guid.NewGuid();
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0,
                Duration = 0, // Routers have instant duration
                Selector = new SimpleVariableSelector { Expression = variableName },
                Branches = new List<ConditionalBranch>
                {
                    CreateBranch("DefaultBranch", "true", targetNodeId)
                }
            }
        };
    }

    private static RouterNode CreateRouterWithBranches(
        string name,
        string variableName,
        IEnumerable<ConditionalBranch> branches)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = variableName },
                Branches = branches.ToList()
            }
        };
    }

    private static ConditionalBranch CreateBranch(
        string name,
        string? condition,
        Guid? targetNodeId,
        int priority = 0)
    {
        return new ConditionalBranch
        {
            Name = name,
            Condition = condition,
            Priority = priority,
            TargetNodeId = targetNodeId
        };
    }

    #endregion
}