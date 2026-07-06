using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Services.Scheduling.Duration;

/// <summary>
///     Planning mode duration provider that uses <see cref="IAgentCapabilityAnalyzer" />
///     to estimate skill execution durations based on agent capabilities.
///     This is used during initial procedure planning before execution begins.
/// </summary>
public class PlanningModeDurationProvider : ISkillDurationProvider
{
    private readonly IAgentCapabilityAnalyzer _capabilityAnalyzer;
    private readonly ILogger<PlanningModeDurationProvider> _logger;
    private readonly INodeAgentMapper _nodeAgentMapper;

    /// <summary>
    ///     Initializes a new instance of <see cref="PlanningModeDurationProvider" />.
    /// </summary>
    public PlanningModeDurationProvider(
        INodeAgentMapper nodeAgentMapper,
        IAgentCapabilityAnalyzer capabilityAnalyzer,
        ILogger<PlanningModeDurationProvider> logger)
    {
        _nodeAgentMapper = nodeAgentMapper ?? throw new ArgumentNullException(nameof(nodeAgentMapper));
        _capabilityAnalyzer = capabilityAnalyzer ?? throw new ArgumentNullException(nameof(capabilityAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IPlannedSkillExecution?> AnalyzeAsync(
        SkillExecutionNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.SkillExecutionTask);

        _logger.LogPlanningModeAnalysisStarted(node.Id);

        // Map node to agent
        var mapping = await _nodeAgentMapper.MapAsync(node);
        if (mapping is null)
        {
            _logger.LogNodeAgentMappingFailed(node.Id);
            return null;
        }

        var (domainSkill, domainAgent, runtimeAgent) = mapping.Value;

        // Analyze capability
        var result = await _capabilityAnalyzer.AnalyzeAsync(
            node.Id,
            domainSkill,
            domainAgent,
            runtimeAgent);

        if (result is null)
            _logger.LogCapabilityAnalysisFailed(node.Id, domainSkill.Id, domainAgent.Id);

        return result;
    }
}