using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Unit tests for <see cref="DurationProviderFactory" />.
/// </summary>
public class DurationProviderFactoryTests
{
    private readonly DurationProviderFactory _factory;
    private readonly Mock<ILogger<ExecutionAwareDurationProvider>> _mockExecutionLogger;
    private readonly PlanningModeDurationProvider _planningProvider;

    public DurationProviderFactoryTests()
    {
        // Create a real PlanningModeDurationProvider with mocked dependencies
        var mockNodeAgentMapper = new Mock<INodeAgentMapper>();
        var mockCapabilityAnalyzer = new Mock<IAgentCapabilityAnalyzer>();
        var mockPlanningLogger = new Mock<ILogger<PlanningModeDurationProvider>>();

        _planningProvider = new PlanningModeDurationProvider(
            mockNodeAgentMapper.Object,
            mockCapabilityAnalyzer.Object,
            mockPlanningLogger.Object);

        _mockExecutionLogger = new Mock<ILogger<ExecutionAwareDurationProvider>>();
        _factory = new DurationProviderFactory(_planningProvider, _mockExecutionLogger.Object);
    }

    [Fact]
    public void CreateDurationProvider_WithPlanningMode_ReturnsPlanningModeDurationProvider()
    {
        // Arrange
        var isExecutionMode = false;
        DateTime? procedureStartTimeUtc = null;
        IReadOnlyDictionary<Guid, SkillExecutionProgress>? executionProgressData = null;

        // Act
        var result = _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PlanningModeDurationProvider>(result);
        Assert.Same(_planningProvider, result);
    }

    [Fact]
    public void CreateDurationProvider_WithExecutionMode_ReturnsExecutionAwareDurationProvider()
    {
        // Arrange
        var isExecutionMode = true;
        var procedureStartTimeUtc = DateTime.UtcNow;
        var executionProgressData = new Dictionary<Guid, SkillExecutionProgress>();

        // Act
        var result = _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ExecutionAwareDurationProvider>(result);
    }

    [Fact]
    public void CreateDurationProvider_WithExecutionModeButNullStartTime_ThrowsArgumentNullException()
    {
        // Arrange
        var isExecutionMode = true;
        DateTime? procedureStartTimeUtc = null;
        var executionProgressData = new Dictionary<Guid, SkillExecutionProgress>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData));

        Assert.Equal("procedureStartTimeUtc", exception.ParamName);
    }

    [Fact]
    public void CreateDurationProvider_WithExecutionModeButNullProgressData_ThrowsArgumentNullException()
    {
        // Arrange
        var isExecutionMode = true;
        var procedureStartTimeUtc = DateTime.UtcNow;
        IReadOnlyDictionary<Guid, SkillExecutionProgress>? executionProgressData = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData));

        Assert.Equal("executionProgressData", exception.ParamName);
    }

    [Fact]
    public void CreateDurationProvider_WithExecutionMode_CreatesProviderWithCorrectDependencies()
    {
        // Arrange
        var isExecutionMode = true;
        var procedureStartTimeUtc = DateTime.UtcNow;
        var nodeId = Guid.NewGuid();
        var executionProgressData = new Dictionary<Guid, SkillExecutionProgress>
        {
            {
                nodeId,
                new SkillExecutionProgress
                {
                    ExecutionId = nodeId,
                    SkillId = Guid.NewGuid(),
                    AgentId = Guid.NewGuid(),
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = 5.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = "In Progress"
                }
            }
        };

        // Act
        var result = _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData);

        // Assert
        Assert.NotNull(result);
        var executionProvider = Assert.IsType<ExecutionAwareDurationProvider>(result);
        // Verify the provider was created (we can't easily inspect internal state, but we can verify type and that it doesn't throw)
        Assert.NotNull(executionProvider);
    }

    [Fact]
    public void CreateDurationProvider_MultipleCalls_ReturnsNewInstancesEachTime()
    {
        // Arrange
        var isExecutionMode = true;
        var procedureStartTimeUtc = DateTime.UtcNow;
        var executionProgressData = new Dictionary<Guid, SkillExecutionProgress>();

        // Act
        var result1 = _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData);
        var result2 = _factory.CreateDurationProvider(isExecutionMode, procedureStartTimeUtc, executionProgressData);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }
}