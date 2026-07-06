using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Immutable snapshot of a single execution run's loaded state. Built once after initialization
///     and threaded through the run, so each run reads only its own data and never a prior run's.
/// </summary>
internal sealed record ExecutionRunState
{
    /// <summary>The procedure's nodes for this run (post-schedule snapshot if available, else loaded).</summary>
    public required IReadOnlyList<Node> Nodes { get; init; }

    /// <summary>The procedure's dependency edges for this run.</summary>
    public required IReadOnlyList<DependencyEdge> Edges { get; init; }

    /// <summary>The initial schedule computed at load time, or <see langword="null" /> if none was produced.</summary>
    public ScheduleResult? Schedule { get; init; }

    /// <summary>The wall-clock instant this run's execution started, used as the timing reference.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>The identifier of the procedure being executed.</summary>
    public required Guid ProcedureId { get; init; }
}