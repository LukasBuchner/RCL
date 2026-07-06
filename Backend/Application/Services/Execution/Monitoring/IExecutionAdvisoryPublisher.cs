namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Publishes operator-facing execution advisories for real-time consumption by GraphQL
///     subscriptions. This is an observability side channel: nothing in the scheduling,
///     triggering, or orchestration layers consumes it.
/// </summary>
public interface IExecutionAdvisoryPublisher
{
    /// <summary>
    ///     Observable stream of advisories, suitable for GraphQL subscription binding.
    /// </summary>
    IObservable<ExecutionAdvisory> Advisories { get; }

    /// <summary>
    ///     Publishes a new advisory to all subscribers.
    /// </summary>
    /// <param name="advisory">The advisory to publish.</param>
    void Publish(ExecutionAdvisory advisory);
}