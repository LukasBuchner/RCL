using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement;

/// <summary>
///     Verifies that query methods reading from the change tracker return complete entity hierarchies:
///     all node types (TaskNode, SkillExecutionNode, RouterNode), parent-child relationships,
///     and all dependency edges connecting nodes at any nesting level.
/// </summary>
public sealed class ChangeTrackerQueryConsistencyTests
{
    private readonly Guid _otherProcedureId = Guid.NewGuid();
    private readonly Guid _procedureId = Guid.NewGuid();

    #region Realistic Hierarchy Builder

    /// <summary>
    ///     Builds a realistic procedure hierarchy:
    ///
    ///     RouterNode (root)
    ///       ├─ TaskNode "BranchA" (child of router)
    ///       │    ├─ SkillExecutionNode "SkillA1" (child of BranchA)
    ///       │    └─ SkillExecutionNode "SkillA2" (child of BranchA)
    ///       └─ TaskNode "BranchB" (child of router)
    ///            └─ SkillExecutionNode "SkillB1" (child of BranchB)
    ///
    ///     SkillExecutionNode "Standalone" (root, no parent)
    ///
    ///     Edges:
    ///       SkillA1 → SkillA2  (sequential within branch A)
    ///       Router  → Standalone (router finishes then standalone runs)
    /// </summary>
    private (List<Node> Nodes, List<DependencyEdge> Edges) BuildHierarchy(Guid procedureId)
    {
        var agentId = Guid.NewGuid();
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties = []
        };

        var routerNodeId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();
        var skillA1Id = Guid.NewGuid();
        var skillA2Id = Guid.NewGuid();
        var skillB1Id = Guid.NewGuid();
        var standaloneId = Guid.NewGuid();

        var pos = new NodePosition { X = 0, Y = 0 };

        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = procedureId,
            Position = pos,
            RouterTask = new RouterTask
            {
                Name = "Decision",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "branch_var" },
                Branches =
                [
                    new ConditionalBranch
                    {
                        Name = "BranchA",
                        Condition = "value == 'A'",
                        Priority = 1,
                        TargetNodeId = branchAId
                    },
                    new ConditionalBranch
                    {
                        Name = "BranchB",
                        Condition = null,
                        Priority = 2,
                        TargetNodeId = branchBId
                    }
                ]
            }
        };

        var branchANode = new TaskNode
        {
            Id = branchAId,
            ProcedureId = procedureId,
            ParentId = routerNodeId,
            Position = pos,
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchA",
                StartTime = 0,
                Duration = 1.0
            }
        };

        var branchBNode = new TaskNode
        {
            Id = branchBId,
            ProcedureId = procedureId,
            ParentId = routerNodeId,
            Position = pos,
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchB",
                StartTime = 0,
                Duration = 1.0
            }
        };

        var skillA1 = new SkillExecutionNode
        {
            Id = skillA1Id,
            ProcedureId = procedureId,
            ParentId = branchAId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillA1",
                StartTime = 0,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var skillA2 = new SkillExecutionNode
        {
            Id = skillA2Id,
            ProcedureId = procedureId,
            ParentId = branchAId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillA2",
                StartTime = 0.5,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var skillB1 = new SkillExecutionNode
        {
            Id = skillB1Id,
            ProcedureId = procedureId,
            ParentId = branchBId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillB1",
                StartTime = 0,
                Duration = 1.0,
                Skill = skill,
                AgentId = agentId
            }
        };

        var standalone = new SkillExecutionNode
        {
            Id = standaloneId,
            ProcedureId = procedureId,
            Position = pos,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Standalone",
                StartTime = 1.0,
                Duration = 0.5,
                Skill = skill,
                AgentId = agentId
            }
        };

        var nodes = new List<Node> { routerNode, branchANode, branchBNode, skillA1, skillA2, skillB1, standalone };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = procedureId,
                SourceId = skillA1Id,
                TargetId = skillA2Id,
                SourceHandle = "right",
                TargetHandle = "left"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcedureId = procedureId,
                SourceId = routerNodeId,
                TargetId = standaloneId,
                SourceHandle = "right",
                TargetHandle = "left"
            }
        };

        return (nodes, edges);
    }

    #endregion

    #region Service Factories

    private (NodeApplicationService Service, Mock<INodeChangeTracker> Tracker) CreateNodeService(
        Guid? activeProcedureId)
    {
        var mockTracker = new Mock<INodeChangeTracker>();
        var nodesSubject = new BehaviorSubject<IReadOnlyList<Node>>(Array.Empty<Node>());
        mockTracker.Setup(x => x.Nodes).Returns(nodesSubject);

        var mockProcedureContext = new Mock<IProcedureContext>();
        mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(activeProcedureId);
        if (activeProcedureId.HasValue)
            mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(activeProcedureId.Value);
        mockProcedureContext.Setup(x => x.ProcedureChanges)
            .Returns(new BehaviorSubject<Guid?>(activeProcedureId));

        var config = Options.Create(new SchedulingConfiguration
        {
            Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
        });

        var service = new NodeApplicationService(
            new Mock<IProcedureRepository>().Object,
            new Mock<ICrudSchedulingOrchestrator>().Object,
            mockTracker.Object,
            mockProcedureContext.Object,
            new Mock<IProcedureVariableService>().Object,
            config,
            new Mock<ILogger<NodeApplicationService>>().Object);

        return (service, mockTracker);
    }

    private (DependencyEdgeApplicationService Service, Mock<IDependencyEdgeChangeTracker> Tracker)
        CreateEdgeService(Guid? activeProcedureId)
    {
        var mockTracker = new Mock<IDependencyEdgeChangeTracker>();
        var edgesSubject =
            new BehaviorSubject<IReadOnlyList<DependencyEdge>>(Array.Empty<DependencyEdge>());
        mockTracker.Setup(x => x.Edges).Returns(edgesSubject);

        var mockProcedureContext = new Mock<IProcedureContext>();
        mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(activeProcedureId);
        if (activeProcedureId.HasValue)
            mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(activeProcedureId.Value);
        mockProcedureContext.Setup(x => x.ProcedureChanges)
            .Returns(new BehaviorSubject<Guid?>(activeProcedureId));

        var service = new DependencyEdgeApplicationService(
            new Mock<ICrudSchedulingOrchestrator>().Object,
            mockTracker.Object,
            mockProcedureContext.Object,
            new Mock<ILogger<DependencyEdgeApplicationService>>().Object);

        return (service, mockTracker);
    }

    #endregion

    #region GetAllNodesAsync – Full Hierarchy

    [Fact]
    public async Task GetAllNodesAsync_ReturnsAllNodeTypes_RouterTaskAndSkillExecution()
    {
        // Arrange
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert
        result.Should().HaveCount(7);
        result.OfType<RouterNode>().Should().HaveCount(1);
        result.OfType<TaskNode>().Should().HaveCount(2);
        result.OfType<SkillExecutionNode>().Should().HaveCount(4);
    }

    [Fact]
    public async Task GetAllNodesAsync_PreservesParentChildRelationships()
    {
        // Arrange
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert – router has no parent
        var router = result.OfType<RouterNode>().Single();
        router.ParentId.Should().BeNull();

        // Assert – branch TaskNodes are children of the router
        var branchNodes = result.OfType<TaskNode>().ToList();
        branchNodes.Should().OnlyContain(n => n.ParentId == router.Id);

        // Assert – SkillA1 and SkillA2 are children of BranchA
        var branchA = branchNodes.Single(n => n.Task.Name == "BranchA");
        var branchAChildren = result.OfType<SkillExecutionNode>()
            .Where(n => n.ParentId == branchA.Id).ToList();
        branchAChildren.Should().HaveCount(2);
        branchAChildren.Select(n => n.SkillExecutionTask.Name)
            .Should().BeEquivalentTo("SkillA1", "SkillA2");

        // Assert – SkillB1 is child of BranchB
        var branchB = branchNodes.Single(n => n.Task.Name == "BranchB");
        var branchBChildren = result.OfType<SkillExecutionNode>()
            .Where(n => n.ParentId == branchB.Id).ToList();
        branchBChildren.Should().ContainSingle()
            .Which.SkillExecutionTask.Name.Should().Be("SkillB1");

        // Assert – standalone has no parent
        var standalone = result.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "Standalone");
        standalone.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task GetAllNodesAsync_FiltersOutOtherProcedureNodes_KeepsEntireHierarchy()
    {
        // Arrange – our procedure hierarchy + a stray node from another procedure
        var (nodes, _) = BuildHierarchy(_procedureId);
        var foreignNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _otherProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Foreign",
                StartTime = 0,
                Duration = 1
            }
        };
        var allNodes = nodes.Append(foreignNode).ToList();

        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(allNodes);

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert – all 7 hierarchy nodes returned, foreign node excluded
        result.Should().HaveCount(7);
        result.Should().NotContain(n => n.ProcedureId == _otherProcedureId);
    }

    #endregion

    #region GetNodeByIdAsync – All Types

    [Fact]
    public async Task GetNodeByIdAsync_FindsRouterNode()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var router = nodes.OfType<RouterNode>().Single();
        var result = await service.GetNodeByIdAsync(router.Id);

        result.Should().NotBeNull();
        result.Should().BeOfType<RouterNode>();
        ((RouterNode)result!).RouterTask.Branches.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNodeByIdAsync_FindsNestedSkillExecutionNode()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var skillA2 = nodes.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "SkillA2");

        var result = await service.GetNodeByIdAsync(skillA2.Id);

        result.Should().NotBeNull();
        result.Should().BeOfType<SkillExecutionNode>();
        var sn = (SkillExecutionNode)result!;
        sn.SkillExecutionTask.Name.Should().Be("SkillA2");
        sn.ParentId.Should().NotBeNull("SkillA2 is a child of BranchA TaskNode");
    }

    [Fact]
    public async Task GetNodeByIdAsync_ReturnsNullForNonExistentId()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var result = await service.GetNodeByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region GetNodesByParentIdAsync – Hierarchy Traversal

    [Fact]
    public async Task GetNodesByParentIdAsync_RouterChildren_ReturnsBothBranches()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var router = nodes.OfType<RouterNode>().Single();
        var result = await service.GetNodesByParentIdAsync(router.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.Should().BeOfType<TaskNode>());
        result.Should().OnlyContain(n => n.ParentId == router.Id);
    }

    [Fact]
    public async Task GetNodesByParentIdAsync_BranchAChildren_ReturnsTwoSkillNodes()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var branchA = nodes.OfType<TaskNode>().Single(n => n.Task.Name == "BranchA");
        var result = await service.GetNodesByParentIdAsync(branchA.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.Should().BeOfType<SkillExecutionNode>());
        result.OfType<SkillExecutionNode>().Select(n => n.SkillExecutionTask.Name)
            .Should().BeEquivalentTo("SkillA1", "SkillA2");
    }

    [Fact]
    public async Task GetNodesByParentIdAsync_BranchBChildren_ReturnsSingleSkillNode()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var branchB = nodes.OfType<TaskNode>().Single(n => n.Task.Name == "BranchB");
        var result = await service.GetNodesByParentIdAsync(branchB.Id);

        result.Should().ContainSingle()
            .Which.Should().BeOfType<SkillExecutionNode>();
    }

    [Fact]
    public async Task GetNodesByParentIdAsync_LeafNode_ReturnsEmpty()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var leaf = nodes.OfType<SkillExecutionNode>()
            .Single(n => n.SkillExecutionTask.Name == "SkillA1");

        var result = await service.GetNodesByParentIdAsync(leaf.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNodesByParentIdAsync_ExcludesChildrenFromOtherProcedure()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var router = nodes.OfType<RouterNode>().Single();

        // Foreign child claiming the same parent but different procedure
        var foreignChild = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _otherProcedureId,
            ParentId = router.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "ForeignChild",
                StartTime = 0,
                Duration = 1
            }
        };
        var allNodes = nodes.Append(foreignChild).ToList();

        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(allNodes);

        var result = await service.GetNodesByParentIdAsync(router.Id);

        result.Should().HaveCount(2, "only children from the active procedure should be returned");
        result.Should().NotContain(n => n.ProcedureId == _otherProcedureId);
    }

    #endregion

    #region GetAllDependencyEdgesAsync – Complete Edges

    [Fact]
    public async Task GetAllDependencyEdgesAsync_ReturnsAllEdgesIncludingNestedNodeEdges()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateEdgeService(_procedureId);
        tracker.Setup(t => t.GetCurrentEdges()).Returns(edges);

        var result = await service.GetAllDependencyEdgesAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.ProcedureId == _procedureId);
    }

    [Fact]
    public async Task GetAllDependencyEdgesAsync_EdgesConnectCorrectNodes()
    {
        var (nodes, edges) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateEdgeService(_procedureId);
        tracker.Setup(t => t.GetCurrentEdges()).Returns(edges);

        var result = await service.GetAllDependencyEdgesAsync();

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();

        // Every edge's source and target must reference a node in the hierarchy
        result.Should().AllSatisfy(e =>
        {
            nodeIds.Should().Contain(e.SourceId, "edge source must reference an existing node");
            nodeIds.Should().Contain(e.TargetId, "edge target must reference an existing node");
        });
    }

    [Fact]
    public async Task GetAllDependencyEdgesAsync_FiltersOutOtherProcedureEdges()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        var foreignEdge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = _otherProcedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        var allEdges = edges.Append(foreignEdge).ToList();

        var (service, tracker) = CreateEdgeService(_procedureId);
        tracker.Setup(t => t.GetCurrentEdges()).Returns(allEdges);

        var result = await service.GetAllDependencyEdgesAsync();

        result.Should().HaveCount(2);
        result.Should().NotContain(e => e.ProcedureId == _otherProcedureId);
    }

    [Fact]
    public async Task GetDependencyEdgeByIdAsync_FindsEdgeBetweenNestedNodes()
    {
        var (_, edges) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateEdgeService(_procedureId);
        tracker.Setup(t => t.GetCurrentEdges()).Returns(edges);

        var edgeBetweenSkills = edges[0]; // SkillA1 → SkillA2

        var result = await service.GetDependencyEdgeByIdAsync(edgeBetweenSkills.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(edgeBetweenSkills.Id);
    }

    #endregion

    #region Nodes + Edges Cross-Consistency

    [Fact]
    public async Task NodesAndEdges_AllEdgeEndpointsExistInNodeResults()
    {
        var (nodes, edges) = BuildHierarchy(_procedureId);

        var (nodeService, nodeTracker) = CreateNodeService(_procedureId);
        nodeTracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var (edgeService, edgeTracker) = CreateEdgeService(_procedureId);
        edgeTracker.Setup(t => t.GetCurrentEdges()).Returns(edges);

        // Act
        var allNodes = await nodeService.GetAllNodesAsync();
        var allEdges = await edgeService.GetAllDependencyEdgesAsync();

        // Assert
        var nodeIds = allNodes.Select(n => n.Id).ToHashSet();

        allEdges.Should().AllSatisfy(edge =>
        {
            nodeIds.Should().Contain(edge.SourceId,
                $"edge {edge.Id} source {edge.SourceId} must be present in the node results");
            nodeIds.Should().Contain(edge.TargetId,
                $"edge {edge.Id} target {edge.TargetId} must be present in the node results");
        });
    }

    [Fact]
    public async Task NodesAndEdges_ParentIdsReferenceExistingNodes()
    {
        var (nodes, _) = BuildHierarchy(_procedureId);
        var (service, tracker) = CreateNodeService(_procedureId);
        tracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        var allNodes = await service.GetAllNodesAsync();
        var nodeIds = allNodes.Select(n => n.Id).ToHashSet();

        var nodesWithParent = allNodes.Where(n => n.ParentId.HasValue).ToList();
        nodesWithParent.Should().NotBeEmpty("the hierarchy has nested nodes");

        nodesWithParent.Should().AllSatisfy(node =>
        {
            nodeIds.Should().Contain(node.ParentId!.Value,
                $"node {node.Id} parent {node.ParentId} must be present in the node results");
        });
    }

    #endregion
}