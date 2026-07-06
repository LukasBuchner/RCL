using FHOOE.Freydis.Application.Services.Execution.Events;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Events;

/// <summary>
///     Tests for ExecutionEvent data structures.
/// </summary>
public sealed class ExecutionEventTests
{
    [Fact]
    public void ExecutionEvent_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var startEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        // Assert
        startEvent.SkillId.Should().Be(skillId);
        startEvent.EventType.Should().Be(ExecutionEventType.Start);
        startEvent.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ExecutionEvent_FinishEvent_CanBeCreated()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var finishEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp
        };

        // Assert
        finishEvent.SkillId.Should().Be(skillId);
        finishEvent.EventType.Should().Be(ExecutionEventType.Finish);
        finishEvent.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ExecutionEventType_HasStartValue()
    {
        // Act
        var eventType = ExecutionEventType.Start;

        // Assert
        eventType.Should().Be(ExecutionEventType.Start);
        ((int)eventType).Should().Be(0);
    }

    [Fact]
    public void ExecutionEventType_HasFinishValue()
    {
        // Act
        var eventType = ExecutionEventType.Finish;

        // Assert
        eventType.Should().Be(ExecutionEventType.Finish);
        ((int)eventType).Should().Be(1);
    }

    [Fact]
    public void ExecutionEvent_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var event1 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var event2 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        // Assert
        event1.Should().Be(event2);
        event1.GetHashCode().Should().Be(event2.GetHashCode());
    }

    [Fact]
    public void ExecutionEvent_RecordEquality_DiffersBySkillId()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        var event1 = new ExecutionEvent
        {
            SkillId = Guid.NewGuid(),
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var event2 = new ExecutionEvent
        {
            SkillId = Guid.NewGuid(),
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        // Assert
        event1.Should().NotBe(event2);
    }

    [Fact]
    public void ExecutionEvent_RecordEquality_DiffersByEventType()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var event1 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = timestamp
        };

        var event2 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Finish,
            Timestamp = timestamp
        };

        // Assert
        event1.Should().NotBe(event2);
    }

    [Fact]
    public void ExecutionEvent_RecordEquality_DiffersByTimestamp()
    {
        // Arrange
        var skillId = Guid.NewGuid();

        var event1 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow
        };

        var event2 = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Start,
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        // Assert
        event1.Should().NotBe(event2);
    }
}