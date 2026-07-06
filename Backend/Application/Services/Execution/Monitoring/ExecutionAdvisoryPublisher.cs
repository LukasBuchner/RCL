using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Subject-backed publisher that exposes an observable stream of <see cref="ExecutionAdvisory" />
///     events for GraphQL subscriptions. Advisories are transient events, so a plain
///     <see cref="Subject{T}" /> is used (new subscribers do not receive past advisories).
/// </summary>
public class ExecutionAdvisoryPublisher : IExecutionAdvisoryPublisher, IDisposable
{
    private readonly Subject<ExecutionAdvisory> _subject = new();

    /// <summary>
    ///     Releases all resources used by the publisher.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subject.Dispose();
    }

    /// <inheritdoc />
    public IObservable<ExecutionAdvisory> Advisories => _subject.AsObservable();

    /// <inheritdoc />
    public void Publish(ExecutionAdvisory advisory)
    {
        _subject.OnNext(advisory);
    }
}