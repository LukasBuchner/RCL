using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Factory for creating appropriate duration providers based on execution mode.
/// </summary>
public class DurationProviderFactory : IDurationProviderFactory
{
    private readonly ILogger<ExecutionAwareDurationProvider> _executionProviderLogger;
    private readonly PlanningModeDurationProvider _planningModeProvider;

    /// <summary>
    ///     Initializes a new instance of <see cref="DurationProviderFactory" />.
    /// </summary>
    /// <param name="planningModeProvider">The planning mode duration provider.</param>
    /// <param name="executionProviderLogger">Logger for execution-aware duration provider.</param>
    public DurationProviderFactory(
        PlanningModeDurationProvider planningModeProvider,
        ILogger<ExecutionAwareDurationProvider> executionProviderLogger)
    {
        _planningModeProvider = planningModeProvider ?? throw new ArgumentNullException(nameof(planningModeProvider));
        _executionProviderLogger = executionProviderLogger ??
                                   throw new ArgumentNullException(nameof(executionProviderLogger));
    }

    /// <inheritdoc />
    public ISkillDurationProvider CreateDurationProvider(
        bool isExecutionMode,
        DateTime? procedureStartTimeUtc,
        IReadOnlyDictionary<Guid, SkillExecutionProgress>? executionProgressData)
    {
        if (!isExecutionMode)
            return _planningModeProvider;

        if (!procedureStartTimeUtc.HasValue)
            throw new ArgumentNullException(nameof(procedureStartTimeUtc),
                "Procedure start time is required in execution mode.");

        if (executionProgressData == null)
            throw new ArgumentNullException(nameof(executionProgressData),
                "Execution progress data is required in execution mode.");

        return new ExecutionAwareDurationProvider(
            _planningModeProvider,
            procedureStartTimeUtc.Value,
            executionProgressData,
            _executionProviderLogger);
    }
}