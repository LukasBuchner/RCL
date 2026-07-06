using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests for <see cref="ConfigurableDummyRuntimeAgent" /> to verify skill matching and logging behavior.
/// </summary>
public class ConfigurableDummyRuntimeAgentTests
{
    private readonly ConfigurableDummyRuntimeAgent _agent;
    private readonly List<Skill> _availableSkills;
    private readonly Mock<ILogger<DummyRuntimeAgent>> _mockBaseLogger = new();
    private readonly Mock<ILogger<ConfigurableDummyRuntimeAgent>> _mockLogger = new();

    public ConfigurableDummyRuntimeAgentTests()
    {
        // Create test skills with specific IDs
        var skillId1 = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc");
        var skillId2 = Guid.Parse("23456789-abcd-4cde-ef01-23456789abcd");

        _availableSkills =
        [
            new Skill
            {
                Id = skillId1,
                Name = "Move Object To Tag",
                Description = "Move an object to a predefined location tag",
                Properties =
                [
                    new TypedProperty
                    {
                        Name = "NominalDuration",
                        Value = TypedValue.Number(10.0),
                        Direction = PropertyDirection.Input
                    }
                ]
            },

            new Skill
            {
                Id = skillId2,
                Name = "Grasp Object",
                Description = "Grasp an object",
                Properties =
                [
                    new TypedProperty
                    {
                        Name = "NominalDuration",
                        Value = TypedValue.Number(5.0),
                        Direction = PropertyDirection.Input
                    }
                ]
            }
        ];

        var agentConfig = new DummyAgentConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            Description = "Test agent for unit tests",
            MaxConcurrentExecutions = 2,
            Skills =
            [
                new DummySkillConfig
                {
                    Id = skillId1,
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    CanExecuteAdaptively = true,
                    NominalDuration = 12.0,
                    MinAdaptiveDuration = 8.0,
                    FailureChance = 0.04
                },

                new DummySkillConfig
                {
                    Id = skillId2,
                    Name = "Grasp Object",
                    Description = "Grasp an object",
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0,
                    FailureChance = 0.02
                }
            ]
        };

        _agent = new ConfigurableDummyRuntimeAgent(
            Guid.NewGuid(),
            "Test Agent",
            _availableSkills,
            _mockBaseLogger.Object,
            _mockLogger.Object,
            agentConfig);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithMatchingSkillId_ReturnsEstimate()
    {
        // Arrange
        var skill = _availableSkills[0]; // Move Object To Tag

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill, result.Skill);
        Assert.Equal(_agent.Id, result.AgentId);
        Assert.True(result.CanExecuteAdaptively);
        Assert.Equal(12.0, result.EstimatedNominalDuration);
        Assert.Equal(8.0, result.MinAdaptiveDuration);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithMatchingSkillName_ReturnsEstimate()
    {
        // Arrange - Create a skill with different ID but same name
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Move Object To Tag", // Same name
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill, result.Skill);
        Assert.Equal(_agent.Id, result.AgentId);
        Assert.True(result.CanExecuteAdaptively);
        Assert.Equal(12.0, result.EstimatedNominalDuration);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithNonAdaptiveSkill_ReturnsFixedEstimate()
    {
        // Arrange
        var skill = _availableSkills[1]; // Grasp Object (non-adaptive)

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(skill, result.Skill);
        Assert.Equal(_agent.Id, result.AgentId);
        Assert.False(result.CanExecuteAdaptively);
        Assert.Equal(5.0, result.EstimatedNominalDuration);
        Assert.Null(result.MinAdaptiveDuration);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithUnknownSkill_DelegatesToBase()
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
        var result = await _agent.GetExecutionEstimateAsync(unknownSkill);

        // Assert
        // Should delegate to base agent, which will return null for unknown skills
        Assert.Null(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithAdaptiveSkillId_ReturnsTrue()
    {
        // Arrange
        var skill = _availableSkills[0]; // Move Object To Tag (adaptive)

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithNonAdaptiveSkillId_ReturnsFalse()
    {
        // Arrange
        var skill = _availableSkills[1]; // Grasp Object (non-adaptive)

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithMatchingSkillName_ReturnsCorrectValue()
    {
        // Arrange - Create a skill with different ID but same name as adaptive skill
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Move Object To Tag", // Same name as adaptive skill
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.True(result);
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
        var result = await _agent.CanExecuteAdaptivelyAsync(unknownSkill);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Id_ReturnsBaseAgentId()
    {
        // Act & Assert
        Assert.NotEqual(Guid.Empty, _agent.Id);
    }

    [Fact]
    public void Name_ReturnsBaseAgentName()
    {
        // Act & Assert
        Assert.Equal("Test Agent", _agent.Name);
    }

    [Fact]
    public async Task GetAvailableSkillsAsync_ReturnsBaseAgentSkills()
    {
        // Act
        var result = await _agent.GetAvailableSkillsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Name == "Move Object To Tag");
        Assert.Contains(result, s => s.Name == "Grasp Object");
    }
}