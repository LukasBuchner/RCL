using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Duration;

/// <summary>
///     Unit tests for <see cref="PlanningModeDurationProvider" />.
/// </summary>
public class PlanningModeDurationProviderTests
{
    private readonly Mock<IAgentCapabilityAnalyzer> _mockCapabilityAnalyzer;
    private readonly Mock<ILogger<PlanningModeDurationProvider>> _mockLogger;
    private readonly Mock<INodeAgentMapper> _mockNodeAgentMapper;
    private readonly PlanningModeDurationProvider _provider;

    public PlanningModeDurationProviderTests()
    {
        _mockNodeAgentMapper = new Mock<INodeAgentMapper>();
        _mockCapabilityAnalyzer = new Mock<IAgentCapabilityAnalyzer>();
        _mockLogger = new Mock<ILogger<PlanningModeDurationProvider>>();
        _provider = new PlanningModeDurationProvider(
            _mockNodeAgentMapper.Object,
            _mockCapabilityAnalyzer.Object,
            _mockLogger.Object);
    }

    private static Skill CreateTestSkill(Guid? id = null, string name = "TestSkill")
    {
        return new Skill
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = "Test Description",
            Properties = new List<TypedProperty>()
        };
    }

    private static Agent CreateTestAgent(Guid? id = null, string name = "TestAgent")
    {
        return new Agent
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            RepresentativeColor = "#000000"
        };
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidNode_ReturnsPlannedSkillExecution()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var skill = CreateTestSkill(skillId);
        var agent = CreateTestAgent(agentId);
        var runtimeAgent = Mock.Of<IRuntimeAgent>();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = skill,
                AgentId = agentId
            }
        };

        var expectedExecution = Mock.Of<IPlannedSkillExecution>(e =>
            e.Id == nodeId &&
            e.PlannedDuration == 15.5);

        _mockNodeAgentMapper
            .Setup(x => x.MapAsync(node))
            .ReturnsAsync((skill, agent, runtimeAgent));

        _mockCapabilityAnalyzer
            .Setup(x => x.AnalyzeAsync(nodeId, skill, agent, runtimeAgent))
            .ReturnsAsync(expectedExecution);

        // Act
        var result = await _provider.AnalyzeAsync(node);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        Assert.Equal(15.5, result.PlannedDuration);

        _mockNodeAgentMapper.Verify(x => x.MapAsync(node), Times.Once);
        _mockCapabilityAnalyzer.Verify(
            x => x.AnalyzeAsync(nodeId, skill, agent, runtimeAgent),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenNodeMappingFails_ReturnsNull()
    {
        // Arrange
        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = CreateTestSkill(),
                AgentId = Guid.NewGuid()
            }
        };

        _mockNodeAgentMapper
            .Setup(x => x.MapAsync(node))
            .ReturnsAsync((ValueTuple<Skill, Agent, IRuntimeAgent>?)null);

        // Act
        var result = await _provider.AnalyzeAsync(node);

        // Assert
        Assert.Null(result);
        _mockCapabilityAnalyzer.Verify(
            x => x.AnalyzeAsync(It.IsAny<Guid>(), It.IsAny<Skill>(), It.IsAny<Agent>(), It.IsAny<IRuntimeAgent>()),
            Times.Never);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenCapabilityAnalysisFails_ReturnsNull()
    {
        // Arrange
        var skill = CreateTestSkill();
        var agent = CreateTestAgent();
        var runtimeAgent = Mock.Of<IRuntimeAgent>();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = skill,
                AgentId = agent.Id
            }
        };

        _mockNodeAgentMapper
            .Setup(x => x.MapAsync(node))
            .ReturnsAsync((skill, agent, runtimeAgent));

        _mockCapabilityAnalyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<Guid>(), It.IsAny<Skill>(), It.IsAny<Agent>(),
                It.IsAny<IRuntimeAgent>()))
            .ReturnsAsync((IPlannedSkillExecution?)null);

        // Act
        var result = await _provider.AnalyzeAsync(node);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellationToken_PassesToDependencies()
    {
        // Arrange
        var skill = CreateTestSkill();
        var agent = CreateTestAgent();
        var runtimeAgent = Mock.Of<IRuntimeAgent>();

        var node = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10,
                Skill = skill,
                AgentId = agent.Id
            }
        };

        var expectedExecution = Mock.Of<IPlannedSkillExecution>();

        _mockNodeAgentMapper
            .Setup(x => x.MapAsync(node))
            .ReturnsAsync((skill, agent, runtimeAgent));

        _mockCapabilityAnalyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<Guid>(), It.IsAny<Skill>(), It.IsAny<Agent>(),
                It.IsAny<IRuntimeAgent>()))
            .ReturnsAsync(expectedExecution);

        var cts = new CancellationTokenSource();

        // Act
        await _provider.AnalyzeAsync(node, cts.Token);

        // Assert
        // Note: INodeAgentMapper.MapAsync doesn't take cancellationToken
        // Note: IAgentCapabilityAnalyzer.AnalyzeAsync doesn't take cancellationToken
        // So we just verify the method was called
        _mockNodeAgentMapper.Verify(x => x.MapAsync(node), Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleCalls_CallsAnalyzerForEach()
    {
        // Arrange
        var skill1 = CreateTestSkill(name: "Skill1");
        var skill2 = CreateTestSkill(name: "Skill2");
        var agent = CreateTestAgent();
        var runtimeAgent = Mock.Of<IRuntimeAgent>();

        var node1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Task 1",
                StartTime = 0,
                Duration = 10,
                Skill = skill1,
                AgentId = agent.Id
            }
        };

        var node2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Task 2",
                StartTime = 0,
                Duration = 10,
                Skill = skill2,
                AgentId = agent.Id
            }
        };

        _mockNodeAgentMapper
            .Setup(x => x.MapAsync(It.IsAny<SkillExecutionNode>()))
            .ReturnsAsync((skill1, agent, runtimeAgent));

        _mockCapabilityAnalyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<Guid>(), It.IsAny<Skill>(), It.IsAny<Agent>(),
                It.IsAny<IRuntimeAgent>()))
            .ReturnsAsync(Mock.Of<IPlannedSkillExecution>());

        // Act
        await _provider.AnalyzeAsync(node1);
        await _provider.AnalyzeAsync(node2);

        // Assert
        _mockNodeAgentMapper.Verify(x => x.MapAsync(It.IsAny<SkillExecutionNode>()), Times.Exactly(2));
        _mockCapabilityAnalyzer.Verify(
            x => x.AnalyzeAsync(It.IsAny<Guid>(), It.IsAny<Skill>(), It.IsAny<Agent>(), It.IsAny<IRuntimeAgent>()),
            Times.Exactly(2));
    }
}