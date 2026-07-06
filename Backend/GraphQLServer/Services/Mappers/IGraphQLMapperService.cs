using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;

namespace FHOOE.Freydis.GraphQLServer.Services.Mappers;

/// <summary>
///     Unified GraphQL mapping service that handles transformation from GraphQL types to domain entities.
///     Consolidates all GraphQL-specific mapping concerns in one place.
/// </summary>
/// <remarks>
///     This service operates at the GraphQL boundary and provides direct mapping from GraphQL input types
///     to domain entities, eliminating the need for intermediate DTOs in most cases.
///     Also supports DTO-to-domain mapping for legacy workflows.
/// </remarks>
public interface IGraphQlMapperService
{
    #region Helper Methods

    /// <summary>
    ///     Maps a GraphQL input to an intermediate DTO for legacy workflows.
    /// </summary>
    /// <param name="agentInput">The GraphQL agent input to map.</param>
    /// <returns>The mapped agent DTO.</returns>
    AgentDto MapToAgentDto(AgentInput agentInput);

    #endregion

    #region Direct GraphQL Input to Domain Mapping

    /// <summary>
    ///     Asynchronously maps a <see cref="NodeInput" /> GraphQL input directly to a <see cref="Node" /> domain entity.
    /// </summary>
    /// <param name="nodeInput">The GraphQL node input to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped node entity.</returns>
    Task<Node> MapToNodeAsync(NodeInput nodeInput);

    /// <summary>
    ///     Asynchronously maps an <see cref="AgentInput" /> GraphQL input directly to an <see cref="Agent" /> domain entity.
    /// </summary>
    /// <param name="agentInput">The GraphQL agent input to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped agent entity.</returns>
    Task<Agent> MapToAgentAsync(AgentInput agentInput);

    /// <summary>
    ///     Asynchronously maps a <see cref="SkillInput" /> GraphQL input directly to a <see cref="Skill" /> domain entity.
    /// </summary>
    /// <param name="skillInput">The GraphQL skill input to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped skill entity.</returns>
    Task<Skill> MapToSkillAsync(SkillInput skillInput);

    /// <summary>
    ///     Maps a <see cref="PropertyInput" /> GraphQL input directly to a <see cref="TypedProperty" /> domain entity.
    /// </summary>
    /// <param name="propertyInput">The GraphQL property input to map.</param>
    /// <returns>The mapped property entity.</returns>
    TypedProperty MapToProperty(PropertyInput propertyInput);

    /// <summary>
    ///     Maps a <see cref="PropertyTypeInput" /> GraphQL input directly to a <see cref="TypedValue" /> domain entity.
    /// </summary>
    /// <param name="propertyTypeInput">The GraphQL property type input to map.</param>
    /// <returns>The mapped typed value.</returns>
    TypedValue MapToPropertyType(PropertyTypeInput propertyTypeInput);

    #endregion

    #region DTO to Domain Mapping (Legacy Support)

    /// <summary>
    ///     Asynchronously maps a <see cref="NodeDto" /> to a <see cref="Node" /> domain entity.
    ///     Provided for legacy DTO-based workflows.
    /// </summary>
    /// <param name="nodeDto">The node DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped node entity.</returns>
    Task<Node> MapToNodeAsync(NodeDto nodeDto);

    /// <summary>
    ///     Asynchronously maps an <see cref="AgentDto" /> to an <see cref="Agent" /> domain entity.
    ///     Provided for legacy DTO-based workflows.
    /// </summary>
    /// <param name="agentDto">The agent DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped agent entity.</returns>
    Task<Agent> MapToAgentAsync(AgentDto agentDto);

    /// <summary>
    ///     Asynchronously maps a <see cref="SkillDto" /> to a <see cref="Skill" /> domain entity.
    ///     Provided for legacy DTO-based workflows.
    /// </summary>
    /// <param name="skillDto">The skill DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped skill entity.</returns>
    Task<Skill> MapToSkillAsync(SkillDto skillDto);

    /// <summary>
    ///     Asynchronously maps a <see cref="DependencyEdgeDto" /> to a <see cref="DependencyEdge" /> domain entity.
    /// </summary>
    /// <param name="dependencyEdgeDto">The dependency edge DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped dependency edge entity.</returns>
    Task<DependencyEdge> MapToDependencyEdgeAsync(DependencyEdgeDto dependencyEdgeDto);

    /// <summary>
    ///     Asynchronously maps a <see cref="SceneObjectDto" /> to a <see cref="SceneObject" /> domain entity.
    /// </summary>
    /// <param name="sceneObjectDto">The scene object DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped scene object entity.</returns>
    Task<SceneObject> MapToSceneObjectAsync(SceneObjectDto sceneObjectDto);

    /// <summary>
    ///     Asynchronously maps a <see cref="PositionTagDto" /> to a <see cref="PositionTag" /> domain entity.
    /// </summary>
    /// <param name="positionTagDto">The position tag DTO to map.</param>
    /// <returns>A task representing the asynchronous operation with the mapped position tag entity.</returns>
    Task<PositionTag> MapToPositionTagAsync(PositionTagDto positionTagDto);

    #endregion
}