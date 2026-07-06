using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     High-level orchestration pipeline that coordinates the complete scheduling workflow.
/// </summary>
/// <remarks>
///     This pipeline is responsible for:
///     <list type="number">
///         <item>
///             <description>Filtering nodes based on router branch selections (Phase 0)</description>
///         </item>
///         <item>
///             <description>Processing the node hierarchy (separating task nodes from skill execution nodes)</description>
///         </item>
///         <item>
///             <description>Calculating timing information through execution graph building and scheduling</description>
///         </item>
///         <item>
///             <description>Computing node positions based on timing (X-axis) and hierarchy (Y-axis)</description>
///         </item>
///         <item>
///             <description>Returning fully updated domain nodes with timing and positioning information</description>
///         </item>
///     </list>
///     The pipeline ensures proper separation of concerns by delegating specific responsibilities
///     to specialized services while maintaining the overall workflow coordination.
/// </remarks>
public interface ITimingCalculationOrchestrator
{
    /// <summary>
    ///     Executes the complete scheduling pipeline to calculate durations and return updated domain nodes.
    ///     Includes Phase 0 router branch filtering when <see cref="SchedulingRequest.RouterSelections"/> is provided
    ///     or when nodes have persisted branch selections.
    /// </summary>
    /// <param name="request">The scheduling request containing all necessary input data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Complete schedule result with updated domain model and timing information.</returns>
    Task<ScheduleResult> CalculateAsync(SchedulingRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
///     Request object for scheduling pipeline operations.
///     Encapsulates all input parameters to avoid parameter proliferation.
///     Supports both planning mode (initial schedule calculation) and execution mode (recalculation with progress data).
/// </summary>
public record SchedulingRequest
{
    /// <summary>
    ///     The ID of the procedure for logging and error reporting.
    /// </summary>
    public required Guid ProcedureId { get; init; }

    /// <summary>
    ///     The input domain nodes to process.
    /// </summary>
    public required IReadOnlyList<Node> Nodes { get; init; }

    /// <summary>
    ///     The input domain edges representing dependencies.
    /// </summary>
    public required IReadOnlyList<DependencyEdge> Edges { get; init; }

    /// <summary>
    ///     The current time from which to schedule forward. Tasks not yet started cannot begin before this time.
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    ///     Whether to use strict mode for agent capability matching.
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
    ///     [Execution Mode] Reference timestamp for converting UTC times to relative times.
    ///     Set to the UTC time when procedure execution started.
    ///     Used to convert SkillExecutionProgress.ActualStartTimeUtc to relative times.
    ///     Null in planning mode.
    /// </summary>
    public DateTime? ProcedureStartTimeUtc { get; init; }

    /// <summary>
    ///     [Execution Mode] Progress data from agents, keyed by ExecutionId.
    ///     When present, these actual execution data points are used instead of estimates.
    ///     Null or empty in planning mode.
    /// </summary>
    public IReadOnlyDictionary<Guid, SkillExecutionProgress>? ExecutionProgressData { get; init; }

    /// <summary>
    ///     [Execution Mode] Router selections from execution trigger service.
    ///     Maps router node ID to the selected target node ID (the chosen branch).
    ///     When provided, non-selected branches are filtered out of the scheduling calculation.
    ///     Null or empty in planning mode or when no routers have been evaluated yet.
    /// </summary>
    public IReadOnlyDictionary<Guid, Guid>? RouterSelections { get; init; }

    /// <summary>
    ///     Indicates whether this request is for execution mode (with progress data) or planning mode.
    /// </summary>
    public bool IsExecutionMode => ProcedureStartTimeUtc.HasValue && ExecutionProgressData != null;
}