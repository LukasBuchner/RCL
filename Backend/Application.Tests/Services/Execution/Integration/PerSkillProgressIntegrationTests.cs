using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Integration;

/// <summary>
///     Integration tests verifying that per-skill progress data flows correctly
///     through the rescheduling pipeline: State → ProgressDataBuilder →
///     ExecutionAwareDurationProvider → NodeTimingMapper → node.Progress.
/// </summary>
public class PerSkillProgressIntegrationTests
{
    private readonly ExecutionProgressDataBuilder _progressDataBuilder = new();
    private readonly NodeTimingMapper _timingMapper;
    private readonly DateTime _procedureStartTimeUtc = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public PerSkillProgressIntegrationTests()
    {
        _timingMapper = new NodeTimingMapper(NullLogger<NodeTimingMapper>.Instance);
    }

    #region B1: Single Running Skill

    [Fact]
    public async Task PerSkillProgress_RunningSkill_HasNonZeroProgress()
    {
        // Arrange: 1 SkillExecutionNode with agent at 50% progress
        var executionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode(
            "TestSkill", executionId, agentId,
            0, 10);

        var agentStartTimeUtc = _procedureStartTimeUtc.AddSeconds(0.5);
        var agentProgress = CreateAgentProgress(
            executionId, skillNode, agentId, agentStartTimeUtc,
            5.0, 10.0);

        var state = new SkillExecutionState(skillNode)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = new DateTimeOffset(agentStartTimeUtc, TimeSpan.Zero),
            LastProgress = agentProgress,
            LastProgressPercentage = 50.0
        };

        // Act: Run through the rescheduling pipeline
        var resultNode = await RunReschedulingPipeline(skillNode, state, agentId);

        // Assert: Node should have non-zero progress
        Assert.NotNull(resultNode);
        Assert.NotNull(resultNode.SkillExecutionTask.Progress);
        Assert.True(resultNode.SkillExecutionTask.Progress > 0,
            $"Expected progress > 0, but got {resultNode.SkillExecutionTask.Progress}");
        Assert.Equal(50.0, resultNode.SkillExecutionTask.Progress!.Value, 1);
        Assert.True(resultNode.SkillExecutionTask.IsExecuting);
    }

    #endregion

    #region B2: Parallel Skills Show Independent Progress

    [Fact]
    public async Task PerSkillProgress_UserScenario_ParallelSkillsBothShowProgress()
    {
        // Arrange: User's exact scenario:
        // Grasp1(0-55) → [Hold(55-145) ∥ Weld(55-145)] → Grasp2(145-200)
        // At t≈100: Grasp1=Completed, Hold=Running@50%, Weld=Running@50%, Grasp2=NotStarted
        var agentId = Guid.NewGuid();

        var grasp1ExecId = Guid.NewGuid();
        var holdExecId = Guid.NewGuid();
        var weldExecId = Guid.NewGuid();
        var grasp2ExecId = Guid.NewGuid();

        var grasp1 = CreateSkillExecutionNode("Grasp1", grasp1ExecId, agentId, 0, 55);
        var hold = CreateSkillExecutionNode("Hold", holdExecId, agentId, 55, 90);
        var weld = CreateSkillExecutionNode("Weld", weldExecId, agentId, 55, 90);
        var grasp2 = CreateSkillExecutionNode("Grasp2", grasp2ExecId, agentId, 145, 55);

        var grasp1StartUtc = _procedureStartTimeUtc.AddSeconds(0);
        var holdStartUtc = _procedureStartTimeUtc.AddSeconds(55);
        var weldStartUtc = _procedureStartTimeUtc.AddSeconds(55);

        // Grasp1 completed
        var grasp1Progress = CreateAgentProgress(
            grasp1ExecId, grasp1, agentId, grasp1StartUtc,
            55, 55);
        var grasp1State = new SkillExecutionState(grasp1)
        {
            ExecutionStatus = ExecutionStatus.Completed,
            StartedAt = new DateTimeOffset(grasp1StartUtc, TimeSpan.Zero),
            CompletedAt = new DateTimeOffset(grasp1StartUtc.AddSeconds(55), TimeSpan.Zero),
            LastProgress = grasp1Progress with { CompletedSuccessfully = true },
            LastProgressPercentage = 100.0
        };

        // Hold running at 50%
        var holdProgress = CreateAgentProgress(
            holdExecId, hold, agentId, holdStartUtc,
            45, 90);
        var holdState = new SkillExecutionState(hold)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = new DateTimeOffset(holdStartUtc, TimeSpan.Zero),
            LastProgress = holdProgress,
            LastProgressPercentage = 50.0
        };

        // Weld running at 50%
        var weldProgress = CreateAgentProgress(
            weldExecId, weld, agentId, weldStartUtc,
            45, 90);
        var weldState = new SkillExecutionState(weld)
        {
            ExecutionStatus = ExecutionStatus.Running,
            StartedAt = new DateTimeOffset(weldStartUtc, TimeSpan.Zero),
            LastProgress = weldProgress,
            LastProgressPercentage = 50.0
        };

        // Grasp2 not started
        var grasp2State = new SkillExecutionState(grasp2)
        {
            ExecutionStatus = ExecutionStatus.NotStarted
        };

        var states = new List<SkillExecutionState> { grasp1State, holdState, weldState, grasp2State };

        // Act: Run Hold and Weld through the rescheduling pipeline
        var holdResult = await RunReschedulingPipelineWithStates(hold, states, agentId);
        var weldResult = await RunReschedulingPipelineWithStates(weld, states, agentId);

        // Assert: BOTH parallel skill nodes should show ~50% progress
        Assert.NotNull(holdResult);
        Assert.NotNull(holdResult.SkillExecutionTask.Progress);
        Assert.Equal(50.0, holdResult.SkillExecutionTask.Progress!.Value, 1);
        Assert.True(holdResult.SkillExecutionTask.IsExecuting);

        Assert.NotNull(weldResult);
        Assert.NotNull(weldResult.SkillExecutionTask.Progress);
        Assert.Equal(50.0, weldResult.SkillExecutionTask.Progress!.Value, 1);
        Assert.True(weldResult.SkillExecutionTask.IsExecuting);
    }

    #endregion

    #region B3: Sequential Progression Through Time Snapshots

    [Fact]
    public async Task PerSkillProgress_UserScenario_SequentialProgression()
    {
        // Arrange: Full user scenario tested at multiple time snapshots
        var agentId = Guid.NewGuid();

        var grasp1ExecId = Guid.NewGuid();
        var holdExecId = Guid.NewGuid();
        var weldExecId = Guid.NewGuid();
        var grasp2ExecId = Guid.NewGuid();

        var grasp1 = CreateSkillExecutionNode("Grasp1", grasp1ExecId, agentId, 0, 55);
        var hold = CreateSkillExecutionNode("Hold", holdExecId, agentId, 55, 90);
        var weld = CreateSkillExecutionNode("Weld", weldExecId, agentId, 55, 90);
        var grasp2 = CreateSkillExecutionNode("Grasp2", grasp2ExecId, agentId, 145, 55);

        // --- Snapshot 1: Grasp1 running at 50% (t≈27.5) ---
        var grasp1StartUtc = _procedureStartTimeUtc;
        var grasp1Progress = CreateAgentProgress(
            grasp1ExecId, grasp1, agentId, grasp1StartUtc,
            27.5, 55);
        var snapshot1States = new List<SkillExecutionState>
        {
            new(grasp1)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = new DateTimeOffset(grasp1StartUtc, TimeSpan.Zero),
                LastProgress = grasp1Progress,
                LastProgressPercentage = 50.0
            },
            new(hold) { ExecutionStatus = ExecutionStatus.NotStarted },
            new(weld) { ExecutionStatus = ExecutionStatus.NotStarted },
            new(grasp2) { ExecutionStatus = ExecutionStatus.NotStarted }
        };

        var grasp1Result = await RunReschedulingPipelineWithStates(grasp1, snapshot1States, agentId);
        Assert.NotNull(grasp1Result);
        Assert.NotNull(grasp1Result.SkillExecutionTask.Progress);
        Assert.True(grasp1Result.SkillExecutionTask.Progress > 0,
            "Grasp1 should have non-zero progress when running");

        // --- Snapshot 2: Hold + Weld running at ~50% (t≈100) ---
        var holdStartUtc = _procedureStartTimeUtc.AddSeconds(55);
        var weldStartUtc = _procedureStartTimeUtc.AddSeconds(55);

        var holdProgress = CreateAgentProgress(
            holdExecId, hold, agentId, holdStartUtc,
            45, 90);
        var weldProgress = CreateAgentProgress(
            weldExecId, weld, agentId, weldStartUtc,
            45, 90);

        var snapshot2States = new List<SkillExecutionState>
        {
            new(grasp1)
            {
                ExecutionStatus = ExecutionStatus.Completed,
                StartedAt = new DateTimeOffset(grasp1StartUtc, TimeSpan.Zero),
                CompletedAt = new DateTimeOffset(grasp1StartUtc.AddSeconds(55), TimeSpan.Zero),
                LastProgress = grasp1Progress with
                {
                    CurrentTimeIntoExecution = 55,
                    CompletedSuccessfully = true
                },
                LastProgressPercentage = 100.0
            },
            new(hold)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = new DateTimeOffset(holdStartUtc, TimeSpan.Zero),
                LastProgress = holdProgress,
                LastProgressPercentage = 50.0
            },
            new(weld)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = new DateTimeOffset(weldStartUtc, TimeSpan.Zero),
                LastProgress = weldProgress,
                LastProgressPercentage = 50.0
            },
            new(grasp2) { ExecutionStatus = ExecutionStatus.NotStarted }
        };

        var holdResult = await RunReschedulingPipelineWithStates(hold, snapshot2States, agentId);
        var weldResult = await RunReschedulingPipelineWithStates(weld, snapshot2States, agentId);

        Assert.NotNull(holdResult);
        Assert.True(holdResult.SkillExecutionTask.Progress > 0,
            "Hold should have non-zero progress when running");
        Assert.NotNull(weldResult);
        Assert.True(weldResult.SkillExecutionTask.Progress > 0,
            "Weld should have non-zero progress when running");

        // --- Snapshot 3: Grasp2 running at 50% (t≈172.5) ---
        var grasp2StartUtc = _procedureStartTimeUtc.AddSeconds(145);
        var grasp2Progress = CreateAgentProgress(
            grasp2ExecId, grasp2, agentId, grasp2StartUtc,
            27.5, 55);

        var snapshot3States = new List<SkillExecutionState>
        {
            new(grasp1)
            {
                ExecutionStatus = ExecutionStatus.Completed,
                LastProgressPercentage = 100.0
            },
            new(hold)
            {
                ExecutionStatus = ExecutionStatus.Completed,
                LastProgressPercentage = 100.0
            },
            new(weld)
            {
                ExecutionStatus = ExecutionStatus.Completed,
                LastProgressPercentage = 100.0
            },
            new(grasp2)
            {
                ExecutionStatus = ExecutionStatus.Running,
                StartedAt = new DateTimeOffset(grasp2StartUtc, TimeSpan.Zero),
                LastProgress = grasp2Progress,
                LastProgressPercentage = 50.0
            }
        };

        var grasp2Result = await RunReschedulingPipelineWithStates(grasp2, snapshot3States, agentId);
        Assert.NotNull(grasp2Result);
        Assert.NotNull(grasp2Result.SkillExecutionTask.Progress);
        Assert.True(grasp2Result.SkillExecutionTask.Progress > 0,
            "Grasp2 should have non-zero progress when running");
    }

    #endregion

    #region Pipeline Helpers

    /// <summary>
    ///     Runs a single node through the rescheduling pipeline:
    ///     State → ProgressDataBuilder → ExecutionAwareDurationProvider → NodeTimingMapper.
    /// </summary>
    private async Task<SkillExecutionNode?> RunReschedulingPipeline(
        SkillExecutionNode node, SkillExecutionState state, Guid agentId)
    {
        return await RunReschedulingPipelineWithStates(node, [state], agentId);
    }

    /// <summary>
    ///     Runs a single node through the rescheduling pipeline using multiple states
    ///     (needed for multi-node scenarios where all states feed into the progress data builder).
    /// </summary>
    private async Task<SkillExecutionNode?> RunReschedulingPipelineWithStates(
        SkillExecutionNode node, IList<SkillExecutionState> states, Guid agentId)
    {
        // Step 1: Build progress data from all states
        var currentTime = DateTimeOffset.UtcNow;
        var progressData = _progressDataBuilder.BuildProgressData(states, currentTime);

        // Step 2: Create mock planning provider (fallback for nodes without progress)
        var mockPlanningProvider = CreateMockPlanningProvider(node, agentId);

        // Step 3: Create ExecutionAwareDurationProvider with progress data
        var durationProvider = new ExecutionAwareDurationProvider(
            mockPlanningProvider,
            _procedureStartTimeUtc,
            progressData,
            NullLogger<ExecutionAwareDurationProvider>.Instance);

        // Step 4: Analyze the node to get planned skill execution
        var plannedSkill = await durationProvider.AnalyzeAsync(node);
        if (plannedSkill == null)
            return null;

        // Step 5: Create timing info and apply via NodeTimingMapper
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [node.Id] = new()
            {
                AbsoluteStartTime = node.SkillExecutionTask.StartTime,
                AbsoluteFinishTime = node.SkillExecutionTask.StartTime + node.SkillExecutionTask.Duration,
                Duration = node.SkillExecutionTask.Duration,
                RelativeStartTime = node.SkillExecutionTask.StartTime,
                RelativeFinishTime = node.SkillExecutionTask.StartTime + node.SkillExecutionTask.Duration,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var durations = new Dictionary<Guid, double>
        {
            [node.Id] = node.SkillExecutionTask.Duration
        };

        var plannedSkills = new Dictionary<Guid, IPlannedSkillExecution>
        {
            [node.Id] = plannedSkill
        };

        var resultNode = _timingMapper.ApplyTimingToNode(node, timingInfo, durations, plannedSkills);
        return resultNode as SkillExecutionNode;
    }

    /// <summary>
    ///     Creates a mock planning provider that returns a basic planned skill execution
    ///     for fallback purposes.
    /// </summary>
    private static ISkillDurationProvider CreateMockPlanningProvider(
        SkillExecutionNode node, Guid agentId)
    {
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Id).Returns(agentId);
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        var plannedResult = new PlannedSkillExecution
        {
            Id = node.Id,
            PlannedDuration = node.SkillExecutionTask.Duration,
            Name = node.SkillExecutionTask.Name,
            DomainSkill = node.SkillExecutionTask.Skill,
            DomainAgent = new Agent
            {
                Id = agentId,
                Name = "TestAgent",
                RepresentativeColor = "#000000",
                SkillIds = [node.SkillExecutionTask.Skill.Id]
            },
            RuntimeAgent = mockAgent.Object
        };

        var mockProvider = new Mock<ISkillDurationProvider>();
        mockProvider
            .Setup(p => p.AnalyzeAsync(It.IsAny<SkillExecutionNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plannedResult);

        return mockProvider.Object;
    }

    #endregion

    #region Factory Helpers

    private static SkillExecutionNode CreateSkillExecutionNode(
        string skillName, Guid executionId, Guid agentId,
        double startTime, double duration)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = skillName,
            Description = $"{skillName} test skill",
            Properties = new List<TypedProperty>()
        };

        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"{skillName}Task",
                StartTime = startTime,
                Duration = duration,
                FinishTime = startTime + duration,
                Skill = skill,
                AgentId = agentId,
                ExecutionId = executionId
            }
        };
    }

    private static SkillExecutionProgress CreateAgentProgress(
        Guid executionId, SkillExecutionNode node, Guid agentId,
        DateTime startTimeUtc, double currentTimeIntoExecution, double estimatedTotalDuration)
    {
        return new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = node.SkillExecutionTask.Skill.Id,
            AgentId = agentId,
            ActualStartTimeUtc = startTimeUtc,
            CurrentTimeIntoExecution = currentTimeIntoExecution,
            EstimatedTotalDuration = estimatedTotalDuration,
            StatusMessage = $"Executing {node.SkillExecutionTask.Name}",
            CompletedSuccessfully = false
        };
    }

    #endregion
}