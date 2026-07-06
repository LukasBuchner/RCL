using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Layer-2 integration tests that wire a real <see cref="NodeAgentMapper" /> to a real
///     <see cref="DummyRuntimeAgent" /> through mocked data sources. Verifies that the
///     mapper resolves the canonical "Move To Position" skill node to a Dummy runtime
///     agent whose <see cref="DummyRuntimeAgent.CanExecuteAdaptivelyAsync" /> reports
///     non-adaptive — the upstream signal that Rule 10 of <c>DependencyEdgeValidator</c>
///     consumes when rejecting finish-side dependencies.
/// </summary>
public class NodeAgentMapperAdaptivePredicateTests
{
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DummyRuntimeAgent _dummyAgent;
    private readonly Agent _domainAgent;
    private readonly NodeAgentMapper _mapper;
    private readonly Mock<IAgentApplicationService> _mockAgentAppService = new();
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider = new();
    private readonly Skill _moveToPositionSkill;

    /// <summary>
    ///     Sets up Bob as a Dummy runtime agent named "Bob" whose available-skills list
    ///     contains a single "Move To Position" skill. The same skill is also recorded in
    ///     Bob's domain <see cref="Agent.SkillIds" /> so the procedural plan and the
    ///     runtime advertise consistent capabilities.
    /// </summary>
    public NodeAgentMapperAdaptivePredicateTests()
    {
        _moveToPositionSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Moves robot to a target pose",
            Properties = []
        };

        _domainAgent = new Agent
        {
            Id = _agentId,
            Name = "Bob",
            RepresentativeColor = "#000000",
            SkillIds = [_moveToPositionSkill.Id],
            State = AgentState.Active
        };

        _dummyAgent = new DummyRuntimeAgent(
            _agentId,
            "Bob",
            [_moveToPositionSkill],
            Mock.Of<ILogger<DummyRuntimeAgent>>());

        _mockAgentAppService
            .Setup(s => s.GetAgentByIdAsync(_agentId))
            .ReturnsAsync(_domainAgent);

        _mapper = new NodeAgentMapper(
            _mockAgentProvider.Object,
            NullLogger<NodeAgentMapper>.Instance,
            _mockAgentAppService.Object);
    }

    /// <summary>
    ///     Verifies that the mapper returns the tuple containing Bob and his real Dummy
    ///     runtime agent for a "Move To Position" skill node, and that the runtime predicate
    ///     reports non-adaptive — confirming the validator's input contract.
    /// </summary>
    [Fact]
    public async Task MapAsync_MoveToPositionSkillNodeAssignedToBob_ReturnsTupleWhoseAgentIsNonAdaptive()
    {
        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(_agentId)).Returns(_dummyAgent);
        var skillNode = MakeSkillNode(_moveToPositionSkill, _agentId);

        var mapping = await _mapper.MapAsync(skillNode);

        Assert.NotNull(mapping);
        Assert.Same(_moveToPositionSkill, mapping.Value.DomainSkill);
        Assert.Same(_domainAgent, mapping.Value.DomainAgent);
        Assert.Same(_dummyAgent, mapping.Value.Agent);

        var canAdapt = await mapping.Value.Agent.CanExecuteAdaptivelyAsync(_moveToPositionSkill);
        Assert.False(canAdapt);
    }

    /// <summary>
    ///     Verifies that the mapper returns null when Bob is registered as a domain agent
    ///     but is not currently online (no runtime agent registered for his id).
    /// </summary>
    [Fact]
    public async Task MapAsync_BobOffline_ReturnsNull()
    {
        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(_agentId)).Returns((IRuntimeAgent?)null);
        var skillNode = MakeSkillNode(_moveToPositionSkill, _agentId);

        var mapping = await _mapper.MapAsync(skillNode);

        Assert.Null(mapping);
    }

    /// <summary>
    ///     Verifies that the mapper returns null when the assigned <c>AgentId</c> does not
    ///     match any domain agent.
    /// </summary>
    [Fact]
    public async Task MapAsync_UnknownAgentId_ReturnsNull()
    {
        var unknownAgentId = Guid.NewGuid();
        _mockAgentAppService
            .Setup(s => s.GetAgentByIdAsync(unknownAgentId))
            .ReturnsAsync((Agent?)null);
        var skillNode = MakeSkillNode(_moveToPositionSkill, unknownAgentId);

        var mapping = await _mapper.MapAsync(skillNode);

        Assert.Null(mapping);
    }

    private static SkillExecutionNode MakeSkillNode(Skill skill, Guid agentId)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move",
                StartTime = 0,
                Duration = 1,
                Skill = skill,
                AgentId = agentId
            }
        };
    }
}