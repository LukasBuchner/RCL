using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Services.Scheduling.Duration;

/// <summary>
///     Execution-aware duration provider that uses actual <see cref="SkillExecutionProgress" /> data
///     from running agents when available, falling back to the planning provider for nodes not yet executing.
///     This is used during procedure execution to incorporate real-time progress data into scheduling.
/// </summary>
public class ExecutionAwareDurationProvider : ISkillDurationProvider
{
    private readonly ILogger<ExecutionAwareDurationProvider> _logger;
    private readonly ISkillDurationProvider _planningProvider;
    private readonly DateTime _procedureStartTimeUtc;
    private readonly IReadOnlyDictionary<Guid, SkillExecutionProgress> _progressData;

    /// <summary>
    ///     Initializes a new instance of <see cref="ExecutionAwareDurationProvider" />.
    /// </summary>
    /// <param name="planningProvider">Fallback provider for nodes without execution progress.</param>
    /// <param name="procedureStartTimeUtc">UTC timestamp when procedure execution began.</param>
    /// <param name="progressData">Progress data keyed by ExecutionId.</param>
    /// <param name="logger">Logger instance.</param>
    public ExecutionAwareDurationProvider(
        ISkillDurationProvider planningProvider,
        DateTime procedureStartTimeUtc,
        IReadOnlyDictionary<Guid, SkillExecutionProgress> progressData,
        ILogger<ExecutionAwareDurationProvider> logger)
    {
        _planningProvider = planningProvider ?? throw new ArgumentNullException(nameof(planningProvider));
        _procedureStartTimeUtc = procedureStartTimeUtc;
        _progressData = progressData ?? throw new ArgumentNullException(nameof(progressData));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IPlannedSkillExecution?> AnalyzeAsync(
        SkillExecutionNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(node.SkillExecutionTask);

        var executionId = node.SkillExecutionTask.ExecutionId;

        // If no ExecutionId or no progress data available, fall back to planning provider
        if (!executionId.HasValue || !_progressData.TryGetValue(executionId.Value, out var progress))
        {
            var executionIdText = executionId?.ToString() ?? "null";
            _logger.LogNoLiveProgressUsingPlanningEstimate(node.Id, executionIdText);

            return await _planningProvider.AnalyzeAsync(node, cancellationToken);
        }

        // Convert UTC start time to relative time (seconds since procedure start)
        var actualStartTime = (progress.ActualStartTimeUtc - _procedureStartTimeUtc).TotalSeconds;

        // Determine duration and finish time based on execution state
        double duration;
        double? actualFinishTime;

        if (progress.CompletedSuccessfully)
        {
            // Completed: Use actual duration and calculate actual finish time
            duration = progress.CurrentTimeIntoExecution;
            actualFinishTime = actualStartTime + duration;
        }
        else
        {
            // Still running: Use estimated duration, no actual finish time yet
            duration = progress.EstimatedTotalDuration;
            actualFinishTime = null;
        }

        // First, get the planning provider's result to obtain domain objects and runtime agent
        // We need these for the IPlannedSkillExecution interface
        var planningResult = await _planningProvider.AnalyzeAsync(node, cancellationToken);
        if (planningResult == null)
        {
            _logger.LogPlanningProviderReturnedNull(node.Id, executionId.Value);
            return null;
        }

        // Check if this is an adaptive skill (reports a minimum achievable bound)
        var isAdaptive = progress.MinAchievableDuration.HasValue;

        if (isAdaptive)
        {
            var result = new AdaptiveSkillExecution
            {
                Id = node.Id,
                ExecutionId = executionId.Value,
                PlannedDuration = duration,
                EstimatedDuration = duration,
                MinDuration = progress.MinAchievableDuration!.Value,
                ActualStartTime = actualStartTime,
                ActualFinishTime = actualFinishTime,
                Name = planningResult.Name,
                DomainSkill = planningResult.DomainSkill,
                DomainAgent = planningResult.DomainAgent,
                RuntimeAgent = planningResult.RuntimeAgent,
                LastProgress = progress
            };

            var adaptiveName = planningResult.Name ?? "Unknown";
            var adaptiveAgentId = planningResult.DomainAgent?.Id ?? Guid.Empty;
            var adaptiveStatus = progress.CompletedSuccessfully ? "FINISHED" : "RUNNING";

            _logger.LogSkillTiming(
                "DURATION_PROVIDER",
                node.Id,
                adaptiveName,
                adaptiveAgentId,
                executionId.Value,
                adaptiveStatus,
                true,
                plannedDuration: duration,
                actualStart: actualStartTime,
                actualFinish: actualFinishTime,
                estimatedDuration: duration,
                minDuration: progress.MinAchievableDuration!.Value,
                additionalInfo: "Created AdaptiveSkillExecution");

            return result;
        }
        else
        {
            var result = new SkillExecution
            {
                Id = node.Id,
                ExecutionId = executionId.Value,
                PlannedDuration = duration,
                EstimatedDuration = duration,
                ActualStartTime = actualStartTime,
                ActualFinishTime = actualFinishTime,
                Name = planningResult.Name,
                DomainSkill = planningResult.DomainSkill,
                DomainAgent = planningResult.DomainAgent,
                RuntimeAgent = planningResult.RuntimeAgent,
                LastProgress = progress
            };

            var skillName = planningResult.Name ?? "Unknown";
            var agentId = planningResult.DomainAgent?.Id ?? Guid.Empty;
            var executionStatus = progress.CompletedSuccessfully ? "FINISHED" : "RUNNING";

            _logger.LogSkillTiming(
                "DURATION_PROVIDER",
                node.Id,
                skillName,
                agentId,
                executionId.Value,
                executionStatus,
                false,
                plannedDuration: duration,
                actualStart: actualStartTime,
                actualFinish: actualFinishTime,
                estimatedDuration: duration,
                additionalInfo: "Created SkillExecution");

            return result;
        }
    }
}