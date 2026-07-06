using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Timing;

/// <summary>
///     Unit tests for TimingAggregator service.
/// </summary>
public class TimingAggregatorTests
{
    private readonly TimingAggregator _aggregator;
    private readonly Mock<ILogger<TimingAggregator>> _mockLogger;

    public TimingAggregatorTests()
    {
        _mockLogger = new Mock<ILogger<TimingAggregator>>();
        _aggregator = new TimingAggregator(_mockLogger.Object);
    }

    [Fact]
    public void AggregateTimings_WithValidTimings_ReturnsCorrectAggregation()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>
        {
            (10.0, 0.0, 10.0),
            (15.0, 5.0, 20.0),
            (8.0, 2.0, 10.0)
        };

        // Act
        var result = _aggregator.AggregateTimings(timings);

        // Assert
        Assert.Equal(20.0, result.Duration); // 20.0 - 0.0
        Assert.Equal(0.0, result.StartTime); // min start
        Assert.Equal(20.0, result.FinishTime); // max finish
    }

    [Fact]
    public void AggregateTimings_WithSingleTiming_ReturnsSameTiming()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>
        {
            (10.0, 5.0, 15.0)
        };

        // Act
        var result = _aggregator.AggregateTimings(timings);

        // Assert
        Assert.Equal(10.0, result.Duration); // 15.0 - 5.0
        Assert.Equal(5.0, result.StartTime);
        Assert.Equal(15.0, result.FinishTime);
    }

    [Fact]
    public void AggregateTimings_WithOverlappingTimings_ReturnsCorrectSpan()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>
        {
            (10.0, 0.0, 10.0),
            (10.0, 5.0, 15.0), // overlaps with first
            (5.0, 12.0, 17.0) // overlaps with second
        };

        // Act
        var result = _aggregator.AggregateTimings(timings);

        // Assert
        Assert.Equal(17.0, result.Duration); // 17.0 - 0.0
        Assert.Equal(0.0, result.StartTime);
        Assert.Equal(17.0, result.FinishTime);
    }

    [Fact]
    public void AggregateTimings_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _aggregator.AggregateTimings(timings));
    }

    [Fact]
    public void AggregateTimings_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _aggregator.AggregateTimings(null!));
    }

    [Fact]
    public void TryAggregateTimings_WithValidTimings_ReturnsAggregatedResult()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>
        {
            (10.0, 0.0, 10.0),
            (5.0, 8.0, 13.0)
        };

        // Act
        var result = _aggregator.TryAggregateTimings(timings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(13.0, result.Value.Duration); // 13.0 - 0.0
        Assert.Equal(0.0, result.Value.StartTime);
        Assert.Equal(13.0, result.Value.FinishTime);
    }

    [Fact]
    public void TryAggregateTimings_WithEmptyCollection_ReturnsNull()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _aggregator.TryAggregateTimings(timings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryAggregateTimings_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _aggregator.TryAggregateTimings(null!));
    }

    [Fact]
    public void AggregateTimings_WithNegativeTimings_HandlesCorrectly()
    {
        // Arrange
        var timings = new List<(double Duration, double StartTime, double FinishTime)>
        {
            (5.0, -5.0, 0.0),
            (10.0, -2.0, 8.0)
        };

        // Act
        var result = _aggregator.AggregateTimings(timings);

        // Assert
        Assert.Equal(13.0, result.Duration); // 8.0 - (-5.0)
        Assert.Equal(-5.0, result.StartTime); // min start
        Assert.Equal(8.0, result.FinishTime); // max finish
    }
}