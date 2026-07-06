using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Maps a procedure graph <see cref="Node" /> to a tuple of (<see cref="Skill" />, <see cref="IRuntimeAgent" />).
/// </summary>
public interface INodeAgentMapper
{
    /// <summary>
    ///     Attempts to map the given <paramref name="node" /> to its executing <see cref="Skill" /> and
    ///     <see cref="IRuntimeAgent" />.
    /// </summary>
    /// <param name="node">The procedure graph node to map.</param>
    /// <returns>
    ///     A tuple of (<see cref="Skill" />, <see cref="Agent" />, <see cref="IRuntimeAgent" />) if the node is a
    ///     <see cref="SkillExecutionNode" />
    ///     and an agent is found; otherwise <c>null</c>.
    /// </returns>
    Task<(Skill DomainSkill, Agent DomainAgent, IRuntimeAgent Agent)?> MapAsync(Node node);
}