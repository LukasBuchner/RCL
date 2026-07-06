using FHOOE.Freydis.Domain.Entities.Procedure;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Services.Scheduling.Duration;

/// <summary>
///     Strategy interface for providing duration and timing information for skill execution nodes.
///     Supports both planning mode (using capability analysis) and execution mode (using actual progress data).
/// </summary>
public interface ISkillDurationProvider
{
    /// <summary>
    ///     Analyzes a skill execution node and returns timing information.
    /// </summary>
    /// <param name="node">The skill execution node to analyze.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    ///     A planned skill execution with duration and timing information,
    ///     or null if the node cannot be analyzed.
    /// </returns>
    Task<IPlannedSkillExecution?> AnalyzeAsync(
        SkillExecutionNode node,
        CancellationToken cancellationToken = default);
}