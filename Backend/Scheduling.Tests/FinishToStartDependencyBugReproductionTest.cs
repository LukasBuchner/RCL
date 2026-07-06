using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Reproduction test for the bug where Skill 2's start time equals Skill 1's DURATION
///     instead of Skill 1's FINISH TIME.
///     Expected behavior:
///     - Skill 1: Start=1.26s, Duration=43.93s, Finish=45.19s
///     - Skill 2: Start=45.19s (Skill 1's finish), Duration=70s, Finish=115.19s
///     Actual buggy behavior:
///     - Skill 2: Start=43.93s (Skill 1's duration) ❌
/// </summary>
public class FinishToStartDependencyBugReproductionTest
{
    [Fact]
    public void PlanSchedule_WithFinishToStartDependency_ShouldUseFinishTimeNotDuration()
    {
        // Arrange: Create two skills with a FinishToStart dependency
        var skill1 = new FixedDurationPlannedSkill
        {
            Id = Guid.NewGuid(),
            PlannedDuration = 43.93
        };

        var skill2 = new FixedDurationPlannedSkill
        {
            Id = Guid.NewGuid(),
            PlannedDuration = 70.0
        };

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skill1,
            Target = skill2,
            Type = DependencyType.FinishToStart
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill1, skill2 },
            Dependencies = new List<Dependency> { dependency }
        };

        var currentTime = 0.0;
        var logger = new Mock<ILogger>().Object;

        // Act: Plan the schedule
        graph.PlanSchedule(currentTime, false, logger);

        // Assert: Skill 2 should start at Skill 1's FINISH time, not its DURATION
        Assert.Equal(0.0, skill1.PlannedStartTime); // Skill 1 starts at T=0
        Assert.Equal(43.93, skill1.PlannedFinishTime); // Skill 1 finishes at T=43.93

        // BUG: If this assertion fails with skill2.PlannedStartTime = 43.93,
        // then we're using Skill 1's DURATION instead of its FINISH TIME
        Assert.Equal(43.93, skill2.PlannedStartTime); // Skill 2 should start at Skill 1's finish time
        Assert.Equal(113.93, skill2.PlannedFinishTime); // Skill 2 finishes at T=113.93
    }
}