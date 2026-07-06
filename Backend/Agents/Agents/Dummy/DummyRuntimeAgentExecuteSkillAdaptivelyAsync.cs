using System.Reactive;
using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Agents.Dummy;

/// <summary>
///     Partial class containing the ExecuteSkillAdaptivelyAsync implementation for DummyRuntimeAgent.
///     This is separated for clarity due to the complexity of the pure Rx.NET implementation.
/// </summary>
public partial class DummyRuntimeAgent
{
    public IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId,
        Skill skillToExecute,
        double initialTargetDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        CancellationToken cancellationToken)
    {
        // Use Observable.Defer to delay creation until subscription
        // This ensures each subscription gets its own execution instance
        return Observable.Defer(() =>
        {
            var skillActualStartTimeUtc = DateTime.UtcNow;

            // Track execution start immediately when subscribed
            Interlocked.Increment(ref _activeExecutions);
            UpdateLastSeenUtc();

            // Get execution estimate asynchronously and flatmap into the execution stream
            return Observable.FromAsync(ct => GetExecutionEstimateAsync(skillToExecute, ct))
                .SelectMany(estimate =>
                {
                    // Validate estimate
                    if (estimate is not { CanExecuteAdaptively: true } ||
                        !estimate.MinAdaptiveDuration.HasValue)
                    {
                        // Track failed execution
                        Interlocked.Increment(ref _failedExecutions);
                        Interlocked.Decrement(ref _activeExecutions);
                        UpdateLastSeenUtc();

                        return Observable.Throw<SkillExecutionProgress>(
                            new InvalidOperationException(
                                $"Skill {skillToExecute.Name} cannot be executed adaptively or estimate is missing details."));
                    }

                    var agentSkillMinDuration = estimate.MinAdaptiveDuration.Value;

                    var finishSignalObs = finishSignal
                        .Take(1)
                        .Do(_ => logger.LogFinishSignalReceived(skillToExecute.Name, Name))
                        .Publish()
                        .RefCount();

                    // Bound planned finish times below by the agent's minimum capability;
                    // there is no upper bound — the duration is unbounded above.
                    var clampedTarget = plannedFinishTimes
                        .Select(pf => Math.Max(pf, agentSkillMinDuration))
                        .Do(target => logger.LogUpdatedPlannedFinish(Name, target, skillToExecute.Name))
                        .StartWith(Math.Max(initialTargetDuration, agentSkillMinDuration));

                    // Create the main execution timer — combine with latest target (no mutable shared state).
                    // Completion is driven solely by the finish signal (or cancellation); there is no
                    // upper-duration timeout.
                    var executionStream = Observable
                        .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(_pacingConfig.TimerTickMs))
                        .TakeUntil(finishSignalObs)
                        .WithLatestFrom(clampedTarget, (_, target) =>
                        {
                            var currentTimeIntoExecution = (DateTime.UtcNow - skillActualStartTimeUtc).TotalSeconds;

                            return new SkillExecutionProgress
                            {
                                ExecutionId = executionId,
                                SkillId = skillToExecute.Id,
                                AgentId = Id,
                                ActualStartTimeUtc = skillActualStartTimeUtc,
                                CurrentTimeIntoExecution = currentTimeIntoExecution,
                                EstimatedTotalDuration = target,
                                StatusMessage =
                                    $"Adaptive execution: {currentTimeIntoExecution:F1}s / {target:F1}s (min achievable: {agentSkillMinDuration:F1}s)",
                                CompletedSuccessfully = false,
                                MinAchievableDuration = agentSkillMinDuration
                            };
                        })
                        // Append final completion message. The timer only completes when the finish
                        // signal fires (TakeUntil), so completion is always via the finish signal.
                        .Concat(Observable.Defer(() =>
                        {
                            var currentTime = (DateTime.UtcNow - skillActualStartTimeUtc).TotalSeconds;

                            logger.LogCompletingViaFinishSignal(skillToExecute.Name, Name, currentTime);
                            Interlocked.Increment(ref _totalExecutions);

                            return Observable.Return(new SkillExecutionProgress
                            {
                                ExecutionId = executionId,
                                SkillId = skillToExecute.Id,
                                AgentId = Id,
                                ActualStartTimeUtc = skillActualStartTimeUtc,
                                CurrentTimeIntoExecution = currentTime,
                                EstimatedTotalDuration = currentTime,
                                StatusMessage = "Completed via finish signal",
                                CompletedSuccessfully = true,
                                MinAchievableDuration = agentSkillMinDuration,
                                Outputs = GenerateSkillOutputs(skillToExecute)
                            });
                        }))
                        // Handle cancellation
                        .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                        .Catch((OperationCanceledException ex) =>
                        {
                            if (!cancellationToken.IsCancellationRequested)
                                return Observable.Throw<SkillExecutionProgress>(ex);
                            logger.LogSkillCancelled(skillToExecute.Name, Name);
                            Interlocked.Increment(ref _failedExecutions);

                            return Observable.Return(new SkillExecutionProgress
                            {
                                ExecutionId = executionId,
                                SkillId = skillToExecute.Id,
                                AgentId = Id,
                                ActualStartTimeUtc = skillActualStartTimeUtc,
                                CurrentTimeIntoExecution = (DateTime.UtcNow - skillActualStartTimeUtc).TotalSeconds,
                                EstimatedTotalDuration = 0,
                                StatusMessage = "Execution cancelled",
                                CompletedSuccessfully = false,
                                Error = ex
                            });
                        })
                        // Cleanup on completion/error/disposal
                        .Finally(() =>
                        {
                            Interlocked.Decrement(ref _activeExecutions);
                            UpdateLastSeenUtc();
                        });

                    return executionStream;
                });
        });
    }
}