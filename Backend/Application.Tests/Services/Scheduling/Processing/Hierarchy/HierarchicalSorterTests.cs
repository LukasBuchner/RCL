using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Unit tests for Hierarchic<ILogger<HierarchicalSorter>>ry>
public class HierarchicalSorterTests
{
    private readonly Mock<ILogger<HierarchicalSorter>> _mockLogger;
    private readonly HierarchicalSorter _sorter;

    public HierarchicalSorterTests()
    {
        _mockLogger = new Mock<ILogger<HierarchicalSorter>>();
        _sorter = new HierarchicalSorter(_mockLogger.Object);
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithSimpleHierarchy_ReturnsCorrectOrder()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(rootId, "Root"),
            CreateTaskNode(childId, "Child", rootId),
            CreateTaskNode(grandchildId, "Grandchild", childId)
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Equal(3, result.Count);

        // Grandchild should come first, then child, then root
        var grandchildIndex = result.ToList().FindIndex(n => n.Id == grandchildId);
        var childIndex = result.ToList().FindIndex(n => n.Id == childId);
        var rootIndex = result.ToList().FindIndex(n => n.Id == rootId);

        Assert.True(grandchildIndex < childIndex, "Grandchild should come before child");
        Assert.True(childIndex < rootIndex, "Child should come before root");
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithMultipleRoots_ProcessesAllSubtrees()
    {
        // Arrange
        var root1Id = Guid.NewGuid();
        var root2Id = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(root1Id, "Root1"),
            CreateTaskNode(root2Id, "Root2"),
            CreateTaskNode(child1Id, "Child1", root1Id),
            CreateTaskNode(child2Id, "Child2", root2Id)
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Equal(4, result.Count);

        // Children should come before their respective parents
        var child1Index = result.ToList().FindIndex(n => n.Id == child1Id);
        var root1Index = result.ToList().FindIndex(n => n.Id == root1Id);
        var child2Index = result.ToList().FindIndex(n => n.Id == child2Id);
        var root2Index = result.ToList().FindIndex(n => n.Id == root2Id);

        Assert.True(child1Index < root1Index, "Child1 should come before Root1");
        Assert.True(child2Index < root2Index, "Child2 should come before Root2");
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithSiblings_ProcessesAllSiblings()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var sibling1Id = Guid.NewGuid();
        var sibling2Id = Guid.NewGuid();
        var sibling3Id = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(rootId, "Root"),
            CreateTaskNode(sibling1Id, "Sibling1", rootId),
            CreateTaskNode(sibling2Id, "Sibling2", rootId),
            CreateTaskNode(sibling3Id, "Sibling3", rootId)
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Equal(4, result.Count);

        // All siblings should come before root
        var rootIndex = result.ToList().FindIndex(n => n.Id == rootId);
        var sibling1Index = result.ToList().FindIndex(n => n.Id == sibling1Id);
        var sibling2Index = result.ToList().FindIndex(n => n.Id == sibling2Id);
        var sibling3Index = result.ToList().FindIndex(n => n.Id == sibling3Id);

        Assert.True(sibling1Index < rootIndex, "Sibling1 should come before root");
        Assert.True(sibling2Index < rootIndex, "Sibling2 should come before root");
        Assert.True(sibling3Index < rootIndex, "Sibling3 should come before root");
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithOrphanedNodes_IncludesAllNodes()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var orphanId = Guid.NewGuid();
        var invalidParentId = Guid.NewGuid(); // parent doesn't exist

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(rootId, "Root"),
            CreateTaskNode(childId, "Child", rootId),
            CreateTaskNode(orphanId, "Orphan", invalidParentId) // orphaned node
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, n => n.Id == rootId);
        Assert.Contains(result, n => n.Id == childId);
        Assert.Contains(result, n => n.Id == orphanId);
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var taskNodes = new List<TaskNode>();

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithSingleNode_ReturnsSingleNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(nodeId, "Single")
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert
        Assert.Single(result);
        Assert.Equal(nodeId, result[0].Id);
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _sorter.SortTaskNodesHierarchically(null!));
    }

    [Fact]
    public void SortTaskNodesHierarchically_WithDuplicateProcessing_HandlesCorrectly()
    {
        // Arrange - Create a scenario where a node could be processed multiple times
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(rootId, "Root"),
            CreateTaskNode(childId, "Child", rootId),
            // Add the same child again to test duplicate handling
            CreateTaskNode(childId, "Child", rootId)
        };

        // Act
        var result = _sorter.SortTaskNodesHierarchically(taskNodes);

        // Assert - Should handle duplicates gracefully and not include duplicates in result
        Assert.Equal(2, result.Count); // Should only have 2 unique nodes
    }

    private static TaskNode CreateTaskNode(Guid id, string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0,
                IsExecuting = false,
                Progress = 0.0
            }
        };
    }

    private static RouterNode CreateRouterNode(Guid id, Guid branchTargetId, Guid? parentId = null)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = $"Router_{id.ToString()[..4]}",
                StartTime = 0,
                Duration = 5.0,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", TargetNodeId = branchTargetId, Priority = 0 }
                }
            }
        };
    }

    #region Nested Router — HierarchicalSorter with allNodes overload

    [Fact]
    public void SortTaskNodesHierarchically_OnlyAcceptsTaskNodes_CannotSeeRouterBoundaries()
    {
        // With nested routers: Router1 → BT1 → Router2 → BT2
        // BT1.ParentId = Router1Id, BT2.ParentId = Router2Id
        // Using the allNodes overload, the sorter can traverse through RouterNode boundaries
        // to determine that BT2 is semantically deeper than BT1.

        var router1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();

        // Hierarchy: Router1 → BT1 → Router2 → BT2
        var router1 = CreateRouterNode(router1Id, bt1Id);
        var bt1 = CreateTaskNode(bt1Id, "BT1", router1Id);
        var router2 = CreateRouterNode(router2Id, bt2Id, bt1Id);
        var bt2 = CreateTaskNode(bt2Id, "BT2", router2Id);

        var taskNodes = new List<TaskNode> { bt1, bt2 };
        var allNodes = new List<Node> { router1, bt1, router2, bt2 };

        // Act — use the new overload with allNodes
        var result = _sorter.SortTaskNodesHierarchically(taskNodes, allNodes);

        // Assert — BT2 must come before BT1 (deeper in hierarchy must be processed first).
        var bt2Index = result.ToList().FindIndex(n => n.Id == bt2Id);
        var bt1Index = result.ToList().FindIndex(n => n.Id == bt1Id);

        bt2Index.Should().BeLessThan(bt1Index,
            "BT2 (inside nested Router2, which is inside BT1) must be processed before BT1");
    }

    [Fact]
    public void SortTaskNodesHierarchically_ThreeLevelNesting_AllRootsNoOrderingGuarantee()
    {
        // Three-level: Router1 → BT1 → Router2 → BT2 → Router3 → BT3
        // Required order: BT3, BT2, BT1 (deepest first)

        var router1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var router3Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();
        var bt3Id = Guid.NewGuid();

        var router1 = CreateRouterNode(router1Id, bt1Id);
        var bt1 = CreateTaskNode(bt1Id, "BT1", router1Id);
        var router2 = CreateRouterNode(router2Id, bt2Id, bt1Id);
        var bt2 = CreateTaskNode(bt2Id, "BT2", router2Id);
        var router3 = CreateRouterNode(router3Id, bt3Id, bt2Id);
        var bt3 = CreateTaskNode(bt3Id, "BT3", router3Id);

        // Deliberately put bt1 first to provoke wrong ordering
        var taskNodes = new List<TaskNode> { bt1, bt2, bt3 };
        var allNodes = new List<Node> { router1, bt1, router2, bt2, router3, bt3 };

        // Act — use the new overload with allNodes
        var result = _sorter.SortTaskNodesHierarchically(taskNodes, allNodes);

        // Assert — must produce correct order: BT3 < BT2 < BT1
        result.Should().HaveCount(3);
        var bt3Index = result.ToList().FindIndex(n => n.Id == bt3Id);
        var bt2Index = result.ToList().FindIndex(n => n.Id == bt2Id);
        var bt1Index = result.ToList().FindIndex(n => n.Id == bt1Id);

        bt3Index.Should().BeLessThan(bt2Index,
            "BT3 (deepest) must be processed before BT2");
        bt2Index.Should().BeLessThan(bt1Index,
            "BT2 must be processed before BT1 (shallowest)");
    }

    #endregion
}