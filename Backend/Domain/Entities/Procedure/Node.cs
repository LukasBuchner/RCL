using FHOOE.Freydis.Domain.Entities.Common;
using HotChocolate.Types;

namespace FHOOE.Freydis.Domain.Entities.Procedure;

#region Base Types & Positioning

/// <summary>
///     Represents the position in xyflow with X and Y coordinates relative to the parent node.
/// </summary>
public record NodePosition
{
    /// <summary>
    ///     Relative to the parent node x position in the graph layout.
    /// </summary>
    public required double X { get; set; }

    /// <summary>
    ///     Relative to the parent node y position in the graph layout.
    /// </summary>
    public required double Y { get; set; }
}

/// <summary>
///     Abstract base class for all node types in a procedure graph.
///     Defines common properties for positioning, selection, and visual state.
/// </summary>
[UnionType("Node")]
public abstract record Node
{
    /// <summary>
    ///     Unique identifier for this node.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Foreign key to the procedure this node belongs to.
    /// </summary>
    public required Guid ProcedureId { get; set; }

    /// <summary>
    ///     Position of the node in the graph layout.
    /// </summary>
    public required NodePosition Position { get; set; }

    /// <summary>
    ///     Identifier of the parent node, if this node is nested within another.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     Extent property for ReactFlow node sizing.
    /// </summary>
    public string? Extent { get; set; }

    /// <summary>
    ///     Width of the node in the visual representation.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    ///     Height of the node in the visual representation.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    ///     Indicates whether the node can be selected by the user.
    /// </summary>
    public bool? Selectable { get; set; }

    /// <summary>
    ///     Indicates whether the node is currently selected.
    /// </summary>
    public bool? Selected { get; set; }

    /// <summary>
    ///     Indicates whether the node can be dragged by the user.
    /// </summary>
    public bool? Draggable { get; set; }

    /// <summary>
    ///     Indicates whether the node is currently being dragged.
    /// </summary>
    public bool? Dragging { get; set; }

    /// <summary>
    ///     Indicates whether the node is hidden in the visual representation.
    /// </summary>
    public bool? Hidden { get; set; }
}

#endregion

#region Task Types

/// <summary>
///     Represents a task with timing information and execution state.
///     Base class for all task types in the procedure execution model.
/// </summary>
public record Task
{
    /// <summary>
    ///     The display name of the task.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Optional description providing additional context about the task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Absolute start time from the beginning of the procedure.
    ///     For all tasks and skill executions, this represents the planned start time
    ///     relative to the procedure start (time 0).
    /// </summary>
    public required double StartTime { get; set; }

    /// <summary>
    ///     Expected or actual duration of the task execution in time units.
    /// </summary>
    public required double Duration { get; set; }

    /// <summary>
    ///     Absolute finish time from the beginning of the procedure.
    ///     Represents the planned end time relative to the procedure start (time 0).
    /// </summary>
    public double? FinishTime { get; set; }

    /// <summary>
    ///     Indicates whether the task is currently executing.
    /// </summary>
    public bool? IsExecuting { get; set; }

    /// <summary>
    ///     Execution progress as a percentage (0.0 to 1.0).
    /// </summary>
    public double? Progress { get; set; }
}

/// <summary>
///     Represents a task that executes a specific skill on an agent.
///     Extends the base Task with skill definition and agent assignment.
/// </summary>
public record SkillExecutionTask : Task
{
    /// <summary>
    ///     The skill to be executed as part of this task.
    /// </summary>
    public required Skill Skill { get; set; }

    /// <summary>
    ///     Identifier of the agent that will execute this skill.
    /// </summary>
    public required Guid AgentId { get; set; }

    /// <summary>
    ///     Unique identifier for this execution instance.
    ///     Only set when execution has started.
    /// </summary>
    public Guid? ExecutionId { get; set; }
}

/// <summary>
///     Represents a routing task that selects a branch based on conditional logic.
///     Used for decision points in procedure execution flow.
/// </summary>
public record RouterTask : Task
{
    /// <summary>
    ///     Expression used to evaluate which branch to select.
    /// </summary>
    public required SelectorExpression Selector { get; set; }

    /// <summary>
    ///     Available branches that can be selected by the router.
    /// </summary>
    public required IReadOnlyList<ConditionalBranch> Branches { get; set; }

    /// <summary>
    ///     ID of the target node for the branch that was selected during execution.
    ///     Null if the router has not been evaluated yet.
    /// </summary>
    public Guid? SelectedBranchTargetNodeId { get; set; }

    /// <summary>
    ///     Name of the branch that was selected during execution.
    ///     Null if the router has not been evaluated yet.
    /// </summary>
    public string? SelectedBranchName { get; set; }

    /// <summary>
    ///     UTC timestamp when the branch was selected during execution.
    ///     Null if the router has not been evaluated yet.
    /// </summary>
    public DateTime? SelectedAtUtc { get; set; }

    /// <summary>
    ///     Name of the branch manually selected by the user in design mode.
    ///     This is separate from SelectedBranchName which is set during execution.
    ///     Used for timeline filtering before execution begins.
    /// </summary>
    public string? ManuallySelectedBranch { get; set; }
}

#endregion

#region Node Implementations

/// <summary>
///     A node that represents a generic task in the procedure graph.
///     Contains task information without specific skill execution details.
/// </summary>
public record TaskNode : Node
{
    /// <summary>
    ///     Task information including name, description, and timing.
    /// </summary>
    public required Task Task { get; set; }
}

/// <summary>
///     A node that represents a skill execution task in the procedure graph.
///     Associates an agent with a specific skill to be executed.
/// </summary>
public record SkillExecutionNode : Node
{
    /// <summary>
    ///     The skill execution task containing skill definition, agent assignment, and execution state.
    /// </summary>
    public required SkillExecutionTask SkillExecutionTask { get; set; }
}

/// <summary>
///     A node that represents a decision point in the procedure graph.
///     Routes execution flow to different branches based on conditional logic.
/// </summary>
/// <remarks>
///     CONSTRAINT: RouterNodes cannot be nested — a RouterNode must not be a child of another RouterNode.
///     This invariant is enforced at creation time by the node application service to prevent
///     ambiguous branch resolution during execution.
/// </remarks>
public record RouterNode : Node
{
    /// <summary>
    ///     The router task containing selector expression and conditional branches.
    /// </summary>
    public required RouterTask RouterTask { get; set; }
}

#endregion