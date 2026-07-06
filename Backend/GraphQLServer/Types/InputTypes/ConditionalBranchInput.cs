namespace FHOOE.Freydis.GraphQLServer.Types.InputTypes;

/// <summary>
///     GraphQL input type for creating conditional branches in router nodes.
/// </summary>
public record ConditionalBranchInput
{
    /// <summary>
    ///     Human-readable name for this branch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Condition expression to evaluate for this branch.
    ///     Null or empty string indicates the default branch.
    /// </summary>
    public string? Condition { get; init; }

    /// <summary>
    ///     Priority for evaluation order (lower number = higher priority).
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    ///     ID of the target node this branch routes to.
    ///     Optional - can be set later when connecting edges.
    /// </summary>
    public Guid? TargetNodeId { get; init; }
}