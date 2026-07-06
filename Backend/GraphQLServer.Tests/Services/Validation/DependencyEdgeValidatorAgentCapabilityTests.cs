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
///     Unit tests for the adaptive-capability rule of <see cref="DependencyEdgeValidator" />.
///     The rule fires when a proposed edge targets the finish (F) side of a
///     <see cref="SkillExecutionNode" /> — i.e. when the resulting edge is SF or FF — and
///     enforces that the runtime agent currently assigned to that node's
///     <c>SkillExecutionTask.AgentId</c> supports adaptive execution for the node's skill
///     via <see cref="IRuntimeAgent.CanExecuteAdaptivelyAsync" />.
/// </summary>
public class DependencyEdgeValidatorAgentCapabilityTests
{
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeAgentMapper> _mockNodeAgentMapper = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();

    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _sut;

    /// <summary>
    ///     Initializes a fresh <see cref="DependencyEdgeValidator" /> with mocked dependencies
    ///     and a current procedure id. No default mapping is configured — individual tests
    ///     opt in to specific mapping outcomes.
    /// </summary>
    public DependencyEdgeValidatorAgentCapabilityTests()
    {
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId);
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>());

        _sut = new DependencyEdgeValidator(
            _mockNodeService.Object,
            _mockEdgeService.Object,
            _mockProcedureContext.Object,
            _mockNodeAgentMapper.Object);
    }

    #region Happy path

    /// <summary>
    ///     Verifies that an FF edge into a skill execution whose assigned agent supports
    ///     adaptive execution for the target's skill is accepted.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FinishHandleWithAdaptiveCapableAgent_Succeeds()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupMapping(target, true);

        await _sut.ValidateAsync(source.Id, target.Id, "right", "right");
    }

    /// <summary>
    ///     Verifies that on the happy path the runtime predicate is invoked exactly once
    ///     against the target's skill.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FinishHandleHappyPath_InvokesCanExecuteAdaptivelyAsyncOnce()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        var (runtimeAgentMock, _) = SetupMapping(target, true);

        await _sut.ValidateAsync(source.Id, target.Id, "right", "right");

        runtimeAgentMock.Verify(
            a => a.CanExecuteAdaptivelyAsync(target.SkillExecutionTask.Skill, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Rejection paths

    /// <summary>
    ///     Verifies that an FF edge is rejected when the assigned agent reports it cannot
    ///     execute the skill adaptively. The exception message must name the agent and the
    ///     skill, and reference adaptive execution so the user understands the cause.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FinishHandleWithNonAdaptiveAgent_ThrowsNamingAgentAndSkill()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupMapping(target, false, "AgentY");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right"));
        Assert.Contains("AgentY", ex.Message);
        Assert.Contains(target.SkillExecutionTask.Skill.Name, ex.Message);
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Verifies that when the mapper cannot resolve a domain agent for the target, the
    ///     edge is rejected with a clear unassigned/offline message that names the target.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FinishHandleWithUnresolvableDomainAgent_ThrowsOfflineMessage()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupNullMapping(target);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right"));
        Assert.Contains("agent", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unassigned", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(target.Id.ToString(), ex.Message);
    }

    /// <summary>
    ///     Documents that the validator collapses the "no domain agent" and "no runtime
    ///     agent" failure modes into the same exception. The mapper returns null for both
    ///     causes, and the user-facing message is identical because both manifest as the
    ///     agent being unavailable for the planning/execution pipeline.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FinishHandleWithOfflineRuntimeAgent_ThrowsOfflineMessage()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupNullMapping(target);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right"));
        Assert.Contains("online", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Short-circuits (rule must NOT fire)

    /// <summary>
    ///     Verifies that an S-side (FS or SS) edge into a skill execution bypasses the
    ///     adaptive-capability rule entirely. The mapper must not be called.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_StartHandleSkipsAdaptiveCheck()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());

        await _sut.ValidateAsync(source.Id, target.Id, "right", "left");

        _mockNodeAgentMapper.Verify(m => m.MapAsync(It.IsAny<Node>()), Times.Never);
    }

    #endregion

    #region Update path and handle-equivalence

    /// <summary>
    ///     Verifies that the rule applies on update operations (with <c>excludeEdgeId</c>
    ///     set) the same way it applies on create.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_UpdatePathFinishHandleWithNonAdaptiveAgent_StillThrows()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupMapping(target, false, "AgentY");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right", Guid.NewGuid()));
        Assert.Contains("AgentY", ex.Message);
    }

    /// <summary>
    ///     Verifies that a <c>null</c> target handle is treated as the finish side
    ///     (consistent with <c>DependencyGraphAnalyzer.GetEventTypeFromHandle</c>).
    /// </summary>
    [Fact]
    public async Task ValidateAsync_NullTargetHandleTreatedAsFinishSide()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupMapping(target, false, "AgentY");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", null));
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Verifies that handle comparison is case-insensitive: <c>"RIGHT"</c> is treated
    ///     as the finish side.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_UppercaseRightTargetHandleTreatedAsFinishSide()
    {
        var source = SetupSkillNode(Guid.NewGuid());
        var target = SetupSkillNode(Guid.NewGuid());
        SetupMapping(target, false, "AgentY");

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "RIGHT"));
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helpers

    /// <summary>
    ///     Creates a top-level <see cref="SkillExecutionNode" /> with a fresh skill and
    ///     agent id, and registers it with the mock node service so
    ///     <see cref="INodeApplicationService.GetNodeByIdAsync" /> resolves it.
    /// </summary>
    /// <param name="nodeId">The id to assign to the node.</param>
    /// <returns>The created skill execution node.</returns>
    private SkillExecutionNode SetupSkillNode(Guid nodeId)
    {
        var node = new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"Skill {nodeId.ToString()[..8]}",
                StartTime = 0,
                Duration = 1,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "SkillX",
                    Description = "Test",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
        return node;
    }

    /// <summary>
    ///     Configures the mapper to return a successful mapping for the given target node.
    /// </summary>
    /// <param name="targetNode">The skill execution node to map.</param>
    /// <param name="canAdapt">The value the mocked runtime agent reports for adaptive capability.</param>
    /// <param name="agentName">The display name to assign to the mocked domain agent.</param>
    /// <returns>The runtime-agent mock and the constructed domain agent.</returns>
    private (Mock<IRuntimeAgent> RuntimeAgent, Agent DomainAgent) SetupMapping(
        SkillExecutionNode targetNode, bool canAdapt, string agentName = "AgentY")
    {
        var runtimeMock = new Mock<IRuntimeAgent>();
        runtimeMock.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canAdapt);

        var domainAgent = new Agent
        {
            Id = targetNode.SkillExecutionTask.AgentId,
            Name = agentName,
            RepresentativeColor = "#FFFFFF",
            SkillIds = [targetNode.SkillExecutionTask.Skill.Id]
        };

        _mockNodeAgentMapper
            .Setup(m => m.MapAsync(targetNode))
            .ReturnsAsync((targetNode.SkillExecutionTask.Skill, domainAgent, runtimeMock.Object));

        return (runtimeMock, domainAgent);
    }

    /// <summary>
    ///     Configures the mapper to return <c>null</c> for the given target node, modelling
    ///     an unassigned <c>AgentId</c>, an unknown agent, or a runtime agent that is not
    ///     currently registered/online.
    /// </summary>
    /// <param name="targetNode">The skill execution node whose mapping should be null.</param>
    private void SetupNullMapping(SkillExecutionNode targetNode)
    {
        _mockNodeAgentMapper
            .Setup(m => m.MapAsync(targetNode))
            .Returns(Task.FromResult<(Skill DomainSkill, Agent DomainAgent, IRuntimeAgent Agent)?>(null));
    }

    #endregion
}