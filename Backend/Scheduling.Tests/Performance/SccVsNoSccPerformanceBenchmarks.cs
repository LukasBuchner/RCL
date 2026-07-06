using System.Diagnostics;
using FHOOE.Freydis.Scheduling.Core;
using Xunit.Abstractions;

namespace FHOOE.Freydis.Scheduling.Tests.Performance;

/// <summary>
///     Performance benchmark tests comparing scheduling with SCC (Strongly Connected Components) analysis
///     versus scheduling without SCC analysis. Uses 40 skill executions to measure the overhead
///     and benefits of the SCC-based approach.
/// </summary>
/// <remarks>
///     <para>
///         The two approaches compared:
///         <list type="bullet">
///             <item>
///                 <term>With SCC</term>
///                 <description>
///                     Uses <see cref="ExecutionGraphExtensions.PlanSchedule" /> which performs Tarjan's SCC detection,
///                     classifies SCCs (Trivial, AdaptiveCycle, FixedCoupledGroup), and handles each type appropriately.
///                     Complexity: O(V+E) for SCC detection + LP solving for adaptive cycles.
///                 </description>
///             </item>
///             <item>
///                 <term>Without SCC</term>
///                 <description>
///                     Uses <see cref="ExecutionGraphExtensions.SolveWithLinearProgramming" /> directly on the entire graph.
///                     Solves everything as one LP problem without SCC decomposition.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public sealed class SccVsNoSccPerformanceBenchmarks
{
    private const int SkillCount = 40;
    private const int WarmupIterations = 3;
    private const int MeasuredIterations = 10;
    private readonly ITestOutputHelper _output;

    /// <summary>
    ///     Initialises a new instance of the <see cref="SccVsNoSccPerformanceBenchmarks" /> class.
    /// </summary>
    /// <param name="output">The xUnit test output helper for logging results.</param>
    public SccVsNoSccPerformanceBenchmarks(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    ///     Creates a linear chain of fixed-duration skills: A → B → C → ... (no cycles).
    ///     This represents a best-case scenario for SCC analysis as all SCCs are trivial (single tasks).
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <returns>An execution graph with a linear chain of skills.</returns>
    private static ExecutionGraph CreateLinearChainGraph(int count)
    {
        var skills = Enumerable.Range(0, count)
            .Select(i => new FixedDurationPlannedSkill
            {
                Id = Guid.NewGuid(),
                PlannedDuration = 10.0 + i % 5 // Varying durations for realism
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
    ///     Tests parallelism handling where SCC can process independent components.
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
    ///     Creates a graph with adaptive skills that form cycles, requiring LP solving within SCCs.
    ///     This tests the scenario where SCC analysis is most beneficial.
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <param name="cycleSize">Size of each cycle (number of skills per cycle).</param>
    /// <returns>An execution graph with adaptive skill cycles.</returns>
    private static ExecutionGraph CreateAdaptiveCycleGraph(int count, int cycleSize = 4)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();

        var numCycles = count / cycleSize;
        var remaining = count % cycleSize;

        // Create cycles with adaptive skills
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

            // Link cycles together (if not the first cycle)
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

        // Add remaining skills as a linear chain
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
    ///     Creates a complex realistic graph with a mix of patterns: linear chains, parallel tasks,
    ///     and some adaptive cycles. Represents a typical real-world scheduling scenario.
    /// </summary>
    /// <param name="count">Number of skills to create.</param>
    /// <returns>An execution graph with mixed patterns.</returns>
    private static ExecutionGraph CreateMixedComplexGraph(int count)
    {
        var skills = new List<IPlannedSkillExecution>();
        var dependencies = new List<Dependency>();

        // 40% linear chain
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

        // 30% parallel independent tasks (no dependencies between them)
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

        // Connect parallel skills to the end of linear chain
        if (linearSkills.Count > 0)
            foreach (var parallelSkill in parallelSkills)
                dependencies.Add(new Dependency
                {
                    Id = Guid.NewGuid(),
                    Source = linearSkills[^1],
                    Target = parallelSkill,
                    Type = DependencyType.FinishToStart
                });

        // 30% adaptive cycle (for SCC testing)
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

            // Connect cycle to parallel tasks (fan-in)
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

    /// <summary>
    ///     Runs performance comparison between scheduling with SCC analysis and without.
    /// </summary>
    /// <param name="graphFactory">Factory function to create the test graph.</param>
    /// <param name="scenarioName">Name of the test scenario for reporting.</param>
    /// <param name="expectSccBenefit">Whether SCC approach is expected to be beneficial.</param>
    private void RunPerformanceComparison(
        Func<ExecutionGraph> graphFactory,
        string scenarioName,
        bool expectSccBenefit)
    {
        _output.WriteLine($"\n{'='.ToString().PadLeft(70, '=')}");
        _output.WriteLine($"SCENARIO: {scenarioName}");
        _output.WriteLine($"{'='.ToString().PadLeft(70, '=')}");

        var templateGraph = graphFactory();
        _output.WriteLine(
            $"Graph Configuration: {templateGraph.SkillExecutions.Count} skills, {templateGraph.Dependencies.Count} dependencies");
        _output.WriteLine($"Adaptive skills: {templateGraph.SkillExecutions.Count(s => s is AdaptivePlannedSkill)}");
        _output.WriteLine($"Fixed skills: {templateGraph.SkillExecutions.Count(s => s is FixedDurationPlannedSkill)}");

        // Warmup phase
        _output.WriteLine($"\nWarming up ({WarmupIterations} iterations)...");
        for (var i = 0; i < WarmupIterations; i++)
        {
            var warmupGraph1 = CloneGraph(templateGraph);
            var warmupGraph2 = CloneGraph(templateGraph);

            try
            {
                warmupGraph1.PlanSchedule();
            }
            catch (ScheduleInfeasibleException)
            {
                // Expected for some graphs with fixed cycles
            }

            try
            {
                warmupGraph2.SolveWithLinearProgramming();
            }
            catch (ScheduleInfeasibleException)
            {
                // Expected for some graphs
            }
        }

        // Measurement phase - With SCC
        var withSccTimes = new List<double>(MeasuredIterations);
        var withSccSuccessCount = 0;

        for (var i = 0; i < MeasuredIterations; i++)
        {
            var graph = CloneGraph(templateGraph);
            var sw = Stopwatch.StartNew();
            try
            {
                graph.PlanSchedule();
                sw.Stop();
                withSccTimes.Add(sw.Elapsed.TotalMicroseconds);
                withSccSuccessCount++;
            }
            catch (ScheduleInfeasibleException)
            {
                sw.Stop();
                withSccTimes.Add(sw.Elapsed.TotalMicroseconds);
            }
        }

        // Measurement phase - Without SCC (direct LP)
        var withoutSccTimes = new List<double>(MeasuredIterations);
        var withoutSccSuccessCount = 0;

        for (var i = 0; i < MeasuredIterations; i++)
        {
            var graph = CloneGraph(templateGraph);
            var sw = Stopwatch.StartNew();
            try
            {
                graph.SolveWithLinearProgramming();
                sw.Stop();
                withoutSccTimes.Add(sw.Elapsed.TotalMicroseconds);
                withoutSccSuccessCount++;
            }
            catch (ScheduleInfeasibleException)
            {
                sw.Stop();
                withoutSccTimes.Add(sw.Elapsed.TotalMicroseconds);
            }
        }

        // Calculate statistics
        var withSccMean = withSccTimes.Average();
        var withSccStdDev = CalculateStdDev(withSccTimes, withSccMean);
        var withSccMin = withSccTimes.Min();
        var withSccMax = withSccTimes.Max();
        var withSccMedian = CalculateMedian(withSccTimes);

        var withoutSccMean = withoutSccTimes.Average();
        var withoutSccStdDev = CalculateStdDev(withoutSccTimes, withoutSccMean);
        var withoutSccMin = withoutSccTimes.Min();
        var withoutSccMax = withoutSccTimes.Max();
        var withoutSccMedian = CalculateMedian(withoutSccTimes);

        // Report results
        _output.WriteLine($"\n--- Results ({MeasuredIterations} iterations) ---");
        _output.WriteLine("\nWITH SCC (PlanSchedule):");
        _output.WriteLine($"  Success rate: {withSccSuccessCount}/{MeasuredIterations}");
        _output.WriteLine($"  Mean:   {withSccMean,10:F2} us");
        _output.WriteLine($"  Median: {withSccMedian,10:F2} us");
        _output.WriteLine($"  StdDev: {withSccStdDev,10:F2} us");
        _output.WriteLine($"  Min:    {withSccMin,10:F2} us");
        _output.WriteLine($"  Max:    {withSccMax,10:F2} us");

        _output.WriteLine("\nWITHOUT SCC (SolveWithLinearProgramming):");
        _output.WriteLine($"  Success rate: {withoutSccSuccessCount}/{MeasuredIterations}");
        _output.WriteLine($"  Mean:   {withoutSccMean,10:F2} us");
        _output.WriteLine($"  Median: {withoutSccMedian,10:F2} us");
        _output.WriteLine($"  StdDev: {withoutSccStdDev,10:F2} us");
        _output.WriteLine($"  Min:    {withoutSccMin,10:F2} us");
        _output.WriteLine($"  Max:    {withoutSccMax,10:F2} us");

        // Analysis
        var speedupRatio = withoutSccMean / withSccMean;
        var absoluteDiff = withoutSccMean - withSccMean;
        var percentDiff = absoluteDiff / withoutSccMean * 100;

        _output.WriteLine("\n--- Analysis ---");
        _output.WriteLine(
            $"Absolute difference: {absoluteDiff:F2} us ({(absoluteDiff >= 0 ? "SCC faster" : "LP faster")})");
        _output.WriteLine(
            $"Percentage difference: {Math.Abs(percentDiff):F2}% ({(percentDiff >= 0 ? "SCC faster" : "LP faster")})");
        _output.WriteLine($"Speedup ratio: {speedupRatio:F3}x ({(speedupRatio > 1 ? "SCC faster" : "LP faster")})");

        // Statistical significance test (simple t-test approximation)
        var tStatistic = CalculateTStatistic(withSccTimes, withoutSccTimes);
        var isSignificant = Math.Abs(tStatistic) > 2.101; // t-critical for df=18, alpha=0.05 (two-tailed)

        _output.WriteLine($"\nt-statistic: {tStatistic:F3}");
        _output.WriteLine($"Statistically significant (alpha=0.05): {(isSignificant ? "YES" : "NO")}");

        // Performance rating
        string rating;
        if (Math.Abs(percentDiff) < 5)
            rating = "NEGLIGIBLE DIFFERENCE";
        else if (percentDiff > 0)
            rating = speedupRatio > 1.5 ? "SCC SIGNIFICANTLY FASTER" : "SCC MODERATELY FASTER";
        else
            rating = speedupRatio < 0.67 ? "LP SIGNIFICANTLY FASTER" : "LP MODERATELY FASTER";

        _output.WriteLine($"\nPerformance Rating: {rating}");

        // Verify expectation
        if (expectSccBenefit && speedupRatio < 0.9)
            _output.WriteLine("NOTE: SCC was expected to be beneficial but was slower in this run.");
    }

    /// <summary>
    ///     Calculates the standard deviation of a collection of values.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <param name="mean">The pre-calculated mean.</param>
    /// <returns>The standard deviation.</returns>
    private static double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count <= 1) return 0;
        var sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquaredDiffs / (values.Count - 1));
    }

    /// <summary>
    ///     Calculates the median of a collection of values.
    /// </summary>
    /// <param name="values">The values.</param>
    /// <returns>The median value.</returns>
    private static double CalculateMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var n = sorted.Count;
        return n % 2 == 0
            ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0
            : sorted[n / 2];
    }

    /// <summary>
    ///     Calculates an independent samples t-statistic for comparing two groups.
    /// </summary>
    /// <param name="group1">First group of samples.</param>
    /// <param name="group2">Second group of samples.</param>
    /// <returns>The t-statistic.</returns>
    private static double CalculateTStatistic(List<double> group1, List<double> group2)
    {
        var mean1 = group1.Average();
        var mean2 = group2.Average();
        var var1 = group1.Sum(v => (v - mean1) * (v - mean1)) / (group1.Count - 1);
        var var2 = group2.Sum(v => (v - mean2) * (v - mean2)) / (group2.Count - 1);

        var pooledStdErr = Math.Sqrt(var1 / group1.Count + var2 / group2.Count);
        return pooledStdErr > 0 ? (mean1 - mean2) / pooledStdErr : 0;
    }

    /// <summary>
    ///     Benchmark test for linear chain graph (40 skills in sequence).
    ///     In this scenario, all SCCs are trivial (single tasks), so SCC overhead is minimal.
    /// </summary>
    [Fact]
    public void LinearChain_40Skills_PerformanceComparison()
    {
        RunPerformanceComparison(
            () => CreateLinearChainGraph(SkillCount),
            "LINEAR CHAIN (40 skills, 39 dependencies, all trivial SCCs)",
            false);
    }

    /// <summary>
    ///     Benchmark test for parallel chains (4 chains of 10 skills each).
    ///     Tests parallelism and independent SCC handling.
    /// </summary>
    [Fact]
    public void ParallelChains_40Skills_PerformanceComparison()
    {
        RunPerformanceComparison(
            () => CreateParallelChainsGraph(SkillCount, 4),
            "PARALLEL CHAINS (4 chains x 10 skills, independent SCCs)",
            false);
    }

    /// <summary>
    ///     Benchmark test for adaptive cycles (10 cycles of 4 adaptive skills each).
    ///     This is the scenario where SCC analysis should provide the most benefit.
    /// </summary>
    [Fact]
    public void AdaptiveCycles_40Skills_PerformanceComparison()
    {
        RunPerformanceComparison(
            () => CreateAdaptiveCycleGraph(SkillCount),
            "ADAPTIVE CYCLES (10 cycles of 4 adaptive skills, linked together)",
            true);
    }

    /// <summary>
    ///     Benchmark test for mixed complex graph (realistic scenario).
    ///     40% linear, 30% parallel, 30% adaptive cycle.
    /// </summary>
    [Fact]
    public void MixedComplex_40Skills_PerformanceComparison()
    {
        RunPerformanceComparison(
            () => CreateMixedComplexGraph(SkillCount),
            "MIXED COMPLEX (40% linear, 30% parallel, 30% adaptive cycle)",
            true);
    }

    /// <summary>
    ///     Comprehensive benchmark running all scenarios and summarising results.
    /// </summary>
    [Fact]
    public void ComprehensiveBenchmark_AllScenarios_Summary()
    {
        _output.WriteLine("\n" + new string('*', 80));
        _output.WriteLine("COMPREHENSIVE SCC vs NO-SCC PERFORMANCE BENCHMARK");
        _output.WriteLine(
            $"Configuration: {SkillCount} skills, {WarmupIterations} warmup, {MeasuredIterations} measured iterations");
        _output.WriteLine(new string('*', 80));

        // Run all scenarios
        LinearChain_40Skills_PerformanceComparison();
        ParallelChains_40Skills_PerformanceComparison();
        AdaptiveCycles_40Skills_PerformanceComparison();
        MixedComplex_40Skills_PerformanceComparison();

        _output.WriteLine("\n" + new string('*', 80));
        _output.WriteLine("BENCHMARK COMPLETE");
        _output.WriteLine(new string('*', 80));

        _output.WriteLine(@"
INTERPRETATION GUIDE:
- For graphs WITHOUT cycles: SCC overhead is minimal but not beneficial
- For graphs WITH adaptive cycles: SCC allows targeted LP solving, potentially faster
- For mixed graphs: Results depend on the proportion and size of cycles
- Statistical significance (t-test): p < 0.05 indicates real difference, not noise

EXPECTED PATTERNS:
1. Linear Chain: Similar performance (SCC adds minimal overhead)
2. Parallel Chains: Similar performance (independent trivial SCCs)
3. Adaptive Cycles: SCC may be faster (solves smaller LP problems)
4. Mixed Complex: Varies based on cycle proportion
");
    }
}