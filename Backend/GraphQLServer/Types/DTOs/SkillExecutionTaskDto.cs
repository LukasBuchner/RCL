namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for SkillExecutionTask operations.
///     Represents skill execution task data in a format suitable for application layer processing.
/// </summary>
/// <param name="Name">The name of the skill execution task.</param>
/// <param name="Description">The description of the skill execution task.</param>
/// <param name="StartTime">The start time of the task.</param>
/// <param name="Duration">The duration of the task.</param>
/// <param name="AgentId">The ID of the agent assigned to execute this skill.</param>
/// <param name="Skill">The skill to be executed with its configuration.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record SkillExecutionTaskDto(
    string Name,
    string? Description,
    double StartTime,
    double Duration,
    Guid AgentId,
    SkillDto Skill);