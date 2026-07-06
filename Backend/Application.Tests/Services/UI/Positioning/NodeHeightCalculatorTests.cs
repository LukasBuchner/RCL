using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Comprehensive unit tests for NodeHeightCalculator.
///     Tests cover all scenarios: null inputs, empty collections, single nodes, multiple nodes,
///     container nodes with children, and leaf nodes.
/// </summary>
public class NodeHeightCalculatorTests
{
    private readonly NodeHeightCalculator _calculator;
    private readonly Mock<ILogger<NodeHeightCalculator>> _mockLogger;
    private readonly Mock<INodePositionYCalculator> _mockPositionYCalculator;

    public NodeHeightCalculatorTests()
    {
        _mockPositionYCalculator = new Mock<INodePositionYCalculator>();
        _mockLogger = new Mock<ILogger<NodeHeightCalculator>>();
        _calculator = new NodeHeightCalculator(_mockPositionYCalculator.Object, _mockLogger.Object);

        // Setup default base height
        _mockPositionYCalculator.Setup(x => x.BaseHeight).Returns(50.0);
    }

    #region Nested Hierarchy Tests

    [Fact]
    public void CalculateNodeHeights_WithNestedHierarchy_ShouldReturnCorrectHeights()
    {
        // Arrange
        var grandParentNode = CreateTaskNode("GrandParent", 200.0);
        var parentNode = CreateTaskNode("Parent", 100.0);
        var childNode = CreateTaskNode("Child", 50.0);

        var nodes = new List<Node> { grandParentNode, parentNode, childNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [grandParentNode.Id] = new List<Node> { parentNode },
            [parentNode.Id] = new List<Node> { childNode }
        };

        var grandParentHeight = 180.0;
        var parentHeight = 90.0;

        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                grandParentNode, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(grandParentHeight);

        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                parentNode, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(parentHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(grandParentHeight, result[grandParentNode.Id]);
        Assert.Equal(parentHeight, result[parentNode.Id]);
        Assert.Equal(50.0, result[childNode.Id]); // Leaf node gets base height
    }

    #endregion

    #region Container Detection Tests

    [Fact]
    public void CalculateNodeHeights_WithContainerHavingEmptyChildrenList_ShouldTreatAsLeafNode()
    {
        // Arrange
        var node = CreateTaskNode("Node", 100.0);
        var nodes = new List<Node> { node };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [node.Id] = new List<Node>() // Empty children list
        };

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[node.Id]); // Should get base height, not calculated height

        // Verify CalculateContainerHeight was not called
        _mockPositionYCalculator.Verify(x => x.CalculateContainerHeight(
                It.IsAny<Node>(), It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateNodeHeights_WithNodeTypeMix_ShouldHandleAllTypes()
    {
        // Arrange
        var taskNode = CreateTaskNode("TaskNode", 50.0);
        var skillNode = CreateSkillExecutionNode("SkillNode", 60.0);
        var containerNode = CreateTaskNode("Container", 100.0);

        var nodes = new List<Node> { taskNode, skillNode, containerNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [containerNode.Id] = new List<Node> { taskNode, skillNode }
        };

        var containerHeight = 150.0;
        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                containerNode, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(containerHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(50.0, result[taskNode.Id]);
        Assert.Equal(50.0, result[skillNode.Id]);
        Assert.Equal(containerHeight, result[containerNode.Id]);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPositionYCalculator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeHeightCalculator(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NodeHeightCalculator(_mockPositionYCalculator.Object, null!));
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
    public void CalculateNodeHeights_WithNullNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateNodeHeights(null!, parentToChildrenMapping));
    }

    [Fact]
    public void CalculateNodeHeights_WithNullParentToChildrenMapping_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Task1", 50.0) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateNodeHeights(nodes, null!));
    }

    #endregion

    #region Empty Collection Tests

    [Fact]
    public void CalculateNodeHeights_WithEmptyNodes_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var nodes = new List<Node>();
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateNodeHeights_WithEmptyParentToChildrenMapping_ShouldReturnBaseHeightsForAllNodes()
    {
        // Arrange
        var node1 = CreateTaskNode("Task1", 50.0);
        var node2 = CreateTaskNode("Task2", 75.0);
        var nodes = new List<Node> { node1, node2 };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(50.0, result[node1.Id]);
        Assert.Equal(50.0, result[node2.Id]);
    }

    #endregion

    #region Single Node Tests

    [Fact]
    public void CalculateNodeHeights_WithSingleLeafNode_ShouldReturnBaseHeight()
    {
        // Arrange
        var node = CreateTaskNode("Task1", 50.0);
        var nodes = new List<Node> { node };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeHeights_WithSingleContainerNode_ShouldReturnCalculatedHeight()
    {
        // Arrange
        var parentNode = CreateTaskNode("Parent", 100.0);
        var childNode = CreateTaskNode("Child", 50.0);
        var nodes = new List<Node> { parentNode, childNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [parentNode.Id] = new List<Node> { childNode }
        };

        var calculatedHeight = 120.0;
        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                parentNode, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(calculatedHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(calculatedHeight, result[parentNode.Id]);
        Assert.Equal(50.0, result[childNode.Id]); // Child should get base height
    }

    #endregion

    #region Multiple Node Tests

    [Fact]
    public void CalculateNodeHeights_WithMultipleLeafNodes_ShouldReturnBaseHeightForAll()
    {
        // Arrange
        var nodes = new List<Node>
        {
            CreateTaskNode("Task1", 50.0),
            CreateTaskNode("Task2", 75.0),
            CreateTaskNode("Task3", 100.0)
        };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(3, result.Count);
        foreach (var node in nodes) Assert.Equal(50.0, result[node.Id]);
    }

    [Fact]
    public void CalculateNodeHeights_WithMixedContainerAndLeafNodes_ShouldReturnCorrectHeights()
    {
        // Arrange
        var parentNode = CreateTaskNode("Parent", 100.0);
        var childNode1 = CreateTaskNode("Child1", 50.0);
        var childNode2 = CreateTaskNode("Child2", 60.0);
        var independentNode = CreateTaskNode("Independent", 80.0);

        var nodes = new List<Node> { parentNode, childNode1, childNode2, independentNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [parentNode.Id] = new List<Node> { childNode1, childNode2 }
        };

        var calculatedHeight = 140.0;
        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                parentNode, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(calculatedHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(calculatedHeight, result[parentNode.Id]); // Container height
        Assert.Equal(50.0, result[childNode1.Id]); // Base height for leaf
        Assert.Equal(50.0, result[childNode2.Id]); // Base height for leaf
        Assert.Equal(50.0, result[independentNode.Id]); // Base height for leaf
    }

    #endregion

    #region RouterNode Height Tests

    [Fact]
    public void CalculateNodeHeights_RouterNodeWithBranches_AddsDropdownHeight()
    {
        // Arrange
        var routerNode = CreateRouterNode(false, 2);
        var nodes = new List<Node> { routerNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Setup RouterDropdownHeight
        _mockPositionYCalculator.Setup(x => x.RouterDropdownHeight).Returns(26.0);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        var expectedHeight = 50.0 + 26.0; // BaseHeight (50) + RouterDropdownHeight (26) = 76
        Assert.Single(result);
        Assert.Equal(expectedHeight, result[routerNode.Id]);
    }

    [Fact]
    public void CalculateNodeHeights_RouterNodeWithoutBranches_UsesBaseHeight()
    {
        // Arrange
        var routerNode = CreateRouterNode(false, 0);
        var nodes = new List<Node> { routerNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[routerNode.Id]); // Only BaseHeight (50)

        // Verify RouterDropdownHeight was not accessed since there are no branches
        _mockPositionYCalculator.Verify(x => x.RouterDropdownHeight, Times.Never);
    }

    [Fact]
    public void CalculateNodeHeights_SkillExecutionNode_UnaffectedByRouterChanges()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("SkillNode", 60.0);
        var nodes = new List<Node> { skillNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[skillNode.Id]); // Only BaseHeight (50)

        // Verify RouterDropdownHeight was not accessed for non-router nodes
        _mockPositionYCalculator.Verify(x => x.RouterDropdownHeight, Times.Never);
    }

    [Fact]
    public void CalculateNodeHeights_TaskNode_UnaffectedByRouterChanges()
    {
        // Arrange
        var taskNode = CreateTaskNode("TaskNode", 100.0);
        var nodes = new List<Node> { taskNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Single(result);
        Assert.Equal(50.0, result[taskNode.Id]); // Only BaseHeight (50)

        // Verify RouterDropdownHeight was not accessed for non-router nodes
        _mockPositionYCalculator.Verify(x => x.RouterDropdownHeight, Times.Never);
    }

    [Fact]
    public void CalculateNodeHeights_MixedNodes_AppliesCorrectHeightsToEach()
    {
        // Arrange
        var routerWithBranches = CreateRouterNode(false, 3);
        var routerWithoutBranches = CreateRouterNode(false, 0);
        var skillNode = CreateSkillExecutionNode("SkillNode", 60.0);
        var taskNode = CreateTaskNode("TaskNode", 100.0);
        var nodes = new List<Node> { routerWithBranches, routerWithoutBranches, skillNode, taskNode };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Setup RouterDropdownHeight
        _mockPositionYCalculator.Setup(x => x.RouterDropdownHeight).Returns(26.0);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        var expectedRouterHeight = 50.0 + 26.0; // BaseHeight + RouterDropdownHeight
        Assert.Equal(4, result.Count);
        Assert.Equal(expectedRouterHeight, result[routerWithBranches.Id]);
        Assert.Equal(50.0, result[routerWithoutBranches.Id]);
        Assert.Equal(50.0, result[skillNode.Id]);
        Assert.Equal(50.0, result[taskNode.Id]);
    }

    [Fact]
    public void CalculateNodeHeights_RouterNodeAsContainerWithBranches_AddsDropdownHeightToContainerHeight()
    {
        // Arrange
        // A RouterNode that is both:
        // 1. A container (has children) - so it needs container height calculation
        // 2. Has branches - so it needs RouterDropdownHeight added for the dropdown UI
        var routerContainer = CreateRouterNode(true, 2);
        var child1 = CreateSkillExecutionNode("Child1", 50.0, parentId: routerContainer.Id);
        var child2 = CreateSkillExecutionNode("Child2", 50.0, parentId: routerContainer.Id);
        var nodes = new List<Node> { routerContainer, child1, child2 };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerContainer.Id] = new List<Node> { child1, child2 }
        };

        // Mock the container height calculation (height needed to contain children)
        var baseContainerHeight = 150.0;
        var routerDropdownHeight = 26.0; // Still need this for the expected height
        var expectedContainerHeightFromMock =
            baseContainerHeight + routerDropdownHeight; // The value the mock should return

        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                routerContainer, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(expectedContainerHeightFromMock); // Mock returns the full expected height

        // Mock the RouterDropdownHeight (extra height for the dropdown selector UI)
        _mockPositionYCalculator.Setup(x => x.RouterDropdownHeight).Returns(routerDropdownHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        var expectedRouterHeight = baseContainerHeight + routerDropdownHeight; // 150 + 26 = 176
        Assert.Equal(3, result.Count);

        // CRITICAL: RouterNode container should have BOTH container height AND dropdown height
        Assert.Equal(expectedRouterHeight, result[routerContainer.Id]);


        // Child heights should remain unchanged
        Assert.Equal(50.0, result[child1.Id]);
        Assert.Equal(50.0, result[child2.Id]);
    }

    [Fact]
    public void CalculateNodeHeights_RouterNodeAsContainerWithoutBranches_UsesOnlyContainerHeight()
    {
        // Arrange
        // A RouterNode that is a container but has NO branches
        // Should use container height WITHOUT adding dropdown height
        var routerContainer = CreateRouterNode(true, 0);
        var child1 = CreateSkillExecutionNode("Child1", 50.0, parentId: routerContainer.Id);
        var nodes = new List<Node> { routerContainer, child1 };
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerContainer.Id] = new List<Node> { child1 }
        };

        var baseContainerHeight = 100.0;
        _mockPositionYCalculator.Setup(x => x.CalculateContainerHeight(
                routerContainer, It.IsAny<IReadOnlyList<Node>>(), parentToChildrenMapping))
            .Returns(baseContainerHeight);

        // Act
        var result = _calculator.CalculateNodeHeights(nodes, parentToChildrenMapping);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(baseContainerHeight, result[routerContainer.Id]); // Only container height, no dropdown
        Assert.Equal(50.0, result[child1.Id]);

        // Verify RouterDropdownHeight was NOT accessed since there are no branches
        _mockPositionYCalculator.Verify(x => x.RouterDropdownHeight, Times.Never);
    }

    #endregion

    #region Helper Methods

    private static TaskNode CreateTaskNode(string name, double duration, Guid? id = null, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Height = duration,
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string name, double duration, Guid? id = null,
        Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Height = duration,
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

    private static RouterNode CreateRouterNode(bool hasChildren, int branchCount, Guid? id = null,
        Guid? parentId = null)
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
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Height = hasChildren ? 150.0 : 50.0,
            RouterTask = new RouterTask
            {
                Name = "RouterTask",
                Duration = 0.0,
                StartTime = 0.0,
                FinishTime = 0.0,
                Selector = new SimpleVariableSelector { Expression = "testSelector" },
                Branches = branches
            }
        };
    }

    #endregion
}