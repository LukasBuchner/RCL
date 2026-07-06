using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;

namespace FHOOE.Freydis.GraphQLServer.Services.Mappers;

/// <summary>
///     Service for mapping GraphQL input types to Application layer DTOs.
///     Maintains separation between GraphQL concerns and Application layer business logic.
/// </summary>
/// <remarks>
///     This service operates at the GraphQL boundary and handles the transformation
///     from GraphQL-specific input types to application-layer DTOs, ensuring proper
///     separation of concerns according to Clean Architecture principles.
/// </remarks>
public static class GraphQlDtoMapperService
{
    /// <summary>
    ///     Maps a <see cref="AgentInput" /> GraphQL input type to an <see cref="AgentDto" />.
    /// </summary>
    /// <param name="agentInput">The GraphQL agent input to map.</param>
    /// <returns>The mapped agent DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when agentInput is null.</exception>
    public static AgentDto MapToAgentDto(AgentInput agentInput)
    {
        ArgumentNullException.ThrowIfNull(agentInput);

        var properties = new List<PropertyDto>(); // AgentInput doesn't have properties currently
        var skills = agentInput.SkillIds ?? [];

        return new AgentDto(agentInput.Id, agentInput.Name, agentInput.RepresentativeColor, properties, skills);
    }

    /// <summary>
    ///     Maps a <see cref="SkillInput" /> GraphQL input type to a <see cref="SkillDto" />.
    /// </summary>
    /// <param name="skillInput">The GraphQL skill input to map.</param>
    /// <returns>The mapped skill DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when skillInput is null.</exception>
    public static SkillDto MapToSkillDto(SkillInput skillInput)
    {
        ArgumentNullException.ThrowIfNull(skillInput);

        var properties = skillInput.Properties
            ?.Select(p => new PropertyDto(p.Name, ConvertToPropertyType(p.PropertyType)))
            .ToList() ?? [];

        return new SkillDto(skillInput.Id, skillInput.Name, skillInput.Description, properties);
    }

    /// <summary>
    ///     Maps a <see cref="NodeInput" /> GraphQL input type to a <see cref="NodeDto" />.
    /// </summary>
    /// <param name="nodeInput">The GraphQL node input to map.</param>
    /// <returns>The mapped node DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when nodeInput is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no valid node type is set in the input.</exception>
    public static NodeDto MapToNodeDto(NodeInput nodeInput)
    {
        ArgumentNullException.ThrowIfNull(nodeInput);

        TaskNodeDto? taskNodeDto = null;
        SkillExecutionNodeDto? skillExecutionNodeDto = null;
        RouterNodeDto? routerNodeDto = null;

        if (nodeInput.TaskNode != null)
        {
            var taskDto = new TaskDto(
                nodeInput.TaskNode.TaskInput.Name,
                nodeInput.TaskNode.TaskInput.Description,
                nodeInput.TaskNode.TaskInput.StartTime,
                nodeInput.TaskNode.TaskInput.Duration,
                nodeInput.TaskNode.TaskInput.StartTime + nodeInput.TaskNode.TaskInput.Duration,
                nodeInput.TaskNode.TaskInput.IsExecuting ?? false,
                0.0);

            taskNodeDto = new TaskNodeDto(
                nodeInput.TaskNode.Id,
                new NodePositionDto(nodeInput.TaskNode.Position.X, nodeInput.TaskNode.Position.Y),
                nodeInput.TaskNode.ParentId,
                nodeInput.TaskNode.Extent,
                nodeInput.TaskNode.Width,
                nodeInput.TaskNode.Height,
                nodeInput.TaskNode.Selectable,
                nodeInput.TaskNode.Selected,
                nodeInput.TaskNode.Draggable,
                nodeInput.TaskNode.Dragging,
                nodeInput.TaskNode.Hidden,
                taskDto);
        }
        else if (nodeInput.SkillExecutionNode != null)
        {
            // Map the full skill input to SkillDto
            var skillDto = MapToSkillDto(nodeInput.SkillExecutionNode.SkillExecutionTask.Skill);

            var skillExecutionTaskDto = new SkillExecutionTaskDto(
                nodeInput.SkillExecutionNode.SkillExecutionTask.Name,
                nodeInput.SkillExecutionNode.SkillExecutionTask.Description,
                nodeInput.SkillExecutionNode.SkillExecutionTask.StartTime,
                nodeInput.SkillExecutionNode.SkillExecutionTask.Duration,
                nodeInput.SkillExecutionNode.SkillExecutionTask.AgentId,
                skillDto);

            skillExecutionNodeDto = new SkillExecutionNodeDto(
                nodeInput.SkillExecutionNode.Id,
                new NodePositionDto(nodeInput.SkillExecutionNode.Position.X, nodeInput.SkillExecutionNode.Position.Y),
                nodeInput.SkillExecutionNode.ParentId,
                nodeInput.SkillExecutionNode.Extent,
                nodeInput.SkillExecutionNode.Width,
                nodeInput.SkillExecutionNode.Height,
                nodeInput.SkillExecutionNode.Selectable,
                nodeInput.SkillExecutionNode.Selected,
                nodeInput.SkillExecutionNode.Draggable,
                nodeInput.SkillExecutionNode.Dragging,
                nodeInput.SkillExecutionNode.Hidden,
                skillExecutionTaskDto);
        }
        else if (nodeInput.RouterNode != null)
        {
            // Map selector (required)
            var selector = MapFromSelectorInput(nodeInput.RouterNode.RouterTaskInput.Selector);

            // Map branches (required)
            var branches = nodeInput.RouterNode.RouterTaskInput.Branches
                .Select(MapFromConditionalBranchInput)
                .ToList();

            var routerTaskDto = new RouterTaskDto(
                nodeInput.RouterNode.RouterTaskInput.Name,
                nodeInput.RouterNode.RouterTaskInput.Description,
                nodeInput.RouterNode.RouterTaskInput.StartTime,
                nodeInput.RouterNode.RouterTaskInput.Duration,
                nodeInput.RouterNode.RouterTaskInput.StartTime + nodeInput.RouterNode.RouterTaskInput.Duration,
                nodeInput.RouterNode.RouterTaskInput.IsExecuting ?? false,
                0.0,
                selector,
                branches,
                null, // SelectedBranchTargetNodeId - set during execution
                null, // SelectedBranchName - set during execution
                null, // SelectedAtUtc - set during execution
                nodeInput.RouterNode.RouterTaskInput
                    .ManuallySelectedBranch); // ManuallySelectedBranch - set by user in design mode

            routerNodeDto = new RouterNodeDto(
                nodeInput.RouterNode.Id,
                new NodePositionDto(nodeInput.RouterNode.Position.X, nodeInput.RouterNode.Position.Y),
                nodeInput.RouterNode.ParentId,
                nodeInput.RouterNode.Extent,
                nodeInput.RouterNode.Width,
                nodeInput.RouterNode.Height,
                nodeInput.RouterNode.Selectable,
                nodeInput.RouterNode.Selected,
                nodeInput.RouterNode.Draggable,
                nodeInput.RouterNode.Dragging,
                nodeInput.RouterNode.Hidden,
                routerTaskDto);
        }
        else
        {
            throw new ArgumentException("No valid node type set in NodeInput.", nameof(nodeInput));
        }

        return new NodeDto(taskNodeDto, skillExecutionNodeDto, routerNodeDto);
    }

    /// <summary>
    ///     Converts a <see cref="PropertyTypeInput" /> to a <see cref="TypedValue" />.
    /// </summary>
    /// <param name="input">The property type input to convert.</param>
    /// <returns>The converted typed value.</returns>
    /// <exception cref="ArgumentException">Thrown when no valid property type is set in the input.</exception>
    private static TypedValue ConvertToPropertyType(PropertyTypeInput input)
    {
        if (input.BooleanProperty != null)
            return TypedValue.Boolean(input.BooleanProperty.Value);
        if (input.NumberProperty != null)
            return TypedValue.Number(input.NumberProperty.Value);
        if (input.StringProperty is { Value: not null })
            return TypedValue.Text(input.StringProperty.Value);
        if (input.PositionProperty is { Value: not null })
            return TypedValue.Position(input.PositionProperty.Value);
        if (input.PositionTagProperty is { Value: not null })
        {
            var positionTag = new PositionTag
            {
                Id = input.PositionTagProperty.Value.Id,
                Tag = input.PositionTagProperty.Value.Tag,
                Position = input.PositionTagProperty.Value.Position
            };
            return TypedValue.PositionTag(positionTag);
        }

        if (input.SceneObjectProperty is { Value: not null })
        {
            var sceneObject = new SceneObject
            {
                Id = input.SceneObjectProperty.Value.Id,
                Name = input.SceneObjectProperty.Value.Name,
                Position = input.SceneObjectProperty.Value.Position
            };
            return TypedValue.SceneObject(sceneObject);
        }

        throw new ArgumentException("No valid property type set in PropertyTypeInput.", nameof(input));
    }

    /// <summary>
    ///     Maps a domain VariableDefinition to a DTO.
    /// </summary>
    /// <param name="variableDefinition">Domain variable definition.</param>
    /// <returns>DTO representation of the variable definition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when variableDefinition is null.</exception>
    public static VariableDefinitionDto MapToVariableDefinitionDto(VariableDefinition variableDefinition)
    {
        ArgumentNullException.ThrowIfNull(variableDefinition);

        return new VariableDefinitionDto(
            variableDefinition.Name,
            variableDefinition.Type,
            variableDefinition.DefaultValue,
            variableDefinition.Scope,
            variableDefinition.Source,
            variableDefinition.Description,
            variableDefinition.IsReadOnly);
    }

    /// <summary>
    ///     Maps a VariableDefinitionInput to a domain VariableDefinition.
    /// </summary>
    /// <param name="input">GraphQL input for variable definition.</param>
    /// <returns>Domain variable definition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static VariableDefinition MapFromVariableDefinitionInput(VariableDefinitionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new VariableDefinition
        {
            Name = input.Name,
            Type = input.Type,
            DefaultValue = input.DefaultValue,
            Scope = input.Scope,
            Source = input.Source,
            Description = input.Description,
            IsReadOnly = input.IsReadOnly
        };
    }

    /// <summary>
    ///     Maps a VariableValueInput to a name-value tuple.
    /// </summary>
    /// <param name="input">GraphQL input for variable value.</param>
    /// <returns>Tuple containing variable name and value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static (string Name, object Value) MapFromVariableValueInput(VariableValueInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return (input.Name, input.Value);
    }

    /// <summary>
    ///     Maps a collection of VariableValueInputs to a dictionary.
    ///     If duplicate names exist, the last value wins.
    /// </summary>
    /// <param name="inputs">Collection of variable value inputs.</param>
    /// <returns>Dictionary mapping variable names to their values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputs is null.</exception>
    public static Dictionary<string, object> MapVariableValueInputsToDictionary(IEnumerable<VariableValueInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var dictionary = new Dictionary<string, object>();
        foreach (var input in inputs) dictionary[input.Name] = input.Value; // Overwrites if duplicate key exists
        return dictionary;
    }

    /// <summary>
    ///     Maps a SelectorExpressionInput to a domain SelectorExpression.
    ///     Handles polymorphic selector types (Simple, Conditional, Complex) using OneOf pattern.
    ///     Exactly one of the selector properties must be set.
    /// </summary>
    /// <param name="input">GraphQL selector expression input.</param>
    /// <returns>Domain selector expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no valid selector type is set.</exception>
    public static SelectorExpression MapFromSelectorInput(SelectorExpressionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.SimpleVariableSelector != null)
            return new SimpleVariableSelector { Expression = input.SimpleVariableSelector.Expression };

        if (input.ExpressionSelector != null)
            return new ExpressionSelector { Expression = input.ExpressionSelector.Expression };

        throw new ArgumentException("No valid selector type set in SelectorExpressionInput.", nameof(input));
    }

    /// <summary>
    ///     Maps a ConditionalBranchInput to a domain ConditionalBranch.
    /// </summary>
    /// <param name="input">GraphQL conditional branch input.</param>
    /// <returns>Domain conditional branch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static ConditionalBranch MapFromConditionalBranchInput(ConditionalBranchInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ConditionalBranch
        {
            Name = input.Name,
            Condition = input.Condition,
            Priority = input.Priority,
            TargetNodeId = input.TargetNodeId
        };
    }

    /// <summary>
    ///     Maps a domain ConditionalBranch to a DTO.
    /// </summary>
    /// <param name="branch">Domain conditional branch.</param>
    /// <returns>DTO representation of the conditional branch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when branch is null.</exception>
    public static ConditionalBranchDto MapToConditionalBranchDto(ConditionalBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        return new ConditionalBranchDto(
            branch.Name,
            branch.Condition,
            branch.Priority,
            branch.TargetNodeId);
    }
}