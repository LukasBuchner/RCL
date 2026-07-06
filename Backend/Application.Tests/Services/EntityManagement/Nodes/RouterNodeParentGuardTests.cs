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
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Nodes;

/// <summary>
///     Tests for the guard that prevents manual child node creation under RouterNodes.
///     Only the internal branch auto-creation path should be allowed to add children to routers.
/// </summary>
public sealed class RouterNodeParentGuardTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _mockCrudOrchestrator;
    private readonly Mock<ILogger<NodeApplicationService>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IProcedureVariableService> _mockProcedureVariableService;
    private readonly NodeApplicationService _service;
    private readonly Guid _testProcedureId;

    public RouterNodeParentGuardTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockCrudOrchestrator = new Mock<ICrudSchedulingOrchestrator>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockProcedureVariableService = new Mock<IProcedureVariableService>();
        _mockLogger = new Mock<ILogger<NodeApplicationService>>();

        _testProcedureId = Guid.NewGuid();

        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns(_testProcedureId);
        _mockProcedureContext.Setup(p => p.ValidateProcedureOwnership(_testProcedureId));

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

    /// <summary>
    ///     Verifies that creating a node whose ParentId points to a RouterNode
    ///     throws an <see cref="InvalidOperationException" />.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_WithRouterNodeParent_ThrowsInvalidOperationException()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var parentRouterNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Parent Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", Priority = 0 }
                }
            }
        };

        _mockProcedureRepository.Setup(r => r.GetNodeByIdAsync(routerNodeId))
            .ReturnsAsync(parentRouterNode);

        var childNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _testProcedureId,
            ParentId = routerNodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Manual Child",
                StartTime = 0,
                Duration = 10
            }
        };

        // Act
        var act = async () => await _service.CreateNodeAsync(childNode);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot manually create a child node under RouterNode*");

        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(It.IsAny<Node>()),
            Times.Never,
            "Should not reach the orchestrator when parent is a RouterNode");
    }

    /// <summary>
    ///     Verifies that creating a node whose ParentId points to a non-router node
    ///     (e.g. a TaskNode) succeeds without throwing.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_WithNonRouterParent_Succeeds()
    {
        // Arrange
        var parentTaskNodeId = Guid.NewGuid();
        var parentTaskNode = new TaskNode
        {
            Id = parentTaskNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Parent Task",
                StartTime = 0,
                Duration = 10
            }
        };

        _mockProcedureRepository.Setup(r => r.GetNodeByIdAsync(parentTaskNodeId))
            .ReturnsAsync(parentTaskNode);

        var childNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _testProcedureId,
            ParentId = parentTaskNodeId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Child Task",
                StartTime = 0,
                Duration = 5
            }
        };

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(childNode))
            .ReturnsAsync(childNode);

        // Act
        var result = await _service.CreateNodeAsync(childNode);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(childNode.Id);

        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(childNode),
            Times.Once,
            "Should reach the orchestrator when parent is not a RouterNode");
    }

    /// <summary>
    ///     Verifies that creating a top-level node (ParentId is null)
    ///     succeeds without any parent validation.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_WithNoParent_Succeeds()
    {
        // Arrange
        var node = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _testProcedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Top-Level Task",
                StartTime = 0,
                Duration = 10
            }
        };

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(node))
            .ReturnsAsync(node);

        // Act
        var result = await _service.CreateNodeAsync(node);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(node.Id);

        _mockProcedureRepository.Verify(
            r => r.GetNodeByIdAsync(It.IsAny<Guid>()),
            Times.Never,
            "Should not look up parent when ParentId is null");
    }
}