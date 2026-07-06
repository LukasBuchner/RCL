namespace FHOOE.Freydis.Domain.Entities.Procedure;

/// <summary>
///     Represents a directed dependency relationship between two nodes in a procedure graph.
///     The edge flows from source to target, meaning the target node depends on the source node
///     and cannot begin execution until the source has completed.
/// </summary>
/// <remarks>
///     Dependency edges define the execution order within a procedure. They are used by the
///     dependency graph analyzer to determine which nodes are ready for execution based on
///     the completion state of their prerequisites. Handles allow edges to connect to specific
///     ports on nodes (e.g., conditional branch outputs on router nodes).
/// </remarks>
public record DependencyEdge
{
    /// <summary>
    ///     Unique identifier for this dependency edge.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Foreign key to the procedure this edge belongs to.
    /// </summary>
    public required Guid ProcedureId { get; set; }

    /// <summary>
    ///     Identifier of the source (predecessor) node that must complete before the target can start.
    /// </summary>
    public required Guid SourceId { get; set; }

    /// <summary>
    ///     Identifier of the target (successor) node that depends on the source node's completion.
    /// </summary>
    public required Guid TargetId { get; set; }

    /// <summary>
    ///     Optional handle identifier on the source node specifying which output port this edge connects from.
    ///     Used for router nodes where different conditional branches have distinct output handles.
    /// </summary>
    public string? SourceHandle { get; set; }

    /// <summary>
    ///     Optional handle identifier on the target node specifying which input port this edge connects to.
    /// </summary>
    public string? TargetHandle { get; set; }
}