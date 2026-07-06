using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Domain.Entities.Procedure;

/// <summary>
///     Represents a defined procedure or workflow, composed of a collection of
///     nodes (tasks/skills) and their dependencies.
/// </summary>
public record Procedure
{
    /// <summary>
    ///     Unique identifier for the procedure.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     A user-friendly name for the procedure.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Optional description of the procedure.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Timestamp of when this procedure was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    ///     Timestamp of the last update to this procedure's definition.
    /// </summary>
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <summary>
    ///     Nodes part of the procedure
    /// </summary>
    public IReadOnlyList<Node>? Nodes { get; set; }

    /// <summary>
    ///     Edges part of the procedure
    /// </summary>
    public IReadOnlyList<DependencyEdge>? Edges { get; init; }

    /// <summary>
    ///     Identifiers of the root nodes that initiate this procedure.
    ///     A procedure might have multiple entry points.
    /// </summary>
    public required IReadOnlyList<Guid> RootNodeIds { get; set; }

    /// <summary>
    ///     Root nodes of the procedure.
    /// </summary>
    public IReadOnlyList<Node>? RootNodes { get; set; }

    /// <summary>
    ///     Variable definitions for this procedure.
    /// </summary>
    public IReadOnlyList<VariableDefinition> Variables { get; set; } = [];

    /// <summary>
    ///     Runtime variable context (not persisted to database).
    /// </summary>
    public VariableContext? RuntimeContext { get; set; }

    /// <summary>
    ///     Indicates whether this procedure is currently loaded/active.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    ///     Timestamp of when this procedure was last loaded.
    /// </summary>
    public DateTime? LastLoadedUtc { get; set; }
}