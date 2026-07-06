using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Scheduling.Filtering;

/// <summary>
/// Result of branch filtering operation
/// </summary>
public record BranchFilterResult
{
    /// <summary>
    /// Nodes that should be included in scheduling (selected branches + non-router nodes)
    /// </summary>
    public required IReadOnlyList<Node> IncludedNodes { get; init; }

    /// <summary>
    /// Nodes that were excluded (non-selected branch TaskNodes and descendants)
    /// </summary>
    public required IReadOnlyList<Node> ExcludedNodes { get; init; }

    /// <summary>
    /// Details of which branches were selected for each router
    /// </summary>
    public required IReadOnlyDictionary<Guid, BranchSelection> RouterSelections { get; init; }
}

/// <summary>
/// Information about a router's branch selection
/// </summary>
public record BranchSelection
{
    public required Guid RouterNodeId { get; init; }
    public required string RouterName { get; init; }
    public required Guid SelectedBranchTargetNodeId { get; init; }
    public required string SelectedBranchName { get; init; }
    public required string Reason { get; init; }
}