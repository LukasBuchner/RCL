using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Node = FHOOE.Freydis.Domain.Entities.Procedure.Node;
using TaskNode = FHOOE.Freydis.Domain.Entities.Procedure.TaskNode;
using NodePosition = FHOOE.Freydis.Domain.Entities.Procedure.NodePosition;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Integration tests to verify React Flow compatibility with proper container heights and relative positioning.
///     These tests ensure that parent containers have sufficient height to contain their children.
/// </summary>
public class ReactFlowIntegrationTests
{
    private readonly TimingCalculationOrchestrator _schedulingPipeline;

    public ReactFlowIntegrationTests()
    {
        // Create mocks for all dependencies
        var mockNodeHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        var mockTimingCalculationEngine = new Mock<ITimingCalculationEngine>();
        var mockDurationProviderFactory = new Mock<IDurationProviderFactory>();
        var mockNodePositioningService = new Mock<INodePositioningService>();
        var mockTimingAnalyzer = new Mock<ITimingAnalyzer>();
        var mockSchedulingPhaseLogger = new Mock<ISchedulingPhaseLogger>();
        var mockLogger = new Mock<ILogger<TimingCalculationOrchestrator>>();

        var scheduleResultConverter = new ScheduleResultConverter(new Mock<ILogger<ScheduleResultConverter>>().Object);

        // Setup mock duration provider factory to return a planning provider
        var mockPlanningProvider = new Mock<ISkillDurationProvider>();
        // Note: We don't need to setup AnalyzeAsync for these tests as it's not called directly in constructor

        mockDurationProviderFactory
            .Setup(f => f.CreateDurationProvider(It.IsAny<bool>(), It.IsAny<DateTime?>(),
                It.IsAny<IReadOnlyDictionary<Guid, SkillExecutionProgress>?>()))
            .Returns(mockPlanningProvider.Object);

        // Setup NodePositioningService to return input nodes unchanged
        mockNodePositioningService
            .Setup(s => s.ApplyPositionsAndHeights(It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns((IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, NodeTimingInfo> _,
                IReadOnlyDictionary<Guid, IReadOnlyList<Node>> _) => nodes);

        // Setup TimingAnalyzer with default values
        mockTimingAnalyzer
            .Setup(a => a.CollectStatistics(It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>()))
            .Returns(new TimingStatistics
            {
                MinDuration = 0,
                MaxDuration = 0,
                AverageDuration = 0,
                SumDuration = 0,
                NodeCount = 0,
                EarliestStart = 0,
                LatestFinish = 0,
                TotalProcedureSpan = 0
            });

        mockTimingAnalyzer
            .Setup(a => a.AnalyzeCriticalPath(It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyList<Node>>()))
            .Returns(new CriticalPathInfo { CriticalPathNodeIds = [], MaxParallelism = 1, PeakParallelismTime = 0 });

        _schedulingPipeline = new TimingCalculationOrchestrator(
            mockNodeHierarchyProcessor.Object,
            mockTimingCalculationEngine.Object,
            scheduleResultConverter,
            mockDurationProviderFactory.Object,
            mockNodePositioningService.Object,
            mockTimingAnalyzer.Object,
            mockSchedulingPhaseLogger.Object,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            mockLogger.Object);
    }

    [Fact]
    public void ParentContainerWithChild_ShouldHaveSufficientHeight()
    {
        // Arrange - Create parent and child nodes like in the failing scenario
        var parentId = Guid.Parse("ae3f4432-52bd-4dbb-819f-06959d491917");
        var childId = Guid.Parse("b2798799-ecb1-4aa4-9092-ddd1d8e6fd90");

        var parentNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Height = 50, // Initial incorrect height
            Task = new Task
            {
                Name = "45",
                Description = "",
                StartTime = 0,
                Duration = 200
            },
            ParentId = null
        };

        var childNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = childId,
            Position = new NodePosition { X = 50, Y = 100 }, // Child positioned at Y=100
            Height = 50,
            Task = new Task
            {
                Name = "sdf",
                Description = "",
                StartTime = 0,
                Duration = 200
            },
            ParentId = parentId
        };

        var nodes = new List<Node> { parentNode, childNode };

        // Act - This should calculate and apply correct heights
        var result = ProcessNodesWithHeightCalculation(nodes);

        // Assert - Parent should have sufficient height to contain child
        var updatedParent = result.First(n => n.Id == parentId);
        var updatedChild = result.First(n => n.Id == childId);

        // Parent container should have height to contain child positioned at Y=30 (relative) with height 50
        // Expected parent height: ContainerTopPadding (30) + BaseHeight (50) + ContainerBottomPadding (10) = 90
        Assert.True(updatedParent.Height >= 90,
            $"Parent height {updatedParent.Height} should be at least 90 to contain child at relative Y=30 with height=50");

        // Child should be positioned relative to parent (React Flow style)
        Assert.Equal(30.0, updatedChild.Position.Y); // ContainerTopPadding
    }

    [Fact]
    public void ParentWithTwoChildren_ShouldHaveSufficientHeight()
    {
        // Arrange - Create parent with two children 
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var parentNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = parentId,
            Position = new NodePosition { X = 0, Y = 50 },
            Height = 50, // Initial incorrect height
            Task = new Task { Name = "Parent", StartTime = 0, Duration = 200 },
            ParentId = null
        };

        var child1Node = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = child1Id,
            Position = new NodePosition { X = 20, Y = 30 }, // Will be positioned relatively
            Height = 50,
            Task = new Task { Name = "Child1", StartTime = 0, Duration = 100 },
            ParentId = parentId
        };

        var child2Node = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = child2Id,
            Position = new NodePosition { X = 20, Y = 140 }, // Will be positioned relatively  
            Height = 50,
            Task = new Task { Name = "Child2", StartTime = 0, Duration = 100 },
            ParentId = parentId
        };

        var nodes = new List<Node> { parentNode, child1Node, child2Node };

        // Act
        var result = ProcessNodesWithHeightCalculation(nodes);

        // Assert
        var updatedParent = result.First(n => n.Id == parentId);
        var updatedChild1 = result.First(n => n.Id == child1Id);
        var updatedChild2 = result.First(n => n.Id == child2Id);

        // Parent should have height to contain both children
        // Child1 at Y=30, Child2 at Y=140 (relative), each with height=50
        // Required height: ContainerTopPadding (30) + Child2 bottom (140+50) + ContainerBottomPadding (10) = 200
        Assert.True(updatedParent.Height >= 200,
            $"Parent height {updatedParent.Height} should be at least 200 to contain both children");

        // Children should be positioned relative to parent
        Assert.Equal(30.0, updatedChild1.Position.Y); // ContainerTopPadding
        Assert.Equal(140.0,
            updatedChild2.Position.Y); // ContainerTopPadding + BaseHeight + SiblingSpacing = 30 + 50 + 60
    }

    [Fact]
    public void NestedContainers_ShouldHaveCorrectHeights()
    {
        // Arrange - Three level hierarchy: Root -> Mid -> Leaf
        var rootId = Guid.NewGuid();
        var midId = Guid.NewGuid();
        var leafId = Guid.NewGuid();

        var rootNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = rootId,
            Position = new NodePosition { X = 0, Y = 50 },
            Height = 50, // Initial incorrect height
            Task = new Task { Name = "Root", StartTime = 0, Duration = 200 },
            ParentId = null
        };

        var midNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = midId,
            Position = new NodePosition { X = 20, Y = 30 },
            Height = 50, // Initial incorrect height  
            Task = new Task { Name = "Mid", StartTime = 0, Duration = 100 },
            ParentId = rootId
        };

        var leafNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = leafId,
            Position = new NodePosition { X = 40, Y = 30 },
            Height = 50,
            Task = new Task { Name = "Leaf", StartTime = 0, Duration = 50 },
            ParentId = midId
        };

        var nodes = new List<Node> { rootNode, midNode, leafNode };

        // Act
        var result = ProcessNodesWithHeightCalculation(nodes);

        // Assert
        var updatedRoot = result.First(n => n.Id == rootId);
        var updatedMid = result.First(n => n.Id == midId);
        var updatedLeaf = result.First(n => n.Id == leafId);

        // Leaf node should have base height
        Assert.Equal(50.0, updatedLeaf.Height);

        // Mid container should have height to contain leaf: 30 + 50 + 10 = 90
        Assert.True(updatedMid.Height >= 90,
            $"Mid container height {updatedMid.Height} should be at least 90 to contain leaf node");

        // Root container should have height to contain mid: 30 + 90 + 10 = 130
        Assert.True(updatedRoot.Height >= 130,
            $"Root container height {updatedRoot.Height} should be at least 130 to contain mid container");
    }

    /// <summary>
    ///     Simulates the height calculation logic from TimingCalculationOrchestrator.CalculateNodePositions
    /// </summary>
    private List<Node> ProcessNodesWithHeightCalculation(List<Node> nodes)
    {
        // Create hierarchy mapping like the scheduling pipeline does (using Guid.Empty for null parents)
        // We'll use a simple approach that mimics how NodePositionYCalculator.CalculateYPositions works
        var parentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>();
        var lookup = nodes.ToLookup(n => n.ParentId ?? Guid.Empty);

        foreach (var group in lookup) parentToChildrenMapping[group.Key] = group.ToList().AsReadOnly();

        // Create Y calculator
        var schedulingOptions = Options.Create(new SchedulingConfiguration
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
        var yCalculator = new NodePositionYCalculator(schedulingOptions);

        // Generate timing info to ensure deterministic sort order (creation order)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>();
        for (var i = 0; i < nodes.Count; i++)
            timingInfo[nodes[i].Id] = new NodeTimingInfo
            {
                NodeId = nodes[i].Id,
                AbsoluteStartTime = i * 0.1,
                RelativeStartTime = i * 0.1
            };

        // Calculate Y positions (React Flow relative positioning)
        var yPositions = yCalculator.CalculateYPositions(nodes, parentToChildrenMapping, timingInfo);

        // Calculate heights
        var nodeHeights = CalculateNodeHeights(nodes, parentToChildrenMapping, yCalculator);

        // Apply positions and heights to nodes (like TimingCalculationOrchestrator does)
        var updatedNodes = new List<Node>();
        foreach (var node in nodes)
        {
            var hasYPosition = yPositions.TryGetValue(node.Id, out var yPos);
            var hasHeight = nodeHeights.TryGetValue(node.Id, out var nodeHeight);

            var newPosition = node.Position;
            if (hasYPosition) newPosition = node.Position with { Y = yPos };

            var updatedNode = node switch
            {
                TaskNode taskNode => taskNode with
                {
                    Position = newPosition,
                    Height = hasHeight ? nodeHeight : taskNode.Height
                },
                _ => node
            };

            updatedNodes.Add(updatedNode);
        }

        return updatedNodes;
    }

    /// <summary>
    ///     Replicates the CalculateNodeHeights logic from TimingCalculationOrchestrator
    /// </summary>
    private Dictionary<Guid, double> CalculateNodeHeights(
        List<Node> nodes,
        Dictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping,
        NodePositionYCalculator yCalculator)
    {
        var nodeHeights = new Dictionary<Guid, double>();

        // parentToChildrenMapping already uses Guid keys
        var typedHierarchy = parentToChildrenMapping;

        foreach (var node in nodes)
            if (typedHierarchy.TryGetValue(node.Id, out var children) && children.Any())
            {
                // Container node - calculate height based on children
                var containerHeight = yCalculator.CalculateContainerHeight(node, children, typedHierarchy);
                nodeHeights[node.Id] = containerHeight;
            }
            else
            {
                // Leaf node - use base height
                nodeHeights[node.Id] = yCalculator.BaseHeight;
            }

        return nodeHeights;
    }
}