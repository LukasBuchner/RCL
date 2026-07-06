using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Tests for adaptive skills after the removal of the maximum-duration cap. An adaptive
///     skill is now defined solely by <see cref="AdaptivePlannedSkill.MinDuration" />; its
///     duration is unbounded above, so the LP can stretch it arbitrarily to satisfy coupling
///     constraints, and validation no longer rejects large minimums.
/// </summary>
public class AdaptiveDurationTests
{
    /// <summary>
    ///     A lone adaptive skill schedules to its minimum duration with no upper-bound concept.
    /// </summary>
    [Fact]
    public void PlanSchedule_LoneAdaptiveSkill_UsesMinDuration()
    {
        var skill = CreateAdaptiveSkill(5.0);
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill },
            Dependencies = new List<Dependency>()
        };

        var exception = Record.Exception(() => graph.PlanSchedule(0.0, false, NullLogger.Instance));

        Assert.Null(exception);
        Assert.Equal(5.0, skill.PlannedDuration, 5);
    }

    /// <summary>
    ///     A running adaptive skill (start pinned at its actual start) coupled by FinishToFinish
    ///     to a long fixed task is forced to stretch its duration far past any value that would
    ///     previously have been a "maximum". Because the start is fixed and the finish is pulled
    ///     out by the coupling, the only feasible duration is large; the unbounded duration
    ///     variable makes this solvable where a 20s cap would have been infeasible.
    /// </summary>
    [Fact]
    public void PlanSchedule_RunningAdaptiveCoupledToLongTask_StretchesDurationPastFormerCeiling()
    {
        const double currentTime = 1.0;
        var runningAdaptive = CreateRunningAdaptiveSkill(5.0, 0.0);
        var longFixed = CreateFixedSkill(100.0);
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(), Source = longFixed, Target = runningAdaptive,
                Type = DependencyType.FinishToFinish
            }
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { runningAdaptive, longFixed },
            Dependencies = dependencies
        };

        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, NullLogger.Instance));

        Assert.Null(exception);
        // Start pinned at 0, finish pulled to the long task's finish (>= 100), so the duration
        // is ~100s — far beyond the old 20s "max" and infeasible under the former cap.
        Assert.True(runningAdaptive.PlannedDuration >= 95.0,
            $"Expected the adaptive duration to stretch past 95s, got {runningAdaptive.PlannedDuration}s.");
        Assert.Equal(runningAdaptive.PlannedStartTime + runningAdaptive.PlannedDuration,
            runningAdaptive.PlannedFinishTime, 5);
        Assert.True(runningAdaptive.PlannedFinishTime >= longFixed.PlannedFinishTime - 1e-6);
    }

    /// <summary>
    ///     A large minimum duration that would previously have exceeded a maximum bound is now
    ///     valid: there is no upper bound to violate, so <c>ValidateModel</c> accepts it.
    /// </summary>
    [Fact]
    public void PlanSchedule_LargeMinDuration_NoLongerRejected()
    {
        var skill = CreateAdaptiveSkill(500.0);
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill },
            Dependencies = new List<Dependency>()
        };

        var exception = Record.Exception(() => graph.PlanSchedule(0.0, false, NullLogger.Instance));

        Assert.Null(exception);
        Assert.Equal(500.0, skill.PlannedDuration, 5);
    }

    /// <summary>
    ///     A negative minimum duration is still rejected by <c>ValidateModel</c>.
    /// </summary>
    [Fact]
    public void PlanSchedule_NegativeMinDuration_ThrowsScheduleModelException()
    {
        var skill = new AdaptivePlannedSkill
        {
            Id = Guid.NewGuid(),
            MinDuration = -1.0,
            PlannedDuration = 0.0,
            PlannedStartTime = 0.0,
            PlannedFinishTime = 0.0
        };
        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill },
            Dependencies = new List<Dependency>()
        };

        var exception =
            Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(0.0, false, NullLogger.Instance));
        Assert.Contains("negative MinDuration", exception.Message);
    }

    /// <summary>
    ///     Creates an adaptive skill execution with only a minimum duration.
    /// </summary>
    /// <param name="minDuration">Minimum allowable duration; the duration is unbounded above.</param>
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
    ///     Creates a fixed-duration skill execution.
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
    ///     Creates a running adaptive skill execution whose start is pinned to its actual start.
    /// </summary>
    /// <param name="minDuration">Minimum allowable duration; the duration is unbounded above.</param>
    /// <param name="actualStartTime">The actual start time, which the solver pins the start to.</param>
    /// <returns>A mocked running <see cref="IAdaptiveSkillExecution" />.</returns>
    private static IAdaptiveSkillExecution CreateRunningAdaptiveSkill(double minDuration, double actualStartTime)
    {
        var mock = new Mock<IAdaptiveSkillExecution>();
        mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mock.SetupProperty(t => t.PlannedStartTime, actualStartTime);
        mock.SetupProperty(t => t.PlannedFinishTime, actualStartTime + minDuration);
        mock.SetupProperty(t => t.PlannedDuration, minDuration);
        mock.SetupGet(t => t.MinDuration).Returns(minDuration);
        mock.SetupGet(t => t.ActualStartTime).Returns(actualStartTime);
        mock.SetupGet(t => t.ActualFinishTime).Returns((double?)null);
        mock.SetupGet(t => t.EstimatedDuration).Returns(minDuration);
        mock.SetupGet(t => t.IsRunning).Returns(true);
        mock.SetupGet(t => t.IsFinished).Returns(false);
        return mock.Object;
    }
}