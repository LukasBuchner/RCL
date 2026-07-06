using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Reactive primitives used by per-execution pipelines that write into persistent,
///     singleton-backed channels. Keeps the "sampled during execution, one guaranteed
///     terminal value" shape in exactly one place so no channel's subscription site
///     repeats the topology.
/// </summary>
public static class PerExecutionRxExtensions
{
    /// <summary>
    ///     Emits <paramref name="source" /> values sampled at <paramref name="interval" /> until
    ///     <paramref name="terminal" /> produces its first value, then emits that single terminal
    ///     value and completes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used to splice a "final" result into each per-execution channel without losing it
    ///         to <c>Sample</c>-dropping. The intermediate phase rate-limits chatter during
    ///         execution; the terminal phase guarantees that the last value a consumer observes
    ///         is the finalised snapshot, regardless of where the last sample tick fell.
    ///     </para>
    ///     <para>
    ///         <paramref name="terminal" /> must be a multicast observable that replays its single
    ///         value to late subscribers (for example <c>Replay(1).AutoConnect(0)</c>), because
    ///         <c>TakeUntil</c> and <c>Concat</c> each subscribe to it independently. When
    ///         <c>TakeUntil</c> fires and unsubscribes, <c>Concat</c> subscribes and must
    ///         immediately receive the replayed value.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">Element type carried by both source and terminal streams.</typeparam>
    /// <param name="source">Intermediate stream of values produced while execution is in progress.</param>
    /// <param name="terminal">Single-value multicast stream that emits exactly once at completion.</param>
    /// <param name="interval">Sampling interval applied to <paramref name="source" /> during the intermediate phase.</param>
    /// <param name="scheduler">
    ///     The scheduler the intermediate <c>Sample</c> runs on. Inject a virtual-time scheduler
    ///     (for example <c>TestScheduler</c>) to make the sampling deterministic in tests.
    /// </param>
    /// <returns>
    ///     An observable that emits zero or more sampled values from <paramref name="source" />,
    ///     then exactly one value from <paramref name="terminal" />, and completes.
    /// </returns>
    public static IObservable<T> SampleUntilTerminal<T>(
        this IObservable<T> source,
        IObservable<T> terminal,
        TimeSpan interval,
        IScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(scheduler);

        return source
            .Sample(interval, scheduler)
            .TakeUntil(terminal)
            .Concat(terminal);
    }
}