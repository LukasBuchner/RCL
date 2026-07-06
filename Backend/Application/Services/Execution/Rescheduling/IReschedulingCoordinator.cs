using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Rescheduling;

/// <summary>
///     Coordinates the re-scheduling of remaining skills based on current execution state.
/// </summary>
/// <remarks>
///     This coordinator is responsible for:
///     <list type="number">
///         <item>
///             <description>Collecting current execution progress data from all skills</description>
///         </item>
///         <item>
///             <description>Calculating the current execution time</description>
///         </item>
///         <item>
///             <description>Requesting a new schedule from the timing calculation orchestrator</description>
///         </item>
///         <item>
///             <description>Returning the updated nodes if re-scheduling succeeds</description>
///         </item>
///     </list>
///     It acts as a bridge between the execution orchestrator and the scheduling pipeline,
///     ensuring that re-scheduling requests contain all necessary execution context.
/// </remarks>
public interface IReschedulingCoordinator
{
    /// <summary>
    ///     Initializes the coordinator with execution context.
    ///     This must be called before calling RescheduleAsync.
    /// </summary>
    /// <param name="procedureId">The procedure ID</param>
    /// <param name="nodes">The current nodes</param>
    /// <param name="edges">The current edges</param>
    /// <param name="executionStartTime">The execution start time</param>
    void Initialize(
        Guid procedureId,
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        DateTimeOffset executionStartTime);

    /// <summary>
    ///     Updates the router selections from execution trigger service.
    ///     This allows filtering of non-selected branches during rescheduling.
    /// </summary>
    /// <param name="routerSelections">Dictionary mapping router node ID to selected target node ID</param>
    void SetRouterSelections(IReadOnlyDictionary<Guid, Guid>? routerSelections);

    /// <summary>
    ///     Re-schedules remaining skills based on current execution state.
    /// </summary>
    /// <param name="reason">The reason for re-scheduling (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing updated nodes if successful, or null if re-scheduling failed</returns>
    Task<ReschedulingResult> RescheduleAsync(RescheduleReason reason, CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of a re-scheduling operation.
/// </summary>
public record ReschedulingResult
{
    /// <summary>
    ///     Indicates whether the re-scheduling was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     The updated nodes after re-scheduling, or null if re-scheduling failed.
    /// </summary>
    public IReadOnlyList<Node>? UpdatedNodes { get; init; }

    /// <summary>
    ///     The complete schedule result from the timing calculation orchestrator.
    /// </summary>
    public ScheduleResult? ScheduleResult { get; init; }

    /// <summary>
    ///     Error message if re-scheduling failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     The current execution time when re-scheduling was performed.
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    ///     Snapshot of execution completeness at the moment this result was produced.
    ///     <c>true</c> when every <see cref="SkillExecutionNode" /> in the source graph is in a
    ///     terminal state (Completed, Failed, or NotSelected). Stamped inside the coordinator's
    ///     async body so it is snapshot-consistent with <see cref="UpdatedNodes" /> — the same
    ///     state read by the scheduling computation produced both values.
    /// </summary>
    /// <remarks>
    ///     Downstream operators rely on this flag to detect completion without reading sidecar
    ///     state from the progress monitor. When <c>true</c>, every skill node in
    ///     <see cref="UpdatedNodes" /> carries a non-null finish time (terminal-state invariant
    ///     enforced by the scheduling pipeline).
    /// </remarks>
    public bool IsExecutionComplete { get; init; }
}