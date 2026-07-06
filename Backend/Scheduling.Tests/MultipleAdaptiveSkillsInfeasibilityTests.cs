using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Tests to reproduce and verify the hypothesis that INFEASIBLE errors occur specifically
///     when MULTIPLE adaptive skills (with variable Min/Max durations) are involved,
///     particularly when they have dependency relationships.
///     Hypothesis: OR-Tools GLOP solver may have difficulty when BOTH the source and target
///     of a dependency are adaptive (have duration variables with Min/Max bounds).
/// </summary>
public class MultipleAdaptiveSkillsInfeasibilityTests
{
    /// <summary>
    ///     Scenario A: Two adaptive skills with FinishToStart dependency (both adaptive).
    ///     This reproduces the real-world scenario from logs:
    ///     - Robot skill: Nominal=65s, Min=45s, Max=95s
    ///     - Alice skill: Nominal=85s, Min=65s, Max=120s
    ///     - FinishToStart dependency between them
    ///     EXPECTED: Should be feasible. Robot finishes between [45, 95], Alice starts after that.
    ///     ACTUAL (if bug exists): INFEASIBLE solver status
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_WithFinishToStartDependency_ShouldBeFeasible()
    {
        // Arrange: Create two adaptive skills similar to the real scenario
        var robotSkill = CreateAdaptiveSkill(45.0);

        var aliceSkill = CreateAdaptiveSkill(65.0);

        // FinishToStart: Alice cannot start until Robot finishes
        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = robotSkill,
            Target = aliceSkill,
            Type = DependencyType.FinishToStart
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { robotSkill, aliceSkill },
            Dependencies = new List<Dependency> { dependency }
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert: Should NOT throw ScheduleInfeasibleException
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception); // No exception should be thrown

        // Verify the schedule is logical
        Assert.True(robotSkill.PlannedStartTime >= 0, "Robot skill should start at or after time 0");
        Assert.True(robotSkill.PlannedDuration is >= 45.0 and <= 95.0,
            "Robot duration should be within [45, 95]");
        Assert.Equal(robotSkill.PlannedStartTime + robotSkill.PlannedDuration, robotSkill.PlannedFinishTime);

        // Alice must start at or after Robot's finish time
        Assert.True(aliceSkill.PlannedStartTime >= robotSkill.PlannedFinishTime,
            $"Alice should start ({aliceSkill.PlannedStartTime:F3}s) at or after Robot finishes ({robotSkill.PlannedFinishTime:F3}s)");
        Assert.True(aliceSkill.PlannedDuration is >= 65.0 and <= 120.0,
            "Alice duration should be within [65, 120]");
        Assert.Equal(aliceSkill.PlannedStartTime + aliceSkill.PlannedDuration, aliceSkill.PlannedFinishTime);
    }

    /// <summary>
    ///     Scenario B: One adaptive skill and one fixed-duration skill with FinishToStart dependency.
    ///     This serves as a control test - if ONLY one skill is adaptive, does it work?
    ///     EXPECTED: Should definitely be feasible (simpler constraint setup)
    /// </summary>
    [Fact]
    public void OneAdaptive_OneFixed_WithFinishToStartDependency_ShouldBeFeasible()
    {
        // Arrange: First skill is adaptive, second is fixed
        var adaptiveSkill = CreateAdaptiveSkill(45.0);

        var fixedSkill = CreateFixedSkill(70.0);

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = adaptiveSkill,
            Target = fixedSkill,
            Type = DependencyType.FinishToStart
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { adaptiveSkill, fixedSkill },
            Dependencies = new List<Dependency> { dependency }
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);

        // Verify schedule
        Assert.True(adaptiveSkill.PlannedDuration is >= 45.0 and <= 95.0);
        Assert.Equal(70.0, fixedSkill.PlannedDuration);
        Assert.True(fixedSkill.PlannedStartTime >= adaptiveSkill.PlannedFinishTime);
    }

    /// <summary>
    ///     Scenario C: Two adaptive skills with NO dependencies (parallel execution).
    ///     This tests if the issue is specific to dependencies between adaptive skills.
    ///     EXPECTED: Should be feasible (no constraints between them)
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_NoDependencies_ShouldBeFeasible()
    {
        // Arrange: Two independent adaptive skills
        var skill1 = CreateAdaptiveSkill(45.0);

        var skill2 = CreateAdaptiveSkill(65.0);

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill1, skill2 },
            Dependencies = new List<Dependency>() // No dependencies
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);

        // Both should be scheduled (possibly in parallel)
        Assert.True(skill1.PlannedDuration is >= 45.0 and <= 95.0);
        Assert.True(skill2.PlannedDuration is >= 65.0 and <= 120.0);
    }

    /// <summary>
    ///     Scenario D: Two adaptive skills with StartToStart dependency.
    ///     Tests if the dependency TYPE matters when both skills are adaptive.
    ///     EXPECTED: Should be feasible
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_WithStartToStartDependency_ShouldBeFeasible()
    {
        // Arrange
        var skill1 = CreateAdaptiveSkill(45.0);

        var skill2 = CreateAdaptiveSkill(65.0);

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skill1,
            Target = skill2,
            Type = DependencyType.StartToStart // Both start at same time or skill2 after skill1
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill1, skill2 },
            Dependencies = new List<Dependency> { dependency }
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);

        // skill2 should start at or after skill1 starts
        Assert.True(skill2.PlannedStartTime >= skill1.PlannedStartTime);
    }

    /// <summary>
    ///     Scenario E: Three adaptive skills in a chain (A -> B -> C).
    ///     Tests if multiple adaptive-to-adaptive dependencies cause issues.
    ///     EXPECTED: Should be feasible
    /// </summary>
    [Fact]
    public void ThreeAdaptiveSkills_InChain_ShouldBeFeasible()
    {
        // Arrange
        var skillA = CreateAdaptiveSkill(30.0);
        var skillB = CreateAdaptiveSkill(45.0);
        var skillC = CreateAdaptiveSkill(65.0);

        var depAtoB = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skillA,
            Target = skillB,
            Type = DependencyType.FinishToStart
        };

        var depBtoC = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skillB,
            Target = skillC,
            Type = DependencyType.FinishToStart
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skillA, skillB, skillC },
            Dependencies = new List<Dependency> { depAtoB, depBtoC }
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);

        // Verify chain: A finishes before B starts, B finishes before C starts
        Assert.True(skillB.PlannedStartTime >= skillA.PlannedFinishTime);
        Assert.True(skillC.PlannedStartTime >= skillB.PlannedFinishTime);
    }

    /// <summary>
    ///     Scenario F: Two adaptive skills with FinishToFinish dependency.
    ///     EXPECTED: Should be feasible
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_WithFinishToFinishDependency_ShouldBeFeasible()
    {
        // Arrange
        var skill1 = CreateAdaptiveSkill(45.0);
        var skill2 = CreateAdaptiveSkill(65.0);

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = skill1,
            Target = skill2,
            Type = DependencyType.FinishToFinish
        };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<IPlannedSkillExecution> { skill1, skill2 },
            Dependencies = new List<Dependency> { dependency }
        };

        var currentTime = 0.0;
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);

        // skill2 should finish at or after skill1 finishes
        Assert.True(skill2.PlannedFinishTime >= skill1.PlannedFinishTime);
    }

    /// <summary>
    ///     Scenario G: Edge case - Adaptive skill with very narrow bounds (min ≈ max).
    ///     Tests if the issue is related to the range of variability.
    ///     EXPECTED: Should be feasible
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_WithNarrowBounds_ShouldBeFeasible()
    {
        // Arrange: Very narrow bounds (almost fixed)
        var skill1 = CreateAdaptiveSkill(50.0);
        var skill2 = CreateAdaptiveSkill(70.0);

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
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);
    }

    /// <summary>
    ///     Scenario H: Adaptive skills with very wide bounds.
    ///     Tests if large variability ranges cause issues.
    ///     EXPECTED: Should be feasible
    /// </summary>
    [Fact]
    public void TwoAdaptiveSkills_WithWideBounds_ShouldBeFeasible()
    {
        // Arrange: Very wide bounds
        var skill1 = CreateAdaptiveSkill(10.0);
        var skill2 = CreateAdaptiveSkill(5.0);

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
        var logger = CreateTestLogger();

        // Act & Assert
        var exception = Record.Exception(() => graph.PlanSchedule(currentTime, false, logger));

        Assert.Null(exception);
    }

    #region Helper Methods

    /// <summary>
    ///     Creates an adaptive skill execution for testing.
    /// </summary>
    private static AdaptivePlannedSkill CreateAdaptiveSkill(double minDuration)
    {
        return new AdaptivePlannedSkill
        {
            Id = Guid.NewGuid(),
            MinDuration = minDuration,
            PlannedDuration = minDuration, // Initial value
            PlannedStartTime = 0.0,
            PlannedFinishTime = 0.0
        };
    }

    /// <summary>
    ///     Creates a fixed-duration skill execution for testing.
    /// </summary>
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
    ///     Creates a test logger that outputs to the test console.
    /// </summary>
    private static ILogger CreateTestLogger()
    {
        var mockLogger = new Mock<ILogger>();

        // Setup logging to capture trace output for debugging
        mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var logLevel = (LogLevel)invocation.Arguments[0];
                var formatter = invocation.Arguments[4];
                var message = formatter?.GetType()
                    .GetMethod("Invoke")?
                    .Invoke(formatter, [invocation.Arguments[2], invocation.Arguments[3]]);

                // Only output if it's a warning or error during tests
                if (logLevel >= LogLevel.Warning) Console.WriteLine($"[{logLevel}] {message}");
            }));

        return mockLogger.Object;
    }

    #endregion
}