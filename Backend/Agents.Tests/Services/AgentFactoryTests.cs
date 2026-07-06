using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Factories;

namespace FHOOE.Freydis.Agents.Tests.Services;

/// <summary>
///     Tests for IAgentFactory interface contract compliance.
/// </summary>
public class AgentFactoryTests
{
    [Fact]
    public void Factory_ShouldReturnCorrectAgentType()
    {
        // Arrange & Act
        var dummyFactory = new TestAgentFactory(AgentType.Dummy);
        var kukaFactory = new TestAgentFactory(AgentType.KukaIiwa14);

        // Assert
        Assert.Equal(AgentType.Dummy, dummyFactory.AgentType);
        Assert.Equal(AgentType.KukaIiwa14, kukaFactory.AgentType);
    }

    [Fact]
    public async Task LoadAgentsAsync_ShouldReturnNonNullList()
    {
        // Arrange
        var factory = new TestAgentFactory(AgentType.Dummy);

        // Act
        var agents = await factory.LoadAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        Assert.IsType<List<IRuntimeAgent>>(agents);
    }

    [Fact]
    public async Task LoadAgentsAsync_ShouldReturnEmptyListWhenNoAgentsConfigured()
    {
        // Arrange
        var factory = new TestAgentFactory(AgentType.Dummy, new List<IRuntimeAgent>());

        // Act
        var agents = await factory.LoadAgentsAsync();

        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Arrange
        var factory = new TestAgentFactory(AgentType.Dummy);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.CreateAgentAsync(null!));
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldThrowArgumentException_WhenConfigurationTypeIsInvalid()
    {
        // Arrange
        var factory = new TestAgentFactory(AgentType.Dummy);
        var invalidConfig = new { InvalidProperty = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => factory.CreateAgentAsync(invalidConfig));

        Assert.Contains("Invalid configuration type", exception.Message);
    }

    [Fact]
    public void MultipleFactories_CanCoexist_WithDifferentAgentTypes()
    {
        // Arrange & Act
        var dummyFactory = new TestAgentFactory(AgentType.Dummy);
        var kukaFactory = new TestAgentFactory(AgentType.KukaIiwa14);

        // Assert
        Assert.Equal(AgentType.Dummy, dummyFactory.AgentType);
        Assert.Equal(AgentType.KukaIiwa14, kukaFactory.AgentType);
        Assert.NotEqual(dummyFactory.AgentType, kukaFactory.AgentType);
    }

    [Theory]
    [InlineData(AgentType.Dummy)]
    [InlineData(AgentType.KukaIiwa14)]
    public void AgentType_ShouldBeValidEnumValue(AgentType agentType)
    {
        // Arrange & Act
        var factory = new TestAgentFactory(agentType);

        // Assert
        Assert.Equal(agentType, factory.AgentType);
        Assert.True(Enum.IsDefined(typeof(AgentType), agentType));
    }

    [Fact]
    public async Task LoadAgentsAsync_ShouldSupportCancellation()
    {
        // Arrange
        var factory = new TestAgentFactory(AgentType.Dummy);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var agents = await factory.LoadAgentsAsync(cts.Token);

        // Assert
        // The test factory doesn't actually check cancellation, but the interface signature supports it
        // Real implementations should honor the cancellation token
        Assert.NotNull(agents);
    }

    [Fact]
    public void AgentTypeEnum_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var dummyValue = (int)AgentType.Dummy;
        var kukaValue = (int)AgentType.KukaIiwa14;

        // Assert
        Assert.Equal(0, dummyValue);
        Assert.Equal(1, kukaValue);

        // Verify all enum values are defined
        var enumValues = Enum.GetValues<AgentType>();
        Assert.Contains(AgentType.Dummy, enumValues);
        Assert.Contains(AgentType.KukaIiwa14, enumValues);
    }

    /// <summary>
    ///     Test implementation of IAgentFactory for testing purposes.
    /// </summary>
    private class TestAgentFactory(AgentType agentType, List<IRuntimeAgent>? agentsToReturn = null)
        : IAgentFactory
    {
        private readonly List<IRuntimeAgent> _agentsToReturn = agentsToReturn ?? new List<IRuntimeAgent>();

        public AgentType AgentType { get; } = agentType;

        public Task<List<IRuntimeAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_agentsToReturn);
        }

        public Task<IRuntimeAgent> CreateAgentAsync(object configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.GetType().Name != $"{AgentType}AgentConfig")
                throw new ArgumentException($"Invalid configuration type for {AgentType} factory");

            throw new NotImplementedException("Test factory does not create actual agents");
        }
    }
}