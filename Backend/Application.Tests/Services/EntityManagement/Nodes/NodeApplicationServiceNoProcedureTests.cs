using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Nodes;

/// <summary>
///     Tests for NodeApplicationService behavior when no procedure is loaded.
///     These tests verify that the service gracefully handles the no-procedure-loaded state,
///     which is valid at application startup and during transitions.
/// </summary>
public class NodeApplicationServiceNoProcedureTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _mockCrudOrchestrator;
    private readonly Mock<ILogger<NodeApplicationService>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IProcedureVariableService> _mockProcedureVariableService;
    private readonly NodeApplicationService _service;

    public NodeApplicationServiceNoProcedureTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockCrudOrchestrator = new Mock<ICrudSchedulingOrchestrator>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockProcedureVariableService = new Mock<IProcedureVariableService>();
        _mockLogger = new Mock<ILogger<NodeApplicationService>>();

        var schedulingConfig = new SchedulingConfiguration
        {
            Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
        };
        var mockSchedulingOptions = Options.Create(schedulingConfig);

        _service = new NodeApplicationService(
            _mockProcedureRepository.Object,
            _mockCrudOrchestrator.Object,
            _mockNodeChangeTracker.Object,
            _mockProcedureContext.Object,
            _mockProcedureVariableService.Object,
            mockSchedulingOptions,
            _mockLogger.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAllNodesAsync_WhenNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange - Simulate no procedure loaded (valid at startup)
        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns((Guid?)null);

        // Act
        var result = await _service.GetAllNodesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // Verify that change tracker was NOT called since there's no procedure
        _mockNodeChangeTracker.Verify(t => t.GetCurrentNodes(), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAllNodesAsync_WhenProcedureLoaded_ReturnsNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var nodes = new List<Node>
        {
            new TaskNode
            {
                Id = Guid.NewGuid(),
                ProcedureId = procedureId,
                Task = new Task
                {
                    Name = "Test Task",
                    StartTime = 0,
                    Duration = 10
                },
                Position = new NodePosition { X = 0, Y = 0 }
            }
        };

        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns(procedureId);
        _mockNodeChangeTracker.Setup(t => t.GetCurrentNodes()).Returns(nodes);

        // Act
        var result = await _service.GetAllNodesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(nodes[0].Id, result[0].Id);

        // Verify that change tracker was called
        _mockNodeChangeTracker.Verify(t => t.GetCurrentNodes(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNodesByParentIdAsync_WhenNoProcedureLoaded_ReturnsEmptyList()
    {
        // Arrange - Simulate no procedure loaded (valid at startup)
        var parentId = Guid.NewGuid();
        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns((Guid?)null);

        // Act
        var result = await _service.GetNodesByParentIdAsync(parentId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // Verify that change tracker was NOT called since there's no procedure
        _mockNodeChangeTracker.Verify(t => t.GetCurrentNodes(), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNodesByParentIdAsync_WhenProcedureLoaded_ReturnsChildNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            ParentId = parentId,
            Task = new Task
            {
                Name = "Child Task",
                StartTime = 0,
                Duration = 10
            },
            Position = new NodePosition { X = 0, Y = 0 }
        };
        var otherNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            ParentId = Guid.NewGuid(), // Different parent
            Task = new Task
            {
                Name = "Other Task",
                StartTime = 0,
                Duration = 10
            },
            Position = new NodePosition { X = 0, Y = 0 }
        };

        var allNodes = new List<Node> { childNode, otherNode };

        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns(procedureId);
        _mockNodeChangeTracker.Setup(t => t.GetCurrentNodes()).Returns(allNodes);

        // Act
        var result = await _service.GetNodesByParentIdAsync(parentId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(childNode.Id, result[0].Id);

        // Verify that change tracker was called
        _mockNodeChangeTracker.Verify(t => t.GetCurrentNodes(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_WhenNoProcedureLoaded_ThrowsException()
    {
        // Arrange - Creating nodes REQUIRES a procedure to be loaded
        var procedureId = Guid.NewGuid();
        var node = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10
            },
            Position = new NodePosition { X = 0, Y = 0 }
        };

        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(p => p.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateNodeAsync(node));

        // Verify orchestrator was NOT called
        _mockCrudOrchestrator.Verify(o => o.CreateNodeAsync(It.IsAny<Node>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateNodeAsync_WhenNoProcedureLoaded_ThrowsException()
    {
        // Arrange - Updating nodes REQUIRES a procedure to be loaded
        var procedureId = Guid.NewGuid();
        var node = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 10
            },
            Position = new NodePosition { X = 0, Y = 0 }
        };

        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns((Guid?)null);
        _mockProcedureContext.Setup(p => p.ValidateProcedureOwnership(It.IsAny<Guid>()))
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateNodeAsync(node));

        // Verify orchestrator was NOT called
        _mockCrudOrchestrator.Verify(o => o.UpdateNodeAsync(It.IsAny<Node>()), Times.Never);
    }
}