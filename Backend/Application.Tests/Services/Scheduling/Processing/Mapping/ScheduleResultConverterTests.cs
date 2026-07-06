using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Comprehensive unit tests for ScheduleResultConverter.
///     Tests cover all scenarios: null inputs, empty results, single nodes, multiple nodes, and different node types.
/// </summary>
public class ScheduleResultConverterTests
{
    private readonly ScheduleResultConverter _converter = new(NullLogger<ScheduleResultConverter>.Instance);

    #region Multiple Node Tests

    [Fact]
    public void ConvertTimingToScheduleResult_WithMultipleNodes_ShouldReturnAllSchedules()
    {
        // Arrange
        var taskNodeId = Guid.NewGuid();
        var skillNodeId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var taskNode = CreateTaskNode("Task1", 10.0, taskNodeId, parentId);
        var skillNode = CreateSkillExecutionNode("Skill1", 8.0, skillNodeId, taskNodeId);
        var nodes = new List<Node> { taskNode, skillNode };

        var detailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [taskNodeId] = new()
            {
                Duration = 15.0,
                AbsoluteStartTime = 5.0,
                AbsoluteFinishTime = 20.0,
                RelativeStartTime = 2.0,
                RelativeFinishTime = 17.0,
                NodeType = NodeTimingType.Task,
                IsCalculated = true
            },
            [skillNodeId] = new()
            {
                Duration = 12.0,
                AbsoluteStartTime = 6.0,
                AbsoluteFinishTime = 18.0,
                RelativeStartTime = 1.0,
                RelativeFinishTime = 13.0,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            }
        };

        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double> { [taskNodeId] = 15.0, [skillNodeId] = 12.0 },
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = detailedTimingInfo
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.NodeSchedules.Count);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(nodes, result.UpdatedNodes);

        // Check task node schedule
        var taskSchedule = result.NodeSchedules.First(s => s.NodeId == taskNodeId);
        Assert.Equal(NodeScheduleType.TaskNode, taskSchedule.NodeType);
        Assert.Equal(parentId, taskSchedule.ParentNodeId);
        Assert.Equal(15.0, taskSchedule.Duration);

        // Check skill node schedule
        var skillSchedule = result.NodeSchedules.First(s => s.NodeId == skillNodeId);
        Assert.Equal(NodeScheduleType.SkillExecutionNode, skillSchedule.NodeType);
        Assert.Equal(taskNodeId, skillSchedule.ParentNodeId);
        Assert.Equal(12.0, skillSchedule.Duration);
    }

    #endregion

    #region NodeType Conversion Tests

    [Theory]
    [InlineData(NodeTimingType.SkillExecution, NodeScheduleType.SkillExecutionNode)]
    [InlineData(NodeTimingType.Task, NodeScheduleType.TaskNode)]
    [InlineData(NodeTimingType.Original, NodeScheduleType.TaskNode)]
    public void ConvertTimingToScheduleResult_WithDifferentTimingTypes_ShouldConvertCorrectly(
        NodeTimingType inputType, NodeScheduleType expectedType)
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = CreateTaskNode("Test", 10.0, nodeId);
        var nodes = new List<Node> { node };

        var detailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [nodeId] = new()
            {
                Duration = 10.0,
                AbsoluteStartTime = 0.0,
                AbsoluteFinishTime = 10.0,
                RelativeStartTime = 0.0,
                RelativeFinishTime = 10.0,
                NodeType = inputType,
                IsCalculated = true
            }
        };

        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double> { [nodeId] = 10.0 },
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = detailedTimingInfo
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        var schedule = result.NodeSchedules.First();
        Assert.Equal(expectedType, schedule.NodeType);
    }

    #endregion

    #region Missing Node Tests

    [Fact]
    public void ConvertTimingToScheduleResult_WithTimingForMissingNode_ShouldCreateScheduleWithNullParent()
    {
        // Arrange
        var missingNodeId = Guid.NewGuid();
        var nodes = new List<Node>(); // No nodes provided

        var detailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [missingNodeId] = new()
            {
                Duration = 10.0,
                AbsoluteStartTime = 0.0,
                AbsoluteFinishTime = 10.0,
                RelativeStartTime = 0.0,
                RelativeFinishTime = 10.0,
                NodeType = NodeTimingType.Task,
                IsCalculated = true
            }
        };

        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double> { [missingNodeId] = 10.0 },
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = detailedTimingInfo
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.NodeSchedules);

        var schedule = result.NodeSchedules.First();
        Assert.Equal(missingNodeId, schedule.NodeId);
        Assert.Null(schedule.ParentNodeId); // Should be null since node wasn't found
    }

    #endregion

    #region Constructor and Null Argument Tests

    [Fact]
    public void ConvertTimingToScheduleResult_WithNullTimingResult_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _converter.ConvertTimingToScheduleResult(null!, nodes));
    }

    [Fact]
    public void ConvertTimingToScheduleResult_WithNullNodesWithPositions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double>(),
            UpdatedNodes = new List<Node>().AsReadOnly(),
            DetailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>()
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _converter.ConvertTimingToScheduleResult(timingResult, null!));
    }

    #endregion

    #region Empty and Null DetailedTimingInfo Tests

    [Fact]
    public void ConvertTimingToScheduleResult_WithNullDetailedTimingInfo_ShouldReturnSuccessWithEmptySchedules()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Task1", 10.0) };
        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double>(),
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = null
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(nodes, result.UpdatedNodes);
    }

    [Fact]
    public void ConvertTimingToScheduleResult_WithEmptyDetailedTimingInfo_ShouldReturnSuccessWithEmptySchedules()
    {
        // Arrange
        var nodes = new List<Node> { CreateTaskNode("Task1", 10.0) };
        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double>(),
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>()
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.NodeSchedules);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(nodes, result.UpdatedNodes);
    }

    #endregion

    #region Single Node Tests

    [Fact]
    public void ConvertTimingToScheduleResult_WithSingleTaskNode_ShouldReturnCorrectSchedule()
    {
        // Arrange
        var taskNodeId = Guid.NewGuid();
        var taskNode = CreateTaskNode("Task1", 10.0, taskNodeId);
        var nodes = new List<Node> { taskNode };

        var detailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [taskNodeId] = new()
            {
                Duration = 15.0,
                AbsoluteStartTime = 5.0,
                AbsoluteFinishTime = 20.0,
                RelativeStartTime = 2.0,
                RelativeFinishTime = 17.0,
                NodeType = NodeTimingType.Task,
                IsCalculated = true
            }
        };

        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double> { [taskNodeId] = 15.0 },
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = detailedTimingInfo
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.NodeSchedules);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(nodes, result.UpdatedNodes);

        var schedule = result.NodeSchedules.First();
        Assert.Equal(taskNodeId, schedule.NodeId);
        Assert.Equal(15.0, schedule.Duration);
        Assert.Equal(5.0, schedule.AbsoluteStartTime);
        Assert.Equal(20.0, schedule.AbsoluteFinishTime);
        Assert.Equal(2.0, schedule.RelativeStartTime);
        Assert.Equal(17.0, schedule.RelativeFinishTime);
        Assert.Equal(NodeScheduleType.TaskNode, schedule.NodeType);
        Assert.Null(schedule.ParentNodeId);
    }

    [Fact]
    public void ConvertTimingToScheduleResult_WithSingleSkillNode_ShouldReturnCorrectScheduleWithParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var skillNodeId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNode("Skill1", 8.0, skillNodeId, parentId);
        var nodes = new List<Node> { skillNode };

        var detailedTimingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [skillNodeId] = new()
            {
                Duration = 12.0,
                AbsoluteStartTime = 3.0,
                AbsoluteFinishTime = 15.0,
                RelativeStartTime = 1.0,
                RelativeFinishTime = 13.0,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            }
        };

        var timingResult = new TimingResult
        {
            Success = true,
            Durations = new Dictionary<Guid, double> { [skillNodeId] = 12.0 },
            UpdatedNodes = nodes.AsReadOnly(),
            DetailedTimingInfo = detailedTimingInfo
        };

        // Act
        var result = _converter.ConvertTimingToScheduleResult(timingResult, nodes);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.NodeSchedules);

        var schedule = result.NodeSchedules.First();
        Assert.Equal(skillNodeId, schedule.NodeId);
        Assert.Equal(12.0, schedule.Duration);
        Assert.Equal(3.0, schedule.AbsoluteStartTime);
        Assert.Equal(15.0, schedule.AbsoluteFinishTime);
        Assert.Equal(1.0, schedule.RelativeStartTime);
        Assert.Equal(13.0, schedule.RelativeFinishTime);
        Assert.Equal(NodeScheduleType.SkillExecutionNode, schedule.NodeType);
        Assert.Equal(parentId, schedule.ParentNodeId);
    }

    #endregion

    #region Helper Methods

    private static TaskNode CreateTaskNode(string name, double duration, Guid? id = null, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 50,
            Task = new Task
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string name, double duration, Guid? id = null,
        Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 50,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                Duration = duration,
                StartTime = 0,
                FinishTime = duration,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Skill for {name}",
                    Properties =
                    [
                    ]
                },
                AgentId = Guid.NewGuid()
            },
            ProcedureId = default
        };
    }

    #endregion
}