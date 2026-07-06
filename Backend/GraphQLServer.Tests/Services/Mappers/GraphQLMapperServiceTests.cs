using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services.Mappers;

/// <summary>
///     Tests for GraphQLMapperService using IProcedureContext instead of IProcedureOrchestrator.
/// </summary>
public class GraphQlMapperServiceTests
{
    private readonly Mock<IAgentApplicationService> _mockAgentService;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<ISkillApplicationService> _mockSkillService;
    private readonly GraphQlMapperService _sut;

    public GraphQlMapperServiceTests()
    {
        _mockSkillService = new Mock<ISkillApplicationService>();
        _mockAgentService = new Mock<IAgentApplicationService>();
        _mockProcedureContext = new Mock<IProcedureContext>();

        _sut = new GraphQlMapperService(
            _mockSkillService.Object,
            _mockAgentService.Object,
            _mockProcedureContext.Object,
            NullLogger<GraphQlMapperService>.Instance);
    }

    #region MapToRouterNode Tests (DTO version)

    [Fact]
    public async Task MapToRouterNode_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var nodeId = Guid.NewGuid();
        var routerTaskDto = new RouterTaskDto(
            "Router Task",
            "Test Router",
            0.0,
            5.0,
            null,
            null,
            null,
            new SimpleVariableSelector { Expression = "true" },
            new List<ConditionalBranch>(),
            null, // SelectedBranchTargetNodeId
            null, // SelectedBranchName
            null, // SelectedAtUtc
            null); // ManuallySelectedBranch

        var routerNodeDto = new RouterNodeDto(
            nodeId,
            new NodePositionDto(100, 200),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            routerTaskDto);

        var nodeDto = new NodeDto(null, null, routerNodeDto);

        // Act
        var result = await _sut.MapToNodeAsync(nodeDto);

        // Assert
        var routerNode = Assert.IsType<RouterNode>(result);
        Assert.Equal(procedureId, routerNode.ProcedureId);
        Assert.Equal(nodeId, routerNode.Id);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task MapToRouterNode_WithSelectedBranchFields_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var targetNodeId = Guid.NewGuid();
        var selectedTime = DateTime.UtcNow;

        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var routerTaskDto = new RouterTaskDto(
            "Router Task",
            "Test Router",
            0.0,
            5.0,
            5.0,
            true,
            1.0,
            new SimpleVariableSelector { Expression = "true" },
            new List<ConditionalBranch>
            {
                new()
                {
                    Name = "BranchA",
                    Condition = "x > 0",
                    TargetNodeId = targetNodeId
                }
            },
            targetNodeId, // SelectedBranchTargetNodeId
            "BranchA", // SelectedBranchName
            selectedTime, // SelectedAtUtc
            null); // ManuallySelectedBranch

        var routerNodeDto = new RouterNodeDto(
            nodeId,
            new NodePositionDto(100, 200),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            routerTaskDto);

        var nodeDto = new NodeDto(null, null, routerNodeDto);

        // Act
        var result = await _sut.MapToNodeAsync(nodeDto);

        // Assert
        var routerNode = Assert.IsType<RouterNode>(result);
        Assert.Equal(procedureId, routerNode.ProcedureId);
        Assert.Equal(nodeId, routerNode.Id);
        Assert.NotNull(routerNode.RouterTask);
        Assert.Equal(targetNodeId, routerNode.RouterTask.SelectedBranchTargetNodeId);
        Assert.Equal("BranchA", routerNode.RouterTask.SelectedBranchName);
        Assert.Equal(selectedTime, routerNode.RouterTask.SelectedAtUtc);
    }

    [Fact]
    public async Task MapToRouterNode_WithNullSelectedBranchFields_ShouldMapNullFieldsCorrectly()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var routerTaskDto = new RouterTaskDto(
            "Router Task",
            "Test Router",
            0.0,
            5.0,
            null,
            false,
            null,
            new SimpleVariableSelector { Expression = "true" },
            new List<ConditionalBranch>(),
            null, // SelectedBranchTargetNodeId
            null, // SelectedBranchName
            null, // SelectedAtUtc
            null); // ManuallySelectedBranch

        var routerNodeDto = new RouterNodeDto(
            nodeId,
            new NodePositionDto(100, 200),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            routerTaskDto);

        var nodeDto = new NodeDto(null, null, routerNodeDto);

        // Act
        var result = await _sut.MapToNodeAsync(nodeDto);

        // Assert
        var routerNode = Assert.IsType<RouterNode>(result);
        Assert.NotNull(routerNode.RouterTask);
        Assert.Null(routerNode.RouterTask.SelectedBranchTargetNodeId);
        Assert.Null(routerNode.RouterTask.SelectedBranchName);
        Assert.Null(routerNode.RouterTask.SelectedAtUtc);
    }

    #endregion

    #region MapToTaskNode Tests

    [Fact]
    public async Task MapToTaskNode_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var taskNodeInput = new TaskNodeInput
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            TaskInput = new TaskInput
            {
                Name = "Test Task",
                Description = "Test Description",
                Duration = 5.0,
                StartTime = 0.0
            }
        };

        var nodeInput = new NodeInput { TaskNode = taskNodeInput };

        // Act
        var result = await _sut.MapToNodeAsync(nodeInput);

        // Assert
        var taskNode = Assert.IsType<TaskNode>(result);
        Assert.Equal(procedureId, taskNode.ProcedureId);
        Assert.Equal(taskNodeInput.Id, taskNode.Id);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public void MapToTaskNode_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var taskNodeInput = new TaskNodeInput
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            TaskInput = new TaskInput
            {
                Name = "Test Task",
                Duration = 5.0,
                StartTime = 0.0
            }
        };

        var nodeInput = new NodeInput { TaskNode = taskNodeInput };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _sut.MapToNodeAsync(nodeInput).GetAwaiter().GetResult());

        Assert.Contains("No procedure is currently loaded", ex.Message);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    #endregion

    #region MapToSkillExecutionNode Tests

    [Fact]
    public async Task MapToSkillExecutionNode_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var agent = new Agent { Id = agentId, Name = "Test Agent", RepresentativeColor = "#FF0000" };
        var skill = new Skill
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test Description",
            Properties = new List<TypedProperty>()
        };

        _mockAgentService.Setup(x => x.GetAgentByIdAsync(agentId)).ReturnsAsync(agent);
        _mockSkillService.Setup(x => x.GetSkillByIdAsync(skillId)).ReturnsAsync(skill);

        var skillExecutionNodeInput = new SkillExecutionNodeInput
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            SkillExecutionTask = new SkillExecutionTaskInput
            {
                AgentId = agentId,
                Name = "Execute Skill",
                Duration = 10.0,
                StartTime = 0.0,
                Skill = new SkillInput
                {
                    Id = skillId,
                    Name = "Test Skill",
                    Description = null,
                    Properties = null
                }
            }
        };

        var nodeInput = new NodeInput { SkillExecutionNode = skillExecutionNodeInput };

        // Act
        var result = await _sut.MapToNodeAsync(nodeInput);

        // Assert
        var skillNode = Assert.IsType<SkillExecutionNode>(result);
        Assert.Equal(procedureId, skillNode.ProcedureId);
        Assert.Equal(skillExecutionNodeInput.Id, skillNode.Id);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task MapToSkillExecutionNode_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var skillExecutionNodeInput = new SkillExecutionNodeInput
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 200 },
            SkillExecutionTask = new SkillExecutionTaskInput
            {
                AgentId = Guid.NewGuid(),
                Name = "Execute Skill",
                Duration = 10.0,
                StartTime = 0.0,
                Skill = new SkillInput
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = null,
                    Properties = null
                }
            }
        };

        var nodeInput = new NodeInput { SkillExecutionNode = skillExecutionNodeInput };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.MapToNodeAsync(nodeInput));

        Assert.Contains("No procedure is currently loaded", ex.Message);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    #endregion

    #region MapToDependencyEdge Tests

    [Fact]
    public async Task MapToDependencyEdge_WithValidProcedureContext_ShouldSetProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId()).Returns(procedureId);

        var edgeId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var dependencyEdgeDto = new DependencyEdgeDto(
            edgeId,
            sourceId,
            targetId,
            "output",
            "input");

        // Act
        var result = await _sut.MapToDependencyEdgeAsync(dependencyEdgeDto);

        // Assert
        Assert.Equal(procedureId, result.ProcedureId);
        Assert.Equal(edgeId, result.Id);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    [Fact]
    public async Task MapToDependencyEdge_WithNoProcedureLoaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockProcedureContext.Setup(x => x.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        var dependencyEdgeDto = new DependencyEdgeDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.MapToDependencyEdgeAsync(dependencyEdgeDto));

        Assert.Contains("No procedure is currently loaded", ex.Message);
        _mockProcedureContext.Verify(x => x.RequireCurrentProcedureId(), Times.Once);
    }

    #endregion
}