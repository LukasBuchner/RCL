namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for Skill operations.
///     Represents skill data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the skill.</param>
/// <param name="Name">The name of the skill.</param>
/// <param name="Description">The description of the skill.</param>
/// <param name="Properties">The properties associated with the skill.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record SkillDto(
    Guid Id,
    string Name,
    string? Description,
    List<PropertyDto> Properties);