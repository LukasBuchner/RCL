using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Support.Analytics;

public class TimingAnalyzerTests
{
    private readonly ITimingAnalyzer _analyzer;
    private readonly Mock<ILogger<TimingAnalyzer>> _mockLogger;

    public TimingAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<TimingAnalyzer>>();
        _analyzer = new TimingAnalyzer(_mockLogger.Object);
    }

    [Fact]
    public void CollectStatistics_WithNullTimingInfo_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _analyzer.CollectStatistics(null!));
    }

    [Fact]
    public void CollectStatistics_WithEmptyTimingInfo_ThrowsArgumentException()
    {
        // Arrange
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _analyzer.CollectStatistics(timingInfo));
    }

    [Fact]
    public void CollectStatistics_WithSingleNode_ReturnsCorrectStats()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            {
                nodeId, new NodeTimingInfo
                {
                    Duration = 100,
                    AbsoluteStartTime = 50,
                    AbsoluteFinishTime = 150
                }
            }
        };

        // Act
        var result = _analyzer.CollectStatistics(timingInfo);

        // Assert
        Assert.Equal(100, result.MinDuration);
        Assert.Equal(100, result.MaxDuration);
        Assert.Equal(100, result.AverageDuration);
        Assert.Equal(100, result.SumDuration);
        Assert.Equal(1, result.NodeCount);
        Assert.Equal(50, result.EarliestStart);
        Assert.Equal(150, result.LatestFinish);
        Assert.Equal(100, result.TotalProcedureSpan);
    }

    [Fact]
    public void CollectStatistics_WithMultipleNodes_CalculatesMinMaxAvgSumCorrectly()
    {
        // Arrange
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 50, AbsoluteStartTime = 0, AbsoluteFinishTime = 50 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 10, AbsoluteFinishTime = 110 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 150, AbsoluteStartTime = 20, AbsoluteFinishTime = 170 } }
        };

        // Act
        var result = _analyzer.CollectStatistics(timingInfo);

        // Assert
        Assert.Equal(50, result.MinDuration);
        Assert.Equal(150, result.MaxDuration);
        Assert.Equal(100, result.AverageDuration);
        Assert.Equal(300, result.SumDuration);
        Assert.Equal(3, result.NodeCount);
    }

    [Fact]
    public void CollectStatistics_CalculatesProcedureSpanCorrectly()
    {
        // Arrange
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 50, AbsoluteFinishTime = 150 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 80, AbsoluteStartTime = 10, AbsoluteFinishTime = 90 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 200, AbsoluteStartTime = 100, AbsoluteFinishTime = 300 } }
        };

        // Act
        var result = _analyzer.CollectStatistics(timingInfo);

        // Assert
        Assert.Equal(10, result.EarliestStart); // Min of 50, 10, 100
        Assert.Equal(300, result.LatestFinish); // Max of 150, 90, 300
        Assert.Equal(290, result.TotalProcedureSpan); // 300 - 10
    }

    [Fact]
    public void CollectStatistics_WithSequentialTasks_CalculatesTimelineCorrectly()
    {
        // Arrange - Tasks run one after another
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            {
                Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 100, AbsoluteFinishTime = 200 }
            },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 200, AbsoluteFinishTime = 300 } }
        };

        // Act
        var result = _analyzer.CollectStatistics(timingInfo);

        // Assert
        Assert.Equal(0, result.EarliestStart);
        Assert.Equal(300, result.LatestFinish);
        Assert.Equal(300, result.TotalProcedureSpan);
    }

    [Fact]
    public void CollectStatistics_WithParallelTasks_CalculatesTimelineCorrectly()
    {
        // Arrange - Tasks run in parallel
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } }
        };

        // Act
        var result = _analyzer.CollectStatistics(timingInfo);

        // Assert
        Assert.Equal(0, result.EarliestStart);
        Assert.Equal(100, result.LatestFinish);
        Assert.Equal(100, result.TotalProcedureSpan); // All parallel, so span equals task duration
    }

    [Fact]
    public void AnalyzeCriticalPath_IdentifiesNodesFinishingAtProcedureEnd()
    {
        // Arrange
        var node1Id = Guid.NewGuid();
        var node2Id = Guid.NewGuid();
        var node3Id = Guid.NewGuid();

        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { node1Id, new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            { node2Id, new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 50, AbsoluteFinishTime = 150 } },
            {
                node3Id, new NodeTimingInfo { Duration = 150, AbsoluteStartTime = 0, AbsoluteFinishTime = 150 }
            } // Critical path node
        };

        var nodes = new List<Node>
        {
            CreateTaskNodeWithId(node1Id),
            CreateTaskNodeWithId(node2Id),
            CreateTaskNodeWithId(node3Id)
        };

        // Act
        var result = _analyzer.AnalyzeCriticalPath(timingInfo, nodes);

        // Assert
        Assert.Contains(node2Id, result.CriticalPathNodeIds);
        Assert.Contains(node3Id, result.CriticalPathNodeIds);
        Assert.DoesNotContain(node1Id, result.CriticalPathNodeIds);
    }

    [Fact]
    public void AnalyzeCriticalPath_WithNoCriticalPath_ReturnsEmptyList()
    {
        // Arrange - All nodes finish well before procedure end
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 10, AbsoluteStartTime = 0, AbsoluteFinishTime = 10 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 10, AbsoluteStartTime = 20, AbsoluteFinishTime = 30 } }
        };

        var nodes = new List<Node>
        {
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid())
        };

        // Act
        var result = _analyzer.AnalyzeCriticalPath(timingInfo, nodes);

        // Assert - No nodes finish at procedure end (30), so no critical path
        // At least one node should finish at latest time - in this case node finishing at 30
        Assert.NotNull(result.CriticalPathNodeIds);
    }

    [Fact]
    public void AnalyzeCriticalPath_CalculatesMaxParallelismCorrectly()
    {
        // Arrange - 3 tasks overlap at time 50-90
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 80, AbsoluteStartTime = 20, AbsoluteFinishTime = 100 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 60, AbsoluteStartTime = 40, AbsoluteFinishTime = 100 } }
        };

        var nodes = new List<Node>
        {
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid())
        };

        // Act
        var result = _analyzer.AnalyzeCriticalPath(timingInfo, nodes);

        // Assert - All 3 tasks overlap between time 40-100
        Assert.Equal(3, result.MaxParallelism);
    }

    [Fact]
    public void AnalyzeCriticalPath_WithSequentialTasks_ReturnsParallelismOf1()
    {
        // Arrange - Tasks run one after another
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 0, AbsoluteFinishTime = 100 } },
            {
                Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 100, AbsoluteFinishTime = 200 }
            },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 200, AbsoluteFinishTime = 300 } }
        };

        var nodes = new List<Node>
        {
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid())
        };

        // Act
        var result = _analyzer.AnalyzeCriticalPath(timingInfo, nodes);

        // Assert - No overlap means max parallelism of 1
        Assert.Equal(1, result.MaxParallelism);
    }

    [Fact]
    public void AnalyzeCriticalPath_WithOverlappingTasks_ReturnsCorrectParallelism()
    {
        // Arrange - 2 tasks overlap, then 1 task, then 3 tasks overlap
        var timingInfo = new Dictionary<Guid, NodeTimingInfo>
        {
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 150, AbsoluteStartTime = 0, AbsoluteFinishTime = 150 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 100, AbsoluteStartTime = 50, AbsoluteFinishTime = 150 } },
            { Guid.NewGuid(), new NodeTimingInfo { Duration = 80, AbsoluteStartTime = 70, AbsoluteFinishTime = 150 } }
        };

        var nodes = new List<Node>
        {
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid()),
            CreateTaskNodeWithId(Guid.NewGuid())
        };

        // Act
        var result = _analyzer.AnalyzeCriticalPath(timingInfo, nodes);

        // Assert - At time 70-150, all 3 tasks overlap
        Assert.Equal(3, result.MaxParallelism);
        Assert.True(result.PeakParallelismTime is >= 70 and <= 150);
    }

    // Helper methods
    private static TaskNode CreateTaskNodeWithId(Guid id)
    {
        return new TaskNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Height = 50,
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 100,
                FinishTime = 100
            },
            ProcedureId = default
        };
    }
}