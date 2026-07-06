using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Unit tests for <see cref="AgentCapabilityAnalyzer" />, adaptive vs non-adaptive branches.
/// </summary>
public class AgentCapabilityAnalyzerTests
{
    private readonly AgentCapabilityAnalyzer _analyzer;
    private readonly Mock<ILogger<AgentCapabilityAnalyzer>> _mockLogger = new();

    public AgentCapabilityAnalyzerTests()
    {
        _analyzer = new AgentCapabilityAnalyzer(_mockLogger.Object);
    }

    /// <summary>
    ///     When <see cref="IRuntimeAgent.CanExecuteAdaptivelyAsync" /> is <c>false</c>
    ///     and estimate is non-null, returns <see cref="PlannedSkillExecution" />.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_NonAdaptive_ReturnsFixedExecution()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "S",
            Description = string.Empty,
            Properties = []
        };
        var domainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "DomA",
            SkillIds = [],
            RepresentativeColor = "Green"
        };
        var nodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.SetupGet(a => a.Id).Returns(agentId);
        mockAgent
            .Setup(a => a.CanExecuteAdaptivelyAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var estimate = new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = agentId,
            CanExecuteAdaptively = false,
            EstimatedNominalDuration = 7.5,
            MinAdaptiveDuration = null
        };
        mockAgent
            .Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estimate);

        // Act
        var result = await _analyzer.AnalyzeAsync(nodeId, skill, domainAgent, mockAgent.Object);

        // Assert
        var fixedExec = Assert.IsType<PlannedSkillExecution>(result);
        Assert.Equal(nodeId, fixedExec.Id);
        Assert.Equal(7.5, fixedExec.PlannedDuration);
    }

    /// <summary>
    ///     When <see cref="IRuntimeAgent.CanExecuteAdaptivelyAsync" /> is <c>true</c>
    ///     and the minimum adaptive duration is provided, returns <see cref="PlannedAdaptiveSkillExecution" />.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_Adaptive_ReturnsAdaptiveExecution()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "A",
            Description = string.Empty,
            Properties = []
        };
        var domainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "DomB",
            SkillIds = [],
            RepresentativeColor = "Yellow"
        };
        var nodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.SetupGet(a => a.Id).Returns(agentId);
        mockAgent
            .Setup(a => a.CanExecuteAdaptivelyAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var estimate = new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = agentId,
            CanExecuteAdaptively = true,
            EstimatedNominalDuration = 3.0,
            MinAdaptiveDuration = 1.0
        };
        mockAgent
            .Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estimate);

        // Act
        var result = await _analyzer.AnalyzeAsync(nodeId, skill, domainAgent, mockAgent.Object);

        // Assert
        var adaptive = Assert.IsType<PlannedAdaptiveSkillExecution>(result);
        Assert.Equal(nodeId, adaptive.Id);
        Assert.Equal(1.0, adaptive.MinDuration);
        Assert.Equal(3.0, adaptive.PlannedDuration);
    }

    /// <summary>
    ///     If the estimate is <c>null</c>, returns <c>null</c> regardless of adaptivity.
    /// </summary>
    [Fact]
    public async Task AnalyzeAsync_NoEstimate_ReturnsNull()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "None",
            Description = string.Empty,
            Properties = []
        };
        var domainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "DomC",
            SkillIds = [],
            RepresentativeColor = "Gray"
        };
        var nodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.SetupGet(a => a.Id).Returns(agentId);
        mockAgent
            .Setup(a => a.CanExecuteAdaptivelyAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockAgent
            .Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillExecutionEstimate?)null);

        // Act
        var result = await _analyzer.AnalyzeAsync(nodeId, skill, domainAgent, mockAgent.Object);

        // Assert
        Assert.Null(result);
    }
}