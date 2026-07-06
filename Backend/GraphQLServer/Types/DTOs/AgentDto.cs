using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for Agent operations.
///     Represents agent data in a format suitable for application layer processing.
/// </summary>
/// <param name="Id">The unique identifier of the agent.</param>
/// <param name="Name">The name of the agent.</param>
/// <param name="RepresentativeColor">The color representing the agent.</param>
/// <param name="Properties">The properties associated with the agent.</param>
/// <param name="Skills">The skills available to the agent.</param>
/// <param name="State">The current operational state of the agent (optional).</param>
/// <param name="LastSeenUtc">The timestamp when the agent was last seen active (optional).</param>
/// <param name="Metadata">Additional metadata about the agent (optional).</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record AgentDto(
    Guid Id,
    string Name,
    string RepresentativeColor,
    List<PropertyDto> Properties,
    List<Guid> Skills,
    AgentState? State = null,
    DateTime? LastSeenUtc = null,
    Dictionary<string, object>? Metadata = null);