using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     GraphQL input type for node operations using a discriminated union pattern.
///     Exactly one of the node type properties must be set.
/// </summary>
public record NodeInput
{
    /// <summary>
    ///     Task node input data, if this is a task node.
    /// </summary>
    public TaskNodeInput? TaskNode { get; set; }

    /// <summary>
    ///     Skill execution node input data, if this is a skill execution node.
    /// </summary>
    public SkillExecutionNodeInput? SkillExecutionNode { get; set; }

    /// <summary>
    ///     Router node input data, if this is a router node.
    /// </summary>
    public RouterNodeInput? RouterNode { get; set; }
}

/// <summary>
///     GraphQL input type for skill execution node operations.
///     Represents a leaf node that executes a specific skill.
/// </summary>
public record SkillExecutionNodeInput
{
    public required SkillExecutionTaskInput SkillExecutionTask { get; set; }
    public required Guid Id { get; set; }
    public required NodePosition Position { get; set; }
    public Guid? ParentId { get; set; }
    public string? Extent { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool? Selectable { get; set; }
    public bool? Selected { get; set; }
    public bool? Draggable { get; set; }
    public bool? Dragging { get; set; }
    public bool? Hidden { get; set; }
}

/// <summary>
///     GraphQL input type for skill execution task operations.
///     Contains the skill and agent information for execution.
/// </summary>
public record SkillExecutionTaskInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required double StartTime { get; set; }
    public required double Duration { get; set; }
    public Guid AgentId { get; set; }
    public required SkillInput Skill { get; set; }
}

/// <summary>
///     GraphQL input type for task node operations.
///     Represents a simple composite task (all children execute sequentially or in parallel).
///     Does NOT support routing - use RouterNodeInput for conditional branching.
/// </summary>
public record TaskNodeInput
{
    public Guid Id { get; set; }
    public required NodePosition Position { get; set; }
    public Guid? ParentId { get; set; }
    public string? Extent { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool? Selectable { get; set; }
    public bool? Selected { get; set; }
    public bool? Draggable { get; set; }
    public bool? Dragging { get; set; }
    public bool? Hidden { get; set; }
    public required TaskInput TaskInput { get; set; }
}

/// <summary>
///     GraphQL input type for task operations.
///     Contains basic task information (name, description, timing).
/// </summary>
public record TaskInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required double StartTime { get; set; }
    public required double Duration { get; set; }
    public bool? IsExecuting { get; set; }
}

/// <summary>
///     GraphQL input type for router node operations.
///     Represents a router with conditional branching (exactly one branch executes based on selector).
/// </summary>
public record RouterNodeInput
{
    public required Guid Id { get; set; }
    public required NodePosition Position { get; set; }
    public Guid? ParentId { get; set; }
    public string? Extent { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool? Selectable { get; set; }
    public bool? Selected { get; set; }
    public bool? Draggable { get; set; }
    public bool? Dragging { get; set; }
    public bool? Hidden { get; set; }
    public required RouterTaskInput RouterTaskInput { get; set; }
}

/// <summary>
///     GraphQL input type for router task operations.
///     Contains routing logic (selector and branches).
/// </summary>
public record RouterTaskInput
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required double StartTime { get; set; }
    public required double Duration { get; set; }
    public bool? IsExecuting { get; set; }

    /// <summary>
    ///     Selector expression for routing (required).
    ///     Evaluates to determine which branch to execute.
    /// </summary>
    public required SelectorExpressionInput Selector { get; set; }

    /// <summary>
    ///     Conditional branches for routing (required).
    ///     Each branch represents a possible execution path.
    ///     Must have at least one branch.
    /// </summary>
    public required IReadOnlyList<ConditionalBranchInput> Branches { get; set; }

    /// <summary>
    ///     Name of the branch manually selected by the user in design mode.
    ///     This is separate from SelectedBranchName which is set during execution.
    ///     Used for timeline filtering before execution begins.
    /// </summary>
    public string? ManuallySelectedBranch { get; set; }
}