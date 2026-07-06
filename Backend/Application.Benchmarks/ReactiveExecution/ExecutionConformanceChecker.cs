using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Checks, per reactive-execution run, that the runtime honored the invariants proved in the paper's
///     verification section, and writes one conformance row per run to a CSV under the BenchmarkDotNet
///     artifacts directory. This is not a timing-prediction study: the human mock is driven under a spread
///     of pacing uncertainties, and the checker verifies the orchestration flow stayed correct under each.
/// </summary>
/// <remarks>
///     <para>
///         A checker is created for one (topology, profile, seed) cell and subscribed to the runtime's
///         public reactive streams <i>before</i> the procedure is started. From the execution event bus
///         (<see cref="ISkillExecutionEventBus.AllEvents" />) it records, per node, the wall-clock timestamp
///         of its first Start, first Finish, and NotSelected events, and it counts the node-change snapshots
///         the outer loop publishes.
///     </para>
///     <para>
///         After the run completes it verifies four invariant classes, each mapped to a proof:
///         <list type="number">
///             <item>
///                 <b>Dependency ordering at runtime</b> (no premature triggering, preserved under
///                 rescheduling): every node fired its Start only after every start-prerequisite's required
///                 event, and its Finish only after every finish-prerequisite's required event. The
///                 prerequisites are the runtime's own resolved structure (<see cref="DependencyGraph" />),
///                 so this is the operational form of the FS, SS, and FF orderings, and by the
///                 scheduling-execution bridge a conforming event order also witnesses that the schedule the
///                 runtime followed honoured those inequalities.
///             </item>
///             <item>
///                 <b>Router branch consistency</b>: no node on a branch the router did not select ever
///                 fired a Start or Finish.
///             </item>
///             <item>
///                 <b>Termination</b>: every reachable skill or router node reached a terminal event
///                 (Finish, or NotSelected for an off-branch node), the runtime form of the measure dropping
///                 to zero.
///             </item>
///             <item>
///                 <b>Adaptive coupling</b>: every adaptive node (one carrying a finish prerequisite)
///                 finished together with its finish-prerequisite source, the empirical form of the robot's
///                 Hold tracking the human's Weld whatever the pacing.
///             </item>
///         </list>
///         A run is <i>conformant</i> when it completed and every violation count is zero.
///     </para>
/// </remarks>
public sealed class ExecutionConformanceChecker : IDisposable
{
    /// <summary>
    ///     Tolerance, in seconds, on the wall-clock ordering of two events. The trigger gate publishes a
    ///     node's event only after its prerequisite event is on the bus, so the ordering is causal; this
    ///     small slack only absorbs timestamp resolution, not a real inversion.
    /// </summary>
    private const double EventOrderToleranceSeconds = 0.005;

    /// <summary>
    ///     Tolerance, in seconds, within which an adaptive node's finish is considered to have coincided
    ///     with its finish-prerequisite source's finish (the FF coupling firing promptly).
    /// </summary>
    private const double CouplingToleranceSeconds = 0.1;

    /// <summary>
    ///     Process-wide registry of the most recently computed result per parameter cell, read by
    ///     <see cref="ExecutionConformanceColumns" /> when the summary table is rendered.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ConformanceResult> Results = new();

    /// <summary>
    ///     Guards the shared CSV file so concurrent or interleaved runs do not corrupt a row.
    /// </summary>
    private static readonly object CsvLock = new();

    /// <summary>
    ///     Absolute path of the conformance CSV for this process, created lazily on the first written row.
    /// </summary>
    private static string? _csvPath;

    private readonly Topology _topology;
    private readonly PacingProfile _pacingProfile;
    private readonly int _seed;

    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _firstStart = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _firstFinish = new();
    private readonly ConcurrentDictionary<Guid, byte> _notSelected = new();

    private int _rescheduleSnapshots;

    private IDisposable? _eventsSubscription;
    private IDisposable? _nodesSubscription;

    /// <summary>
    ///     Initializes a checker for one benchmark cell.
    /// </summary>
    /// <param name="topology">The procedure shape being executed.</param>
    /// <param name="pacingProfile">The human mock's pacing profile (the injected uncertainty).</param>
    /// <param name="seed">The seed governing the mock agents' random draws.</param>
    public ExecutionConformanceChecker(Topology topology, PacingProfile pacingProfile, int seed)
    {
        _topology = topology;
        _pacingProfile = pacingProfile;
        _seed = seed;
    }

    /// <summary>
    ///     Builds the registry key identifying a single (topology, profile, seed) cell, shared with
    ///     <see cref="ExecutionConformanceColumns" /> so a rendered column finds the row the run wrote.
    /// </summary>
    /// <param name="topology">The procedure shape.</param>
    /// <param name="pacingProfile">The pacing profile.</param>
    /// <param name="seed">The seed.</param>
    /// <returns>A stable string key for the cell.</returns>
    public static string BuildKey(Topology topology, PacingProfile pacingProfile, int seed)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{topology}|{pacingProfile}|{seed}");
    }

    /// <summary>
    ///     Attempts to fetch the recorded result for a cell, used by <see cref="ExecutionConformanceColumns" />.
    /// </summary>
    /// <param name="key">The cell key from <see cref="BuildKey" />.</param>
    /// <param name="result">The recorded result when present.</param>
    /// <returns><c>true</c> when a result has been recorded for the key; otherwise <c>false</c>.</returns>
    public static bool TryGetResult(string key, out ConformanceResult result)
    {
        return Results.TryGetValue(key, out result!);
    }

    /// <summary>
    ///     Subscribes to the event and node-change streams. Must be called before the procedure is started so
    ///     no early event or snapshot is missed.
    /// </summary>
    /// <param name="allEvents">The runtime's execution event bus stream.</param>
    /// <param name="nodesChanged">The runtime's node-change stream carrying the planned schedule.</param>
    public void Subscribe(
        IObservable<ExecutionEvent> allEvents,
        IObservable<IReadOnlyList<Node>> nodesChanged)
    {
        _eventsSubscription = allEvents.Subscribe(new DelegatingObserver<ExecutionEvent>(OnEvent));
        _nodesSubscription = nodesChanged.Subscribe(new DelegatingObserver<IReadOnlyList<Node>>(OnNodesChanged));
    }

    /// <summary>
    ///     Computes the conformance result for a completed run, publishes it for the summary columns, and
    ///     appends one CSV row.
    /// </summary>
    /// <param name="nodes">All nodes of the executed procedure.</param>
    /// <param name="graph">The resolved dependency graph (the runtime's prerequisite structure).</param>
    public void WriteConformance(
        IReadOnlyList<Node> nodes,
        DependencyGraph graph)
    {
        var result = Compute(nodes, graph, true, null);
        Publish(result);
        AppendCsvRow(result);
    }

    /// <summary>
    ///     Records a run that did not complete (an initialization failure or a timeout). Failing to reach a
    ///     terminal state is itself a violation of the termination guarantee, so the row is marked
    ///     non-conformant with the supplied reason.
    /// </summary>
    /// <param name="reason">A short machine-readable reason, such as "timeout" or "initialization-failed".</param>
    public void WriteDidNotComplete(string reason)
    {
        var result = new ConformanceResult(
            _topology, _pacingProfile, _seed,
            false, reason,
            0, 0, 0,
            0, 0,
            0, 0,
            Volatile.Read(ref _rescheduleSnapshots),
            false, 0,
            false);
        Publish(result);
        AppendCsvRow(result);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _eventsSubscription?.Dispose();
        _eventsSubscription = null;
        _nodesSubscription?.Dispose();
        _nodesSubscription = null;
    }

    /// <summary>
    ///     Records the first Start, first Finish, and NotSelected event timestamp per node id.
    /// </summary>
    /// <param name="e">The published execution event.</param>
    private void OnEvent(ExecutionEvent e)
    {
        switch (e.EventType)
        {
            case ExecutionEventType.Start:
                _firstStart.TryAdd(e.SkillId, e.Timestamp);
                break;
            case ExecutionEventType.Finish:
                _firstFinish.TryAdd(e.SkillId, e.Timestamp);
                break;
            case ExecutionEventType.NotSelected:
                _notSelected.TryAdd(e.SkillId, 0);
                break;
        }
    }

    /// <summary>
    ///     Counts reschedule snapshots, the outer-loop refinements published during the run.
    /// </summary>
    /// <param name="nodes">The nodes published in this snapshot.</param>
    private void OnNodesChanged(IReadOnlyList<Node> nodes)
    {
        Interlocked.Increment(ref _rescheduleSnapshots);
    }

    /// <summary>
    ///     Computes the conformance result by running the five invariant checks over the captured events and
    ///     the final planned schedule.
    /// </summary>
    /// <param name="nodes">All nodes of the executed procedure.</param>
    /// <param name="graph">The resolved dependency graph.</param>
    /// <param name="completed">Whether the run reached its terminal state.</param>
    /// <param name="reason">The reason a non-completed run is recorded, or null.</param>
    /// <returns>The aggregated conformance result for the run.</returns>
    private ConformanceResult Compute(
        IReadOnlyList<Node> nodes,
        DependencyGraph graph,
        bool completed,
        string? reason)
    {
        // The events and prerequisites are over skill-execution tasks and routers (the proofs' T union R);
        // task containers carry no events and are resolved away in the prerequisite structure.
        var skillRouterIds = nodes
            .Where(n => n is SkillExecutionNode or RouterNode)
            .Select(n => n.Id)
            .ToHashSet();

        // Off-branch nodes are exactly those the runtime marked NotSelected when a router rejected their
        // branch. Using the marking itself, rather than the trigger service's selection map (which the
        // session clears on teardown), keeps the check self-contained and is the operational form of the
        // terminal status: a node is terminal once it has fired Finish or NotSelected.
        var offBranchIds = skillRouterIds.Where(id => _notSelected.ContainsKey(id)).ToHashSet();

        var dependencyConstraints = 0;
        var orderingViolations = 0;

        foreach (var (nodeId, prereqs) in graph.Prerequisites)
        {
            // Runtime dependency ordering: a node that fired Start must have done so after every start
            // prerequisite's required event; likewise its Finish after every finish prerequisite's required
            // event. By the scheduling-execution bridge a conforming event order also witnesses that the
            // schedule the runtime followed honoured the FS, SS, and FF inequalities of the declared model.
            if (!_firstStart.ContainsKey(nodeId))
                continue;

            foreach (var p in prereqs.StartPrerequisites)
            {
                dependencyConstraints++;
                if (!RuntimeOrderHolds(_firstStart[nodeId], p))
                    orderingViolations++;
            }

            if (_firstFinish.TryGetValue(nodeId, out var nodeFinish))
                foreach (var p in prereqs.FinishPrerequisites)
                {
                    dependencyConstraints++;
                    if (!RuntimeOrderHolds(nodeFinish, p))
                        orderingViolations++;
                }
        }

        // Router consistency: no node the router marked off-branch ever fired a Start or Finish.
        var routerOffBranchFirings = offBranchIds.Count(id =>
            _firstStart.ContainsKey(id) || _firstFinish.ContainsKey(id));

        // Termination: every skill or router node reached a terminal event (Finish, or NotSelected for an
        // off-branch node), the runtime form of the measure dropping to zero.
        var unterminatedNodes = skillRouterIds.Count(id =>
            !_firstFinish.ContainsKey(id) && !_notSelected.ContainsKey(id));
        var terminalTransitions = skillRouterIds.Count(id =>
            _firstFinish.ContainsKey(id) || _notSelected.ContainsKey(id));
        var reachableIds = skillRouterIds.Where(id => !offBranchIds.Contains(id)).ToHashSet();

        // Adaptive coupling: every adaptive node finished together with its finish-prerequisite source.
        var (couplingHeld, maxCouplingGap) = CheckAdaptiveCoupling(graph);

        var conformant = completed
                         && orderingViolations == 0
                         && routerOffBranchFirings == 0
                         && unterminatedNodes == 0;

        return new ConformanceResult(
            _topology, _pacingProfile, _seed,
            completed,
            reason,
            skillRouterIds.Count,
            reachableIds.Count,
            dependencyConstraints,
            orderingViolations,
            routerOffBranchFirings,
            unterminatedNodes,
            terminalTransitions,
            Volatile.Read(ref _rescheduleSnapshots),
            couplingHeld,
            maxCouplingGap,
            conformant);
    }

    /// <summary>
    ///     Determines whether a node's event at <paramref name="nodeEventTime" /> occurred no earlier than the
    ///     prerequisite source's required event, within the ordering tolerance.
    /// </summary>
    /// <param name="nodeEventTime">The timestamp of the node's own event.</param>
    /// <param name="prerequisite">The prerequisite naming the source node and its required event.</param>
    /// <returns>
    ///     <c>true</c> when the source's required event was observed and preceded the node's event; otherwise
    ///     <c>false</c> (a premature firing, or the prerequisite event never occurred).
    /// </returns>
    private bool RuntimeOrderHolds(DateTimeOffset nodeEventTime, EventPrerequisite prerequisite)
    {
        var sourceTimes = prerequisite.RequiredEventType == EventTriggerType.Start ? _firstStart : _firstFinish;
        if (!sourceTimes.TryGetValue(prerequisite.DependencySkillId, out var sourceTime))
            return false;

        return (nodeEventTime - sourceTime).TotalSeconds >= -EventOrderToleranceSeconds;
    }

    /// <summary>
    ///     Checks that every adaptive node (one carrying a finish prerequisite) finished together with its
    ///     finish-prerequisite source, the empirical form of the adaptive coupling absorbing the partner's
    ///     pacing.
    /// </summary>
    /// <param name="graph">The resolved dependency graph.</param>
    /// <returns>
    ///     A pair of whether every observed adaptive coupling coincided within tolerance, and the worst
    ///     observed gap in seconds.
    /// </returns>
    private (bool Held, double MaxGapSeconds) CheckAdaptiveCoupling(DependencyGraph graph)
    {
        var held = true;
        var maxGap = 0.0;
        var observedAny = false;

        foreach (var (nodeId, prereqs) in graph.Prerequisites)
        {
            if (prereqs.FinishPrerequisites.Count == 0)
                continue;
            if (!_firstFinish.TryGetValue(nodeId, out var nodeFinish))
                continue;

            foreach (var p in prereqs.FinishPrerequisites)
            {
                var sourceTimes = p.RequiredEventType == EventTriggerType.Start ? _firstStart : _firstFinish;
                if (!sourceTimes.TryGetValue(p.DependencySkillId, out var sourceTime))
                    continue;

                observedAny = true;
                var gap = Math.Abs((nodeFinish - sourceTime).TotalSeconds);
                if (gap > maxGap)
                    maxGap = gap;
                if (gap > CouplingToleranceSeconds)
                    held = false;
            }
        }

        // A run with no adaptive finish coupling (the FS control) trivially holds.
        return (observedAny ? held : true, maxGap);
    }

    /// <summary>
    ///     Publishes a result into the process-wide registry so the summary columns can read it.
    /// </summary>
    /// <param name="result">The computed result.</param>
    private static void Publish(ConformanceResult result)
    {
        Results[BuildKey(result.Topology, result.PacingProfile, result.Seed)] = result;
    }

    /// <summary>
    ///     Appends one row for the run to the process's conformance CSV under the BenchmarkDotNet artifacts
    ///     directory, creating the file with a header on the first write.
    /// </summary>
    /// <param name="result">The computed result to serialize.</param>
    private static void AppendCsvRow(ConformanceResult result)
    {
        lock (CsvLock)
        {
            var path = EnsureCsvFile();
            var row = string.Join(",", new[]
            {
                Csv(result.Topology.ToString()),
                Csv(result.PacingProfile.ToString()),
                Num(result.Seed),
                result.Completed ? "1" : "0",
                Csv(result.DidNotCompleteReason ?? string.Empty),
                Num(result.NodeCount),
                Num(result.ReachableNodeCount),
                Num(result.DependencyConstraints),
                Num(result.OrderingViolations),
                Num(result.RouterOffBranchFirings),
                Num(result.UnterminatedNodes),
                Num(result.TerminalTransitions),
                Num(result.RescheduleSnapshots),
                result.AdaptiveCouplingHeld ? "1" : "0",
                Num(result.MaxCouplingGapSeconds),
                result.Conformant ? "1" : "0"
            });

            File.AppendAllText(path, row + Environment.NewLine, Encoding.UTF8);
        }
    }

    /// <summary>
    ///     Resolves the per-process CSV path under <c>BenchmarkDotNet.Artifacts</c>, creating the directory
    ///     and writing the header row the first time it is requested.
    /// </summary>
    /// <returns>The absolute path of the conformance CSV.</returns>
    private static string EnsureCsvFile()
    {
        if (_csvPath is not null)
            return _csvPath;

        var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts");
        Directory.CreateDirectory(artifactsDir);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Combine(artifactsDir, $"conformance-{stamp}.csv");

        const string header =
            "Topology,PacingProfile,Seed,Completed,DidNotCompleteReason,NodeCount,ReachableNodeCount," +
            "DependencyConstraints,OrderingViolations,RouterOffBranchFirings," +
            "UnterminatedNodes,TerminalTransitions,RescheduleSnapshots,AdaptiveCouplingHeld," +
            "MaxCouplingGapSeconds,Conformant";

        File.WriteAllText(path, header + Environment.NewLine, Encoding.UTF8);
        _csvPath = path;
        return path;
    }

    /// <summary>Formats a floating-point value with invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture string representation.</returns>
    private static string Num(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats an integer value with invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture string representation.</returns>
    private static string Num(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Escapes a string field for CSV.</summary>
    /// <param name="value">The field value.</param>
    /// <returns>The CSV-safe representation.</returns>
    private static string Csv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    ///     A minimal <see cref="IObserver{T}" /> that forwards <c>OnNext</c> to a delegate and ignores
    ///     completion and error notifications, so the checker depends only on the BCL observable contract.
    /// </summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class DelegatingObserver<T>(Action<T> onNext) : IObserver<T>
    {
        /// <summary>Forwards the next value to the supplied delegate.</summary>
        /// <param name="value">The published value.</param>
        public void OnNext(T value)
        {
            onNext(value);
        }

        /// <summary>Ignores error notifications.</summary>
        /// <param name="error">The error from the source observable.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>Ignores completion.</summary>
        public void OnCompleted()
        {
        }
    }
}

/// <summary>
///     The per-run runtime-conformance result for one reactive-execution run, written as a single CSV row
///     and surfaced in the BenchmarkDotNet summary by <see cref="ExecutionConformanceColumns" />.
/// </summary>
/// <param name="Topology">The procedure shape executed.</param>
/// <param name="PacingProfile">The human mock's pacing profile (the injected uncertainty).</param>
/// <param name="Seed">The seed governing the mock agents' random draws.</param>
/// <param name="Completed">Whether the run reached a state in which all reachable nodes were terminal.</param>
/// <param name="DidNotCompleteReason">The reason a non-completed run was recorded, or null.</param>
/// <param name="NodeCount">Number of skill and router nodes (the proofs' set of tasks and routers).</param>
/// <param name="ReachableNodeCount">Number of those nodes on a selected branch (expected to run).</param>
/// <param name="DependencyConstraints">Number of runtime start/finish prerequisites checked.</param>
/// <param name="OrderingViolations">Runtime premature-trigger or ordering violations (expected zero).</param>
/// <param name="RouterOffBranchFirings">Off-branch nodes that fired a Start or Finish (expected zero).</param>
/// <param name="UnterminatedNodes">Reachable nodes that never reached a Finish (expected zero).</param>
/// <param name="TerminalTransitions">Nodes that reached a terminal event (Finish or NotSelected).</param>
/// <param name="RescheduleSnapshots">Distinct node-change snapshots (outer-loop refinements, context).</param>
/// <param name="AdaptiveCouplingHeld">Whether every adaptive node finished together with its partner.</param>
/// <param name="MaxCouplingGapSeconds">Worst observed gap between an adaptive finish and its partner's.</param>
/// <param name="Conformant">Whether the run completed and every violation count is zero.</param>
public readonly record struct ConformanceResult(
    Topology Topology,
    PacingProfile PacingProfile,
    int Seed,
    bool Completed,
    string? DidNotCompleteReason,
    int NodeCount,
    int ReachableNodeCount,
    int DependencyConstraints,
    int OrderingViolations,
    int RouterOffBranchFirings,
    int UnterminatedNodes,
    int TerminalTransitions,
    int RescheduleSnapshots,
    bool AdaptiveCouplingHeld,
    double MaxCouplingGapSeconds,
    bool Conformant);