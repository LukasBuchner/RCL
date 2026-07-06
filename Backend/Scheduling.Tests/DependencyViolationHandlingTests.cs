using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Tests for handling dependency violations when re-scheduling skills that are already running or finished.
///     When skills are already executing, their start times are fixed and cannot be changed.
///     If they started in the wrong order (violating dependencies), we cannot change the past.
///     The solver should skip these constraints and log warnings instead of failing with INFEASIBLE.
///     This addresses GitHub issue #36.
/// </summary>
public class DependencyViolationHandlingTests
{
    private static ExecutionGraph Graph(IEnumerable<IPlannedSkillExecution> tasks, IEnumerable<Dependency> deps)
    {
        return new ExecutionGraph
        {
            SkillExecutions = tasks.ToList(),
            Dependencies = deps.ToList()
        };
    }

    /// <summary>
    ///     Helper to create a fixed ISkillExecution
    /// </summary>
    private static ISkillExecution CreateFixedSkill(
        string? name = null,
        double initialPlannedDuration = 1.0,
        double? actualStartTime = null,
        double? actualFinishTime = null,
        double? estimatedDuration = null)
    {
        var mock = new Mock<ISkillExecution>();
        mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mock.SetupProperty(t => t.PlannedStartTime, 0);
        mock.SetupProperty(t => t.PlannedFinishTime, 0);
        mock.As<IPlannedSkillExecution>().SetupProperty(p => p.PlannedDuration, initialPlannedDuration);

        mock.SetupGet(t => t.ActualStartTime).Returns(actualStartTime);
        mock.SetupGet(t => t.ActualFinishTime).Returns(actualFinishTime);
        mock.SetupGet(t => t.EstimatedDuration).Returns(estimatedDuration);

        // Calculated properties from ISkillExecution
        mock.SetupGet(t => t.IsRunning).Returns(actualStartTime.HasValue && !actualFinishTime.HasValue);
        mock.SetupGet(t => t.IsFinished).Returns(actualStartTime.HasValue && actualFinishTime.HasValue);
        mock.SetupGet(t => t.ActualDuration).Returns(
            actualStartTime.HasValue && actualFinishTime.HasValue
                ? actualFinishTime - actualStartTime
                : null);

        if (name is not null)
            mock.Setup(t => t.ToString()).Returns(name);
        return mock.Object;
    }

    /// <summary>
    ///     Helper to create an adaptive IAdaptiveSkillExecution
    /// </summary>
    private static IAdaptiveSkillExecution CreateAdaptiveSkill(
        string? name = null,
        double minDuration = 1.0,
        double? actualStartTime = null,
        double? actualFinishTime = null,
        double? estimatedDuration = null)
    {
        var mock = new Mock<IAdaptiveSkillExecution>();
        mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mock.SetupProperty(t => t.PlannedStartTime, double.NaN);
        mock.SetupProperty(t => t.PlannedFinishTime, double.NaN);
        mock.SetupProperty(t => t.PlannedDuration, minDuration);

        mock.SetupGet(t => t.MinDuration).Returns(minDuration);

        mock.SetupGet(t => t.ActualStartTime).Returns(actualStartTime);
        mock.SetupGet(t => t.ActualFinishTime).Returns(actualFinishTime);
        mock.SetupGet(t => t.EstimatedDuration).Returns(estimatedDuration);

        // Calculated properties
        mock.SetupGet(t => t.IsRunning).Returns(actualStartTime.HasValue && !actualFinishTime.HasValue);
        mock.SetupGet(t => t.IsFinished).Returns(actualStartTime.HasValue && actualFinishTime.HasValue);
        mock.SetupGet(t => t.ActualDuration).Returns(
            actualStartTime.HasValue && actualFinishTime.HasValue
                ? actualFinishTime - actualStartTime
                : null);

        if (name is not null)
            mock.Setup(t => t.ToString()).Returns(name);
        return mock.Object;
    }

    /// <summary>
    ///     Creates a mock logger that captures log messages for verification.
    /// </summary>
    private static Mock<ILogger> CreateMockLogger()
    {
        var mockLogger = new Mock<ILogger>();

        // Enable IsEnabled for all log levels
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        return mockLogger;
    }

    /// <summary>
    ///     Test 1: Both skills running with StartToStart violation
    ///     Source started at 0.704s, Target started at 0.497s
    ///     StartToStart means Target should start >= Source.Start
    ///     0.497 >= 0.704 is FALSE -> Violation
    ///     Should skip constraint and log WARNING
    /// </summary>
    [Fact]
    public void BothSkillsRunning_WithStartToStartViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 1.0;
        var sourceSkill = CreateFixedSkill("Alice", 10.0, 0.704); // Running
        var targetSkill = CreateFixedSkill("Robot", 10.0, 0.497); // Running, started BEFORE source

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart // Target.Start >= Source.Start
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act - should NOT throw ScheduleInfeasibleException
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception); // Should succeed

        // Verify warning was logged about the violation
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning about the dependency violation");
    }

    /// <summary>
    ///     Test 2: Both skills running with FinishToStart violation
    ///     Source finishes at 5.0s, Target started at 2.0s
    ///     FinishToStart means Target should start >= Source.Finish
    ///     2.0 >= 5.0 is FALSE -> Violation
    ///     Should skip constraint and log WARNING
    /// </summary>
    [Fact]
    public void BothSkillsRunning_WithFinishToStartViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0, 2.0, 5.0); // Finished
        var targetSkill = CreateFixedSkill("Target", 5.0, 2.0); // Running, started before source finished

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToStart // Target.Start >= Source.Finish
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning about the dependency violation");
    }

    /// <summary>
    ///     Test 3: One skill running, one pending
    ///     Should apply constraint normally (no violation possible yet)
    /// </summary>
    [Fact]
    public void OneSkillRunning_OnePending_ShouldApplyConstraintNormally()
    {
        // Arrange
        var currentTime = 5.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0, 2.0); // Running since 2.0
        var targetSkill = CreateFixedSkill("Target", 2.0); // Not started yet

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart // Target.Start >= Source.Start
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Target should be scheduled to respect the dependency
        Assert.True(targetSkill.PlannedStartTime >= sourceSkill.ActualStartTime!.Value,
            $"Target start ({targetSkill.PlannedStartTime}) should be >= Source start ({sourceSkill.ActualStartTime})");

        // Should NOT log a violation warning (constraint was applied normally)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Should NOT log a violation warning when one skill is pending");
    }

    /// <summary>
    ///     Test 4: Both skills pending
    ///     Should apply constraint normally (standard case)
    /// </summary>
    [Fact]
    public void BothSkillsPending_ShouldApplyConstraintNormally()
    {
        // Arrange
        var currentTime = 0.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0); // Not started
        var targetSkill = CreateFixedSkill("Target", 2.0); // Not started

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToStart
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Verify dependency is respected
        Assert.True(targetSkill.PlannedStartTime >= sourceSkill.PlannedFinishTime,
            $"Target start ({targetSkill.PlannedStartTime}) should be >= Source finish ({sourceSkill.PlannedFinishTime})");

        // No violation warning
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    ///     Test 5: Both skills finished with FinishToFinish violation
    ///     Source finished at 8.0s, Target finished at 5.0s
    ///     FinishToFinish means Target.Finish >= Source.Finish
    ///     5.0 >= 8.0 is FALSE -> Violation
    /// </summary>
    [Fact]
    public void BothSkillsFinished_WithFinishToFinishViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0, 5.0, 8.0); // Finished
        var targetSkill = CreateFixedSkill("Target", 3.0, 2.0, 5.0); // Finished earlier

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToFinish // Target.Finish >= Source.Finish
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning about the FinishToFinish violation");
    }

    /// <summary>
    ///     Test 6: Both skills finished with StartToFinish violation
    ///     Source started at 5.0s, Target finished at 3.0s
    ///     StartToFinish means Target.Finish >= Source.Start
    ///     3.0 >= 5.0 is FALSE -> Violation
    /// </summary>
    [Fact]
    public void BothSkillsFinished_WithStartToFinishViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0, 5.0, 8.0); // Started at 5.0
        var targetSkill = CreateFixedSkill("Target", 3.0, 0.0, 3.0); // Finished at 3.0

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToFinish // Target.Finish >= Source.Start
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning about the StartToFinish violation");
    }

    /// <summary>
    ///     Test 7: Both adaptive skills running with StartToStart violation
    ///     Tests that the fix works for adaptive skills as well
    /// </summary>
    [Fact]
    public void BothAdaptiveSkillsRunning_WithStartToStartViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 2.0;
        var sourceSkill = CreateAdaptiveSkill("AdaptiveSource", 5.0, 1.5); // Running
        var targetSkill = CreateAdaptiveSkill("AdaptiveTarget", 3.0, 0.8); // Running, started before source

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning for adaptive skills with dependency violation");
    }

    /// <summary>
    ///     Test 8: Both skills running but NO violation
    ///     Should skip constraint (both already executed) but NOT log warning
    /// </summary>
    [Fact]
    public void BothSkillsRunning_WithNoViolation_ShouldSkipConstraintWithoutWarning()
    {
        // Arrange
        var currentTime = 5.0;
        var sourceSkill = CreateFixedSkill("Source", 3.0, 1.0); // Running since 1.0
        var targetSkill = CreateFixedSkill("Target", 2.0, 2.0); // Running since 2.0

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart // Target.Start >= Source.Start -> 2.0 >= 1.0 -> TRUE (no violation)
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Should NOT log a violation warning (no violation occurred)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Should NOT log a violation warning when there is no violation");
    }

    /// <summary>
    ///     Test 9: Real-world scenario from GitHub issue #36
    ///     Alice started at 0.497s, Robot started at 0.704s
    ///     StartToStart dependency from Alice to Robot
    ///     This was causing INFEASIBLE error
    /// </summary>
    [Fact]
    public void RealWorldScenario_AliceAndRobot_ShouldNotFailWithInfeasible()
    {
        // Arrange - Exact scenario from issue
        var currentTime = 1.0;
        var aliceSkill = CreateFixedSkill("Alice", 85.0, 0.497); // Running
        var robotSkill = CreateFixedSkill("Robot", 65.0, 0.704); // Running

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = aliceSkill,
            Target = robotSkill,
            Type = DependencyType
                .StartToStart // Robot.Start >= Alice.Start -> 0.704 >= 0.497 -> TRUE (no violation in this direction)
        };

        var graph = Graph([aliceSkill, robotSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act - This used to throw ScheduleInfeasibleException
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception); // Should NOT throw

        // In this case, the dependency is actually satisfied, so no warning
        // But constraint should still be skipped since both are running
    }

    /// <summary>
    ///     Test 10: Mixed scenario - One finished, one running with violation
    /// </summary>
    [Fact]
    public void OneFinished_OneRunning_WithViolation_ShouldSkipConstraintAndLogWarning()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateFixedSkill("Source", 2.0, 5.0, 7.0); // Finished
        var targetSkill = CreateFixedSkill("Target", 5.0, 4.0); // Running, started before source

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart // Target.Start >= Source.Start -> 4.0 >= 5.0 -> FALSE
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Test 11: Adaptive running skill with FinishToFinish dependency (THE BUG FIX)
    ///     Fixed skill (Alice) running, adaptive skill (Robot) running
    ///     FinishToFinish means Robot.Finish >= Alice.Finish
    ///     Robot is ADAPTIVE and RUNNING, so its finish time CAN be adjusted
    ///     Constraint SHOULD be applied (not skipped)
    /// </summary>
    [Fact]
    public void FixedRunning_AdaptiveRunning_WithFinishToFinish_ShouldApplyConstraint()
    {
        // Arrange - Real scenario from the bug report
        var currentTime = 20.0;
        var aliceSkill = CreateFixedSkill("Alice", 85.0, 0.497); // Running, fixed duration, finishes at ~85.5s
        var robotSkill = CreateAdaptiveSkill("Robot", 45.0, 0.704); // Running, adaptive min 45s

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = aliceSkill,
            Target = robotSkill,
            Type = DependencyType.FinishToFinish // Robot.Finish >= Alice.Finish
        };

        var graph = Graph([aliceSkill, robotSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act - Should NOT skip constraint (Robot can adjust its finish time)
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Robot's planned finish should be adjusted to finish after Alice
        // Alice finishes at ~85.5s (0.497 + 85.0), Robot should finish >= that time
        var aliceFinish = aliceSkill.ActualStartTime!.Value + aliceSkill.PlannedDuration;
        Assert.True(robotSkill.PlannedFinishTime >= aliceFinish,
            $"Robot finish ({robotSkill.PlannedFinishTime}) should be >= Alice finish ({aliceFinish})");

        // Verify Robot's duration respects its lower bound
        var robotDuration = robotSkill.PlannedFinishTime - robotSkill.ActualStartTime!.Value;
        Assert.True(robotDuration >= robotSkill.MinDuration,
            $"Robot duration ({robotDuration}) should be >= MinDuration ({robotSkill.MinDuration})");

        // Should NOT log a violation warning (constraint was applied successfully)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Should NOT log a violation when constraint is applied to adaptive skill");
    }

    /// <summary>
    ///     Test 12: Both adaptive skills running with FinishToFinish dependency
    ///     Both skills can adjust their finish times
    ///     Constraint SHOULD be applied
    /// </summary>
    [Fact]
    public void BothAdaptiveRunning_WithFinishToFinish_ShouldApplyConstraint()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateAdaptiveSkill("AdaptiveSource", 20.0, 2.0); // Running, adaptive
        var targetSkill = CreateAdaptiveSkill("AdaptiveTarget", 15.0, 3.0); // Running, adaptive

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToFinish // Target.Finish >= Source.Finish
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Constraint should be respected
        Assert.True(targetSkill.PlannedFinishTime >= sourceSkill.PlannedFinishTime,
            $"Target finish ({targetSkill.PlannedFinishTime}) should be >= Source finish ({sourceSkill.PlannedFinishTime})");

        // No violation warning
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    ///     Test 13: Fixed finished + Adaptive running with FinishToFinish
    ///     Fixed skill is finished (finish time is fixed)
    ///     Adaptive skill is running (finish time can be adjusted)
    ///     Constraint SHOULD be applied
    /// </summary>
    [Fact]
    public void FixedFinished_AdaptiveRunning_WithFinishToFinish_ShouldApplyConstraint()
    {
        // Arrange
        var currentTime = 15.0;
        var sourceSkill = CreateFixedSkill("FixedSource", 5.0, 2.0, 7.0); // Finished at 7.0
        var targetSkill = CreateAdaptiveSkill("AdaptiveTarget", 10.0, 5.0); // Running, adaptive

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToFinish // Target.Finish >= Source.Finish (7.0)
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Target should finish at or after source (7.0)
        Assert.True(targetSkill.PlannedFinishTime >= sourceSkill.ActualFinishTime!.Value,
            $"Target finish ({targetSkill.PlannedFinishTime}) should be >= Source finish ({sourceSkill.ActualFinishTime})");

        // No violation warning
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    ///     Test 14: Fixed running + Adaptive running with StartToFinish
    ///     Fixed source is running (start time is fixed)
    ///     Adaptive target is running (finish time can be adjusted)
    ///     Constraint SHOULD be applied to adjust adaptive skill's finish time
    /// </summary>
    [Fact]
    public void FixedRunning_AdaptiveRunning_WithStartToFinish_ShouldApplyConstraint()
    {
        // Arrange
        var currentTime = 15.0;
        var sourceSkill = CreateFixedSkill("FixedSource", 10.0, 5.0); // Running since 5.0
        var targetSkill = CreateAdaptiveSkill("AdaptiveTarget", 5.0, 2.0); // Running, adaptive

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToFinish // Target.Finish >= Source.Start
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Target should finish at or after source starts
        Assert.True(targetSkill.PlannedFinishTime >= sourceSkill.ActualStartTime!.Value,
            $"Target finish ({targetSkill.PlannedFinishTime}) should be >= Source start ({sourceSkill.ActualStartTime})");

        // No violation warning
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    ///     Test 15: Both fixed running skills with FinishToFinish (should SKIP)
    ///     Both skills are fixed and running, so both finish times are FIXED
    ///     Constraint SHOULD be skipped (cannot change the past)
    /// </summary>
    [Fact]
    public void BothFixedRunning_WithFinishToFinish_ShouldSkipConstraint()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateFixedSkill("FixedSource", 20.0, 2.0); // Running, finishes at 22.0
        var targetSkill = CreateFixedSkill("FixedTarget", 15.0, 3.0); // Running, finishes at 18.0

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToFinish // Target.Finish >= Source.Finish -> 18.0 >= 22.0 -> FALSE (violation)
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Should log violation warning (both finish times are fixed and violated)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log violation when both fixed skills violate FinishToFinish");
    }

    /// <summary>
    ///     Test 16: Adaptive running with StartToStart dependency (should SKIP)
    ///     Both skills are running, so both start times are FIXED (cannot change)
    ///     Constraint SHOULD be skipped
    /// </summary>
    [Fact]
    public void BothAdaptiveRunning_WithStartToStart_ShouldSkipConstraint()
    {
        // Arrange
        var currentTime = 10.0;
        var sourceSkill = CreateAdaptiveSkill("AdaptiveSource", 20.0, 5.0); // Running since 5.0
        var targetSkill =
            CreateAdaptiveSkill("AdaptiveTarget", 15.0, 3.0); // Running since 3.0 (started before source)

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.StartToStart // Target.Start >= Source.Start -> 3.0 >= 5.0 -> FALSE (violation)
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Should log violation warning (both start times are fixed and violated)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log violation when both adaptive skills have fixed start times that violate StartToStart");
    }

    /// <summary>
    ///     Test 17: Adaptive finished + Adaptive running with FinishToFinish (should SKIP for source, but apply for target)
    ///     Source is finished (finish time is fixed)
    ///     Target is running adaptive (finish time can adjust)
    ///     Constraint SHOULD be applied (target can adjust)
    /// </summary>
    [Fact]
    public void AdaptiveFinished_AdaptiveRunning_WithFinishToFinish_ShouldApplyConstraint()
    {
        // Arrange
        var currentTime = 20.0;
        var sourceSkill = CreateAdaptiveSkill("AdaptiveSource", 15.0, 2.0, 22.0); // Finished
        var targetSkill = CreateAdaptiveSkill("AdaptiveTarget", 10.0, 5.0); // Running, adaptive

        var dependency = new Dependency
        {
            Id = Guid.NewGuid(),
            Source = sourceSkill,
            Target = targetSkill,
            Type = DependencyType.FinishToFinish // Target.Finish >= Source.Finish (22.0)
        };

        var graph = Graph([sourceSkill, targetSkill], [dependency]);
        var mockLogger = CreateMockLogger();

        // Act
        var exception = Record.Exception(() => graph.SolveWithLinearProgramming(currentTime, mockLogger.Object));

        // Assert
        Assert.Null(exception);

        // Target should finish at or after source
        Assert.True(targetSkill.PlannedFinishTime >= sourceSkill.ActualFinishTime!.Value,
            $"Target finish ({targetSkill.PlannedFinishTime}) should be >= Source finish ({sourceSkill.ActualFinishTime})");

        // No violation warning
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CONSTRAINT_DEPENDENCY_VIOLATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}