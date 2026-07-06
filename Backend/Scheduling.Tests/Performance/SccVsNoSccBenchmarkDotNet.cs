using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling.Tests.Performance;

/// <summary>
///     BenchmarkDotNet configuration for SCC vs No-SCC performance comparison.
///     Uses ShortRun job for faster iteration during development.
/// </summary>
public class SccBenchmarkConfig : ManualConfig
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="SccBenchmarkConfig" /> class.
    /// </summary>
    public SccBenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithId("ShortRun"));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddColumn(StatisticColumn.P95);
        AddExporter(MarkdownExporter.GitHub);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
    }
}

/// <summary>
///     BenchmarkDotNet benchmarks for comparing scheduling performance with SCC (Strongly Connected Components)
///     analysis versus direct LP (Linear Programming) solving without SCC decomposition.
///     Uses 40 skill executions as the baseline configuration.
/// </summary>
/// <remarks>
///     <para>
///         To run these benchmarks from the command line:
///         <code>
///         cd Backend/Scheduling.Tests
///         dotnet run -c Release -- --filter "*SccVsNoSccBenchmarkDotNet*"
///         </code>
///     </para>
///     <para>
///         Or use the <see cref="RunBenchmarks" /> test method to trigger from within the test framework.
///     </para>
/// </remarks>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SccVsNoSccBenchmarkDotNet
{
    private ExecutionGraph _adaptiveCycleTemplate = null!;
    private ExecutionGraph _linearChainTemplate = null!;
    private ExecutionGraph _mixedComplexTemplate = null!;
    private ExecutionGraph _parallelChainsTemplate = null!;

    /// <summary>
    ///     Gets or sets the number of skills to use in the benchmark.
    /// </summary>
    [Params(40)]
    public int SkillCount { get; set; }

    /// <summary>
    ///     Global setup for the benchmark. Creates template graphs for each scenario.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _linearChainTemplate = CreateLinearChainGraph(SkillCount);
        _parallelChainsTemplate = CreateParallelChainsGraph(SkillCount, 4);
        _adaptiveCycleTemplate = CreateAdaptiveCycleGraph(SkillCount);
        _mixedComplexTemplate = CreateMixedComplexGraph(SkillCount);
    }

    #region Benchmark Methods - Linear Chain

    /// <summary>
    ///     Benchmarks SCC-based scheduling (PlanSchedule) on a linear chain graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "LinearChain_WithSCC")]
    [BenchmarkCategory("LinearChain")]
    public IExecutionGraph LinearChain_WithScc()
    {
        var graph = CloneGraph(_linearChainTemplate);
        return graph.PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving (SolveWithLinearProgramming) on a linear chain graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Baseline = true, Description = "LinearChain_NoSCC")]
    [BenchmarkCategory("LinearChain")]
    public IExecutionGraph LinearChain_NoScc()
    {
        var graph = CloneGraph(_linearChainTemplate);
        return graph.SolveWithLinearProgramming();
    }

    #endregion

    #region Benchmark Methods - Parallel Chains

    /// <summary>
    ///     Benchmarks SCC-based scheduling on parallel chains graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "ParallelChains_WithSCC")]
    [BenchmarkCategory("ParallelChains")]
    public IExecutionGraph ParallelChains_WithScc()
    {
        var graph = CloneGraph(_parallelChainsTemplate);
        return graph.PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving on parallel chains graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "ParallelChains_NoSCC")]
    [BenchmarkCategory("ParallelChains")]
    public IExecutionGraph ParallelChains_NoScc()
    {
        var graph = CloneGraph(_parallelChainsTemplate);
        return graph.SolveWithLinearProgramming();
    }

    #endregion

    #region Benchmark Methods - Adaptive Cycles

    /// <summary>
    ///     Benchmarks SCC-based scheduling on adaptive cycles graph.
    ///     This is the scenario where SCC analysis should provide benefits by solving smaller LP problems.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "AdaptiveCycles_WithSCC")]
    [BenchmarkCategory("AdaptiveCycles")]
    public IExecutionGraph AdaptiveCycles_WithScc()
    {
        var graph = CloneGraph(_adaptiveCycleTemplate);
        try
        {
            return graph.PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            // Cycles of fixed-duration tasks are infeasible, but adaptive should work
            return graph;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on adaptive cycles graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "AdaptiveCycles_NoSCC")]
    [BenchmarkCategory("AdaptiveCycles")]
    public IExecutionGraph AdaptiveCycles_NoScc()
    {
        var graph = CloneGraph(_adaptiveCycleTemplate);
        try
        {
            return graph.SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return graph;
        }
    }

    #endregion

    #region Benchmark Methods - Mixed Complex

    /// <summary>
    ///     Benchmarks SCC-based scheduling on mixed complex graph (realistic scenario).
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "MixedComplex_WithSCC")]
    [BenchmarkCategory("MixedComplex")]
    public IExecutionGraph MixedComplex_WithScc()
    {
        var graph = CloneGraph(_mixedComplexTemplate);
        try
        {
            return graph.PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            return graph;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on mixed complex graph.
    /// </summary>
    /// <returns>The scheduled execution graph.</returns>
    [Benchmark(Description = "MixedComplex_NoSCC")]
    [BenchmarkCategory("MixedComplex")]
    public IExecutionGraph MixedComplex_NoScc()
    {
        var graph = CloneGraph(_mixedComplexTemplate);
        try
        {
            return graph.SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return graph;
        }
    }

    #endregion

    #region Graph Factory Methods

    /// <summary>
    ///     Creates a linear chain of fixed-duration skills: A -> B -> C -> ... (no cycles).
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <returns>An execution graph with a linear chain of skills.</returns>
    private static ExecutionGraph CreateLinearChainGraph(int count)
    {
        var skills = Enumerable.Range(0, count)
            .Select(i => new FixedDurationPlannedSkill
            {
                Id = Guid.NewGuid(),
                PlannedDuration = 10.0 + i % 5
            })
            .Cast<IPlannedSkillExecution>()
            .ToList();

        var dependencies = new List<Dependency>();
        for (var i = 0; i < count - 1; i++)
            dependencies.Add(new Dependency
            {
                Id = Guid.NewGuid(),
                Source = skills[i],
                Target = skills[i + 1],
                Type = DependencyType.FinishToStart
            });

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Creates a graph with multiple independent parallel chains.
    /// </summary>
    /// <param name="count">Total number of skills to create.</param>
    /// <param name="chains">Number of parallel chains.</param>
    /// <returns>An execution graph with parallel chains of skills.</returns>
    private static ExecutionGraph CreateParallelChainsGraph(int count, int chains)
    {
        var skillsPerChain = count / chains;
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();

        for (var chain = 0; chain < chains; chain++)
        {
            var chainSkills = Enumerable.Range(0, skillsPerChain)
                .Select(i => new FixedDurationPlannedSkill
                {
                    Id = Guid.NewGuid(),
                    PlannedDuration = 10.0 + (chain + i) % 7
                })
                .Cast<IPlannedSkillExecution>()
                .ToList();

            skills.AddRange(chainSkills);

            for (var i = 0; i < skillsPerChain - 1; i++)
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = chainSkills[i],
                    Target = chainSkills[i + 1],
                    Type = DependencyType.FinishToStart
                });
        }

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Creates a graph with adaptive skills that form cycles.
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <param name="cycleSize">Size of each cycle.</param>
    /// <returns>An execution graph with adaptive skill cycles.</returns>
    private static ExecutionGraph CreateAdaptiveCycleGraph(int count, int cycleSize = 4)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();

        var numCycles = count / cycleSize;
        var remaining = count % cycleSize;

        for (var cycle = 0; cycle < numCycles; cycle++)
        {
            var cycleSkills = Enumerable.Range(0, cycleSize)
                .Select(i => new AdaptivePlannedSkill
                {
                    Id = Guid.NewGuid(),
                    MinDuration = 5.0,
                    PlannedDuration = 10.0
                })
                .Cast<IPlannedSkillExecution>()
                .ToList();

            skills.AddRange(cycleSkills);

            // Intra-cycle: SS forward chain + single FF wraparound.
            // Event-level acyclic (passes ValidateModel) and forms a non-trivial SCC
            // whose feasibility exercises the LP's duration-assignment freedom.
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

            if (cycle <= 0 || skills.Count <= cycleSize) continue;
            var previousCycleLastSkill = skills[(cycle - 1) * cycleSize + cycleSize - 1];
            var currentCycleFirstSkill = cycleSkills[0];
            dependencies.Add(new Dependency
            {
                Id = Guid.NewGuid(),
                Source = previousCycleLastSkill,
                Target = currentCycleFirstSkill,
                Type = DependencyType.FinishToStart
            });
        }

        if (remaining <= 0) return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
        {
            var lastSkill = skills.LastOrDefault();
            for (var i = 0; i < remaining; i++)
            {
                var skill = new FixedDurationPlannedSkill
                {
                    Id = Guid.NewGuid(),
                    PlannedDuration = 8.0
                };
                skills.Add(skill);

                if (lastSkill != null)
                    dependencies.Add(new Dependency
                    {
                        Id = Guid.NewGuid(),
                        Source = lastSkill,
                        Target = skill,
                        Type = DependencyType.FinishToStart
                    });

                lastSkill = skill;
            }
        }

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Creates a complex realistic graph with a mix of patterns.
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <returns>An execution graph with mixed patterns.</returns>
    private static ExecutionGraph CreateMixedComplexGraph(int count)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();

        var linearCount = (int)(count * 0.4);
        var linearSkills = Enumerable.Range(0, linearCount)
            .Select(i => new FixedDurationPlannedSkill
            {
                Id = Guid.NewGuid(),
                PlannedDuration = 10.0 + i % 5
            })
            .Cast<IPlannedSkillExecution>()
            .ToList();

        skills.AddRange(linearSkills);
        for (var i = 0; i < linearCount - 1; i++)
            dependencies.Add(new Dependency
            {
                Id = Guid.NewGuid(),
                Source = linearSkills[i],
                Target = linearSkills[i + 1],
                Type = DependencyType.FinishToStart
            });

        var parallelCount = (int)(count * 0.3);
        var parallelSkills = Enumerable.Range(0, parallelCount)
            .Select(i => new FixedDurationPlannedSkill
            {
                Id = Guid.NewGuid(),
                PlannedDuration = 15.0 + i % 3
            })
            .Cast<IPlannedSkillExecution>()
            .ToList();

        skills.AddRange(parallelSkills);

        if (linearSkills.Count > 0)
            foreach (var parallelSkill in parallelSkills)
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = linearSkills[^1],
                    Target = parallelSkill,
                    Type = DependencyType.FinishToStart
                });

        var cycleCount = count - linearCount - parallelCount;
        if (cycleCount < 2) return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
        {
            var cycleSkills = Enumerable.Range(0, cycleCount)
                .Select(i => new AdaptivePlannedSkill
                {
                    Id = Guid.NewGuid(),
                    MinDuration = 5.0,
                    PlannedDuration = 12.0
                })
                .Cast<IPlannedSkillExecution>()
                .ToList();

            skills.AddRange(cycleSkills);

            // Intra-cycle: SS forward chain + single FF wraparound (event-level acyclic).
            for (var i = 0; i < cycleCount - 1; i++)
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
                Source = cycleSkills[cycleCount - 1],
                Target = cycleSkills[0],
                Type = DependencyType.FinishToFinish
            });

            if (parallelSkills.Count > 0)
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = parallelSkills[0],
                    Target = cycleSkills[0],
                    Type = DependencyType.FinishToStart
                });
        }

        return new ExecutionGraph { SkillExecutions = skills, Dependencies = dependencies };
    }

    /// <summary>
    ///     Deep clones an execution graph to ensure fresh data for each benchmark iteration.
    /// </summary>
    /// <param name="original">The original graph to clone.</param>
    /// <returns>A deep clone of the execution graph.</returns>
    private static ExecutionGraph CloneGraph(ExecutionGraph original)
    {
        var skillMap = new Dictionary<Guid, IPlannedSkillExecution>();
        var newSkills = new List<IPlannedSkillExecution>();

        foreach (var skill in original.SkillExecutions)
        {
            IPlannedSkillExecution newSkill;
            if (skill is AdaptivePlannedSkill adaptive)
                newSkill = new AdaptivePlannedSkill
                {
                    Id = skill.Id,
                    MinDuration = adaptive.MinDuration,
                    PlannedDuration = adaptive.PlannedDuration,
                    PlannedStartTime = skill.PlannedStartTime,
                    PlannedFinishTime = skill.PlannedFinishTime
                };
            else
                newSkill = new FixedDurationPlannedSkill
                {
                    Id = skill.Id,
                    PlannedDuration = skill.PlannedDuration,
                    PlannedStartTime = skill.PlannedStartTime,
                    PlannedFinishTime = skill.PlannedFinishTime
                };

            newSkills.Add(newSkill);
            skillMap[skill.Id] = newSkill;
        }

        var newDeps = original.Dependencies
            .Select(d => new Dependency
            {
                Id = d.Id,
                Source = skillMap[d.Source.Id],
                Target = skillMap[d.Target.Id],
                Type = d.Type
            })
            .ToList();

        return new ExecutionGraph { SkillExecutions = newSkills, Dependencies = newDeps };
    }

    #endregion
}

/// <summary>
///     xUnit test class to trigger BenchmarkDotNet benchmarks from the test runner.
/// </summary>
public class SccBenchmarkRunner
{
    /// <summary>
    ///     Runs the BenchmarkDotNet benchmarks. This test is skipped by default in CI
    ///     as it takes significant time. Run manually for performance analysis.
    /// </summary>
    /// <remarks>
    ///     To run this benchmark:
    ///     <code>
    ///     dotnet test --filter "FullyQualifiedName~RunBenchmarks" -c Release
    ///     </code>
    ///     Or from command line:
    ///     <code>
    ///     dotnet run -c Release --project Backend/Scheduling.Tests -- --filter "*SccVsNoScc*"
    ///     </code>
    /// </remarks>
    [Fact]
    public void RunBenchmarks()
    {
        var summary = BenchmarkRunner.Run<SccVsNoSccBenchmarkDotNet>();
        Assert.NotNull(summary);
    }
}