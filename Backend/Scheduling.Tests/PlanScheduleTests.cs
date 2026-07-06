using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling.Tests;

/// <summary>
///     Unit‑tests for <see cref="ExecutionGraphExtensions.PlanSchedule" />.
/// </summary>
public sealed partial class PlanScheduleTests
{
    private static Dependency Dep(IPlannedSkillExecution src, IPlannedSkillExecution tgt, DependencyType type)
    {
        return new Dependency { Source = src, Target = tgt, Type = type, Id = Guid.Empty };
    }

    private static ExecutionGraph Graph(IEnumerable<IPlannedSkillExecution> tasks, IEnumerable<Dependency> deps)
    {
        return new ExecutionGraph { SkillExecutions = tasks.ToList(), Dependencies = deps.ToList() };
    }

    // Helper to create a fixed duration task
    private static IPlannedSkillExecution CreateFixedTask(string? name = null, double duration = 1.0)
    {
        var mockTask = new Mock<IPlannedSkillExecution>();
        mockTask.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        mockTask.SetupGet(t => t.PlannedDuration).Returns(duration);
        mockTask.SetupProperty(t => t.PlannedStartTime, double.NaN); // Initialise with NaN as per PlanSchedule logic
        mockTask.SetupProperty(t => t.PlannedFinishTime, double.NaN);
        if (name is not null)
            mockTask.Setup(t => t.ToString()).Returns(name);
        return mockTask.Object;
    }

    // Helper to create an adaptive duration task
    private static IAdaptivePlannedSkillExecution CreateAdaptiveTask(string? name = null, double minDuration = 1.0)
    {
        var mockTask = new Mock<IAdaptivePlannedSkillExecution>();
        mockTask.SetupGet(t => t.Id).Returns(Guid.NewGuid());
        // MinDuration is the only solver input; the duration is unbounded above.
        mockTask.SetupGet(t => t.MinDuration).Returns(minDuration);

        // Duration, StartTime, and FinishTime will be set by PlanSchedule or the LP solver.
        // Initialise them here. SetupProperty makes them behave like auto-properties.
        mockTask.SetupProperty(t => t.PlannedDuration,
            minDuration); // PlanSchedule can read initial value for trivial SCCs
        mockTask.SetupProperty(t => t.PlannedStartTime, double.NaN); // Expected initial state for PlanSchedule
        mockTask.SetupProperty(t => t.PlannedFinishTime, double.NaN); // Expected initial state for PlanSchedule

        // Ensure properties are set on the actual object instance to satisfy initial conditions if SetupProperty needs it.
        // (Moq's SetupProperty usually handles initialisation with the provided value, 
        // but this ensures the state before the object is returned if there were any subtleties).
        var taskInstance = mockTask.Object;
        taskInstance.PlannedDuration = minDuration;
        taskInstance.PlannedStartTime = double.NaN;
        taskInstance.PlannedFinishTime = double.NaN;

        if (name is not null)
            mockTask.Setup(t => t.ToString()).Returns(name);

        // The custom SetupSet for Duration that modified FinishTime has been removed,
        // as it likely caused interference with the LP solver's calculations.

        return taskInstance;
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
    ///     Test 1: PlanSchedule with an empty graph.
    /// </summary>
    /// <remarks>
    ///     Expected: Completes successfully, returns the same empty graph.
    ///     No exceptions should be thrown.
    /// </remarks>
    [Fact]
    public void PlanSchedule_EmptyGraph_CompletesSuccessfully()
    {
        var graph = Graph([], []);
        var resultGraph = graph.PlanSchedule();

        Assert.Same(graph, resultGraph);
        Assert.Empty(resultGraph.SkillExecutions);
    }

    /// <summary>
    ///     Test 2: PlanSchedule with a single fixed-duration task.
    /// </summary>
    /// <remarks>
    ///     Graph: A(dur=2)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 2, Duration = 2
    ///     The schedule should place the single task at the beginning.
    /// </remarks>
    [Fact]
    public void PlanSchedule_SingleFixedTask_ScheduledAtZero()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var graph = Graph([taskA], []);

        // Mocking GetStronglyConnectedComponents and ClassifyStronglyConnectedComponent indirectly
        // by controlling what PlanSchedule *would* find.
        // For a single task, it's one SCC of type Trivial.
        // We need to ensure our mock setup for tasks allows StartTime/FinishTime to be set.

        var resultGraph = graph.PlanSchedule();

        Assert.Same(graph, resultGraph);
        Assert.Single(resultGraph.SkillExecutions);
        var scheduledTaskA = resultGraph.SkillExecutions[0];
        Assert.Equal(taskA.Id, scheduledTaskA.Id);
        Assert.Equal(0.0, scheduledTaskA.PlannedStartTime, 5);
        Assert.Equal(2.0, scheduledTaskA.PlannedFinishTime, 5);
        Assert.Equal(2.0, scheduledTaskA.PlannedDuration, 5);
    }

    /// <summary>
    ///     Test 3: PlanSchedule with a single adaptive-duration task.
    /// </summary>
    /// <remarks>
    ///     Graph: A(min=1, max=3)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 1, Duration = 1
    ///     For a single adaptive task (which is a Trivial SCC, not an AdaptiveCycle),
    ///     PlanSchedule doesn't call SolveWithLinearProgramming on the SCC itself.
    ///     It relies on the initial duration (or MinDuration) for trivial SCCs.
    ///     The task should be scheduled at t=0, and its duration should be its initial one (MinDuration by default from our
    ///     helper).
    /// </remarks>
    [Fact]
    public void PlanSchedule_SingleAdaptiveTask_ScheduledAtZero_UsesInitialDuration()
    {
        var taskA = CreateAdaptiveTask("A");
        // Initial duration of mock is MinDuration
        var graph = Graph([taskA], []);

        var resultGraph = graph.PlanSchedule();

        Assert.Same(graph, resultGraph);
        Assert.Single(resultGraph.SkillExecutions);
        var scheduledTaskA = (IAdaptivePlannedSkillExecution)resultGraph.SkillExecutions[0];
        Assert.Equal(taskA.Id, scheduledTaskA.Id);
        Assert.Equal(0.0, scheduledTaskA.PlannedStartTime, 5);
        Assert.Equal(1.0, scheduledTaskA.PlannedDuration, 5); // Should use initial/min duration
        Assert.Equal(1.0, scheduledTaskA.PlannedFinishTime, 5);
        Assert.True(scheduledTaskA.PlannedDuration is >= 1.0 - 1e-9 and <= 3.0 + 1e-9);
    }

    /// <summary>
    ///     Test 4: Two fixed tasks in sequence (A -> B).
    /// </summary>
    /// <remarks>
    ///     Graph: A(dur=2) --FS--> B(dur=3)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 2, Duration = 2
    ///     B: StartTime = 2, FinishTime = 5, Duration = 3
    ///     Both are trivial SCCs. B should follow A.
    /// </remarks>
    [Fact]
    public void PlanSchedule_TwoFixedTasks_Sequence_FeasibleSchedule()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateFixedTask("B", 3.0);
        var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
        var graph = Graph([taskA, taskB], deps);

        var resultGraph = graph.PlanSchedule();

        Assert.Same(graph, resultGraph);
        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);
        Assert.Equal(2.0, taskA.PlannedDuration, 5);

        Assert.Equal(2.0, taskB.PlannedStartTime, 5);
        Assert.Equal(5.0, taskB.PlannedFinishTime, 5);
        Assert.Equal(3.0, taskB.PlannedDuration, 5);
    }

    /// <summary>
    ///     Test 5: One fixed and one adaptive task in a sequence (A_fixed -> B_adaptive).
    /// </summary>
    /// <remarks>
    ///     Graph: A(dur=2) --FS--> B(min=1, max=3)
    ///     Expected:
    ///     A: StartTime = 0, FinishTime = 2, Duration = 2
    ///     B: StartTime = 2, FinishTime = 3, Duration = 1 (uses its MinDuration as it's a Trivial SCC)
    /// </remarks>
    [Fact]
    public void PlanSchedule_FixedAndAdaptiveTasks_Sequence_AdaptiveUsesInitialDuration()
    {
        var taskA = CreateFixedTask("A", 2.0);
        var taskB = CreateAdaptiveTask("B");
        var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
        var graph = Graph([taskA, taskB], deps);

        graph.PlanSchedule();

        Assert.Equal(0.0, taskA.PlannedStartTime, 5);
        Assert.Equal(2.0, taskA.PlannedFinishTime, 5);

        Assert.Equal(2.0, taskB.PlannedStartTime, 5);
        Assert.Equal(1.0, taskB.PlannedDuration, 5); // Trivial SCC, uses initial (MinDuration)
        Assert.Equal(3.0, taskB.PlannedFinishTime, 5);
    }

    /// <summary>
    ///     Graph with a Finish-to-Start cycle of fixed-duration tasks. Such a cycle is
    ///     event-level cyclic, so <c>ValidateModel</c> rejects it before SCC analysis runs.
    /// </summary>
    /// <remarks>
    ///     Graph:  A(dur=1) ---FS---> B(dur=1)
    ///     ^----------------FS---|
    ///     Expected: Throws <see cref="ScheduleModelException" /> with an
    ///     event-level-cycle diagnostic.
    /// </remarks>
    [Fact]
    public void PlanSchedule_FsCycle_ThrowsScheduleModelException()
    {
        var taskA = CreateFixedTask("A");
        var taskB = CreateFixedTask("B");

        var deps = new[]
        {
            Dep(taskA, taskB, DependencyType.FinishToStart),
            Dep(taskB, taskA, DependencyType.FinishToStart)
        };
        var graph = Graph([taskA, taskB], deps);

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule());
        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     Test 7: PlanSchedule with duplicate task IDs.
    /// </summary>
    /// <remarks>
    ///     Expected: Throws ScheduleModelException due to ValidateModel.
    /// </remarks>
    [Fact]
    public void PlanSchedule_DuplicateTaskIds_ThrowsScheduleModelException()
    {
        var sharedId = Guid.NewGuid();
        var mockTaskA = new Mock<IPlannedSkillExecution>();
        mockTaskA.SetupGet(t => t.Id).Returns(sharedId);
        mockTaskA.SetupGet(t => t.PlannedDuration).Returns(1.0);
        // PlanSchedule initializes NaN and expects them to be set
        mockTaskA.SetupProperty(t => t.PlannedStartTime, double.NaN);
        mockTaskA.SetupProperty(t => t.PlannedFinishTime, double.NaN);
        var taskA = mockTaskA.Object;

        var mockTaskB = new Mock<IPlannedSkillExecution>();
        mockTaskB.SetupGet(t => t.Id).Returns(sharedId); // Same ID
        mockTaskB.SetupGet(t => t.PlannedDuration).Returns(1.0);
        mockTaskB.SetupProperty(t => t.PlannedStartTime, double.NaN);
        mockTaskB.SetupProperty(t => t.PlannedFinishTime, double.NaN);
        var taskB = mockTaskB.Object;

        var graph = Graph([taskA, taskB], []);

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule());
        Assert.Contains("Duplicate task identifier(s):", exception.Message);
        Assert.Contains(sharedId.ToString(), exception.Message);
    }

    /// <summary>
    ///     Complex graph with multiple SCCs where one SCC is an FS-cycle of an adaptive
    ///     and a fixed task. <c>ValidateModel</c> rejects the graph for event-level
    ///     cyclicity before any SCC solving is attempted; adaptive duration flexibility
    ///     does not rescue an FS cycle with positive durations.
    /// </summary>
    /// <remarks>
    ///     SCC1 (FS cycle): A(adap min1,max3), B(fix 2). A→B, B→A (FS dependencies).
    ///     SCC2 (Trivial Fixed): C(fix 1).
    ///     SCC3 (Trivial Adaptive): D(adap min1,max2).
    ///     Inter-SCC: B→C, C→D (FS).
    /// </remarks>
    [Fact]
    public void PlanSchedule_ComplexGraph_AdaptiveFsCycle_ThrowsScheduleModelException()
    {
        var taskAScc1 = CreateAdaptiveTask("A_scc1");
        var taskBScc1 = CreateFixedTask("B_scc1", 2.0);
        var depsScc1 = new List<Dependency>
        {
            Dep(taskAScc1, taskBScc1, DependencyType.FinishToStart),
            Dep(taskBScc1, taskAScc1, DependencyType.FinishToStart)
        };

        var taskCScc2 = CreateFixedTask("C_scc2");
        var taskDScc3 = CreateAdaptiveTask("D_scc3");

        var allTasks = new List<IPlannedSkillExecution> { taskAScc1, taskBScc1, taskCScc2, taskDScc3 };
        var allDeps = new List<Dependency>(depsScc1)
        {
            Dep(taskBScc1, taskCScc2, DependencyType.FinishToStart),
            Dep(taskCScc2, taskDScc3, DependencyType.FinishToStart)
        };

        var graph = Graph(allTasks, allDeps);

        var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule());
        Assert.Contains("Event-level", exception.Message);
    }

    /// <summary>
    ///     Test 11: Fixed (SS+FF) to Adaptive, both NOT started - j's start pushed, duration unchanged.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Graph: i(fix 10) --SS,FF--> j(adap min=2, max=20)
    ///     </para>
    ///     <para>
    ///         Since there is no back-edge from j to i, both tasks are in separate trivial SCCs.
    ///         For trivial SCCs, the adaptive task uses MinDuration (LP solver is NOT invoked).
    ///         The FinishToFinish constraint is satisfied by pushing j's start time later.
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
    public void PlanSchedule_FixedAndAdaptive_SS_And_FF_BothNotStarted_JStartPushedDurationUnchanged()
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

        // Act
        graph.PlanSchedule();

        // Assert - i scheduled normally at start
        Assert.Equal(0.0, taskI.PlannedStartTime, 5);
        Assert.Equal(10.0, taskI.PlannedDuration, 5);
        Assert.Equal(10.0, taskI.PlannedFinishTime, 5);

        // Assert - j's duration unchanged (MinDuration) because trivial SCCs don't use LP
        Assert.Equal(2.0, taskJ.PlannedDuration, 5);

        // Assert - j's start pushed to satisfy FF constraint
        // FF: j.Finish >= i.Finish => j.Start + j.Duration >= 10 => j.Start >= 10 - 2 = 8
        Assert.Equal(8.0, taskJ.PlannedStartTime, 5);
        Assert.Equal(10.0, taskJ.PlannedFinishTime, 5);

        // Verify constraints satisfied
        Assert.True(taskJ.PlannedStartTime >= taskI.PlannedStartTime, "SS: j.Start >= i.Start");
        Assert.True(taskJ.PlannedFinishTime >= taskI.PlannedFinishTime, "FF: j.Finish >= i.Finish");
    }

    /// <summary>
    ///     Verifies correct handling of running adaptive tasks with inter-SCC FF constraints.
    ///     Fixed (SS+FF) to Adaptive, both RUNNING - j's duration is extended to satisfy FF, start time preserved.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Graph: i(fix 85s, running since 0.5s) --SS,FF--> j(adap 45-95s, running since 0.7s)
    ///     </para>
    ///     <para>
    ///         Realistic scenario: Human and robot started simultaneously, robot should finish with/after human.
    ///     </para>
    ///     <para>
    ///         Expected behavior:
    ///         <list type="bullet">
    ///             <item><description>j.PlannedStartTime = 0.7 (preserved ActualStartTime - running task's start is fixed)</description></item>
    ///             <item><description>j.Duration extended to ~84.8 to satisfy FF (i.Finish - j.Start = 85.5 - 0.7)</description></item>
    ///             <item><description>j.Finish = 0.7 + 84.8 = 85.5 ≥ i.Finish (FF satisfied correctly)</description></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This test verifies:
    ///         <list type="bullet">
    ///             <item><description>Running tasks have their PlannedStartTime locked to ActualStartTime</description></item>
    ///             <item><description>Adaptive running tasks have their duration extended for inter-SCC FF/SF constraints</description></item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Fact]
    public void PlanSchedule_FixedAndAdaptive_SS_And_FF_BothRunning_FFConstraintShouldExtendAdaptiveDuration()
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

        // Act
        graph.PlanSchedule(currentTime);

        // Debug output to understand actual values
        var iStart = taskI.PlannedStartTime;
        var iFinish = taskI.PlannedFinishTime;
        var jStart = taskJ.PlannedStartTime;
        var jDuration = taskJ.PlannedDuration;
        var jFinish = taskJ.PlannedFinishTime;

        // Assert - Running task's PlannedStartTime equals ActualStartTime (start is fixed)
        Assert.True(Math.Abs(jStart - taskJ.ActualStartTime!.Value) < 0.01,
            $"Running task j's PlannedStartTime ({jStart:F2}) should match ActualStartTime ({taskJ.ActualStartTime:F2}).");

        // Assert - SS should be satisfied
        Assert.True(jStart >= iStart,
            $"SS violated: j.Start ({jStart:F2}) should be >= i.Start ({iStart:F2})");

        // Assert - j's duration was extended to satisfy FF constraint
        // Required: j.Finish >= i.Finish => j.Start + j.Duration >= i.Start + i.Duration
        // j.Duration >= (0.5 + 85) - 0.7 = 84.8
        Assert.True(jDuration >= 84.8 - 0.01,
            $"j.Duration ({jDuration:F2}) should be extended to at least 84.8 to satisfy FF constraint");

        // Assert - j's duration respects the lower bound
        Assert.True(jDuration >= taskJ.MinDuration,
            $"j.Duration ({jDuration:F2}) should be at least MinDuration ({taskJ.MinDuration})");

        // Assert - FF is satisfied
        Assert.True(jFinish >= iFinish - 0.01,
            $"FF violated: j.Finish ({jFinish:F2}) should be >= i.Finish ({iFinish:F2})");
    }
}