using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Immutable snapshot of the current procedure state, holding nodes, edges, and variables
///     in a single unified structure. Used by <see cref="ProcedureStateTracker" /> as the value
///     type for the single BehaviorSubject that replaces multiple separate BehaviorSubjects.
/// </summary>
public sealed record ProcedureState
{
    public static readonly ProcedureState Empty = new();

    public IReadOnlyList<Node> Nodes { get; init; } = [];
    public IReadOnlyList<DependencyEdge> Edges { get; init; } = [];
    public IReadOnlyList<VariableDefinition> Variables { get; init; } = [];
}