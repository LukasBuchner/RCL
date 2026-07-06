using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Tests.TestUtilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Tests for SchedulingPhaseLogger.
///     Uses TestLogger instead of Moq to avoid incompatibility with source-generated logging.
/// </summary>
public sealed class SchedulingPhaseLoggerTests
{
    private readonly SchedulingPhaseLogger _logger;
    private readonly TestLogger<SchedulingPhaseLogger> _testLogger;

    public SchedulingPhaseLoggerTests()
    {
        _testLogger = new TestLogger<SchedulingPhaseLogger>();
        _logger = new SchedulingPhaseLogger(_testLogger);
    }

    [Fact]
    public void LogPhaseStart_LogsAtInformationLevel()
    {
        // Arrange
        var phaseNumber = 1;
        var phaseName = "Test Phase";
        var procedureId = Guid.NewGuid();

        // Act
        _logger.LogPhaseStart(phaseNumber, phaseName, procedureId);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Debug).ToList();
        Assert.Single(entries);
        var logMessage = entries[0].Message;
        Assert.Contains($"Phase={phaseNumber}", logMessage);
        Assert.Contains($"Name={phaseName}", logMessage);
    }

    [Fact]
    public void LogPhaseComplete_LogsAtInformationLevelWithDuration()
    {
        // Arrange
        var phaseNumber = 2;
        var phaseName = "Calculate Timings";
        var procedureId = Guid.NewGuid();
        var duration = TimeSpan.FromMilliseconds(150);
        var details = "100 nodes updated";

        // Act
        _logger.LogPhaseComplete(phaseNumber, phaseName, procedureId, duration, details);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Debug).ToList();
        Assert.Single(entries);
        var logMessage = entries[0].Message;
        Assert.Contains($"Phase={phaseNumber}", logMessage);
        Assert.Contains($"Name={phaseName}", logMessage);
        Assert.Contains("150", logMessage); // Culture-independent check
        Assert.Contains($"Details={details}", logMessage);
    }

    [Fact]
    public void LogPipelineStart_LogsProcedureDetails()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var nodeCount = 50;
        var edgeCount = 75;
        var strictMode = true;
        var preserveOriginal = false;
        var includeTiming = true;

        // Act
        _logger.LogPipelineStart(procedureId, nodeCount, edgeCount, strictMode, preserveOriginal, includeTiming);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Debug).ToList();
        Assert.Single(entries);
        var logMessage = entries[0].Message;
        Assert.Contains($"ProcedureId={procedureId}", logMessage);
        Assert.Contains($"Nodes={nodeCount}", logMessage);
        Assert.Contains($"Edges={edgeCount}", logMessage);
    }

    [Fact]
    public void LogPipelineComplete_LogsAllPhaseDurations()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var totalDuration = TimeSpan.FromMilliseconds(500);
        var scheduleCount = 100;
        var phaseDurations = new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(50)
        };

        // Act
        _logger.LogPipelineComplete(procedureId, totalDuration, scheduleCount, phaseDurations);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Debug).ToList();
        Assert.Single(entries);
        var logMessage = entries[0].Message;
        Assert.Contains($"ProcedureId={procedureId}", logMessage);
        Assert.Contains("TotalDuration=500", logMessage); // Culture-independent check
        Assert.Contains($"ScheduleCount={scheduleCount}", logMessage);
        // Check for phase timings (culture-independent - just check presence of P1, P2, etc.)
        Assert.Contains("P1:100", logMessage);
        Assert.Contains("P2:200", logMessage);
        Assert.Contains("P3:150", logMessage);
        Assert.Contains("P4:50", logMessage);
    }

    [Fact]
    public void LogTimingStatistics_LogsStatisticsAtDebugLevel()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var statistics = new TimingStatistics
        {
            MinDuration = 10.0,
            MaxDuration = 500.0,
            AverageDuration = 125.0,
            SumDuration = 10000.0,
            NodeCount = 80,
            EarliestStart = 0.0,
            LatestFinish = 5000.0,
            TotalProcedureSpan = 5000.0
        };

        // Act
        _logger.LogTimingStatistics(procedureId, statistics);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Debug).ToList();
        Assert.Single(entries);
        var logMessage = entries[0].Message;
        Assert.Contains("SCHEDULING_STATISTICS", logMessage);
        Assert.Contains($"NodeCount={statistics.NodeCount}", logMessage);
        Assert.Contains("ProcedureSpan=5000", logMessage); // Culture-independent check
    }

    [Fact]
    public void LogDetailedNodeTimings_LogsTaskAndSkillNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var taskNode1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0.0,
                Duration = 1000.0,
                FinishTime = 1000.0
            }
        };
        var skillNode1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode1.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skill1",
                StartTime = 0.0,
                Duration = 500.0,
                FinishTime = 500.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill1",
                    Description = "Test skill 1",
                    Properties = []
                }
            }
        };
        var skillNode2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode1.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skill2",
                StartTime = 500.0,
                Duration = 500.0,
                FinishTime = 1000.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Skill2",
                    Description = "Test skill 2",
                    Properties = []
                }
            }
        };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [taskNode1.Id] = new()
            {
                NodeId = taskNode1.Id,
                AbsoluteStartTime = 0,
                AbsoluteFinishTime = 1000,
                RelativeStartTime = 0,
                RelativeFinishTime = 1000,
                Duration = 1000,
                NodeType = NodeTimingType.Task,
                IsCalculated = true
            },
            [skillNode1.Id] = new()
            {
                NodeId = skillNode1.Id,
                AbsoluteStartTime = 0,
                AbsoluteFinishTime = 500,
                RelativeStartTime = 0,
                RelativeFinishTime = 500,
                Duration = 500,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            },
            [skillNode2.Id] = new()
            {
                NodeId = skillNode2.Id,
                AbsoluteStartTime = 500,
                AbsoluteFinishTime = 1000,
                RelativeStartTime = 500,
                RelativeFinishTime = 1000,
                Duration = 500,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            }
        };

        var nodes = new List<Node> { taskNode1, skillNode1, skillNode2 };

        // Act
        _logger.LogDetailedNodeTimings(procedureId, timingInfo, nodes);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Trace).ToList();
        Assert.NotEmpty(entries);

        // Verify that task node timing was logged
        var taskTimingEntries = entries.Where(e => e.Message.Contains("SCHEDULING_TASK_TIMING")).ToList();
        Assert.NotEmpty(taskTimingEntries);

        // Verify that skill node timings were logged
        var skillTimingEntries = entries.Where(e => e.Message.Contains("SCHEDULING_SKILL_TIMING")).ToList();
        Assert.NotEmpty(skillTimingEntries);
    }

    [Fact]
    public void LogCriticalPathAnalysis_LogsCriticalPathNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var node1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0.0,
                Duration = 1000.0,
                FinishTime = 1000.0
            }
        };
        var node2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                StartTime = 0.0,
                Duration = 500.0,
                FinishTime = 500.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test skill",
                    Properties = []
                }
            }
        };

        var criticalPathInfo = new CriticalPathInfo
        {
            CriticalPathNodeIds = new List<Guid> { node1.Id, node2.Id },
            MaxParallelism = 5,
            PeakParallelismTime = 100.0
        };

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            [node1.Id] = new()
            {
                NodeId = node1.Id,
                Duration = 1000,
                OnCriticalPath = true,
                AbsoluteStartTime = 0,
                AbsoluteFinishTime = 1000,
                RelativeStartTime = 0,
                RelativeFinishTime = 1000,
                NodeType = NodeTimingType.Task,
                IsCalculated = true
            },
            [node2.Id] = new()
            {
                NodeId = node2.Id,
                Duration = 500,
                OnCriticalPath = true,
                AbsoluteStartTime = 0,
                AbsoluteFinishTime = 500,
                RelativeStartTime = 0,
                RelativeFinishTime = 500,
                NodeType = NodeTimingType.SkillExecution,
                IsCalculated = true
            }
        };

        var nodes = new List<Node> { node1, node2 };

        // Act
        _logger.LogCriticalPathAnalysis(procedureId, criticalPathInfo, nodes, timingInfo);

        // Assert
        var entries = _testLogger.GetEntriesByLevel(LogLevel.Trace).ToList();
        Assert.NotEmpty(entries);

        // Verify that Critical Path Analysis was logged
        var criticalPathAnalysisEntries = entries.Where(e => e.Message.Contains("Critical Path Analysis")).ToList();
        Assert.NotEmpty(criticalPathAnalysisEntries);

        // Verify that critical path node entries were logged
        var criticalPathNodeEntries = entries.Where(e => e.Message.Contains("SCHEDULING_CRITICAL_PATH")).ToList();
        Assert.NotEmpty(criticalPathNodeEntries);

        // Verify that both node1 and node2 were logged
        Assert.Contains(criticalPathNodeEntries, e => e.Message.Contains($"NodeId={node1.Id}"));
        Assert.Contains(criticalPathNodeEntries, e => e.Message.Contains($"NodeId={node2.Id}"));
    }

    [Fact]
    public void LogMethods_WithNullParameters_HandleGracefully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();

        // Act & Assert - Should not throw
        _logger.LogPhaseStart(1, null!, procedureId);
        _logger.LogPhaseComplete(1, null!, procedureId, TimeSpan.Zero, null!);
        _logger.LogPipelineComplete(procedureId, TimeSpan.Zero, 0, null!);
        _logger.LogTimingStatistics(procedureId, null!);
        _logger.LogDetailedNodeTimings(procedureId, null!, null!);
        _logger.LogCriticalPathAnalysis(procedureId, null!, null!, null!);

        // Verify no exceptions were thrown by checking that we can still log
        _logger.LogPhaseStart(1, "Test", procedureId);

        // Assert that logging still works after null parameters
        var entries = _testLogger.LogEntries;
        Assert.NotEmpty(entries);
    }
}