using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Common;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Analyzes an agent’s capability to execute a skill and produces a planned-execution model.
/// </summary>
public interface IAgentCapabilityAnalyzer
{
    /// <summary>
    ///     Determines whether <paramref name="runtimeAgent" /> can execute <paramref name="skill" />,
    ///     and constructs the appropriate <see cref="IPlannedSkillExecution" />.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the procedure node.</param>
    /// <param name="skill">The domain skill to execute.</param>
    /// <param name="domainAgent">The domain agent that should execute it.</param>
    /// <param name="runtimeAgent">The agent assigned to execute the skill.</param>
    /// <returns>
    ///     A <see cref="IPlannedSkillExecution" /> instance (adaptive or fixed) if execution is possible; otherwise
    ///     <c>null</c>.
    /// </returns>
    Task<IPlannedSkillExecution?> AnalyzeAsync(
        Guid nodeId, Skill domainSkill, Agent domainAgent, IRuntimeAgent runtimeAgent);
}