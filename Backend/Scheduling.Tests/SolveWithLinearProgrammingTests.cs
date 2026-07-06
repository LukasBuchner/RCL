using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Unit‑tests for <see cref="ExecutionGraphExtensions.SolveWithLinearProgramming" />.
/// </summary>
public sealed class SolveWithLinearProgrammingTests
{
    private static Dependency Dep(IPlannedSkillExecution src, IPlannedSkillExecution tgt, DependencyType type)
    {
        return new Dependency { Source = src, Target = tgt, Type = type, Id = Guid.Empty };
    }

    private static ExecutionGraph Graph(IEnumerable<IPlannedSkillExecution> tasks, IEnumerable<Dependency> deps)
    {
        return new ExecutionGraph { SkillExecutions = tasks.ToList(), Dependencies = deps.ToList() };
    }

    private static IPlannedSkillExecution CreateFixedTask(string? name = null, double duration = 1.0)
    {
        var mockTask = new Mock<IPlannedSkillExecution>();

        mockTask.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mockTask.SetupGet(t => t.PlannedDuration).Returns(duration);

        mockTask.SetupProperty(t => t.PlannedStartTime, 0.0);
        mockTask.SetupProperty(t => t.PlannedFinishTime, 0.0);

        if (name is not null)
            mockTask.Setup(t => t.ToString()).Returns(name);

        return mockTask.Object;
    }

    private static IAdaptivePlannedSkillExecution CreateAdaptiveTask(string? name = null, double minDuration = 1.0)
    {
        var mockTask = new Mock<IAdaptivePlannedSkillExecution>();
        mockTask.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        // For adaptive tasks, the initial Duration property might not be used by the solver,
        // but it's good practice to have it. It will be updated by the solver.
        // We'll set it to MinDuration initially for consistency, though the solver will override.
        mockTask.SetupProperty(t => t.PlannedDuration, minDuration); // Allows setting and getting
        mockTask.SetupGet(t => t.MinDuration).Returns(minDuration);

        // Setup StartTime and FinishTime as properties so they can be set by the solver
        mockTask.SetupProperty(t => t.PlannedStartTime, 0.0);
        mockTask.SetupProperty(t => t.PlannedFinishTime, 0.0);

        if (name != null) mockTask.Setup(t => t.ToString()).Returns(name);
        return mockTask.Object;
    }

    /// <summary>
    ///     Test 1: Simple two-task graph (A -> B) with fixed durations.
    /// </summary>
    /// <remarks>
    ///     Graph:  A(dur=2) ---FS---> B(dur=3)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 2, Duration = 2
    ///     B: StartTime = 2, FinishTime = 5, Duration = 3
    ///     The schedule should be feasible, and B should start right after A finishes.
    ///     The earliest task (A) should start at t=0 due to makespan minimization.
    /// </remarks>
    [Fact]
    public void Solve_SimpleFixedDurationTasks_FinishToStart_FeasibleSchedule()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateFixedTask("B", 3.0);

        var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
        var graph = Graph([taskA, taskB], deps);

        var solvedGraph = graph.SolveWithLinearProgramming();

        // Verify that the same graph instance is returned and populated
        Assert.Same(graph, solvedGraph);

        // Task A assertions
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);
        Assert.Equal(2.0, taskA.PlannedDuration, 5); // Duration should remain unchanged for fixed tasks

        // Task B assertions
        Assert.Equal(2.0, taskB.PlannedStartTime, 5);
        Assert.Equal(5.0, taskB.PlannedFinishTime, 5);
        Assert.Equal(3.0, taskB.PlannedDuration, 5); // Duration should remain unchanged for fixed tasks
    }

    /// <summary>
    ///     Test 2: Graph with one fixed and one adaptive task (A_fixed -> B_adaptive).
    /// </summary>
    /// <remarks>
    ///     Graph:  A(dur=2) ---FS---> B(min=1, max=3)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 2, Duration = 2
    ///     B: StartTime = 2, FinishTime = 3, Duration = 1 (solver should pick minDuration to minimize makespan)
    ///     The schedule should be feasible. Task B should start after A finishes, and its duration
    ///     should be chosen as its MinDuration because the solver minimizes the total makespan.
    /// </remarks>
    [Fact]
    public void Solve_FixedAndAdaptiveTasks_AdaptiveTakesMinDuration()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateAdaptiveTask("B");

        var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
        var graph = Graph([taskA, taskB], deps);

        graph.SolveWithLinearProgramming();

        // Task A assertions
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);
        Assert.Equal(2.0, taskA.PlannedDuration, 5);

        // Task B assertions (adaptive)
        Assert.Equal(2.0, taskB.PlannedStartTime, 5);
        Assert.Equal(1.0, taskB.PlannedDuration, 5); // Should take MinDuration
        Assert.Equal(taskB.PlannedStartTime + taskB.PlannedDuration, taskB.PlannedFinishTime, 5);
        Assert.True(
            taskB.PlannedDuration is >= 1.0 - 1e-9
                and <= 3.0 + 1e-9); // Check within bounds (allowing for float precision)
    }

    /// <summary>
    ///     Graph with a direct FS cycle (A → B → A). The cycle is event-level cyclic,
    ///     so <c>ValidateModel</c> rejects it before the LP solver is invoked.
    /// </summary>
    /// <remarks>
    ///     Graph:  A(dur=1) ---FS---> B(dur=1)
    ///     ^----------------FS---|
    /// </remarks>
    [Fact]
    public void Solve_CircularDependency_ThrowsScheduleModelException()
    {
        var taskA = CreateFixedTask("A");
        var taskB = CreateFixedTask("B");

        var deps = new[]
        {
            Dep(taskA, taskB, DependencyType.FinishToStart),
            Dep(taskB, taskA, DependencyType.FinishToStart)
        };
        var graph = Graph([taskA, taskB], deps);

        var exception = Assert.Throws<ScheduleModelException>(() => graph.SolveWithLinearProgramming());
        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     Test 4: Graph containing tasks with duplicate IDs.
    /// </summary>
    /// <remarks>
    ///     Expected: Throws ScheduleModelException.
    ///     The solver setup relies on unique task IDs for variable creation.
    ///     Duplicate IDs should be caught by the initial validation.
    /// </remarks>
    [Fact]
    public void Solve_DuplicateTaskIds_ThrowsScheduleModelException()
    {
        var sharedId = Guid.NewGuid();

        var mockTaskA = new Mock<IPlannedSkillExecution>();
        mockTaskA.SetupGet(t => t.Id).Returns(sharedId);
        mockTaskA.SetupGet(t => t.PlannedDuration).Returns(1.0);
        var taskA = mockTaskA.Object;

        var mockTaskB = new Mock<IPlannedSkillExecution>();
        mockTaskB.SetupGet(t => t.Id).Returns(sharedId); // Same ID as taskA
        mockTaskB.SetupGet(t => t.PlannedDuration).Returns(1.0);
        var taskB = mockTaskB.Object;

        var graph = Graph([taskA, taskB], []); // No dependencies needed for this test

        var exception = Assert.Throws<ScheduleModelException>(() => graph.SolveWithLinearProgramming());
        Assert.Contains("Duplicate ", exception.Message);
        Assert.Contains(sharedId.ToString(), exception.Message);
    }

    // /// <summary>
    // ///     Test 5: Adaptive task with a negative MinDuration.
    // /// </summary>
    // /// <remarks>
    // ///     Expected: Throws ScheduleModelException.
    // ///     Durations must be non-negative. This should be caught by validation.
    // /// </remarks>
    // [Fact]
    // public void Solve_AdaptiveTaskNegativeMinDuration_ThrowsScheduleModelException()
    // {
    //     var adaptiveTask = CreateAdaptiveTask("A", -1.0, 2.0);
    //     var graph = Graph([adaptiveTask], []);
    //
    //     var exception = Assert.Throws<ScheduleModelException>(() => graph.SolveWithLinearProgramming());
    //     Assert.Contains($"Adaptive task {adaptiveTask.Id} has negative duration bounds.", exception.Message);
    // }

    /// <summary>
    ///     Test 8: Solving an empty graph (no tasks, no dependencies).
    /// </summary>
    /// <remarks>
    ///     Expected: Completes successfully, returns the same empty graph.
    ///     The solver should handle this edge case without throwing an exception.
    ///     The resulting graph will have no tasks with updated times.
    /// </remarks>
    [Fact]
    public void Solve_EmptyGraph_CompletesSuccessfully()
    {
        var graph = Graph([], []);

        var solvedGraph = graph.SolveWithLinearProgramming();

        Assert.Same(graph, solvedGraph);
        Assert.Empty(solvedGraph.SkillExecutions);
        Assert.Empty(solvedGraph.Dependencies); // Should remain empty
    }

    /// <summary>
    ///     Test 9: Graph with only fixed duration tasks in a sequence (A -> B -> C).
    /// </summary>
    /// <remarks>
    ///     Graph:  A(dur=1) ---FS---> B(dur=2) ---FS---> C(dur=1.5)
    ///     Expected:
    ///     A: StartTime = 0,   FinishTime = 1
    ///     B: StartTime = 1,   FinishTime = 3
    ///     C: StartTime = 3,   FinishTime = 4.5
    ///     All durations should remain fixed. The schedule should be a simple linear sequence.
    /// </remarks>
    [Fact]
    public void Solve_OnlyFixedDurationTasks_Sequence_FeasibleSchedule()
    {
        var taskA = CreateFixedTask("A");
        var taskB = CreateFixedTask("B", 2.0);
        var taskC = CreateFixedTask("C", 1.5);

        var deps = new[]
        {
            Dep(taskA, taskB, DependencyType.FinishToStart),
            Dep(taskB, taskC, DependencyType.FinishToStart)
        };
        var graph = Graph([taskA, taskB, taskC], deps);

        graph.SolveWithLinearProgramming();

        // Task A
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(1.0, taskA.PlannedFinishTime, 5);
        Assert.Equal(1.0, taskA.PlannedDuration, 5);

        // Task B
        Assert.Equal(1.0, taskB.PlannedStartTime, 5);
        Assert.Equal(3.0, taskB.PlannedFinishTime, 5);
        Assert.Equal(2.0, taskB.PlannedDuration, 5);

        // Task C
        Assert.Equal(3.0, taskC.PlannedStartTime, 5);
        Assert.Equal(4.5, taskC.PlannedFinishTime, 5);
        Assert.Equal(1.5, taskC.PlannedDuration, 5);
    }

    /// <summary>
    ///     Test 10: Graph utilizing all four dependency types (FS, SS, SF, FF).
    /// </summary>
    /// <remarks>
    ///     Tasks (all fixed duration):
    ///     A (dur=2)
    ///     B (dur=3)
    ///     C (dur=1)
    ///     D (dur=4)
    ///     Dependencies:
    ///     A --FS--▸ B  (B.Start >= A.Finish)
    ///     A --SS--▸ C  (C.Start >= A.Start)
    ///     B --SF--▸ D  (D.Finish >= B.Start)
    ///     C --FF--▸ D  (D.Finish >= C.Finish)
    ///     Expected Schedule (shifted for earliest start at t=0, makespan minimized):
    ///     A: StartTime=0, FinishTime=2, Duration=2
    ///     C: StartTime=0, FinishTime=1, Duration=1 (due to A-SS->C)
    ///     B: StartTime=2, FinishTime=5, Duration=3 (due to A-FS->B)
    ///     D: StartTime=0, FinishTime=4, Duration=4 (D.Finish >= B.Start=2, D.Finish >= C.Finish=1; S_D = F_D-4 >=0 => F_D
    ///     >=4. Min F_D=4)
    ///     Makespan = 5 (from Task B)
    /// </remarks>
    [Fact]
    public void Solve_AllDependencyTypes_FeasibleSchedule()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateFixedTask("B", 3.0);
        var taskC = CreateFixedTask("C");
        var taskD = CreateFixedTask("D", 4.0);

        var deps = new[]
        {
            Dep(taskA, taskB, DependencyType.FinishToStart), // B.Start >= A.Finish
            Dep(taskA, taskC, DependencyType.StartToStart), // C.Start >= A.Start
            Dep(taskB, taskD, DependencyType.StartToFinish), // D.Finish >= B.Start
            Dep(taskC, taskD, DependencyType.FinishToFinish) // D.Finish >= C.Finish
        };
        var graph = Graph([taskA, taskB, taskC, taskD], deps);

        graph.SolveWithLinearProgramming();

        // Task A
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);

        // Task C (depends on A.Start)
        Assert.Equal(0.0, taskC.PlannedStartTime, 5);
        Assert.Equal(1.0, taskC.PlannedFinishTime, 5);

        // Task B (depends on A.Finish)
        Assert.Equal(2.0, taskB.PlannedStartTime, 5);
        Assert.Equal(5.0, taskB.PlannedFinishTime, 5);

        // Task D (depends on B.Start and C.Finish)
        // F_D >= S_B (2) and F_D >= F_C (1). S_D = F_D - 4 >= 0 => F_D >= 4.
        // So, solver chooses F_D = 4, S_D = 0.
        Assert.Equal(0.0, taskD.PlannedStartTime, 5);
        Assert.Equal(4.0, taskD.PlannedFinishTime, 5);
    }

    /// <summary>
    ///     Test 11: Ensures schedule is normalized (earliest task starts at t=0) and makespan is reasonable.
    /// </summary>
    /// <remarks>
    ///     Graph: Three independent fixed-duration tasks.
    ///     A (dur=2)
    ///     B (dur=3)
    ///     C (dur=1)
    ///     Expected:
    ///     All tasks start at t=0 as there are no dependencies.
    ///     A: StartTime=0, FinishTime=2
    ///     B: StartTime=0, FinishTime=3
    ///     C: StartTime=0, FinishTime=1
    ///     The earliest start time across all tasks should be 0.
    ///     The makespan objective should ensure tasks are scheduled as early as possible.
    /// </remarks>
    [Fact]
    public void Solve_IndependentTasks_NormalizedAndMakespanMinimized()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateFixedTask("B", 3.0);
        var taskC = CreateFixedTask("C");

        var graph = Graph([taskA, taskB, taskC], []);

        graph.SolveWithLinearProgramming();

        // Check normalization: at least one task must start at 0
        var minStartTime = new[] { taskA.PlannedStartTime, taskB.PlannedStartTime, taskC.PlannedStartTime }.Min();
        Assert.Equal(0.0, minStartTime, 5);

        // Check individual task schedules (should all start at 0)
        // Task A
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);

        // Task B
        Assert.Equal(0.0, taskB.PlannedStartTime, 5);
        Assert.Equal(3.0, taskB.PlannedFinishTime, 5);

        // Task C
        Assert.Equal(0.0, taskC.PlannedStartTime, 5);
        Assert.Equal(1.0, taskC.PlannedFinishTime, 5);

        // All start times should be non-negative
        Assert.True(taskA.PlannedStartTime >= -1e-9);
        Assert.True(taskB.PlannedStartTime >= -1e-9);
        Assert.True(taskC.PlannedStartTime >= -1e-9);
    }


    /// <summary>
    ///     Test 12: Two‑task graph where the adaptive task must start and finish
    ///     exactly with a fixed‑duration task.
    /// </summary>
    /// <remarks>
    ///     Tasks:
    ///     F (fixed, dur = 2)
    ///     A (adaptive, min = 1, max = 4)
    ///     Dependencies:
    ///     F --SS--> A   (A.Start ≥ F.Start)
    ///     F --FF--> A   (A.Finish ≥ F.Finish)
    /// </remarks>
    [Fact]
    public void Solve_FixedAndAdaptive_SS_And_FF()
    {
        var fixedF = CreateFixedTask("F", 2.0);
        var adaptA = CreateAdaptiveTask("A");

        var deps = new[]
        {
            Dep(fixedF, adaptA, DependencyType.StartToStart),
            Dep(fixedF, adaptA, DependencyType.FinishToFinish)
        };
        var graph = Graph([fixedF, adaptA], deps);

        graph.SolveWithLinearProgramming();

        Assert.True(adaptA.PlannedStartTime >= fixedF.PlannedStartTime);
        Assert.True(adaptA.PlannedFinishTime >= fixedF.PlannedFinishTime);
        Assert.True(adaptA.PlannedDuration >= 1);
    }


    /// <summary>
    ///     Helper to create a running fixed-duration skill execution.
    /// </summary>
    /// <param name="name">Optional name for debugging.</param>
    /// <param name="duration">The estimated/planned duration of the skill.</param>
    /// <param name="actualStartTime">The actual start time (skill is running).</param>
    /// <returns>A mock ISkillExecution representing a running fixed skill.</returns>
    private static ISkillExecution CreateRunningFixedSkill(string? name, double duration, double actualStartTime)
    {
        var mock = new Mock<ISkillExecution>();
        mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mock.SetupProperty(t => t.PlannedStartTime, actualStartTime);
        mock.SetupProperty(t => t.PlannedFinishTime, actualStartTime + duration);
        mock.SetupProperty(t => t.PlannedDuration, duration);
        mock.SetupGet(t => t.ActualStartTime).Returns(actualStartTime);
        mock.SetupGet(t => t.ActualFinishTime).Returns((double?)null);
        mock.SetupGet(t => t.EstimatedDuration).Returns(duration);
        mock.SetupGet(t => t.IsRunning).Returns(true);
        mock.SetupGet(t => t.IsFinished).Returns(false);
        if (name is not null)
            mock.Setup(t => t.ToString()).Returns(name);
        return mock.Object;
    }

    /// <summary>
    ///     Helper to create a running adaptive-duration skill execution.
    /// </summary>
    /// <param name="name">Optional name for debugging.</param>
    /// <param name="minDuration">Minimum duration of the adaptive skill.</param>
    /// <param name="actualStartTime">The actual start time (skill is running).</param>
    /// <returns>A mock IAdaptiveSkillExecution representing a running adaptive skill.</returns>
    private static IAdaptiveSkillExecution CreateRunningAdaptiveSkill(
        string? name,
        double minDuration,
        double actualStartTime)
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
        if (name is not null)
            mock.Setup(t => t.ToString()).Returns(name);
        return mock.Object;
    }

    /// <summary>
    ///     Test 14: Fixed (SS+FF) to Adaptive, both NOT started - LP should push j's start, duration unchanged.
    ///     This is a companion test to PlanScheduleTests.PlanSchedule_FixedAndAdaptive_SS_And_FF_BothNotStarted
    ///     to compare LP solver behavior vs full PlanSchedule pipeline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Graph: i(fix 10) --SS,FF--> j(adap min=2, max=20)
    ///     </para>
    ///     <para>
    ///         Since no back-edge exists, both are trivial SCCs. For NOT_STARTED tasks,
    ///         j's start time should be pushed to satisfy FF constraint.
    ///     </para>
    ///     <para>
    ///         Expected:
    ///         <list type="bullet">
    ///             <item><description>i: S=0, D=10, F=10</description></item>
    ///             <item><description>j: D=2 (MinDuration, unchanged), S=8 (pushed for FF), F=10</description></item>
    ///             <item><description>SS: 8 >= 0 ✓</description></item>
    ///             <item><description>FF: 10 >= 10 ✓</description></item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Fact]
    public void Solve_FixedAndAdaptive_SS_And_FF_BothNotStarted_JStartPushedDurationUnchanged()
    {
        // Arrange
        var taskI = CreateFixedTask("i", 10.0);
        var taskJ = CreateAdaptiveTask("j", 2.0);

        var deps = new[]
        {
            Dep(taskI, taskJ, DependencyType.StartToStart),
            Dep(taskI, taskJ, DependencyType.FinishToFinish)
        };
        var graph = Graph([taskI, taskJ], deps);

        // Act - Use LP directly
        graph.SolveWithLinearProgramming();

        // Debug output
        var iStart = taskI.PlannedStartTime;
        var iDuration = taskI.PlannedDuration;
        var iFinish = taskI.PlannedFinishTime;
        var jStart = taskJ.PlannedStartTime;
        var jDuration = taskJ.PlannedDuration;
        var jFinish = taskJ.PlannedFinishTime;

        // Assert - i scheduled at start
        Assert.Equal(0.0, iStart, 5);
        Assert.Equal(10.0, iDuration, 5);
        Assert.Equal(10.0, iFinish, 5);

        // Assert - j's duration should be MinDuration (LP minimizes duration)
        Assert.Equal(2.0, jDuration, 5);

        // Assert - j's start pushed to satisfy FF (Fj >= Fi => Sj + Dj >= Fi => Sj >= 10 - 2 = 8)
        Assert.True(jStart >= 8.0 - 0.01,
            $"j.Start ({jStart:F2}) should be >= 8.0 to satisfy FF constraint");

        // Assert - SS satisfied: Sj >= Si
        Assert.True(jStart >= iStart,
            $"SS violated: j.Start ({jStart:F2}) should be >= i.Start ({iStart:F2})");

        // Assert - FF satisfied: Fj >= Fi
        Assert.True(jFinish >= iFinish - 0.01,
            $"FF violated: j.Finish ({jFinish:F2}) should be >= i.Finish ({iFinish:F2})");
    }

    /// <summary>
    ///     Test 15: Fixed (SS+FF) to Adaptive, both RUNNING - LP should extend j's duration correctly.
    ///     This is a companion test to PlanScheduleTests.PlanSchedule_FixedAndAdaptive_SS_And_FF_BothRunning
    ///     to compare LP solver behavior vs full PlanSchedule pipeline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Graph: i(fix 85s, running since 0.5s) --SS,FF--> j(adap 45-95s, running since 0.7s)
    ///     </para>
    ///     <para>
    ///         Realistic scenario: Human and robot started simultaneously, robot should finish with/after human.
    ///         Using LP directly (not through PlanSchedule) to see if LP handles this correctly.
    ///     </para>
    ///     <para>
    ///         Expected behavior when using LP directly:
    ///         <list type="bullet">
    ///             <item><description>j.PlannedStartTime = 0.7 (fixed, ActualStartTime)</description></item>
    ///             <item><description>j.Duration extended to ~84.8 to satisfy FF (i.Finish - j.Start = 85.5 - 0.7)</description></item>
    ///             <item><description>j.Finish = 0.7 + 84.8 = 85.5 ≥ i.Finish (FF satisfied correctly)</description></item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Fact]
    public void Solve_FixedAndAdaptive_SS_And_FF_BothRunning_LPShouldExtendDuration()
    {
        // Arrange - Realistic human+robot scenario
        var currentTime = 20.0;
        var taskI = CreateRunningFixedSkill("Human_i", 85.0, 0.5);
        var taskJ = CreateRunningAdaptiveSkill("Robot_j", 45.0, 0.7);

        var deps = new[]
        {
            Dep(taskI, taskJ, DependencyType.StartToStart),
            Dep(taskI, taskJ, DependencyType.FinishToFinish)
        };
        var graph = Graph([taskI, taskJ], deps);

        // Act - Use LP directly (not PlanSchedule)
        graph.SolveWithLinearProgramming(currentTime);

        // Debug output
        var iStart = taskI.PlannedStartTime;
        var iDuration = taskI.PlannedDuration;
        var iFinish = taskI.PlannedFinishTime;
        var jStart = taskJ.PlannedStartTime;
        var jDuration = taskJ.PlannedDuration;
        var jFinish = taskJ.PlannedFinishTime;

        // Assert - For running tasks, PlannedStartTime should equal ActualStartTime
        Assert.True(Math.Abs(jStart - taskJ.ActualStartTime!.Value) < 0.01,
            $"Running task j's PlannedStartTime ({jStart:F2}) should match ActualStartTime ({taskJ.ActualStartTime:F2}). " +
            $"Values: i: Start={iStart:F2}, Duration={iDuration:F2}, Finish={iFinish:F2}; " +
            $"j: Start={jStart:F2}, Duration={jDuration:F2}, Finish={jFinish:F2}");

        // Assert - SS should be satisfied
        Assert.True(jStart >= iStart,
            $"SS violated: j.Start ({jStart:F2}) should be >= i.Start ({iStart:F2})");

        // Assert - FF should be satisfied (j's duration should have been extended)
        Assert.True(jFinish >= iFinish,
            $"FF violated: j.Finish ({jFinish:F2}) should be >= i.Finish ({iFinish:F2}). " +
            $"j.Duration={jDuration:F2} should have been extended to satisfy this constraint.");

        // Assert - j's duration should be within bounds
        Assert.True(jDuration is >= 45.0 and <= 95.0,
            $"j.Duration ({jDuration:F2}) should be within [45, 95]");
    }
}