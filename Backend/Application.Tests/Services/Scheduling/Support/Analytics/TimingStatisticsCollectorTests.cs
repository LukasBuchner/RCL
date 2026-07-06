using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Support.Analytics;

/// <summary>
///     Unit tests for TimingStatisticsCollector.
/// </summary>
public class TimingStatisticsCollectorTests
{
    private readonly TimingStatisticsCollector _collector = new();

    [Fact]
    public void CreateStatistics_ShouldReturnEmptyStatistics()
    {
        // Act
        var stats = _collector.CreateStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.SkillExecutionNodesProcessed);
        Assert.Equal(0, stats.TaskNodesProcessed);
        Assert.Equal(TimeSpan.Zero, stats.ExecutionGraphBuildTime);
        Assert.Equal(TimeSpan.Zero, stats.SchedulingTime);
        Assert.Equal(TimeSpan.Zero, stats.DomainUpdateTime);
        Assert.Equal(TimeSpan.Zero, stats.TotalTime);
    }

    [Fact]
    public void UpdateStatistics_WithAllValues_ShouldUpdateAllFields()
    {
        // Arrange
        var baseStats = _collector.CreateStatistics();
        var executionTime = TimeSpan.FromMilliseconds(100);
        var schedulingTime = TimeSpan.FromMilliseconds(200);
        var domainTime = TimeSpan.FromMilliseconds(50);
        var totalTime = TimeSpan.FromMilliseconds(350);

        // Act
        var updatedStats = _collector.UpdateStatistics(
            baseStats,
            5,
            3,
            executionTime,
            schedulingTime,
            domainTime,
            totalTime);

        // Assert
        Assert.Equal(5, updatedStats.SkillExecutionNodesProcessed);
        Assert.Equal(3, updatedStats.TaskNodesProcessed);
        Assert.Equal(executionTime, updatedStats.ExecutionGraphBuildTime);
        Assert.Equal(schedulingTime, updatedStats.SchedulingTime);
        Assert.Equal(domainTime, updatedStats.DomainUpdateTime);
        Assert.Equal(totalTime, updatedStats.TotalTime);
    }

    [Fact]
    public void UpdateStatistics_WithPartialValues_ShouldPreserveExistingValues()
    {
        // Arrange
        var baseStats = _collector.CreateStatistics() with
        {
            ExecutionGraphBuildTime = TimeSpan.FromMilliseconds(100),
            SchedulingTime = TimeSpan.FromMilliseconds(200)
        };

        // Act
        var updatedStats = _collector.UpdateStatistics(
            baseStats,
            10,
            5,
            domainUpdateTime: TimeSpan.FromMilliseconds(75));

        // Assert
        Assert.Equal(10, updatedStats.SkillExecutionNodesProcessed);
        Assert.Equal(5, updatedStats.TaskNodesProcessed);
        Assert.Equal(TimeSpan.FromMilliseconds(100), updatedStats.ExecutionGraphBuildTime); // Preserved
        Assert.Equal(TimeSpan.FromMilliseconds(200), updatedStats.SchedulingTime); // Preserved
        Assert.Equal(TimeSpan.FromMilliseconds(75), updatedStats.DomainUpdateTime); // Updated
    }

    [Fact]
    public void UpdateStatistics_WithNullBaseStats_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _collector.UpdateStatistics(
            null!,
            0,
            0));
    }
}