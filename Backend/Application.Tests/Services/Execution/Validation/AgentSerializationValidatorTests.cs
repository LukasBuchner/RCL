using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Validation;

/// <summary>
///     Unit tests for <see cref="AgentSerializationValidator" />.
///     Each test covers one scenario from the specification table, verifying that the validator
///     correctly identifies when agent-assigned skills lack a transitive FS dependency chain and
///     when they are correctly serialized or mutually excluded by router branches.
/// </summary>
public sealed class AgentSerializationValidatorTests
{
    private readonly Mock<INodeHierarchyProcessor> _mockHierarchyProcessor;
    private readonly Mock<INodeResolver> _mockNodeResolver;
    private readonly Mock<IAgentNameResolver> _mockAgentNameResolver;
    private readonly AgentSerializationValidator _validator;

    /// <summary>
    ///     Initializes a new instance of the test class, wiring up mocks and the system under test.
    ///     The <see cref="IAgentNameResolver" /> mock is configured to return <c>"Test Agent"</c> for
    ///     any agent ID by default, keeping all existing tests independent of resolver setup.
    /// </summary>
    public AgentSerializationValidatorTests()
    {
        _mockHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        _mockNodeResolver = new Mock<INodeResolver>();
        _mockAgentNameResolver = new Mock<IAgentNameResolver>();
        _mockAgentNameResolver.Setup(r => r.Resolve(It.IsAny<Guid>())).Returns("Test Agent");

        // The validator resolves edge endpoints via ResolveToFiringEndpointsIds. Mirror the real wrapper over
        // whatever each test sets up for ResolveToExecutableIds: the executable leaves when non-empty,
        // otherwise the node itself (a leafless container is its own firing endpoint).
        _mockNodeResolver
            .Setup(r => r.ResolveToFiringEndpointsIds(It.IsAny<Guid>(), It.IsAny<NodeHierarchyInfo>()))
            .Returns((Guid id, NodeHierarchyInfo h) =>
            {
                var leaves = _mockNodeResolver.Object.ResolveToExecutableIds(id, h)?.ToList()
                             ?? new List<Guid>();
                return leaves.Count > 0 ? leaves : new List<Guid> { id };
            });

        _validator = new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            _mockNodeResolver.Object,
            _mockAgentNameResolver.Object,
            NullLogger<AgentSerializationValidator>.Instance);
    }

    // -------------------------------------------------------------------------
    // Test cases 1–18: Validator scenario matrix
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Test 1: A procedure with a single skill assigned to one agent has no pairs to check,
    ///     so the validator must return an empty violation list.
    /// </summary>
    [Fact]
    public void SingleAgent_SingleSkill_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var nodes = new List<Node> { skillA };

        SetupHierarchy(nodes, [skillA], [], []);
        SetupResolverForSkills([skillA]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 2: Two skills assigned to different agents with no edges between them.
    ///     Because the agents are distinct the serialization constraint does not apply across agents,
    ///     so no violation is raised.
    /// </summary>
    [Fact]
    public void DifferentAgents_NoFsEdge_ReturnsNoViolations()
    {
        // Arrange
        var agentX = Guid.NewGuid();
        var agentY = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentX);
        var skillB = CreateSkillNode("B", agentY);
        var nodes = new List<Node> { skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 3: Two skills on the same agent are connected by a direct FS edge (A ->FS-> B).
    ///     A can reach B via BFS, so the pair is valid and no violation is raised.
    /// </summary>
    [Fact]
    public void SameAgent_DirectFsEdge_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };
        var fsEdge = CreateFsEdge(skillA.Id, skillB.Id);

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, [fsEdge]);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 4: Three skills on the same agent form an FS chain A ->FS-> M ->FS-> B.
    ///     A can reach B transitively, so no violation is raised.
    /// </summary>
    [Fact]
    public void SameAgent_TransitiveFsChain_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillM = CreateSkillNode("M", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillM, skillB };
        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(skillA.Id, skillM.Id),
            CreateFsEdge(skillM.Id, skillB.Id)
        };

        SetupHierarchy(nodes, [skillA, skillM, skillB], [], []);
        SetupResolverForSkills([skillA, skillM, skillB]);

        // Act
        var violations = _validator.Validate(nodes, edges);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 5: Two skills on the same agent with no edges at all.
    ///     Neither skill can reach the other, so one violation is raised containing the pair (A, B).
    /// </summary>
    [Fact]
    public void SameAgent_NoFsEdge_ReturnsViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        var violation = Assert.Single(violations);
        violation.AgentId.Should().Be(agentId);
        var pair = Assert.Single(violation.MissingFsPairs);
        pair.SkillA.Should().Be(skillA.Id);
        pair.SkillB.Should().Be(skillB.Id);
    }

    /// <summary>
    ///     Test 6: Two skills on the same agent connected only by a Start-to-Start edge (A ->SS-> B).
    ///     SS is not FS, so no FS reachability exists and one violation is raised.
    /// </summary>
    [Fact]
    public void SameAgent_SsEdgeOnly_ReturnsViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };
        var ssEdge = CreateSsEdge(skillA.Id, skillB.Id);

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, [ssEdge]);

        // Assert
        Assert.Single(violations);
        violations[0].AgentId.Should().Be(agentId);
        violations[0].MissingFsPairs.Should().HaveCount(1);
    }

    /// <summary>
    ///     Test 7: Two skills on the same agent connected only by a Finish-to-Finish edge (A ->FF-> B).
    ///     FF is not FS, so no FS reachability exists and one violation is raised.
    /// </summary>
    [Fact]
    public void SameAgent_FfEdgeOnly_ReturnsViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };
        var ffEdge = CreateFfEdge(skillA.Id, skillB.Id);

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, [ffEdge]);

        // Assert
        Assert.Single(violations);
        violations[0].AgentId.Should().Be(agentId);
        violations[0].MissingFsPairs.Should().HaveCount(1);
    }

    /// <summary>
    ///     Test 8: Two skills on the same agent connected only by a Start-to-Finish edge (A ->SF-> B).
    ///     SF is not FS, so no FS reachability exists and one violation is raised.
    /// </summary>
    [Fact]
    public void SameAgent_SfEdgeOnly_ReturnsViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };
        var sfEdge = CreateSfEdge(skillA.Id, skillB.Id);

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, [sfEdge]);

        // Assert
        Assert.Single(violations);
        violations[0].AgentId.Should().Be(agentId);
        violations[0].MissingFsPairs.Should().HaveCount(1);
    }

    /// <summary>
    ///     Test 9: Two skills on the same agent reside in mutually exclusive branches of a router.
    ///     Because only one branch executes per run the pair cannot be concurrent, so no violation
    ///     is raised even without an FS edge between them.
    /// </summary>
    [Fact]
    public void MutuallyExclusiveBranches_SameAgent_NoViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();

        // Each skill is a direct child of the router and sits in its own branch.
        var skillA = CreateSkillNode("A", agentId, routerId);
        var skillB = CreateSkillNode("B", agentId, routerId);

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Branch1", Priority = 0, TargetNodeId = skillA.Id },
            new ConditionalBranch { Name = "Branch2", Priority = 1, TargetNodeId = skillB.Id }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node> { router, skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], [router]);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 10: Two skills on the same agent sit in the same router branch (same TargetNodeId path)
    ///     with no FS edge between them.  Because they are co-reachable the validator must report one
    ///     violation.
    /// </summary>
    [Fact]
    public void SameBranch_SameAgent_NoFsEdge_ReturnsViolation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchRootId = Guid.NewGuid();

        // Both skills share the same branch-root parent, which is the branch target of the router.
        var skillA = CreateSkillNode("A", agentId, branchRootId);
        var skillB = CreateSkillNode("B", agentId, branchRootId);

        var branchRoot = CreateTaskNode("BranchRoot", routerId);
        branchRoot = branchRoot with { Id = branchRootId };

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Branch1", Priority = 0, TargetNodeId = branchRootId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node> { router, branchRoot, skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [branchRoot], [router]);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Single(violations);
        violations[0].AgentId.Should().Be(agentId);
        violations[0].MissingFsPairs.Should().HaveCount(1);
    }

    /// <summary>
    ///     Test 11: Three skills on the same agent form the complete FS chain A ->FS-> B ->FS-> C.
    ///     Every pair (A,B), (A,C), (B,C) has FS reachability, so no violation is raised.
    /// </summary>
    [Fact]
    public void ThreeSkills_FullFsChain_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var skillC = CreateSkillNode("C", agentId);
        var nodes = new List<Node> { skillA, skillB, skillC };
        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(skillA.Id, skillB.Id),
            CreateFsEdge(skillB.Id, skillC.Id)
        };

        SetupHierarchy(nodes, [skillA, skillB, skillC], [], []);
        SetupResolverForSkills([skillA, skillB, skillC]);

        // Act
        var violations = _validator.Validate(nodes, edges);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 12: Three skills on the same agent where A ->FS-> B but C is not connected.
    ///     The pairs (A,C) and (B,C) both lack FS reachability, so one violation is raised
    ///     containing two missing pairs.
    /// </summary>
    [Fact]
    public void ThreeSkills_BrokenChain_ReturnsOneViolationWithTwoPairs()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var skillC = CreateSkillNode("C", agentId);
        var nodes = new List<Node> { skillA, skillB, skillC };
        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(skillA.Id, skillB.Id)
            // C is intentionally unconnected
        };

        SetupHierarchy(nodes, [skillA, skillB, skillC], [], []);
        SetupResolverForSkills([skillA, skillB, skillC]);

        // Act
        var violations = _validator.Validate(nodes, edges);

        // Assert
        var violation = Assert.Single(violations);
        violation.AgentId.Should().Be(agentId);
        violation.MissingFsPairs.Should().HaveCount(2);
        var pairIds = violation.MissingFsPairs
            .SelectMany(p => new[] { p.SkillA, p.SkillB })
            .ToHashSet();
        pairIds.Should().Contain(skillC.Id);
    }

    /// <summary>
    ///     Test 13: Two skills assigned to child SkillExecutionNodes inside different TaskNodes
    ///     that are connected by a TaskNode-level FS edge.  The resolver expands each TaskNode to
    ///     its child skill IDs, so the FS edge transitively serializes the skills.
    ///     No violation is raised.
    /// </summary>
    [Fact]
    public void FsThroughTaskNode_SkillsSerializedTransitively_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var taskA = CreateTaskNode("TaskA");
        var taskB = CreateTaskNode("TaskB");

        // Skills are children of the respective task nodes.
        var skillA = CreateSkillNode("SkillA", agentId, taskA.Id);
        var skillB = CreateSkillNode("SkillB", agentId, taskB.Id);

        var nodes = new List<Node> { taskA, taskB, skillA, skillB };

        // TaskNode FS edge: taskA ->FS-> taskB
        var taskFsEdge = CreateFsEdge(taskA.Id, taskB.Id);

        // Hierarchy: each task maps to its child skill
        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskA.Id] = [skillA],
            [taskB.Id] = [skillB]
        };
        var hierarchy = BuildHierarchy([skillA, skillB], [taskA, taskB], [], taskToSkillMapping, []);

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // The resolver must expand TaskNode IDs to their child skill IDs.
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(taskA.Id, It.IsAny<NodeHierarchyInfo>()))
            .Returns([skillA.Id]);
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(taskB.Id, It.IsAny<NodeHierarchyInfo>()))
            .Returns([skillB.Id]);
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(skillA.Id, It.IsAny<NodeHierarchyInfo>()))
            .Returns([skillA.Id]);
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(skillB.Id, It.IsAny<NodeHierarchyInfo>()))
            .Returns([skillB.Id]);

        // Act
        var violations = _validator.Validate(nodes, [taskFsEdge]);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 14: Two skills on the same agent reside in different outer router branches.
    ///     The outer router is the common ancestor and each skill descends from a different branch
    ///     target, so they are mutually exclusive and no violation is raised.
    /// </summary>
    [Fact]
    public void NestedRouters_OuterExclusiveBranches_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var outerRouterId = Guid.NewGuid();

        // Skills are direct children of the outer router, each in its own branch.
        var skillA = CreateSkillNode("A", agentId, outerRouterId);
        var skillB = CreateSkillNode("B", agentId, outerRouterId);

        var outerRouter = CreateRouterNode("Outer", [
            new ConditionalBranch { Name = "Left", Priority = 0, TargetNodeId = skillA.Id },
            new ConditionalBranch { Name = "Right", Priority = 1, TargetNodeId = skillB.Id }
        ]);
        outerRouter = outerRouter with { Id = outerRouterId };

        var nodes = new List<Node> { outerRouter, skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], [outerRouter]);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 15: Two skills on the same agent reside in different inner router branches, but
    ///     both inner branches are within the same outer branch.  The inner router is the deepest
    ///     common router ancestor and each skill enters it through a different branch target,
    ///     so they are mutually exclusive — no violation is raised.
    /// </summary>
    [Fact]
    public void NestedRouters_InnerExclusiveBranches_SameOuterBranch_ReturnsNoViolations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var outerRouterId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();

        // The outer router has a single branch pointing to the inner router.
        // The inner router then splits into two branches, one per skill.
        var skillA = CreateSkillNode("A", agentId, innerRouterId);
        var skillB = CreateSkillNode("B", agentId, innerRouterId);

        var innerRouter = CreateRouterNode("Inner", [
            new ConditionalBranch { Name = "InnerLeft", Priority = 0, TargetNodeId = skillA.Id },
            new ConditionalBranch { Name = "InnerRight", Priority = 1, TargetNodeId = skillB.Id }
        ]);
        innerRouter = innerRouter with { Id = innerRouterId, ParentId = outerRouterId };

        var outerRouter = CreateRouterNode("Outer", [
            new ConditionalBranch { Name = "OuterLeft", Priority = 0, TargetNodeId = innerRouterId }
        ]);
        outerRouter = outerRouter with { Id = outerRouterId };

        var nodes = new List<Node> { outerRouter, innerRouter, skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], [outerRouter, innerRouter]);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 16: Mixed-agent scenario.
    ///     Agent X has skills A ->FS-> B (valid).
    ///     Agent Y has skills C and D with no FS edge (invalid).
    ///     Only one violation is raised, for agent Y only.
    /// </summary>
    [Fact]
    public void MixedAgents_OneValidOneInvalid_ReturnsOneViolation()
    {
        // Arrange
        var agentX = Guid.NewGuid();
        var agentY = Guid.NewGuid();

        var skillA = CreateSkillNode("A", agentX);
        var skillB = CreateSkillNode("B", agentX);
        var skillC = CreateSkillNode("C", agentY);
        var skillD = CreateSkillNode("D", agentY);

        var nodes = new List<Node> { skillA, skillB, skillC, skillD };
        var edges = new List<DependencyEdge> { CreateFsEdge(skillA.Id, skillB.Id) };

        SetupHierarchy(nodes, [skillA, skillB, skillC, skillD], [], []);
        SetupResolverForSkills([skillA, skillB, skillC, skillD]);

        // Act
        var violations = _validator.Validate(nodes, edges);

        // Assert
        var violation = Assert.Single(violations);
        violation.AgentId.Should().Be(agentY);
        var pair = Assert.Single(violation.MissingFsPairs);
        new[] { pair.SkillA, pair.SkillB }.Should().BeEquivalentTo([skillC.Id, skillD.Id]);
    }

    /// <summary>
    ///     Test 17: An empty procedure with no nodes and no edges.
    ///     There is nothing to validate, so no violations are raised.
    /// </summary>
    [Fact]
    public void EmptyProcedure_ReturnsNoViolations()
    {
        // Arrange
        var nodes = new List<Node>();
        SetupHierarchy(nodes, [], [], []);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    /// <summary>
    ///     Test 18: A procedure that contains only TaskNodes (no SkillExecutionNodes).
    ///     With no skill nodes, the agent group list is empty and no violation is raised.
    /// </summary>
    [Fact]
    public void NoSkillNodes_OnlyTaskNodes_ReturnsNoViolations()
    {
        // Arrange
        var taskA = CreateTaskNode("Task A");
        var taskB = CreateTaskNode("Task B");
        var nodes = new List<Node> { taskA, taskB };

        SetupHierarchy(nodes, [], [taskA, taskB], []);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        Assert.Empty(violations);
    }

    // -------------------------------------------------------------------------
    // Additional structural / null-guard tests
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Verifies that the constructor guards against a null hierarchyProcessor argument.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHierarchyProcessorIsNull()
    {
        // Act
        var act = () => new AgentSerializationValidator(
            null!,
            _mockNodeResolver.Object,
            _mockAgentNameResolver.Object,
            NullLogger<AgentSerializationValidator>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchyProcessor");
    }

    /// <summary>
    ///     Verifies that the constructor guards against a null nodeResolver argument.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNodeResolverIsNull()
    {
        // Act
        var act = () => new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            null!,
            _mockAgentNameResolver.Object,
            NullLogger<AgentSerializationValidator>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("nodeResolver");
    }

    /// <summary>
    ///     Verifies that the constructor guards against a null agentNameResolver argument.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAgentNameResolverIsNull()
    {
        // Act
        var act = () => new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            _mockNodeResolver.Object,
            null!,
            NullLogger<AgentSerializationValidator>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("agentNameResolver");
    }

    /// <summary>
    ///     Verifies that the constructor guards against a null logger argument.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            _mockNodeResolver.Object,
            _mockAgentNameResolver.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    ///     Verifies that <see cref="AgentSerializationValidator.Validate" /> throws
    ///     <see cref="ArgumentNullException" /> when the nodes list is null.
    /// </summary>
    [Fact]
    public void Validate_ThrowsArgumentNullException_WhenNodesIsNull()
    {
        // Act
        var act = () => _validator.Validate(null!, []);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("nodes");
    }

    /// <summary>
    ///     Verifies that <see cref="AgentSerializationValidator.Validate" /> throws
    ///     <see cref="ArgumentNullException" /> when the edges list is null.
    /// </summary>
    [Fact]
    public void Validate_ThrowsArgumentNullException_WhenEdgesIsNull()
    {
        // Arrange
        var nodes = new List<Node>();
        SetupHierarchy(nodes, [], [], []);

        // Act
        var act = () => _validator.Validate(nodes, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("edges");
    }

    /// <summary>
    ///     Verifies that the violation record contains the human-readable skill names drawn from
    ///     <see cref="SkillExecutionTask.Skill" /> for each unserialized skill.
    /// </summary>
    [Fact]
    public void Violation_UnserializedSkills_ContainCorrectSkillNames()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("Alpha", agentId);
        var skillB = CreateSkillNode("Beta", agentId);
        var nodes = new List<Node> { skillA, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        var violation = Assert.Single(violations);
        violation.UnserializedSkills.Should().HaveCount(2);
        violation.UnserializedSkills.Select(s => s.SkillName)
            .Should().BeEquivalentTo(["Alpha", "Beta"]);
    }

    /// <summary>
    ///     Verifies that a violation's <see cref="AgentSerializationViolation.AgentName" /> is
    ///     populated from <see cref="IAgentNameResolver.Resolve" /> rather than from a raw ID string.
    ///     This ensures users see a meaningful label such as "KUKA Right" in the violation modal
    ///     instead of a raw GUID.
    /// </summary>
    [Fact]
    public void Violation_AgentName_UsesResolverResult()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };

        _mockAgentNameResolver.Setup(r => r.Resolve(agentId)).Returns("KUKA Right");

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        // Act
        var violations = _validator.Validate(nodes, []);

        // Assert
        var violation = Assert.Single(violations);
        violation.AgentName.Should().Be("KUKA Right");
    }

    // -------------------------------------------------------------------------
    // Level 2 — FS-first path scenarios (FS + SS combined)
    // -------------------------------------------------------------------------

    /// <summary>
    ///     L2 worked example (unsafe): agent A has skills A1, A2, A3; agent B has B1.
    ///     Edges: <c>A1 FS B1, B1 SS A2, B1 FS A3</c>. The pair (A2, A3) has no FS-first
    ///     path in either direction — A2 and A3 have no outgoing edges to each other
    ///     and no intermediate FS edge seeds the search. The LP scheduler is therefore
    ///     free to overlap them on agent A, so the validator must flag exactly one
    ///     violation naming skills A2 and A3.
    /// </summary>
    [Fact]
    public void FsThenAny_UnsafeWorkedExample_FlagsA2A3Pair()
    {
        var agentA = Guid.NewGuid();
        var agentB = Guid.NewGuid();
        var a1 = CreateSkillNode("A1", agentA);
        var a2 = CreateSkillNode("A2", agentA);
        var a3 = CreateSkillNode("A3", agentA);
        var b1 = CreateSkillNode("B1", agentB);
        var nodes = new List<Node> { a1, a2, a3, b1 };

        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(a1.Id, b1.Id),
            CreateSsEdge(b1.Id, a2.Id),
            CreateFsEdge(b1.Id, a3.Id)
        };

        SetupHierarchy(nodes, [a1, a2, a3, b1], [], []);
        SetupResolverForSkills([a1, a2, a3, b1]);

        var violations = _validator.Validate(nodes, edges);

        var violation = Assert.Single(violations);
        violation.AgentId.Should().Be(agentA);

        var pair = Assert.Single(violation.MissingFsPairs);
        var pairIds = new[] { pair.SkillA, pair.SkillB };
        pairIds.Should().BeEquivalentTo([a2.Id, a3.Id]);

        violation.UnserializedSkills.Should().HaveCount(2);
        violation.UnserializedSkills.Select(s => s.NodeId)
            .Should().BeEquivalentTo([a2.Id, a3.Id]);
    }

    /// <summary>
    ///     L2 worked example (fixed): the same graph as
    ///     <see cref="FsThenAny_UnsafeWorkedExample_FlagsA2A3Pair" /> plus a direct
    ///     <c>A2 FS A3</c> edge. The pair (A2, A3) now has an FS-first path by
    ///     <c>FsThenAny.base</c>, and the other two same-agent pairs remain serialized
    ///     by their prior FS-first paths, so no violations are reported.
    /// </summary>
    [Fact]
    public void FsThenAny_FixedWorkedExample_NoViolations()
    {
        var agentA = Guid.NewGuid();
        var agentB = Guid.NewGuid();
        var a1 = CreateSkillNode("A1", agentA);
        var a2 = CreateSkillNode("A2", agentA);
        var a3 = CreateSkillNode("A3", agentA);
        var b1 = CreateSkillNode("B1", agentB);
        var nodes = new List<Node> { a1, a2, a3, b1 };

        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(a1.Id, b1.Id),
            CreateSsEdge(b1.Id, a2.Id),
            CreateFsEdge(b1.Id, a3.Id),
            CreateFsEdge(a2.Id, a3.Id)
        };

        SetupHierarchy(nodes, [a1, a2, a3, b1], [], []);
        SetupResolverForSkills([a1, a2, a3, b1]);

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     A direct Start-to-Start edge between two same-agent skills does NOT serialize
    ///     them. SS alone only bounds starts (<c>S_B ≥ S_A</c>) — it permits concurrent
    ///     starts and therefore concurrent execution on the same agent. The validator
    ///     must flag the pair because no FS edge leaves either skill.
    /// </summary>
    [Fact]
    public void FsThenAny_PlainSsBetweenSameAgentSkills_FlagsViolation()
    {
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, skillB };

        var edges = new List<DependencyEdge> { CreateSsEdge(skillA.Id, skillB.Id) };

        SetupHierarchy(nodes, [skillA, skillB], [], []);
        SetupResolverForSkills([skillA, skillB]);

        var violations = _validator.Validate(nodes, edges);

        var violation = Assert.Single(violations);
        var pair = Assert.Single(violation.MissingFsPairs);
        var pairIds = new[] { pair.SkillA, pair.SkillB };
        pairIds.Should().BeEquivalentTo([skillA.Id, skillB.Id]);
    }

    /// <summary>
    ///     A chain starting with an FS edge and continuing through an SS edge is
    ///     FS-first and therefore sufficient for serialization: <c>A1 FS B1, B1 SS A2</c>
    ///     gives <c>F_A1 ≤ S_B1 ≤ S_A2</c>, so <c>F_A1 ≤ S_A2</c>. The validator must
    ///     accept the graph when A1 and A2 are on the same agent.
    /// </summary>
    [Fact]
    public void FsThenAny_FsThenSsChain_SameAgent_NoViolation()
    {
        var agentA = Guid.NewGuid();
        var agentB = Guid.NewGuid();
        var a1 = CreateSkillNode("A1", agentA);
        var a2 = CreateSkillNode("A2", agentA);
        var b1 = CreateSkillNode("B1", agentB);
        var nodes = new List<Node> { a1, a2, b1 };

        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(a1.Id, b1.Id),
            CreateSsEdge(b1.Id, a2.Id)
        };

        SetupHierarchy(nodes, [a1, a2, b1], [], []);
        SetupResolverForSkills([a1, a2, b1]);

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     A chain that starts with an SS edge and then has an FS edge does NOT
    ///     serialize the endpoints. The leading SS gives <c>S_B1 ≥ S_A1</c> but
    ///     never bounds <c>F_A1</c>, so the downstream FS <c>B1 FS A2</c> only
    ///     guarantees <c>S_A2 ≥ F_B1</c>, which can still permit <c>F_A1 > S_A2</c>
    ///     when <c>D_A1 &gt; D_B1</c>. The validator must flag the same-agent pair.
    /// </summary>
    [Fact]
    public void FsThenAny_SsThenFsChain_SameAgent_FlagsViolation()
    {
        var agentA = Guid.NewGuid();
        var agentB = Guid.NewGuid();
        var a1 = CreateSkillNode("A1", agentA);
        var a2 = CreateSkillNode("A2", agentA);
        var b1 = CreateSkillNode("B1", agentB);
        var nodes = new List<Node> { a1, a2, b1 };

        var edges = new List<DependencyEdge>
        {
            CreateSsEdge(a1.Id, b1.Id),
            CreateFsEdge(b1.Id, a2.Id)
        };

        SetupHierarchy(nodes, [a1, a2, b1], [], []);
        SetupResolverForSkills([a1, a2, b1]);

        var violations = _validator.Validate(nodes, edges);

        var violation = Assert.Single(violations);
        var pair = Assert.Single(violation.MissingFsPairs);
        var pairIds = new[] { pair.SkillA, pair.SkillB };
        pairIds.Should().BeEquivalentTo([a1.Id, a2.Id]);
    }

    // -------------------------------------------------------------------------
    // Router-branch expansion: FS/SS reachability through a router must
    // propagate to every skill descendant of the router's branch subtrees.
    // -------------------------------------------------------------------------

    /// <summary>
    ///     T1 — Skill A outside a router and skill B inside the router's branch task share the same
    ///     agent. A single FS edge <c>A →FS→ Router</c> must serialize the pair because
    ///     <c>A.finish ≤ Router.start ≤ B.start</c> holds for every branch skill. Before the
    ///     router-branch expansion the validator flagged this as a false positive.
    /// </summary>
    [Fact]
    public void FsThroughRouter_OutsideIntoRouterBranch_SameAgent_ReturnsNoViolations()
    {
        var agentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();

        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId, branchTaskId);

        var branchTask = CreateTaskNode("Branch", routerId);
        branchTask = branchTask with { Id = branchTaskId };

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = branchTaskId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node> { skillA, router, branchTask, skillB };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerId] = [branchTask],
            [branchTaskId] = [skillB]
        };

        SetupHierarchyWithParents(nodes, [skillA, skillB], [branchTask], [router], parentToChildren);
        SetupResolverForSkills([skillA, skillB]);
        SetupResolverForRouters([router]);

        var edges = new List<DependencyEdge> { CreateFsEdge(skillA.Id, routerId) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T2 — Downstream mirror of T1. Skill A inside the router's branch task and skill B
    ///     outside share the same agent; the router has an FS edge to B. <c>A.finish ≤ Router.finish
    ///     ≤ B.start</c> holds for every branch skill, so the pair must be serialized.
    /// </summary>
    [Fact]
    public void FsFromRouterBranchToOutside_SameAgent_ReturnsNoViolations()
    {
        var agentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();

        var skillA = CreateSkillNode("A", agentId, branchTaskId);
        var skillB = CreateSkillNode("B", agentId);

        var branchTask = CreateTaskNode("Branch", routerId);
        branchTask = branchTask with { Id = branchTaskId };

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = branchTaskId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node> { router, branchTask, skillA, skillB };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerId] = [branchTask],
            [branchTaskId] = [skillA]
        };

        SetupHierarchyWithParents(nodes, [skillA, skillB], [branchTask], [router], parentToChildren);
        SetupResolverForSkills([skillA, skillB]);
        SetupResolverForRouters([router]);

        var edges = new List<DependencyEdge> { CreateFsEdge(routerId, skillB.Id) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T3 — Regression: router expansion must not fabricate cross-branch serialization obligations.
    ///     An external FS edge into a router fans out via expansion to both branch skills, but
    ///     <c>AreInMutuallyExclusiveBranches</c> still skips the inter-branch pair because both
    ///     skills share the router as an ancestor via different branch targets.
    /// </summary>
    [Fact]
    public void ExternalFsEdgeIntoRouter_DifferentBranches_SameAgent_StillExclusive()
    {
        var agentId = Guid.NewGuid();
        var otherAgent = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTaskAId = Guid.NewGuid();
        var branchTaskBId = Guid.NewGuid();

        var predecessor = CreateSkillNode("Pre", otherAgent);
        var skillA = CreateSkillNode("A", agentId, branchTaskAId);
        var skillB = CreateSkillNode("B", agentId, branchTaskBId);

        var branchTaskA = CreateTaskNode("BranchA", routerId);
        branchTaskA = branchTaskA with { Id = branchTaskAId };
        var branchTaskB = CreateTaskNode("BranchB", routerId);
        branchTaskB = branchTaskB with { Id = branchTaskBId };

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Left", Priority = 0, TargetNodeId = branchTaskAId },
            new ConditionalBranch { Name = "Right", Priority = 1, TargetNodeId = branchTaskBId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node>
        {
            predecessor, router, branchTaskA, branchTaskB, skillA, skillB
        };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerId] = [branchTaskA, branchTaskB],
            [branchTaskAId] = [skillA],
            [branchTaskBId] = [skillB]
        };

        SetupHierarchyWithParents(
            nodes, [predecessor, skillA, skillB], [branchTaskA, branchTaskB], [router], parentToChildren);
        SetupResolverForSkills([predecessor, skillA, skillB]);
        SetupResolverForRouters([router]);

        var edges = new List<DependencyEdge> { CreateFsEdge(predecessor.Id, routerId) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T4 — Negative test: expansion must not mask genuine violations. A skill outside the
    ///     router and one inside its only branch share an agent, but there is no FS edge touching
    ///     the router at all. The validator must still flag the pair.
    /// </summary>
    [Fact]
    public void OutsideAndInsideRouter_NoFsEdge_SameAgent_StillFlagged()
    {
        var agentId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();

        var outsideSkill = CreateSkillNode("Outside", agentId);
        var insideSkill = CreateSkillNode("Inside", agentId, branchTaskId);

        var branchTask = CreateTaskNode("Branch", routerId);
        branchTask = branchTask with { Id = branchTaskId };

        var router = CreateRouterNode("Router", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = branchTaskId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node> { outsideSkill, router, branchTask, insideSkill };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [routerId] = [branchTask],
            [branchTaskId] = [insideSkill]
        };

        SetupHierarchyWithParents(
            nodes, [outsideSkill, insideSkill], [branchTask], [router], parentToChildren);
        SetupResolverForSkills([outsideSkill, insideSkill]);
        SetupResolverForRouters([router]);

        var violations = _validator.Validate(nodes, []);

        var violation = Assert.Single(violations);
        violation.AgentId.Should().Be(agentId);
        violation.MissingFsPairs.Should().HaveCount(1);
    }

    /// <summary>
    ///     T5 — End-to-end reproduction of the user-reported scenario on procedure
    ///     <c>a63b7266-...</c>: four skills on agent X (Inspect, Move-inside-Task, Weld, Default-Weld
    ///     inside router branch) plus two skills on a second agent (Hold, Grasp) bridging the chain
    ///     through a router. The FS chain routes external → router, so the three previously flagged
    ///     pairs involving the Default-Weld inside the router must now be serialized.
    /// </summary>
    [Fact]
    public void UsersScenario_FourSkillsAcrossTaskAndRouterBranch_ReturnsNoViolations()
    {
        var agentX = Guid.NewGuid();
        var agentHold = Guid.NewGuid();
        var agentGrasp = Guid.NewGuid();

        var routerId = Guid.NewGuid();
        var taskExampleId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();

        var inspect = CreateSkillNode("Inspect", agentX);
        var moveInTask = CreateSkillNode("Move", agentX, taskExampleId);
        var weld = CreateSkillNode("Weld", agentX);
        var hold = CreateSkillNode("Hold", agentHold);
        var grasp = CreateSkillNode("Grasp", agentGrasp);
        var defaultWeld = CreateSkillNode("DefaultWeld", agentX, branchTaskId);

        var taskExample = CreateTaskNode("Example");
        taskExample = taskExample with { Id = taskExampleId };

        var branchTask = CreateTaskNode("DefaultBranch", routerId);
        branchTask = branchTask with { Id = branchTaskId };

        var router = CreateRouterNode("Branch", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = branchTaskId }
        ]);
        router = router with { Id = routerId };

        var nodes = new List<Node>
        {
            inspect, taskExample, moveInTask, weld, hold, grasp, router, branchTask, defaultWeld
        };

        var taskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
        {
            [taskExampleId] = [moveInTask],
            [branchTaskId] = [defaultWeld]
        };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [taskExampleId] = [moveInTask],
            [routerId] = [branchTask],
            [branchTaskId] = [defaultWeld]
        };

        var hierarchy = BuildHierarchy(
            [inspect, moveInTask, weld, hold, grasp, defaultWeld],
            [taskExample, branchTask],
            [router],
            taskToSkillMapping,
            parentToChildren);
        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        SetupResolverForSkills([inspect, moveInTask, weld, hold, grasp, defaultWeld]);
        SetupResolverForRouters([router]);
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(taskExampleId, It.IsAny<NodeHierarchyInfo>()))
            .Returns([moveInTask.Id]);
        _mockNodeResolver
            .Setup(r => r.ResolveToExecutableIds(branchTaskId, It.IsAny<NodeHierarchyInfo>()))
            .Returns([defaultWeld.Id]);

        // User-drawn edges from the reported procedure.
        var edges = new List<DependencyEdge>
        {
            CreateFsEdge(inspect.Id, taskExampleId), // Inspect → Task(Move)
            CreateFsEdge(taskExampleId, hold.Id), // Task(Move) → Hold
            CreateSsEdge(hold.Id, weld.Id), // Hold SS Weld (bridges chain)
            CreateFsEdge(weld.Id, grasp.Id), // Weld → Grasp
            CreateFsEdge(grasp.Id, routerId) // Grasp → Router
        };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T6 — Deep nesting, forward direction. A skill outside an outer router with an FS edge
    ///     only to the outer router must serialize a skill buried two router levels deep (inside an
    ///     inner router that sits in the outer router's branch task). Proves the subtree walk
    ///     descends through nested routers even though domain rules currently forbid this structure.
    /// </summary>
    [Fact]
    public void FsThroughNestedRouter_OutsideIntoInnerBranchSkill_SameAgent_ReturnsNoViolations()
    {
        var agentId = Guid.NewGuid();
        var outerRouterId = Guid.NewGuid();
        var outerBranchTaskId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();
        var innerBranchTaskId = Guid.NewGuid();

        var outsideSkill = CreateSkillNode("Outside", agentId);
        var deepSkill = CreateSkillNode("Deep", agentId, innerBranchTaskId);

        var innerBranchTask = CreateTaskNode("InnerBranch", innerRouterId);
        innerBranchTask = innerBranchTask with { Id = innerBranchTaskId };

        var innerRouter = CreateRouterNode("Inner", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = innerBranchTaskId }
        ], outerBranchTaskId);
        innerRouter = innerRouter with { Id = innerRouterId };

        var outerBranchTask = CreateTaskNode("OuterBranch", outerRouterId);
        outerBranchTask = outerBranchTask with { Id = outerBranchTaskId };

        var outerRouter = CreateRouterNode("Outer", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = outerBranchTaskId }
        ]);
        outerRouter = outerRouter with { Id = outerRouterId };

        var nodes = new List<Node>
        {
            outsideSkill, outerRouter, outerBranchTask, innerRouter, innerBranchTask, deepSkill
        };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouterId] = [outerBranchTask],
            [outerBranchTaskId] = [innerRouter],
            [innerRouterId] = [innerBranchTask],
            [innerBranchTaskId] = [deepSkill]
        };

        SetupHierarchyWithParents(
            nodes,
            [outsideSkill, deepSkill],
            [outerBranchTask, innerBranchTask],
            [outerRouter, innerRouter],
            parentToChildren);
        SetupResolverForSkills([outsideSkill, deepSkill]);
        SetupResolverForRouters([outerRouter, innerRouter]);

        var edges = new List<DependencyEdge> { CreateFsEdge(outsideSkill.Id, outerRouterId) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T7 — Deep nesting, downstream direction. Skill two router levels deep plus skill outside
    ///     both routers on the same agent, with a single FS edge from the outer router to the
    ///     outside skill. Expansion must propagate through both router levels so the deep skill is
    ///     recognised as serialized before the outside skill.
    /// </summary>
    [Fact]
    public void FsFromInnerRouterBranchSkill_ToOutside_SameAgent_ReturnsNoViolations()
    {
        var agentId = Guid.NewGuid();
        var outerRouterId = Guid.NewGuid();
        var outerBranchTaskId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();
        var innerBranchTaskId = Guid.NewGuid();

        var deepSkill = CreateSkillNode("Deep", agentId, innerBranchTaskId);
        var outsideSkill = CreateSkillNode("Outside", agentId);

        var innerBranchTask = CreateTaskNode("InnerBranch", innerRouterId);
        innerBranchTask = innerBranchTask with { Id = innerBranchTaskId };

        var innerRouter = CreateRouterNode("Inner", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = innerBranchTaskId }
        ], outerBranchTaskId);
        innerRouter = innerRouter with { Id = innerRouterId };

        var outerBranchTask = CreateTaskNode("OuterBranch", outerRouterId);
        outerBranchTask = outerBranchTask with { Id = outerBranchTaskId };

        var outerRouter = CreateRouterNode("Outer", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = outerBranchTaskId }
        ]);
        outerRouter = outerRouter with { Id = outerRouterId };

        var nodes = new List<Node>
        {
            outerRouter, outerBranchTask, innerRouter, innerBranchTask, deepSkill, outsideSkill
        };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouterId] = [outerBranchTask],
            [outerBranchTaskId] = [innerRouter],
            [innerRouterId] = [innerBranchTask],
            [innerBranchTaskId] = [deepSkill]
        };

        SetupHierarchyWithParents(
            nodes,
            [deepSkill, outsideSkill],
            [outerBranchTask, innerBranchTask],
            [outerRouter, innerRouter],
            parentToChildren);
        SetupResolverForSkills([deepSkill, outsideSkill]);
        SetupResolverForRouters([outerRouter, innerRouter]);

        var edges = new List<DependencyEdge> { CreateFsEdge(outerRouterId, outsideSkill.Id) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     T8 — Regression for mutual exclusivity across nesting levels. Outer router has two
    ///     branches; one contains skill A directly, the other contains a nested inner router whose
    ///     branch task contains skill B. A and B share an agent. An external FS edge into the outer
    ///     router triggers expansion that adds both A and B to the predecessor's reachable set, but
    ///     the pair (A, B) must still be exempt because they sit in different outer-level branches.
    /// </summary>
    [Fact]
    public void NestedRouter_OuterMutualExclusivity_IgnoresInnerNesting()
    {
        var agentId = Guid.NewGuid();
        var otherAgent = Guid.NewGuid();
        var outerRouterId = Guid.NewGuid();
        var outerBranchAId = Guid.NewGuid();
        var outerBranchBId = Guid.NewGuid();
        var innerRouterId = Guid.NewGuid();
        var innerBranchId = Guid.NewGuid();

        var predecessor = CreateSkillNode("Pre", otherAgent);
        var skillA = CreateSkillNode("A", agentId, outerBranchAId);
        var skillB = CreateSkillNode("B", agentId, innerBranchId);

        var outerBranchA = CreateTaskNode("OuterBranchA", outerRouterId);
        outerBranchA = outerBranchA with { Id = outerBranchAId };
        var outerBranchB = CreateTaskNode("OuterBranchB", outerRouterId);
        outerBranchB = outerBranchB with { Id = outerBranchBId };

        var innerBranch = CreateTaskNode("InnerBranch", innerRouterId);
        innerBranch = innerBranch with { Id = innerBranchId };

        var innerRouter = CreateRouterNode("Inner", [
            new ConditionalBranch { Name = "Default", Priority = 0, TargetNodeId = innerBranchId }
        ], outerBranchBId);
        innerRouter = innerRouter with { Id = innerRouterId };

        var outerRouter = CreateRouterNode("Outer", [
            new ConditionalBranch { Name = "Left", Priority = 0, TargetNodeId = outerBranchAId },
            new ConditionalBranch { Name = "Right", Priority = 1, TargetNodeId = outerBranchBId }
        ]);
        outerRouter = outerRouter with { Id = outerRouterId };

        var nodes = new List<Node>
        {
            predecessor, outerRouter, outerBranchA, outerBranchB, innerRouter, innerBranch, skillA, skillB
        };
        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            [outerRouterId] = [outerBranchA, outerBranchB],
            [outerBranchAId] = [skillA],
            [outerBranchBId] = [innerRouter],
            [innerRouterId] = [innerBranch],
            [innerBranchId] = [skillB]
        };

        SetupHierarchyWithParents(
            nodes,
            [predecessor, skillA, skillB],
            [outerBranchA, outerBranchB, innerBranch],
            [outerRouter, innerRouter],
            parentToChildren);
        SetupResolverForSkills([predecessor, skillA, skillB]);
        SetupResolverForRouters([outerRouter, innerRouter]);

        var edges = new List<DependencyEdge> { CreateFsEdge(predecessor.Id, outerRouterId) };

        var violations = _validator.Validate(nodes, edges);

        violations.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Private helper methods
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Creates a <see cref="SkillExecutionNode" /> with the given display name and agent assignment.
    ///     The skill name is also used as the <see cref="Skill.Name" /> for violation assertions.
    /// </summary>
    /// <param name="name">Human-readable name used both as task name and skill name.</param>
    /// <param name="agentId">The agent identifier assigned to execute this skill.</param>
    /// <param name="parentId">Optional parent node ID when the skill is nested inside a task or router.</param>
    /// <returns>A fully initialized <see cref="SkillExecutionNode" />.</returns>
    /// <summary>
    ///     A skill nested under a sub-task that is the source of a container-level FS edge is serialized
    ///     against a co-agent sibling once resolution recurses into the nested skill. The single-level
    ///     resolution dropped the nested skill from the adjacency, making the pair appear unserialized and
    ///     producing a false-positive violation — the gap fails safe (over-reports), never permitting
    ///     overlap. This test exercises the real <see cref="NodeResolver" />, not the mock.
    /// </summary>
    [Fact]
    public void NestedTaskSource_CoAgentSkill_SerializedByContainerEdge_ReturnsNoViolations()
    {
        var agentId = Guid.NewGuid();
        var taskA = NewTask();
        var taskB = NewTask(taskA.Id);
        var skillX = CreateSkillNode("X", agentId, taskB.Id);
        var skillY = CreateSkillNode("Y", agentId);
        var nodes = new List<Node> { taskA, taskB, skillX, skillY };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { taskA, taskB }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillX, skillY }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { Guid.Empty, new List<Node> { taskA, skillY }.AsReadOnly() },
                { taskA.Id, new List<Node> { taskB }.AsReadOnly() },
                { taskB.Id, new List<Node> { skillX }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { taskA.Id, Array.Empty<SkillExecutionNode>().AsReadOnly() },
                { taskB.Id, new List<SkillExecutionNode> { skillX }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode> { { skillX.Id, taskB } }
        };
        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        var validator = new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            _mockAgentNameResolver.Object,
            NullLogger<AgentSerializationValidator>.Instance);

        var fsEdge = CreateFsEdge(taskA.Id, skillY.Id);

        // Act
        var violations = validator.Validate(nodes, [fsEdge]);

        // Assert: X (nested under A) and Y are serialized via the container edge A -> Y.
        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     Builds an <see cref="AgentSerializationValidator" /> backed by the real <see cref="NodeResolver" />
    ///     (not the mock), so the FS-chain reachability is exercised against actual node resolution. The
    ///     hierarchy is still supplied through <see cref="_mockHierarchyProcessor" />.
    /// </summary>
    private AgentSerializationValidator CreateValidatorWithRealResolver()
    {
        return new AgentSerializationValidator(
            _mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            _mockAgentNameResolver.Object,
            NullLogger<AgentSerializationValidator>.Instance);
    }

    /// <summary>
    ///     A —FS→ emptyTask —FS→ B with A and B on the same agent. The empty task is a zero-extent firing
    ///     endpoint, so at runtime B fires after the task's Finish after A's Finish — A and B ARE serialized.
    ///     The validator must recognize the chain through the empty container and report no violation.
    /// </summary>
    [Fact]
    public void Validate_FsChainThroughEmptyTask_SameAgent_IsSerialized()
    {
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var emptyTask = CreateTaskNode("EmptyTask");
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, emptyTask, skillB };

        SetupHierarchy(nodes, [skillA, skillB], [emptyTask], []);
        var validator = CreateValidatorWithRealResolver();

        // Act
        var violations = validator.Validate(
            nodes,
            [CreateFsEdge(skillA.Id, emptyTask.Id), CreateFsEdge(emptyTask.Id, skillB.Id)]);

        // Assert — A and B are serialized through the empty task; no violation.
        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     A —FS→ outerTask —FS→ B, where outerTask contains only an empty inner task (so it resolves to no
    ///     executable leaf). The chain still serializes A and B; no violation.
    /// </summary>
    [Fact]
    public void Validate_FsChainThroughNestedEmptyTask_SameAgent_IsSerialized()
    {
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var outerTask = CreateTaskNode("Outer");
        var innerTask = CreateTaskNode("Inner", outerTask.Id);
        var skillB = CreateSkillNode("B", agentId);
        var nodes = new List<Node> { skillA, outerTask, innerTask, skillB };

        SetupHierarchyWithParents(
            nodes, [skillA, skillB], [outerTask, innerTask], [],
            new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { outerTask.Id, new List<Node> { innerTask }.AsReadOnly() }
            });
        var validator = CreateValidatorWithRealResolver();

        // Act
        var violations = validator.Validate(
            nodes,
            [CreateFsEdge(skillA.Id, outerTask.Id), CreateFsEdge(outerTask.Id, skillB.Id)]);

        // Assert — A and B are serialized through the nested-empty container; no violation.
        violations.Should().BeEmpty();
    }

    private static TaskNode NewTask(Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Task", StartTime = 0, Duration = 1 }
        };
    }

    private static SkillExecutionNode CreateSkillNode(string name, Guid agentId, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0,
                Duration = 1,
                AgentId = agentId,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = string.Empty,
                    Properties = []
                }
            }
        };
    }

    /// <summary>
    ///     Creates a <see cref="TaskNode" /> with the given display name.
    /// </summary>
    /// <param name="name">Human-readable name of the task.</param>
    /// <param name="parentId">Optional parent node ID when the task is nested inside a router.</param>
    /// <returns>A fully initialized <see cref="TaskNode" />.</returns>
    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                StartTime = 0,
                Duration = 1
            }
        };
    }

    /// <summary>
    ///     Creates a <see cref="RouterNode" /> with the given display name and branch list.
    /// </summary>
    /// <param name="name">Human-readable name of the router.</param>
    /// <param name="branches">The conditional branches that the router can select between.</param>
    /// <param name="parentId">Optional parent node ID when the router is nested.</param>
    /// <returns>A fully initialized <see cref="RouterNode" />.</returns>
    private static RouterNode CreateRouterNode(
        string name,
        List<ConditionalBranch> branches,
        Guid? parentId = null)
    {
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = branches
            }
        };
    }

    /// <summary>
    ///     Creates a Finish-to-Start <see cref="DependencyEdge" /> using the canonical handle
    ///     strings ("right" source = Finish, "left" target = Start).
    /// </summary>
    /// <param name="sourceId">The ID of the predecessor node.</param>
    /// <param name="targetId">The ID of the successor node.</param>
    /// <returns>A <see cref="DependencyEdge" /> representing an FS dependency.</returns>
    private static DependencyEdge CreateFsEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "right", // Finish
            TargetHandle = "left" // Start
        };
    }

    /// <summary>
    ///     Creates a Start-to-Start <see cref="DependencyEdge" /> (left-to-left handles).
    /// </summary>
    /// <param name="sourceId">The ID of the predecessor node.</param>
    /// <param name="targetId">The ID of the successor node.</param>
    /// <returns>A <see cref="DependencyEdge" /> representing an SS dependency.</returns>
    private static DependencyEdge CreateSsEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "left", // Start
            TargetHandle = "left" // Start
        };
    }

    /// <summary>
    ///     Creates a Finish-to-Finish <see cref="DependencyEdge" /> (right-to-right handles).
    /// </summary>
    /// <param name="sourceId">The ID of the predecessor node.</param>
    /// <param name="targetId">The ID of the successor node.</param>
    /// <returns>A <see cref="DependencyEdge" /> representing an FF dependency.</returns>
    private static DependencyEdge CreateFfEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "right", // Finish
            TargetHandle = "right" // Finish
        };
    }

    /// <summary>
    ///     Creates a Start-to-Finish <see cref="DependencyEdge" /> (left-to-right handles).
    /// </summary>
    /// <param name="sourceId">The ID of the predecessor node.</param>
    /// <param name="targetId">The ID of the successor node.</param>
    /// <returns>A <see cref="DependencyEdge" /> representing an SF dependency.</returns>
    private static DependencyEdge CreateSfEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "left", // Start
            TargetHandle = "right" // Finish
        };
    }

    /// <summary>
    ///     Configures <see cref="_mockHierarchyProcessor" /> to return a <see cref="NodeHierarchyInfo" />
    ///     built from the supplied collections.  The <c>TaskToSkillMapping</c> and
    ///     <c>SkillToTaskMapping</c> are left empty because most tests do not involve TaskNode expansion.
    /// </summary>
    /// <param name="nodes">The full node list that the mock will be matched against.</param>
    /// <param name="skillNodes">All <see cref="SkillExecutionNode" />s in the hierarchy.</param>
    /// <param name="taskNodes">All <see cref="TaskNode" />s in the hierarchy.</param>
    /// <param name="routerNodes">All <see cref="RouterNode" />s in the hierarchy.</param>
    private void SetupHierarchy(
        List<Node> nodes,
        IReadOnlyList<SkillExecutionNode> skillNodes,
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<RouterNode> routerNodes)
    {
        var hierarchy = BuildHierarchy(skillNodes, taskNodes, routerNodes, [], []);
        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);
    }

    /// <summary>
    ///     Configures <see cref="_mockHierarchyProcessor" /> to return a <see cref="NodeHierarchyInfo" />
    ///     with an explicit parent-to-children mapping.  Router-branch expansion tests require this
    ///     because the validator walks the mapping downward from each router to collect branch skills.
    /// </summary>
    /// <param name="nodes">The full node list that the mock will be matched against.</param>
    /// <param name="skillNodes">All <see cref="SkillExecutionNode" />s in the hierarchy.</param>
    /// <param name="taskNodes">All <see cref="TaskNode" />s in the hierarchy.</param>
    /// <param name="routerNodes">All <see cref="RouterNode" />s in the hierarchy.</param>
    /// <param name="parentToChildrenMapping">
    ///     Map from each parent node ID to its direct children, used by the router-branch subtree walk.
    /// </param>
    private void SetupHierarchyWithParents(
        List<Node> nodes,
        IReadOnlyList<SkillExecutionNode> skillNodes,
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<RouterNode> routerNodes,
        Dictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        var hierarchy = BuildHierarchy(skillNodes, taskNodes, routerNodes, [], parentToChildrenMapping);
        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);
    }

    /// <summary>
    ///     Builds a <see cref="NodeHierarchyInfo" /> from the provided collections, optionally
    ///     including a custom <c>taskToSkillMapping</c> and <c>parentToChildrenMapping</c>.
    /// </summary>
    /// <param name="skillNodes">All <see cref="SkillExecutionNode" />s.</param>
    /// <param name="taskNodes">All <see cref="TaskNode" />s.</param>
    /// <param name="routerNodes">All <see cref="RouterNode" />s.</param>
    /// <param name="taskToSkillMapping">
    ///     Maps each <see cref="TaskNode" /> ID to its child <see cref="SkillExecutionNode" />s.
    ///     Pass an empty dictionary when no TaskNode expansion is required.
    /// </param>
    /// <param name="parentToChildrenMapping">
    ///     Map from each parent node ID to its direct children.  Needed for router-subtree traversal;
    ///     pass an empty dictionary when no router expansion is exercised.
    /// </param>
    /// <returns>A fully populated <see cref="NodeHierarchyInfo" />.</returns>
    private static NodeHierarchyInfo BuildHierarchy(
        IReadOnlyList<SkillExecutionNode> skillNodes,
        IReadOnlyList<TaskNode> taskNodes,
        IReadOnlyList<RouterNode> routerNodes,
        Dictionary<Guid, IReadOnlyList<SkillExecutionNode>> taskToSkillMapping,
        Dictionary<Guid, IReadOnlyList<Node>> parentToChildrenMapping)
    {
        return new NodeHierarchyInfo
        {
            TaskNodes = taskNodes,
            SkillExecutionNodes = skillNodes,
            RouterNodes = routerNodes,
            ParentToChildrenMapping = parentToChildrenMapping,
            TaskToSkillMapping = taskToSkillMapping,
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };
    }

    /// <summary>
    ///     Configures the <see cref="_mockNodeResolver" /> so that every
    ///     <see cref="SkillExecutionNode" /> in <paramref name="skills" /> resolves to its own ID
    ///     (identity mapping).  This covers the common case where edges connect skill nodes directly.
    /// </summary>
    /// <param name="skills">The skill nodes whose IDs should resolve to themselves.</param>
    private void SetupResolverForSkills(IReadOnlyList<SkillExecutionNode> skills)
    {
        foreach (var skill in skills)
            _mockNodeResolver
                .Setup(r => r.ResolveToExecutableIds(skill.Id, It.IsAny<NodeHierarchyInfo>()))
                .Returns([skill.Id]);
    }

    /// <summary>
    ///     Configures the <see cref="_mockNodeResolver" /> so that every <see cref="RouterNode" />
    ///     in <paramref name="routers" /> resolves to its own ID (identity mapping), matching the
    ///     production <c>NodeResolver</c> behaviour. Tests that draw edges touching a router must
    ///     call this helper in addition to <see cref="SetupResolverForSkills" />.
    /// </summary>
    /// <param name="routers">The router nodes whose IDs should resolve to themselves.</param>
    private void SetupResolverForRouters(IReadOnlyList<RouterNode> routers)
    {
        foreach (var router in routers)
            _mockNodeResolver
                .Setup(r => r.ResolveToExecutableIds(router.Id, It.IsAny<NodeHierarchyInfo>()))
                .Returns([router.Id]);
    }
}