using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Domain.Entities.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CoordinatorProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using SkillExecutionProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;


namespace FHOOE.Freydis.Application.Tests.Services.Execution.Coordination;

/// <summary>
///     Tests for SkillExecutionCoordinator.
/// </summary>
public sealed class SkillExecutionCoordinatorTests
{
    private readonly SkillExecutionCoordinator _coordinator;
    private readonly Mock<IRuntimeAgent> _mockAgent;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<ILogger<SkillExecutionCoordinator>> _mockLogger;
    private readonly Mock<IPropertyBindingService> _mockPropertyBindingService;
    private readonly Mock<ISceneEntityResolver> _mockSceneEntityResolver;
    private readonly TimeProvider _timeProvider;

    public SkillExecutionCoordinatorTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockAgent = new Mock<IRuntimeAgent>();
        _mockPropertyBindingService = new Mock<IPropertyBindingService>();
        _mockSceneEntityResolver = new Mock<ISceneEntityResolver>();
        _mockSceneEntityResolver
            .Setup(r => r.RefreshSceneEntityProperties(It.IsAny<Skill>()))
            .Returns<Skill>(s => s);
        _timeProvider = TimeProvider.System;
        _mockLogger = new Mock<ILogger<SkillExecutionCoordinator>>();

        // Setup logger to enable all log levels
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _coordinator = new SkillExecutionCoordinator(
            _mockEventBus.Object,
            _mockAgentProvider.Object,
            _mockPropertyBindingService.Object,
            _mockSceneEntityResolver.Object,
            _timeProvider,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);
    }

    private static Skill CreateTestSkill()
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test",
            Properties = new List<TypedProperty>()
        };
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEventBusIsNull()
    {
        // Act
        var act = () => new SkillExecutionCoordinator(
            null!,
            _mockAgentProvider.Object,
            _mockPropertyBindingService.Object,
            _mockSceneEntityResolver.Object,
            _timeProvider,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventBus");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAgentProviderIsNull()
    {
        // Act
        var act = () => new SkillExecutionCoordinator(
            _mockEventBus.Object,
            null!,
            _mockPropertyBindingService.Object,
            _mockSceneEntityResolver.Object,
            _timeProvider,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("agentProvider");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPropertyBindingServiceIsNull()
    {
        // Act
        var act = () => new SkillExecutionCoordinator(
            _mockEventBus.Object,
            _mockAgentProvider.Object,
            null!,
            _mockSceneEntityResolver.Object,
            _timeProvider,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("propertyBindingService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTimeProviderIsNull()
    {
        // Act
        var act = () => new SkillExecutionCoordinator(
            _mockEventBus.Object,
            _mockAgentProvider.Object,
            _mockPropertyBindingService.Object,
            _mockSceneEntityResolver.Object,
            null!,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new SkillExecutionCoordinator(
            _mockEventBus.Object,
            _mockAgentProvider.Object,
            _mockPropertyBindingService.Object,
            _mockSceneEntityResolver.Object,
            _timeProvider,
            null!,
            NullLogger<PipelineEvents>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void ExecuteSkillAsync_ThrowsArgumentNullException_WhenSkillIsNull()
    {
        // Act
        var act = () =>
            _coordinator.ExecuteSkillAsync(Guid.NewGuid(), null!, Guid.NewGuid(), null, CancellationToken.None);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("skill");
    }

    [Fact]
    public async Task ExecuteSkillAsync_PublishesStartEvent_WhenExecutionBegins()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var progressSubject = new Subject<SkillExecutionProgress>();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns(_mockAgent.Object);

        _mockAgent
            .Setup(x => x.ExecuteSkillAsync(It.IsAny<Guid>(), skill, It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        var publishedEvents = new List<ExecutionEvent>();
        _mockEventBus
            .Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => publishedEvents.Add(e));

        // Act
        var observable = _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, null, CancellationToken.None);
        observable.Subscribe();

        await Task.Delay(50); // Allow async operations to complete

        // Emit first progress update to trigger Start event
        progressSubject.OnNext(new SkillExecutionProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 1.0,
            EstimatedTotalDuration = 10.0,
            StatusMessage = "Starting"
        });

        await Task.Delay(50); // Allow event publishing to complete

        // Assert - First event should be Start
        publishedEvents.Should().NotBeEmpty();
        publishedEvents[0].SkillId.Should().Be(skillNodeId); // Should be SkillExecutionNode ID
        publishedEvents[0].EventType.Should().Be(ExecutionEventType.Start);
    }

    [Fact]
    public async Task ExecuteSkillAsync_PublishesFinishEvent_WhenExecutionCompletes()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var progressSubject = new Subject<SkillExecutionProgress>();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns(_mockAgent.Object);

        _mockAgent
            .Setup(x => x.ExecuteSkillAsync(It.IsAny<Guid>(), skill, It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        var publishedEvents = new List<ExecutionEvent>();
        _mockEventBus
            .Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => publishedEvents.Add(e));

        // Act
        var observable = _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, null, CancellationToken.None);
        observable.Subscribe();

        await Task.Delay(50);

        // Simulate completion
        progressSubject.OnNext(new SkillExecutionProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 10.0,
            EstimatedTotalDuration = 10.0,
            StatusMessage = "Done",
            CompletedSuccessfully = true
        });

        await Task.Delay(50);

        // Assert
        publishedEvents.Should().HaveCount(2);
        publishedEvents[0].EventType.Should().Be(ExecutionEventType.Start);
        publishedEvents[1].EventType.Should().Be(ExecutionEventType.Finish);
        publishedEvents.All(e => e.SkillId == skillNodeId).Should().BeTrue(); // Should be SkillExecutionNode ID
    }

    [Fact]
    public async Task ExecuteSkillAsync_ForwardsProgress_FromAgent()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var progressSubject = new Subject<SkillExecutionProgress>();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns(_mockAgent.Object);

        _mockAgent
            .Setup(x => x.ExecuteSkillAsync(It.IsAny<Guid>(), skill, It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        var receivedProgress = new List<CoordinatorProgress>();

        // Act
        var observable = _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, null, CancellationToken.None);
        observable.Subscribe(p => receivedProgress.Add(p));

        await Task.Delay(50);

        progressSubject.OnNext(new SkillExecutionProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 5.0,
            EstimatedTotalDuration = 10.0,
            StatusMessage = "In progress"
        });

        await Task.Delay(50);

        // Assert
        receivedProgress.Should().HaveCount(1);
        receivedProgress[0].SkillId.Should().Be(skillNodeId); // Should be SkillExecutionNode ID
        receivedProgress[0].Progress.Should().Be(0.5);
        receivedProgress[0].StatusMessage.Should().Be("In progress");
    }

    [Fact]
    public async Task ExecuteAdaptiveSkillAsync_PublishesStartAndFinishEvents()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var finishSignal = new Subject<ExecutionEvent>();
        var progressSubject = new Subject<SkillExecutionProgress>();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns(_mockAgent.Object);

        _mockAgent
            .Setup(x => x.ExecuteSkillAdaptivelyAsync(
                It.IsAny<Guid>(),
                skill,
                It.IsAny<double>(),
                It.IsAny<IObservable<double>>(),
                It.IsAny<IObservable<Unit>>(),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        var publishedEvents = new List<ExecutionEvent>();
        _mockEventBus
            .Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => publishedEvents.Add(e));

        // Act - convert finish signal ExecutionEvent -> Unit
        var finishSignalUnit = finishSignal.Select(_ => Unit.Default);
        var plannedFinishTimes = Observable.Return(10.0);
        var observable = _coordinator.ExecuteAdaptiveSkillAsync(
            skillNodeId,
            skill,
            agentId,
            10.0,
            plannedFinishTimes,
            finishSignalUnit,
            null,
            CancellationToken.None);
        observable.Subscribe();

        await Task.Delay(50);

        // Simulate completion
        progressSubject.OnNext(new SkillExecutionProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 10.0,
            EstimatedTotalDuration = 10.0,
            StatusMessage = "Completed",
            CompletedSuccessfully = true
        });

        await Task.Delay(50);

        // Assert
        publishedEvents.Should().HaveCount(2);
        publishedEvents[0].EventType.Should().Be(ExecutionEventType.Start);
        publishedEvents[1].EventType.Should().Be(ExecutionEventType.Finish);
    }

    [Fact]
    public async Task ExecuteAdaptiveSkillAsync_PassesFinishSignalToAgent()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var finishSignal = new Subject<ExecutionEvent>();
        var progressSubject = new Subject<SkillExecutionProgress>();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns(_mockAgent.Object);

        IObservable<Unit>? capturedFinishSignal = null;
        _mockAgent
            .Setup(x => x.ExecuteSkillAdaptivelyAsync(
                It.IsAny<Guid>(),
                skill,
                It.IsAny<double>(),
                It.IsAny<IObservable<double>>(),
                It.IsAny<IObservable<Unit>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, Skill, double, IObservable<double>, IObservable<Unit>, CancellationToken>((_, _, _, _,
                finishSig, _) => capturedFinishSignal = finishSig)
            .Returns(progressSubject.AsObservable());

        // Act - convert finish signal ExecutionEvent -> Unit
        var finishSignalUnit = finishSignal.Select(_ => Unit.Default);
        var plannedFinishTimes = Observable.Return(10.0);
        var observable = _coordinator.ExecuteAdaptiveSkillAsync(
            skillNodeId,
            skill,
            agentId,
            10.0,
            plannedFinishTimes,
            finishSignalUnit,
            null,
            CancellationToken.None);
        observable.Subscribe();

        await Task.Delay(50); // Wait for subscription to complete

        // Assert - Agent receives the finish signal
        capturedFinishSignal.Should().NotBeNull("the coordinator should pass the finish signal to the agent");

        // Verify finish signal works by subscribing to it
        var signalReceived = false;
        capturedFinishSignal!.Subscribe(_ => signalReceived = true);

        finishSignal.OnNext(new ExecutionEvent
        {
            SkillId = Guid.NewGuid(),
            EventType = ExecutionEventType.Finish,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Task.Delay(50);
        signalReceived.Should().BeTrue("the finish signal should propagate to subscribers");
    }

    [Fact]
    public void ExecuteAdaptiveSkillAsync_ThrowsArgumentNullException_WhenSkillIsNull()
    {
        // Arrange
        var finishSignal = new Subject<ExecutionEvent>();
        var plannedFinishTimes = Observable.Return(10.0);

        // Act
        var act = () => _coordinator.ExecuteAdaptiveSkillAsync(
            Guid.NewGuid(),
            null!,
            Guid.NewGuid(),
            10.0,
            plannedFinishTimes,
            finishSignal.Select(_ => Unit.Default),
            null,
            CancellationToken.None);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("skill");
    }

    [Fact]
    public void ExecuteAdaptiveSkillAsync_ThrowsArgumentNullException_WhenFinishSignalIsNull()
    {
        // Arrange
        var skill = CreateTestSkill();
        var plannedFinishTimes = Observable.Return(10.0);

        // Act
        var act = () => _coordinator.ExecuteAdaptiveSkillAsync(
            Guid.NewGuid(),
            skill,
            Guid.NewGuid(),
            10.0,
            plannedFinishTimes,
            null!,
            null,
            CancellationToken.None);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("finishSignal");
    }

    [Fact]
    public async Task ExecuteSkillAsync_HandlesAgentNotFound_Gracefully()
    {
        // Arrange
        var skill = CreateTestSkill();
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        _mockAgentProvider
            .Setup(x => x.GetRuntimeAgent(agentId))
            .Returns((IRuntimeAgent?)null);

        Exception? capturedException = null;

        // Act
        var observable = _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, null, CancellationToken.None);
        observable.Subscribe(
            _ => { },
            ex => capturedException = ex);

        await Task.Delay(50);

        // Assert
        capturedException.Should().NotBeNull();
        capturedException.Should().BeOfType<InvalidOperationException>();
        capturedException!.Message.Should().Contain(agentId.ToString());
    }
}