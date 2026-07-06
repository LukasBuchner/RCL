using System.Text.Json.Serialization;

namespace FHOOE.Freydis.Domain.Entities.Procedure;

/// <summary>
///     Represents a conditional branch in a router TaskNode.
///     Similar to a 'case' in a switch statement.
/// </summary>
public record ConditionalBranch
{
    /// <summary>
    ///     Human-readable name for this branch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Condition expression to evaluate for this branch.
    ///     Null or empty string indicates the default branch (like 'default:' in switch).
    ///     Examples: "quality_result == 'OK'", "temperature > 100"
    /// </summary>
    public string? Condition { get; init; }

    /// <summary>
    ///     Priority for evaluation order (lower number = higher priority).
    ///     Used when multiple branches might match (though they should be mutually exclusive).
    ///     Default branch typically has highest number (evaluated last).
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    ///     ID of the target node this branch routes to.
    ///     Optional - can be set later when connecting edges.
    /// </summary>
    public Guid? TargetNodeId { get; init; }

    /// <summary>
    ///     Navigation property to the target node (not persisted).
    ///     Resolved by Application layer.
    /// </summary>
    [JsonIgnore]
    public Node? TargetNode { get; set; }

    /// <summary>
    ///     Determines if this is the default branch (fallback when no conditions match).
    /// </summary>
    /// <returns>True if Condition is null or empty string, false otherwise.</returns>
    public bool IsDefaultBranch()
    {
        return string.IsNullOrEmpty(Condition);
    }
}