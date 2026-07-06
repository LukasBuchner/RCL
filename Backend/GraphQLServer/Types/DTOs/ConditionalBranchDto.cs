namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for ConditionalBranch.
///     Represents a conditional branch in a router TaskNode.
/// </summary>
/// <param name="Name">Human-readable name for this branch.</param>
/// <param name="Condition">Condition expression to evaluate for this branch.</param>
/// <param name="Priority">Priority for evaluation order.</param>
/// <param name="TargetNodeId">ID of the target node this branch routes to. Optional - can be set later.</param>
public record ConditionalBranchDto(
    string Name,
    string? Condition,
    int Priority,
    Guid? TargetNodeId);