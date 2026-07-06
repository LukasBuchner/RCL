using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests for <see cref="ConfigurableDummyAgentWrapper" /> to verify delegation behavior.
/// </summary>
public class ConfigurableDummyAgentWrapperTests
{
    private readonly DummyAgentConfig _agentConfig;
    private readonly List<Skill> _availableSkills;
    private readonly ConfigurableDummyRuntimeAgent _configurableAgent;
    private readonly Mock<ILogger<DummyRuntimeAgent>> _mockBaseLogger = new();
    private readonly Mock<ILogger<ConfigurableDummyRuntimeAgent>> _mockConfigurableLogger = new();
    private readonly ConfigurableDummyAgentWrapper _wrapper;

    public ConfigurableDummyAgentWrapperTests()
    {
        // Create test skills with specific IDs
        var skillId = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc");

        _availableSkills =
        [
            new Skill
            {
                Id = skillId,
                Name = "Move Object To Tag",
                Description = "Move an object to a predefined location tag",
                Properties = []
            }
        ];

        _agentConfig = new DummyAgentConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test Wrapper Agent",
            Description = "Test agent for wrapper tests",
            MaxConcurrentExecutions = 2,
            Skills =
            [
                new DummySkillConfig
                {
                    Id = skillId,
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    CanExecuteAdaptively = true,
                    NominalDuration = 12.0,
                    MinAdaptiveDuration = 8.0,
                    FailureChance = 0.04
                }
            ]
        };

        _configurableAgent = new ConfigurableDummyRuntimeAgent(
            Guid.NewGuid(),
            "Test Configurable Agent",
            _availableSkills,
            _mockBaseLogger.Object,
            _mockConfigurableLogger.Object,
            _agentConfig);

        _wrapper = new ConfigurableDummyAgentWrapper(
            Guid.NewGuid(),
            "Test Wrapper Agent",
            _availableSkills,
            _mockBaseLogger.Object,
            _configurableAgent);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_DelegatesToConfigurableAgent()
    {
        // Arrange
        var skill = _availableSkills[0];

        // Act
        var result = await _wrapper.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill, result.Skill);
        Assert.True(result.CanExecuteAdaptively);
        Assert.Equal(12.0, result.EstimatedNominalDuration);
        Assert.Equal(8.0, result.MinAdaptiveDuration);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_DelegatesToConfigurableAgent()
    {
        // Arrange
        var skill = _availableSkills[0]; // Adaptive skill

        // Act
        var result = await _wrapper.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithUnknownSkill_ReturnsNull()
    {
        // Arrange
        var unknownSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Skill",
            Description = "This skill is not in the configuration",
            Properties = []
        };

        // Act
        var result = await _wrapper.GetExecutionEstimateAsync(unknownSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithUnknownSkill_ReturnsFalse()
    {
        // Arrange
        var unknownSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Skill",
            Description = "This skill is not in the configuration",
            Properties = []
        };

        // Act
        var result = await _wrapper.CanExecuteAdaptivelyAsync(unknownSkill);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InheritsFromDummyRuntimeAgent()
    {
        // Assert
        Assert.IsAssignableFrom<DummyRuntimeAgent>(_wrapper);
        Assert.IsAssignableFrom<IRuntimeAgent>(_wrapper);
    }

    [Fact]
    public void HasCorrectAgentProperties()
    {
        // Assert
        Assert.NotEqual(Guid.Empty, _wrapper.Id);
        Assert.Equal("Test Wrapper Agent", _wrapper.Name);
    }

    [Fact]
    public async Task GetAvailableSkillsAsync_ReturnsBaseAgentSkills()
    {
        // Act
        var result = await _wrapper.GetAvailableSkillsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Move Object To Tag", result[0].Name);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsHealthStatus()
    {
        // Act
        var result = await _wrapper.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAvailable);
        Assert.Equal(0, result.ActiveExecutions);
    }
}