using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Kuka;
using FHOOE.Freydis.Agents.Agents.Kuka.Configuration;
using FHOOE.Freydis.Agents.Agents.Kuka.Services;
using FHOOE.Freydis.Agents.Services.Factories;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests for <see cref="KukaAgentFactory" /> to verify KUKA agent creation and configuration loading.
/// </summary>
public class KukaAgentFactoryTests
{
    private readonly KukaAgentFactory _factory;
    private readonly Mock<ILogger<KukaAgentFactory>> _mockLogger = new();
    private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();
    private readonly Mock<ISceneEntityProvider> _mockSceneEntityProvider = new();
    private readonly Mock<ISkillDefinitionProvider> _mockSkillDefinitionProvider = new();

    public KukaAgentFactoryTests()
    {
        // Setup logger factory mocks
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

        _factory = new KukaAgentFactory(
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockSceneEntityProvider.Object,
            _mockSkillDefinitionProvider.Object);
    }

    #region LoadAgentsAsync Tests

    [Fact]
    public async Task LoadAgentsAsync_WithoutConfigurationPath_ReturnsEmptyList()
    {
        // Act
        var agents = await _factory.LoadAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }

    #endregion

    #region Skill Resolution Tests

    [Fact]
    public async Task CreateAgentAsync_WithSkillProperties_ResolvesPoseProperties()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skillDefinition = new SkillDefinition
        {
            Id = skillId,
            Name = "Move To Pose",
            Description = "Move to specific pose",
            Properties =
            [
                new DummyPropertyConfig
                {
                    Name = "X",
                    Type = "Number",
                    Value = 100.0
                },
                new DummyPropertyConfig
                {
                    Name = "Y",
                    Type = "Number",
                    Value = 200.0
                },
                new DummyPropertyConfig
                {
                    Name = "Z",
                    Type = "Number",
                    Value = 300.0
                }
            ]
        };

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                { skillId, skillDefinition }
            });

        var agentConfig = new KukaIiwa14AgentConfig
        {
            Name = "KUKA_Pose_Test",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Skills =
            [
                new KukaSkillConfig
                {
                    SkillDefinitionId = skillId,
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0
                }
            ]
        };

        // Act
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        var skills = await agent.GetAvailableSkillsAsync();
        Assert.Single(skills);
        var skill = skills[0];
        Assert.Equal(3, skill.Properties.Count);
        Assert.Contains(skill.Properties, p => p.Name == "X");
        Assert.Contains(skill.Properties, p => p.Name == "Y");
        Assert.Contains(skill.Properties, p => p.Name == "Z");
    }

    #endregion

    #region Multiple Agents Tests

    [Fact]
    public async Task CreateFromJsonFileAsync_WithMultipleAgents_CreatesAll()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skillDefinition = new SkillDefinition
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test skill",
            Properties = []
        };

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                { skillId, skillDefinition }
            });

        var configuration = new KukaIiwa14AgentConfiguration
        {
            Agents =
            [
                new KukaIiwa14AgentConfig
                {
                    Name = "KUKA_1",
                    OpcUaEndpoint = "opc.tcp://localhost:4841",
                    Skills =
                    [
                        new KukaSkillConfig
                        {
                            SkillDefinitionId = skillId,
                            CanExecuteAdaptively = false,
                            NominalDuration = 5.0
                        }
                    ]
                },
                new KukaIiwa14AgentConfig
                {
                    Name = "KUKA_2",
                    OpcUaEndpoint = "opc.tcp://localhost:4842",
                    Skills =
                    [
                        new KukaSkillConfig
                        {
                            SkillDefinitionId = skillId,
                            CanExecuteAdaptively = false,
                            NominalDuration = 6.0
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(configuration);
        var tempFile = Path.Combine(Path.GetTempPath(), $"kuka-multi-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var agents = await _factory.CreateFromJsonFileAsync(tempFile);

            // Assert
            Assert.NotNull(agents);
            Assert.Equal(2, agents.Count);
            Assert.Contains(agents, a => a.Name == "KUKA_1");
            Assert.Contains(agents, a => a.Name == "KUKA_2");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region IAgentFactory Interface Compliance Tests

    [Fact]
    public void Factory_ShouldImplementIAgentFactory()
    {
        // Assert
        Assert.IsAssignableFrom<IAgentFactory>(_factory);
    }

    [Fact]
    public void Factory_AgentType_ShouldReturnKukaIiwa14()
    {
        // Act
        var agentType = _factory.AgentType;

        // Assert
        Assert.Equal(AgentType.KukaIiwa14, agentType);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KukaAgentFactory(
                null!,
                _mockLoggerFactory.Object,
                _mockSceneEntityProvider.Object,
                _mockSkillDefinitionProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KukaAgentFactory(
                _mockLogger.Object,
                null!,
                _mockSceneEntityProvider.Object,
                _mockSkillDefinitionProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullSceneEntityProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KukaAgentFactory(
                _mockLogger.Object,
                _mockLoggerFactory.Object,
                null!,
                _mockSkillDefinitionProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullSkillDefinitionProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new KukaAgentFactory(
                _mockLogger.Object,
                _mockLoggerFactory.Object,
                _mockSceneEntityProvider.Object,
                null!));
    }

    #endregion

    #region CreateAgentAsync Tests

    [Fact]
    public async Task CreateAgentAsync_WithValidConfig_CreatesKukaIiwa14RuntimeAgent()
    {
        // Arrange
        var skillDefinitionId = Guid.NewGuid();
        var skillDefinition = new SkillDefinition
        {
            Id = skillDefinitionId,
            Name = "Move To Position",
            Description = "Move robot to target position",
            Properties = []
        };

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                { skillDefinitionId, skillDefinition }
            });

        var agentConfig = new KukaIiwa14AgentConfig
        {
            Name = "KUKA_Test_Agent",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Description = "Test KUKA robot",
            Skills =
            [
                new KukaSkillConfig
                {
                    SkillDefinitionId = skillDefinitionId,
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0
                }
            ]
        };

        // Act
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(agent);
        Assert.IsType<KukaIiwa14RuntimeAgent>(agent);
        Assert.Equal("KUKA_Test_Agent", agent.Name);

        var skills = await agent.GetAvailableSkillsAsync();
        Assert.Single(skills);
        Assert.Equal("Move To Position", skills[0].Name);
    }

    [Fact]
    public async Task CreateAgentAsync_WithInvalidConfigType_ThrowsArgumentException()
    {
        // Arrange
        var invalidConfig = new { Name = "Invalid" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => ((IAgentFactory)_factory).CreateAgentAsync(invalidConfig));
    }

    [Fact]
    public async Task CreateAgentAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IAgentFactory)_factory).CreateAgentAsync(null!));
    }

    [Fact]
    public async Task CreateAgentAsync_GeneratesIdWhenNotProvided()
    {
        // Arrange
        var agentConfig = new KukaIiwa14AgentConfig
        {
            Name = "KUKA_Without_ID",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Skills = []
        };

        // Act
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(agent);
        Assert.NotEqual(Guid.Empty, agent.Id);
        Assert.Equal("KUKA_Without_ID", agent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_UsesProvidedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var agentConfig = new KukaIiwa14AgentConfig
        {
            Id = expectedId,
            Name = "KUKA_With_ID",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Skills = []
        };

        // Act
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(expectedId, agent.Id);
        Assert.Equal("KUKA_With_ID", agent.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_ResolvesSkillsFromProvider()
    {
        // Arrange
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();

        var skill1 = new SkillDefinition
        {
            Id = skillId1,
            Name = "Pick",
            Description = "Pick object",
            Properties = []
        };

        var skill2 = new SkillDefinition
        {
            Id = skillId2,
            Name = "Place",
            Description = "Place object",
            Properties = []
        };

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                { skillId1, skill1 },
                { skillId2, skill2 }
            });

        var agentConfig = new KukaIiwa14AgentConfig
        {
            Name = "KUKA_Multi_Skill",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Skills =
            [
                new KukaSkillConfig
                {
                    SkillDefinitionId = skillId1,
                    CanExecuteAdaptively = false,
                    NominalDuration = 3.0
                },
                new KukaSkillConfig
                {
                    SkillDefinitionId = skillId2,
                    CanExecuteAdaptively = false,
                    NominalDuration = 4.0
                }
            ]
        };

        // Act
        var agent = await ((IAgentFactory)_factory).CreateAgentAsync(agentConfig);

        // Assert
        var skills = await agent.GetAvailableSkillsAsync();
        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "Pick");
        Assert.Contains(skills, s => s.Name == "Place");
    }

    [Fact]
    public async Task CreateAgentAsync_WithMissingSkillDefinition_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentSkillId = Guid.NewGuid();

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>());

        var agentConfig = new KukaIiwa14AgentConfig
        {
            Name = "KUKA_Missing_Skill",
            OpcUaEndpoint = "opc.tcp://localhost:4840",
            Skills =
            [
                new KukaSkillConfig
                {
                    SkillDefinitionId = nonExistentSkillId,
                    CanExecuteAdaptively = false,
                    NominalDuration = 5.0
                }
            ]
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IAgentFactory)_factory).CreateAgentAsync(agentConfig));
    }

    #endregion

    #region CreateFromJsonFileAsync Tests

    [Fact]
    public async Task CreateFromJsonFileAsync_WithValidFile_CreatesAgents()
    {
        // Arrange
        var skillDefinitionId = Guid.NewGuid();
        var skillDefinition = new SkillDefinition
        {
            Id = skillDefinitionId,
            Name = "Move To Position",
            Description = "Move robot to target position",
            Properties = []
        };

        _mockSkillDefinitionProvider.Setup(p => p.GetSkillDefinitionsAsync())
            .ReturnsAsync(new Dictionary<Guid, SkillDefinition>
            {
                { skillDefinitionId, skillDefinition }
            });

        var configuration = new KukaIiwa14AgentConfiguration
        {
            Agents =
            [
                new KukaIiwa14AgentConfig
                {
                    Id = Guid.NewGuid(),
                    Name = "KUKA_From_File",
                    OpcUaEndpoint = "opc.tcp://localhost:4840",
                    Description = "Test KUKA from file",
                    Skills =
                    [
                        new KukaSkillConfig
                        {
                            SkillDefinitionId = skillDefinitionId,
                            CanExecuteAdaptively = false,
                            NominalDuration = 6.0
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(configuration);
        var tempFile = Path.Combine(Path.GetTempPath(), $"kuka-test-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var agents = await _factory.CreateFromJsonFileAsync(tempFile);

            // Assert
            Assert.NotNull(agents);
            Assert.Single(agents);
            Assert.Equal("KUKA_From_File", agents[0].Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CreateFromJsonFileAsync_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "/path/to/nonexistent/file.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _factory.CreateFromJsonFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task CreateFromJsonFileAsync_WithEmptyConfiguration_ReturnsEmptyList()
    {
        // Arrange
        var configuration = new KukaIiwa14AgentConfiguration
        {
            Agents = []
        };

        var json = JsonSerializer.Serialize(configuration);
        var tempFile = Path.Combine(Path.GetTempPath(), $"kuka-empty-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var agents = await _factory.CreateFromJsonFileAsync(tempFile);

            // Assert
            Assert.NotNull(agents);
            Assert.Empty(agents);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CreateFromJsonFileAsync_WithInvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var tempFile = Path.Combine(Path.GetTempPath(), $"kuka-invalid-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tempFile, invalidJson);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _factory.CreateFromJsonFileAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}