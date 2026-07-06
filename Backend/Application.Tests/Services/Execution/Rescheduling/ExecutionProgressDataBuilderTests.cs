using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Rescheduling;

/// <summary>
///     Unit tests for ExecutionProgressDataBuilder service.
///     Tests the critical logic of building progress data from execution state.
/// </summary>
public class ExecutionProgressDataBuilderTests
{
    private readonly ExecutionProgressDataBuilder _builder = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void BuildProgressData_WithEmptyStates_ReturnsEmptyDictionary()
    {
        // Arrange
        var states = new List<SkillExecutionState>();
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildProgressData_WithStateWithoutExecutionId_SkipsNode()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-5)
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Empty(result); // Should skip nodes without ExecutionId
    }

    [Fact]
    public void BuildProgressData_WithLastProgressFromAgent_UsesAgentProgress()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);
        var agentProgress = new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = ((SkillExecutionNode)skillNode).SkillExecutionTask.Skill.Id,
            AgentId = Guid.NewGuid(),
            ActualStartTimeUtc = DateTime.UtcNow.AddSeconds(-10),
            CurrentTimeIntoExecution = 10.0,
            EstimatedTotalDuration = 20.0,
            StatusMessage = "Agent status",
            CompletedSuccessfully = false
        };

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-10),
            LastProgress = agentProgress
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(executionId));
        var progress = result[executionId];
        Assert.Equal(executionId, progress.ExecutionId);
        Assert.Equal("Agent status", progress.StatusMessage);
        Assert.Equal(10.0, progress.CurrentTimeIntoExecution);
        Assert.Equal(20.0, progress.EstimatedTotalDuration);
    }

    [Fact]
    public void BuildProgressData_WithLastProgress_OverridesCompletedSuccessfully_WhenStatusIsCompleted()
    {
        // CRITICAL TEST: The LastProgress might have CompletedSuccessfully=false,
        // but if ExecutionStatus is Completed, we must override it to true

        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);
        var agentProgress = new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = ((SkillExecutionNode)skillNode).SkillExecutionTask.Skill.Id,
            AgentId = Guid.NewGuid(),
            ActualStartTimeUtc = DateTime.UtcNow.AddSeconds(-10),
            CurrentTimeIntoExecution = 10.0,
            EstimatedTotalDuration = 20.0,
            StatusMessage = "Agent status",
            CompletedSuccessfully = false // Agent says not completed yet
        };

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Completed, // But orchestrator knows it's completed
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-10),
            CompletedAt = _timeProvider.GetUtcNow(),
            LastProgress = agentProgress
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        var progress = result[executionId];
        Assert.True(progress.CompletedSuccessfully,
            "CompletedSuccessfully should be overridden to true when ExecutionStatus is Completed");
    }

    [Fact]
    public void BuildProgressData_WithLastProgress_KeepsCompletedSuccessfullyFalse_WhenStatusIsRunning()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);
        var agentProgress = new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = ((SkillExecutionNode)skillNode).SkillExecutionTask.Skill.Id,
            AgentId = Guid.NewGuid(),
            ActualStartTimeUtc = DateTime.UtcNow.AddSeconds(-10),
            CurrentTimeIntoExecution = 10.0,
            EstimatedTotalDuration = 20.0,
            StatusMessage = "Agent status",
            CompletedSuccessfully = false
        };

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running, // Still running
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-10),
            LastProgress = agentProgress
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        var progress = result[executionId];
        Assert.False(progress.CompletedSuccessfully,
            "CompletedSuccessfully should remain false when ExecutionStatus is Running");
    }

    [Fact]
    public void BuildProgressData_WithoutLastProgress_CreatesSyntheticProgress()
    {
        // Test creating synthetic progress when agent hasn't reported yet

        // Arrange
        var executionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var startTime = _timeProvider.GetUtcNow().AddSeconds(-5);
        var skillNode = CreateSkillExecutionNodeWithIds("Skill 1", executionId, agentId, skillId, 10.0);

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = startTime,
            LastProgress = null // No progress from agent yet
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey(executionId));
        var progress = result[executionId];
        Assert.Equal(executionId, progress.ExecutionId);
        Assert.Equal(skillId, progress.SkillId);
        Assert.Equal(agentId, progress.AgentId);
        Assert.Equal(startTime.UtcDateTime, progress.ActualStartTimeUtc);
        Assert.InRange(progress.CurrentTimeIntoExecution, 4.5, 5.5); // ~5 seconds elapsed
        Assert.Equal(10.0, progress.EstimatedTotalDuration);
        Assert.Equal(ExecutionStatus.Running.ToString(), progress.StatusMessage);
        Assert.False(progress.CompletedSuccessfully);
        Assert.Null(progress.Error);
    }

    [Fact]
    public void BuildProgressData_WithoutLastProgress_CompletedSkill_CreatesSyntheticProgressWithCorrectElapsedTime()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var startTime = _timeProvider.GetUtcNow().AddSeconds(-10);
        var completedTime = _timeProvider.GetUtcNow().AddSeconds(-2);
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Completed,
            StartedAt = startTime,
            CompletedAt = completedTime,
            LastProgress = null
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        var progress = result[executionId];
        Assert.InRange(progress.CurrentTimeIntoExecution, 7.5, 8.5); // completedTime - startTime = 8 seconds
        Assert.True(progress.CompletedSuccessfully);
    }

    [Fact]
    public void BuildProgressData_WithoutLastProgress_WithErrorMessage_IncludesError()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Failed,
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-5),
            CompletedAt = _timeProvider.GetUtcNow(),
            ErrorMessage = "Test error",
            LastProgress = null
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Single(result);
        var progress = result[executionId];
        Assert.NotNull(progress.Error);
        Assert.Equal("Test error", progress.Error.Message);
        Assert.False(progress.CompletedSuccessfully);
    }

    [Fact]
    public void BuildProgressData_WithoutStartedAt_SkipsNode()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill 1", executionId);

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.NotStarted,
            StartedAt = null, // Not started yet
            LastProgress = null
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Empty(result); // Should skip nodes that haven't started
    }

    [Fact]
    public void BuildProgressData_WithMultipleStates_ReturnsAllProgressData()
    {
        // Arrange
        var executionId1 = Guid.NewGuid();
        var executionId2 = Guid.NewGuid();
        var executionId3 = Guid.NewGuid();

        var skillNode1 = CreateSkillExecutionNode("Skill 1", executionId1);
        var skillNode2 = CreateSkillExecutionNode("Skill 2", executionId2);
        var skillNode3 = CreateSkillExecutionNode("Skill 3", executionId3);

        var states = new List<SkillExecutionState>
        {
            new(skillNode1)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = _timeProvider.GetUtcNow().AddSeconds(-5),
                LastProgress = new SkillExecutionProgress
                {
                    ExecutionId = executionId1,
                    SkillId = ((SkillExecutionNode)skillNode1).SkillExecutionTask.Skill.Id,
                    AgentId = Guid.NewGuid(),
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = 5.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = "Progress 1"
                }
            },
            new(skillNode2)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = _timeProvider.GetUtcNow().AddSeconds(-3),
                LastProgress = null // Synthetic progress
            },
            new(skillNode3)
            {
                ExecutionStatus = ExecutionStatus.Completed,
                StartedAt = _timeProvider.GetUtcNow().AddSeconds(-10),
                CompletedAt = _timeProvider.GetUtcNow(),
                LastProgress = new SkillExecutionProgress
                {
                    ExecutionId = executionId3,
                    SkillId = ((SkillExecutionNode)skillNode3).SkillExecutionTask.Skill.Id,
                    AgentId = Guid.NewGuid(),
                    ActualStartTimeUtc = DateTime.UtcNow,
                    CurrentTimeIntoExecution = 10.0,
                    EstimatedTotalDuration = 10.0,
                    StatusMessage = "Progress 3",
                    CompletedSuccessfully = false // Will be overridden
                }
            }
        };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(executionId1, result.Keys);
        Assert.Contains(executionId2, result.Keys);
        Assert.Contains(executionId3, result.Keys);
        Assert.True(result[executionId3].CompletedSuccessfully, "Should override CompletedSuccessfully for skill 3");
    }

    [Fact]
    public void BuildProgressData_WithNonSkillExecutionNode_SkipsNode()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task 1");
        var state = new SkillExecutionState(taskNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = _timeProvider.GetUtcNow().AddSeconds(-5)
        };
        var states = new List<SkillExecutionState> { state };
        var currentTime = _timeProvider.GetUtcNow();

        // Act
        var result = _builder.BuildProgressData(states, currentTime);

        // Assert
        Assert.Empty(result); // Should skip non-SkillExecutionNode types
    }

    // Helper methods
    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid? executionId = null,
        Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                ExecutionId = executionId,
                Skill = CreateSkill(skillName)
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNodeWithIds(
        string skillName,
        Guid executionId,
        Guid agentId,
        Guid skillId,
        double duration = 5.0,
        Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = duration,
                StartTime = 0.0,
                FinishTime = duration,
                AgentId = agentId,
                ExecutionId = executionId,
                Skill = CreateSkillWithId(skillName,
                    skillId)
            },
            ProcedureId = default
        };
    }

    private static Skill CreateSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };
    }

    private static Skill CreateSkillWithId(string name, Guid id)
    {
        return new Skill
        {
            Id = id,
            Name = name,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };
    }
}