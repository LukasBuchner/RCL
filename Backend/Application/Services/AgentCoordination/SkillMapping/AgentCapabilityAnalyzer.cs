using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.Support.Logging;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;

/// <summary>
///     Default implementation of <see cref="IAgentCapabilityAnalyzer" /> that queries the agent’s
///     adaptive capability and duration estimates.
/// </summary>
public class AgentCapabilityAnalyzer : IAgentCapabilityAnalyzer
{
    private readonly ILogger<AgentCapabilityAnalyzer> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="AgentCapabilityAnalyzer" />.
    /// </summary>
    /// <param name="logger">The logger for debugging and diagnostics.</param>
    public AgentCapabilityAnalyzer(ILogger<AgentCapabilityAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IPlannedSkillExecution?> AnalyzeAsync(
        Guid nodeId, Skill domainSkill, Agent domainAgent, IRuntimeAgent runtimeAgent)
    {
        _logger.LogCapabilityAnalysisStart(nodeId, domainSkill.Name, runtimeAgent.Name, runtimeAgent.Id);

        try
        {
            // Check if the agent can adaptively execute this skill
            var isAdaptive = await runtimeAgent
                .CanExecuteAdaptivelyAsync(domainSkill)
                .ConfigureAwait(false);

            // Get nominal & adaptive duration estimates
            var constraints = await runtimeAgent
                .GetExecutionEstimateAsync(domainSkill)
                .ConfigureAwait(false);

            _logger.LogAdaptiveCapabilityCheck(runtimeAgent.Name, domainSkill.Name, isAdaptive, constraints != null);

            if (constraints == null)
            {
                _logger.LogNoExecutionConstraints(domainSkill.Name, runtimeAgent.Name);
                return null;
            }

            _logger.LogExecutionConstraintsRetrieved(
                runtimeAgent.Id,
                domainSkill.Name,
                (int)(constraints.MinAdaptiveDuration ?? constraints.EstimatedNominalDuration));

            // Check for zero or negative durations
            if (constraints.EstimatedNominalDuration <= 0)
                _logger.LogZeroDurationDetected(domainSkill.Name, runtimeAgent.Name, "Nominal");

            var result = isAdaptive switch
            {
                // Adaptive execution path
                true when constraints is { MinAdaptiveDuration: not null } => new
                    PlannedAdaptiveSkillExecution
                {
                    Id = nodeId,
                    Name = domainSkill.Name,
                    DomainSkill = domainSkill,
                    DomainAgent = domainAgent,
                    RuntimeAgent = runtimeAgent,
                    MinDuration = constraints.MinAdaptiveDuration.Value,
                    PlannedDuration = constraints.EstimatedNominalDuration
                },
                // Fixed execution path
                false => new PlannedSkillExecution
                {
                    Id = nodeId,
                    Name = domainSkill.Name,
                    DomainSkill = domainSkill,
                    DomainAgent = domainAgent,
                    RuntimeAgent = runtimeAgent,
                    PlannedDuration = constraints.EstimatedNominalDuration
                },
                _ => null
            };

            if (result != null)
                _logger.LogPlannedSkillCreated(
                    result.Id,
                    result.Name,
                    runtimeAgent.Name,
                    (int)result.PlannedDuration,
                    result is PlannedAdaptiveSkillExecution);
            else
                _logger.LogPlannedSkillCreationFailed(
                    domainSkill.Name, runtimeAgent.Name, isAdaptive, constraints != null);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCapabilityAnalysisError(nodeId, domainSkill.Name, ex.Message);
            return null;
        }
    }
}