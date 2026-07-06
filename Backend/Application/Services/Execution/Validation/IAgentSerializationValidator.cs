using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Validates that every physical agent referenced in a procedure graph has its assigned
///     skills connected by a Finish-to-Start dependency chain, ensuring no two skills on the
///     same agent can be dispatched to the robot concurrently.
/// </summary>
/// <remarks>
///     <para>
///         The correctness of this check is formalised in the Lean 4 proof file
///         <c>AgentSerialization.lean</c>, specifically the lemmas
///         <c>fs_reachable_prevents_overlap</c> and <c>agent_serialization_sound</c>.
///         The implementation must remain consistent with those proofs.
///     </para>
///     <para>
///         The validator operates purely on the structural graph \u2014 nodes and edges \u2014 and carries
///         no side effects. It is safe to call at any point: during procedure save, at the start
///         of an execution request, or inside a CI validation pipeline.
///     </para>
/// </remarks>
public interface IAgentSerializationValidator
{
    /// <summary>
    ///     Inspects the procedure graph and returns a violation record for every physical agent
    ///     whose skills are not fully serialized by Finish-to-Start reachability.
    /// </summary>
    /// <param name="nodes">
    ///     All nodes in the procedure graph, including skill nodes, router nodes, and any other
    ///     node types. Only skill nodes that carry an agent assignment are examined; other node
    ///     types are traversed solely for reachability analysis.
    /// </param>
    /// <param name="edges">
    ///     All directed dependency edges in the procedure graph. An edge from A to B means
    ///     B cannot start until A has finished (Finish-to-Start semantics). The validator
    ///     uses these edges to compute transitive FS reachability between skill nodes.
    /// </param>
    /// <returns>
    ///     A list of <see cref="AgentSerializationViolation" /> records, one per offending agent.
    ///     Returns an empty list when every agent\u2019s skills are properly serialized.
    ///     Never returns <see langword="null" />.
    /// </returns>
    IReadOnlyList<AgentSerializationViolation> Validate(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges);
}