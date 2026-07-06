using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Agents.Dummy;

/// <summary>
///     Partial class containing the ExecuteSkillAsync method implementation.
///     This method executes a skill with fixed duration using proper Rx.NET patterns.
/// </summary>
public partial class DummyRuntimeAgent
{
    /// <summary>
    ///     Executes a skill with fixed duration and reports progress.
    ///     Uses proper Rx.NET patterns (Defer + FromAsync + Timer) for reactive execution.
    /// </summary>
    /// <param name="executionId">The unique identifier for this execution instance.</param>
    /// <param name="skillToExecute">The skill to execute.</param>
    /// <param name="cancellationToken">Token for cancelling the execution (used for errors/abort).</param>
    /// <returns>An observable stream of execution progress updates.</returns>
    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid executionId,
        Skill skillToExecute,
        CancellationToken cancellationToken)
    {
        return Observable.Defer(() =>
        {
            // Defer creation until subscription - proper Rx.NET pattern
            return Observable.FromAsync(async ct =>
                {
                    // Async initialization phase
                    var skillActualStartTimeUtc = DateTime.UtcNow;

                    // Track execution start
                    Interlocked.Increment(ref _activeExecutions);
                    UpdateLastSeenUtc();

                    var estimate = await GetExecutionEstimateAsync(skillToExecute, ct);
                    var nominalDuration = estimate?.EstimatedNominalDuration ?? 10.0;

                    if (estimate is null)
                        logger.LogExecuteFallbackNominalDuration(Name, skillToExecute.Name, skillToExecute.Id,
                            nominalDuration);

                    // Apply symmetric jitter to simulate real-world execution variability.
                    // The jitter fraction is configurable; its default is ±15%.
                    var jitter = _pacingConfig.DurationJitter;
                    nominalDuration *= 1.0 + (_random.NextDouble() * (2.0 * jitter) - jitter);

                    return new
                    {
                        skillActualStartTimeUtc,
                        nominalDuration,
                        executionId,
                        skillId = skillToExecute.Id
                    };
                })
                .SelectMany(context =>
                {
                    // Create the observable stream using Timer (proper Rx.NET operator)
                    var progressStream = Observable
                        .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(_pacingConfig.TimerTickMs))
                        .TakeWhile(_ =>
                        {
                            // Continue until we reach the nominal duration
                            var elapsed = (DateTime.UtcNow - context.skillActualStartTimeUtc).TotalSeconds;
                            return elapsed < context.nominalDuration;
                        })
                        .Select(_ =>
                        {
                            var elapsed = (DateTime.UtcNow - context.skillActualStartTimeUtc).TotalSeconds;
                            var isCompleted = elapsed >= context.nominalDuration;

                            // Simulate estimate refinement: early in execution the ETA
                            // oscillates around the true duration, then converges as the
                            // skill nears completion — mimicking a real agent that revises
                            // its time estimate based on observed progress.
                            var completionFraction = elapsed / context.nominalDuration;
                            var noiseAmplitude = (1.0 - completionFraction) * _pacingConfig.EstimateNoiseAmplitude;
                            var noise = Math.Sin(elapsed * _pacingConfig.EstimateSinusoidFrequency) * noiseAmplitude;
                            var currentEstimate = context.nominalDuration * (1.0 + noise);

                            return new SkillExecutionProgress
                            {
                                ExecutionId = context.executionId,
                                SkillId = context.skillId,
                                AgentId = Id,
                                ActualStartTimeUtc = context.skillActualStartTimeUtc,
                                CurrentTimeIntoExecution = elapsed,
                                EstimatedTotalDuration = currentEstimate,
                                StatusMessage =
                                    $"Executing. Step {(int)elapsed}/{(int)context.nominalDuration}. Est. total: {currentEstimate:F1}s",
                                CompletedSuccessfully = isCompleted
                            };
                        });

                    // Emit final completion event with outputs
                    var completionStream = progressStream
                        .Concat(Observable.Return(new SkillExecutionProgress
                        {
                            ExecutionId = context.executionId,
                            SkillId = context.skillId,
                            AgentId = Id,
                            ActualStartTimeUtc = context.skillActualStartTimeUtc,
                            CurrentTimeIntoExecution = context.nominalDuration,
                            EstimatedTotalDuration = context.nominalDuration,
                            StatusMessage = $"Completed after {context.nominalDuration:F1}s",
                            CompletedSuccessfully = true,
                            Outputs = GenerateSkillOutputs(skillToExecute)
                        }));

                    return completionStream;
                })
                .Finally(() =>
                {
                    // Cleanup when observable completes or is disposed
                    Interlocked.Decrement(ref _activeExecutions);
                    Interlocked.Increment(ref _totalExecutions);
                    UpdateLastSeenUtc();
                })
                .TakeUntil(Observable.Create<SkillExecutionProgress>(obs =>
                {
                    // Convert cancellation token to observable for proper reactive cancellation
                    var registration = cancellationToken.Register(() =>
                    {
                        // On cancellation, emit error progress and complete
                        Interlocked.Increment(ref _failedExecutions);
                        obs.OnNext(new SkillExecutionProgress
                        {
                            ExecutionId = executionId,
                            SkillId = skillToExecute.Id,
                            AgentId = Id,
                            ActualStartTimeUtc = DateTime.UtcNow,
                            CurrentTimeIntoExecution = 0,
                            EstimatedTotalDuration = 0,
                            StatusMessage = "Execution cancelled",
                            Error = new TaskCanceledException()
                        });
                        obs.OnCompleted();
                    });

                    return registration;
                }));
        });
    }
}