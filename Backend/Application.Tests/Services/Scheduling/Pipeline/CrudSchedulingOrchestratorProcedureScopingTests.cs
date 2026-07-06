using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     TDD tests for CrudSchedulingOrchestrator procedure scoping functionality.
///     Validates that scheduling operations only operate on entities from the currently loaded procedure.
/// </summary>
public class CrudSchedulingOrchestratorProcedureScopingTests
{
    private readonly Mock<IDependencyEdgeChangeTracker> _mockEdgeChangeTracker;
    private readonly Mock<ILogger<CrudSchedulingOrchestrator>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<ISchedulingResultLogger> _mockResultLogger;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;

    // Test procedure IDs
    private readonly Guid _procedureAId = Guid.NewGuid();
    private readonly Guid _procedureBId = Guid.NewGuid();

    public CrudSchedulingOrchestratorProcedureScopingTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockEdgeChangeTracker = new Mock<IDependencyEdgeChangeTracker>();
        _mockResultLogger = new Mock<ISchedulingResultLogger>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockLogger = new Mock<ILogger<CrudSchedulingOrchestrator>>();

        // Setup default empty observables
        _mockNodeChangeTracker.Setup(t => t.Nodes)
            .Returns(Observable.Empty<IReadOnlyList<Node>>());
        _mockEdgeChangeTracker.Setup(t => t.Edges)
            .Returns(Observable.Empty<IReadOnlyList<DependencyEdge>>());

        // Setup change tracker update methods
        _mockNodeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<Node>>()));
        _mockEdgeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<DependencyEdge>>()));
    }

    [Fact]
    public async Task Schedule_WithLoadedProcedure_LoadsOnlyThatProceduresEntities()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var nodeInProcedureA = CreateTaskNode("NodeA", _procedureAId);
        var edgeInProcedureA = CreateDependencyEdge(Guid.NewGuid(), Guid.NewGuid(), _procedureAId);

        // Setup repository to return procedure-specific entities
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([nodeInProcedureA]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([edgeInProcedureA]);

        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.IsAny<Node>()))
            .ReturnsAsync(nodeInProcedureA);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act - Create a node which triggers scheduling
        await orchestrator.CreateNodeAsync(CreateTaskNode("NewNode", _procedureAId));

        // Assert - Verify GetByProcedureIdAsync was called, NOT GetAllAsync
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);

        // Verify GetAllAsync was NOT called
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
        _mockProcedureRepository.Verify(r => r.GetAllEdgesAsync(), Times.Never);
    }

    [Fact]
    public async Task Schedule_WithMultipleProcedures_DoesNotMixEntities()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var nodeInProcedureA = CreateTaskNode("NodeA", _procedureAId);
        var nodeInProcedureB = CreateTaskNode("NodeB", _procedureBId);

        // Setup repository to return only Procedure A entities when queried
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([nodeInProcedureA]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        // If GetAllAsync is called (bug scenario), it would return both procedures' nodes
        _mockProcedureRepository.Setup(r => r.GetAllNodesAsync())
            .ReturnsAsync([nodeInProcedureA, nodeInProcedureB]);

        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.IsAny<Node>()))
            .ReturnsAsync(nodeInProcedureA);

        SchedulingRequest? capturedRequest = null;
        _mockTimingOrchestrator.Setup(t => t.CalculateAsync(
                It.IsAny<SchedulingRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = new List<Node>(),
                NodeSchedules = new List<NodeSchedule>()
            });

        var orchestrator = CreateOrchestrator();

        // Act - Create a node in Procedure A
        await orchestrator.CreateNodeAsync(CreateTaskNode("NewNode", _procedureAId));

        // Assert - Scheduling request should have Procedure A's ID
        Assert.NotNull(capturedRequest);
        Assert.Equal(_procedureAId, capturedRequest.ProcedureId);

        // Verify only Procedure A entities were included
        Assert.DoesNotContain(capturedRequest.Nodes, n => ((TaskNode)n).ProcedureId == _procedureBId);
    }

    [Fact]
    public async Task Schedule_WithoutLoadedProcedure_ThrowsException()
    {
        // Arrange - No procedure loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns((Guid?)null);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Throws<InvalidOperationException>();

        var orchestrator = CreateOrchestrator();

        // Act & Assert - Should throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await orchestrator.CreateNodeAsync(CreateTaskNode("NewNode", _procedureAId)));
    }

    [Fact]
    public async Task UpdateNodeAsync_LoadsEntitiesFromCorrectProcedure()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var existingNode = CreateTaskNode("ExistingNode", _procedureAId);
        var updatedNode = CreateTaskNode("UpdatedNode", _procedureAId);
        updatedNode.Id = existingNode.Id;

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([existingNode]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        _mockProcedureRepository.Setup(r => r.UpdateNodeAsync(It.IsAny<Node>()))
            .ReturnsAsync(true);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.UpdateNodeAsync(updatedNode);

        // Assert - Verify procedure-scoped loading
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteNodeAsync_LoadsEntitiesFromCorrectProcedure()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var nodeToDelete = CreateTaskNode("NodeToDelete", _procedureAId);

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([nodeToDelete]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        _mockProcedureRepository.Setup(r => r.DeleteNodeAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.DeleteNodeAsync(nodeToDelete.Id);

        // Assert - Verify procedure-scoped loading
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateDependencyEdgeAsync_LoadsEntitiesFromCorrectProcedure()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var edge = CreateDependencyEdge(Guid.NewGuid(), Guid.NewGuid(), _procedureAId);

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        _mockProcedureRepository.Setup(r => r.CreateEdgeAsync(It.IsAny<DependencyEdge>()))
            .ReturnsAsync(edge);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.CreateDependencyEdgeAsync(edge);

        // Assert - Verify procedure-scoped loading
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetAllEdgesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteNodeTreeAsync_LoadsEntitiesFromCorrectProcedure()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var parentNode = CreateTaskNode("ParentNode", _procedureAId);
        var childNode = CreateSkillExecutionNode("ChildNode", parentNode.Id, _procedureAId);

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([parentNode, childNode]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        _mockProcedureRepository.Setup(r => r.DeleteNodeAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.DeleteNodeTreeAsync(parentNode.Id);

        // Assert - Verify procedure-scoped loading
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce);
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    [Fact]
    public async Task SchedulingRequest_ContainsProcedureId()
    {
        // Arrange - Procedure A is loaded
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        var node = CreateTaskNode("TestNode", _procedureAId);

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.IsAny<Node>()))
            .ReturnsAsync(node);

        SchedulingRequest? capturedRequest = null;
        _mockTimingOrchestrator.Setup(t => t.CalculateAsync(
                It.IsAny<SchedulingRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = new List<Node>(),
                NodeSchedules = new List<NodeSchedule>()
            });

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.CreateNodeAsync(node);

        // Assert - Verify ProcedureId is set in scheduling request
        Assert.NotNull(capturedRequest);
        Assert.Equal(_procedureAId, capturedRequest.ProcedureId);
        Assert.NotEqual(Guid.Empty, capturedRequest.ProcedureId);
    }

    [Fact]
    public async Task NotifyNodesChanged_WhenSwitchingProcedures_UpdatesWithCurrentProcedureNodes()
    {
        // This test verifies that when switching procedures, the cache is updated with only
        // the current procedure's nodes. This ensures proper procedure scoping throughout
        // the application - only the currently loaded procedure's entities reach the front end.

        // Arrange - Start with Procedure A loaded
        var currentProcedureId = _procedureAId;
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(() => currentProcedureId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(() => currentProcedureId);

        var nodeA = CreateTaskNode("NodeA", _procedureAId);
        var nodeB = CreateTaskNode("NodeB", _procedureBId);

        // Track all nodes that have been sent to the change tracker
        var trackedNodeUpdates = new List<IReadOnlyList<Node>>();
        _mockNodeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<Node>>()))
            .Callback<IReadOnlyList<Node>>(nodes => trackedNodeUpdates.Add(nodes));

        // Setup repository responses for each procedure
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([nodeA]);
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureBId))
            .ReturnsAsync([nodeB]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync([]);

        // Setup node creation to return the created node
        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.Is<Node>(n => n.ProcedureId == _procedureAId)))
            .ReturnsAsync((Node n) => n);
        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.Is<Node>(n => n.ProcedureId == _procedureBId)))
            .ReturnsAsync((Node n) => n);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act - Create node in Procedure A
        await orchestrator.CreateNodeAsync(nodeA);

        // Verify NodeA was added to the tracker when Procedure A was active
        Assert.Contains(trackedNodeUpdates, update => update.Any(n => n.Id == nodeA.Id));

        // Switch to Procedure B
        currentProcedureId = _procedureBId;

        // Create node in Procedure B
        await orchestrator.CreateNodeAsync(nodeB);

        // Assert - The last update to the change tracker should contain only Procedure B's nodes
        // This ensures proper procedure scoping - only current procedure's entities are sent to front end
        var lastUpdate = trackedNodeUpdates.Last();

        Assert.True(lastUpdate.Any(n => n.Id == nodeB.Id),
            "NodeB from Procedure B should be in the update");
        Assert.True(lastUpdate.All(n => n.ProcedureId == _procedureBId),
            "Update should only contain nodes from the current procedure (Procedure B)");

        // Verify GetAllAsync was never called - only GetByProcedureIdAsync should be used
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
    }

    [Fact]
    public async Task CrudOperation_RefreshesCacheWithCurrentProcedureEntities()
    {
        // This test ensures that CRUD operations refresh the cache with entities
        // scoped to the current procedure. The orchestrator uses GetByProcedureIdAsync
        // for proper procedure scoping - only the current procedure's entities reach the front end.

        // Arrange
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_procedureAId);

        // Setup nodes and edges that exist in the database for the current procedure
        var existingNode1 = CreateTaskNode("ExistingNode1", _procedureAId);
        var existingNode2 = CreateTaskNode("ExistingNode2", _procedureAId);
        var newNode = CreateTaskNode("NewNode", _procedureAId);
        var existingEdge = CreateDependencyEdge(existingNode1.Id, existingNode2.Id, _procedureAId);

        // Track what gets sent to change trackers
        var nodeUpdates = new List<IReadOnlyList<Node>>();
        var edgeUpdates = new List<IReadOnlyList<DependencyEdge>>();

        _mockNodeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<Node>>()))
            .Callback<IReadOnlyList<Node>>(nodes => nodeUpdates.Add(nodes));
        _mockEdgeChangeTracker.Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Callback<IReadOnlyList<DependencyEdge>>(edges => edgeUpdates.Add(edges));

        // Setup repository to return procedure-scoped entities
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([existingNode1, existingNode2, newNode]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([existingEdge]);
        _mockProcedureRepository.Setup(r => r.CreateNodeAsync(It.IsAny<Node>()))
            .ReturnsAsync((Node n) => n);

        SetupSuccessfulScheduling();

        var orchestrator = CreateOrchestrator();

        // Act - Create a node, which triggers cache refresh
        await orchestrator.CreateNodeAsync(newNode);

        // Assert - Verify that the cache was refreshed with procedure-scoped data
        Assert.NotEmpty(nodeUpdates);
        Assert.NotEmpty(edgeUpdates);

        var lastNodeUpdate = nodeUpdates.Last();
        var lastEdgeUpdate = edgeUpdates.Last();

        // Verify all nodes from the current procedure are in the update
        Assert.Contains(lastNodeUpdate, n => n.Id == existingNode1.Id);
        Assert.Contains(lastNodeUpdate, n => n.Id == existingNode2.Id);
        Assert.Contains(lastNodeUpdate, n => n.Id == newNode.Id);
        Assert.Contains(lastEdgeUpdate, e => e.Id == existingEdge.Id);

        // Verify all entities belong to the current procedure
        Assert.True(lastNodeUpdate.All(n => n.ProcedureId == _procedureAId),
            "All nodes should belong to the current procedure");
        Assert.True(lastEdgeUpdate.All(e => e.ProcedureId == _procedureAId),
            "All edges should belong to the current procedure");

        // Verify that GetByProcedureIdAsync was called (not GetAllAsync)
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.AtLeastOnce,
            "GetByProcedureIdAsync should be called to load procedure-scoped entities");
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never,
            "GetAllAsync should not be called - procedure scoping is required");
    }

    #region Helper Methods

    private CrudSchedulingOrchestrator CreateOrchestrator()
    {
        // Use real extracted services for procedure scoping integration tests
        var dataPreparation = new CrudDataPreparationService(
            _mockProcedureRepository.Object,
            _mockProcedureContext.Object,
            new Mock<ILogger<CrudDataPreparationService>>().Object);

        var cascadeDeletion = new CascadeDeletionService(
            _mockProcedureRepository.Object,
            _mockProcedureContext.Object,
            new Mock<ILogger<CascadeDeletionService>>().Object);

        var notification = new CrudNotificationService(
            _mockProcedureRepository.Object,
            _mockNodeChangeTracker.Object,
            _mockEdgeChangeTracker.Object,
            _mockProcedureContext.Object,
            new Mock<ILogger<CrudNotificationService>>().Object);

        return new CrudSchedulingOrchestrator(
            _mockProcedureRepository.Object,
            dataPreparation,
            cascadeDeletion,
            notification,
            _mockResultLogger.Object,
            _mockTimingOrchestrator.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object);
    }

    private void SetupSuccessfulScheduling()
    {
        _mockTimingOrchestrator.Setup(t => t.CalculateAsync(
                It.IsAny<SchedulingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = new List<Node>(),
                NodeSchedules = new List<NodeSchedule>()
            });
    }

    private static TaskNode CreateTaskNode(string name, Guid procedureId)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 100.0,
                FinishTime = 100.0,
                IsExecuting = false,
                Progress = 0.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string name, Guid parentId, Guid procedureId)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = 50.0,
                FinishTime = 50.0,
                IsExecuting = false,
                Progress = 0.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Test skill: {name}",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static DependencyEdge CreateDependencyEdge(Guid sourceId, Guid targetId, Guid procedureId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            ProcedureId = procedureId,
            SourceHandle = "bottom",
            TargetHandle = "top"
        };
    }

    #endregion
}