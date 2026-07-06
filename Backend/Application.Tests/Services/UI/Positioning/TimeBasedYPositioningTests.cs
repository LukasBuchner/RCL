using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Options;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Positioning;

/// <summary>
///     Tests for time-based Y-positioning functionality.
///     Tests time-based sorting of root nodes by AbsoluteStartTime and siblings by RelativeStartTime.
/// </summary>
public class TimeBasedYPositioningTests
{
    private readonly IOptions<SchedulingConfiguration> _defaultOptions = Options.Create(new SchedulingConfiguration
    {
        Positioning = new PositioningConfiguration
        {
            BaseYOffset = 50.0,
            SiblingSpacing = 60.0,
            ContainerTopPadding = 30.0,
            ContainerBottomPadding = 10.0,
            BaseHeight = 50.0
        }
    });

    /// <summary>
    ///     Test 1: Root nodes sorted by AbsoluteStartTime.
    ///     Given three root nodes with different start times, they are positioned in time order.
    /// </summary>
    [Fact]
    public void CalculateYPositions_ThreeRootNodesWithDifferentStartTimes_SortsRootNodesByAbsoluteStartTime()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        // Create root nodes with different absolute start times
        // Note: Creating nodes out of time order to verify sorting
        var nodeA = CreateSkillExecutionNode("NodeA");
        var nodeB = CreateSkillExecutionNode("NodeB");
        var nodeC = CreateSkillExecutionNode("NodeC");

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { nodeA.Id, CreateNodeTimingInfo(nodeA.Id, 100.0, 50.0) },
            { nodeB.Id, CreateNodeTimingInfo(nodeB.Id, 50.0, 50.0) },
            { nodeC.Id, CreateNodeTimingInfo(nodeC.Id, 75.0, 50.0) }
        };

        var allNodes = new List<Node> { nodeA, nodeB, nodeC };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy, timingInfo);

        // Assert
        // Expected order: B (t=50), C (t=75), A (t=100)
        // B should be at Y=50 (BaseYOffset)
        // C should be at Y=160 (50 + BaseHeight + SiblingSpacing = 50 + 50 + 60)
        // A should be at Y=270 (160 + BaseHeight + SiblingSpacing = 160 + 50 + 60)
        Assert.Equal(50.0, result[nodeB.Id]);
        Assert.Equal(160.0, result[nodeC.Id]);
        Assert.Equal(270.0, result[nodeA.Id]);

        // Verify ordering: B before C before A
        Assert.True(result[nodeB.Id] < result[nodeC.Id]);
        Assert.True(result[nodeC.Id] < result[nodeA.Id]);
    }

    /// <summary>
    ///     Test 2: Sibling nodes sorted by RelativeStartTime.
    ///     Given children with different relative start times, they are positioned in time order.
    /// </summary>
    [Fact]
    public void CalculateYPositions_ChildrenWithDifferentRelativeStartTimes_SortsByRelativeStartTime()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        // Create parent container
        var parent = CreateTaskNode("Parent");

        // Create children with different relative start times
        // Note: Creating children out of time order to verify sorting
        var childA = CreateSkillExecutionNode("ChildA", parentId: parent.Id);
        var childB = CreateSkillExecutionNode("ChildB", parentId: parent.Id);
        var childC = CreateSkillExecutionNode("ChildC", parentId: parent.Id);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { parent.Id, CreateNodeTimingInfo(parent.Id, 0.0, relativeStart: 0.0, duration: 100.0) },
            { childA.Id, CreateNodeTimingInfo(childA.Id, 20.0, relativeStart: 20.0, duration: 10.0) },
            { childB.Id, CreateNodeTimingInfo(childB.Id, 10.0, relativeStart: 10.0, duration: 10.0) },
            { childC.Id, CreateNodeTimingInfo(childC.Id, 15.0, relativeStart: 15.0, duration: 10.0) }
        };

        var allNodes = new List<Node> { parent, childA, childB, childC };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { parent.Id, new List<Node> { childA, childB, childC } }
        };

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy, timingInfo);

        // Assert
        // Expected order within parent: B (rel t=10), C (rel t=15), A (rel t=20)
        // All positions are relative to parent
        // B should be at ContainerTopPadding = 30
        // C should be at 30 + 50 + 60 = 140
        // A should be at 140 + 50 + 60 = 250
        Assert.Equal(30.0, result[childB.Id]);
        Assert.Equal(140.0, result[childC.Id]);
        Assert.Equal(250.0, result[childA.Id]);

        // Verify ordering: B before C before A
        Assert.True(result[childB.Id] < result[childC.Id]);
        Assert.True(result[childC.Id] < result[childA.Id]);
    }

    /// <summary>
    ///     Test 3: Complex hierarchy with timing preserves structure while sorting by time.
    ///     Given multiple root nodes with children, verifies hierarchical structure is maintained and timing-based sorting
    ///     works at all levels.
    /// </summary>
    [Fact]
    public void CalculateYPositions_ComplexHierarchyWithTiming_PreservesStructureAndSortsByTime()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        // Create three root nodes with different start times
        var rootB = CreateTaskNode("RootB");
        var rootC = CreateTaskNode("RootC");
        var rootA = CreateTaskNode("RootA");

        // Create children for rootB with different relative start times
        var childB2 = CreateSkillExecutionNode("ChildB2", parentId: rootB.Id);
        var childB1 = CreateSkillExecutionNode("ChildB1", parentId: rootB.Id);

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            // Root B starts at t=50 with duration 100
            { rootB.Id, CreateNodeTimingInfo(rootB.Id, 50.0, relativeStart: 50.0, duration: 100.0) },
            // Root C starts at t=75 (no children)
            { rootC.Id, CreateNodeTimingInfo(rootC.Id, 75.0, relativeStart: 75.0, duration: 50.0) },
            // Root A starts at t=100 (no children)
            { rootA.Id, CreateNodeTimingInfo(rootA.Id, 100.0, relativeStart: 100.0, duration: 50.0) },
            // Children of Root B: B2 has rel t=10, B1 has rel t=20
            { childB2.Id, CreateNodeTimingInfo(childB2.Id, 60.0, relativeStart: 10.0, duration: 10.0) },
            { childB1.Id, CreateNodeTimingInfo(childB1.Id, 70.0, relativeStart: 20.0, duration: 10.0) }
        };

        var allNodes = new List<Node> { rootB, rootC, rootA, childB2, childB1 };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { rootB.Id, new List<Node> { childB2, childB1 } }
        };

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy, timingInfo);

        // Assert
        // Expected root order: B (t=50), C (t=75), A (t=100)
        // Root B at Y=50
        Assert.Equal(50.0, result[rootB.Id]);

        // Within Root B, children sorted by relative time: B2 (rel t=10), B1 (rel t=20)
        Assert.Equal(30.0, result[childB2.Id]); // ContainerTopPadding
        Assert.Equal(140.0, result[childB1.Id]); // 30 + 50 + 60

        // Verify child ordering
        Assert.True(result[childB2.Id] < result[childB1.Id]);

        // Root B has 2 children, so its height = 30 + 50 + 60 + 50 + 10 = 200
        // Root C should start at 50 + 200 + 60 = 310
        Assert.Equal(310.0, result[rootC.Id]);

        // Root C is a leaf, height = 50
        // Root A should start at 310 + 50 + 60 = 420
        Assert.Equal(420.0, result[rootA.Id]);

        // Verify root ordering
        Assert.True(result[rootB.Id] < result[rootC.Id]);
        Assert.True(result[rootC.Id] < result[rootA.Id]);
    }

    /// <summary>
    ///     Test 4: Graceful fallback when timing information is missing.
    ///     When no timing info is provided, nodes are sorted by ID for deterministic ordering.
    /// </summary>
    [Fact]
    public void CalculateYPositions_NoTimingInfo_FallsBackToIdBasedSorting()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        // Create nodes with GUIDs that have a specific order
        var nodeA = CreateSkillExecutionNode("NodeA");
        var nodeB = CreateSkillExecutionNode("NodeB");
        var nodeC = CreateSkillExecutionNode("NodeC");

        // Sort nodes by ID to get expected order
        var sortedNodes = new[] { nodeA, nodeB, nodeC }.OrderBy(n => n.Id).ToList();

        var allNodes = new List<Node> { nodeA, nodeB, nodeC };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act - no timing info provided
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy);

        // Assert
        // Nodes should be sorted by ID
        var firstNode = sortedNodes[0];
        var secondNode = sortedNodes[1];
        var thirdNode = sortedNodes[2];

        Assert.Equal(50.0, result[firstNode.Id]); // BaseYOffset
        Assert.Equal(160.0, result[secondNode.Id]); // 50 + 50 + 60 = 160
        Assert.Equal(270.0, result[thirdNode.Id]); // 160 + 50 + 60 = 270

        // Verify ordering matches ID sort
        Assert.True(result[firstNode.Id] < result[secondNode.Id]);
        Assert.True(result[secondNode.Id] < result[thirdNode.Id]);
    }

    /// <summary>
    ///     Test 5: Partial timing information uses fallback for missing nodes.
    ///     When timing info is available for some nodes but not others, missing nodes fall back to ID-based ordering.
    /// </summary>
    [Fact]
    public void CalculateYPositions_PartialTimingInfo_UsesFallbackForMissingNodes()
    {
        // Arrange
        var calculator = new NodePositionYCalculator(_defaultOptions);

        var nodeA = CreateSkillExecutionNode("NodeA");
        var nodeB = CreateSkillExecutionNode("NodeB");
        var nodeC = CreateSkillExecutionNode("NodeC");

        // Only provide timing for nodeA and nodeC
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { nodeA.Id, CreateNodeTimingInfo(nodeA.Id, 50.0, 50.0) },
            { nodeC.Id, CreateNodeTimingInfo(nodeC.Id, 100.0, 50.0) }
            // nodeB has no timing info
        };

        var allNodes = new List<Node> { nodeA, nodeB, nodeC };
        var nodeHierarchy = new Dictionary<Guid, IReadOnlyList<Node>>();

        // Act
        var result = calculator.CalculateYPositions(allNodes, nodeHierarchy, timingInfo);

        // Assert
        // Nodes with timing should come first (A at t=50, C at t=100)
        // Node B without timing should come last (using double.MaxValue fallback)
        Assert.Equal(50.0, result[nodeA.Id]); // First with timing
        Assert.Equal(160.0, result[nodeC.Id]); // Second with timing (50 + 50 + 60 = 160)
        Assert.Equal(270.0, result[nodeB.Id]); // Last (no timing = MaxValue) (160 + 50 + 60 = 270)
    }

    /// <summary>
    ///     Creates a helper method to build NodeTimingInfo objects.
    /// </summary>
    private NodeTimingInfo CreateNodeTimingInfo(
        Guid nodeId,
        double absoluteStart,
        double duration,
        double? relativeStart = null)
    {
        var relStart = relativeStart ?? absoluteStart;
        return new NodeTimingInfo
        {
            NodeId = nodeId,
            Duration = duration,
            AbsoluteStartTime = absoluteStart,
            AbsoluteFinishTime = absoluteStart + duration,
            RelativeStartTime = relStart,
            RelativeFinishTime = relStart + duration,
            NodeType = NodeTimingType.SkillExecution,
            IsCalculated = true,
            OnCriticalPath = false
        };
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
}