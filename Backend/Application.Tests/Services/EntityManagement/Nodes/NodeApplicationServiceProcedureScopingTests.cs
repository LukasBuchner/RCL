using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Nodes;

/// <summary>
///     Tests for procedure scoping and validation in NodeApplicationService.
///     Ensures that all operations properly validate procedure ownership and filter results.
/// </summary>
public sealed class NodeApplicationServiceProcedureScopingTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _mockCrudOrchestrator;
    private readonly Mock<ILogger<NodeApplicationService>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IProcedureVariableService> _mockProcedureVariableService;
    private readonly Guid _nodeId1 = Guid.NewGuid();
    private readonly Guid _nodeId2 = Guid.NewGuid();
    private readonly Guid _nodeId3 = Guid.NewGuid();
    private readonly BehaviorSubject<IReadOnlyList<Node>> _nodesSubject;
    private readonly BehaviorSubject<Guid?> _procedureChangesSubject;

    private readonly Guid _procedureId1 = Guid.NewGuid();
    private readonly Guid _procedureId2 = Guid.NewGuid();

    public NodeApplicationServiceProcedureScopingTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockCrudOrchestrator = new Mock<ICrudSchedulingOrchestrator>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockProcedureVariableService = new Mock<IProcedureVariableService>();
        _mockLogger = new Mock<ILogger<NodeApplicationService>>();

        _nodesSubject = new BehaviorSubject<IReadOnlyList<Node>>(Array.Empty<Node>());
        _mockNodeChangeTracker.Setup(x => x.Nodes).Returns(_nodesSubject);

        _procedureChangesSubject = new BehaviorSubject<Guid?>(_procedureId1);
        _mockProcedureContext.Setup(x => x.ProcedureChanges).Returns(_procedureChangesSubject);
    }

    private static TaskNode CreateTestTaskNode(Guid id, Guid procedureId, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = id,
            ProcedureId = procedureId,
            ParentId = parentId,
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "Test", StartTime = 0, Duration = 10 },
            Position = new NodePosition { X = 0, Y = 0 }
        };
    }

    private NodeApplicationService CreateService()
    {
        var schedulingConfig = new SchedulingConfiguration
        {
            Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
        };
        var mockSchedulingOptions = Options.Create(schedulingConfig);

        return new NodeApplicationService(
            _mockProcedureRepository.Object,
            _mockCrudOrchestrator.Object,
            _mockNodeChangeTracker.Object,
            _mockProcedureContext.Object,
            _mockProcedureVariableService.Object,
            mockSchedulingOptions,
            _mockLogger.Object);
    }

    #region CreateNodeAsync Tests

    [Fact]
    public async Task CreateNodeAsync_WithNoProcedureLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        // Act
        var act = async () => await service.CreateNodeAsync(node);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");
    }

    [Fact]
    public async Task CreateNodeAsync_WithDifferentProcedure_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(_procedureId2))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureId2}' does not belong to the current procedure '{_procedureId1}'."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId2); // Different procedure

        // Act
        var act = async () => await service.CreateNodeAsync(node);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _mockCrudOrchestrator.Verify(x => x.CreateNodeAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task CreateNodeAsync_WithCorrectProcedure_Succeeds()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockCrudOrchestrator.Setup(x => x.CreateNodeAsync(node)).ReturnsAsync(node);

        // Act
        var result = await service.CreateNodeAsync(node);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_nodeId1);
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(_procedureId1), Times.Once);
        _mockCrudOrchestrator.Verify(x => x.CreateNodeAsync(node), Times.Once);
    }

    #endregion

    #region UpdateNodeAsync Tests

    [Fact]
    public async Task UpdateNodeAsync_WithNoProcedureLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        // Act
        var act = async () => await service.UpdateNodeAsync(node);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");
    }

    [Fact]
    public async Task UpdateNodeAsync_WithDifferentProcedure_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(_procedureId2))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureId2}' does not belong to the current procedure '{_procedureId1}'."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId2); // Different procedure

        // Act
        var act = async () => await service.UpdateNodeAsync(node);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _mockCrudOrchestrator.Verify(x => x.UpdateNodeAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async Task UpdateNodeAsync_WithCorrectProcedure_Succeeds()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockCrudOrchestrator.Setup(x => x.UpdateNodeAsync(node)).ReturnsAsync(true);

        // Act
        var result = await service.UpdateNodeAsync(node);

        // Assert
        result.Should().BeTrue();
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(_procedureId1), Times.Once);
        _mockCrudOrchestrator.Verify(x => x.UpdateNodeAsync(node), Times.Once);
    }

    #endregion

    #region GetAllNodesAsync Tests

    [Fact]
    public async Task GetAllNodesAsync_WithNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _procedureChangesSubject.OnNext(null);
        var service = CreateService();

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllNodesAsync_ReturnsOnlyCurrentProcedureNodes()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(_procedureId1);
        var service = CreateService();

        var node1 = CreateTestTaskNode(_nodeId1, _procedureId1);
        var node2 = CreateTestTaskNode(_nodeId2, _procedureId1);

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes())
            .Returns(new List<Node> { node1, node2 });

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.ProcedureId == _procedureId1);
        _mockNodeChangeTracker.Verify(x => x.GetCurrentNodes(), Times.Once);
    }

    [Fact]
    public async Task GetAllNodesAsync_WithMultipleProcedures_FiltersCorrectly()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId2);
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(_procedureId2);
        var service = CreateService();

        var node1 = CreateTestTaskNode(_nodeId1, _procedureId1);
        var node3 = CreateTestTaskNode(_nodeId3, _procedureId2);

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes())
            .Returns(new List<Node> { node1, node3 });

        // Act
        var result = await service.GetAllNodesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].ProcedureId.Should().Be(_procedureId2);
        _mockNodeChangeTracker.Verify(x => x.GetCurrentNodes(), Times.Once);
    }

    #endregion

    #region GetNodeByIdAsync Tests

    [Fact]
    public async Task GetNodeByIdAsync_WithNoProcedureLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes()).Returns(new List<Node> { node });

        // Act
        var act = async () => await service.GetNodeByIdAsync(_nodeId1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");
    }

    [Fact]
    public async Task GetNodeByIdAsync_ValidatesOwnership_ThrowsForWrongProcedure()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(_procedureId2))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureId2}' does not belong to the current procedure '{_procedureId1}'."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId2); // Different procedure

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes()).Returns(new List<Node> { node });

        // Act
        var act = async () => await service.GetNodeByIdAsync(_nodeId1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");
    }

    [Fact]
    public async Task GetNodeByIdAsync_WithCorrectProcedure_ReturnsNode()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes()).Returns(new List<Node> { node });

        // Act
        var result = await service.GetNodeByIdAsync(_nodeId1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_nodeId1);
        result.ProcedureId.Should().Be(_procedureId1);
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(_procedureId1), Times.Once);
    }

    [Fact]
    public async Task GetNodeByIdAsync_WhenNodeNotFound_ReturnsNull()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes()).Returns(new List<Node>());

        // Act
        var result = await service.GetNodeByIdAsync(_nodeId1);

        // Assert
        result.Should().BeNull();
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region GetNodesByParentIdAsync Tests

    [Fact]
    public async Task GetNodesByParentIdAsync_WithNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _procedureChangesSubject.OnNext(null);
        var service = CreateService();
        var parentId = Guid.NewGuid();

        // Act
        var result = await service.GetNodesByParentIdAsync(parentId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNodesByParentIdAsync_FiltersToCurrentProcedure()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(_procedureId1);
        var service = CreateService();
        var parentId = Guid.NewGuid();

        var child1 = CreateTestTaskNode(_nodeId1, _procedureId1, parentId);
        var child2 = CreateTestTaskNode(_nodeId2, _procedureId1, parentId);

        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes())
            .Returns(new List<Node> { child1, child2 });

        // Act
        var result = await service.GetNodesByParentIdAsync(parentId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.ParentId == parentId);
        result.Should().OnlyContain(n => n.ProcedureId == _procedureId1);
    }

    #endregion

    #region Nodes Observable Tests

    [Fact]
    public void Nodes_Observable_FiltersToCurrentProcedure()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        var node1 = CreateTestTaskNode(_nodeId1, _procedureId1);
        var node2 = CreateTestTaskNode(_nodeId2, _procedureId2);
        var node3 = CreateTestTaskNode(_nodeId3, _procedureId1);

        IReadOnlyList<Node>? receivedNodes = null;
        service.Nodes.Subscribe(nodes => receivedNodes = nodes);

        // Act
        _nodesSubject.OnNext(new List<Node> { node1, node2, node3 });

        // Assert
        receivedNodes.Should().NotBeNull();
        receivedNodes!.Should().HaveCount(2);
        receivedNodes.Should().OnlyContain(n => n.ProcedureId == _procedureId1);
    }

    [Fact]
    public void Nodes_Observable_ReturnsEmptyWhenNoProcedureLoaded()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _procedureChangesSubject.OnNext(null);
        var service = CreateService();

        var node1 = CreateTestTaskNode(_nodeId1, _procedureId1);
        var node2 = CreateTestTaskNode(_nodeId2, _procedureId2);

        IReadOnlyList<Node>? receivedNodes = null;
        service.Nodes.Subscribe(nodes => receivedNodes = nodes);

        // Act
        _nodesSubject.OnNext(new List<Node> { node1, node2 });

        // Assert
        receivedNodes.Should().NotBeNull();
        receivedNodes!.Should().BeEmpty();
    }

    [Fact]
    public void Nodes_Observable_UpdatesWhenProcedureChanges()
    {
        // Arrange
        var service = CreateService();

        var node1 = CreateTestTaskNode(_nodeId1, _procedureId1);
        var node2 = CreateTestTaskNode(_nodeId2, _procedureId2);

        var receivedNodeCounts = new List<int>();
        service.Nodes.Subscribe(nodes => receivedNodeCounts.Add(nodes.Count));

        // Set initial nodes
        _nodesSubject.OnNext(new List<Node> { node1, node2 });
        receivedNodeCounts.Clear(); // Clear initial emissions

        // Act - Switch to procedure 2
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId2);
        _procedureChangesSubject.OnNext(_procedureId2);

        // Assert
        receivedNodeCounts.Should().HaveCount(1);
        receivedNodeCounts[0].Should().Be(1); // Only node2 from procedure2
    }

    #endregion

    #region DeleteNodeAsync Tests

    [Fact]
    public async Task DeleteNodeAsync_WithNoProcedureLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(_nodeId1)).ReturnsAsync(node);

        // Act
        var act = async () => await service.DeleteNodeAsync(_nodeId1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no procedure is currently loaded*");
    }

    [Fact]
    public async Task DeleteNodeAsync_WithDifferentProcedure_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        _mockProcedureContext.Setup(x => x.ValidateProcedureOwnership(_procedureId2))
            .Throws(new InvalidOperationException(
                $"Entity with procedure ID '{_procedureId2}' does not belong to the current procedure '{_procedureId1}'."));
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId2);

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(_nodeId1)).ReturnsAsync(node);

        // Act
        var act = async () => await service.DeleteNodeAsync(_nodeId1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to the current procedure*");

        _mockCrudOrchestrator.Verify(x => x.DeleteNodeTreeAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteNodeAsync_WithCorrectProcedure_Succeeds()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();
        var node = CreateTestTaskNode(_nodeId1, _procedureId1);

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(_nodeId1)).ReturnsAsync(node);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(_nodeId1)).ReturnsAsync(true);

        // Act
        var result = await service.DeleteNodeAsync(_nodeId1);

        // Assert
        result.Should().BeTrue();
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(_procedureId1), Times.Once);
        _mockCrudOrchestrator.Verify(x => x.DeleteNodeTreeAsync(_nodeId1), Times.Once);
    }

    [Fact]
    public async Task DeleteNodeAsync_WhenNodeNotFound_ReturnsFalse()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(_nodeId1)).ReturnsAsync((Node?)null);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(_nodeId1)).ReturnsAsync(false);

        // Act
        var result = await service.DeleteNodeAsync(_nodeId1);

        // Assert
        result.Should().BeFalse();
        _mockProcedureContext.Verify(x => x.ValidateProcedureOwnership(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region UpdateNodeAsync RouterNode Branch TargetNodeId Preservation Tests

    [Fact]
    public async Task UpdateNodeAsync_RouterNodeWithMissingTargetNodeIds_PreservesExistingTargetNodeIds()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        var routerId = Guid.NewGuid();
        var branch1TargetId = Guid.NewGuid();
        var branch2TargetId = Guid.NewGuid();

        // Existing RouterNode with TargetNodeIds set
        var existingRouter = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId1,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = branch1TargetId },
                    new() { Name = "BranchB", TargetNodeId = branch2TargetId }
                },
                ManuallySelectedBranch = "BranchA"
            }
        };

        // Update with ManuallySelectedBranch changed but TargetNodeIds missing
        var updatedRouter = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId1,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = null }, // TargetNodeId missing
                    new() { Name = "BranchB", TargetNodeId = null } // TargetNodeId missing
                },
                ManuallySelectedBranch = "BranchB" // Changed selection
            }
        };

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(routerId)).ReturnsAsync(existingRouter);
        _mockCrudOrchestrator.Setup(x => x.UpdateNodeAsync(It.IsAny<Node>())).ReturnsAsync(true);

        // Act
        var result = await service.UpdateNodeAsync(updatedRouter);

        // Assert
        result.Should().BeTrue();

        // Verify the orchestrator was called with preserved TargetNodeIds
        _mockCrudOrchestrator.Verify(x => x.UpdateNodeAsync(It.Is<Node>(n =>
            n.GetType() == typeof(RouterNode) &&
            ((RouterNode)n).RouterTask.Branches.Any(b => b.Name == "BranchA" && b.TargetNodeId == branch1TargetId) &&
            ((RouterNode)n).RouterTask.Branches.Any(b => b.Name == "BranchB" && b.TargetNodeId == branch2TargetId) &&
            ((RouterNode)n).RouterTask.ManuallySelectedBranch == "BranchB"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateNodeAsync_RouterNodeWithExistingTargetNodeIds_KeepsProvidedTargetNodeIds()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        var routerId = Guid.NewGuid();
        var oldTargetId = Guid.NewGuid();
        var newTargetId = Guid.NewGuid();

        // Existing RouterNode
        var existingRouter = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId1,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = oldTargetId }
                }
            }
        };

        // Update with new TargetNodeId explicitly set
        var updatedRouter = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId1,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = newTargetId } // New TargetNodeId
                }
            }
        };

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(routerId)).ReturnsAsync(existingRouter);
        _mockCrudOrchestrator.Setup(x => x.UpdateNodeAsync(It.IsAny<Node>())).ReturnsAsync(true);

        // Act
        var result = await service.UpdateNodeAsync(updatedRouter);

        // Assert
        result.Should().BeTrue();

        // Verify the orchestrator was called with the new TargetNodeId (not the old one)
        _mockCrudOrchestrator.Verify(x => x.UpdateNodeAsync(It.Is<Node>(n =>
            n.GetType() == typeof(RouterNode) &&
            ((RouterNode)n).RouterTask.Branches.Any(b => b.Name == "BranchA" && b.TargetNodeId == newTargetId)
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateNodeAsync_NonRouterNode_PassesThroughWithoutModification()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId1);
        var service = CreateService();

        var taskNode = new TaskNode
        {
            Id = _nodeId1,
            ProcedureId = _procedureId1,
            Position = new NodePosition { X = 10, Y = 20 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10
            }
        };

        _mockCrudOrchestrator.Setup(x => x.UpdateNodeAsync(It.IsAny<Node>())).ReturnsAsync(true);

        // Act
        var result = await service.UpdateNodeAsync(taskNode);

        // Assert
        result.Should().BeTrue();

        // Repository should not be called for non-router nodes (no TargetNodeId preservation needed)
        _mockProcedureRepository.Verify(x => x.GetNodeByIdAsync(It.IsAny<Guid>()), Times.Never);

        // Orchestrator should be called with the original node
        _mockCrudOrchestrator.Verify(x => x.UpdateNodeAsync(taskNode), Times.Once);
    }

    #endregion
}