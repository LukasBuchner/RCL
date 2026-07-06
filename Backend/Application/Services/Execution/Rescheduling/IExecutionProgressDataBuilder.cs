using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;

namespace FHOOE.Freydis.Application.Services.Execution.Rescheduling;

/// <summary>
///     Builds SkillExecutionProgress data from execution state for re-scheduling.
/// </summary>
/// <remarks>
///     This service is responsible for transforming execution state into progress data
///     that can be used by the timing calculation orchestrator for re-scheduling operations.
///     It handles both progress data from agents and synthetic progress for skills without agent updates.
/// </remarks>
public interface IExecutionProgressDataBuilder
{
    /// <summary>
    ///     Builds progress data dictionary from current execution states.
    /// </summary>
    /// <param name="states">All current skill execution states</param>
    /// <param name="currentTime">Current time for calculating elapsed time</param>
    /// <returns>Dictionary mapping ExecutionId to SkillExecutionProgress</returns>
    IReadOnlyDictionary<Guid, SkillExecutionProgress> BuildProgressData(
        IEnumerable<SkillExecutionState> states,
        DateTimeOffset currentTime);
}