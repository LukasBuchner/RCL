using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ConditionalBranch = FHOOE.Freydis.Domain.Entities.Procedure.ConditionalBranch;
using NodePosition = FHOOE.Freydis.Domain.Entities.Procedure.NodePosition;
using ProcedureEntity = FHOOE.Freydis.Domain.Entities.Procedure.Procedure;
using RouterNode = FHOOE.Freydis.Domain.Entities.Procedure.RouterNode;
using RouterTask = FHOOE.Freydis.Domain.Entities.Procedure.RouterTask;
using SimpleVariableSelector = FHOOE.Freydis.Domain.Entities.Procedure.SimpleVariableSelector;
using SkillExecutionNode = FHOOE.Freydis.Domain.Entities.Procedure.SkillExecutionNode;
using SkillExecutionTask = FHOOE.Freydis.Domain.Entities.Procedure.SkillExecutionTask;
using Task = System.Threading.Tasks.Task;
using TaskNode = FHOOE.Freydis.Domain.Entities.Procedure.TaskNode;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Nodes;

/// <summary>
///     Tests verifying that deleting a node also removes the procedure variables
///     that were auto-created for skill output properties — both for directly
///     deleted SkillExecutionNodes and for SkillExecutionNodes nested inside
///     a parent node hierarchy (TaskNode, RouterNode).
/// </summary>
public sealed class NodeDeletionVariableCleanupTests
{
    private readonly Mock<ICrudSchedulingOrchestrator> _mockCrudOrchestrator;
    private readonly Mock<ILogger<NodeApplicationService>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IProcedureVariableService> _mockProcedureVariableService;
    private readonly Guid _procedureId = Guid.NewGuid();

    public NodeDeletionVariableCleanupTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockCrudOrchestrator = new Mock<ICrudSchedulingOrchestrator>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockProcedureVariableService = new Mock<IProcedureVariableService>();
        _mockLogger = new Mock<ILogger<NodeApplicationService>>();

        _mockProcedureContext.Setup(x => x.CurrentProcedureId).Returns(_procedureId);
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

    /// <summary>
    ///     Creates a stub <see cref="Procedure" /> for mock return values.
    /// </summary>
    private ProcedureEntity CreateStubProcedure()
    {
        return new ProcedureEntity
        {
            Id = _procedureId,
            Name = "Test",
            RootNodeIds = [],
            Variables = new List<VariableDefinition>().AsReadOnly()
        };
    }

    /// <summary>
    ///     Configures the <see cref="IProcedureVariableService" /> mock to accept
    ///     any <see cref="IProcedureVariableService.RemoveVariableAsync" /> call.
    /// </summary>
    private void SetupRemoveVariableAcceptsAll()
    {
        _mockProcedureVariableService
            .Setup(x => x.RemoveVariableAsync(_procedureId, It.IsAny<string>()))
            .ReturnsAsync(CreateStubProcedure());
    }

    /// <summary>
    ///     Helper that creates a <see cref="Freydis.Domain.Entities.Procedure.SkillExecutionNode" /> whose skill
    ///     has the given output typedProperty names.
    /// </summary>
    /// <param name="nodeId">The unique identifier for the node.</param>
    /// <param name="procedureId">The procedure this node belongs to.</param>
    /// <param name="skillName">Display name for the skill.</param>
    /// <param name="parentId">Optional parent node ID for nesting inside a hierarchy.</param>
    /// <param name="outputPropertyNames">Names of output properties that produce variables.</param>
    /// <returns>A fully constructed <see cref="Freydis.Domain.Entities.Procedure.SkillExecutionNode" />.</returns>
    private static SkillExecutionNode CreateSkillExecutionNodeWithOutputs(
        Guid nodeId,
        Guid procedureId,
        string skillName,
        Guid? parentId = null,
        params string[] outputPropertyNames)
    {
        var properties = outputPropertyNames.Select(name => new TypedProperty
        {
            Name = name,
            Value = TypedValue.Boolean(false),
            Direction = PropertyDirection.Output
        }).ToList();

        return new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = procedureId,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                StartTime = 0,
                Duration = 100,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = $"Test skill {skillName}",
                    Properties = properties
                }
            }
        };
    }

    /// <summary>
    ///     Sets up <see cref="INodeChangeTracker.GetCurrentNodes" /> and
    ///     <see cref="IProcedureRepository.GetNodesByProcedureIdAsync" /> to return the given nodes,
    ///     so the service can discover descendants when cleaning up variables.
    /// </summary>
    private void SetupNodeHierarchy(params Node[] allNodes)
    {
        var nodeList = allNodes.ToList();
        _mockNodeChangeTracker.Setup(x => x.GetCurrentNodes()).Returns(nodeList);
        _mockProcedureRepository.Setup(x => x.GetNodesByProcedureIdAsync(_procedureId)).ReturnsAsync(nodeList);
    }

    #region Direct SkillExecutionNode deletion

    /// <summary>
    ///     Deleting a SkillExecutionNode that has output properties should remove
    ///     the corresponding procedure variables.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_SkillNodeWithOutputVariables_RemovesVariables()
    {
        // Arrange
        var service = CreateService();
        var nodeId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNodeWithOutputs(
            nodeId, _procedureId, "GripperSkill", null, "GripForce", "IsGripped");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(skillNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(nodeId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(skillNode);

        // Act
        var result = await service.DeleteNodeAsync(nodeId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "GripForce"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "IsGripped"), Times.Once);
    }

    /// <summary>
    ///     Deleting a non-skill node that has no children should not attempt
    ///     any variable removal.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_LeafTaskNode_DoesNotAttemptVariableRemoval()
    {
        // Arrange
        var service = CreateService();
        var nodeId = Guid.NewGuid();
        var taskNode = new TaskNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Plain Task",
                StartTime = 0,
                Duration = 100
            }
        };

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(taskNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(nodeId)).ReturnsAsync(true);
        SetupNodeHierarchy(taskNode);

        // Act
        var result = await service.DeleteNodeAsync(nodeId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    ///     A SkillExecutionNode whose skill has only input properties should not
    ///     trigger any variable removal on deletion.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_SkillNodeWithOnlyInputProperties_DoesNotRemoveVariables()
    {
        // Arrange
        var service = CreateService();
        var nodeId = Guid.NewGuid();

        var inputOnlyNode = new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "MoveSkill",
                StartTime = 0,
                Duration = 100,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "MoveSkill",
                    Description = "Movement skill",
                    Properties =
                    [
                        new TypedProperty
                        {
                            Name = "TargetPosition",
                            Value = TypedValue.Boolean(false),
                            Direction = PropertyDirection.Input
                        }
                    ]
                }
            }
        };

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(inputOnlyNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(nodeId)).ReturnsAsync(true);
        SetupNodeHierarchy(inputOnlyNode);

        // Act
        var result = await service.DeleteNodeAsync(nodeId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    ///     InputOutput properties also produce variables at creation time, so they
    ///     must be cleaned up on deletion. Input-only properties must not be touched.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_SkillNodeWithMixedDirections_RemovesOnlyOutputAndInputOutputVariables()
    {
        // Arrange
        var service = CreateService();
        var nodeId = Guid.NewGuid();

        var mixedProperties = new List<TypedProperty>
        {
            new()
            {
                Name = "Speed",
                Value = TypedValue.Number(1.0),
                Direction = PropertyDirection.Input
            },
            new()
            {
                Name = "ActualSpeed",
                Value = TypedValue.Number(0.0),
                Direction = PropertyDirection.InputOutput
            },
            new()
            {
                Name = "DistanceTravelled",
                Value = TypedValue.Number(0.0),
                Direction = PropertyDirection.Output
            }
        };

        var skillNode = new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "NavigateSkill",
                StartTime = 0,
                Duration = 100,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "NavigateSkill",
                    Description = "Navigation skill",
                    Properties = mixedProperties
                }
            }
        };

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(skillNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(nodeId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(skillNode);

        // Act
        var result = await service.DeleteNodeAsync(nodeId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "ActualSpeed"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "DistanceTravelled"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "Speed"), Times.Never);
    }

    /// <summary>
    ///     If a variable was already manually removed by the user before the node is
    ///     deleted, the cleanup should not fail — best-effort, not blocking.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_SkillNodeWithAlreadyRemovedVariable_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        var nodeId = Guid.NewGuid();
        var skillNode = CreateSkillExecutionNodeWithOutputs(
            nodeId, _procedureId, "GripperSkill", null, "GripForce");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(nodeId)).ReturnsAsync(skillNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(nodeId)).ReturnsAsync(true);
        SetupNodeHierarchy(skillNode);

        // Simulate that the variable was already manually removed by the user
        _mockProcedureVariableService
            .Setup(x => x.RemoveVariableAsync(_procedureId, "GripForce"))
            .ThrowsAsync(new InvalidOperationException("Variable with name 'GripForce' not found"));

        // Act — should NOT throw even though the variable is gone
        var act = async () => await service.DeleteNodeAsync(nodeId);

        // Assert
        await act.Should().NotThrowAsync(
            "Variable cleanup should be best-effort and not prevent node deletion");
    }

    #endregion

    #region Hierarchical deletion — SkillExecutionNodes nested inside parents

    /// <summary>
    ///     Deleting a TaskNode that contains a SkillExecutionNode child should
    ///     also remove the child's output variables.
    ///     Hierarchy: TaskNode → SkillExecutionNode
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_TaskNodeContainingSkillChild_RemovesChildSkillVariables()
    {
        // Arrange
        var service = CreateService();
        var parentTaskId = Guid.NewGuid();
        var childSkillId = Guid.NewGuid();

        var parentTask = new TaskNode
        {
            Id = parentTaskId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Parent Task",
                StartTime = 0,
                Duration = 200
            }
        };

        var childSkill = CreateSkillExecutionNodeWithOutputs(
            childSkillId, _procedureId, "GripperSkill", parentTaskId, "GripForce", "IsGripped");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(parentTaskId)).ReturnsAsync(parentTask);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(parentTaskId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(parentTask, childSkill);

        // Act
        var result = await service.DeleteNodeAsync(parentTaskId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "GripForce"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "IsGripped"), Times.Once);
    }

    /// <summary>
    ///     Deleting a TaskNode that contains multiple SkillExecutionNode children
    ///     should remove all of their output variables.
    ///     Hierarchy: TaskNode → [SkillExecA, SkillExecB]
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_TaskNodeContainingMultipleSkillChildren_RemovesAllVariables()
    {
        // Arrange
        var service = CreateService();
        var parentTaskId = Guid.NewGuid();
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();

        var parentTask = new TaskNode
        {
            Id = parentTaskId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Parent Task",
                StartTime = 0,
                Duration = 300
            }
        };

        var skillA = CreateSkillExecutionNodeWithOutputs(
            skillAId, _procedureId, "GripperSkill", parentTaskId, "GripForce");
        var skillB = CreateSkillExecutionNodeWithOutputs(
            skillBId, _procedureId, "SensorSkill", parentTaskId, "Temperature", "Pressure");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(parentTaskId)).ReturnsAsync(parentTask);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(parentTaskId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(parentTask, skillA, skillB);

        // Act
        var result = await service.DeleteNodeAsync(parentTaskId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "GripForce"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "Temperature"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "Pressure"), Times.Once);
    }

    /// <summary>
    ///     Deleting a RouterNode should clean up variables from SkillExecutionNodes
    ///     nested inside its branch TaskNodes.
    ///     Hierarchy: RouterNode → BranchTaskNode → SkillExecutionNode
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_RouterNodeWithNestedSkills_RemovesAllDescendantSkillVariables()
    {
        // Arrange
        var service = CreateService();
        var routerId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid();
        var branchBTaskId = Guid.NewGuid();
        var skillInBranchAId = Guid.NewGuid();
        var skillInBranchBId = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Decision Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "mode" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = branchATaskId },
                    new() { Name = "BranchB", TargetNodeId = branchBTaskId }
                }
            }
        };

        var branchATask = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = _procedureId,
            ParentId = routerId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchA Branch",
                StartTime = 0,
                Duration = 200
            }
        };

        var branchBTask = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = _procedureId,
            ParentId = routerId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchB Branch",
                StartTime = 0,
                Duration = 200
            }
        };

        var skillInA = CreateSkillExecutionNodeWithOutputs(
            skillInBranchAId, _procedureId, "WeldSkill", branchATaskId, "WeldStrength");
        var skillInB = CreateSkillExecutionNodeWithOutputs(
            skillInBranchBId, _procedureId, "PaintSkill", branchBTaskId, "CoatThickness", "ColorAccuracy");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(routerId)).ReturnsAsync(routerNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(routerId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(routerNode, branchATask, branchBTask, skillInA, skillInB);

        // Act
        var result = await service.DeleteNodeAsync(routerId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "WeldStrength"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "CoatThickness"), Times.Once);
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "ColorAccuracy"), Times.Once);
    }

    /// <summary>
    ///     Deep nesting: RouterNode → BranchTask → inner TaskNode → SkillExecutionNode.
    ///     Variables from the deeply nested skill must still be cleaned up.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_DeeplyNestedSkillNode_RemovesVariablesAtEveryDepth()
    {
        // Arrange
        var service = CreateService();
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var innerTaskId = Guid.NewGuid();
        var deepSkillId = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = _procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Outer Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Main", TargetNodeId = branchTaskId }
                }
            }
        };

        var branchTask = new TaskNode
        {
            Id = branchTaskId,
            ProcedureId = _procedureId,
            ParentId = routerId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Main Branch",
                StartTime = 0,
                Duration = 200
            }
        };

        var innerTask = new TaskNode
        {
            Id = innerTaskId,
            ProcedureId = _procedureId,
            ParentId = branchTaskId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Inner Group",
                StartTime = 0,
                Duration = 100
            }
        };

        var deepSkill = CreateSkillExecutionNodeWithOutputs(
            deepSkillId, _procedureId, "DeepSkill", innerTaskId, "DeepOutput");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(routerId)).ReturnsAsync(routerNode);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(routerId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(routerNode, branchTask, innerTask, deepSkill);

        // Act
        var result = await service.DeleteNodeAsync(routerId);

        // Assert
        result.Should().BeTrue();
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "DeepOutput"), Times.Once);
    }

    /// <summary>
    ///     Deleting a mid-level TaskNode that itself sits inside a router branch
    ///     should still clean up skill variables from its children, without
    ///     touching skills in sibling branches.
    /// </summary>
    [Fact]
    public async Task DeleteNodeAsync_MidLevelTaskNode_OnlyCleansUpOwnDescendants()
    {
        // Arrange
        var service = CreateService();
        var routerId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid();
        var branchBTaskId = Guid.NewGuid();
        var skillInAId = Guid.NewGuid();
        var skillInBId = Guid.NewGuid();

        // We are only deleting branchATask, NOT the router
        var branchATask = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = _procedureId,
            ParentId = routerId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchA Branch",
                StartTime = 0,
                Duration = 200
            }
        };

        var branchBTask = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = _procedureId,
            ParentId = routerId,
            Extent = "parent",
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchB Branch",
                StartTime = 0,
                Duration = 200
            }
        };

        var skillInA = CreateSkillExecutionNodeWithOutputs(
            skillInAId, _procedureId, "WeldSkill", branchATaskId, "WeldStrength");
        var skillInB = CreateSkillExecutionNodeWithOutputs(
            skillInBId, _procedureId, "PaintSkill", branchBTaskId, "CoatThickness");

        _mockProcedureRepository.Setup(x => x.GetNodeByIdAsync(branchATaskId)).ReturnsAsync(branchATask);
        _mockCrudOrchestrator.Setup(x => x.DeleteNodeTreeAsync(branchATaskId)).ReturnsAsync(true);
        SetupRemoveVariableAcceptsAll();
        SetupNodeHierarchy(branchATask, branchBTask, skillInA, skillInB);

        // Act — delete only branch A
        var result = await service.DeleteNodeAsync(branchATaskId);

        // Assert
        result.Should().BeTrue();

        // Branch A's skill variable should be removed
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "WeldStrength"), Times.Once);

        // Branch B's skill variable must NOT be touched
        _mockProcedureVariableService.Verify(
            x => x.RemoveVariableAsync(_procedureId, "CoatThickness"), Times.Never);
    }

    #endregion
}