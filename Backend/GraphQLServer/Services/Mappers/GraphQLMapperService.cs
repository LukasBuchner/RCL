using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using Microsoft.Extensions.Logging;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Services.Mappers;

/// <summary>
///     Unified GraphQL mapping service implementation that handles transformation from GraphQL types to domain entities.
///     Consolidates all GraphQL-specific mapping concerns in one place.
/// </summary>
/// <remarks>
///     This service operates at the GraphQL boundary and provides both direct GraphQL Input → Domain mapping
///     and legacy DTO → Domain mapping support. It validates entity relationships using application services.
/// </remarks>
public sealed partial class GraphQlMapperService : IGraphQlMapperService
{
    private readonly IAgentApplicationService _agentApplicationService;
    private readonly ILogger<GraphQlMapperService> _logger;
    private readonly IProcedureContext _procedureContext;
    private readonly ISkillApplicationService _skillApplicationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GraphQlMapperService" /> class.
    /// </summary>
    /// <param name="skillApplicationService">The skill application service for validating skills.</param>
    /// <param name="agentApplicationService">The agent application service for validating agents.</param>
    /// <param name="procedureContext">The context for accessing the currently loaded procedure.</param>
    /// <param name="logger">The logger for recording diagnostic and warning events.</param>
    public GraphQlMapperService(
        ISkillApplicationService skillApplicationService,
        IAgentApplicationService agentApplicationService,
        IProcedureContext procedureContext,
        ILogger<GraphQlMapperService> logger)
    {
        _skillApplicationService =
            skillApplicationService ?? throw new ArgumentNullException(nameof(skillApplicationService));
        _agentApplicationService =
            agentApplicationService ?? throw new ArgumentNullException(nameof(agentApplicationService));
        _procedureContext =
            procedureContext ?? throw new ArgumentNullException(nameof(procedureContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Direct GraphQL Input to Domain Mapping

    /// <inheritdoc />
    public async Task<Node> MapToNodeAsync(NodeInput nodeInput)
    {
        ArgumentNullException.ThrowIfNull(nodeInput);

        if (nodeInput.TaskNode is not null)
            return MapToTaskNode(nodeInput.TaskNode);

        if (nodeInput.SkillExecutionNode is not null)
            return await MapToSkillExecutionNodeAsync(nodeInput.SkillExecutionNode);

        if (nodeInput.RouterNode is not null)
            return MapToRouterNode(GraphQlDtoMapperService.MapToNodeDto(nodeInput).RouterNode!);

        throw new ArgumentException("Unknown or missing node type in NodeInput", nameof(nodeInput));
    }

    /// <inheritdoc />
    public async Task<Agent> MapToAgentAsync(AgentInput agentInput)
    {
        ArgumentNullException.ThrowIfNull(agentInput);

        List<Skill?> skills = [];
        if (agentInput.SkillIds is not null)
        {
            var skillTasks = agentInput.SkillIds.Select(skillId => _skillApplicationService.GetSkillByIdAsync(skillId));
            skills = (await Task.WhenAll(skillTasks))
                .Where(skill => skill != null)
                .ToList();
        }

        return new Agent
        {
            Id = agentInput.Id,
            Name = agentInput.Name,
            RepresentativeColor = agentInput.RepresentativeColor,
            SkillIds = skills.Where(s => s != null).Select(s => s!.Id).ToList()
        };
    }

    /// <inheritdoc />
    public Task<Skill> MapToSkillAsync(SkillInput skillInput)
    {
        ArgumentNullException.ThrowIfNull(skillInput);

        return Task.FromResult(new Skill
        {
            Id = skillInput.Id,
            Name = skillInput.Name,
            Description = skillInput.Description ?? string.Empty,
            Properties = skillInput.Properties?.Select(MapToProperty).ToList() ?? []
        });
    }

    /// <inheritdoc />
    public TypedProperty MapToProperty(PropertyInput propertyInput)
    {
        ArgumentNullException.ThrowIfNull(propertyInput);

        VariableBinding? binding = null;
        if (propertyInput.Binding != null)
            binding = new VariableBinding
            {
                VariableName = propertyInput.Binding.VariableName,
                Mode = propertyInput.Binding.Mode,
                TransformExpression = propertyInput.Binding.TransformExpression
            };

        return new TypedProperty
        {
            Name = propertyInput.Name,
            Value = MapToPropertyType(propertyInput.PropertyType),
            Direction = propertyInput.Direction,
            Binding = binding
        };
    }

    /// <inheritdoc />
    public TypedValue MapToPropertyType(PropertyTypeInput propertyTypeInput)
    {
        ArgumentNullException.ThrowIfNull(propertyTypeInput);

        if (propertyTypeInput.BooleanProperty != null)
            return TypedValue.Boolean(propertyTypeInput.BooleanProperty.Value);
        if (propertyTypeInput.NumberProperty != null)
            return TypedValue.Number(propertyTypeInput.NumberProperty.Value);
        if (propertyTypeInput.StringProperty is { Value: not null })
            return TypedValue.Text(propertyTypeInput.StringProperty.Value);
        if (propertyTypeInput.PositionProperty is { Value: not null })
            return TypedValue.Position(propertyTypeInput.PositionProperty.Value);
        if (propertyTypeInput.PositionTagProperty is { Value: not null })
        {
            var positionTag = new PositionTag
            {
                Id = propertyTypeInput.PositionTagProperty.Value.Id,
                Tag = propertyTypeInput.PositionTagProperty.Value.Tag,
                Position = propertyTypeInput.PositionTagProperty.Value.Position
            };
            return TypedValue.PositionTag(positionTag);
        }

        if (propertyTypeInput.SceneObjectProperty is { Value: not null })
        {
            var sceneObject = new SceneObject
            {
                Id = propertyTypeInput.SceneObjectProperty.Value.Id,
                Name = propertyTypeInput.SceneObjectProperty.Value.Name,
                Position = propertyTypeInput.SceneObjectProperty.Value.Position
            };
            return TypedValue.SceneObject(sceneObject);
        }

        throw new ArgumentException("Unknown or unassigned property type in PropertyTypeInput",
            nameof(propertyTypeInput));
    }

    #endregion

    #region DTO to Domain Mapping (Legacy Support)

    /// <inheritdoc />
    public Task<Agent> MapToAgentAsync(AgentDto agentDto)
    {
        ArgumentNullException.ThrowIfNull(agentDto);

        return Task.FromResult(new Agent
        {
            Id = agentDto.Id,
            Name = agentDto.Name,
            SkillIds = agentDto.Skills,
            RepresentativeColor = agentDto.RepresentativeColor,
            State = agentDto.State ?? AgentState.Registered,
            LastSeenUtc = agentDto.LastSeenUtc,
            Metadata = agentDto.Metadata
        });
    }

    /// <inheritdoc />
    public Task<Skill> MapToSkillAsync(SkillDto skillDto)
    {
        ArgumentNullException.ThrowIfNull(skillDto);

        var properties = skillDto.Properties.Select(p => new TypedProperty
        {
            Name = p.Name,
            Value = p.Value,
            Direction = PropertyDirection.Input
        })
            .ToList();

        return Task.FromResult(new Skill
        {
            Id = skillDto.Id,
            Name = skillDto.Name,
            Description = skillDto.Description ?? string.Empty,
            Properties = properties
        });
    }

    /// <inheritdoc />
    public Task<DependencyEdge> MapToDependencyEdgeAsync(DependencyEdgeDto dependencyEdgeDto)
    {
        ArgumentNullException.ThrowIfNull(dependencyEdgeDto);

        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var dependencyEdge = new DependencyEdge
        {
            Id = dependencyEdgeDto.Id,
            ProcedureId = procedureId,
            SourceId = dependencyEdgeDto.SourceId,
            TargetId = dependencyEdgeDto.TargetId,
            SourceHandle = dependencyEdgeDto.SourceHandle,
            TargetHandle = dependencyEdgeDto.TargetHandle
        };

        return Task.FromResult(dependencyEdge);
    }

    /// <inheritdoc />
    public Task<SceneObject> MapToSceneObjectAsync(SceneObjectDto sceneObjectDto)
    {
        ArgumentNullException.ThrowIfNull(sceneObjectDto);

        var sceneObject = new SceneObject
        {
            Id = sceneObjectDto.Id,
            Name = sceneObjectDto.Name,
            Position = sceneObjectDto.Position
        };

        return Task.FromResult(sceneObject);
    }

    /// <inheritdoc />
    public Task<PositionTag> MapToPositionTagAsync(PositionTagDto positionTagDto)
    {
        ArgumentNullException.ThrowIfNull(positionTagDto);

        var positionTag = new PositionTag
        {
            Id = positionTagDto.Id,
            Tag = positionTagDto.Tag,
            Position = positionTagDto.Position
        };

        return Task.FromResult(positionTag);
    }

    /// <inheritdoc />
    public async Task<Node> MapToNodeAsync(NodeDto nodeDto)
    {
        ArgumentNullException.ThrowIfNull(nodeDto);
        nodeDto.Validate();

        if (nodeDto.IsTaskNode) return MapToTaskNode(nodeDto.TaskNode!);

        if (nodeDto.IsSkillExecutionNode) return await MapToSkillExecutionNodeAsync(nodeDto.SkillExecutionNode!);

        if (nodeDto.IsRouterNode) return MapToRouterNode(nodeDto.RouterNode!);

        throw new InvalidOperationException(
            "NodeDto must have either TaskNode, SkillExecutionNode, or RouterNode set.");
    }

    #endregion

    #region Helper Methods

    /// <inheritdoc />
    public AgentDto MapToAgentDto(AgentInput agentInput)
    {
        ArgumentNullException.ThrowIfNull(agentInput);

        var properties = new List<PropertyDto>(); // AgentInput doesn't have properties currently
        var skills = agentInput.SkillIds ?? [];

        return new AgentDto(
            agentInput.Id,
            agentInput.Name,
            agentInput.RepresentativeColor,
            properties,
            skills // Metadata - Not provided in AgentInput
        );
    }

    private TaskNode MapToTaskNode(TaskNodeInput taskNodeInput)
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodePosition = new NodePosition
        {
            X = taskNodeInput.Position.X,
            Y = taskNodeInput.Position.Y
        };

        var task = new DomainTask
        {
            Name = taskNodeInput.TaskInput.Name,
            Description = taskNodeInput.TaskInput.Description,
            StartTime = taskNodeInput.TaskInput.StartTime,
            Duration = taskNodeInput.TaskInput.Duration
        };

        return new TaskNode
        {
            Id = taskNodeInput.Id,
            ProcedureId = procedureId,
            Position = nodePosition,
            ParentId = taskNodeInput.ParentId,
            Extent = taskNodeInput.Extent,
            Width = taskNodeInput.Width,
            Height = taskNodeInput.Height,
            Selectable = taskNodeInput.Selectable,
            Selected = taskNodeInput.Selected,
            Draggable = taskNodeInput.Draggable,
            Dragging = taskNodeInput.Dragging,
            Hidden = taskNodeInput.Hidden,
            Task = task
        };
    }

    private TaskNode MapToTaskNode(TaskNodeDto taskNodeDto)
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodePosition = new NodePosition
        {
            X = taskNodeDto.Position.X,
            Y = taskNodeDto.Position.Y
        };

        var task = new DomainTask
        {
            Name = taskNodeDto.Task.Name,
            Description = taskNodeDto.Task.Description,
            StartTime = taskNodeDto.Task.StartTime,
            Duration = taskNodeDto.Task.Duration,
            FinishTime = taskNodeDto.Task.FinishTime,
            IsExecuting = taskNodeDto.Task.IsExecuting,
            Progress = taskNodeDto.Task.Progress
        };

        return new TaskNode
        {
            Id = taskNodeDto.Id,
            ProcedureId = procedureId,
            Position = nodePosition,
            ParentId = taskNodeDto.ParentId,
            Extent = taskNodeDto.Extent,
            Width = taskNodeDto.Width,
            Height = taskNodeDto.Height,
            Selectable = taskNodeDto.Selectable,
            Selected = taskNodeDto.Selected,
            Draggable = taskNodeDto.Draggable,
            Dragging = taskNodeDto.Dragging,
            Hidden = taskNodeDto.Hidden,
            Task = task
        };
    }

    private RouterNode MapToRouterNode(RouterNodeDto routerNodeDto)
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodePosition = new NodePosition
        {
            X = routerNodeDto.Position.X,
            Y = routerNodeDto.Position.Y
        };

        var routerTask = new RouterTask
        {
            Name = routerNodeDto.RouterTask.Name,
            Description = routerNodeDto.RouterTask.Description,
            StartTime = routerNodeDto.RouterTask.StartTime,
            Duration = routerNodeDto.RouterTask.Duration,
            FinishTime = routerNodeDto.RouterTask.FinishTime,
            IsExecuting = routerNodeDto.RouterTask.IsExecuting,
            Progress = routerNodeDto.RouterTask.Progress,
            Selector = routerNodeDto.RouterTask.Selector,
            Branches = routerNodeDto.RouterTask.Branches,
            SelectedBranchTargetNodeId = routerNodeDto.RouterTask.SelectedBranchTargetNodeId,
            SelectedBranchName = routerNodeDto.RouterTask.SelectedBranchName,
            SelectedAtUtc = routerNodeDto.RouterTask.SelectedAtUtc,
            ManuallySelectedBranch = routerNodeDto.RouterTask.ManuallySelectedBranch
        };

        return new RouterNode
        {
            Id = routerNodeDto.Id,
            ProcedureId = procedureId,
            Position = nodePosition,
            ParentId = routerNodeDto.ParentId,
            Extent = routerNodeDto.Extent,
            Width = routerNodeDto.Width,
            Height = routerNodeDto.Height,
            Selectable = routerNodeDto.Selectable,
            Selected = routerNodeDto.Selected,
            Draggable = routerNodeDto.Draggable,
            Dragging = routerNodeDto.Dragging,
            Hidden = routerNodeDto.Hidden,
            RouterTask = routerTask
        };
    }

    private async Task<SkillExecutionNode> MapToSkillExecutionNodeAsync(SkillExecutionNodeInput skillExecutionNodeInput)
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodePosition = new NodePosition
        {
            X = skillExecutionNodeInput.Position.X,
            Y = skillExecutionNodeInput.Position.Y
        };

        var agent = await _agentApplicationService.GetAgentByIdAsync(skillExecutionNodeInput.SkillExecutionTask
            .AgentId);
        if (agent is null)
            throw new ArgumentException(
                $"Agent with ID {skillExecutionNodeInput.SkillExecutionTask.AgentId} not found.");

        var skillTemplate = await _skillApplicationService.GetSkillByIdAsync(
            skillExecutionNodeInput.SkillExecutionTask.Skill.Id);
        if (skillTemplate is null)
            throw new ArgumentException(
                $"Skill with ID {skillExecutionNodeInput.SkillExecutionTask.Skill.Id} not found.");

        List<TypedProperty> skillProperties;
        if (skillExecutionNodeInput.SkillExecutionTask.Skill.Properties != null)
        {
            skillProperties = skillExecutionNodeInput.SkillExecutionTask.Skill.Properties.Select(MapToProperty)
                .ToList();
        }
        else
        {
            LogSkillNoInstanceProperties(
                _logger,
                skillExecutionNodeInput.Id,
                skillExecutionNodeInput.SkillExecutionTask.Skill.Id);
            skillProperties = skillTemplate.Properties;
        }

        // Create skill instance with execution-specific properties
        var skill = skillTemplate with { Properties = skillProperties };

        var skillExecutionTask = new SkillExecutionTask
        {
            AgentId = skillExecutionNodeInput.SkillExecutionTask.AgentId,
            Skill = skill,
            Name = skillExecutionNodeInput.SkillExecutionTask.Name,
            Duration = skillExecutionNodeInput.SkillExecutionTask.Duration,
            Description = skillExecutionNodeInput.SkillExecutionTask.Description,
            StartTime = skillExecutionNodeInput.SkillExecutionTask.StartTime
        };

        return new SkillExecutionNode
        {
            Id = skillExecutionNodeInput.Id,
            ProcedureId = procedureId,
            Position = nodePosition,
            SkillExecutionTask = skillExecutionTask,
            ParentId = skillExecutionNodeInput.ParentId,
            Dragging = skillExecutionNodeInput.Dragging,
            Draggable = skillExecutionNodeInput.Draggable,
            Extent = skillExecutionNodeInput.Extent,
            Height = skillExecutionNodeInput.Height,
            Selected = skillExecutionNodeInput.Selected,
            Hidden = skillExecutionNodeInput.Hidden,
            Selectable = skillExecutionNodeInput.Selectable,
            Width = skillExecutionNodeInput.Width
        };
    }

    private async Task<SkillExecutionNode> MapToSkillExecutionNodeAsync(SkillExecutionNodeDto skillExecutionNodeDto)
    {
        var procedureId = _procedureContext.RequireCurrentProcedureId();

        var nodePosition = new NodePosition
        {
            X = skillExecutionNodeDto.Position.X,
            Y = skillExecutionNodeDto.Position.Y
        };

        Skill? skill;
        if (skillExecutionNodeDto.SkillExecutionTask.Skill.Id == Guid.Empty)
            throw new InvalidOperationException(
                "SkillExecutionNode requires an existing skill ID. Skills must be created separately before being referenced in SkillExecutionNodes.");

        skill = await _skillApplicationService.GetSkillByIdAsync(skillExecutionNodeDto.SkillExecutionTask.Skill.Id);
        if (skill == null)
            throw new InvalidOperationException(
                $"Skill with ID {skillExecutionNodeDto.SkillExecutionTask.Skill.Id} not found.");

        var agent = await _agentApplicationService.GetAgentByIdAsync(skillExecutionNodeDto.SkillExecutionTask.AgentId);
        if (agent == null)
            throw new InvalidOperationException(
                $"Agent with ID {skillExecutionNodeDto.SkillExecutionTask.AgentId} not found.");

        var skillExecutionTask = new SkillExecutionTask
        {
            Name = skillExecutionNodeDto.SkillExecutionTask.Name,
            Description = skillExecutionNodeDto.SkillExecutionTask.Description,
            StartTime = skillExecutionNodeDto.SkillExecutionTask.StartTime,
            Duration = skillExecutionNodeDto.SkillExecutionTask.Duration,
            Skill = skill,
            AgentId = skillExecutionNodeDto.SkillExecutionTask.AgentId
        };

        return new SkillExecutionNode
        {
            Id = skillExecutionNodeDto.Id,
            ProcedureId = procedureId,
            Position = nodePosition,
            ParentId = skillExecutionNodeDto.ParentId,
            Extent = skillExecutionNodeDto.Extent,
            Width = skillExecutionNodeDto.Width,
            Height = skillExecutionNodeDto.Height,
            Selectable = skillExecutionNodeDto.Selectable,
            Selected = skillExecutionNodeDto.Selected,
            Draggable = skillExecutionNodeDto.Draggable,
            Dragging = skillExecutionNodeDto.Dragging,
            Hidden = skillExecutionNodeDto.Hidden,
            SkillExecutionTask = skillExecutionTask
        };
    }

    #endregion

    /// <summary>
    ///     Logs at Warning level that a skill execution node input carried no instance-specific properties, so
    ///     the skill template's default properties are used for the execution instead.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The skill execution node identifier.</param>
    /// <param name="skillId">The skill template identifier.</param>
    [LoggerMessage(
        LogLevel.Warning,
        "SkillExecutionNode {NodeId} (skill {SkillId}) provided no instance-specific properties; falling back to template default properties for skill execution.")]
    private static partial void LogSkillNoInstanceProperties(ILogger logger, Guid nodeId, Guid skillId);
}