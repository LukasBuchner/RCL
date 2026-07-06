namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for Node operations using a discriminated union pattern.
///     Represents node data in a format suitable for application layer processing.
/// </summary>
/// <param name="TaskNode">The task node data, if this is a task node.</param>
/// <param name="SkillExecutionNode">The skill execution node data, if this is a skill execution node.</param>
/// <param name="RouterNode">The router node data, if this is a router node.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles. Uses a discriminated union pattern
///     similar to the GraphQL OneOf pattern but in the application layer.
/// </remarks>
public record NodeDto(
    TaskNodeDto? TaskNode,
    SkillExecutionNodeDto? SkillExecutionNode,
    RouterNodeDto? RouterNode)
{
    /// <summary>
    ///     Gets a value indicating whether this is a task node.
    /// </summary>
    public bool IsTaskNode => TaskNode != null;

    /// <summary>
    ///     Gets a value indicating whether this is a skill execution node.
    /// </summary>
    public bool IsSkillExecutionNode => SkillExecutionNode != null;

    /// <summary>
    ///     Gets a value indicating whether this is a router node.
    /// </summary>
    public bool IsRouterNode => RouterNode != null;

    /// <summary>
    ///     Gets the ID of the node regardless of its type.
    /// </summary>
    public Guid Id => TaskNode?.Id ?? SkillExecutionNode?.Id ?? RouterNode?.Id ??
        throw new InvalidOperationException(
            "NodeDto must have either TaskNode, SkillExecutionNode, or RouterNode set.");

    /// <summary>
    ///     Validates that exactly one node type is set.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        var count = (TaskNode != null ? 1 : 0) +
                    (SkillExecutionNode != null ? 1 : 0) +
                    (RouterNode != null ? 1 : 0);
        if (count != 1)
            throw new ArgumentException(
                "NodeDto must have exactly one of TaskNode, SkillExecutionNode, or RouterNode set.");
    }
}