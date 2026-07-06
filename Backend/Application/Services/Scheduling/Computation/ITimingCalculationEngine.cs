using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Computation;

/// <summary>
///     Engine responsible for timing calculations.
///     Handles duration calculations for both skill executions and task nodes.
/// </summary>
public interface ITimingCalculationEngine
{
    /// <summary>
    ///     Calculates timing information for processed nodes and their dependencies.
    /// </summary>
    /// <param name="processedNodes">The processed node hierarchy.</param>
    /// <param name="edges">The dependency edges between nodes.</param>
    /// <param name="options">Options controlling the timing calculation behavior.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Complete timing results for all nodes.</returns>
    Task<TimingResult> CalculateTimingsAsync(
        NodeHierarchyInfo nodeHierarchy,
        IReadOnlyList<DependencyEdge> edges,
        TimingCalculationOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Options for controlling timing calculation behavior.
/// </summary>
public record TimingCalculationOptions
{
    /// <summary>
    ///     The procedure ID for logging and error reporting.
    /// </summary>
    public required Guid ProcedureId { get; init; }

    /// <summary>
    ///     The current time from which to schedule forward. Tasks not yet started cannot begin before this time.
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    ///     Whether to use strict mode for agent capability matching during execution graph building.
    /// </summary>
    public bool StrictMode { get; init; }

    /// <summary>
    ///     Whether to preserve original task node durations when no skill executions exist.
    /// </summary>
    public bool PreserveOriginalTaskDurations { get; init; } = true;

    /// <summary>
    ///     Whether to include detailed timing information in the result.
    /// </summary>
    public bool IncludeDetailedTiming { get; init; } = true;

    /// <summary>
    ///     Duration provider for skill execution analysis (planning mode or execution-aware mode).
    /// </summary>
    public required ISkillDurationProvider DurationProvider { get; init; }

    /// <summary>
    ///     [Execution Mode] Router selections from execution trigger service.
    ///     Maps router node ID to the selected target node ID (the chosen branch).
    ///     When provided, used to calculate RouterNode durations based on selected branch target.
    ///     Null or empty in planning mode or when no routers have been evaluated yet.
    /// </summary>
    public IReadOnlyDictionary<Guid, Guid>? RouterSelections { get; init; }
}

/// <summary>
///     Result of timing calculations containing all calculated timing information.
/// </summary>
public record TimingResult
{
    /// <summary>
    ///     Dictionary mapping node IDs to their calculated durations.
    /// </summary>
    public required IReadOnlyDictionary<Guid, double> Durations { get; init; }

    /// <summary>
    ///     Detailed timing information for all nodes (optional based on calculation options).
    /// </summary>
    public IReadOnlyDictionary<Guid, NodeTimingInfo>? DetailedTimingInfo { get; init; }

    /// <summary>
    ///     The updated domain nodes with timing information applied.
    /// </summary>
    public required IReadOnlyList<Node> UpdatedNodes { get; init; }

    /// <summary>
    ///     Whether the timing calculation was successful.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    ///     Error message if timing calculation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Statistics about the timing calculation performance.
    /// </summary>
    public TimingCalculationStatistics Statistics { get; init; } = new();
}

/// <summary>
///     Detailed timing information for a single node.
/// </summary>
public record NodeTimingInfo
{
    /// <summary>
    ///     The node ID this timing information belongs to.
    /// </summary>
    public Guid NodeId { get; init; }

    /// <summary>
    ///     The calculated duration of the node.
    /// </summary>
    public double Duration { get; init; }

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
    ///     The type of node for context.
    /// </summary>
    public NodeTimingType NodeType { get; init; }

    /// <summary>
    ///     Whether this timing was calculated from scheduling or inherited from original values.
    /// </summary>
    public bool IsCalculated { get; init; }

    /// <summary>
    ///     Whether this node is on the critical path.
    /// </summary>
    public bool OnCriticalPath { get; init; }
}

/// <summary>
///     Enumeration of node timing types.
/// </summary>
public enum NodeTimingType
{
    /// <summary>
    ///     Timing for a skill execution node.
    /// </summary>
    SkillExecution,

    /// <summary>
    ///     Timing for a task node (aggregated from children).
    /// </summary>
    Task,

    /// <summary>
    ///     Timing for a router node (based on selected branch target).
    /// </summary>
    Router,

    /// <summary>
    ///     Original timing preserved when no calculation was possible.
    /// </summary>
    Original
}

/// <summary>
///     Statistics about timing calculation performance.
/// </summary>
public record TimingCalculationStatistics
{
    /// <summary>
    ///     Number of skill execution nodes processed.
    /// </summary>
    public int SkillExecutionNodesProcessed { get; init; }

    /// <summary>
    ///     Number of task nodes processed.
    /// </summary>
    public int TaskNodesProcessed { get; init; }

    /// <summary>
    ///     Time taken for execution graph building.
    /// </summary>
    public TimeSpan ExecutionGraphBuildTime { get; init; }

    /// <summary>
    ///     Time taken for scheduling calculations.
    /// </summary>
    public TimeSpan SchedulingTime { get; init; }

    /// <summary>
    ///     Time taken for domain model updates.
    /// </summary>
    public TimeSpan DomainUpdateTime { get; init; }

    /// <summary>
    ///     Total time for timing calculations.
    /// </summary>
    public TimeSpan TotalTime { get; init; }
}