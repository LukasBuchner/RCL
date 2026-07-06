using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Comprehensive unit tests for NodeWidthCalculator.
///     Tests cover scaling time durations to pixel widths based on TimeToPixelScale configuration,
///     and hierarchy-aware container expansion to prevent child overflow.
/// </summary>
public class NodeWidthCalculatorTests
{
    private static readonly Dictionary<Guid, IReadOnlyList<Node>> EmptyMapping = new();

    private readonly NodeWidthCalculator _calculator;
    private readonly IOptions<SchedulingConfiguration> _defaultOptions;
    private readonly Mock<ILogger<NodeWidthCalculator>> _mockLogger;

    public NodeWidthCalculatorTests()
    {
        _mockLogger = new Mock<ILogger<NodeWidthCalculator>>();

        // Default configuration: 100 pixels per time unit
        _defaultOptions = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 100.0
            }
        });

        _calculator = new NodeWidthCalculator(_defaultOptions, _mockLogger.Object);
    }

    #region Empty Collection Tests

    [Fact]
    public void CalculateNodeWidths_WithEmptyNodes_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeWidthCalculator(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeWidthCalculator(_defaultOptions, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert (constructor called in setup - no exception should be thrown)
        Assert.NotNull(_calculator);
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void CalculateNodeWidths_WithNullNodes_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _calculator.CalculateNodeWidths(null!, EmptyMapping));
    }

    [Fact]
    public void CalculateNodeWidths_WithNullMapping_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateNodeWidths(new List<Node>(), null!));
    }

    #endregion

    #region Single Node Tests

    [Fact]
    public void CalculateNodeWidths_WithSingleTaskNode_ShouldReturnCorrectWidth()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 5.0);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 5.0 * 100.0 = 500.0
        Assert.Equal(500.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithSingleSkillExecutionNode_ShouldReturnCorrectWidth()
    {
        // Arrange
        var node = CreateSkillExecutionNode("Skill1", 3.5);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 3.5 * 100.0 = 350.0
        Assert.Equal(350.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithSingleRouterNode_ShouldReturnCorrectWidth()
    {
        // Arrange
        var node = CreateRouterNode(0.5, 2);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 0.5 * 100.0 = 50.0
        Assert.Equal(50.0, result[node.Id]);
    }

    #endregion

    #region Multiple Node Tests

    [Fact]
    public void CalculateNodeWidths_WithMultipleNodesOfDifferentTypes_ShouldReturnCorrectWidths()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1", 2.0);
        var skillNode = CreateSkillExecutionNode("Skill1", 4.0);
        var routerNode = CreateRouterNode(1.0, 3);
        var nodes = new List<Node> { taskNode, skillNode, routerNode };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(200.0, result[taskNode.Id]); // 2.0 * 100.0
        Assert.Equal(400.0, result[skillNode.Id]); // 4.0 * 100.0
        Assert.Equal(100.0, result[routerNode.Id]); // 1.0 * 100.0
    }

    [Fact]
    public void CalculateNodeWidths_WithMultipleNodesOfSameType_ShouldReturnCorrectWidths()
    {
        // Arrange
        var node1 = CreateTaskNode("Task1", 1.0);
        var node2 = CreateTaskNode("Task2", 2.5);
        var node3 = CreateTaskNode("Task3", 0.75);
        var nodes = new List<Node> { node1, node2, node3 };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(100.0, result[node1.Id]); // 1.0 * 100.0
        Assert.Equal(250.0, result[node2.Id]); // 2.5 * 100.0
        Assert.Equal(75.0, result[node3.Id]); // 0.75 * 100.0
    }

    #endregion

    #region Custom Scale Tests

    [Fact]
    public void CalculateNodeWidths_WithCustomTimeToPixelScale_ShouldReturnCorrectWidths()
    {
        // Arrange
        var customOptions = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 50.0 // 50 pixels per time unit
            }
        });
        var customCalculator = new NodeWidthCalculator(customOptions, _mockLogger.Object);

        var node = CreateTaskNode("Task1", 4.0);
        var nodes = new List<Node> { node };

        // Act
        var result = customCalculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 4.0 * 50.0 = 200.0
        Assert.Equal(200.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithDifferentScales_ShouldScaleProportionally()
    {
        // Arrange - Scale of 200 pixels per time unit
        var doubleScaleOptions = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 200.0
            }
        });
        var doubleScaleCalculator = new NodeWidthCalculator(doubleScaleOptions, _mockLogger.Object);

        var node = CreateTaskNode("Task1", 3.0);
        var nodes = new List<Node> { node };

        // Act
        var defaultResult = _calculator.CalculateNodeWidths(nodes, EmptyMapping);
        var doubleResult = doubleScaleCalculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(defaultResult);
        Assert.Single(doubleResult);
        Assert.Equal(300.0, defaultResult[node.Id]); // 3.0 * 100.0
        Assert.Equal(600.0, doubleResult[node.Id]); // 3.0 * 200.0
        // Double scale should produce double width
        Assert.Equal(defaultResult[node.Id] * 2, doubleResult[node.Id]);
    }

    #endregion

    #region Container Width Tests — Nested Router Scenario

    /// <summary>
    ///     Reproduces the production bug where a nested router inside a branch task has a longer
    ///     duration than the parent task. The NodeWidthCalculator must ensure that a container
    ///     node's width is at least as large as the widest child's width; otherwise the child
    ///     overflows the parent visually.
    ///     <para>
    ///         Production scenario (TimeToPixelScale = 2):
    ///         <list type="bullet">
    ///             <item><c>OuterRouter (duration=75)</c> → Width = 150</item>
    ///             <item><c>Default Branch TaskNode (duration=75, parent=OuterRouter)</c> → Width = 150</item>
    ///             <item><c>InnerRouter (duration=110, parent=Default Branch)</c> → Width = 220</item>
    ///             <item><c>Branch 1 TaskNode (duration=110, parent=InnerRouter)</c> → Width = 220</item>
    ///             <item><c>Skill (duration=55, parent=Branch 1)</c> → Width = 110</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Bug: Default Branch TaskNode Width=150, but its child InnerRouter Width=220.
    ///         The child is wider than its parent container, causing visual overflow.
    ///     </para>
    /// </summary>
    [Fact]
    public void CalculateNodeWidths_NestedRouterWiderThanParentTask_ParentMustBeAtLeastAsWideAsChild()
    {
        // Arrange — match production config (TimeToPixelScale=2)
        var productionOptions = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 2.0
            }
        });
        var calculator = new NodeWidthCalculator(productionOptions, _mockLogger.Object);

        // Build the production hierarchy:
        // OuterRouter (dur=75) → Default Branch (dur=75) → InnerRouter (dur=110) → Branch1 (dur=110) → Skill (dur=55)
        var outerRouterId = Guid.NewGuid();
        var defaultBranchId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var outerRouter = CreateRouterNode(75, 2, outerRouterId);
        var defaultBranch = CreateTaskNode("Default Branch", 75, defaultBranchId);
        defaultBranch = defaultBranch with { ParentId = outerRouterId };

        var innerRouter = CreateRouterNode(110, 2, innerRouterId);
        innerRouter = innerRouter with { ParentId = defaultBranchId };

        var branch1 = CreateTaskNode("Branch 1", 110, branch1Id);
        branch1 = branch1 with { ParentId = innerRouterId };

        var skill = CreateSkillExecutionNode("Skill1", 55, skillId);
        skill = skill with { ParentId = branch1Id };

        var allNodes = new List<Node> { outerRouter, defaultBranch, innerRouter, branch1, skill };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouterId] = new List<Node> { defaultBranch },
            [defaultBranchId] = new List<Node> { innerRouter },
            [innerRouterId] = new List<Node> { branch1 },
            [branch1Id] = new List<Node> { skill }
        };

        // Act
        var widths = calculator.CalculateNodeWidths(allNodes, parentToChildrenMapping);

        // Assert — every container must be at least as wide as its widest child
        var outerRouterWidth = widths[outerRouterId];
        var defaultBranchWidth = widths[defaultBranchId];
        var innerRouterWidth = widths[innerRouterId];
        var skillWidth = widths[skillId];

        // Leaf node widths are duration * scale
        Assert.Equal(110.0, skillWidth); // 55 * 2

        // InnerRouter duration-based width would be 220 (110 * 2)
        // Branch1 duration-based width would be 220 (110 * 2)
        // These are correct for their own content

        // Default Branch must be expanded from 150 to 220 to fit InnerRouter
        Assert.True(
            defaultBranchWidth >= innerRouterWidth,
            $"Container 'Default Branch' (width={defaultBranchWidth}) must be at least as wide as " +
            $"its child 'InnerRouter' (width={innerRouterWidth}). " +
            $"Currently the child overflows the parent.");

        // OuterRouter must be at least as wide as Default Branch
        Assert.True(
            outerRouterWidth >= defaultBranchWidth,
            $"Container 'OuterRouter' (width={outerRouterWidth}) must be at least as wide as " +
            $"its child 'Default Branch' (width={defaultBranchWidth}). " +
            $"Currently the child overflows the parent.");
    }

    /// <summary>
    ///     Verifies that when a child node has a shorter duration than its parent,
    ///     the parent width remains based on its own duration (no shrinking).
    /// </summary>
    [Fact]
    public void CalculateNodeWidths_ChildShorterThanParent_ParentKeepsOwnDurationWidth()
    {
        // Arrange
        var productionOptions = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 2.0
            }
        });
        var calculator = new NodeWidthCalculator(productionOptions, _mockLogger.Object);

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentTask = CreateTaskNode("Parent", 100, parentId);
        var childRouter = CreateRouterNode(30, 1, childId);
        childRouter = childRouter with { ParentId = parentId };

        var allNodes = new List<Node> { parentTask, childRouter };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [parentId] = new List<Node> { childRouter }
        };

        // Act
        var widths = calculator.CalculateNodeWidths(allNodes, parentToChildrenMapping);

        // Assert — parent keeps own width (200) since child (60) is smaller
        Assert.Equal(200.0, widths[parentId]);
        Assert.Equal(60.0, widths[childId]);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateNodeWidths_WithZeroDuration_ShouldReturnZeroWidth()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 0.0);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(0.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithVerySmallDuration_ShouldReturnCorrectWidth()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 0.01);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 0.01 * 100.0 = 1.0
        Assert.Equal(1.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithVeryLargeDuration_ShouldReturnCorrectWidth()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 1000.0);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 1000.0 * 100.0 = 100000.0
        Assert.Equal(100000.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeWidths_WithFractionalDuration_ShouldReturnPreciseWidth()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 2.75);
        var nodes = new List<Node> { node };

        // Act
        var result = _calculator.CalculateNodeWidths(nodes, EmptyMapping);

        // Assert
        Assert.Single(result);
        // Width = Duration * TimeToPixelScale = 2.75 * 100.0 = 275.0
        Assert.Equal(275.0, result[node.Id]);
    }

    #endregion

    #region Helper Methods

    private static TaskNode CreateTaskNode(string name, double duration, Guid? id = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string name, double duration, Guid? id = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Skill for {name}",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static RouterNode CreateRouterNode(double duration, int branchCount, Guid? id = null)
    {
        var branches = new List<ConditionalBranch>();
        for (var i = 0; i < branchCount; i++)
            branches.Add(new ConditionalBranch
            {
                Name = $"Branch {i}",
                Condition = $"condition{i}",
                Priority = i,
                TargetNodeId = Guid.NewGuid()
            });

        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "RouterTask",
                Duration = duration,
                StartTime = 0.0,
                FinishTime = duration,
                Selector = new SimpleVariableSelector { Expression = "testSelector" },
                Branches = branches
            }
        };
    }

    #endregion
}