using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     BehaviorSubject-backed publisher that exposes an observable stream of
///     <see cref="ExecutionTimingInfo" /> snapshots for GraphQL subscriptions.
/// </summary>
public class ExecutionTimingPublisher : IExecutionTimingPublisher, IDisposable
{
    private readonly BehaviorSubject<ExecutionTimingInfo> _subject = new(
        new ExecutionTimingInfo
        {
            StartTimeUtc = DateTimeOffset.MinValue,
            CurrentTimeSeconds = 0,
            EstimatedTotalDurationSeconds = 0,
            ProgressPercentage = 0,
            IsRunning = false
        });

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionTimingPublisher" /> class.
    /// </summary>
    /// <param name="logger">Logger for publish failures surfaced through the observer surface.</param>
    public ExecutionTimingPublisher(ILogger<ExecutionTimingPublisher> logger)
    {
        var logger1 = logger ?? throw new ArgumentNullException(nameof(logger));

        TimingObserver = Observer.Create<ExecutionTimingInfo>(
            PublishTiming,
            ex => logger1.LogTimingPublishFailed(ex),
            () =>
            {
                /* singleton channel stays hot across executions */
            });
    }

    /// <summary>
    ///     Releases all resources used by the publisher.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subject.Dispose();
    }

    /// <inheritdoc />
    public IObservable<ExecutionTimingInfo> TimingUpdates => _subject.AsObservable();

    /// <inheritdoc />
    public IObserver<ExecutionTimingInfo> TimingObserver { get; }

    /// <inheritdoc />
    public void PublishTiming(ExecutionTimingInfo timing)
    {
        _subject.OnNext(timing);
    }
}