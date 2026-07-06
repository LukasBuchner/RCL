using HotChocolate;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Operations;
using FHOOE.Freydis.GraphQLServer.Services.Validation;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.GraphQL;

/// <summary>
///     Layer-4 integration tests that run <see cref="Mutation.CreateDependencyEdgeAsync" />
///     and <see cref="Mutation.UpdateDependencyEdgeAsync" /> with the production wiring
///     of <see cref="DependencyEdgeValidator" />, <see cref="NodeAgentMapper" />, and a
///     real <see cref="DummyRuntimeAgent" />. Confirms that Rule 10 fires on the GraphQL
///     mutation path, not just in isolated validator unit tests, for the canonical
///     scenario of a Dummy agent named "Bob" assigned to a "Move To Position" skill node.
/// </summary>
public class MutationDependencyEdgeAgentCapabilityTests
{
    private readonly Guid _bobId = Guid.NewGuid();
    private readonly Agent _bobDomainAgent;
    private readonly DummyRuntimeAgent _bobRuntimeAgent;
    private readonly Mock<IDependencyEdgeApplicationService> _mockEdgeService = new();
    private readonly Mock<INodeApplicationService> _mockNodeService = new();
    private readonly Mock<IProcedureContext> _mockProcedureContext = new();
    private readonly Mock<IAgentApplicationService> _mockAgentAppService = new();
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider = new();

    private readonly Mutation _mutation;
    private readonly Skill _moveToPositionSkill;
    private readonly Guid _procedureId = Guid.NewGuid();
    private readonly DependencyEdgeValidator _validator;

    /// <summary>
    ///     Wires up Bob as a real Dummy runtime agent, the production
    ///     <see cref="NodeAgentMapper" />, and the production
    ///     <see cref="DependencyEdgeValidator" />. The edge application service is mocked
    ///     so we can assert that it is or is not called depending on whether validation
    ///     accepts the edge.
    /// </summary>
    public MutationDependencyEdgeAgentCapabilityTests()
    {
        _moveToPositionSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move To Position",
            Description = "Moves robot to a target pose",
            Properties = []
        };

        _bobDomainAgent = new Agent
        {
            Id = _bobId,
            Name = "Bob",
            RepresentativeColor = "#000000",
            SkillIds = [_moveToPositionSkill.Id],
            State = AgentState.Active
        };

        _bobRuntimeAgent = new DummyRuntimeAgent(
            _bobId,
            "Bob",
            [_moveToPositionSkill],
            Mock.Of<ILogger<DummyRuntimeAgent>>());

        _mockProcedureContext.Setup(c => c.CurrentProcedureId).Returns(_procedureId);
        _mockProcedureContext.Setup(c => c.RequireCurrentProcedureId()).Returns(_procedureId);
        _mockEdgeService.Setup(s => s.GetAllDependencyEdgesAsync())
            .ReturnsAsync(new List<DependencyEdge>());
        _mockAgentAppService.Setup(s => s.GetAgentByIdAsync(_bobId)).ReturnsAsync(_bobDomainAgent);
        _mockAgentProvider.Setup(p => p.GetRuntimeAgent(_bobId)).Returns(_bobRuntimeAgent);

        var nodeAgentMapper = new NodeAgentMapper(
            _mockAgentProvider.Object,
            NullLogger<NodeAgentMapper>.Instance,
            _mockAgentAppService.Object);

        _validator = new DependencyEdgeValidator(
            _mockNodeService.Object,
            _mockEdgeService.Object,
            _mockProcedureContext.Object,
            nodeAgentMapper);

        _mutation = new Mutation();
    }

    /// <summary>
    ///     Verifies that <see cref="Mutation.CreateDependencyEdgeAsync" /> rejects an FF
    ///     edge into a "Move To Position" skill execution node assigned to Dummy Bob, and
    ///     that <see cref="IDependencyEdgeApplicationService.CreateDependencyEdgeAsync" />
    ///     is never invoked.
    /// </summary>
    [Fact]
    public async Task CreateDependencyEdgeAsync_FfEdgeIntoMoveToPositionAssignedToBob_ThrowsAndDoesNotPersist()
    {
        var source = SetupSkillNode();
        var target = SetupSkillNode();

        var input = new CreateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                TargetId = target.Id,
                SourceHandle = "right",
                TargetHandle = "right"
            }
        };

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _mutation.CreateDependencyEdgeAsync(
                input,
                _mockEdgeService.Object,
                _validator,
                _mockProcedureContext.Object,
                Mock.Of<ILogger<Mutation>>()));

        Assert.Contains("Bob", ex.Message);
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
        _mockEdgeService.Verify(
            s => s.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies that <see cref="Mutation.UpdateDependencyEdgeAsync" /> rejects an FF
    ///     edge update into the same node and does not invoke the update on the edge
    ///     application service.
    /// </summary>
    [Fact]
    public async Task UpdateDependencyEdgeAsync_FfEdgeIntoMoveToPositionAssignedToBob_ThrowsAndDoesNotPersist()
    {
        var source = SetupSkillNode();
        var target = SetupSkillNode();

        var input = new UpdateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                TargetId = target.Id,
                SourceHandle = "right",
                TargetHandle = "right"
            }
        };

        var ex = await Assert.ThrowsAsync<GraphQLException>(() =>
            _mutation.UpdateDependencyEdgeAsync(
                input,
                _mockEdgeService.Object,
                _validator,
                _mockProcedureContext.Object,
                Mock.Of<ILogger<Mutation>>()));

        Assert.Contains("Bob", ex.Message);
        Assert.Contains("adaptive", ex.Message, StringComparison.OrdinalIgnoreCase);
        _mockEdgeService.Verify(
            s => s.UpdateDependencyEdgeAsync(It.IsAny<DependencyEdge>()),
            Times.Never);
    }

    /// <summary>
    ///     Sanity check: an FS edge (target on start) is accepted by the mutation and the
    ///     edge application service is invoked exactly once. This proves Rule 10 is the
    ///     specific gate, not a general block on all edges into Bob's node.
    /// </summary>
    [Fact]
    public async Task CreateDependencyEdgeAsync_FsEdgeIntoMoveToPositionAssignedToBob_PersistsEdge()
    {
        var source = SetupSkillNode();
        var target = SetupSkillNode();

        _mockEdgeService
            .Setup(s => s.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()))
            .ReturnsAsync((DependencyEdge e) => e);

        var input = new CreateDependencyEdgeInput
        {
            DependencyEdge = new DependencyEdgeInput
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                TargetId = target.Id,
                SourceHandle = "right",
                TargetHandle = "left"
            }
        };

        var result = await _mutation.CreateDependencyEdgeAsync(
            input,
            _mockEdgeService.Object,
            _validator,
            _mockProcedureContext.Object,
            Mock.Of<ILogger<Mutation>>());

        Assert.NotNull(result);
        _mockEdgeService.Verify(
            s => s.CreateDependencyEdgeAsync(It.IsAny<DependencyEdge>()),
            Times.Once);
    }

    private SkillExecutionNode SetupSkillNode()
    {
        var node = new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = _procedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move",
                StartTime = 0,
                Duration = 1,
                Skill = _moveToPositionSkill,
                AgentId = _bobId
            }
        };
        _mockNodeService.Setup(s => s.GetNodeByIdAsync(node.Id)).ReturnsAsync(node);
        return node;
    }
}