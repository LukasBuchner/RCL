namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Publishes aggregated execution timing information for real-time
///     consumption by GraphQL subscriptions.
/// </summary>
public interface IExecutionTimingPublisher
{
    /// <summary>
    ///     Observable stream of timing updates, suitable for GraphQL subscription binding.
    /// </summary>
    IObservable<ExecutionTimingInfo> TimingUpdates { get; }

    /// <summary>
    ///     <see cref="IObserver{T}" /> surface that forwards <c>OnNext</c> to
    ///     <see cref="PublishTiming" /> and logs-then-swallows <c>OnError</c>. Its
    ///     <c>OnCompleted</c> is a deliberate no-op so that a per-execution source observable
    ///     completing does not tear down the singleton <see cref="TimingUpdates" /> channel.
    ///     Consumers subscribed before the current execution continue to receive values from
    ///     subsequent executions without re-subscribing.
    /// </summary>
    IObserver<ExecutionTimingInfo> TimingObserver { get; }

    /// <summary>
    ///     Publishes a new timing snapshot to all subscribers.
    /// </summary>
    void PublishTiming(ExecutionTimingInfo timing);
}