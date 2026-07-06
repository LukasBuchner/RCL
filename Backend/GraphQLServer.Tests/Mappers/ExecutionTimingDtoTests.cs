using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.GraphQLServer.Types;

namespace FHOOE.Freydis.GraphQLServer.Tests.Mappers;

/// <summary>
///     Unit tests for <see cref="ExecutionTimingDto.FromExecutionTimingInfo" />.
/// </summary>
public class ExecutionTimingDtoTests
{
    [Fact]
    public void FromExecutionTimingInfo_MapsAllFields()
    {
        // Arrange
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var info = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 7.5,
            EstimatedTotalDurationSeconds = 25.0,
            ProgressPercentage = 30.0,
            IsRunning = true
        };

        // Act
        var dto = ExecutionTimingDto.FromExecutionTimingInfo(info);

        // Assert
        Assert.Equal(start, dto.StartTimeUtc);
        Assert.Equal(7.5, dto.CurrentTimeSeconds);
        Assert.Equal(25.0, dto.EstimatedTotalDurationSeconds);
        Assert.Equal(start.AddSeconds(25.0), dto.EstimatedEndTimeUtc);
        Assert.Equal(30.0, dto.ProgressPercentage);
        Assert.True(dto.IsRunning);
    }

    [Fact]
    public void FromExecutionTimingInfo_NotRunning_MapsCorrectly()
    {
        // Arrange
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var info = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 20.0,
            EstimatedTotalDurationSeconds = 20.0,
            ProgressPercentage = 100.0,
            IsRunning = false
        };

        // Act
        var dto = ExecutionTimingDto.FromExecutionTimingInfo(info);

        // Assert
        Assert.Equal(100.0, dto.ProgressPercentage);
        Assert.False(dto.IsRunning);
        Assert.Equal(start.AddSeconds(20.0), dto.EstimatedEndTimeUtc);
    }
}