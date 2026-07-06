using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using FHOOE.Freydis.Scheduling.Core;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace FHOOE.Freydis.Scheduling.Benchmarks;

/// <summary>
///     BenchmarkDotNet configuration for SCC vs No-SCC performance comparison.
///     Uses InProcess toolchain to avoid project name/assembly name mismatch issues.
///     Groups benchmarks by category for per-scenario baseline comparisons.
/// </summary>
public class SccBenchmarkConfig : ManualConfig
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="SccBenchmarkConfig" /> class.
    /// </summary>
    public SccBenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(5)
            .WithIterationCount(20)
            .WithId("ShortRun"));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddColumn(StatisticColumn.P95);
        AddColumn(CategoriesColumn.Default);
        AddExporter(MarkdownExporter.GitHub);

        // CSV exporter with units only in header (not in each value)
        // Use custom culture without thousand separators for clean numeric output
        var csvCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        csvCulture.NumberFormat.NumberGroupSeparator = string.Empty;

        var csvStyle = new SummaryStyle(
            csvCulture,
            true,
            printUnitsInContent: false,
            sizeUnit: SizeUnit.KB,
            timeUnit: TimeUnit.Microsecond,
            ratioStyle: RatioStyle.Trend);
        AddExporter(new CsvExporter(CsvSeparator.Comma, csvStyle));

        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
    }
}

/// <summary>
///     Base class containing shared graph factory and clone methods for benchmarks.
/// </summary>
public abstract class SccBenchmarkBase
{
    /// <summary>
    ///     Gets or sets the number of skills to use in the benchmark.
    /// </summary>
    [Params(40, 100, 200)]
    public int SkillCount { get; set; }

    /// <summary>
    ///     Creates a linear chain of fixed-duration skills.
    /// </summary>
    protected static ExecutionGraph CreateLinearChainGraph(int count)
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
    protected static ExecutionGraph CreateParallelChainsGraph(int count, int chains)
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
    protected static ExecutionGraph CreateAdaptiveCycleGraph(int count, int cycleSize = 4)
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

            if (cycle > 0 && skills.Count > cycleSize)
            {
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
        }

        if (remaining > 0)
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
    protected static ExecutionGraph CreateMixedComplexGraph(int count)
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
        if (cycleCount >= 2)
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
    ///     Deep clones an execution graph.
    /// </summary>
    protected static ExecutionGraph CloneGraph(ExecutionGraph original)
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
}

/// <summary>
///     Benchmark comparing SCC vs No-SCC for Linear Chain graphs (40 skills in sequence).
///     All SCCs are trivial (single tasks) - tests SCC overhead vs LP overhead.
/// </summary>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
public class LinearChainBenchmarks : SccBenchmarkBase
{
    private ExecutionGraph _template = null!;

    /// <summary>
    ///     Global setup - creates the template graph.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _template = CreateLinearChainGraph(SkillCount);
    }

    /// <summary>
    ///     Benchmarks SCC-based scheduling (PlanSchedule) on a linear chain graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    public IExecutionGraph WithScc()
    {
        return CloneGraph(_template).PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving on a linear chain graph (baseline).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    public IExecutionGraph NoScc()
    {
        return CloneGraph(_template).SolveWithLinearProgramming();
    }
}

/// <summary>
///     Benchmark comparing SCC vs No-SCC for Parallel Chains graphs (4 chains of 10 skills).
///     Tests independent SCC handling with multiple trivial SCCs.
/// </summary>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
public class ParallelChainsBenchmarks : SccBenchmarkBase
{
    private ExecutionGraph _template = null!;

    /// <summary>
    ///     Global setup - creates the template graph.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _template = CreateParallelChainsGraph(SkillCount, 4);
    }

    /// <summary>
    ///     Benchmarks SCC-based scheduling on parallel chains graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    public IExecutionGraph WithScc()
    {
        return CloneGraph(_template).PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving on parallel chains graph (baseline).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    public IExecutionGraph NoScc()
    {
        return CloneGraph(_template).SolveWithLinearProgramming();
    }
}

/// <summary>
///     Benchmark comparing SCC vs No-SCC for Adaptive Cycles graphs (10 cycles of 4 adaptive skills).
///     Tests the scenario where SCC analysis provides the most benefit by solving smaller LP problems.
/// </summary>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
public class AdaptiveCyclesBenchmarks : SccBenchmarkBase
{
    private ExecutionGraph _template = null!;

    /// <summary>
    ///     Global setup - creates the template graph.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _template = CreateAdaptiveCycleGraph(SkillCount);
    }

    /// <summary>
    ///     Benchmarks SCC-based scheduling on adaptive cycles graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    public IExecutionGraph WithScc()
    {
        try
        {
            return CloneGraph(_template).PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            return _template;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on adaptive cycles graph (baseline).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    public IExecutionGraph NoScc()
    {
        try
        {
            return CloneGraph(_template).SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return _template;
        }
    }
}

/// <summary>
///     Benchmark comparing SCC vs No-SCC for Mixed Complex graphs (40% linear, 30% parallel, 30% adaptive cycle).
///     Tests a realistic scenario with mixed graph patterns.
/// </summary>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
public class MixedComplexBenchmarks : SccBenchmarkBase
{
    private ExecutionGraph _template = null!;

    /// <summary>
    ///     Global setup - creates the template graph.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _template = CreateMixedComplexGraph(SkillCount);
    }

    /// <summary>
    ///     Benchmarks SCC-based scheduling on mixed complex graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    public IExecutionGraph WithScc()
    {
        try
        {
            return CloneGraph(_template).PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            return _template;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on mixed complex graph (baseline).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    public IExecutionGraph NoScc()
    {
        try
        {
            return CloneGraph(_template).SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return _template;
        }
    }
}

/// <summary>
///     Combined benchmark class that runs all scenarios. Each scenario has its own baseline.
///     Use this for a quick overview of all scenarios in one run.
/// </summary>
/// <remarks>
///     Run from command line:
///     <code>
///     cd Backend/Scheduling.Benchmarks
///     dotnet run -c Release
///     </code>
/// </remarks>
[Config(typeof(SccBenchmarkConfig))]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SccVsNoSccBenchmarks : SccBenchmarkBase
{
    private ExecutionGraph _adaptiveCycleTemplate = null!;
    private ExecutionGraph _linearChainTemplate = null!;
    private ExecutionGraph _mixedComplexTemplate = null!;
    private ExecutionGraph _parallelChainsTemplate = null!;

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

    #region Linear Chain Benchmarks

    /// <summary>
    ///     Benchmarks SCC-based scheduling (PlanSchedule) on a linear chain graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    [BenchmarkCategory("1_LinearChain")]
    public IExecutionGraph LinearChain_WithScc()
    {
        return CloneGraph(_linearChainTemplate).PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving on a linear chain graph (baseline for this category).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    [BenchmarkCategory("1_LinearChain")]
    public IExecutionGraph LinearChain_NoScc()
    {
        return CloneGraph(_linearChainTemplate).SolveWithLinearProgramming();
    }

    #endregion

    #region Parallel Chains Benchmarks

    /// <summary>
    ///     Benchmarks SCC-based scheduling on parallel chains graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    [BenchmarkCategory("2_ParallelChains")]
    public IExecutionGraph ParallelChains_WithScc()
    {
        return CloneGraph(_parallelChainsTemplate).PlanSchedule();
    }

    /// <summary>
    ///     Benchmarks direct LP solving on parallel chains graph (baseline for this category).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    [BenchmarkCategory("2_ParallelChains")]
    public IExecutionGraph ParallelChains_NoScc()
    {
        return CloneGraph(_parallelChainsTemplate).SolveWithLinearProgramming();
    }

    #endregion

    #region Adaptive Cycles Benchmarks

    /// <summary>
    ///     Benchmarks SCC-based scheduling on adaptive cycles graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    [BenchmarkCategory("3_AdaptiveCycles")]
    public IExecutionGraph AdaptiveCycles_WithScc()
    {
        try
        {
            return CloneGraph(_adaptiveCycleTemplate).PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            return _adaptiveCycleTemplate;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on adaptive cycles graph (baseline for this category).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    [BenchmarkCategory("3_AdaptiveCycles")]
    public IExecutionGraph AdaptiveCycles_NoScc()
    {
        try
        {
            return CloneGraph(_adaptiveCycleTemplate).SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return _adaptiveCycleTemplate;
        }
    }

    #endregion

    #region Mixed Complex Benchmarks

    /// <summary>
    ///     Benchmarks SCC-based scheduling on mixed complex graph.
    /// </summary>
    [Benchmark(Description = "WithSCC")]
    [BenchmarkCategory("4_MixedComplex")]
    public IExecutionGraph MixedComplex_WithScc()
    {
        try
        {
            return CloneGraph(_mixedComplexTemplate).PlanSchedule();
        }
        catch (ScheduleInfeasibleException)
        {
            return _mixedComplexTemplate;
        }
    }

    /// <summary>
    ///     Benchmarks direct LP solving on mixed complex graph (baseline for this category).
    /// </summary>
    [Benchmark(Baseline = true, Description = "NoSCC")]
    [BenchmarkCategory("4_MixedComplex")]
    public IExecutionGraph MixedComplex_NoScc()
    {
        try
        {
            return CloneGraph(_mixedComplexTemplate).SolveWithLinearProgramming();
        }
        catch (ScheduleInfeasibleException)
        {
            return _mixedComplexTemplate;
        }
    }

    #endregion
}