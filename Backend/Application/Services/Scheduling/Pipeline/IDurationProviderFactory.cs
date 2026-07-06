using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Factory for creating appropriate duration providers based on execution mode.
/// </summary>
public interface IDurationProviderFactory
{
    /// <summary>
    ///     Creates the appropriate duration provider based on execution mode.
    /// </summary>
    /// <param name="isExecutionMode">
    ///     True if in execution mode (requires actual progress data),
    ///     false if in planning mode (uses capability analysis).
    /// </param>
    /// <param name="procedureStartTimeUtc">
    ///     The UTC start time of the procedure. Required when in execution mode.
    /// </param>
    /// <param name="executionProgressData">
    ///     Dictionary mapping node IDs to their execution progress. Required when in execution mode.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="ISkillDurationProvider" /> appropriate for the given mode.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="procedureStartTimeUtc" /> or <paramref name="executionProgressData" />
    ///     are null while <paramref name="isExecutionMode" /> is true.
    /// </exception>
    ISkillDurationProvider CreateDurationProvider(
        bool isExecutionMode,
        DateTime? procedureStartTimeUtc,
        IReadOnlyDictionary<Guid, SkillExecutionProgress>? executionProgressData);
}