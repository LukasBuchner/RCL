using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Planning;

/// <summary>
///     Unit tests for <see cref="SchedulePlanner" />.
/// </summary>
public class SchedulePlannerTests
{
    private readonly Mock<ILogger<SchedulePlanner>> _loggerMock;
    private readonly SchedulePlanner _schedulePlanner;

    public SchedulePlannerTests()
    {
        _loggerMock = new Mock<ILogger<SchedulePlanner>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _schedulePlanner = new SchedulePlanner(_loggerMock.Object);
    }

    [Fact]
    public void Plan_WithDefaultCurrentTime_CallsPlanScheduleWithZero()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        mockGraph.Setup(g => g.SkillExecutions).Returns([]);
        mockGraph.Setup(g => g.Dependencies).Returns([]);

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object);

        // Assert
        Assert.True(result);
        // Verify PlanSchedule was called (indirectly through the extension method)
    }

    [Fact]
    public void Plan_WithCurrentTime10_PassesCurrentTimeToSchedule()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        var skillExecution = CreateMockSkillExecution();
        mockGraph.Setup(g => g.SkillExecutions).Returns([skillExecution.Object]);
        mockGraph.Setup(g => g.Dependencies).Returns([]);

        const double currentTime = 10.0;

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object, currentTime);

        // Assert
        Assert.True(result);
        // After planning with currentTime=10, a not-started task should start >= currentTime
        Assert.True(skillExecution.Object.PlannedStartTime >= currentTime);
    }

    [Fact]
    public void Plan_WithCurrentTime25_PassesCurrentTimeToSchedule()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        var skillExecution = CreateMockSkillExecution(plannedDuration: 5.0);
        mockGraph.Setup(g => g.SkillExecutions).Returns([skillExecution.Object]);
        mockGraph.Setup(g => g.Dependencies).Returns([]);

        const double currentTime = 25.0;

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object, currentTime);

        // Assert
        Assert.True(result);
        // After planning with currentTime=25, a not-started task should start at currentTime
        Assert.Equal(25.0, skillExecution.Object.PlannedStartTime, 5);
        Assert.Equal(5.0, skillExecution.Object.PlannedDuration, 5);
        Assert.Equal(30.0, skillExecution.Object.PlannedFinishTime, 5);
    }

    [Fact]
    public void Plan_WithMultipleTasks_RespectsCurrentTime()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        var taskA = CreateMockSkillExecution(Guid.NewGuid(), 3.0);
        var taskB = CreateMockSkillExecution(Guid.NewGuid(), 2.0);

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = taskA.Object,
            Target = taskB.Object,
            Type = DependencyType.FinishToStart
        };

        mockGraph.Setup(g => g.SkillExecutions).Returns([taskA.Object, taskB.Object]);
        mockGraph.Setup(g => g.Dependencies).Returns([dependency]);

        const double currentTime = 15.0;

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object, currentTime);

        // Assert
        Assert.True(result);
        // Task A starts at currentTime=15
        Assert.Equal(15.0, taskA.Object.PlannedStartTime, 5);
        Assert.Equal(3.0, taskA.Object.PlannedDuration, 5);
        Assert.Equal(18.0, taskA.Object.PlannedFinishTime, 5);

        // Task B starts after A finishes (18.0)
        Assert.Equal(18.0, taskB.Object.PlannedStartTime, 5);
        Assert.Equal(2.0, taskB.Object.PlannedDuration, 5);
        Assert.Equal(20.0, taskB.Object.PlannedFinishTime, 5);
    }

    [Fact]
    public void Plan_WithNegativeCurrentTime_ReturnsFalse()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        mockGraph.Setup(g => g.SkillExecutions).Returns([]);
        mockGraph.Setup(g => g.Dependencies).Returns([]);

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object, -5.0);

        // Assert
        Assert.False(result);
        // Verify error was logged (the exception is caught inside Plan and logged)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Plan_WhenPlanScheduleThrowsException_ReturnsFalseAndLogsError()
    {
        // Arrange
        var mockGraph = new Mock<IExecutionGraph>();
        // Create a scenario that will cause an exception in PlanSchedule (e.g., fixed cycle)
        var taskA = CreateMockSkillExecution(Guid.NewGuid());
        var taskB = CreateMockSkillExecution(Guid.NewGuid());

        // Create a cycle
        var depAb = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = taskA.Object,
            Target = taskB.Object,
            Type = DependencyType.FinishToStart
        };
        var depBa = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = taskB.Object,
            Target = taskA.Object,
            Type = DependencyType.FinishToStart
        };

        mockGraph.Setup(g => g.SkillExecutions).Returns([taskA.Object, taskB.Object]);
        mockGraph.Setup(g => g.Dependencies).Returns([depAb, depBa]);

        // Act
        var result = _schedulePlanner.Plan(mockGraph.Object, 10.0);

        // Assert
        Assert.False(result);
        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Helper method to create a mock IPlannedSkillExecution for testing.
    /// </summary>
    private static Mock<IPlannedSkillExecution> CreateMockSkillExecution(
        Guid? id = null,
        double plannedDuration = 1.0)
    {
        var mock = new Mock<IPlannedSkillExecution>();
        mock.SetupGet(t => t.Id).Returns(id ?? Guid.NewGuid());
        mock.SetupProperty(t => t.PlannedDuration, plannedDuration);
        mock.SetupProperty(t => t.PlannedStartTime, 0);
        mock.SetupProperty(t => t.PlannedFinishTime, 0);
        return mock;
    }
}