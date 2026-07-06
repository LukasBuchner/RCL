using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests specifically for skill matching behavior in ConfigurableDummyRuntimeAgent.
///     Tests ID-based matching vs name-based fallback scenarios.
/// </summary>
public class SkillMatchingTests
{
    private readonly ConfigurableDummyRuntimeAgent _agent;
    private readonly Guid _graspSkillId = Guid.Parse("23456789-abcd-4cde-ef01-23456789abcd");
    private readonly Mock<ILogger<DummyRuntimeAgent>> _mockBaseLogger = new();
    private readonly Mock<ILogger<ConfigurableDummyRuntimeAgent>> _mockLogger = new();

    // Test skill IDs
    private readonly Guid _moveSkillId = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc");

    public SkillMatchingTests()
    {
        var availableSkills = new List<Skill>
        {
            new()
            {
                Id = _moveSkillId,
                Name = "Move Object To Tag",
                Description = "Move an object to a predefined location tag",
                Properties = []
            },
            new()
            {
                Id = _graspSkillId,
                Name = "Grasp Object",
                Description = "Grasp an object",
                Properties = []
            }
        };

        var agentConfig = new DummyAgentConfig
        {
            Id = Guid.NewGuid(),
            Name = "Skill Matching Test Agent",
            Skills =
            [
                new DummySkillConfig
                {
                    Id = _moveSkillId,
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    CanExecuteAdaptively = true,
                    NominalDuration = 12.0,
                    MinAdaptiveDuration = 8.0
                },

                new DummySkillConfig
                {
                    Id = _graspSkillId,
                    Name = "Grasp Object",
                    Description = "Grasp an object",
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0
                },

                new DummySkillConfig
                {
                    // Skill with same name but different ID - should match by name
                    Id = Guid.NewGuid(),
                    Name = "Release Object", // This is only in config, not in available skills
                    Description = "Release an object",
                    CanExecuteAdaptively = false,
                    NominalDuration = 3.0
                }
            ]
        };

        _agent = new ConfigurableDummyRuntimeAgent(
            Guid.NewGuid(),
            "Test Agent",
            availableSkills,
            _mockBaseLogger.Object,
            _mockLogger.Object,
            agentConfig);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_PrioritizesIdMatch_OverNameMatch()
    {
        // Arrange - Create a skill with the exact ID from config
        var skill = new Skill
        {
            Id = _moveSkillId, // Exact ID match
            Name = "Different Name", // Different name to test ID priority
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanExecuteAdaptively); // Should match config for _moveSkillId
        Assert.Equal(12.0, result.EstimatedNominalDuration);
        Assert.Equal(8.0, result.MinAdaptiveDuration);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_FallsBackToNameMatch_WhenIdNotFound()
    {
        // Arrange - Create a skill with different ID but matching name
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Move Object To Tag", // Matching name
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanExecuteAdaptively); // Should match config by name
        Assert.Equal(12.0, result.EstimatedNominalDuration);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_ReturnsNull_WhenNeitherIdNorNameMatch()
    {
        // Arrange - Create a skill that doesn't exist in config
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Unknown Skill", // Unknown name
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        // Should delegate to base agent, which returns null for unknown skills
        Assert.Null(result);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_PrioritizesIdMatch_OverNameMatch()
    {
        // Arrange - Create a skill with exact ID from config
        var skill = new Skill
        {
            Id = _graspSkillId, // Exact ID match (non-adaptive)
            Name = "Different Name", // Different name
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.False(result); // Should match non-adaptive config for _graspSkillId
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_FallsBackToNameMatch_WhenIdNotFound()
    {
        // Arrange - Create a skill with different ID but matching name
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Grasp Object", // Matching name (non-adaptive)
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.False(result); // Should match non-adaptive config by name
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_ReturnsFalse_WhenNeitherIdNorNameMatch()
    {
        // Arrange - Create a skill that doesn't exist in config
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID
            Name = "Unknown Skill", // Unknown name
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Move Object To Tag", true)] // Adaptive skill
    [InlineData("Grasp Object", false)] // Non-adaptive skill
    [InlineData("Unknown Skill", false)] // Unknown skill
    public async Task CanExecuteAdaptivelyAsync_NameMatching_ReturnsCorrectAdaptiveFlag(string skillName,
        bool expectedAdaptive)
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Force name-based matching
            Name = skillName,
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.Equal(expectedAdaptive, result);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithCaseInsensitiveNameMatch_ReturnsEstimate()
    {
        // Arrange - Test case insensitive name matching
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID to force name matching
            Name = "MOVE OBJECT TO TAG", // Different case
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanExecuteAdaptively);
        Assert.Equal(12.0, result.EstimatedNominalDuration);
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithCaseInsensitiveNameMatch_ReturnsCorrectValue()
    {
        // Arrange - Test case insensitive name matching
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID to force name matching
            Name = "grasp object", // Different case, non-adaptive skill
            Description = "Test skill",
            Properties = []
        };

        // Act
        var result = await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.False(result);
    }
}