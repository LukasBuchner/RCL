using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services;

/// <summary>
///     Tests for <see cref="GraphQlMutationLogger.LogNodeOperationDetails" />,
///     verifying that node-type dispatch and logging work correctly for all node types
///     and operations. This covers the OCP fix that unified duplicate switch statements.
/// </summary>
public class GraphQlMutationLoggerTests
{
    private readonly Mock<ILogger> _mockLogger;

    public GraphQlMutationLoggerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    // ─── TaskNode Logging ────────────────────────────────────────────

    [Fact]
    public void LogNodeOperationDetails_TaskNode_CreateOperation_LogsCorrectly()
    {
        var node = CreateTaskNode("Pick Part", 5.0, 1.0);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_TaskNode_UpdateOperation_LogsCorrectly()
    {
        var node = CreateTaskNode("Pick Part", 5.0, 1.0);

        _mockLogger.Object.LogNodeOperationDetails("UPDATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_TaskNode_WithParent_LogsParentId()
    {
        var parentId = Guid.NewGuid();
        var node = CreateTaskNode("Child Task", parentId: parentId);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_TaskNode_WithoutParent_LogsNullParentId()
    {
        var node = CreateTaskNode("Root Task", parentId: null);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    // ─── SkillExecutionNode Logging ──────────────────────────────────

    [Fact]
    public void LogNodeOperationDetails_SkillExecutionNode_CreateOperation_LogsCorrectly()
    {
        var node = CreateSkillExecutionNode(duration: 3.0, startTime: 2.0);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_SkillExecutionNode_UpdateOperation_LogsCorrectly()
    {
        var node = CreateSkillExecutionNode(duration: 3.0, startTime: 2.0);

        _mockLogger.Object.LogNodeOperationDetails("UPDATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_SkillExecutionNode_WithProperties_LogsProperties()
    {
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Speed",
                Value = TypedValue.Number(1.5),
                Direction = PropertyDirection.Input
            }
        };

        var node = CreateSkillExecutionNode(properties: properties);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_SkillExecutionNode_WithEmptyProperties_LogsCorrectly()
    {
        var node = CreateSkillExecutionNode("Gripper Open", properties: new List<TypedProperty>());

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_SkillExecutionNode_LogsAgentId()
    {
        var agentId = Guid.NewGuid();
        var node = CreateSkillExecutionNode(agentId: agentId);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    // ─── RouterNode Logging ──────────────────────────────────────────

    [Fact]
    public void LogNodeOperationDetails_RouterNode_CreateOperation_LogsCorrectly()
    {
        var node = CreateRouterNode("Quality Check Router", 0.5, 10.0);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_RouterNode_UpdateOperation_LogsCorrectly()
    {
        var node = CreateRouterNode("Quality Check Router", 0.5, 10.0);

        _mockLogger.Object.LogNodeOperationDetails("UPDATE_NODE", node);

        VerifyLoggedOnce();
    }

    [Fact]
    public void LogNodeOperationDetails_RouterNode_WithParent_LogsParentId()
    {
        var parentId = Guid.NewGuid();
        var node = CreateRouterNode("Branch Router", parentId: parentId);

        _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", node);

        VerifyLoggedOnce();
    }

    // ─── Unknown Node Type ───────────────────────────────────────────

    [Fact]
    public void LogNodeOperationDetails_UnknownNodeType_ThrowsNotSupportedException()
    {
        var unknownNode = new UnknownTestNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 }
        };

        var ex = Assert.Throws<NotSupportedException>(() =>
            _mockLogger.Object.LogNodeOperationDetails("CREATE_NODE", unknownNode));

        Assert.Contains("UnknownTestNode", ex.Message);
    }

    // ─── Custom Operation Names ──────────────────────────────────────

    [Theory]
    [InlineData("CREATE_NODE")]
    [InlineData("UPDATE_NODE")]
    [InlineData("DELETE_NODE")]
    [InlineData("CUSTOM_OP")]
    public void LogNodeOperationDetails_AcceptsArbitraryOperationName(string operation)
    {
        var node = CreateTaskNode();

        _mockLogger.Object.LogNodeOperationDetails(operation, node);

        VerifyLoggedOnce();
    }

    // ─── FormatProperties ────────────────────────────────────────────

    [Fact]
    public void FormatProperties_NullProperties_ReturnsNull()
    {
        var result = GraphQlMutationLogger.FormatProperties(null);

        Assert.Null(result);
    }

    [Fact]
    public void FormatProperties_EmptyList_ReturnsNull()
    {
        var result = GraphQlMutationLogger.FormatProperties(new List<TypedProperty>());

        Assert.Null(result);
    }

    [Fact]
    public void FormatProperties_NumberProperty_FormatsWithTwoDecimals()
    {
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Speed",
                Value = TypedValue.Number(1.5),
                Direction = PropertyDirection.Input
            }
        };

        var result = GraphQlMutationLogger.FormatProperties(properties);

        Assert.NotNull(result);
        Assert.Contains("Speed=1.50", result);
    }

    [Fact]
    public void FormatProperties_BooleanProperty_FormatsCorrectly()
    {
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Enabled",
                Value = TypedValue.Boolean(true),
                Direction = PropertyDirection.Input
            }
        };

        var result = GraphQlMutationLogger.FormatProperties(properties);

        Assert.NotNull(result);
        Assert.Contains("Enabled=True", result);
    }

    [Fact]
    public void FormatProperties_StringProperty_FormatsWithQuotes()
    {
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Label",
                Value = TypedValue.Text("hello"),
                Direction = PropertyDirection.Input
            }
        };

        var result = GraphQlMutationLogger.FormatProperties(properties);

        Assert.NotNull(result);
        Assert.Contains("Label=\"hello\"", result);
    }

    [Fact]
    public void FormatProperties_PositionProperty_FormatsAllAxes()
    {
        var pos = new Position
        {
            X = 1.0, Y = 2.0, Z = 3.0,
            Alpha = 0.1, Beta = 0.2, Gamma = 0.3
        };
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Target",
                Value = TypedValue.Position(pos),
                Direction = PropertyDirection.Input
            }
        };

        var result = GraphQlMutationLogger.FormatProperties(properties);

        Assert.NotNull(result);
        Assert.Contains("X=1.00", result);
        Assert.Contains("Y=2.00", result);
        Assert.Contains("Z=3.00", result);
        Assert.Contains("Alpha=0.10", result);
    }

    [Fact]
    public void FormatProperties_MultipleProperties_JoinsWithComma()
    {
        var properties = new List<TypedProperty>
        {
            new()
            {
                Name = "Speed",
                Value = TypedValue.Number(1.5),
                Direction = PropertyDirection.Input
            },
            new()
            {
                Name = "Enabled",
                Value = TypedValue.Boolean(false),
                Direction = PropertyDirection.Input
            }
        };

        var result = GraphQlMutationLogger.FormatProperties(properties);

        Assert.NotNull(result);
        Assert.Contains(", ", result);
        Assert.Contains("Speed=", result);
        Assert.Contains("Enabled=", result);
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static TaskNode CreateTaskNode(
        string name = "Test Task",
        double duration = 5.0,
        double startTime = 0.0,
        Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            ParentId = parentId,
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = startTime
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(
        string skillName = "Move To Position",
        double duration = 3.0,
        double startTime = 0.0,
        Guid? agentId = null,
        List<TypedProperty>? properties = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 300, Y = 400 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = duration,
                StartTime = startTime,
                AgentId = agentId ?? Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = $"Test skill: {skillName}",
                    Properties = properties ?? []
                }
            }
        };
    }

    private static RouterNode CreateRouterNode(
        string name = "Test Router",
        double duration = 0.5,
        double startTime = 0.0,
        Guid? parentId = null)
    {
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 500, Y = 600 },
            ParentId = parentId,
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = duration,
                StartTime = startTime,
                Selector = new SimpleVariableSelector { Expression = "test_var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch A", Condition = "value == 'A'", Priority = 0 },
                    new() { Name = "Default", Condition = null, Priority = 1 }
                }
            }
        };
    }

    private void VerifyLoggedOnce()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     A node type not handled by the switch — used to verify the default case throws.
    /// </summary>
    private record UnknownTestNode : Node;
}