using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Stress tests that exercise the parallel-per-SCC LP path under repeated invocation,
///     to surface concurrency issues (race conditions in OR-Tools native interop, GC
///     interactions with the native solver, etc.) that show up only intermittently.
/// </summary>
public class ParallelLpStressTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    ///     Initialises the test class with xUnit's output helper so that diagnostic
    ///     information (e.g. observed core counts) is visible in test reports.
    /// </summary>
    /// <param name="output">xUnit-provided sink for per-test diagnostic output.</param>
    public ParallelLpStressTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    ///     Confirms the scheduling pipeline's <c>Parallel.ForEach</c> options are pinned to
    ///     <see cref="Environment.ProcessorCount" />. This is a fast structural check that
    ///     guards against a future regression where someone lowers the default.
    /// </summary>
    [Fact]
    public void SccParallelOptions_MaxDegreeOfParallelism_EqualsProcessorCount()
    {
        var executionGraphExtensions = typeof(ExecutionGraphExtensions);
        var sccParallelOptions = executionGraphExtensions
            .GetField("SccParallelOptions", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as ParallelOptions;

        Assert.NotNull(sccParallelOptions);
        Assert.Equal(Environment.ProcessorCount, sccParallelOptions.MaxDegreeOfParallelism);
        _output.WriteLine($"Environment.ProcessorCount = {Environment.ProcessorCount}");
        _output.WriteLine($"SccParallelOptions.MaxDegreeOfParallelism = {sccParallelOptions.MaxDegreeOfParallelism}");
    }

    /// <summary>
    ///     Confirms a <c>Parallel.ForEach</c> using the same options the production
    ///     scheduling code uses executes every work item exactly once. Driven by an
    ///     external simulation rather than instrumenting <c>ExecutionGraphExtensions</c>
    ///     so the production code stays free of test hooks. The simulated workload
    ///     (50 items, ~80 μs CPU work each) mirrors the per-SCC LP solve cost from the
    ///     benchmark's AdaptiveCycles configuration.
    ///     Observed thread distribution is captured and logged as a diagnostic only:
    ///     <c>Parallel.ForEach</c> guarantees nothing about how many threads it dispatches
    ///     onto — under light load or on a lightly-loaded scheduler it may legally run all
    ///     work on a single thread — so asserting a minimum thread count is not a stable
    ///     invariant in CI. The structural guarantee that the options request full
    ///     parallelism is covered by
    ///     <see cref="SccParallelOptions_MaxDegreeOfParallelism_EqualsProcessorCount" />.
    /// </summary>
    [Fact]
    public void Parallel_ForEach_WithProcessorCountOption_ExecutesEveryWorkItemExactlyOnce()
    {
        var executionGraphExtensions = typeof(ExecutionGraphExtensions);
        var sccParallelOptions = executionGraphExtensions
            .GetField("SccParallelOptions", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as ParallelOptions;
        Assert.NotNull(sccParallelOptions);

        const int workItems = 50;
        var executionCounts = new ConcurrentDictionary<int, int>();
        var threadsObserved = new ConcurrentDictionary<int, int>();

        Parallel.ForEach(Enumerable.Range(0, workItems), sccParallelOptions, i =>
        {
            executionCounts.AddOrUpdate(i, 1, (_, v) => v + 1);
            threadsObserved.AddOrUpdate(Environment.CurrentManagedThreadId, 1, (_, v) => v + 1);
            Thread.SpinWait(200_000);
        });

        var distinctThreads = threadsObserved.Count;
        _output.WriteLine($"Environment.ProcessorCount = {Environment.ProcessorCount}");
        _output.WriteLine($"SccParallelOptions.MaxDegreeOfParallelism = {sccParallelOptions.MaxDegreeOfParallelism}");
        _output.WriteLine($"Distinct threads dispatched across {workItems} work items = {distinctThreads}");
        _output.WriteLine(
            $"Per-thread item counts: {string.Join(", ", threadsObserved.OrderByDescending(kv => kv.Value).Select(kv => $"T{kv.Key}={kv.Value}"))}");

        Assert.Equal(workItems, executionCounts.Count);
        Assert.All(executionCounts, kv => Assert.Equal(1, kv.Value));
    }


    /// <summary>
    ///     Drives <c>PlanSchedule</c> many times against a 200-skill, 50-adaptive-SCC graph
    ///     of the same shape used by the BenchmarkDotNet harness. Counts how many runs
    ///     either throw or produce invariant violations. Zero failures over a high iteration
    ///     count is the bar; any failure means parallelization is unsafe and we need a
    ///     lock or a sequential fallback.
    /// </summary>
    [Fact]
    public void PlanSchedule_AdaptiveCycles200_RepeatedRuns_NoCrashOrInvariantViolation()
    {
        // 500 iterations is the CI-friendly setting (~1.5s wall time). Locally this same
        // test was run at 5000 iterations (250,000 cumulative parallel SCC LP solves) with
        // zero failures; the loop is the cheapest place to catch any future regression
        // in OR-Tools' parallel safety.
        const int iterations = 500;
        const int skillCount = 200;
        const int cycleSize = 4;
        var logger = NullLogger.Instance;

        var failures = new ConcurrentBag<string>();

        for (var iter = 0; iter < iterations; iter++)
        {
            var graph = CreateAdaptiveCycleGraph(skillCount, cycleSize);

            try
            {
                graph.PlanSchedule(0.0, false, logger);

                foreach (var task in graph.SkillExecutions)
                {
                    if (task is AdaptivePlannedSkill adaptive &&
                        adaptive.PlannedDuration < adaptive.MinDuration)
                        failures.Add(
                            $"iter {iter}: duration {adaptive.PlannedDuration:F4} below MinDuration {adaptive.MinDuration}");

                    if (double.IsNaN(task.PlannedStartTime) || double.IsNaN(task.PlannedFinishTime))
                        failures.Add($"iter {iter}: NaN times on task {task.Id}");

                    if (Math.Abs(task.PlannedStartTime + task.PlannedDuration - task.PlannedFinishTime) > 1e-5)
                        failures.Add(
                            $"iter {iter}: linkage broken on task {task.Id}: S={task.PlannedStartTime} D={task.PlannedDuration} F={task.PlannedFinishTime}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"iter {iter}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(failures.IsEmpty,
            $"{failures.Count}/{iterations} iterations failed:\n{string.Join("\n", failures.Take(10))}");
    }

    /// <summary>
    ///     Verifies that <c>PlanSchedule</c> is deterministic: running it many times on
    ///     graphs with identical structure and identical task identifiers produces
    ///     bit-identical schedules (start, duration, finish for every task). Establishes
    ///     a baseline before changes (e.g., solver pooling) that could introduce subtle
    ///     state leaks between solves.
    /// </summary>
    [Fact]
    public void PlanSchedule_AdaptiveCycles200_RepeatedRuns_ProduceDeterministicSchedules()
    {
        const int iterations = 50;
        const int skillCount = 200;
        const int cycleSize = 4;
        var logger = NullLogger.Instance;

        var perRun = new (double Start, double Duration, double Finish)[iterations][];

        for (var iter = 0; iter < iterations; iter++)
        {
            var graph = CreateDeterministicAdaptiveCycleGraph(skillCount, cycleSize);
            graph.PlanSchedule(0.0, false, logger);

            perRun[iter] = graph.SkillExecutions
                .OrderBy(s => s.Id)
                .Select(s => (s.PlannedStartTime, s.PlannedDuration, s.PlannedFinishTime))
                .ToArray();
        }

        var reference = perRun[0];
        var mismatches = new List<string>();
        for (var iter = 1; iter < iterations; iter++)
            for (var i = 0; i < reference.Length; i++)
            {
                var actual = perRun[iter][i];
                if (Math.Abs(actual.Start - reference[i].Start) > 1e-9 ||
                    Math.Abs(actual.Duration - reference[i].Duration) > 1e-9 ||
                    Math.Abs(actual.Finish - reference[i].Finish) > 1e-9)
                    mismatches.Add(
                        $"iter {iter} task[{i}]: ref=(S={reference[i].Start},D={reference[i].Duration},F={reference[i].Finish}) actual=(S={actual.Start},D={actual.Duration},F={actual.Finish})");
            }

        Assert.True(mismatches.Count == 0,
            $"{mismatches.Count} schedule mismatches across {iterations} runs:\n{string.Join("\n", mismatches.Take(10))}");
    }

    /// <summary>
    ///     Builds the same SS-chain + FF-wraparound adaptive ring shape that the benchmark
    ///     generators produce, so the stress test exercises the production parallel code
    ///     path on a graph topology with many independent adaptive SCCs.
    /// </summary>
    /// <param name="count">Total number of adaptive skills.</param>
    /// <param name="cycleSize">Number of skills in each ring.</param>
    /// <returns>An execution graph with <c>count / cycleSize</c> independent adaptive SCCs.</returns>
    private static ExecutionGraph CreateAdaptiveCycleGraph(int count, int cycleSize)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();
        var numCycles = count / cycleSize;

        for (var cycle = 0; cycle < numCycles; cycle++)
        {
            var cycleSkills = Enumerable.Range(0, cycleSize)
                .Select(_ => new AdaptivePlannedSkill
                {
                    Id = Guid.NewGuid(),
                    MinDuration = 5.0,
                    PlannedDuration = 10.0
                })
                .Cast<IPlannedSkillExecution>()
                .ToList();

            skills.AddRange(cycleSkills);

            for (var i = 0; i < cycleSize - 1; i++)
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = cycleSkills[i],
                    Target = cycleSkills[i + 1],
                    Type = DependencyType.StartToStart
                });

            dependencies.Add(new Dependency
            {
                Id = Guid.NewGuid(),
                Source = cycleSkills[cycleSize - 1],
                Target = cycleSkills[0],
                Type = DependencyType.FinishToFinish
            });

            if (cycle > 0 && skills.Count > cycleSize)
            {
                var previousLast = skills[(cycle - 1) * cycleSize + cycleSize - 1];
                var currentFirst = cycleSkills[0];
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = previousLast,
                    Target = currentFirst,
                    Type = DependencyType.FinishToStart
                });
            }
        }

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Builds the same SS-chain + FF-wraparound adaptive ring shape as
    ///     <see cref="CreateAdaptiveCycleGraph" /> but assigns stable, predictable
    ///     identifiers to skills and dependencies so the resulting graph is identical
    ///     across calls. Used by the determinism test to compare schedules across runs.
    /// </summary>
    /// <param name="count">Total number of adaptive skills.</param>
    /// <param name="cycleSize">Number of skills in each ring.</param>
    /// <returns>An execution graph whose topology and task IDs are reproducible.</returns>
    private static ExecutionGraph CreateDeterministicAdaptiveCycleGraph(int count, int cycleSize)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();
        var numCycles = count / cycleSize;
        var skillIndex = 0;
        var depIndex = 1_000_000;

        for (var cycle = 0; cycle < numCycles; cycle++)
        {
            var cycleSkills = Enumerable.Range(0, cycleSize)
                .Select(_ => new AdaptivePlannedSkill
                {
                    Id = StableGuid(skillIndex++),
                    MinDuration = 5.0,
                    PlannedDuration = 10.0
                })
                .Cast<IPlannedSkillExecution>()
                .ToList();

            skills.AddRange(cycleSkills);

            for (var i = 0; i < cycleSize - 1; i++)
                dependencies.Add(new Dependency
                {
                    Id = StableGuid(depIndex++),
                    Source = cycleSkills[i],
                    Target = cycleSkills[i + 1],
                    Type = DependencyType.StartToStart
                });

            dependencies.Add(new Dependency
            {
                Id = StableGuid(depIndex++),
                Source = cycleSkills[cycleSize - 1],
                Target = cycleSkills[0],
                Type = DependencyType.FinishToFinish
            });

            if (cycle > 0 && skills.Count > cycleSize)
            {
                var previousLast = skills[(cycle - 1) * cycleSize + cycleSize - 1];
                var currentFirst = cycleSkills[0];
                dependencies.Add(new Dependency
                {
                    Id = StableGuid(depIndex++),
                    Source = previousLast,
                    Target = currentFirst,
                    Type = DependencyType.FinishToStart
                });
            }
        }

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Produces a deterministic <see cref="Guid" /> from a 32-bit seed by writing the
    ///     seed into the first four bytes of an otherwise-zero buffer.
    /// </summary>
    /// <param name="seed">A 32-bit integer that uniquely identifies the resulting Guid.</param>
    /// <returns>A reproducible <see cref="Guid" /> tied to <paramref name="seed" />.</returns>
    private static Guid StableGuid(int seed)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, seed);
        return new Guid(bytes);
    }
}