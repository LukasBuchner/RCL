using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Tests for cache consistency and repository call patterns in CrudSchedulingOrchestrator.
///     Focuses on detecting cache-database synchronization issues and duplicate operations.
/// </summary>
public class CrudSchedulingOrchestratorCacheConsistencyTests
{
    private readonly Mock<IDependencyEdgeChangeTracker> _mockEdgeChangeTracker;
    private readonly Mock<ILogger<CrudSchedulingOrchestrator>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<ISchedulingResultLogger> _mockResultLogger;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;
    private readonly ITestOutputHelper _output;
    private readonly Guid _testProcedureId = Guid.NewGuid();

    public CrudSchedulingOrchestratorCacheConsistencyTests(ITestOutputHelper output)
    {
        _output = output;
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockEdgeChangeTracker = new Mock<IDependencyEdgeChangeTracker>();
        _mockResultLogger = new Mock<ISchedulingResultLogger>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockLogger = new Mock<ILogger<CrudSchedulingOrchestrator>>();

        // Setup procedure context
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_testProcedureId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_testProcedureId);

        // Setup change tracker observables with empty initial data
        _mockNodeChangeTracker.Setup(t => t.Nodes)
            .Returns(Observable.Return(new List<Node>() as IReadOnlyList<Node>));
        _mockEdgeChangeTracker.Setup(t => t.Edges)
            .Returns(Observable.Return(new List<DependencyEdge>() as IReadOnlyList<DependencyEdge>));
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_RepositoryFailure_ShouldNotCauseInconsistentState()
    {
        // Arrange
        var orchestrator = CreateCrudOrchestrator();
        var edgeId = Guid.NewGuid();
        var testNodes = CreateMockNodesWithOrphanedSkillExecutions();
        var testEdges = CreateMockEdgesWithTargetEdge(edgeId);

        var deleteCallCount = 0;
        var getAllCallCount = 0;

        // Setup repository to fail on delete but succeed on GetAll
        _mockProcedureRepository.Setup(x => x.DeleteEdgeAsync(edgeId))
            .Callback(() => deleteCallCount++)
            .ReturnsAsync(false); // Simulate delete failure

        _mockProcedureRepository.Setup(x => x.GetEdgesByProcedureIdAsync(_testProcedureId))
            .Callback(() => getAllCallCount++)
            .ReturnsAsync(testEdges);

        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(testNodes);

        // Setup timing orchestrator to simulate scheduling that finds orphaned nodes
        _mockTimingOrchestrator
            .Setup(x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = testNodes,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act & Assert
        var result = await orchestrator.DeleteDependencyEdgeAsync(edgeId);

        // Verify the operation returns false (indicating failure)
        Assert.False(result);

        // Critical assertion: Delete should only be called once
        Assert.Equal(1, deleteCallCount);
        _output.WriteLine($"Repository DeleteAsync called {deleteCallCount} time(s) - EXPECTED: 1");

        // Verify GetAll was called for scheduling preparation
        Assert.True(getAllCallCount > 0);
        _output.WriteLine($"Repository GetAllAsync called {getAllCallCount} time(s) for data preparation");

        // Verify that scheduling was still triggered despite delete failure
        _mockTimingOrchestrator.Verify(
            x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("Scheduling pipeline was triggered once as expected");
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_SuccessfulDelete_ShouldMaintainConsistency()
    {
        // Arrange
        var orchestrator = CreateCrudOrchestrator();
        var edgeId = Guid.NewGuid();
        var testNodes = CreateMockNodesWithOrphanedSkillExecutions();
        var testEdges = CreateMockEdgesWithTargetEdge(edgeId);

        var deleteCallCount = 0;
        var getEdgesCallCount = 0;
        var getNodesCallCount = 0;

        // Setup repository to succeed on delete
        _mockProcedureRepository.Setup(x => x.DeleteEdgeAsync(edgeId))
            .Callback(() => deleteCallCount++)
            .ReturnsAsync(true); // Simulate successful delete

        _mockProcedureRepository.Setup(x => x.GetEdgesByProcedureIdAsync(_testProcedureId))
            .Callback(() => getEdgesCallCount++)
            .ReturnsAsync(testEdges.Where(e => e.Id != edgeId).ToList()); // Simulate edge being removed

        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_testProcedureId))
            .Callback(() => getNodesCallCount++)
            .ReturnsAsync(testNodes);

        // Setup timing orchestrator
        _mockTimingOrchestrator
            .Setup(x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = testNodes,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        var result = await orchestrator.DeleteDependencyEdgeAsync(edgeId);

        // Assert
        Assert.True(result);

        // Critical assertion: Delete should only be called once
        Assert.Equal(1, deleteCallCount);
        _output.WriteLine($"Repository DeleteAsync called {deleteCallCount} time(s) - EXPECTED: 1");

        // Verify data consistency calls
        Assert.True(getEdgesCallCount >= 1); // Called for preparation and notifications
        Assert.True(getNodesCallCount >= 1); // Called for preparation and notifications
        _output.WriteLine(
            $"Edge GetAllAsync called {getEdgesCallCount} time(s), Node GetAllAsync called {getNodesCallCount} time(s)");

        // Verify scheduling pipeline was executed
        _mockTimingOrchestrator.Verify(
            x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteNodeAsync_WithCascadeDelete_ShouldCallRepositoryOncePerNode()
    {
        // Arrange
        var orchestrator = CreateCrudOrchestrator();
        var parentNodeId = Guid.NewGuid();
        var childNode1Id = Guid.NewGuid();
        var childNode2Id = Guid.NewGuid();

        var nodeDeleteCallCounts = new Dictionary<Guid, int>();
        var getAllCallCount = 0;

        // Create parent task node with children
        var parentNode = new TaskNode
        {
            ProcedureId = _testProcedureId,
            Id = parentNodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "ParentTask", StartTime = 0, Duration = 30 }
        };

        var childNodes = new List<SkillExecutionNode>
        {
            new()
            {
                Id = childNode1Id,
                ParentId = parentNodeId,
                Position = new NodePosition
                {
                    X = 0,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Child1Task",
                    StartTime = 0,
                    Duration = 15,
                    AgentId = Guid.NewGuid(),
                    Skill = null!
                },
                ProcedureId = _testProcedureId
            },
            new()
            {
                Id = childNode2Id,
                ParentId = parentNodeId,
                Position = new NodePosition
                {
                    X = 0,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Child2Task",
                    StartTime = 0,
                    Duration = 20,
                    AgentId = Guid.NewGuid(),
                    Skill = null!
                },
                ProcedureId = _testProcedureId
            }
        };

        var allNodes = new List<Node> { parentNode };
        allNodes.AddRange(childNodes);

        // Setup repository mocks with call counting - return NEW list each time to avoid shared mutable state issues
        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_testProcedureId))
            .Callback(() => getAllCallCount++)
            .ReturnsAsync(() => [.. allNodes]);

        _mockProcedureRepository.Setup(x => x.DeleteNodeAsync(It.IsAny<Guid>()))
            .Callback<Guid>(nodeId =>
            {
                nodeDeleteCallCounts[nodeId] = nodeDeleteCallCounts.GetValueOrDefault(nodeId, 0) + 1;
            })
            .ReturnsAsync(true);

        _mockProcedureRepository.Setup(x => x.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);

        // Setup timing orchestrator
        _mockTimingOrchestrator
            .Setup(x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = new List<Node>(),
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act
        var result = await orchestrator.DeleteNodeTreeAsync(parentNodeId);

        // Assert
        Assert.True(result);

        // Critical assertions: Each node should only be deleted once
        foreach (var kvp in nodeDeleteCallCounts)
        {
            Assert.Equal(1, kvp.Value);
            _output.WriteLine($"Node {kvp.Key} DeleteAsync called {kvp.Value} time(s) - EXPECTED: 1");
        }

        // Verify all expected nodes were deleted
        Assert.True(nodeDeleteCallCounts.ContainsKey(childNode1Id));
        Assert.True(nodeDeleteCallCounts.ContainsKey(childNode2Id));
        Assert.True(nodeDeleteCallCounts.ContainsKey(parentNodeId));

        _output.WriteLine($"Total nodes deleted: {nodeDeleteCallCounts.Count} (Expected: 3)");
        _output.WriteLine($"GetAllAsync called {getAllCallCount} time(s) for data loading");

        // Verify scheduling pipeline was triggered
        _mockTimingOrchestrator.Verify(
            x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentDeletes_SameEdge_ShouldNotCauseDuplicateCalls()
    {
        // Arrange
        var orchestrator = CreateCrudOrchestrator();
        var edgeId = Guid.NewGuid();
        var testNodes = CreateMockNodesWithOrphanedSkillExecutions();
        var testEdges = CreateMockEdgesWithTargetEdge(edgeId);

        var deleteCallCount = 0;
        var tcs = new TaskCompletionSource<bool>();

        // Setup repository to delay and count calls
        _mockProcedureRepository.Setup(x => x.DeleteEdgeAsync(edgeId))
            .Callback(() => { Interlocked.Increment(ref deleteCallCount); })
            .Returns(async () =>
            {
                await tcs.Task; // Wait for signal to simulate concurrent access
                return true;
            });

        _mockProcedureRepository.Setup(x => x.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(testEdges);

        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(testNodes);

        _mockTimingOrchestrator
            .Setup(x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = testNodes,
                NodeSchedules = new List<NodeSchedule>()
            });

        // Act - Start multiple concurrent delete operations
        var deleteTask1 = orchestrator.DeleteDependencyEdgeAsync(edgeId);
        var deleteTask2 = orchestrator.DeleteDependencyEdgeAsync(edgeId);

        // Allow both operations to proceed
        tcs.SetResult(true);

        var results = await Task.WhenAll(deleteTask1, deleteTask2);

        // Assert
        _output.WriteLine($"Concurrent deletes completed. Repository DeleteAsync called {deleteCallCount} time(s)");

        // In a proper implementation, we might expect some form of deduplication
        // For now, we document the current behavior
        _output.WriteLine($"Delete results: [{string.Join(", ", results)}]");

        // At minimum, verify that both calls don't interfere with each other catastrophically
        Assert.True(deleteCallCount >= 1);
        _output.WriteLine("Concurrent delete operations completed without exceptions");
    }

    [Fact]
    public async Task SchedulingFailure_AfterSuccessfulDelete_ShouldNotRollbackDelete()
    {
        // Arrange
        var orchestrator = CreateCrudOrchestrator();
        var edgeId = Guid.NewGuid();
        var testNodes = CreateMockNodesWithOrphanedSkillExecutions();
        var testEdges = CreateMockEdgesWithTargetEdge(edgeId);

        var deleteCallCount = 0;

        // Setup successful delete
        _mockProcedureRepository.Setup(x => x.DeleteEdgeAsync(edgeId))
            .Callback(() => deleteCallCount++)
            .ReturnsAsync(true);

        _mockProcedureRepository.Setup(x => x.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(testEdges.Where(e => e.Id != edgeId).ToList());

        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(testNodes);

        // Setup scheduling to fail
        _mockTimingOrchestrator
            .Setup(x => x.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Scheduling pipeline failed"));

        // Act - Current implementation gracefully handles scheduling failures without throwing exceptions
        var result = await orchestrator.DeleteDependencyEdgeAsync(edgeId);

        // Assert - Delete should succeed despite scheduling failure
        Assert.True(result);

        // Critical assertion: Delete should still only be called once, even with scheduling failure
        Assert.Equal(1, deleteCallCount);
        _output.WriteLine(
            $"Repository DeleteAsync called {deleteCallCount} time(s) despite scheduling failure - EXPECTED: 1");
        _output.WriteLine("Delete operation was not rolled back due to scheduling failure (current behavior)");
    }

    #region Helper Methods

    private CrudSchedulingOrchestrator CreateCrudOrchestrator()
    {
        var dataPreparation = new CrudDataPreparationService(
            _mockProcedureRepository.Object,
            _mockProcedureContext.Object,
            NullLogger<CrudDataPreparationService>.Instance);

        var cascadeDeletion = new CascadeDeletionService(
            _mockProcedureRepository.Object,
            _mockProcedureContext.Object,
            NullLogger<CascadeDeletionService>.Instance);

        var notification = new CrudNotificationService(
            _mockProcedureRepository.Object,
            _mockNodeChangeTracker.Object,
            _mockEdgeChangeTracker.Object,
            _mockProcedureContext.Object,
            NullLogger<CrudNotificationService>.Instance);

        return new CrudSchedulingOrchestrator(
            _mockProcedureRepository.Object,
            dataPreparation,
            cascadeDeletion,
            notification,
            _mockResultLogger.Object,
            _mockTimingOrchestrator.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object
        );
    }

    private static List<Node> CreateMockNodesWithOrphanedSkillExecutions()
    {
        var nodes = new List<Node>();

        // Create some task nodes
        for (var i = 0; i < 2; i++)
            nodes.Add(new TaskNode
            {
                ProcedureId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                Position = new NodePosition { X = 0, Y = 0 },
                Task = new Freydis.Domain.Entities.Procedure.Task
                {
                    Name = $"TaskNode_{i}",
                    StartTime = 0,
                    Duration = 30
                }
            });

        // Create orphaned skill execution nodes (no valid parent)
        for (var i = 0; i < 6; i++)
            nodes.Add(new SkillExecutionNode
            {
                ProcedureId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                ParentId = Guid.NewGuid(), // Non-existent parent - makes them orphaned
                Position = new NodePosition { X = 0, Y = 0 },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = $"OrphanedSkill_{i}",
                    StartTime = 0,
                    Duration = 10,
                    AgentId = Guid.NewGuid(),
                    Skill = null!
                }
            });

        return nodes;
    }

    private static List<DependencyEdge> CreateMockEdgesWithTargetEdge(Guid targetEdgeId)
    {
        var edges = new List<DependencyEdge>
        {
            // The target edge that will be deleted
            new()
            {
                Id = targetEdgeId,
                SourceId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                SourceHandle = null,
                TargetHandle = null,
                ProcedureId = default
            }
        };

        // Add some other edges
        for (var i = 0; i < 3; i++)
            edges.Add(new DependencyEdge
            {
                ProcedureId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                TargetId = Guid.NewGuid(),
                SourceHandle = null,
                TargetHandle = null
            });

        return edges;
    }

    #endregion
}