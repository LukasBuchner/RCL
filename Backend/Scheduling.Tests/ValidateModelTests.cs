using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Tests for the structural invariants enforced by
///     <c>ExecutionGraphExtensions.ValidateModel</c> at scheduling time.
///     ValidateModel is invoked from <see cref="ExecutionGraphExtensions.PlanSchedule" />,
///     so tests exercise it indirectly via <c>graph.PlanSchedule(...)</c> and assert that
///     a <see cref="ScheduleModelException" /> is thrown when an invariant is violated.
/// </summary>
public class ValidateModelTests
{
    /// <summary>
    ///     A dependency whose source and target reference the same task is structurally
    ///     malformed and must be rejected before scheduling begins.
    /// </summary>
    [Fact]
    public void ValidateModel_SelfLoopDependency_ThrowsScheduleModelException()
    {
        var skill = CreateFixedSkill(5.0);
        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skill,
            Target = skill,
            Type = DependencyType.FinishToStart
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill },
            Dependencies = new List<Dependency> { dependency }
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Self-loop", exception.Message);
        Assert.Contains(skill.Id.ToString(), exception.Message);
    }

    /// <summary>
    ///     Two dependencies sharing the same (source, target, type) triple are semantic
    ///     duplicates and must be rejected. Same (source, target) with different types is
    ///     legal (covered by <see cref="ValidateModel_DistinctTypesOnSamePair_NoException" />).
    /// </summary>
    [Fact]
    public void ValidateModel_DuplicateEdge_ThrowsScheduleModelException()
    {
        var skillA = CreateFixedSkill(5.0);
        var skillB = CreateFixedSkill(5.0);
        var dep1 = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skillA,
            Target = skillB,
            Type = DependencyType.FinishToStart
        };
        var dep2 = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skillA,
            Target = skillB,
            Type = DependencyType.FinishToStart
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = new List<Dependency> { dep1, dep2 }
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Duplicate dependency edge", exception.Message);
    }

    /// <summary>
    ///     A pair of dependencies between the same two skills but with distinct
    ///     <see cref="DependencyType" /> values is a legitimate coupling (e.g., SS+FF).
    ///     It must pass <c>ValidateModel</c> and be scheduled successfully.
    /// </summary>
    [Fact]
    public void ValidateModel_DistinctTypesOnSamePair_NoException()
    {
        var skillA = CreateFixedSkill(2.0);
        var skillB = CreateFixedSkill(2.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.FinishToFinish
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Record.Exception(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Null(exception);
    }

    /// <summary>
    ///     A pure StartToStart cycle (SS A→B, SS B→A) is event-level cyclic and would
    ///     deadlock at runtime even though the LP can satisfy it as S_A == S_B.
    ///     <c>ValidateModel</c> rejects it as a structural error.
    /// </summary>
    [Fact]
    public void ValidateModel_PureSsCycle_ThrowsScheduleModelException()
    {
        var skillA = CreateFixedSkill(2.0);
        var skillB = CreateFixedSkill(2.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillB, Target = skillA,
                Type = DependencyType.StartToStart
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     A pure FinishToFinish cycle (FF A→B, FF B→A) is event-level cyclic and must
    ///     be rejected for the same reason as the SS variant.
    /// </summary>
    [Fact]
    public void ValidateModel_PureFfCycle_ThrowsScheduleModelException()
    {
        var skillA = CreateFixedSkill(2.0);
        var skillB = CreateFixedSkill(2.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.FinishToFinish
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillB, Target = skillA,
                Type = DependencyType.FinishToFinish
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     A mixed cycle FS A→B + SS B→A is event-level cyclic only through the implicit
    ///     duration-linkage edge (A,Start)→(A,Finish). This test proves the linkage edges
    ///     are wired into the lifted graph used by the cycle check.
    /// </summary>
    [Fact]
    public void ValidateModel_FsThenSsCycle_ThrowsScheduleModelException()
    {
        var skillA = CreateFixedSkill(2.0);
        var skillB = CreateFixedSkill(2.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.FinishToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillB, Target = skillA,
                Type = DependencyType.StartToStart
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     SS A→B paired with FF B→A is event-level acyclic — the longest path is
    ///     (A,S)→(B,S)→(B,F)→(A,F), which terminates. This is a legitimate coupled
    ///     SCC and must pass validation.
    /// </summary>
    [Fact]
    public void ValidateModel_SsFfCoupling_NoException()
    {
        var skillA = CreateFixedSkill(3.0);
        var skillB = CreateFixedSkill(1.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillB, Target = skillA,
                Type = DependencyType.FinishToFinish
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Record.Exception(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Null(exception);
    }

    /// <summary>
    ///     An SS forward chain closed by a single FF wraparound forms an SCC that is
    ///     event-level acyclic, so <c>ValidateModel</c> accepts it and the LP solver
    ///     can satisfy the constraints by choosing per-task durations within the
    ///     adaptive bounds. The FF wraparound forces all skills in the ring to share a
    ///     common finish time, which is the canonical "AdaptiveCycle" workload.
    /// </summary>
    [Fact]
    public void ValidateModel_AdaptiveSsChainWithFfWraparound_NoExceptionAndCommonFinish()
    {
        const double minDuration = 5.0;
        var skills = new[]
        {
            CreateAdaptiveSkill(minDuration),
            CreateAdaptiveSkill(minDuration),
            CreateAdaptiveSkill(minDuration),
            CreateAdaptiveSkill(minDuration)
        };
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skills[0], Target = skills[1],
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skills[1], Target = skills[2],
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skills[2], Target = skills[3],
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skills[3], Target = skills[0],
                Type = DependencyType.FinishToFinish
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = skills.Cast<IPlannedSkillExecution>().ToList(),
            Dependencies = dependencies
        };

        var exception = Record.Exception(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Null(exception);
        foreach (var s in skills)
        {
            Assert.True(s.PlannedDuration >= minDuration);
            Assert.Equal(s.PlannedStartTime + s.PlannedDuration, s.PlannedFinishTime, 5);
        }

        var finishTimes = skills.Select(s => s.PlannedFinishTime).ToArray();
        Assert.Equal(finishTimes.Max(), finishTimes.Min(), 5);
    }

    /// <summary>
    ///     An FS cycle is the original FixedCycle case from the legacy classifier. After
    ///     consolidation it is rejected by <c>ValidateModel</c> (the FS-only cycle is
    ///     also event-level cyclic) before SCC analysis runs.
    /// </summary>
    [Fact]
    public void ValidateModel_FsCycle_RejectedByValidateModel()
    {
        var skillA = CreateFixedSkill(1.0);
        var skillB = CreateFixedSkill(1.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = skillA, Target = skillB,
                Type = DependencyType.FinishToStart
            },
            new()
            {
                Id = Guid.NewGuid(), Source = skillB, Target = skillA,
                Type = DependencyType.FinishToStart
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB },
            Dependencies = dependencies
        };

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, CreateTestLogger()));

        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     Creates an adaptive skill execution for testing.
    /// </summary>
    /// <param name="minDuration">Minimum allowable duration.</param>
    /// <returns>A new <see cref="AdaptivePlannedSkill" /> instance.</returns>
    private static AdaptivePlannedSkill CreateAdaptiveSkill(double minDuration)
    {
        return new AdaptivePlannedSkill
        {
            Id = Guid.NewGuid(),
            MinDuration = minDuration,
            PlannedDuration = minDuration,
            PlannedStartTime = 0.0,
            PlannedFinishTime = 0.0
        };
    }

    /// <summary>
    ///     Creates a fixed-duration skill execution for testing.
    /// </summary>
    /// <param name="duration">The fixed duration.</param>
    /// <returns>A new <see cref="FixedDurationPlannedSkill" /> instance.</returns>
    private static FixedDurationPlannedSkill CreateFixedSkill(double duration)
    {
        return new FixedDurationPlannedSkill
        {
            Id = Guid.NewGuid(),
            PlannedDuration = duration,
            PlannedStartTime = 0.0,
            PlannedFinishTime = 0.0
        };
    }

    /// <summary>
    ///     Creates a no-op logger so PlanSchedule can be invoked from tests.
    /// </summary>
    /// <returns>A mocked <see cref="ILogger" /> that swallows all output.</returns>
    private static ILogger CreateTestLogger()
    {
        var mockLogger = new Mock<ILogger>();
        return mockLogger.Object;
    }
}