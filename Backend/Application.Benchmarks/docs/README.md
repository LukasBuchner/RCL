# Application Benchmarks

> Application-layer BenchmarkDotNet suite measuring scheduling-pipeline latency and the runtime conformance of the
> dual-loop execution to its proved invariants, with mock agents.

---

## Overview

This project hosts the BenchmarkDotNet benchmarks that exercise the Application layer end to end. It runs the real
scheduling pipeline and the real reactive execution runtime against synthetic procedures, so the timing and conformance
results reflect production code paths rather than stubs.

The benchmarks are split into two classes: one measures the latency of a full scheduling calculation, and the other
drives the dual-loop execution under a spread of human-pacing uncertainties and checks that the runtime honoured the
invariants proved in the verification section (no premature triggering, router branch consistency, deadlock-free
termination, and the adaptive coupling). The SCC-vs-monolithic scheduling benchmark is not here — it lives in the
separate `Scheduling.Benchmarks` project. Every benchmark class in this project carries its own `[Config]`, and all are
discovered through the `BenchmarkSwitcher` wired up in `Program.cs`:

```csharp
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

Because the switcher is used, a `--filter` glob selects a class by name. Run from the `Application.Benchmarks` project
directory:

```bash
# Select a benchmark class by name glob
dotnet run -c Release -- --filter '*EndToEndPipeline*'
dotnet run -c Release -- --filter '*ReactiveExecution*'
```

## Key Concepts

- **End-to-end pipeline** — One scheduling calculation through the Application layer, timed from request to result.
- **Runtime conformance** — Checking, after each run, that the execution honoured the dependency ordering, router
  consistency, termination, and adaptive-coupling invariants the verification section proves.
- **Mock agents** — Headless robot and human stand-ins that produce deterministic outputs and paced durations so a full
  reactive run completes in seconds.
- **Pacing profile** — A named perturbation of the human mock's duration and estimate noise, standing in for a different
  human uncertainty the flow must stay correct under.
- **Conformance result** — Per-run violation counts (premature triggers, off-branch firings, unterminated nodes) plus
  the adaptive-coupling flag, written to CSV and surfaced as custom summary columns.

## Benchmarks

| Benchmark             | Class                         | What it measures                                                                          |
|-----------------------|-------------------------------|-------------------------------------------------------------------------------------------|
| **EndToEndPipeline**  | `EndToEndPipelineBenchmarks`  | Latency of the full scheduling pipeline through the Application layer.                    |
| **ReactiveExecution** | `ReactiveExecutionBenchmarks` | Runtime conformance of the dual loop to its proved invariants, under varied human pacing. |

### EndToEndPipeline

`EndToEndPipelineBenchmarks` times the real `TimingCalculationOrchestrator.CalculateAsync` over a synthetic agent,
node, and edge hierarchy. It uses REAL implementations for every service except `IRouterBranchFilterService` and
`INodeHidingService`, which are mocked. `[GlobalSetup]` builds the agents, hierarchy, `SchedulingRequest`, and
orchestrator before timing; the single `[Benchmark(Description = "FullPipeline")]` method invokes the orchestrator. It
sweeps `[Params]` `SkillCount` over `40, 100, 200` and `TaskCount` fixed at `15`, runs under
`PipelineBenchmarkConfig` (a `Job.ShortRun` in-process emit toolchain with warmup count 3, iteration count 15,
markdown + CSV exporters), and is a `[MemoryDiagnoser]`.

> **Note — currently cannot run.** The `SkillCount` `[Params]` property is declared getter-only
> (`[Params(40, 100, 200)] public int SkillCount { get; }`). BenchmarkDotNet sets `[Params]` values by writing to the
> property, so a getter-only property fails validation and this benchmark will not run until a setter is added
> (`public int SkillCount { get; set; }`), matching the already-correct `TaskCount { get; set; }`.

### ReactiveExecution

`ReactiveExecutionBenchmarks` checks the runtime conformance of the dual-loop execution against mock agents. The single
`[Benchmark(Description = "Conformance")]` method subscribes an `ExecutionConformanceChecker` to the runtime's event
bus (`ISkillExecutionEventBus.AllEvents`) and `NodesChanged` stream, starts the loaded procedure, awaits completion
against a 60-second `ExecutionTimeout`, then verifies the captured events against the proved invariants. A run that
fails to start or exceeds the timeout records a non-conformant row (a termination-guarantee failure); otherwise the
checker writes the per-run violation counts. Each iteration rebuilds the DI provider, repository, agent manager,
procedure, and agents, loads through the real `IProcedureOrchestrator.LoadProcedureAsync`, and resolves the concrete
`ExecutionOrchestrator` so its `CurrentExecution` task is awaitable. It sweeps a full grid of
7 topologies x 4 pacing profiles x 5 seeds = 140 cells under `ReactiveExecutionBenchmarkConfig` (in-process emit
toolchain, `RunStrategy.Monitoring`, warmup 0, iteration count 3, launch/invocation/unroll all 1).

## Reactive-Execution Conformance

The reactive benchmark runs a headless DI container assembled by
`BenchmarkExecutionServices.AddReactiveExecutionRuntime`, which replicates the GraphQL host's execution-relevant
service graph without any GraphQL, hosting, or Postgres dependencies. It uses the real
`IProcedureOrchestrator.LoadProcedureAsync` load path and sets all `ExecutionPipeline` publish intervals
(`RescheduleSampleIntervalMs`, `AgentPublishIntervalMs`, `FrontendPublishIntervalMs`, `TimingPublishIntervalMs`) to
`1`, with `Scheduler.Default` and real wall-clock `TimeProvider.System`.

### Benchmarked Workflows

Each `Topology` enum value selects a workflow built by `ProcedureBuilder`. All shapes except the FS control chain are
built from a coupled `Welding` core (`Hold (Adaptive)` + `Weld`) joined by `SS(Hold,Weld)` and `FF(Weld,Hold)`.

| Topology              | Shape                                                                                                                                                                              |
|-----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FsControl`           | Pure Finish-to-Start chain of non-adaptive skills, no coupling; the simplest flow with no adaptive task.                                                                           |
| `HoldWeld`            | Minimal coupled unit: one `Welding` core, one robot mock + one human mock.                                                                                                         |
| `HoldWeldInspectSort` | The running example: coupled `Welding` core, an `FS(Welding,Inspect)` successor binding a `quality` variable, and a `Sort` router branching to `Store{Shelve}` / `Discard{Scrap}`. |
| `ParallelSeams2`      | Two independent `Hold-Weld` pairs concurrently; two adaptive couplings at once.                                                                                                    |
| `ParallelSeams4`      | Four independent `Hold-Weld` pairs concurrently; four adaptive couplings at once.                                                                                                  |
| `SequentialPipeline2` | Two `Hold-Weld` pairs in series, FS-chained; the dependency chain runs down a chain of coupled cores.                                                                              |
| `SequentialPipeline4` | Four `Hold-Weld` pairs in series, FS-chained; a longer chain of coupled cores.                                                                                                     |

### Mock Role Split

A robot mock runs the adaptive `Hold` and any non-adaptive successors (`Inspect`, `Shelve`, `Scrap`); a human mock
runs the paced `Weld` under a `DummyRuntimeAgentPacingConfig`. Only the human mock receives the supplied pacing config;
robot mocks use default behaviour. `ParallelSeams` gives each seam its own robot and human mock
(`RobotMock{n}` / `HumanMock{n}`); `SequentialPipeline` shares one robot mock and one human mock across the whole
chain.

In `HoldWeldInspectSort`, the robot mock is built with `DummyRuntimeAgentOutputConfig { UseConfiguredValues = true }`,
and the `Inspect` skill carries a `quality` output property whose configured value is `"OK"`. Because
`UseConfiguredValues = true` makes the dummy honor a present configured value verbatim, the dummy deterministically
writes `"OK"`, so the `Sort` router's `quality == "OK"` condition genuinely selects the `Store` branch — the routing is
real, not a test workaround. A linchpin assertion (`AssertConfiguredOutputPresent(inspectSkill, "quality")`) verifies
the named output exists with a non-null configured value before assembly.

### Pacing Profiles

`ToPacingConfig(profile, seed)` maps each `PacingProfile` onto a `DummyRuntimeAgentPacingConfig` (always with
`RandomSeed = seed`):

The duration jitter is a symmetric band around the nominal estimate (the draw is uniform in `[nominal*(1-jitter),
nominal*(1+jitter)]`, no directional bias), so the four profiles are increasing levels of human uncertainty, not a
slower/earlier-finishing bias:

| Profile   | Draw     | Effect                                                                                                                                 |
|-----------|----------|----------------------------------------------------------------------------------------------------------------------------------------|
| `Default` | `+/-15%` | The live simulator: `DurationJitter = 0.15`, `EstimateNoiseAmplitude = 0.03`, with a decaying sinusoid.                                |
| `Early`   | `+/-20%` | `DurationJitter = 0.20`, `EstimateNoiseAmplitude = 0.05`; a wider symmetric draw.                                                      |
| `Slow`    | `+/-30%` | `DurationJitter = 0.30`, `EstimateNoiseAmplitude = 0.05`; wider still.                                                                 |
| `Noisy`   | `+/-40%` | `DurationJitter = 0.40`, `EstimateNoiseAmplitude = 0.10`, `EstimateSinusoidFrequency = 0.6`; the widest, with the most estimate noise. |

### Pacing and Output Knobs

`DummyRuntimeAgentPacingConfig` controls how the human mock paces a fixed-duration skill:

| Field                       | Default | Meaning                                                                                        |
|-----------------------------|---------|------------------------------------------------------------------------------------------------|
| `DurationJitter`            | `0.15`  | Symmetric fractional jitter on nominal duration (`0.15` = +/-15 % uniform draw; `0` disables). |
| `EstimateNoiseAmplitude`    | `0.03`  | Peak amplitude (fraction of nominal) of estimate noise, decaying to zero near completion.      |
| `EstimateSinusoidFrequency` | `0.3`   | Angular frequency (rad/elapsed-second) of the estimate-noise sinusoid.                         |
| `TimerTickMs`               | `3`     | Interval (ms) between progress emissions of the reactive timer.                                |
| `RandomSeed`                | `null`  | Optional seed for reproducible random draws; `null` is a non-deterministic time-based seed.    |

`DummyRuntimeAgentOutputConfig` controls how a mock produces skill outputs:

| Field                 | Default | Meaning                                                                                                                                             |
|-----------------------|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| `UseConfiguredValues` | `false` | When `true`, a present configured output value is emitted verbatim; a `null` value still falls through to type-based simulation. Applies per agent. |
| `BooleanTruePassRate` | `0.85`  | Probability that a synthesized boolean output is `true` (simulation mode only).                                                                     |

The `Hold (Adaptive)` and `Weld` skills use a `0.5` s nominal duration; the successor skills (`Inspect`, `Shelve`,
`Scrap`) and the FS control chain use `0.3` s, so a full reactive run completes in seconds.

### Conformance checks

After a run completes, `ExecutionConformanceChecker` builds the runtime's resolved dependency graph
(`IDependencyGraphAnalyzer`, the proofs' `prereq_start` / `prereq_finish` structure) and verifies the captured events
against four invariant classes, each mapped to a proved result:

| Check                         | What it verifies                                                                                                                                                                                                                                                                                                                     | Proof                                                           |
|-------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------|
| **Dependency ordering**       | Every node fired its Start only after every start-prerequisite's required event, and its Finish only after every finish-prerequisite's; the FS, SS, and FF orderings of the declared model held at runtime, even across reschedules. By the scheduling-execution bridge this also witnesses that the followed schedule was feasible. | No premature triggering; safety under rescheduling; the bridge. |
| **Router branch consistency** | No node the router marked off-branch (a `NotSelected` event) ever fired a Start or Finish.                                                                                                                                                                                                                                           | Router branch consistency.                                      |
| **Termination**               | Every skill or router node reached a terminal event (Finish, or NotSelected for an off-branch node), the runtime form of the measure dropping to zero.                                                                                                                                                                               | Deadlock freedom and termination.                               |
| **Adaptive coupling**         | Every adaptive node finished together with its finish-prerequisite source (within tolerance): the robot's Hold tracked the human's Weld whatever the pacing.                                                                                                                                                                         | The SS-and-FF-coupled core.                                     |

One CSV row is written per run. The key columns are `OrderingViolations`, `RouterOffBranchFirings`, and
`UnterminatedNodes` (all expected zero), `AdaptiveCouplingHeld`, `TerminalTransitions` (the measure countdown,
`|T u R|` per run), `RescheduleSnapshots` (outer-loop refinements, context), and `Conformant` (the run completed and
every violation count is zero). Off-branch nodes are identified from their `NotSelected` events, so the check is
self-contained and does not depend on the trigger service's selection map, which the session clears on teardown.

### Running

From the `Application.Benchmarks` project directory:

```bash
dotnet run -c Release -- --filter '*ReactiveExecution*'
```

### Output

The checker writes a timestamped CSV under the BenchmarkDotNet artifacts directory:

```
BenchmarkDotNet.Artifacts/conformance-YYYYMMDD-HHMMSS.csv
```

The BenchmarkDotNet summary table also surfaces `ExecutionConformanceColumns.All`, custom always-shown columns:

| Column        | Meaning                                                           |
|---------------|-------------------------------------------------------------------|
| `Conformant`  | `"yes"` when the run completed and every checked invariant held.  |
| `OrderViol`   | Runtime dependency-ordering violations (expected 0).              |
| `RouterViol`  | Off-branch nodes that fired (expected 0).                         |
| `Unterm`      | Reachable nodes that never reached a terminal event (expected 0). |
| `Coupling`    | Whether every adaptive node finished together with its partner.   |
| `Terminal`    | Nodes that reached a terminal event (Finish or NotSelected).      |
| `Reschedules` | Distinct node-change snapshots (outer-loop refinements, context). |

A case with no recorded result renders `"?"`.

## Related

- [Documentation Hub](../../docs/README.md) — Back to the index
- [Glossary](../../docs/glossary.md) — Term definitions
- [Execution Pipeline](../../docs/execution-pipeline.md) — Detailed execution flow walkthrough
- [Architecture Overview](../../docs/architecture.md) — Full system overview
- [Application Layer](../../Application/docs/README.md) — Business logic, services, reactive patterns
- [Scheduling Tests](../../Scheduling.Tests/docs/README.md) — xUnit + BenchmarkDotNet coverage for the scheduling module
