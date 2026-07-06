using FHOOE.Freydis.Application.Services.Execution.Monitoring;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Monitoring;

/// <summary>
///     Unit tests for <see cref="ExecutionTimingInfo" />.
/// </summary>
public class ExecutionTimingInfoTests
{
    [Fact]
    public void EstimatedEndTimeUtc_ComputedCorrectly()
    {
        // Arrange
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var info = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 30.0,
            ProgressPercentage = 16.67,
            IsRunning = true
        };

        // Act
        var expected = start.AddSeconds(30.0);

        // Assert
        Assert.Equal(expected, info.EstimatedEndTimeUtc);
    }

    [Fact]
    public void EstimatedEndTimeUtc_WithZeroDuration_EqualsStartTime()
    {
        // Arrange
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var info = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 0.0,
            EstimatedTotalDurationSeconds = 0.0,
            ProgressPercentage = 0.0,
            IsRunning = false
        };

        // Assert
        Assert.Equal(start, info.EstimatedEndTimeUtc);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var a = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 20.0,
            ProgressPercentage = 25.0,
            IsRunning = true
        };
        var b = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 20.0,
            ProgressPercentage = 25.0,
            IsRunning = true
        };

        // Assert
        Assert.Equal(a, b);
    }
}