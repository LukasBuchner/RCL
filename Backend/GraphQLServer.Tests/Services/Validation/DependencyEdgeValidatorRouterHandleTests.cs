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
///     Tests for Rule 8: Router nodes may only be targeted on the start (left) handle.
///     FF/SF edges targeting a router would create false SCCs in the dependency graph
///     because routers have no externally-controllable finish behavior.
/// </summary>
public class DependencyEdgeValidatorRouterHandleTests
{
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeAgentMapper> _mockNodeAgentMapper = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();

    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _sut;

    public DependencyEdgeValidatorRouterHandleTests()
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
    ///     Targeting a router's finish (right) handle is rejected — FF/SF edges to routers
    ///     would create false SCCs.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_EdgeToRouterFinishHandle_ThrowsGraphQLException()
    {
        var sourceId = Guid.NewGuid();
        var routerId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupRouterNode(routerId);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, routerId, "right", "right"));
        Assert.Contains("finish handle of router node", ex.Message);
    }

    /// <summary>
    ///     Targeting a router's start (left) handle is allowed — this is how FS/SS edges
    ///     correctly feed into routers.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_EdgeToRouterStartHandle_Succeeds()
    {
        var sourceId = Guid.NewGuid();
        var routerId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupRouterNode(routerId);

        await _sut.ValidateAsync(sourceId, routerId, "right", "left");
    }

    /// <summary>
    ///     A null target handle defaults to finish (right) in the handle mapping.
    ///     When targeting a router, this must be rejected.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_EdgeToRouterNullHandle_ThrowsGraphQLException()
    {
        var sourceId = Guid.NewGuid();
        var routerId = Guid.NewGuid();

        SetupTaskNode(sourceId);
        SetupRouterNode(routerId);

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _sut.ValidateAsync(sourceId, routerId, "right", null));
        Assert.Contains("finish handle of router node", ex.Message);
    }

    /// <summary>
    ///     Routers as edge sources (any handle) are allowed — routers publish Start/Finish
    ///     events that downstream nodes can depend on.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_EdgeFromRouter_AnyHandle_Succeeds()
    {
        var routerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        SetupRouterNode(routerId);
        SetupTaskNode(targetId);

        // Router as source with right (finish) handle → valid
        await _sut.ValidateAsync(routerId, targetId, "right", "left");
    }

    #region Helper Methods

    private void SetupTaskNode(Guid nodeId)
    {
        var node = new TaskNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Domain.Entities.Procedure.Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 1
            }
        };
        _mockNodeService.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(node);
    }

    private void SetupRouterNode(Guid nodeId)
    {
        var node = new RouterNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
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
    }

    #endregion
}