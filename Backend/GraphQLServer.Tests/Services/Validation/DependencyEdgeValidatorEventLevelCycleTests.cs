using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Validation;

/// <summary>
///     Tests for the event-level cycle detection in <see cref="DependencyEdgeValidator"/> Rule 7
///     and the TaskNode finish-handle restriction in Rule 9.
///     Validates all 13 scenarios from <c>Backend/docs/archive/2026-04-15-scc-detection-analysis.md</c>.
/// </summary>
public class DependencyEdgeValidatorEventLevelCycleTests
{
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeAgentMapper> _mockNodeAgentMapper = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();

    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _sut;

    public DependencyEdgeValidatorEventLevelCycleTests()
    {
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId);
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>());

        // Default mapping for any SkillExecutionNode: returns a runtime agent that
        // supports adaptive execution for the node's skill. Tests in this class focus
        // on cycle and handle-restriction rules; they assume the adaptive-capability
        // rule passes, so they reach the rule under test.
        _mockNodeAgentMapper
            .Setup(m => m.MapAsync(It.IsAny<Node>()))
            .Returns<Node>(node => Task.FromResult(BuildAdaptiveCapableMapping(node)));

        _sut = new DependencyEdgeValidator(
            _mockNodeService.Object,
            _mockEdgeService.Object,
            _mockProcedureContext.Object,
            _mockNodeAgentMapper.Object);
    }

    /// <summary>
    ///     Builds a mapping for any <see cref="SkillExecutionNode" /> whose runtime agent
    ///     reports adaptive-capable for the node's skill. Returns <c>null</c> for non-skill
    ///     nodes, matching <see cref="INodeAgentMapper" />'s real contract.
    /// </summary>
    /// <param name="node">The node whose mapping is requested.</param>
    /// <returns>A mapping tuple, or <c>null</c> if the node is not a skill execution node.</returns>
    private static (Skill DomainSkill, Agent DomainAgent, IRuntimeAgent Agent)? BuildAdaptiveCapableMapping(Node node)
    {
        if (node is not SkillExecutionNode skillNode)
            return null;

        var runtimeMock = new Mock<IRuntimeAgent>();
        runtimeMock.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var domainAgent = new Agent
        {
            Id = skillNode.SkillExecutionTask.AgentId,
            Name = "TestAgent",
            RepresentativeColor = "#FFFFFF",
            SkillIds = [skillNode.SkillExecutionTask.Skill.Id]
        };

        return (skillNode.SkillExecutionTask.Skill, domainAgent, runtimeMock.Object);
    }

    private TaskNode SetupTaskNode(Guid nodeId, Guid? parentId = null)
    {
        var node = new TaskNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Domain.Entities.Procedure.Task
            {
                Name = $"Task {nodeId.ToString()[..8]}",
                StartTime = 0,
                Duration = 1
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
        return node;
    }

    private SkillExecutionNode SetupSkillNode(Guid nodeId, Guid? parentId = null)
    {
        var node = new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"Skill {nodeId.ToString()[..8]}",
                StartTime = 0,
                Duration = 1,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
        return node;
    }

    private void SetupExistingEdges(params DependencyEdge[] edges)
    {
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(edges.ToList());
    }

    private static DependencyEdge CreateEdge(
        Guid sourceId, Guid targetId,
        string? sourceHandle = null, string? targetHandle = null)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = sourceHandle,
            TargetHandle = targetHandle
        };
    }

    #region Cycles that MUST be rejected

    [Fact]
    public async Task FsCycle_TwoNodes_Rejected()
    {
        // A -FS-> B exists, adding B -FS-> A creates cycle
        // (A,Start)→(B,Finish)→(B,Start)→(A,Finish)→(A,Start)
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "right", "left")); // A -FS-> B

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(b.Id, a.Id, "right", "left")); // B -FS-> A
        Assert.Contains("circular dependency", ex.Message);
    }

    [Fact]
    public async Task SsMutual_Rejected()
    {
        // A -SS-> B exists, adding B -SS-> A creates cycle
        // (A,Start)→(B,Start)→(A,Start)
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "left", "left")); // A -SS-> B

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(b.Id, a.Id, "left", "left")); // B -SS-> A
        Assert.Contains("circular dependency", ex.Message);
    }

    [Fact]
    public async Task FfMutual_Rejected()
    {
        // A -FF-> B exists, adding B -FF-> A creates cycle
        // (A,Finish)→(B,Finish)→(A,Finish)
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "right", "right")); // A -FF-> B

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(b.Id, a.Id, "right", "right")); // B -FF-> A
        Assert.Contains("circular dependency", ex.Message);
    }

    [Fact]
    public async Task SfPlusFsCycle_Rejected()
    {
        // B -SF-> A exists (A.Finish waits for B.Start), adding A -FS-> B creates cycle
        // (A,Finish)→(B,Start)→... wait, (B,Start)→(A,Finish) via the new FS?
        // No: A -FS-> B means B.Start waits for A.Finish: (B,Start)→(A,Finish)
        // So cycle: (A,Finish)→(B,Start) [from SF] and (B,Start)→(A,Finish) [from FS]
        // → (A,Finish)→(B,Start)→(A,Finish). Direct cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(b.Id, a.Id, "left", "right")); // B -SF-> A

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(a.Id, b.Id, "right", "left")); // A -FS-> B
        Assert.Contains("circular dependency", ex.Message);
    }

    [Fact]
    public async Task ThreeNodeFsCycle_Rejected()
    {
        // A -FS-> B, B -FS-> C exist. Adding C -FS-> A creates 3-node cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());
        var c = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(
            CreateEdge(a.Id, b.Id, "right", "left"), // A -FS-> B
            CreateEdge(b.Id, c.Id, "right", "left")); // B -FS-> C

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(c.Id, a.Id, "right", "left")); // C -FS-> A
        Assert.Contains("circular dependency", ex.Message);
    }

    #endregion

    #region Non-cycles that MUST be allowed

    [Fact]
    public async Task SsPlusFf_CrossEvent_Allowed()
    {
        // Scenario 5: B -SS-> A (A.Start waits for B.Start),
        // adding A -FF-> B (B.Finish waits for A.Finish)
        // Event-level: (A,Start)→(B,Start) dead end; (B,Fin)→(A,Fin)→(A,Start)→(B,Start) dead end
        // No cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(b.Id, a.Id, "left", "left")); // B -SS-> A

        // Should NOT throw
        await _sut.ValidateAsync(a.Id, b.Id, "right", "right"); // A -FF-> B
    }

    [Fact]
    public async Task CoordinatedWindow_SsPlusFf_Allowed()
    {
        // Scenario 9: A -SS-> B (B.Start waits for A.Start),
        // adding B -FF-> A (A.Finish waits for B.Finish)
        // "B runs inside A's execution window" pattern. No deadlock.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "left", "left")); // A -SS-> B

        await _sut.ValidateAsync(b.Id, a.Id, "right", "right"); // B -FF-> A
    }

    [Fact]
    public async Task ThreeNodeCrossEvent_Allowed()
    {
        // Scenario 13: B -SS-> A, C -SS-> B exist. Adding A -FF-> C.
        // Node-level cycle A→B→C→A but no event-level cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());
        var c = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(
            CreateEdge(b.Id, a.Id, "left", "left"), // B -SS-> A
            CreateEdge(c.Id, b.Id, "left", "left")); // C -SS-> B

        await _sut.ValidateAsync(a.Id, c.Id, "right", "right"); // A -FF-> C
    }

    [Fact]
    public async Task FsChain_NoReverse_Allowed()
    {
        // A -FS-> B exists. Adding B -FS-> C. Chain, no cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());
        var c = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "right", "left")); // A -FS-> B

        await _sut.ValidateAsync(b.Id, c.Id, "right", "left"); // B -FS-> C
    }

    [Fact]
    public async Task FfChain_NoReverse_Allowed()
    {
        // A -FF-> B exists. Adding B -FF-> C. Chain, no cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());
        var c = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "right", "right")); // A -FF-> B

        await _sut.ValidateAsync(b.Id, c.Id, "right", "right"); // B -FF-> C
    }

    [Fact]
    public async Task SfMutual_Allowed()
    {
        // A -SF-> B exists (B.Finish waits for A.Start).
        // Adding B -SF-> A (A.Finish waits for B.Start).
        // SF edges go Finish→Start in event graph. Start vertices are sinks. No cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        SetupExistingEdges(CreateEdge(a.Id, b.Id, "left", "right")); // A -SF-> B

        await _sut.ValidateAsync(b.Id, a.Id, "left", "right"); // B -SF-> A
    }

    [Fact]
    public async Task SingleFfEdge_Allowed()
    {
        // A single FF edge with no reverse. No cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        await _sut.ValidateAsync(a.Id, b.Id, "right", "right"); // A -FF-> B
    }

    [Fact]
    public async Task SingleSsEdge_Allowed()
    {
        // A single SS edge. No cycle.
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        await _sut.ValidateAsync(a.Id, b.Id, "left", "left"); // A -SS-> B
    }

    #endregion

    #region Rule 9: No F dependency into TaskNode

    [Fact]
    public async Task FDepToTaskNode_FinishHandle_Rejected()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupTaskNode(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right")); // FF to TaskNode
        Assert.Contains("Cannot target the finish handle of task node", ex.Message);
    }

    [Fact]
    public async Task SDepToTaskNode_StartHandle_Allowed()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupTaskNode(Guid.NewGuid());

        // FS edge (right→left) to TaskNode — should be allowed
        await _sut.ValidateAsync(source.Id, target.Id, "right", "left");
    }

    [Fact]
    public async Task FDepToTaskNode_NullHandle_Rejected()
    {
        // null handle defaults to "right" (Finish) — should be rejected
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupTaskNode(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, null, null)); // null→null = FF
        Assert.Contains("Cannot target the finish handle of task node", ex.Message);
    }

    #endregion

    #region Edge update (excludeEdgeId)

    [Fact]
    public async Task EdgeUpdate_ExcludesOwnEdge_NoFalseCycle()
    {
        // Updating an existing FS edge should not detect itself as a cycle
        var a = SetupSkillNode(Guid.NewGuid());
        var b = SetupSkillNode(Guid.NewGuid());

        var existingEdgeId = Guid.NewGuid();
        SetupExistingEdges(new DependencyEdge
        {
            Id = existingEdgeId,
            ProcedureId = _procedureId,
            SourceId = a.Id,
            TargetId = b.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        });

        // Update same edge (SS instead of FS) — should not trigger cycle
        await _sut.ValidateAsync(a.Id, b.Id, "left", "left", existingEdgeId);
    }

    #endregion
}