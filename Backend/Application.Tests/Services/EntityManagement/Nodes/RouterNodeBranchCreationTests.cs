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
///     Tests for automatic branch TaskNode creation when RouterNodes are created.
///     Validates that the system automatically creates TaskNodes for each branch,
///     links them correctly, and maintains proper parent-child relationships.
/// </summary>
public class RouterNodeBranchCreationTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _mockCrudOrchestrator;
    private readonly Mock<ILogger<NodeApplicationService>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IProcedureVariableService> _mockProcedureVariableService;
    private readonly NodeApplicationService _service;
    private readonly Guid _testProcedureId;

    public RouterNodeBranchCreationTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockCrudOrchestrator = new Mock<ICrudSchedulingOrchestrator>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockProcedureVariableService = new Mock<IProcedureVariableService>();
        _mockLogger = new Mock<ILogger<NodeApplicationService>>();

        _testProcedureId = Guid.NewGuid();

        // Setup procedure context to allow operations
        _mockProcedureContext.Setup(p => p.CurrentProcedureId).Returns(_testProcedureId);
        _mockProcedureContext.Setup(p => p.ValidateProcedureOwnership(_testProcedureId));

        // Setup scheduling configuration with default task duration
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
    ///     Test: RouterNode with 2 branches creates 2 TaskNodes automatically.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNodeWithTwoBranches_CreatesTwoTaskNodes()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Quality Check Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "OK",
                        Condition = "quality_result == 'OK'",
                        Priority = 0
                    },
                    new()
                    {
                        Name = "NotOK",
                        Condition = "quality_result == 'NotOK'",
                        Priority = 1
                    }
                }
            }
        };

        var createdTaskNodes = new List<Node>();

        // Setup: orchestrator returns the router node as-is
        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        // Capture TaskNode creations
        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                createdTaskNodes.Add(n);
                return n;
            });

        // Act
        var result = await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RouterNode>(result);

        // Verify 2 TaskNodes were created
        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)),
            Times.Exactly(2),
            "Should create exactly 2 TaskNodes for 2 branches");

        Assert.Equal(2, createdTaskNodes.Count);
    }

    /// <summary>
    ///     Test: Branch.TargetNodeId is set to the created TaskNode's ID.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_LinksBranchesToCreatedTaskNodes()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Decision Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "result" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", Condition = "result == 1", Priority = 0 },
                    new() { Name = "Branch2", Condition = "result == 2", Priority = 1 }
                }
            }
        };

        var createdTaskNodeIds = new List<Guid>();
        RouterNode? updatedRouterNode = null;

        // Setup: orchestrator returns router node
        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        // Capture TaskNode creations and return them with their IDs
        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                createdTaskNodeIds.Add(n.Id);
                return n;
            });

        // Capture the updated RouterNode
        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                updatedRouterNode = n as RouterNode;
                return true;
            });

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(updatedRouterNode);
        Assert.Equal(2, createdTaskNodeIds.Count);

        // Verify that branches now have TargetNodeIds set
        var branches = updatedRouterNode.RouterTask.Branches;
        Assert.All(branches, branch => Assert.NotNull(branch.TargetNodeId));

        // Verify each branch points to one of the created TaskNodes
        Assert.Contains(branches[0].TargetNodeId!.Value, createdTaskNodeIds);
        Assert.Contains(branches[1].TargetNodeId!.Value, createdTaskNodeIds);

        // Verify the router node was updated with the linked branches
        _mockCrudOrchestrator.Verify(
            o => o.UpdateNodeAsync(It.Is<RouterNode>(n =>
                n.Id == routerNodeId &&
                n.RouterTask.Branches.All(b => b.TargetNodeId.HasValue))),
            Times.Once,
            "Router node should be updated with branch links");
    }

    /// <summary>
    ///     Test: Created TaskNode has ParentId equal to RouterNode.Id.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_CreatedTaskNodesHaveCorrectParentId()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Parent Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "state" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "StateA", Priority = 0 }
                }
            }
        };

        TaskNode? capturedTaskNode = null;

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                capturedTaskNode = n as TaskNode;
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(capturedTaskNode);
        Assert.Equal(routerNodeId, capturedTaskNode.ParentId);
    }

    /// <summary>
    ///     Test: Branch TaskNode has correct name "{branch.Name} Branch".
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_CreatedTaskNodesHaveCorrectNames()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var branch1Name = "Success";
        var branch2Name = "Failure";

        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Result Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "status" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = branch1Name, Priority = 0 },
                    new() { Name = branch2Name, Priority = 1 }
                }
            }
        };

        var createdTaskNodes = new List<TaskNode>();

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                createdTaskNodes.Add((TaskNode)n);
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.Equal(2, createdTaskNodes.Count);
        Assert.Contains(createdTaskNodes, n => n.Task.Name == $"{branch1Name} Branch");
        Assert.Contains(createdTaskNodes, n => n.Task.Name == $"{branch2Name} Branch");
    }

    /// <summary>
    ///     Test: RouterNode with single branch creates one TaskNode.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNodeWithOneBranch_CreatesOneTaskNode()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Single Branch Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "always_true" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Default", Priority = 0 }
                }
            }
        };

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) => n);

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)),
            Times.Once,
            "Should create exactly 1 TaskNode for 1 branch");
    }

    /// <summary>
    ///     Test: RouterNode with three branches creates three TaskNodes.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNodeWithThreeBranches_CreatesThreeTaskNodes()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Multi Branch Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "quality_level" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Excellent", Condition = "quality_level > 90", Priority = 0 },
                    new() { Name = "Good", Condition = "quality_level > 70", Priority = 1 },
                    new() { Name = "Poor", Priority = 2 }
                }
            }
        };

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) => n);

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)),
            Times.Exactly(3),
            "Should create exactly 3 TaskNodes for 3 branches");
    }

    /// <summary>
    ///     Test: Created TaskNodes have the same ProcedureId as the RouterNode.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_CreatedTaskNodesHaveSameProcedureId()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "A", Priority = 0 }
                }
            }
        };

        TaskNode? capturedTaskNode = null;

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                capturedTaskNode = n as TaskNode;
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(capturedTaskNode);
        Assert.Equal(_testProcedureId, capturedTaskNode.ProcedureId);
    }

    /// <summary>
    ///     Test: Non-RouterNode creation is not affected (no branch TaskNodes created).
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_TaskNode_DoesNotCreateBranchTaskNodes()
    {
        // Arrange
        var taskNodeId = Guid.NewGuid();
        var taskNode = new TaskNode
        {
            Id = taskNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Regular Task",
                StartTime = 0,
                Duration = 10
            }
        };

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(taskNode))
            .ReturnsAsync(taskNode);

        // Act
        await _service.CreateNodeAsync(taskNode);

        // Assert - Should NOT create any additional TaskNodes
        _mockCrudOrchestrator.Verify(
            o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == taskNodeId)),
            Times.Never,
            "Should not create branch TaskNodes for regular TaskNode");
    }

    /// <summary>
    ///     Test: Created TaskNodes have initial default timing values.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_CreatedTaskNodesHaveDefaultTiming()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Timing Router",
                StartTime = 100,
                Duration = 5,
                Selector = new SimpleVariableSelector { Expression = "val" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch", Priority = 0 }
                }
            }
        };

        TaskNode? capturedTaskNode = null;

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                capturedTaskNode = n as TaskNode;
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(capturedTaskNode);
        Assert.Equal(0, capturedTaskNode.Task.StartTime);
        Assert.Equal(200, capturedTaskNode.Task.Duration); // Default empty task duration
    }

    /// <summary>
    ///     Test: Created TaskNodes have a valid position set.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_CreatedTaskNodesHaveValidPosition()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 100, Y = 200 },
            RouterTask = new RouterTask
            {
                Name = "Position Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "pos" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch", Priority = 0 }
                }
            }
        };

        TaskNode? capturedTaskNode = null;

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                capturedTaskNode = n as TaskNode;
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.NotNull(capturedTaskNode);
        Assert.NotNull(capturedTaskNode.Position);
        Assert.Equal(0, capturedTaskNode.Position.X);
        Assert.Equal(0, capturedTaskNode.Position.Y);
    }

    /// <summary>
    ///     Test: Each branch gets a unique TaskNode with unique ID.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateNodeAsync_RouterNode_EachBranchGetsUniqueTaskNode()
    {
        // Arrange
        var routerNodeId = Guid.NewGuid();
        var routerNode = new RouterNode
        {
            Id = routerNodeId,
            ProcedureId = _testProcedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Unique Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "val" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch1", Priority = 0 },
                    new() { Name = "Branch2", Priority = 1 },
                    new() { Name = "Branch3", Priority = 2 }
                }
            }
        };

        var createdTaskNodes = new List<TaskNode>();

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<RouterNode>(n => n.Id == routerNodeId)))
            .ReturnsAsync(routerNode);

        _mockCrudOrchestrator.Setup(o => o.CreateNodeAsync(It.Is<TaskNode>(n => n.ParentId == routerNodeId)))
            .ReturnsAsync((Node n) =>
            {
                createdTaskNodes.Add((TaskNode)n);
                return n;
            });

        _mockCrudOrchestrator.Setup(o => o.UpdateNodeAsync(It.IsAny<RouterNode>()))
            .ReturnsAsync(true);

        // Act
        await _service.CreateNodeAsync(routerNode);

        // Assert
        Assert.Equal(3, createdTaskNodes.Count);

        // Verify all IDs are unique
        var uniqueIds = createdTaskNodes.Select(n => n.Id).Distinct().ToList();
        Assert.Equal(3, uniqueIds.Count);
    }
}