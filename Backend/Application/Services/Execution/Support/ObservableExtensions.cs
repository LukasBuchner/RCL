using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FHOOE.Freydis.Application.Services.Execution.Support;

/// <summary>
///     Provides custom Rx.NET operators for the execution pipeline.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    ///     Projects each element of a source sequence into an inner observable, executing all inner
    ///     observables concurrently but only emitting results from the <b>most recently started</b> one.
    ///     Results from older (stale) inner observables are silently dropped.
    ///     <para>
    ///         This ensures that side effects in the subscriber (e.g., publishing node changes to the
    ///         frontend) always reflect the freshest available state. Stale computations may still run
    ///         to completion but their results never reach the observer.
    ///     </para>
    ///     <para>
    ///         The operator uses <see cref="Observer.Synchronize{T}(IObserver{T})" /> to serialize
    ///         emissions, which is required because concurrent inner observables may complete on
    ///         different thread pool threads. The state-tracking lock (<c>gate</c>) is never held
    ///         during emission, preventing deadlocks when the subscriber re-entrantly pushes items
    ///         back into the source.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of elements produced by the inner observables.</typeparam>
    /// <param name="source">The source observable sequence whose elements are projected into inner observables.</param>
    /// <param name="selector">
    ///     A transform function that maps each source element to an inner <see cref="IObservable{TResult}" />.
    ///     Typically wraps an async operation via <c>Observable.FromAsync</c>. The selector must be safe
    ///     for concurrent invocation since multiple inner observables may run in parallel.
    /// </param>
    /// <returns>
    ///     An observable sequence that only forwards results from the most recently started inner
    ///     observable, dropping results from any older concurrent computations.
    /// </returns>
    public static IObservable<TResult> ConcurrentMapLatest<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, IObservable<TResult>> selector)
    {
        return Observable.Create<TResult>(observer =>
        {
            // Synchronize emissions to satisfy the Observable contract (serialized OnNext).
            // This is needed because concurrent inner observables may complete on different threads.
            // The sync lock is separate from `gate` — gate is never held during emission,
            // so re-entrant source pushes (source.onNext → gate) cannot deadlock with emissions.
            var syncObserver = Observer.Synchronize(observer);

            var gate = new object();
            long latestSeq = 0;
            var activeCount = 0;
            var sourceCompleted = false;
            var innerSubscriptions = new CompositeDisposable();

            var sourceSubscription = source.Subscribe(
                item =>
                {
                    long mySeq;
                    lock (gate)
                    {
                        mySeq = ++latestSeq;
                        activeCount++;
                    }

                    var inner = selector(item);
                    var innerSub = new SingleAssignmentDisposable();
                    innerSubscriptions.Add(innerSub);

                    innerSub.Disposable = inner.Subscribe(
                        result =>
                        {
                            bool shouldEmit;
                            lock (gate)
                            {
                                shouldEmit = mySeq == latestSeq;
                            }

                            if (shouldEmit)
                                syncObserver.OnNext(result);
                        },
                        error => { syncObserver.OnError(error); },
                        () =>
                        {
                            bool shouldComplete;
                            lock (gate)
                            {
                                activeCount--;
                                shouldComplete = sourceCompleted && activeCount == 0;
                            }

                            if (shouldComplete)
                                syncObserver.OnCompleted();

                            innerSubscriptions.Remove(innerSub);
                        });
                },
                error => { syncObserver.OnError(error); },
                () =>
                {
                    bool shouldComplete;
                    lock (gate)
                    {
                        sourceCompleted = true;
                        shouldComplete = activeCount == 0;
                    }

                    if (shouldComplete)
                        syncObserver.OnCompleted();
                });

            return new CompositeDisposable(sourceSubscription, innerSubscriptions);
        });
    }
}