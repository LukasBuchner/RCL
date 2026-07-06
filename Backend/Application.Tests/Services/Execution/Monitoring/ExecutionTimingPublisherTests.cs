using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Monitoring;

/// <summary>
///     Unit tests for <see cref="ExecutionTimingPublisher" />.
/// </summary>
public class ExecutionTimingPublisherTests
{
    [Fact]
    public async Task TimingUpdates_InitialValue_IsNotRunning()
    {
        // Arrange
        var publisher = new ExecutionTimingPublisher(NullLogger<ExecutionTimingPublisher>.Instance);

        // Act
        var initial = await publisher.TimingUpdates.FirstAsync();

        // Assert
        Assert.False(initial.IsRunning);
        Assert.Equal(0, initial.CurrentTimeSeconds);
        Assert.Equal(0, initial.EstimatedTotalDurationSeconds);
        Assert.Equal(0, initial.ProgressPercentage);
    }

    [Fact]
    public async Task PublishTiming_EmitsToSubscribers()
    {
        // Arrange
        var publisher = new ExecutionTimingPublisher(NullLogger<ExecutionTimingPublisher>.Instance);
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var timing = new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 20.0,
            ProgressPercentage = 25.0,
            IsRunning = true
        };

        // Act
        publisher.PublishTiming(timing);
        var latest = await publisher.TimingUpdates.FirstAsync();

        // Assert
        Assert.Equal(start, latest.StartTimeUtc);
        Assert.Equal(5.0, latest.CurrentTimeSeconds);
        Assert.Equal(20.0, latest.EstimatedTotalDurationSeconds);
        Assert.Equal(25.0, latest.ProgressPercentage);
        Assert.True(latest.IsRunning);
    }

    [Fact]
    public async Task PublishTiming_MultipleUpdates_LatestIsEmitted()
    {
        // Arrange
        var publisher = new ExecutionTimingPublisher(NullLogger<ExecutionTimingPublisher>.Instance);
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act — publish two updates
        publisher.PublishTiming(new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 1.0,
            EstimatedTotalDurationSeconds = 10.0,
            ProgressPercentage = 10.0,
            IsRunning = true
        });

        publisher.PublishTiming(new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 10.0,
            ProgressPercentage = 50.0,
            IsRunning = true
        });

        var latest = await publisher.TimingUpdates.FirstAsync();

        // Assert — should be the second update (BehaviorSubject replays latest)
        Assert.Equal(5.0, latest.CurrentTimeSeconds);
        Assert.Equal(50.0, latest.ProgressPercentage);
    }

    [Fact]
    public void PublishTiming_SubscriberReceivesAll()
    {
        // Arrange
        var publisher = new ExecutionTimingPublisher(NullLogger<ExecutionTimingPublisher>.Instance);
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var received = new List<ExecutionTimingInfo>();
        using var subscription = publisher.TimingUpdates.Subscribe(t => received.Add(t));

        // Act
        publisher.PublishTiming(new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 1.0,
            EstimatedTotalDurationSeconds = 10.0,
            ProgressPercentage = 10.0,
            IsRunning = true
        });

        publisher.PublishTiming(new ExecutionTimingInfo
        {
            StartTimeUtc = start,
            CurrentTimeSeconds = 5.0,
            EstimatedTotalDurationSeconds = 10.0,
            ProgressPercentage = 50.0,
            IsRunning = true
        });

        // Assert — initial value (from BehaviorSubject) + 2 published
        Assert.Equal(3, received.Count);
        Assert.False(received[0].IsRunning); // initial default
        Assert.Equal(1.0, received[1].CurrentTimeSeconds);
        Assert.Equal(5.0, received[2].CurrentTimeSeconds);
    }
}