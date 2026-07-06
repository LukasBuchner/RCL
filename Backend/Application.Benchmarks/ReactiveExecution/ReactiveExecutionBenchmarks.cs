using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     The procedure shapes driven by <see cref="ReactiveExecutionBenchmarks" />. Each value selects a
///     distinct RCL workflow that <see cref="ProcedureBuilder" /> materializes into Domain entities plus
///     the mock <see cref="DummyRuntimeAgent" />s that run it. The shapes range from the minimal
///     adaptive convergence unit up to multi-variable and pipelined topologies so that convergence can be
///     characterized across structure, not just on the single live trace.
/// </summary>
public enum Topology
{
    /// <summary>
    ///     A pure Finish-to-Start chain with no adaptive skill: convergence is trivially exact and
    ///     reschedules are minimal. Used as the safest first smoke (no adaptive-hang risk) and to
    ///     isolate pipeline cost absent coupling.
    /// </summary>
    FsControl,

    /// <summary>
    ///     The minimal convergence unit: a compound <c>Welding{Hold(adaptive), Weld}</c> core coupled by
    ///     <c>SS(Hold,Weld)</c> + <c>FF(Weld,Hold)</c>, run by one robot mock and one human mock. Reproduces
    ///     the live §7.2 "Hold-Weld portion" headlessly.
    /// </summary>
    HoldWeld,

    /// <summary>
    ///     The paper's running example: the coupled <c>Welding</c> core, an <c>FS(Welding,Inspect)</c>
    ///     successor binding a <c>quality</c> variable, and a <c>Sort</c> router branching to
    ///     <c>Store{Shelve}</c> / <c>Discard{Scrap}</c>. Confirms convergence holds with a downstream FS
    ///     successor and a router branch present.
    /// </summary>
    HoldWeldInspectSort,

    /// <summary>
    ///     Two independent <c>Hold-Weld</c> pairs executing concurrently, stressing the reschedule loop
    ///     with two adaptive variables converging at once.
    /// </summary>
    ParallelSeams2,

    /// <summary>
    ///     Four independent <c>Hold-Weld</c> pairs executing concurrently, stressing the reschedule loop
    ///     with four adaptive variables converging at once.
    /// </summary>
    ParallelSeams4,

    /// <summary>
    ///     Two <c>Hold-Weld</c> pairs in series, FS-chained, testing that convergence propagates down a
    ///     chain of coupled cores (cumulative drift).
    /// </summary>
    SequentialPipeline2,

    /// <summary>
    ///     Four <c>Hold-Weld</c> pairs in series, FS-chained, testing that convergence propagates down a
    ///     longer chain of coupled cores (cumulative drift).
    /// </summary>
    SequentialPipeline4
}

/// <summary>
///     How the human mock paces its <c>Weld</c> skill. Each profile is mapped by
///     <see cref="ReactiveExecutionBenchmarks.ToPacingConfig" /> onto a
///     <see cref="DummyRuntimeAgentPacingConfig" /> that shapes the jitter and noise the adaptive
///     <c>Hold</c> must converge to. The profiles let the benchmark show convergence is robust to
///     <i>how</i> the human paces, not only to the live simulator's ±15 % draw.
/// </summary>
public enum PacingProfile
{
    /// <summary>
    ///     The live simulator's behavior: a ±15 % duration draw plus a small decaying sinusoid. The gap
    ///     the loop must close is small.
    /// </summary>
    Default,

    /// <summary>
    ///     A wider, upward-skewed perturbation standing in for a human who finishes later than nominal;
    ///     the loop must stretch the adaptive plan to a larger actual finish.
    /// </summary>
    Slow,

    /// <summary>
    ///     A wider, downward-skewed perturbation standing in for a human who finishes earlier than
    ///     nominal; the loop must pull the adaptive plan in to a smaller actual finish.
    /// </summary>
    Early,

    /// <summary>
    ///     High jitter and noise with little net bias, stressing how smoothly the plan tracks a noisy
    ///     partner.
    /// </summary>
    Noisy
}

/// <summary>
///     BenchmarkDotNet configuration for the reactive-execution convergence benchmark. The run drives the
///     full event-driven execution runtime on real wall-clock time (no virtual-time path), so the engine
///     uses <see cref="RunStrategy.Monitoring" /> with no warmup and a single invocation per iteration: the
///     fresh provider, procedure, and agents are rebuilt in <c>[IterationSetup]</c> and each
///     <c>[Benchmark]</c> body runs one procedure to completion. The BenchmarkDotNet timing column is
///     incidental; the convergence story is carried by the <see cref="ConvergenceColumns" /> and the CSV
///     row written per (topology, profile, seed).
/// </summary>
public class ReactiveExecutionBenchmarkConfig : ManualConfig
{
    /// <summary>
    ///     Initializes the configuration: a monitoring job (no warmup, one invocation per iteration, a small
    ///     iteration count), the convergence columns, a GitHub-markdown exporter, and an invariant-culture
    ///     CSV exporter for downstream analysis.
    /// </summary>
    public ReactiveExecutionBenchmarkConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithStrategy(RunStrategy.Monitoring)
            .WithWarmupCount(0)
            .WithIterationCount(3)
            .WithLaunchCount(1)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithId("ReactiveExecution"));

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(ExecutionConformanceColumns.All);

        AddExporter(MarkdownExporter.GitHub);

        var csvCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        csvCulture.NumberFormat.NumberGroupSeparator = string.Empty;

        var csvStyle = new SummaryStyle(
            csvCulture,
            true,
            printUnitsInContent: false,
            sizeUnit: SizeUnit.KB,
            timeUnit: TimeUnit.Millisecond,
            ratioStyle: RatioStyle.Trend);
        AddExporter(new CsvExporter(CsvSeparator.Comma, csvStyle));

        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared));
    }
}

/// <summary>
///     Headless runtime-conformance benchmark for the dual-loop execution runtime. The human mock is driven
///     under a spread of pacing uncertainties, and for every combination of <see cref="Topology" />,
///     <see cref="PacingProfile" />, and <see cref="Seed" /> it builds a fresh in-memory runtime (DI
///     replicated by <see cref="BenchmarkExecutionServices.AddReactiveExecutionRuntime" />), materializes the
///     RCL procedure and its mock agents via <see cref="ProcedureBuilder" />, runs the procedure to completion
///     through <see cref="IExecutionOrchestrator.StartLoadedProcedureAsync" />, and checks that the runtime
///     honoured the invariants proved in the verification section (dependency ordering, router consistency,
///     termination, and the adaptive coupling). Each run emits one conformance CSV row through
///     <see cref="ExecutionConformanceChecker" />; a timeout guard records a non-conformant row rather than
///     hang on a malformed graph.
/// </summary>
/// <remarks>
///     <para>
///         The runtime executes on real wall-clock time, so the seed governs only the random draws (jitter,
///         noise) of the mock agents, not absolute timing; a fresh <see cref="IServiceProvider" />, procedure,
///         and agent set are rebuilt for every iteration to keep runs independent. Skills carry small
///         <c>NominalDuration</c>s so a run completes in seconds.
///     </para>
///     <para>
///         This is the third Application-layer benchmark; <c>EndToEndPipelineBenchmarks</c> (scheduling
///         latency) and the separate <c>Scheduling.Benchmarks</c> project (SCC vs monolithic) are unchanged.
///         All three are discoverable through the <c>BenchmarkSwitcher</c> wired up in <c>Program.cs</c>;
///         this class is selected with <c>--filter '*ReactiveExecution*'</c>.
///     </para>
/// </remarks>
[Config(typeof(ReactiveExecutionBenchmarkConfig))]
public class ReactiveExecutionBenchmarks
{
    /// <summary>
    ///     Upper bound on how long a single run may take before it is recorded as non-conformant (a failure
    ///     of the termination guarantee). A malformed graph never fires its finish signal, so the body wraps
    ///     the completion await in a <see cref="Task.WhenAny(Task[])" /> against this delay rather than hanging
    ///     the whole benchmark.
    /// </summary>
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     Grace period awaited after the run's completion task resolves, before the conformance snapshot is
    ///     taken, so that any last in-flight execution event reaches the checker's bus subscription.
    /// </summary>
    private static readonly TimeSpan EventDrainGrace = TimeSpan.FromMilliseconds(250);

    private ServiceProvider _provider = null!;
    private InMemoryProcedureRepository _repository = null!;
    private BenchmarkAgentManager _agentManager = null!;
    private ExecutionOrchestrator _orchestrator = null!;
    private Domain.Entities.Procedure.Procedure _procedure = null!;
    private ExecutionConformanceChecker _checker = null!;

    /// <summary>
    ///     The procedure shape to build and execute. Each value drives a distinct RCL workflow with its own
    ///     robot/human mock role split.
    /// </summary>
    [Params(
        Topology.FsControl,
        Topology.HoldWeld,
        Topology.HoldWeldInspectSort,
        Topology.ParallelSeams2,
        Topology.ParallelSeams4,
        Topology.SequentialPipeline2,
        Topology.SequentialPipeline4)]
    public Topology Topology { get; set; }

    /// <summary>
    ///     How the human mock paces its <c>Weld</c> skill: the injected uncertainty the flow must stay correct
    ///     under (the live-simulator draw, plus wider envelopes for finishing later, earlier, or noisier).
    /// </summary>
    [Params(
        PacingProfile.Default,
        PacingProfile.Slow,
        PacingProfile.Early,
        PacingProfile.Noisy)]
    public PacingProfile PacingProfile { get; set; }

    /// <summary>
    ///     The seed for the mock agents' random draws. Varying the seed repeats each uncertainty profile over
    ///     independent draws, so conformance is checked across many runs per (topology, profile).
    /// </summary>
    [Params(1, 2, 3, 4, 5)]
    public int Seed { get; set; }

    /// <summary>
    ///     Builds a fresh runtime for one iteration: the in-memory repository and benchmark agent manager, a
    ///     <see cref="ServiceProvider" /> wired by
    ///     <see cref="BenchmarkExecutionServices.AddReactiveExecutionRuntime" /> over those seams, the
    ///     procedure and mock agents for the current <see cref="Topology" /> paced by the current
    ///     <see cref="PacingProfile" /> and <see cref="Seed" />, the procedure seeded into the repository,
    ///     and every agent registered with the manager. The procedure is then loaded through the real
    ///     <see cref="IProcedureOrchestrator.LoadProcedureAsync" /> so the production
    ///     <c>ProcedureContext</c> and the <c>ProcedureStateTracker</c> that backs the runtime's node-change
    ///     stream are scoped to it exactly as in production. Rebuilt every iteration so runs stay independent.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        // The execution-runtime seams are constructed here and handed to the DI replication helper, which
        // registers them last so they override the production registrations. They are held as fields so the
        // body can seed the procedure and register agents on the same instances the runtime resolves.
        _repository = new InMemoryProcedureRepository();
        _agentManager = new BenchmarkAgentManager();

        _provider = (ServiceProvider)new ServiceCollection()
            .AddReactiveExecutionRuntime(_repository, _agentManager)
            .BuildServiceProvider();

        var pacing = ToPacingConfig(PacingProfile, Seed);
        var (procedure, agents) = BuildProcedure(Topology, pacing);
        _procedure = procedure;

        _repository.SetProcedure(procedure, procedure.Nodes ?? [], procedure.Edges ?? []);

        foreach (var agent in agents)
            _agentManager.RegisterAgent(agent);

        // Load through the real orchestrator (not a context mock): this marks the procedure loaded, points
        // the production ProcedureContext at it, and triggers the ProcedureStateTracker to scope its
        // node/edge streams to this procedure — the gate that lets the runtime's NodesChanged updates flow.
        _provider.GetRequiredService<IProcedureOrchestrator>()
            .LoadProcedureAsync(procedure.Id)
            .GetAwaiter()
            .GetResult();

        _orchestrator = _provider.GetRequiredService<ExecutionOrchestrator>();
    }

    /// <summary>
    ///     Tears down the per-iteration runtime, disposing the conformance checker's subscriptions and the
    ///     provider so the singleton execution services and their reactive streams are released before the
    ///     next iteration rebuilds them.
    /// </summary>
    [IterationCleanup]
    public void IterationCleanup()
    {
        _checker?.Dispose();
        _provider?.Dispose();
    }

    /// <summary>
    ///     Runs one procedure to completion and records its runtime-conformance row. Subscribes the
    ///     <see cref="ExecutionConformanceChecker" /> to the execution event bus and the node-change stream
    ///     <i>before</i> starting so no early event is missed, starts the loaded procedure, then awaits the
    ///     detached run guarded by a timeout. On timeout the checker writes a non-conformant row (a
    ///     termination-guarantee violation); otherwise it verifies, from the captured events and the final
    ///     schedule, that the runtime honoured the dependency ordering, router consistency, termination, and
    ///     schedule-feasibility invariants proved in the verification section.
    /// </summary>
    /// <returns>A task that completes once the run has finished (or timed out) and its row has been written.</returns>
    [Benchmark(Description = "Conformance")]
    public async Task Conformance()
    {
        var eventPublisher = _provider.GetRequiredService<IExecutionEventPublisher>();
        var eventBus = _provider.GetRequiredService<ISkillExecutionEventBus>();

        _checker = new ExecutionConformanceChecker(Topology, PacingProfile, Seed);
        _checker.Subscribe(eventBus.AllEvents, eventPublisher.NodesChanged);

        var started = await _orchestrator.StartLoadedProcedureAsync();
        if (!started)
        {
            _checker.WriteDidNotComplete("initialization-failed");
            return;
        }

        var completed = await Task.WhenAny(_orchestrator.CurrentExecution, Task.Delay(ExecutionTimeout));
        if (completed != _orchestrator.CurrentExecution)
        {
            _checker.WriteDidNotComplete("timeout");
            return;
        }

        // Surface any fault from the detached run; the orchestrator logs and swallows run-time faults, so a
        // faulted CurrentExecution is unexpected, but observing it here keeps the row honest.
        await _orchestrator.CurrentExecution;

        // The completion task resolves on a thread-pool thread (the orchestrator bridges completion through
        // ObserveOn(TaskPoolScheduler)), so a last in-flight event (notably the router's Finish, published
        // after its branch completes) can still be on its way to the event-bus subscribers when the await
        // returns. A short grace lets those events drain before the conformance snapshot is taken, so the
        // check reflects the run's terminal state rather than a transient mid-delivery view.
        await Task.Delay(EventDrainGrace);

        // Verify the run against the invariants proved in the verification section: build the resolved
        // dependency graph (the runtime's own prerequisite structure) and check the captured events and the
        // final schedule conform. Off-branch nodes are identified from their NotSelected events, so the
        // check does not depend on the trigger service's selection map, which the session clears on teardown.
        var nodes = _procedure.Nodes ?? [];
        var edges = _procedure.Edges ?? [];
        var graph = _provider
            .GetRequiredService<IDependencyGraphAnalyzer>()
            .AnalyzeDependencies(nodes, edges);

        _checker.WriteConformance(nodes, graph);
    }

    /// <summary>
    ///     Dispatches to the <see cref="ProcedureBuilder" /> method that materializes the requested
    ///     <see cref="Topology" />, passing the human mock's pacing configuration and the size parameter the
    ///     parametric shapes (parallel seams, sequential pipeline) require.
    /// </summary>
    /// <param name="topology">The shape to build.</param>
    /// <param name="humanPacing">The pacing configuration applied to the human mock(s).</param>
    /// <returns>The built procedure together with the mock agents that run it.</returns>
    private static (Domain.Entities.Procedure.Procedure Procedure, IReadOnlyList<DummyRuntimeAgent> Agents)
        BuildProcedure(Topology topology, DummyRuntimeAgentPacingConfig humanPacing)
    {
        return topology switch
        {
            Topology.FsControl => ProcedureBuilder.FsControl(humanPacing),
            Topology.HoldWeld => ProcedureBuilder.HoldWeld(humanPacing),
            Topology.HoldWeldInspectSort => ProcedureBuilder.HoldWeldInspectSort(humanPacing),
            Topology.ParallelSeams2 => ProcedureBuilder.ParallelSeams(humanPacing, 2),
            Topology.ParallelSeams4 => ProcedureBuilder.ParallelSeams(humanPacing, 4),
            Topology.SequentialPipeline2 => ProcedureBuilder.SequentialPipeline(humanPacing, 2),
            Topology.SequentialPipeline4 => ProcedureBuilder.SequentialPipeline(humanPacing, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(topology), topology, "Unknown topology.")
        };
    }

    /// <summary>
    ///     Maps a <see cref="PacingProfile" /> and seed onto a <see cref="DummyRuntimeAgentPacingConfig" />
    ///     for the human mock. The seed is threaded into <see cref="DummyRuntimeAgentPacingConfig.RandomSeed" />
    ///     so the random draws are reproducible per (profile, seed). The profiles widen or narrow the jitter
    ///     and noise relative to the live-simulator <see cref="PacingProfile.Default" />.
    /// </summary>
    /// <param name="profile">The pacing profile to translate.</param>
    /// <param name="seed">The seed making the mock's random draws reproducible.</param>
    /// <returns>The pacing configuration for the human mock.</returns>
    private static DummyRuntimeAgentPacingConfig ToPacingConfig(PacingProfile profile, int seed)
    {
        // The config exposes jitter/noise/tick/seed knobs (no explicit mean-bias field), so Slow and Early
        // are expressed as wider perturbation envelopes that, combined with the seed, draw actual durations
        // further from nominal; the seed makes each (profile, seed) cell reproducible.
        return profile switch
        {
            PacingProfile.Default => DummyRuntimeAgentPacingConfig.Default with { RandomSeed = seed },
            PacingProfile.Slow => DummyRuntimeAgentPacingConfig.Default with
            {
                DurationJitter = 0.30,
                EstimateNoiseAmplitude = 0.05,
                RandomSeed = seed
            },
            PacingProfile.Early => DummyRuntimeAgentPacingConfig.Default with
            {
                DurationJitter = 0.20,
                EstimateNoiseAmplitude = 0.05,
                RandomSeed = seed
            },
            PacingProfile.Noisy => DummyRuntimeAgentPacingConfig.Default with
            {
                DurationJitter = 0.40,
                EstimateNoiseAmplitude = 0.10,
                EstimateSinusoidFrequency = 0.6,
                RandomSeed = seed
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown pacing profile.")
        };
    }
}