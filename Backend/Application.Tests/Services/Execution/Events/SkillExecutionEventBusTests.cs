using FHOOE.Freydis.Application.Services.Execution.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Events;

public class SkillExecutionEventBusTests
{
    private readonly SkillExecutionEventBus _eventBus;
    private readonly Mock<ILogger<SkillExecutionEventBus>> _mockLogger;

    public SkillExecutionEventBusTests()
    {
        _mockLogger = new Mock<ILogger<SkillExecutionEventBus>>();
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _eventBus = new SkillExecutionEventBus(_mockLogger.Object);
    }

    [Fact]
    public void Dispose()
    {
        _eventBus.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new SkillExecutionEventBus(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void PublishEvent_ThrowsArgumentNullException_WhenEventIsNull()
    {
        // Act
        var act = () => _eventBus.PublishEvent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("executionEvent");
    }

    [Fact]
    public async Task AllEvents_EmitsPublishedStartEvent()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        ExecutionEvent? receivedEvent = null;
        _eventBus.AllEvents.Subscribe(e => receivedEvent = e);

        // Act
        _eventBus.PublishEvent(executionEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent.Should().Be(executionEvent);
    }

    [Fact]
    public async Task AllEvents_EmitsPublishedFinishEvent()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp
        };

        ExecutionEvent? receivedEvent = null;
        _eventBus.AllEvents.Subscribe(e => receivedEvent = e);

        // Act
        _eventBus.PublishEvent(executionEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent.Should().Be(executionEvent);
    }

    [Fact]
    public async Task StartEvents_OnlyEmitsStartEvents()
    {
        // Arrange
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var startEvent = new ExecutionEvent
        {
            SkillId = skillId1,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var finishEvent = new ExecutionEvent
        {
            SkillId = skillId2,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp.AddSeconds(1)
        };

        var receivedEvents = new List<ExecutionEvent>();
        _eventBus.StartEvents.Subscribe(e => receivedEvents.Add(e));

        // Act
        _eventBus.PublishEvent(startEvent);
        _eventBus.PublishEvent(finishEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].Should().Be(startEvent);
        receivedEvents[0].EventType.Should().Be(ExecutionEventType.Start);
    }

    [Fact]
    public async Task FinishEvents_OnlyEmitsFinishEvents()
    {
        // Arrange
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var startEvent = new ExecutionEvent
        {
            SkillId = skillId1,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var finishEvent = new ExecutionEvent
        {
            SkillId = skillId2,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp.AddSeconds(1)
        };

        var receivedEvents = new List<ExecutionEvent>();
        _eventBus.FinishEvents.Subscribe(e => receivedEvents.Add(e));

        // Act
        _eventBus.PublishEvent(startEvent);
        _eventBus.PublishEvent(finishEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].Should().Be(finishEvent);
        receivedEvents[0].EventType.Should().Be(ExecutionEventType.Finish);
    }

    [Fact]
    public async Task AllEvents_EmitsMultipleEvents_InOrder()
    {
        // Arrange
        var skill1 = Guid.NewGuid();
        var skill2 = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var event1 = new ExecutionEvent
        {
            SkillId = skill1,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var event2 = new ExecutionEvent
        {
            SkillId = skill2,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp.AddSeconds(1)
        };

        var event3 = new ExecutionEvent
        {
            SkillId = skill1,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp.AddSeconds(2)
        };

        var receivedEvents = new List<ExecutionEvent>();
        _eventBus.AllEvents.Subscribe(e => receivedEvents.Add(e));

        // Act
        _eventBus.PublishEvent(event1);
        _eventBus.PublishEvent(event2);
        _eventBus.PublishEvent(event3);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        receivedEvents.Should().HaveCount(3);
        receivedEvents[0].Should().Be(event1);
        receivedEvents[1].Should().Be(event2);
        receivedEvents[2].Should().Be(event3);
    }

    [Fact]
    public async Task EventBus_SupportsMultipleSubscribers()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        ExecutionEvent? subscriber1Event = null;
        ExecutionEvent? subscriber2Event = null;

        _eventBus.AllEvents.Subscribe(e => subscriber1Event = e);
        _eventBus.AllEvents.Subscribe(e => subscriber2Event = e);

        // Act
        _eventBus.PublishEvent(executionEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        subscriber1Event.Should().Be(executionEvent);
        subscriber2Event.Should().Be(executionEvent);
    }

    [Fact]
    public async Task StartEvents_And_FinishEvents_CanBeSubscribedIndependently()
    {
        // Arrange
        var skill1 = Guid.NewGuid();
        var skill2 = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var startEvent = new ExecutionEvent
        {
            SkillId = skill1,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var finishEvent = new ExecutionEvent
        {
            SkillId = skill2,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp.AddSeconds(1)
        };

        var startEvents = new List<ExecutionEvent>();
        var finishEvents = new List<ExecutionEvent>();

        _eventBus.StartEvents.Subscribe(e => startEvents.Add(e));
        _eventBus.FinishEvents.Subscribe(e => finishEvents.Add(e));

        // Act
        _eventBus.PublishEvent(startEvent);
        _eventBus.PublishEvent(finishEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        startEvents.Should().HaveCount(1);
        startEvents[0].Should().Be(startEvent);

        finishEvents.Should().HaveCount(1);
        finishEvents[0].Should().Be(finishEvent);
    }

    [Fact]
    public async Task Dispose_CompletesAllSubscriptions()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SkillExecutionEventBus>>();
        using var eventBus = new SkillExecutionEventBus(mockLogger.Object);

        var completed = false;
        eventBus.AllEvents.Subscribe(
            _ => { },
            () => completed = true);

        // Act
        eventBus.Dispose();

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        completed.Should().BeTrue();
    }

    [Fact]
    public async Task PublishEvent_LogsDebugMessage()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        // Act
        _eventBus.PublishEvent(executionEvent);

        // Wait for async propagation
        await Task.Delay(10);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                (LogLevel)LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EventBus_HandlesHighVolumeOfEvents()
    {
        // Arrange
        const int eventCount = 1000;
        var receivedEvents = new List<ExecutionEvent>();
        _eventBus.AllEvents.Subscribe(e => receivedEvents.Add(e));

        var events = Enumerable.Range(0, eventCount)
            .Select(i => new ExecutionEvent
            {
                SkillId = Guid.NewGuid(),
                EventType = i % 2 == 0 ? ExecutionEventType.Start : ExecutionEventType.Finish,
                Timestamp = DateTimeOffset.UtcNow.AddMilliseconds(i)
            })
            .ToList();

        // Act
        foreach (var evt in events) _eventBus.PublishEvent(evt);

        // Wait for async propagation
        await Task.Delay(100);

        // Assert
        receivedEvents.Should().HaveCount(eventCount);
    }
}