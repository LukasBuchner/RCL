using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Unit tests for <see cref="UnifiedAgentManager" /> to verify unified agent management functionality.
/// </summary>
public class UnifiedAgentManagerTests
{
    private readonly UnifiedAgentManager _manager;
    private readonly Mock<ILogger<UnifiedAgentManager>> _mockLogger = new();

    public UnifiedAgentManagerTests()
    {
        _manager = new UnifiedAgentManager(_mockLogger.Object);
    }

    #region Thread Safety Tests

    [Fact]
    public async Task Manager_WithConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var agentIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        var mockAgents = agentIds.Select(id =>
        {
            var mockAgent = new Mock<IRuntimeAgent>();
            mockAgent.Setup(a => a.Id).Returns(id);
            mockAgent.Setup(a => a.Name).Returns($"Agent_{id}");
            return mockAgent.Object;
        }).ToList();

        foreach (var agent in mockAgents)
            _manager.RegisterAgent(agent);

        // Act - Concurrent reads
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
            tasks.Add(Task.Run(() => { _manager.GetAgent(agentIds[i % 10]); }));

        await Task.WhenAll(tasks);

        // Assert - No exceptions thrown, state consistent
        Assert.Equal(10, _manager.ActiveAgentCount);
    }

    #endregion

    #region StartAgentAsync Tests

    [Fact]
    public async Task StartAgentAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => _manager.StartAgentAsync(agentId, "NewAgent"));
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitialize()
    {
        // Act
        var manager = new UnifiedAgentManager(_mockLogger.Object);

        // Assert
        Assert.NotNull(manager);
        Assert.Empty(manager.ActiveAgents);
        Assert.Equal(0, manager.ActiveAgentCount);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnifiedAgentManager(null!));
    }

    #endregion

    #region GetAgent Tests

    [Fact]
    public void GetAgent_ByGuid_WithExistingAgent_ReturnsAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        _manager.RegisterAgent(mockAgent.Object);

        // Act
        var result = _manager.GetAgent(agentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agentId, result.Id);
        Assert.Equal("TestAgent", result.Name);
    }

    [Fact]
    public void GetAgent_ByGuid_WithNonExistentAgent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = _manager.GetAgent(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAgent_ByName_WithExistingAgent_ReturnsAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        _manager.RegisterAgent(mockAgent.Object);

        // Act
        var result = _manager.GetAgent("TestAgent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestAgent", result.Name);
    }

    [Fact]
    public void GetAgent_ByName_IsCaseInsensitive()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        _manager.RegisterAgent(mockAgent.Object);

        // Act
        var resultLower = _manager.GetAgent("testagent");
        var resultUpper = _manager.GetAgent("TESTAGENT");
        var resultMixed = _manager.GetAgent("TeStAgEnT");

        // Assert
        Assert.NotNull(resultLower);
        Assert.NotNull(resultUpper);
        Assert.NotNull(resultMixed);
        Assert.Equal("TestAgent", resultLower!.Name);
        Assert.Equal("TestAgent", resultUpper!.Name);
        Assert.Equal("TestAgent", resultMixed!.Name);
    }

    [Fact]
    public void GetAgent_ByName_WithNonExistentAgent_ReturnsNull()
    {
        // Act
        var result = _manager.GetAgent("NonExistent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region StopAgentAsync Tests

    [Fact]
    public async Task StopAgentAsync_WithExistingAgent_RemovesAndReturnsTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        _manager.RegisterAgent(mockAgent.Object);

        Assert.Single(_manager.ActiveAgents);

        // Act
        var result = await _manager.StopAgentAsync(agentId);

        // Assert
        Assert.True(result);
        Assert.Empty(_manager.ActiveAgents);
        Assert.Equal(0, _manager.ActiveAgentCount);
    }

    [Fact]
    public async Task StopAgentAsync_WithNonExistentAgent_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _manager.StopAgentAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAllAgentHealthAsync Tests

    [Fact]
    public async Task GetAllAgentHealthAsync_WithMultipleAgents_ReturnsAllHealth()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();

        var health1 = new AgentHealthStatus
        {
            AgentId = agent1Id,
            AgentName = "Agent1",
            IsHealthy = true,
            IsAvailable = true,
            ActiveExecutions = 0,
            TotalExecutionsCompleted = 5,
            FailedExecutions = 0,
            LastSeenUtc = DateTime.UtcNow,
            StartedUtc = DateTime.UtcNow.AddHours(-1)
        };

        var health2 = new AgentHealthStatus
        {
            AgentId = agent2Id,
            AgentName = "Agent2",
            IsHealthy = true,
            IsAvailable = false,
            ActiveExecutions = 1,
            TotalExecutionsCompleted = 10,
            FailedExecutions = 2,
            LastSeenUtc = DateTime.UtcNow,
            StartedUtc = DateTime.UtcNow.AddHours(-2)
        };

        var mockAgent1 = new Mock<IRuntimeAgent>();
        mockAgent1.Setup(a => a.Id).Returns(agent1Id);
        mockAgent1.Setup(a => a.Name).Returns("Agent1");
        mockAgent1.Setup(a => a.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(health1);

        var mockAgent2 = new Mock<IRuntimeAgent>();
        mockAgent2.Setup(a => a.Id).Returns(agent2Id);
        mockAgent2.Setup(a => a.Name).Returns("Agent2");
        mockAgent2.Setup(a => a.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(health2);

        _manager.RegisterAgent(mockAgent1.Object);
        _manager.RegisterAgent(mockAgent2.Object);

        // Act
        var healthStatuses = await _manager.GetAllAgentHealthAsync();

        // Assert
        Assert.Equal(2, healthStatuses.Count);
        Assert.Contains(healthStatuses, h => h.AgentName == "Agent1");
        Assert.Contains(healthStatuses, h => h.AgentName == "Agent2");
    }

    [Fact]
    public async Task GetAllAgentHealthAsync_WithNoAgents_ReturnsEmptyList()
    {
        // Act
        var healthStatuses = await _manager.GetAllAgentHealthAsync();

        // Assert
        Assert.NotNull(healthStatuses);
        Assert.Empty(healthStatuses);
    }

    #endregion

    #region GetAgentHealthAsync Tests

    [Fact]
    public async Task GetAgentHealthAsync_WithExistingAgent_ReturnsHealth()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        var expectedHealth = new AgentHealthStatus
        {
            AgentId = agentId,
            AgentName = "TestAgent",
            IsHealthy = true,
            IsAvailable = true,
            ActiveExecutions = 0,
            TotalExecutionsCompleted = 5,
            FailedExecutions = 0,
            LastSeenUtc = DateTime.UtcNow,
            StartedUtc = DateTime.UtcNow.AddHours(-1)
        };

        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");
        mockAgent.Setup(a => a.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHealth);

        _manager.RegisterAgent(mockAgent.Object);

        // Act
        var health = await _manager.GetAgentHealthAsync(agentId);

        // Assert
        Assert.NotNull(health);
        Assert.Equal(agentId, health.AgentId);
        Assert.Equal("TestAgent", health.AgentName);
        Assert.True(health.IsHealthy);
    }

    [Fact]
    public async Task GetAgentHealthAsync_WithNonExistentAgent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var health = await _manager.GetAgentHealthAsync(nonExistentId);

        // Assert
        Assert.Null(health);
    }

    #endregion
}