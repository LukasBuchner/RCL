using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Services.Validation;

/// <summary>
///     Validates that a dependency edge between two nodes is structurally valid before persistence.
///     Enforces invariants such as self-loop prevention, node existence, router isolation,
///     cross-procedure checks, hierarchy level matching, duplicate prevention, event-level
///     cycle detection, target-handle restrictions on routers and tasks, and adaptive-capability
///     validation for finish-side dependencies into skill executions.
/// </summary>
public interface IDependencyEdgeValidator
{
    /// <summary>
    ///     Validates that a proposed dependency edge satisfies all structural rules.
    ///     Enforces the following invariants:
    ///     <list type="number">
    ///         <item><description>Self-loop: source and target must be different nodes.</description></item>
    ///         <item><description>Node existence: both source and target must exist.</description></item>
    ///         <item><description>Router branch isolation: first-level branch children of router nodes cannot participate in edges.</description></item>
    ///         <item><description>Router target handle: router nodes only accept edges on their start (left) handle.</description></item>
    ///         <item><description>Task target handle: task nodes only accept edges on their start (left) handle.</description></item>
    ///         <item><description>Adaptive capability: finish-side edges into a skill execution require the assigned agent to be online and to support adaptive execution for the target's skill.</description></item>
    ///         <item><description>Cross-procedure: both nodes must belong to the currently loaded procedure.</description></item>
    ///         <item><description>Same hierarchy level: both nodes must share the same parent.</description></item>
    ///         <item><description>Duplicate edge: an edge with the same source and target must not already exist.</description></item>
    ///         <item><description>Event-level cycle: adding the edge must not create a cycle in the event-level waits-for graph.</description></item>
    ///     </list>
    /// </summary>
    /// <param name="sourceId">The unique identifier of the source node for the edge.</param>
    /// <param name="targetId">The unique identifier of the target node for the edge.</param>
    /// <param name="sourceHandle">
    ///     The source handle indicating which event type the source emits
    ///     (e.g. "left" for Start, "right" or null for Finish).
    /// </param>
    /// <param name="targetHandle">
    ///     The target handle indicating which event type is gated on the target
    ///     (e.g. "left" for Start, "right" or null for Finish).
    /// </param>
    /// <param name="excludeEdgeId">
    ///     Optional edge ID to exclude from duplicate and cycle checks. Used during update operations
    ///     so the edge being updated does not conflict with itself.
    /// </param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    /// <exception cref="HotChocolate.GraphQLException">
    ///     Thrown when any of the structural validation rules are violated.
    /// </exception>
    Task ValidateAsync(
        Guid sourceId, Guid targetId,
        string? sourceHandle, string? targetHandle,
        Guid? excludeEdgeId = null);
}