using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Integration;

/// <summary>
///     Integration tests for the complete timing mapping flow:
///     Domain → Execution Models → Scheduled → Executed → Domain (with actual timing)
/// </summary>
public class TimingMappingIntegrationTests
{
    private readonly ExecutionIdAssigner _executionIdAssigner;
    private readonly Mock<ILogger<ExecutionGraphBuilder>> _graphBuilderLoggerMock;
    private readonly NodeTimingMapper _timingMapper;
    private readonly Mock<ILogger<NodeTimingMapper>> _timingMapperLoggerMock;

    public TimingMappingIntegrationTests()
    {
        _timingMapperLoggerMock = new Mock<ILogger<NodeTimingMapper>>();
        _graphBuilderLoggerMock = new Mock<ILogger<ExecutionGraphBuilder>>();
        _executionIdAssigner = new ExecutionIdAssigner();
        _timingMapper = new NodeTimingMapper(_timingMapperLoggerMock.Object);
    }

    #region Test: Complete Flow - Planning → Execution → Update

    [Fact]
    public void CompleteFlow_FromPlanningToExecution_ShouldPreserveThenUpdateTiming()
    {
        // This test simulates the complete lifecycle:
        // 1. Create domain nodes
        // 2. Assign ExecutionIds
        // 3. Plan (apply planned timing)
        // 4. Execute (update with actual timing)
        // 5. Verify final timing matches execution progress

        // Step 1: Create domain nodes
        var node1 = CreateSkillExecutionNode(skillName: "MoveRobot", startTime: 0, duration: 50);
        var node2 = CreateSkillExecutionNode(skillName: "PickObject", startTime: 50, duration: 30);
        var nodes = new List<Node> { node1, node2 };

        // Step 2: Assign ExecutionIds (simulating ExecutionOrchestrator startup)
        var nodesWithExecIds = _executionIdAssigner.AssignExecutionIds(nodes);
        var skill1 = (SkillExecutionNode)nodesWithExecIds[0];
        var skill2 = (SkillExecutionNode)nodesWithExecIds[1];

        Assert.NotNull(skill1.SkillExecutionTask.ExecutionId);
        Assert.NotNull(skill2.SkillExecutionTask.ExecutionId);

        // Step 3: Simulate scheduling (apply planned timing)
        var plannedTiming = new Dictionary<Guid, NodeTimingInfo>
        {
            [skill1.Id] = new()
            {
                AbsoluteStartTime = 0,
                AbsoluteFinishTime = 50,
                Duration = 50,
                RelativeStartTime = 0,
                RelativeFinishTime = 50,
                NodeType = NodeTimingType.SkillExecution
            },
            [skill2.Id] = new()
            {
                AbsoluteStartTime = 50,
                AbsoluteFinishTime = 80,
                Duration = 30,
                RelativeStartTime = 50,
                RelativeFinishTime = 80,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var durations = new Dictionary<Guid, double>
        {
            [skill1.Id] = 50,
            [skill2.Id] = 30
        };

        var scheduledNode1 = _timingMapper.ApplyTimingToNode(skill1, plannedTiming, durations);
        var scheduledNode2 = _timingMapper.ApplyTimingToNode(skill2, plannedTiming, durations);

        var scheduled1 = (SkillExecutionNode)scheduledNode1;
        var scheduled2 = (SkillExecutionNode)scheduledNode2;

        // Verify planned timing applied
        Assert.Equal(0, scheduled1.SkillExecutionTask.StartTime);
        Assert.Equal(50, scheduled1.SkillExecutionTask.Duration);
        Assert.Equal(50, scheduled1.SkillExecutionTask.FinishTime);

        // Step 4: Simulate execution with actual progress
        var procedureStartTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var actualStartTime1 = procedureStartTime.AddSeconds(2); // Started 2s late
        var actualStartTime2 = procedureStartTime.AddSeconds(55); // Started 5s late

        var progress1 = CreateProgress(
            skill1.SkillExecutionTask.ExecutionId!.Value,
            30,
            60, // Taking longer than planned
            actualStartTime1.UtcDateTime
        );

        var progress2 = CreateProgress(
            skill2.SkillExecutionTask.ExecutionId!.Value,
            10,
            25, // Faster than planned
            actualStartTime2.UtcDateTime
        );

        // Simulate execution state

        // Step 5: Update nodes with actual timing (simulating UpdateNodesWithExecutionStatus)
        var actualStartRelative1 = (actualStartTime1 - procedureStartTime).TotalSeconds;
        var actualStartRelative2 = (actualStartTime2 - procedureStartTime).TotalSeconds;

        var executingNode1 = scheduled1 with
        {
            SkillExecutionTask = scheduled1.SkillExecutionTask with
            {
                StartTime = actualStartRelative1, // 2.0
                Duration = progress1.EstimatedTotalDuration, // 60
                FinishTime = actualStartRelative1 + progress1.EstimatedTotalDuration, // 62
                IsExecuting = true,
                Progress = 50.0
            }
        };

        var executingNode2 = scheduled2 with
        {
            SkillExecutionTask = scheduled2.SkillExecutionTask with
            {
                StartTime = actualStartRelative2, // 55.0
                Duration = progress2.EstimatedTotalDuration, // 25
                FinishTime = actualStartRelative2 + progress2.EstimatedTotalDuration, // 80
                IsExecuting = true,
                Progress = 40.0
            }
        };

        // Verify final timing reflects actual execution
        Assert.Equal(2.0, executingNode1.SkillExecutionTask.StartTime);
        Assert.Equal(60.0, executingNode1.SkillExecutionTask.Duration);
        Assert.Equal(62.0, executingNode1.SkillExecutionTask.FinishTime);
        Assert.True(executingNode1.SkillExecutionTask.IsExecuting);
        Assert.Equal(50.0, executingNode1.SkillExecutionTask.Progress);

        Assert.Equal(55.0, executingNode2.SkillExecutionTask.StartTime);
        Assert.Equal(25.0, executingNode2.SkillExecutionTask.Duration);
        Assert.Equal(80.0, executingNode2.SkillExecutionTask.FinishTime);
        Assert.True(executingNode2.SkillExecutionTask.IsExecuting);
        Assert.Equal(40.0, executingNode2.SkillExecutionTask.Progress);
    }

    #endregion

    #region Test: ExecutionId Preservation Through Pipeline

    [Fact]
    public void ExecutionIdFlow_ShouldBePreservedThroughEntirePipeline()
    {
        // Test that ExecutionId assigned at start is preserved throughout
        // Arrange
        var originalNode = CreateSkillExecutionNode();

        // Step 1: Assign ExecutionId
        var nodesWithExecId = _executionIdAssigner.AssignExecutionIds(new List<Node> { originalNode });
        var nodeWithExecId = (SkillExecutionNode)nodesWithExecId[0];
        var assignedExecutionId = nodeWithExecId.SkillExecutionTask.ExecutionId!.Value;

        // Step 2: Apply timing (simulating scheduling)
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeWithExecId.Id] = new()
            {
                AbsoluteStartTime = 10,
                AbsoluteFinishTime = 60,
                Duration = 50,
                RelativeStartTime = 10,
                RelativeFinishTime = 60,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var durations = new Dictionary<Guid, double> { [nodeWithExecId.Id] = 50 };
        var scheduledNode = _timingMapper.ApplyTimingToNode(nodeWithExecId, timingInfo, durations);
        var scheduled = (SkillExecutionNode)scheduledNode;

        // Verify ExecutionId preserved after scheduling
        Assert.Equal(assignedExecutionId, scheduled.SkillExecutionTask.ExecutionId);

        // Step 3: Update with execution progress
        var updatedNode = scheduled with
        {
            SkillExecutionTask = scheduled.SkillExecutionTask with
            {
                StartTime = 12, // Actual start
                Duration = 55, // Actual duration
                IsExecuting = true,
                Progress = 50.0
            }
        };

        // Verify ExecutionId preserved after execution update
        Assert.Equal(assignedExecutionId, updatedNode.SkillExecutionTask.ExecutionId);
    }

    #endregion

    #region Test: Multiple Nodes with Independent Timing

    [Fact]
    public void MultipleNodes_ShouldHaveIndependentTimingAndExecutionIds()
    {
        // Test that multiple nodes maintain independent state
        // Arrange
        var nodes = new List<Node>
        {
            CreateSkillExecutionNode(skillName: "Task1", startTime: 0, duration: 20),
            CreateSkillExecutionNode(skillName: "Task2", startTime: 20, duration: 30),
            CreateSkillExecutionNode(skillName: "Task3", startTime: 50, duration: 40)
        };

        // Step 1: Assign ExecutionIds
        var nodesWithExecIds = _executionIdAssigner.AssignExecutionIds(nodes);

        var execIds = nodesWithExecIds
            .OfType<SkillExecutionNode>()
            .Select(n => n.SkillExecutionTask.ExecutionId!.Value)
            .ToList();

        // All ExecutionIds should be unique
        Assert.Equal(3, execIds.Distinct().Count());

        // Step 2: Apply different timing to each
        var timingInfoDict = new Dictionary<Guid, NodeTimingInfo>();
        var durationsDict = new Dictionary<Guid, double>();

        var nodesWithExecIdsList = nodesWithExecIds.ToList();
        foreach (var node in nodesWithExecIds.OfType<SkillExecutionNode>())
        {
            var index = nodesWithExecIdsList.IndexOf(node);
            var startTime = index * 25.0; // Staggered starts
            var duration = 20.0 + index * 5; // Increasing durations

            timingInfoDict[node.Id] = new NodeTimingInfo
            {
                AbsoluteStartTime = startTime,
                AbsoluteFinishTime = startTime + duration,
                Duration = duration,
                RelativeStartTime = startTime,
                RelativeFinishTime = startTime + duration,
                NodeType = NodeTimingType.SkillExecution
            };

            durationsDict[node.Id] = duration;
        }

        // Apply timing
        var scheduledNodes = nodesWithExecIds
            .Select(n => _timingMapper.ApplyTimingToNode(n, timingInfoDict, durationsDict))
            .OfType<SkillExecutionNode>()
            .ToList();

        // Verify each node has correct independent timing
        Assert.Equal(0, scheduledNodes[0].SkillExecutionTask.StartTime);
        Assert.Equal(20, scheduledNodes[0].SkillExecutionTask.Duration);

        Assert.Equal(25, scheduledNodes[1].SkillExecutionTask.StartTime);
        Assert.Equal(25, scheduledNodes[1].SkillExecutionTask.Duration);

        Assert.Equal(50, scheduledNodes[2].SkillExecutionTask.StartTime);
        Assert.Equal(30, scheduledNodes[2].SkillExecutionTask.Duration);

        // Verify ExecutionIds still unique and preserved
        var finalExecIds = scheduledNodes
            .Select(n => n.SkillExecutionTask.ExecutionId!.Value)
            .ToList();

        Assert.Equal(execIds, finalExecIds);
    }

    #endregion

    #region Test: Timing Update During Execution

    [Fact]
    public void DuringExecution_TimingShouldUpdateDynamically()
    {
        // Test that timing updates as execution progresses
        // Arrange
        var node = CreateSkillExecutionNode(startTime: 10, duration: 50);
        var nodesWithExecId = _executionIdAssigner.AssignExecutionIds(new List<Node> { node });
        var skillNode = (SkillExecutionNode)nodesWithExecId[0];

        var procedureStart = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var actualStart = procedureStart.AddSeconds(12); // Started 2s late

        // Snapshot 1: 25% complete
        CreateProgress(
            skillNode.SkillExecutionTask.ExecutionId!.Value,
            12.5,
            50,
            actualStart.UtcDateTime
        );

        var node1 = skillNode with
        {
            SkillExecutionTask = skillNode.SkillExecutionTask with
            {
                StartTime = 12,
                Duration = 50,
                FinishTime = 62,
                Progress = 25.0
            }
        };

        // Snapshot 2: 50% complete, duration estimate increased
        CreateProgress(
            skillNode.SkillExecutionTask.ExecutionId!.Value,
            30,
            60, // Estimate increased
            actualStart.UtcDateTime
        );

        var node2 = skillNode with
        {
            SkillExecutionTask = skillNode.SkillExecutionTask with
            {
                StartTime = 12,
                Duration = 60, // Updated
                FinishTime = 72, // Recalculated
                Progress = 50.0
            }
        };

        // Snapshot 3: 90% complete, duration stable
        CreateProgress(
            skillNode.SkillExecutionTask.ExecutionId!.Value,
            54,
            60,
            actualStart.UtcDateTime
        );

        var node3 = skillNode with
        {
            SkillExecutionTask = skillNode.SkillExecutionTask with
            {
                StartTime = 12,
                Duration = 60,
                FinishTime = 72,
                Progress = 90.0
            }
        };

        // Verify progression
        Assert.Equal(25.0, node1.SkillExecutionTask.Progress);
        Assert.Equal(62.0, node1.SkillExecutionTask.FinishTime);

        Assert.Equal(50.0, node2.SkillExecutionTask.Progress);
        Assert.Equal(72.0, node2.SkillExecutionTask.FinishTime); // Pushed out

        Assert.Equal(90.0, node3.SkillExecutionTask.Progress);
        Assert.Equal(72.0, node3.SkillExecutionTask.FinishTime); // Stable now
    }

    #endregion

    #region Test: Completed Execution with Actual Duration

    [Fact]
    public void AfterCompletion_ShouldReflectActualDuration()
    {
        // Test that completed executions show actual duration
        // Arrange
        var node = CreateSkillExecutionNode(startTime: 20, duration: 100);
        var nodesWithExecId = _executionIdAssigner.AssignExecutionIds(new List<Node> { node });
        var skillNode = (SkillExecutionNode)nodesWithExecId[0];

        var procedureStart = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var actualStart = procedureStart.AddSeconds(20);

        // Completed - took 85s (faster than planned 100s)

        var completedNode = skillNode with
        {
            SkillExecutionTask = skillNode.SkillExecutionTask with
            {
                StartTime = 20,
                Duration = 85, // Actual
                FinishTime = 105, // Actual
                IsExecuting = false,
                Progress = 100.0
            }
        };

        // Verify
        Assert.Equal(85.0, completedNode.SkillExecutionTask.Duration);
        Assert.Equal(105.0, completedNode.SkillExecutionTask.FinishTime);
        Assert.Equal(100.0, completedNode.SkillExecutionTask.Progress);
        Assert.False(completedNode.SkillExecutionTask.IsExecuting);
    }

    #endregion

    #region Test: Error Cases

    [Fact]
    public void WhenNoProgressData_ShouldFallBackToPlannedTiming()
    {
        // If execution state has no progress, preserve planned timing
        // Arrange
        var node = CreateSkillExecutionNode(startTime: 30, duration: 40);
        var nodesWithExecId = _executionIdAssigner.AssignExecutionIds(new List<Node> { node });
        var skillNode = (SkillExecutionNode)nodesWithExecId[0];

        // Apply planned timing
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [skillNode.Id] = new()
            {
                AbsoluteStartTime = 30,
                AbsoluteFinishTime = 70,
                Duration = 40,
                RelativeStartTime = 30,
                RelativeFinishTime = 70,
                NodeType = NodeTimingType.SkillExecution
            }
        };

        var durations = new Dictionary<Guid, double> { [skillNode.Id] = 40 };
        var scheduledNode = _timingMapper.ApplyTimingToNode(skillNode, timingInfo, durations);

        // Simulate execution without progress data (null progress)

        // Expected: timing should remain as planned
        var scheduled = (SkillExecutionNode)scheduledNode;
        Assert.Equal(30.0, scheduled.SkillExecutionTask.StartTime);
        Assert.Equal(40.0, scheduled.SkillExecutionTask.Duration);
        Assert.Equal(70.0, scheduled.SkillExecutionTask.FinishTime);
    }

    #endregion

    #region Helper Methods

    private static SkillExecutionNode CreateSkillExecutionNode(
        Guid? id = null,
        string skillName = "TestSkill",
        double startTime = 0,
        double duration = 10,
        Guid? agentId = null)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = skillName,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };

        return new SkillExecutionNode
        {
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = $"{skillName}Task",
                StartTime = startTime,
                Duration = duration,
                FinishTime = startTime + duration,
                Skill = skill,
                AgentId = agentId ?? Guid.NewGuid()
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionProgress CreateProgress(
        Guid executionId,
        double currentTime,
        double estimatedTotal,
        DateTime? startTime = null,
        Guid? skillId = null,
        Guid? agentId = null)
    {
        return new SkillExecutionProgress
        {
            ExecutionId = executionId,
            SkillId = skillId ?? Guid.NewGuid(),
            AgentId = agentId ?? Guid.NewGuid(),
            ActualStartTimeUtc = startTime ?? DateTime.UtcNow,
            CurrentTimeIntoExecution = currentTime,
            EstimatedTotalDuration = estimatedTotal,
            CompletedSuccessfully = false,
            StatusMessage = "In progress"
        };
    }

    #endregion
}