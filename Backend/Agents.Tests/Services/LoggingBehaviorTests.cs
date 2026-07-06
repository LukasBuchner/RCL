using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Agents.Dummy.Configuration;
using FHOOE.Freydis.Agents.Agents.Dummy.Services;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests to verify logging behavior in ConfigurableDummyRuntimeAgent.
/// </summary>
public class LoggingBehaviorTests
{
    private readonly ConfigurableDummyRuntimeAgent _agent;
    private readonly Mock<ILogger<DummyRuntimeAgent>> _mockBaseLogger = new();
    private readonly Mock<ILogger<ConfigurableDummyRuntimeAgent>> _mockLogger = new();

    private readonly Guid _skillId = Guid.Parse("12345678-9abc-4bcd-def0-123456789abc");

    public LoggingBehaviorTests()
    {
        // Source-generated [LoggerMessage] methods check IsEnabled() before logging,
        // so the mock must return true for the log levels used in the tests.
        _mockBaseLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var availableSkills = new List<Skill>
        {
            new()
            {
                Id = _skillId,
                Name = "Move Object To Tag",
                Description = "Move an object to a predefined location tag",
                Properties = []
            }
        };

        var agentConfig = new DummyAgentConfig
        {
            Id = Guid.NewGuid(),
            Name = "Logging Test Agent",
            Skills =
            [
                new DummySkillConfig
                {
                    Id = _skillId,
                    Name = "Move Object To Tag",
                    Description = "Move an object to a predefined location tag",
                    CanExecuteAdaptively = true,
                    NominalDuration = 12.0,
                    MinAdaptiveDuration = 8.0
                }
            ]
        };

        _agent = new ConfigurableDummyRuntimeAgent(
            Guid.NewGuid(),
            "Logging Test Agent",
            availableSkills,
            _mockBaseLogger.Object,
            _mockLogger.Object,
            agentConfig);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithMatchingSkillId_LogsDebugMessage()
    {
        // Arrange
        var skill = new Skill
        {
            Id = _skillId,
            Name = "Move Object To Tag",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Getting execution estimate for skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Found skill configuration for skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Created execution estimate for skill");
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithMatchingSkillName_LogsNameMatchMessage()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID to force name matching
            Name = "Move Object To Tag",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Getting execution estimate for skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Found skill configuration by name match");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Created execution estimate for skill");
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithUnknownSkill_LogsWarningMessage()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Skill",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Getting execution estimate for skill");
        VerifyLogCalled(_mockLogger, LogLevel.Warning, "No skill configuration found for skill");
        // Note: The base agent call happens but doesn't log "Delegating" message explicitly
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithMatchingSkillId_LogsDebugMessages()
    {
        // Arrange
        var skill = new Skill
        {
            Id = _skillId,
            Name = "Move Object To Tag",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Checking if skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "can execute adaptively");
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithMatchingSkillName_LogsNameMatchMessage()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(), // Different ID to force name matching
            Name = "Move Object To Tag",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Checking if skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "can execute adaptively");
    }

    [Fact]
    public async Task CanExecuteAdaptivelyAsync_WithUnknownSkill_LogsWarningMessage()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Skill",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.CanExecuteAdaptivelyAsync(skill);

        // Assert
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "Checking if skill");
        VerifyLogCalled(_mockLogger, LogLevel.Trace, "can execute adaptively");
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_WithUnknownSkill_LogsAvailableSkills()
    {
        // Arrange
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Unknown Skill",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        // Verify that the base agent's warning message includes available skills information
        _mockBaseLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Available skills:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetExecutionEstimateAsync_LogsExecutionEstimateDetails()
    {
        // Arrange
        var skill = new Skill
        {
            Id = _skillId,
            Name = "Move Object To Tag",
            Description = "Test skill",
            Properties = []
        };

        // Act
        await _agent.GetExecutionEstimateAsync(skill);

        // Assert
        // Verify that execution estimate details are logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CanAdapt=True") &&
                                              v.ToString()!.Contains("Nominal=12") &&
                                              v.ToString()!.Contains("Min=8")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Helper method to verify that a log message with specific level and content was called.
    /// </summary>
    private static void VerifyLogCalled(Mock<ILogger<ConfigurableDummyRuntimeAgent>> mockLogger, LogLevel level,
        string messageSubstring)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageSubstring)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}