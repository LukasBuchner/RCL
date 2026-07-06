using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AgentProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using IRuntimeAgent = FHOOE.Freydis.Agents.Agents.IRuntimeAgent;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Coordination;

/// <summary>
///     Tests for SkillExecutionCoordinator typedProperty binding integration.
///     Verifies that input bindings are res
///     <ISkillExecutionEventBus>
///         d output bindings
///         are applied after completion.
/// </summary>
public class SkillExecutionCoordinatorBindingTests
{
    private readonly SkillExecutionCoordinator _coordinator;
    private readonly Mock<IRuntimeAgent> _mockAgent;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<ILogger<SkillExecutionCoordinator>> _mockLogger;
    private readonly Mock<IPropertyBindingService> _mockPropertyBindingService;
    private readonly TimeProvider _timeProvider;

    public SkillExecutionCoordinatorBindingTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockPropertyBindingService = new Mock<IPropertyBindingService>();
        _mockLogger = new Mock<ILogger<SkillExecutionCoordinator>>();
        _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _mockAgent = new Mock<IRuntimeAgent>();
        _timeProvider = TimeProvider.System;

        _mockAgent.Setup(a => a.Name).Returns("TestAgent");

        var mockSceneEntityResolver = new Mock<ISceneEntityResolver>();
        mockSceneEntityResolver
            .Setup(r => r.RefreshSceneEntityProperties(It.IsAny<Skill>()))
            .Returns<Skill>(s => s);

        _coordinator = new SkillExecutionCoordinator(
            _mockEventBus.Object,
            _mockAgentProvider.Object,
            _mockPropertyBindingService.Object,
            mockSceneEntityResolver.Object,
            _timeProvider,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);
    }

    #region Input Binding Tests

    [Fact]
    public async Task ExecuteSkillAsync_WithInputBindings_ResolvesBindingsBeforeExecution()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithInputBinding("targetPosition", "positionVar");
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        var resolvedInputs = new Dictionary<string, object>
        {
            ["targetPosition"] = new Position { X = 100, Y = 200, Z = 300 }
        };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ReturnsAsync(resolvedInputs);

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.Is<Skill>(s => s.Properties.Any(p =>
                    p.Name == "targetPosition" &&
                    p.Value.Value is Position)),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        var subscription = observable.Subscribe();

        // Emit progress and complete
        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 1.0,
            EstimatedTotalDuration = 2.0,
            StatusMessage = "Running",
            CompletedSuccessfully = true
        });
        progressSubject.OnCompleted();

        await Task.Delay(50); // Allow async operations to complete

        // Assert
        _mockPropertyBindingService.Verify(
            s => s.ResolveInputBindingsAsync(skill, variableContext),
            Times.Once);

        // Verify agent received a Skill with updated typedProperty values
        _mockAgent.Verify(
            a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.Is<Skill>(s => s.Properties.Any(p =>
                    p.Name == "targetPosition" &&
                    p.Value.Value is Position)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        subscription.Dispose();
    }

    [Fact]
    public async Task ExecuteSkillAsync_WithMultipleInputBindings_ResolvesAllBindings()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithMultipleInputBindings();
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        var resolvedInputs = new Dictionary<string, object>
        {
            ["position"] = new Position { X = 1, Y = 2, Z = 3 },
            ["speed"] = 100.0,
            ["enableSafety"] = true
        };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ReturnsAsync(resolvedInputs);

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        var subscription = observable.Subscribe();

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 2.0,
            EstimatedTotalDuration = 2.0,
            StatusMessage = "Running",
            CompletedSuccessfully = true
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert - Verify agent was called with a Skill (any Skill, since properties were updated)
        _mockAgent.Verify(
            a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        subscription.Dispose();
    }

    [Fact]
    public async Task ExecuteSkillAsync_InputBindingMissingVariable_ThrowsVariableNotFoundException()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithInputBinding("target", "missingVar");
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ThrowsAsync(new VariableNotFoundException("missingVar"));

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<VariableNotFoundException>(async () =>
        {
            await observable.FirstOrDefaultAsync();
        });

        Assert.Equal("missingVar", exception.VariableName);

        // Agent should never be called
        _mockAgent.Verify(
            a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteSkillAsync_InputBindingTypeMismatch_ThrowsVariableTypeMismatchException()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithInputBinding("count", "countVar");
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ThrowsAsync(new VariableTypeMismatchException("countVar", typeof(int), typeof(string)));

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<VariableTypeMismatchException>(async () =>
        {
            await observable.FirstOrDefaultAsync();
        });

        // Agent should never be called
        _mockAgent.Verify(
            a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Output Binding Tests

    [Fact]
    public async Task ExecuteSkillAsync_WithOutputBindings_AppliesBindingsAfterCompletion()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithOutputBinding("quality", "qualityResult");
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        var skillOutputs = new Dictionary<string, object>
        {
            ["quality"] = "OK"
        };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ReturnsAsync(new Dictionary<string, object>());

        _mockPropertyBindingService
            .Setup(s => s.ApplyOutputBindingsAsync(skill, skillOutputs, variableContext))
            .Returns(Task.CompletedTask);

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        var subscription = observable.Subscribe();

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 3.0,
            EstimatedTotalDuration = 3.0,
            StatusMessage = "Completed",
            CompletedSuccessfully = true,
            Outputs = skillOutputs
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert
        _mockPropertyBindingService.Verify(
            s => s.ApplyOutputBindingsAsync(skill, skillOutputs, variableContext),
            Times.Once);

        subscription.Dispose();
    }

    [Fact]
    public async Task ExecuteSkillAsync_WithMultipleOutputBindings_AppliesAllBindings()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithMultipleOutputBindings();
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        var skillOutputs = new Dictionary<string, object>
        {
            ["finalPosition"] = new Position { X = 10, Y = 20, Z = 30 },
            ["completionTime"] = 5.5,
            ["success"] = true
        };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ReturnsAsync(new Dictionary<string, object>());

        _mockPropertyBindingService
            .Setup(s => s.ApplyOutputBindingsAsync(skill, skillOutputs, variableContext))
            .Returns(Task.CompletedTask);

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        var subscription = observable.Subscribe();

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 5.5,
            EstimatedTotalDuration = 5.5,
            StatusMessage = "Completed",
            CompletedSuccessfully = true,
            Outputs = skillOutputs
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert
        _mockPropertyBindingService.Verify(
            s => s.ApplyOutputBindingsAsync(
                skill,
                It.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("finalPosition") &&
                    d.ContainsKey("completionTime") &&
                    d.ContainsKey("success")),
                variableContext),
            Times.Once);

        subscription.Dispose();
    }

    [Fact]
    public async Task ExecuteSkillAsync_OutputBindingError_LoggedButExecutionSucceeds()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithOutputBinding("result", "resultVar");
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        var skillOutputs = new Dictionary<string, object> { ["result"] = "value" };

        _mockPropertyBindingService
            .Setup(s => s.ResolveInputBindingsAsync(skill, variableContext))
            .ReturnsAsync(new Dictionary<string, object>());

        // Output binding fails
        _mockPropertyBindingService
            .Setup(s => s.ApplyOutputBindingsAsync(skill, skillOutputs, variableContext))
            .ThrowsAsync(new InvalidOperationException("Failed to apply output binding"));

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        SkillExecutionProgress? lastProgress = null;
        var subscription = observable.Subscribe(p => lastProgress = p);

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 2.0,
            EstimatedTotalDuration = 2.0,
            StatusMessage = "Completed",
            CompletedSuccessfully = true,
            Outputs = skillOutputs
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert - Execution should still succeed
        Assert.NotNull(lastProgress);
        Assert.True(lastProgress.IsCompleted);

        // Verify error was logged
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to apply output bindings")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        subscription.Dispose();
    }

    #endregion

    #region No Binding Tests

    [Fact]
    public async Task ExecuteSkillAsync_SkillWithNoBindings_ExecutesNormally()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithoutBindings();
        var variableContext = new VariableContext { ProcedureExecutionId = Guid.NewGuid() };

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                skill,
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable =
            _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, variableContext, CancellationToken.None);
        var subscription = observable.Subscribe();

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 1.0,
            EstimatedTotalDuration = 1.0,
            StatusMessage = "Completed",
            CompletedSuccessfully = true
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert - PropertyBindingService should NOT be called (skill has no bindings)
        _mockPropertyBindingService.Verify(
            s => s.ResolveInputBindingsAsync(It.IsAny<Skill>(), It.IsAny<VariableContext>()),
            Times.Never);

        // Output bindings should not be applied (no outputs)
        _mockPropertyBindingService.Verify(
            s => s.ApplyOutputBindingsAsync(
                It.IsAny<Skill>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<VariableContext>()),
            Times.Never);

        subscription.Dispose();
    }

    [Fact]
    public async Task ExecuteSkillAsync_NullVariableContext_SkipsBindingsAndExecutesNormally()
    {
        // Arrange
        var skillNodeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skill = CreateSkillWithInputBinding("target", "targetVar");

        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(agentId)).Returns(_mockAgent.Object);

        var progressSubject = new Subject<AgentProgress>();
        _mockAgent
            .Setup(a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                skill,
                It.IsAny<CancellationToken>()))
            .Returns(progressSubject.AsObservable());

        // Act
        var observable = _coordinator.ExecuteSkillAsync(skillNodeId, skill, agentId, null, CancellationToken.None);
        var subscription = observable.Subscribe();

        progressSubject.OnNext(new AgentProgress
        {
            ExecutionId = Guid.NewGuid(),
            SkillId = skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = DateTime.UtcNow,
            CurrentTimeIntoExecution = 1.0,
            EstimatedTotalDuration = 1.0,
            StatusMessage = "Completed",
            CompletedSuccessfully = true
        });
        progressSubject.OnCompleted();

        await Task.Delay(50);

        // Assert - PropertyBindingService should not be called (null variable context)
        _mockPropertyBindingService.Verify(
            s => s.ResolveInputBindingsAsync(It.IsAny<Skill>(), It.IsAny<VariableContext>()),
            Times.Never);

        // Agent should still be called with original skill
        _mockAgent.Verify(
            a => a.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                skill,
                It.IsAny<CancellationToken>()),
            Times.Once);

        subscription.Dispose();
    }

    #endregion

    #region Helper Methods

    private Skill CreateSkillWithInputBinding(string propertyName, string variableName)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test skill with input binding",
            Properties =
            [
                new TypedProperty
                {
                    Name = propertyName,
                    Value = TypedValue.Number(0),
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding
                    {
                        VariableName = variableName,
                        Mode = BindingMode.Read
                    }
                }
            ]
        };
    }

    private Skill CreateSkillWithOutputBinding(string propertyName, string variableName)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test skill with output binding",
            Properties =
            [
                new TypedProperty
                {
                    Name = propertyName,
                    Value = TypedValue.Text(""),
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding
                    {
                        VariableName = variableName,
                        Mode = BindingMode.Write
                    }
                }
            ]
        };
    }

    private Skill CreateSkillWithMultipleInputBindings()
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test skill with multiple input bindings",
            Properties =
            [
                new TypedProperty
                {
                    Name = "position",
                    Value = TypedValue.Position(new Position { X = 0, Y = 0, Z = 0 }),
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding { VariableName = "posVar", Mode = BindingMode.Read }
                },

                new TypedProperty
                {
                    Name = "speed",
                    Value = TypedValue.Number(0),
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding { VariableName = "speedVar", Mode = BindingMode.Read }
                },

                new TypedProperty
                {
                    Name = "enableSafety",
                    Value = TypedValue.Boolean(false),
                    Direction = PropertyDirection.Input,
                    Binding = new VariableBinding { VariableName = "safetyVar", Mode = BindingMode.Read }
                }
            ]
        };
    }

    private Skill CreateSkillWithMultipleOutputBindings()
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "TestSkill",
            Description = "Test skill with multiple output bindings",
            Properties =
            [
                new TypedProperty
                {
                    Name = "finalPosition",
                    Value = TypedValue.Position(new Position { X = 0, Y = 0, Z = 0 }),
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding { VariableName = "finalPosVar", Mode = BindingMode.Write }
                },

                new TypedProperty
                {
                    Name = "completionTime",
                    Value = TypedValue.Number(0),
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding { VariableName = "timeVar", Mode = BindingMode.Write }
                },

                new TypedProperty
                {
                    Name = "success",
                    Value = TypedValue.Boolean(false),
                    Direction = PropertyDirection.Output,
                    Binding = new VariableBinding { VariableName = "successVar", Mode = BindingMode.Write }
                }
            ]
        };
    }

    private Skill CreateSkillWithoutBindings()
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "SimpleSkill",
            Description = "Skill without any bindings",
            Properties =
            [
                new TypedProperty
                {
                    Name = "simpleParam",
                    Value = TypedValue.Number(42),
                    Direction = PropertyDirection.Input,
                    Binding = null // No binding
                }
            ]
        };
    }

    #endregion
}