using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Validation;

/// <summary>
///     Unit tests for <see cref="DependencyEdgeValidator"/> covering all structural validation rules
///     for dependency edges. Each rule is tested independently using mocked application services.
/// </summary>
public class DependencyEdgeValidatorTests
{
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeAgentMapper> _mockNodeAgentMapper = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();

    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _sut;

    public DependencyEdgeValidatorTests()
    {
        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId);
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>());

        _sut = new DependencyEdgeValidator(
            _mockNodeService.Object,
            _mockEdgeService.Object,
            _mockProcedureContext.Object,
            _mockNodeAgentMapper.Object);
    }

    /// <summary>
    ///     Creates a top-level <see cref="TaskNode"/> with no parent, registered in the mock node service.
    /// </summary>
    /// <param name="nodeId">The ID to assign to the node.</param>
    /// <param name="procedureId">The procedure ID to assign to the node.</param>
    /// <param name="parentId">Optional parent ID for the node.</param>
    /// <returns>The created task node.</returns>
    private TaskNode SetupTaskNode(Guid nodeId, Guid? procedureId = null, Guid? parentId = null)
    {
        var node = new TaskNode
        {
            Id = nodeId,
            ProcedureId = procedureId ?? _procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Domain.Entities.Procedure.Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 1
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
        return node;
    }

    /// <summary>
    ///     Creates a <see cref="RouterNode"/> registered in the mock node service.
    /// </summary>
    /// <param name="nodeId">The ID to assign to the router node.</param>
    /// <param name="procedureId">The procedure ID to assign to the node.</param>
    /// <returns>The created router node.</returns>
    private RouterNode SetupRouterNode(Guid nodeId, Guid? procedureId = null)
    {
        var node = new RouterNode
        {
            Id = nodeId,
            ProcedureId = procedureId ?? _procedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 1,
                Selector = new SimpleVariableSelector { Expression = "test" },
                Branches = new List<ConditionalBranch>()
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
        return node;
    }

    #region Rule 1: Self-loop prevention

    [Fact]
    public async Task ValidateAsync_SelfLoop_ThrowsGraphQLException()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(nodeId, nodeId, null, null));
        Assert.Contains("Cannot create a dependency edge from a node to itself", ex.Message);
    }

    #endregion

    #region Rule 5: Same parent (hierarchy level)

    [Fact]
    public async Task ValidateAsync_DifferentParentIds_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var parentA = Guid.NewGuid();
        var parentB = Guid.NewGuid();

        // Create non-router parents so rule 3 does not trigger.
        SetupTaskNode(parentA);
        SetupTaskNode(parentB);
        SetupTaskNode(sourceId, parentId: parentA);
        SetupTaskNode(targetId, parentId: parentB);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("different hierarchical levels", ex.Message);
    }

    #endregion

    #region Rule 6: Duplicate edge prevention

    [Fact]
    public async Task ValidateAsync_DuplicateEdgeExists_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupTaskNode(targetId);

        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = _procedureId,
                    SourceId = sourceId,
                    TargetId = targetId
                }
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("already exists", ex.Message);
    }

    #endregion

    #region Rule 7: Finish-to-Start cycle detection

    [Fact]
    public async Task ValidateAsync_FinishToStartCycle_ThrowsGraphQLException()
    {
        // Arrange: A → B exists, trying to add B → A would create a cycle.
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();

        SetupTaskNode(nodeA);
        SetupTaskNode(nodeB);

        // Existing FS edge: A → B (sourceHandle=right/finish, targetHandle=left/start)
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = _procedureId,
                    SourceId = nodeA,
                    TargetId = nodeB,
                    SourceHandle = "right",
                    TargetHandle = "left"
                }
            });

        // Act & Assert: Adding B → A as FS should detect the cycle.
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(nodeB, nodeA, "right", "left"));
        Assert.Contains("cycle", ex.Message);
    }

    #endregion

    #region Rule 2: Node existence

    [Fact]
    public async Task ValidateAsync_SourceNotFound_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(sourceId)).ReturnsAsync((Node?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains($"Source node '{sourceId}' was not found", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_TargetNotFound_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        SetupTaskNode(sourceId);
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(targetId)).ReturnsAsync((Node?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains($"Target node '{targetId}' was not found", ex.Message);
    }

    #endregion

    #region Rule 3: Router branch isolation

    [Fact]
    public async Task ValidateAsync_SourceIsRouterBranchChild_ThrowsGraphQLException()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        SetupRouterNode(routerId);
        SetupTaskNode(sourceId, parentId: routerId);
        SetupTaskNode(targetId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("branch child of router node", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_TargetIsRouterBranchChild_ThrowsGraphQLException()
    {
        // Arrange
        var routerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        SetupRouterNode(routerId);
        SetupTaskNode(sourceId);
        SetupTaskNode(targetId, parentId: routerId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("branch child of router node", ex.Message);
    }

    #endregion

    #region Rule 4: Cross-procedure

    [Fact]
    public async Task ValidateAsync_SourceCrossProcedure_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var otherProcedureId = Guid.NewGuid();

        SetupTaskNode(sourceId, otherProcedureId);
        SetupTaskNode(targetId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("not the currently loaded procedure", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_TargetCrossProcedure_ThrowsGraphQLException()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var otherProcedureId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupTaskNode(targetId, otherProcedureId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, targetId, "right", "left"));
        Assert.Contains("not the currently loaded procedure", ex.Message);
    }

    #endregion

    #region Happy paths

    [Fact]
    public async Task ValidateAsync_ValidEdge_DoesNotThrow()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupTaskNode(targetId);

        // Act & Assert — no exception means all rules pass.
        await _sut.ValidateAsync(sourceId, targetId, "right", "left");
    }

    [Fact]
    public async Task ValidateAsync_UpdateExcludesOwnEdge_DoesNotThrow()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var edgeId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupTaskNode(targetId);

        // The existing edge is the one being updated — should be excluded from duplicate check.
        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>
            {
                new()
                {
                    Id = edgeId,
                    ProcedureId = _procedureId,
                    SourceId = sourceId,
                    TargetId = targetId
                }
            });

        // Act & Assert — should pass because the edge being updated is excluded.
        await _sut.ValidateAsync(sourceId, targetId, "right", "left", edgeId);
    }

    [Fact]
    public async Task ValidateAsync_SfPlusFsCrossEventCycle_ThrowsGraphQLException()
    {
        // Arrange: A -FS-> B exists. Adding B -SF-> A creates a cross-event cycle:
        // (A,Finish)→(B,Start) [from SF] and (B,Start)→(A,Finish) [from FS]
        // → (A,Finish)→(B,Start)→(A,Finish). Deadlock.
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();

        SetupTaskNode(nodeA);
        SetupTaskNode(nodeB);

        _mockEdgeService.Setup(x => x.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = _procedureId,
                    SourceId = nodeA,
                    TargetId = nodeB,
                    SourceHandle = "right",
                    TargetHandle = "left"
                }
            });

        // Act & Assert: B -SF-> A targets A's finish handle. But A is a TaskNode →
        // Rule 9 rejects before cycle check. Use "left" target to test cycle detection.
        // Actually, FS+SF cross-event cycle is tested in EventLevelCycleTests with SkillNodes.
        // Here we verify that the old "non-FS edges skip cycle check" behavior is gone —
        // a non-FS edge that would create a cycle (SS mutual) is now correctly rejected.
        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(nodeB, nodeA, "left", "left")); // B -SS-> A
        Assert.Contains("circular dependency", ex.Message);
    }

    #endregion
}