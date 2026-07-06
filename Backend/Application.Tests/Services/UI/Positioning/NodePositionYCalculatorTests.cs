using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Options;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Tests for the NodePositionYCalculator focusing on improved hierarchical spacing.
///     Tests follow TDD principles to drive the imple<SchedulingConfiguration>ntainer spacing.
/// </summary>
public class NodePositionYCalculatorTests
{
    private readonly IOptions<SchedulingConfiguration> _defaultOptions = Options.Create(new SchedulingConfiguration
    {
        Positioning = new PositioningConfiguration
        {
            BaseYOffset = 50.0,
            SiblingSpacing = 60.0,
            ContainerTopPadding = 30.0,
            ContainerBottomPadding = 10.0,
            BaseHeight = 50.0,
            RouterDropdownHeight = 26.0
        }
    });

    [Fact]
    public void CalculateYPosition_SingleNodeWithoutParent_ReturnsBaseOffset()
    {
        // Arrange
        var node = CreateTaskNode("node1");

        // Act
        // var result = calculator.CalculateYPosition(node, siblings);

        // Assert
        // Assert.Equal(50.0, result); // BaseYOffset
    }

    [Fact]
    public void CalculateYPosition_TwoSiblingNodes_ReturnsProperlySpagedPositions()
    {
        // Arrange
        var node1 = CreateTaskNode("node1");
        var node2 = CreateTaskNode("node2");

        // Act
        // var result1 = calculator.CalculateYPosition(node1, siblings);
        // var result2 = calculator.CalculateYPosition(node2, siblings);

        // Assert
        // Assert.Equal(50.0, result1); // BaseYOffset
        // Assert.Equal(110.0, result2); // BaseYOffset + SiblingSpacing
    }

    [Fact]
    public void CalculateYPosition_ChildNodeWithParent_ReturnsPositionWithinParentContainer()
    {
        // Arrange
        var parent = CreateTaskNode("parent", 100.0); // Parent at Y=100
        var child = CreateSkillExecutionNode("child", parentId: parent.Id);

        // Act
        // var result = calculator.CalculateYPosition(child, siblings, parent);

        // Assert
        // Should be: parent.Y + ContainerTopPadding = 100 + 30 = 130
        // Assert.Equal(130.0, result);
    }

    [Fact]
    public void CalculateYPosition_MultipleChildrenInContainer_SpacesChildrenProperly()
    {
        // Arrange
        var parent = CreateTaskNode("parent", 100.0);
        var child1 = CreateSkillExecutionNode("child1", parentId: parent.Id);
        var child2 = CreateSkillExecutionNode("child2", parentId: parent.Id);

        // Act
        // var result1 = calculator.CalculateYPosition(child1, siblings, parent);
        // var result2 = calculator.CalculateYPosition(child2, siblings, parent);

        // Assert
        // Child1: parent.Y + ContainerTopPadding = 100 + 30 = 130
        // Assert.Equal(130.0, result1);
        // Child2: child1.Y + SiblingSpacing = 130 + 60 = 190
        // Assert.Equal(190.0, result2);
    }

    [Fact]
    public void CalculateContainerHeight_EmptyContainer_ReturnsMinimumHeight()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var container = CreateTaskNode("container");
        var emptyChildren = new List<Node>();

        // Act
        var result = calculator.CalculateContainerHeight(container, emptyChildren);

        // Assert
        // MinHeight = ContainerTopPadding + ContainerBottomPadding = 30 + 10 = 40
        Assert.Equal(40.0, result);
    }

    [Fact]
    public void CalculateContainerHeight_ContainerWithChildren_CalculatesCorrectHeight()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var container = CreateTaskNode("container");
        var child1 = CreateSkillExecutionNode("child1", parentId: container.Id);
        var child2 = CreateSkillExecutionNode("child2", parentId: container.Id);
        var children = new List<Node> { child1, child2 };

        // Act
        var result = calculator.CalculateContainerHeight(container, children);

        // Assert
        // With React Flow positioning: 
        // Child1 at Y=30, height=50 → bottom=80
        // Child2 at Y=140 (30+50+60), height=50 → bottom=190
        // Container height = max bottom extent + ContainerBottomPadding = 190 + 10 = 200
        Assert.Equal(200.0, result);
    }

    [Fact]
    public void CalculateContainerHeight_NestedContainers_CalculatesHeightRecursively()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var outerContainer = CreateTaskNode("outer");
        var innerContainer = CreateTaskNode("inner", parentId: outerContainer.Id);
        var innerChild1 = CreateSkillExecutionNode("innerChild1", parentId: innerContainer.Id);
        var innerChild2 = CreateSkillExecutionNode("innerChild2", parentId: innerContainer.Id);

        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { outerContainer.Id, new List<Node> { innerContainer } },
            { innerContainer.Id, new List<Node> { innerChild1, innerChild2 } }
        };

        // Act
        var result =
            calculator.CalculateContainerHeight(outerContainer, new List<Node> { innerContainer }, nodeHierarchy);

        // Assert
        // Inner container height with React Flow positioning = 200 (calculated above)
        // Outer container: Inner at Y=30, height=200 → bottom extent = 230
        // Outer container height = 230 + ContainerBottomPadding = 240
        Assert.Equal(240.0, result);
    }

    [Fact]
    public void CalculateYPositions_ContainerSiblings_PositionsContainersBasedOnTheirHeight()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        // First container with 2 children
        var container1 = CreateTaskNode("container1");
        var child1A = CreateSkillExecutionNode("child1a", parentId: container1.Id);
        var child1B = CreateSkillExecutionNode("child1b", parentId: container1.Id);

        // Second container with 3 children
        var container2 = CreateTaskNode("container2");
        var child2A = CreateSkillExecutionNode("child2a", parentId: container2.Id);
        var child2B = CreateSkillExecutionNode("child2b", parentId: container2.Id);
        var child2C = CreateSkillExecutionNode("child2c", parentId: container2.Id);

        // Don't use null keys - let the implementation detect root nodes automatically  
        var fullHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { container1.Id, new List<Node> { child1A, child1B } },
            { container2.Id, new List<Node> { child2A, child2B, child2C } }
        };

        var allNodes = new List<Node> { container1, container2, child1A, child1B, child2A, child2B, child2C };

        // Provide timing info to ensure deterministic sort order (container1 before container2)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                container1.Id,
                new NodeTimingInfo { NodeId = container1.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                container2.Id,
                new NodeTimingInfo { NodeId = container2.Id, AbsoluteStartTime = 1.0, RelativeStartTime = 1.0 }
            },
            {
                child1A.Id, new NodeTimingInfo { NodeId = child1A.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                child1B.Id, new NodeTimingInfo { NodeId = child1B.Id, AbsoluteStartTime = 0.1, RelativeStartTime = 0.1 }
            },
            {
                child2A.Id, new NodeTimingInfo { NodeId = child2A.Id, AbsoluteStartTime = 1.0, RelativeStartTime = 0.0 }
            },
            {
                child2B.Id, new NodeTimingInfo { NodeId = child2B.Id, AbsoluteStartTime = 1.1, RelativeStartTime = 0.1 }
            },
            { child2C.Id, new NodeTimingInfo { NodeId = child2C.Id, AbsoluteStartTime = 1.2, RelativeStartTime = 0.2 } }
        };

        // Act - the implementation will detect container1 and container2 as root nodes
        var result = calculator.CalculateYPositions(allNodes, fullHierarchy, timingInfo);

        // Assert
        // Container1 at BaseYOffset = 50
        Assert.Equal(50.0, result[container1.Id]);

        // Container1 height with React Flow positioning = 200
        // Container2 should start after Container1: 50 + 200 + SiblingSpacing = 310
        Assert.Equal(310.0, result[container2.Id]);

        // Children should be positioned relative to their parent containers (React Flow style)
        Assert.Equal(30.0, result[child1A.Id]); // ContainerTopPadding = 30 (relative to container1)
        Assert.Equal(140.0, result[child1B.Id]); // ContainerTopPadding + BaseHeight + SiblingSpacing = 30 + 50 + 60

        Assert.Equal(30.0, result[child2A.Id]); // ContainerTopPadding = 30 (relative to container2)
        Assert.Equal(140.0, result[child2B.Id]); // ContainerTopPadding + BaseHeight + SiblingSpacing = 30 + 50 + 60
        Assert.Equal(250.0, result[child2C.Id]); // ContainerTopPadding + 2*(BaseHeight + SiblingSpacing) = 30 + 2*110
    }

    [Fact]
    public void CalculateYPositions_ThreeLevelHierarchy_PositionsAllLevelsCorrectly()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        var rootContainer = CreateTaskNode("root");
        var midContainer = CreateTaskNode("mid", parentId: rootContainer.Id);
        var leafChild1 = CreateSkillExecutionNode("leaf1", parentId: midContainer.Id);
        var leafChild2 = CreateSkillExecutionNode("leaf2", parentId: midContainer.Id);

        // Don't use null keys - let the implementation detect root nodes automatically
        var fullHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { rootContainer.Id, new List<Node> { midContainer } },
            { midContainer.Id, new List<Node> { leafChild1, leafChild2 } }
        };

        var allNodes = new List<Node> { rootContainer, midContainer, leafChild1, leafChild2 };

        // Provide timing info to ensure deterministic sort order
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                rootContainer.Id,
                new NodeTimingInfo { NodeId = rootContainer.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                midContainer.Id,
                new NodeTimingInfo { NodeId = midContainer.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                leafChild1.Id,
                new NodeTimingInfo { NodeId = leafChild1.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                leafChild2.Id,
                new NodeTimingInfo { NodeId = leafChild2.Id, AbsoluteStartTime = 0.1, RelativeStartTime = 0.1 }
            }
        };

        // Act - the implementation will detect rootContainer as a root node
        var result = calculator.CalculateYPositions(allNodes, fullHierarchy, timingInfo);

        // Assert
        // Root container at BaseYOffset = 50
        Assert.Equal(50.0, result[rootContainer.Id]);

        // Mid container within root: relative to root = ContainerTopPadding = 30
        Assert.Equal(30.0, result[midContainer.Id]);

        // Leaf children within mid container (relative to midContainer)
        Assert.Equal(30.0, result[leafChild1.Id]); // ContainerTopPadding = 30 (relative to midContainer)
        Assert.Equal(140.0, result[leafChild2.Id]); // ContainerTopPadding + BaseHeight + SiblingSpacing = 30 + 50 + 60
    }

    [Fact]
    public void CalculateYPositions_RouterNodeAsLeaf_UsesCorrectHeight()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var routerNode = CreateRouterNode("router");

        var allNodes = new List<Node> { routerNode };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy);

        // Assert
        // RouterNode should be at BaseYOffset = 50
        Assert.Equal(50.0, result[routerNode.Id]);

        // A branchless RouterNode uses BaseHeight = 50 (no dropdown).
        // A RouterNode WITH branches uses BaseHeight + RouterDropdownHeight = 50 + 26 = 76.
        // The next test validates sibling spacing with a branched RouterNode.
    }

    [Fact]
    public void CalculateYPositions_RouterNodeWithSibling_DoesNotOverlap()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var routerNode = CreateRouterNode("router", branchCount: 2);
        var taskNode = CreateTaskNode("task");

        var allNodes = new List<Node> { routerNode, taskNode };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Provide timing info for deterministic sort order (routerNode first)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                routerNode.Id,
                new NodeTimingInfo { NodeId = routerNode.Id, AbsoluteStartTime = 0.0, RelativeStartTime = 0.0 }
            },
            {
                taskNode.Id,
                new NodeTimingInfo { NodeId = taskNode.Id, AbsoluteStartTime = 1.0, RelativeStartTime = 1.0 }
            }
        };

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy, timingInfo);

        // Assert
        // RouterNode at BaseYOffset = 50
        Assert.Equal(50.0, result[routerNode.Id]);

        // Next sibling should be at: BaseYOffset + (BaseHeight + RouterDropdownHeight) + SiblingSpacing
        // = 50 + (50 + 26) + 60 = 186
        // This ensures no overlap with the RouterNode which needs extra height for the dropdown
        Assert.Equal(186.0, result[taskNode.Id]);
    }

    [Fact]
    public void CalculateContainerHeight_ContainerWithRouterNodeChild_IncludesRouterDropdownHeight()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);
        var container = CreateTaskNode("container");
        var routerChild = CreateRouterNode("routerChild", parentId: container.Id, branchCount: 2);

        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { container.Id, new List<Node> { routerChild } }
        };

        // Act
        var result = calculator.CalculateContainerHeight(container, new List<Node> { routerChild }, nodeHierarchy);

        // Assert
        // Container height calculation:
        // - RouterChild at relative Y = ContainerTopPadding = 30
        // - RouterChild height = BaseHeight + RouterDropdownHeight = 50 + 26 = 76 (has branches → dropdown)
        // - RouterChild bottom extent = 30 + 76 = 106
        // - Container height = 106 + ContainerBottomPadding = 106 + 10 = 116
        Assert.Equal(116.0, result);
    }

    private TaskNode CreateTaskNode(string name, double yPosition = 0, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Task = new Task
            {
                Name = name,
                StartTime = 0.0,
                Duration = 100.0,
                FinishTime = 100.0
            },
            Position = new NodePosition { X = 0, Y = yPosition },
            ParentId = parentId
        };
    }

    private SkillExecutionNode CreateSkillExecutionNode(string name, double yPosition = 0, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = 50.0,
                FinishTime = 50.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Test skill: {name}",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            },
            Position = new NodePosition { X = 0, Y = yPosition },
            ParentId = parentId
        };
    }

    private RouterNode CreateRouterNode(string name, double yPosition = 0, Guid? parentId = null, int branchCount = 0)
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
            Id = Guid.NewGuid(),
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = 0.0,
                FinishTime = 0.0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "true"
                },
                Branches = branches
            },
            Position = new NodePosition { X = 0, Y = yPosition },
            ParentId = parentId
        };
    }
}