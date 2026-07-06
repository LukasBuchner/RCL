using FHOOE.Freydis.Application.Services.Execution.Utilities;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Utilities;

/// <summary>
///     Unit tests for ExecutionTimeCalculator service.
/// </summary>
public class ExecutionTimeCalculatorTests
{
    private readonly ExecutionTimeCalculator _calculator = new();

    [Fact]
    public void CalculateElapsedSeconds_WithValidTimes_ReturnsCorrectElapsed()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, TimeSpan.Zero);
        var currentTime = new DateTimeOffset(2025, 10, 7, 10, 0, 5, TimeSpan.Zero);

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.Equal(5.0, elapsed);
    }

    [Fact]
    public void CalculateElapsedSeconds_WithZeroElapsed_ReturnsZero()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, TimeSpan.Zero);
        var currentTime = startTime;

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.Equal(0.0, elapsed);
    }

    [Fact]
    public void CalculateElapsedSeconds_WithLargeElapsed_ReturnsCorrectValue()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, TimeSpan.Zero);
        var currentTime = new DateTimeOffset(2025, 10, 7, 10, 5, 30, TimeSpan.Zero);

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.Equal(330.0, elapsed); // 5 minutes 30 seconds = 330 seconds
    }

    [Fact]
    public void CalculateElapsedSeconds_WithMilliseconds_ReturnsAccurateValue()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, 0, TimeSpan.Zero);
        var currentTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, 500, TimeSpan.Zero);

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.Equal(0.5, elapsed, 3); // 500ms = 0.5 seconds
    }

    [Fact]
    public void CalculateElapsedSeconds_WithDifferentTimeZones_ReturnsCorrectElapsed()
    {
        // Arrange - Both times represent the same instant, just different time zones
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, TimeSpan.FromHours(-5)); // EST
        var currentTime = new DateTimeOffset(2025, 10, 7, 16, 0, 10, TimeSpan.FromHours(1)); // CET

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        // Both represent the same UTC time + 10 seconds
        Assert.Equal(10.0, elapsed);
    }

    [Fact]
    public void CalculateElapsedSeconds_WithNegativeElapsed_ReturnsNegativeValue()
    {
        // Test edge case where currentTime is before startTime
        // This shouldn't happen in practice, but the method should handle it

        // Arrange
        var startTime = new DateTimeOffset(2025, 10, 7, 10, 0, 5, TimeSpan.Zero);
        var currentTime = new DateTimeOffset(2025, 10, 7, 10, 0, 0, TimeSpan.Zero);

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.Equal(-5.0, elapsed); // Negative elapsed time
    }

    [Fact]
    public void CalculateElapsedSeconds_WithDefaultStartTime_HandlesGracefully()
    {
        // Test edge case where startTime is default value

        // Arrange
        var startTime = default(DateTimeOffset);
        var currentTime = new DateTimeOffset(2025, 10, 7, 10, 0, 5, TimeSpan.Zero);

        // Act
        var elapsed = _calculator.CalculateElapsedSeconds(startTime, currentTime);

        // Assert
        Assert.True(elapsed > 0); // Should return a large positive number
    }
}