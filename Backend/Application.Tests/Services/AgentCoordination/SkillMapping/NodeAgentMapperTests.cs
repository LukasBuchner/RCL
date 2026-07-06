using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Tests for <see cref="NodeAgentMapper" />, verifying that runtime agent lookups
///     always reflect the current agent manager state, including dynamically connected agents.
/// </summary>
public class NodeAgentMapperTests
{
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Agent _domainAgent;
    private readonly NodeAgentMapper _mapper;
    private readonly Mock<IAgentApplicationService> _mockAgentAppService;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<IRuntimeAgent> _mockRuntimeAgent;
    private readonly Skill _skill;

    public NodeAgentMapperTests()
    {
        _skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Moves robot to a target pose",
            Properties = []
        };

        _domainAgent = new Agent
        {
            Id = _agentId,
            Name = "DigitalTwin-Robot1",
            RepresentativeColor = "#FF0000",
            SkillIds = [_skill.Id],
            State = AgentState.Active
        };

        _mockAgentAppService = new Mock<IAgentApplicationService>();
        _mockAgentAppService
            .Setup(s => s.GetAgentByIdAsync(_agentId))
            .ReturnsAsync(_domainAgent);

        _mockRuntimeAgent = new Mock<IRuntimeAgent>();
        _mockRuntimeAgent.Setup(a => a.Id).Returns(_agentId);
        _mockRuntimeAgent.Setup(a => a.Name).Returns("DigitalTwin-Robot1");

        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();

        _mapper = new NodeAgentMapper(
            _mockAgentProvider.Object,
            NullLogger<NodeAgentMapper>.Instance,
            _mockAgentAppService.Object);
    }

    /// <summary>
    ///     Creates a <see cref="SkillExecutionNode" /> assigned to the test agent and skill.
    /// </summary>
    private SkillExecutionNode CreateSkillNode()
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move To Position",
                StartTime = 0,
                Duration = 5,
                Skill = _skill,
                AgentId = _agentId
            }
        };
    }

    /// <summary>
    ///     Verifies that a dynamically registered agent (e.g., a Digital Twin connecting via WebSocket
    ///     after startup) is correctly resolved by the mapper. This was the root cause of issue #40:
    ///     the mapper used a stale DI snapshot instead of querying the live agent provider.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task MapAsync_DynamicallyRegisteredAgent_ReturnsMapping()
    {
        // Arrange — agent is available via the provider (simulates DT connected via WebSocket)
        _mockAgentProvider
            .Setup(p => p.GetRuntimeAgent(_agentId))
            .Returns(_mockRuntimeAgent.Object);

        var node = CreateSkillNode();

        // Act
        var result = await _mapper.MapAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_skill, result.Value.DomainSkill);
        Assert.Equal(_domainAgent, result.Value.DomainAgent);
        Assert.Equal(_mockRuntimeAgent.Object, result.Value.Agent);
    }

    /// <summary>
    ///     Verifies that the mapper returns null when no runtime agent is registered for the node's agent ID.
    ///     This represents the case where a Digital Twin agent is not connected.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task MapAsync_NoRuntimeAgent_ReturnsNull()
    {
        // Arrange — no runtime agent registered (DT not connected)
        _mockAgentProvider
            .Setup(p => p.GetRuntimeAgent(_agentId))
            .Returns((IRuntimeAgent?)null);

        var node = CreateSkillNode();

        // Act
        var result = await _mapper.MapAsync(node);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Verifies that non-skill-execution nodes (e.g., TaskNode, RouterNode) are ignored.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task MapAsync_NonSkillExecutionNode_ReturnsNull()
    {
        var taskNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Some Task", StartTime = 0, Duration = 5 }
        };

        var result = await _mapper.MapAsync(taskNode);

        Assert.Null(result);
    }

    /// <summary>
    ///     Verifies that the mapper returns null when the domain agent is not found in the database.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task MapAsync_NoDomainAgent_ReturnsNull()
    {
        var unknownAgentId = Guid.NewGuid();
        _mockAgentAppService
            .Setup(s => s.GetAgentByIdAsync(unknownAgentId))
            .ReturnsAsync((Agent?)null);

        var node = new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move To Position",
                StartTime = 0,
                Duration = 5,
                Skill = _skill,
                AgentId = unknownAgentId
            }
        };

        var result = await _mapper.MapAsync(node);

        Assert.Null(result);
    }
}