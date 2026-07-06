using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Models;

/// <summary>
///     Represents the result of a complete schedule calculation for a procedure.
///     Contains both duration information and detailed timing data for all nodes.
/// </summary>
public sealed record ScheduleResult
{
    /// <summary>
    ///     Complete scheduling information for all nodes including start times, finish times, and durations.
    /// </summary>
    public required IReadOnlyList<NodeSchedule> NodeSchedules { get; init; }

    /// <summary>
    ///     Indicates whether the schedule calculation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Error message if the schedule calculation failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     The updated nodes with calculated positions and timing information.
    ///     Includes both scheduled nodes with timings and hidden nodes (marked with <c>Hidden = true</c>)
    ///     that were excluded by router branch filtering.
    /// </summary>
    public IReadOnlyList<Node>? UpdatedNodes { get; init; }
}

/// <summary>
///     Detailed scheduling information for a single node in a procedure.
/// </summary>
public sealed record NodeSchedule
{
    /// <summary>
    ///     The unique identifier of the node.
    /// </summary>
    public required Guid NodeId { get; init; }

    /// <summary>
    ///     The calculated duration of the node.
    /// </summary>
    public required double Duration { get; init; }

    /// <summary>
    ///     The planned absolute start time of the node.
    /// </summary>
    public double AbsoluteStartTime { get; init; }

    /// <summary>
    ///     The planned absolute finish time of the node.
    /// </summary>
    public double AbsoluteFinishTime { get; init; }

    /// <summary>
    ///     The planned start time relative to its containment of the node.
    /// </summary>
    public double RelativeStartTime { get; init; }

    /// <summary>
    ///     The planned finish time relative to its containment of the node.
    /// </summary>
    public double RelativeFinishTime { get; init; }

    /// <summary>
    ///     The type of the node (TaskNode or SkillExecutionNode).
    /// </summary>
    public required NodeScheduleType NodeType { get; init; }

    /// <summary>
    ///     The ID of the parent node, if this node has a parent.
    /// </summary>
    public Guid? ParentNodeId { get; init; }
}

/// <summary>
///     Enumeration of node types that can be scheduled.
/// </summary>
public enum NodeScheduleType
{
    /// <summary>
    ///     A task node that groups skill executions.
    /// </summary>
    TaskNode,

    /// <summary>
    ///     A skill execution node that represents an actual executable skill.
    /// </summary>
    SkillExecutionNode,

    /// <summary>
    ///     A router node that represents conditional branching.
    /// </summary>
    RouterNode
}