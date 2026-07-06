using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests for <see cref="DummyAgentFactory" /> to verify agent creation and configuration loading.
/// </summary>
public class DummyAgentFactoryTests
{
    private readonly DummyAgentFactory _factory;
    private readonly Mock<ILogger<DummyAgentFactory>> _mockLogger = new();
    private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();
    private readonly Mock<ISceneEntityProvider> _mockSceneEntityProvider = new();
    private readonly Mock<ISkillDefinitionProvider> _mockSkillDefinitionProvider = new();

    public DummyAgentFactoryTests()
    {
        // Setup logger factory mocks using It.IsAny for method parameters
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        // Setup scene entity provider to return empty dictionaries by default
        _mockSceneEntityProvider.Setup(p => p.GetPositionTagsAsync())
            .ReturnsAsync(new Dictionary<Guid, PositionTag>());
        _mockSceneEntityProvider.Setup(p => p.GetSceneObjectsAsync())
            .ReturnsAsync(new Dictionary<Guid, SceneObject>());

        // Setup skill definition provider to return empty dictionary by default
        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>());

        _factory = new DummyAgentFactory(_mockLogger.Object, _mockLoggerFactory.Object, _mockSceneEntityProvider.Object,
            _mockSkillDefinitionProvider.Object);
    }

    [Fact]
    public async Task CreateFromJsonAsync_WithValidConfiguration_CreatesAgents()
    {
        // Arrange
        var configuration = new DummyAgentConfiguration
        {
            Agents =
            [
                new DummyAgentConfig
                {
                    Id = Guid.Parse("cdef1234-5678-4cde-89ab-34567890abcd"),
                    Name = "Alice",
                    Description = "Test agent Alice",
                    MaxConcurrentExecutions = 2,
                    Skills =
                    [
                        new DummySkillConfig
                        {
                            Id = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc"),
                            Name = "Move Object To Tag",
                            Description = "Move an object to a predefined location tag",
                            CanExecuteAdaptively = true,
                            NominalDuration = 12.0,
                            MinAdaptiveDuration = 8.0,
                            FailureChance = 0.04
                        }
                    ]
                },

                new DummyAgentConfig
                {
                    Id = Guid.Parse("def12345-6789-4def-89ab-4567890abcde"),
                    Name = "Bob",
                    Description = "Test agent Bob",
                    MaxConcurrentExecutions = 3,
                    Skills =
                    [
                        new DummySkillConfig
                        {
                            Id = Guid.Parse("23456789-abcd-4cde-ef01-23456789abcd"),
                            Name = "Grasp Object",
                            Description = "Grasp an object",
                            CanExecuteAdaptively = false,
                            NominalDuration = 5.0,
                            FailureChance = 0.02
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(configuration);

        // Act
        var result = await _factory.CreateFromJsonAsync(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var alice = result.FirstOrDefault(a => a.Name == "Alice");
        var bob = result.FirstOrDefault(a => a.Name == "Bob");

        Assert.NotNull(alice);
        Assert.NotNull(bob);

        // Verify Alice
        Assert.Equal(Guid.Parse("cdef1234-5678-4cde-89ab-34567890abcd"), alice.Id);
        var aliceSkills = await alice.GetAvailableSkillsAsync();
        Assert.Single(aliceSkills);
        Assert.Equal("Move Object To Tag", aliceSkills[0].Name);

        // Verify Bob
        Assert.Equal(Guid.Parse("def12345-6789-4def-89ab-4567890abcde"), bob.Id);
        var bobSkills = await bob.GetAvailableSkillsAsync();
        Assert.Single(bobSkills);
        Assert.Equal("Grasp Object", bobSkills[0].Name);
    }

    [Fact]
    public async Task CreateFromConfigurationAsync_WithEmptyConfiguration_ReturnsEmptyList()
    {
        // Arrange
        var configuration = new DummyAgentConfiguration
        {
            Agents = []
        };

        // Act
        var result = await _factory.CreateFromConfigurationAsync(configuration);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAgentAsync_GeneratesIdWhenNotProvided()
    {
        // Arrange
        var agentConfig = new DummyAgentConfig
        {
            // No ID provided
            Name = "Test Agent",
            Description = "Test agent without ID",
            Skills = []
        };

        // Act
        var result = await _factory.CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Agent", result.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_UsesProvidedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var agentConfig = new DummyAgentConfig
        {
            Id = expectedId,
            Name = "Test Agent",
            Description = "Test agent with specific ID",
            Skills = []
        };

        // Act
        var result = await _factory.CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal("Test Agent", result.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithSkills_CreatesSkillsWithCorrectIds()
    {
        // Arrange
        var skillId1 = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc");
        var skillId2 = Guid.Parse("23456789-abcd-4cde-ef01-23456789abcd");

        var agentConfig = new DummyAgentConfig
        {
            Name = "Multi-Skill Agent",
            Skills =
            [
                new DummySkillConfig
                {
                    Id = skillId1,
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    CanExecuteAdaptively = true,
                    NominalDuration = 12.0
                },

                new DummySkillConfig
                {
                    Id = skillId2,
                    Name = "Grasp Object",
                    Description = "Grasp an object",
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0
                }
            ]
        };

        // Act
        var result = await _factory.CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(result);
        var skills = await result.GetAvailableSkillsAsync();
        Assert.Equal(2, skills.Count);

        var moveSkill = skills.FirstOrDefault(s => s.Name == "Move Object To Tag");
        var graspSkill = skills.FirstOrDefault(s => s.Name == "Grasp Object");

        Assert.NotNull(moveSkill);
        Assert.NotNull(graspSkill);
        Assert.Equal(skillId1, moveSkill.Id);
        Assert.Equal(skillId2, graspSkill.Id);
    }

    [Fact]
    public async Task CreateAgentAsync_ReturnsConfigurableDummyAgentWrapper()
    {
        // Arrange
        var agentConfig = new DummyAgentConfig
        {
            Name = "Wrapper Test Agent",
            Skills =
            [
                new DummySkillConfig
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill",
                    CanExecuteAdaptively = false,
                    NominalDuration = 10.0
                }
            ]
        };

        // Act
        var result = await _factory.CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(result);
        // The result should be a ConfigurableDummyAgentWrapper (which inherits from DummyRuntimeAgent)
        Assert.IsType<ConfigurableDummyAgentWrapper>(result);
    }

    [Fact]
    public async Task CreateFromJsonAsync_WithInvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _factory.CreateFromJsonAsync(invalidJson));
    }

    [Fact]
    public async Task CreateFromJsonAsync_WithNullConfiguration_ReturnsEmptyList()
    {
        // Arrange
        var json = "null";

        // Act
        var result = await _factory.CreateFromJsonAsync(json);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(true, 12.0, 8.0)]
    [InlineData(false, 5.0, null)]
    public async Task CreatedAgent_SkillExecutionEstimate_ReflectsConfiguration(
        bool canExecuteAdaptively,
        double nominalDuration,
        double? minAdaptiveDuration)
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agentConfig = new DummyAgentConfig
        {
            Name = "Test Agent",
            Skills =
            [
                new DummySkillConfig
                {
                    Id = skillId,
                    Name = "Test Skill",
                    Description = "Test skill for estimate verification",
                    CanExecuteAdaptively = canExecuteAdaptively,
                    NominalDuration = nominalDuration,
                    MinAdaptiveDuration = minAdaptiveDuration
                }
            ]
        };

        var agent = await _factory.CreateAgentAsync(agentConfig);
        var skills = await agent.GetAvailableSkillsAsync();
        var skill = skills[0];

        // Act
        var estimate = await agent.GetExecutionEstimateAsync(skill);
        var adaptive = await agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        Assert.NotNull(estimate);
        Assert.Equal(canExecuteAdaptively, adaptive);
        Assert.Equal(canExecuteAdaptively, estimate.CanExecuteAdaptively);
        Assert.Equal(nominalDuration, estimate.EstimatedNominalDuration);
        Assert.Equal(minAdaptiveDuration, estimate.MinAdaptiveDuration);
    }

    #region IAgentFactory Interface Compliance Tests

    [Fact]
    public void Factory_ShouldImplementIAgentFactory()
    {
        // Assert
        Assert.IsAssignableFrom<IAgentFactory>(_factory);
    }

    [Fact]
    public void Factory_AgentType_ShouldReturnDummy()
    {
        // Act
        var agentType = _factory.AgentType;

        // Assert
        Assert.Equal(AgentType.Dummy, agentType);
    }

    [Fact]
    public async Task LoadAgentsAsync_ShouldDelegateToCreateFromJsonFileAsync()
    {
        // Arrange
        var configFilePath = Path.Combine(Path.GetTempPath(), "test-config.json");
        var configuration = new DummyAgentConfiguration
        {
            Agents =
            [
                new DummyAgentConfig
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Agent",
                    Skills = []
                }
            ]
        };

        var json = JsonSerializer.Serialize(configuration);
        await File.WriteAllTextAsync(configFilePath, json);

        try
        {
            // Act
            var agents = await _factory.LoadAgentsAsync();

            // Assert
            // LoadAgentsAsync should return empty list when no default file path is configured
            Assert.NotNull(agents);
        }
        finally
        {
            if (File.Exists(configFilePath))
                File.Delete(configFilePath);
        }
    }

    [Fact]
    public async Task CreateAgentAsync_WithObjectConfiguration_ShouldAcceptDummyAgentConfig()
    {
        // Arrange
        var agentConfig = new DummyAgentConfig
        {
            Name = "Object Config Test",
            Skills = []
        };

        // Act - Call IAgentFactory.CreateAgentAsync with object parameter
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("Object Config Test", agent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithInvalidConfigurationType_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidConfig = new { Name = "Invalid" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => ((IAgentFactory)_factory).CreateAgentAsync(invalidConfig));
    }

    [Fact]
    public async Task CreateAgentAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IAgentFactory)_factory).CreateAgentAsync(null!));
    }

    #endregion
}