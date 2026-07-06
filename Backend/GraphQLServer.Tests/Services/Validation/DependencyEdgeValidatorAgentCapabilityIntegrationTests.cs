using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Validation;

/// <summary>
///     Layer-3 integration tests that exercise the real <see cref="DependencyEdgeValidator" />
///     wired to a real <see cref="NodeAgentMapper" /> and a real <see cref="DummyRuntimeAgent" />.
///     Only data-source interfaces are mocked. These tests prove that Rule 10 fires
///     end-to-end through the actual agent-resolution chain for the canonical scenario
///     reported by the user: a Dummy agent named "Bob" assigned to a "Move To Position"
///     skill execution node receiving a finish-side dependency.
/// </summary>
public class DependencyEdgeValidatorAgentCapabilityIntegrationTests
{
    private readonly Guid _bobId = Guid.NewGuid();
    private readonly Agent _bobDomainAgent;
    private readonly DummyRuntimeAgent _bobRuntimeAgent;
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();
    private readonly Mock<IAgentApplicationService> _mockAgentAppService = new();
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider = new();

    private readonly Skill _moveToPositionSkill;
    private readonly Skill _moveToPositionAdaptiveSkill;
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _sut;

    /// <summary>
    ///     Wires up the canonical scenario: Bob is a registered Dummy runtime agent whose
    ///     available-skills list contains both the non-adaptive "Move To Position" skill
    ///     and the substring-adaptive "Move To Position Adaptive" skill. The validator is
    ///     constructed with a real <see cref="NodeAgentMapper" /> backed by mocked data
    ///     sources.
    /// </summary>
    public DependencyEdgeValidatorAgentCapabilityIntegrationTests()
    {
        _moveToPositionSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Moves robot to a target pose",
            Properties = []
        };
        _moveToPositionAdaptiveSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position Adaptive",
            Description = "Adaptive variant",
            Properties = []
        };

        _bobDomainAgent = new Agent
        {
            Id = _bobId,
            Name = "Bob",
            RepresentativeColor = "#000000",
            SkillIds = [_moveToPositionSkill.Id, _moveToPositionAdaptiveSkill.Id],
            State = AgentState.Active
        };

        _bobRuntimeAgent = new DummyRuntimeAgent(
            _bobId,
            "Bob",
            [_moveToPositionSkill, _moveToPositionAdaptiveSkill],
            Mock.Of<ILogger<DummyRuntimeAgent>>());

        _mockProcedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureId);
        _mockEdgeService.Setup(s => s.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>());
        _mockAgentAppService.Setup(s => s.GetAgentByIdAsync(_bobId)).ReturnsAsync(_bobDomainAgent);
        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(_bobId)).Returns(_bobRuntimeAgent);

        var nodeAgentMapper = new NodeAgentMapper(
            _mockAgentProvider.Object,
            NullLogger<NodeAgentMapper>.Instance,
            _mockAgentAppService.Object);

        _sut = new DependencyEdgeValidator(
            _mockNodeService.Object,
            _mockEdgeService.Object,
            _mockProcedureContext.Object,
            nodeAgentMapper);
    }

    /// <summary>
    ///     The canonical user scenario: an FF edge into a "Move To Position" skill
    ///     execution node assigned to Dummy Bob is rejected with a message naming the
    ///     agent, the skill, and the substring "adaptive".
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FfEdgeIntoMoveToPositionAssignedToDummyBob_Throws()
    {
        var source = SetupSkillNode(_moveToPositionSkill);
        var target = SetupSkillNode(_moveToPositionSkill);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right"));

        Assert.Contains("Bob", ex.Message);
        Assert.Contains("Move To Position", ex.Message);
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     An SF edge (source on start, target on finish) targeting Bob's "Move To Position"
    ///     node is rejected the same way — only the target handle matters for Rule 10.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_SfEdgeIntoMoveToPositionAssignedToDummyBob_Throws()
    {
        var source = SetupSkillNode(_moveToPositionSkill);
        var target = SetupSkillNode(_moveToPositionSkill);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "left", "right"));

        Assert.Contains("Bob", ex.Message);
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     An FS edge into the same node is accepted because Rule 10 short-circuits when
    ///     the target handle is "left".
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FsEdgeIntoMoveToPositionAssignedToDummyBob_DoesNotThrow()
    {
        var source = SetupSkillNode(_moveToPositionSkill);
        var target = SetupSkillNode(_moveToPositionSkill);

        await _sut.ValidateAsync(source.Id, target.Id, "right", "left");
    }

    /// <summary>
    ///     An FF edge into a "Move To Position Adaptive" node is accepted: the substring
    ///     rule on <c>DummyRuntimeAgent</c> reports the skill as adaptive, so Rule 10
    ///     does not reject.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FfEdgeIntoAdaptiveSkillNodeAssignedToDummyBob_DoesNotThrow()
    {
        var source = SetupSkillNode(_moveToPositionSkill);
        var target = SetupSkillNode(_moveToPositionAdaptiveSkill);

        await _sut.ValidateAsync(source.Id, target.Id, "right", "right");
    }

    /// <summary>
    ///     An FF edge into Bob's "Move To Position" node when Bob is offline (no runtime
    ///     agent currently registered) is rejected with the unassigned/not-online message.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_FfEdgeWhenBobIsOffline_ThrowsOfflineMessage()
    {
        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(_bobId)).Returns((IRuntimeAgent?)null);
        var source = SetupSkillNode(_moveToPositionSkill);
        var target = SetupSkillNode(_moveToPositionSkill);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(source.Id, target.Id, "right", "right"));

        Assert.Contains("online", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private SkillExecutionNode SetupSkillNode(Skill skill)
    {
        var node = new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move",
                StartTime = 0,
                Duration = 1,
                Skill = skill,
                AgentId = _bobId
            }
        };
        _mockNodeService.Setup(s => s.GetNodeByIdAsync(node.Id)).ReturnsAsync(node);
        return node;
    }
}