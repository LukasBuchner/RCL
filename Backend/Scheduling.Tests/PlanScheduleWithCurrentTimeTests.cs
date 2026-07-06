using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling.Tests;

sealed partial class PlanScheduleTests
{
    /// <summary>
    ///     Unit‑tests for <see cref="ExecutionGraphExtensions.PlanSchedule" /> with new requirements.
    /// </summary>
    public sealed class PlanScheduleWithCurrentTimeTests
    {
        // Helper to create a basic IPlannedSkillExecution (fixed duration)
        private static Mock<IPlannedSkillExecution> CreateBaseTaskMock(string? name = null, double duration = 1.0)
        {
            var mockTask = new Mock<IPlannedSkillExecution>();
            mockTask.SetupGet(t => t.Id).Returns(Guid.NewGuid());
            mockTask.SetupGet(t => t.PlannedDuration).Returns(duration); // For fixed, this is the duration
            mockTask.SetupProperty(t => t.PlannedStartTime, 0);
            mockTask.SetupProperty(t => t.PlannedFinishTime, 0);
            if (name is not null)
                mockTask.Setup(t => t.ToString()).Returns(name);
            return mockTask;
        }

        // Helper to create a fixed duration task (not ISkillExecution)
        private static IPlannedSkillExecution CreateFixedPlannedTask(string? name = null, double duration = 1.0)
        {
            return CreateBaseTaskMock(name, duration).Object;
        }

        // Helper to create a fixed ISkillExecution
        private static ISkillExecution CreateFixedSkill(
            string? name = null,
            double initialPlannedDuration = 1.0,
            double? actualStartTime = null,
            double? actualFinishTime = null,
            double? estimatedDuration = null)
        {
            var mock = new Mock<ISkillExecution>(); // Implements IPlannedSkillExecution
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
            mock.SetupGet(t => t.EstimatedFinishTime).Returns(
                actualStartTime.HasValue && (estimatedDuration ?? initialPlannedDuration) > 0
                    ? actualStartTime + (estimatedDuration ?? initialPlannedDuration)
                    : null);
            mock.SetupGet(t => t.ActualDuration).Returns(
                actualStartTime.HasValue && actualFinishTime.HasValue
                    ? actualFinishTime - actualStartTime
                    : null);

            if (name is not null)
                mock.Setup(t => t.ToString()).Returns(name);
            return mock.Object;
        }

        // Helper to create an adaptive IAdaptiveSkillExecution
        private static IAdaptiveSkillExecution CreateAdaptiveSkill(
            string? name = null,
            double minDuration = 1.0,
            double? actualStartTime = null,
            double? actualFinishTime = null,
            double? estimatedDuration = null) // EstimatedDuration is less critical for adaptive but can exist
        {
            var mock =
                new Mock<IAdaptiveSkillExecution>(); // Implements IAdaptivePlannedSkillExecution, ISkillExecution
            mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
            mock.SetupProperty(t => t.PlannedStartTime, double.NaN);
            mock.SetupProperty(t => t.PlannedFinishTime, double.NaN);
            mock.SetupProperty(t => t.PlannedDuration, minDuration); // Solver will set initial default

            mock.SetupGet(t => t.MinDuration).Returns(minDuration);

            mock.SetupGet(t => t.ActualStartTime).Returns(actualStartTime);
            mock.SetupGet(t => t.ActualFinishTime).Returns(actualFinishTime);
            mock.SetupGet(t => t.EstimatedDuration).Returns(estimatedDuration);

            // Calculated properties
            mock.SetupGet(t => t.IsRunning).Returns(actualStartTime.HasValue && !actualFinishTime.HasValue);
            mock.SetupGet(t => t.IsFinished).Returns(actualStartTime.HasValue && actualFinishTime.HasValue);
            mock.SetupGet(t => t.EstimatedFinishTime).Returns(
                actualStartTime.HasValue && (estimatedDuration ?? minDuration) > 0 // Use minDuration if no estimate
                    ? actualStartTime + (estimatedDuration ?? minDuration)
                    : null);
            mock.SetupGet(t => t.ActualDuration).Returns(
                actualStartTime.HasValue && actualFinishTime.HasValue
                    ? actualFinishTime - actualStartTime
                    : null);

            if (name is not null)
                mock.Setup(t => t.ToString()).Returns(name);
            return mock.Object;
        }

        [Fact]
        public void PlanSchedule_EmptyGraph_WithCurrentTime_CompletesSuccessfully()
        {
            var graph = Graph([], []);
            var resultGraph = graph.PlanSchedule(10.0); // currentTime = 10

            Assert.Same(graph, resultGraph);
            Assert.Empty(resultGraph.SkillExecutions);
        }

        [Fact]
        public void PlanSchedule_SingleFixedTask_NotStarted_CurrentTime10_StartsAt10()
        {
            var taskA = CreateFixedSkill("A", 2.0); // Not ISkillExecution, so treated as not started
            var graph = Graph([taskA], []);
            var currentTime = 10.0;

            graph.PlanSchedule(currentTime);

            Assert.Equal(currentTime, taskA.PlannedStartTime, 5); // Starts at 10 (relative to original T0)
            Assert.Equal(2.0, taskA.PlannedDuration, 5);
            Assert.Equal(currentTime + 2.0, taskA.PlannedFinishTime, 5); // Finishes at 12
        }

        [Fact]
        public void PlanSchedule_SingleFixedPlannedTask_NotStarted_CurrentTime10_StartsAt10_GlobalShiftTo0()
        {
            var taskA = CreateFixedPlannedTask("A", 2.0); // Not ISkillExecution, so treated as not started
            var graph = Graph([taskA], []);
            var currentTime = 10.0;

            graph.PlanSchedule(currentTime, true);
            // After global shift, the earliest task (taskA) starts at 0
            Assert.Equal(0.0, taskA.PlannedStartTime, 5);
            Assert.Equal(2.0, taskA.PlannedDuration, 5);
            Assert.Equal(2.0, taskA.PlannedFinishTime, 5);
        }


        [Fact]
        public void PlanSchedule_SingleAdaptiveTask_NotStarted_CurrentTime5_StartsAt5_UsesMinDuration_GlobalShiftTo0()
        {
            var taskA = CreateAdaptiveSkill("A");
            var graph = Graph([taskA], []);
            var currentTime = 5.0;

            graph.PlanSchedule(currentTime, true);
            // For a trivial SCC, it uses MinDuration. Starts at the currentTime, then global shift.
            Assert.Equal(0.0, taskA.PlannedStartTime, 5); // After global shift
            Assert.Equal(1.0, taskA.PlannedDuration, 5);
            Assert.Equal(1.0, taskA.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_FixedTask_AlreadyFinished_TimesAreFixed_GlobalShiftRespectsActualStart()
        {
            var taskA = CreateFixedSkill("A", 5.0, 2.0, 7.0);
            var graph = Graph([taskA], []);
            const double currentTime = 10.0; // The current time is after the task finished

            graph.PlanSchedule(currentTime, true);

            // Global shift will make taskA start at 0, as it's the only task and its actual start is earliest
            Assert.Equal(0.0, taskA.PlannedStartTime, 5); // ActualStartTime 2.0 - global shift 2.0
            Assert.Equal(5.0, taskA.PlannedFinishTime, 5); // ActualFinishTime 7.0 - global shift 2.0
            Assert.Equal(5.0, taskA.PlannedDuration, 5); // ActualDuration
        }

        [Fact]
        public void PlanSchedule_AdaptiveTask_AlreadyFinished_TimesAndDurationAreFixed_GlobalShiftWorking()
        {
            var taskA = CreateAdaptiveSkill("A", 1.0, 3.0, 8.0);
            // Actual duration is 5.0
            var graph = Graph([taskA], []);
            var currentTime = 12.0;

            graph.PlanSchedule(currentTime, true);

            Assert.Equal(0.0, taskA.PlannedStartTime, 5); // ActualStartTime 3.0 - global shift 3.0
            Assert.Equal(5.0, taskA.PlannedDuration, 5); // ActualDuration
            Assert.Equal(5.0, taskA.PlannedFinishTime, 5); // ActualFinishTime 8.0 - global shift 3.0
        }

        [Fact]
        public void PlanSchedule_FixedTask_Running_UsesActualStartAndEstimatedDuration()
        {
            // Task started at t=5, current time t=8, estimated total duration 6 (so 3 more units)
            var taskA = CreateFixedSkill("A", 10.0, 5.0, estimatedDuration: 6.0);
            var graph = Graph([taskA], []);
            var currentTime = 8.0;

            graph.PlanSchedule(currentTime);

            // ActualStart is 5. This will be the earliest time, so the global shift makes it 0.
            Assert.Equal(5.0, taskA.PlannedStartTime, 5);
            Assert.Equal(6.0, taskA.PlannedDuration, 5);
            Assert.Equal(11.0, taskA.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_FixedTask_Running_UsesActualStartAndPlannedDurationIfNoEstimate()
        {
            // Task started at t=5, current time t=8, planned duration 7 (so 4 more units)
            var taskA = CreateFixedSkill("A", 7.0, 5.0, estimatedDuration: null);
            var graph = Graph([taskA], []);
            var currentTime = 8.0;

            graph.PlanSchedule(currentTime);

            Assert.Equal(5.0, taskA.PlannedStartTime, 5);
            Assert.Equal(7.0, taskA.PlannedDuration, 5);
            Assert.Equal(12.0, taskA.PlannedFinishTime, 5);
        }


        [Fact]
        public void PlanSchedule_AdaptiveTask_Running_UsesActualStart_DurationConstrainedByElapsed_GlobalShift()
        {
            // Started at t=2, current t=5. MinDuration=4.
            // Elapsed = 3. So, PlannedDuration must be >= 3.
            // For a trivial SCC, it will pick Max(MinDuration, Elapsed) = Max(4, 3) = 4.
            var taskA = CreateAdaptiveSkill("A", 4.0, 2.0);
            var graph = Graph([taskA], []);
            var currentTime = 5.0;

            graph.PlanSchedule(currentTime, true);

            // ActualStart is 2. Global shift makes it 0.
            Assert.Equal(0.0, taskA.PlannedStartTime, 5);
            Assert.Equal(4.0, taskA.PlannedDuration, 5); // Max(MinDuration, currentTime - ActualStart) for trivial
            Assert.Equal(4.0, taskA.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_TwoTasks_A_Finished_B_NotStarted_CurrentTimeLater_B_StartsAfterA_And_CurrentTime()
        {
            var taskA = CreateFixedSkill("AF", 3.0, 1.0, 4.0); // Finishes at 4
            var taskB = CreateFixedSkill("BN", 2.0); // Not started
            var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
            var graph = Graph([taskA, taskB], deps);
            var currentTime = 6.0; // The current time is after A finishes, but B must also respect this

            graph.PlanSchedule(currentTime);

            // Task A: ActualStart=1, ActualFinish=4. 
            // So, A: S=1, F=4
            Assert.Equal(1.0, taskA.PlannedStartTime, 5);
            Assert.Equal(4.0, taskA.PlannedFinishTime, 5);

            // Task B: Must start after A (PlannedFinishTime=4) AND after currentTime 
            // So B starts at max(4, 6) = 6
            Assert.Equal(6.0, taskB.PlannedStartTime, 5);
            Assert.Equal(2.0, taskB.PlannedDuration, 5);
            Assert.Equal(8.0, taskB.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_TwoTasks_A_Running_B_NotStarted_CurrentTimeDuringA_B_StartsAfterA_And_CurrentTime()
        {
            // A: ActualStart=2, EstDur=5 (so est. finish = 7). CurrentTime=4.
            var taskA = CreateFixedSkill("AR", 10.0, 2.0, estimatedDuration: 5.0);
            var taskB = CreateFixedSkill("BN", 3.0);
            var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
            var graph = Graph([taskA, taskB], deps);
            var currentTime = 4.0;

            graph.PlanSchedule(currentTime);

            // Task A: ActualStart=2
            // So, A: S=2, D=5 (EstDur), F=7
            Assert.Equal(2.0, taskA.PlannedStartTime, 5);
            Assert.Equal(5.0, taskA.PlannedDuration, 5);
            Assert.Equal(7.0, taskA.PlannedFinishTime, 5);

            // Task B: Must start after A (PlannedFinishTime=7) AND after currentTime 4
            // So B starts at max(7, 2) = 7
            Assert.Equal(7.0, taskB.PlannedStartTime, 5);
            Assert.Equal(3.0, taskB.PlannedDuration, 5);
            Assert.Equal(10.0, taskB.PlannedFinishTime, 5);
        }


        /// <summary>
        ///     Two fixed tasks in a FS cycle (A→B, B→A), with A already finished. The
        ///     graph is event-level cyclic regardless of task state, so
        ///     <c>ValidateModel</c> rejects it before scheduling.
        /// </summary>
        [Fact]
        public void PlanSchedule_FsCycle_OneTaskFinished_ThrowsScheduleModelException()
        {
            var taskA = CreateFixedSkill("A", 1.0, 0.0, 1.0);
            var taskB = CreateFixedSkill("B");
            var deps = new[]
            {
                Dep(taskA, taskB, DependencyType.FinishToStart),
                Dep(taskB, taskA, DependencyType.FinishToStart)
            };
            var graph = Graph([taskA, taskB], deps);
            const double currentTime = 2.0;

            var exception = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(currentTime));
            Assert.Contains("Event-level", exception.Message);
        }

        [Fact]
        public void PlanSchedule_RunningAdaptiveTask_DurationMustBeAtLeastElapsed_InLP()
        {
            // Task A: adaptive, running. ActualStart=0, MinDur=5, MaxDur=10. CurrentTime=3.
            // So, D_A must be >= 3. And D_A >= MinDur (5). So D_A >= 5.
            var taskA = CreateAdaptiveSkill("A", 5.0, 0.0);
            var taskB = CreateFixedSkill("B");
            var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
            var graph = Graph([taskA, taskB], deps); // This will be one SCC, solved by LP.
            var currentTime = 3.0;

            graph.PlanSchedule(
                currentTime); // PlanSchedule calls SolveWithLinearProgramming on the whole graph if 1 SCC

            // LP: S_A = 0. D_A in [5,10]. D_A >= (currentTime - S_A) = (3-0) = 3. So D_A in [5,10].
            // Minimize makespan will choose D_A = 5. F_A = 5.
            // S_B >= F_A => S_B >= 5. S_B >= currentTime (3). So S_B >= 5.
            // F_B = S_B + 1 = 6.
            // Global shift by S_A (0).
            Assert.Equal(0.0, taskA.PlannedStartTime, 5);
            Assert.Equal(5.0, taskA.PlannedDuration, 5);
            Assert.Equal(5.0, taskA.PlannedFinishTime, 5);

            Assert.Equal(5.0, taskB.PlannedStartTime, 5);
            Assert.Equal(1.0, taskB.PlannedDuration, 5);
            Assert.Equal(6.0, taskB.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_RunningAdaptiveTask_DurationMustBeAtLeastElapsed_MinDurSmaller_InLP()
        {
            // Task A: adaptive, running. ActualStart=0, MinDur=2, MaxDur=10. CurrentTime=3.
            // So, D_A must be >= 3. And D_A >= MinDur (2). So D_A >= 3.
            var taskA = CreateAdaptiveSkill("A", 2.0, 0.0);
            var taskB = CreateFixedSkill("B");
            var deps = new[] { Dep(taskA, taskB, DependencyType.FinishToStart) };
            var graph = Graph([taskA, taskB], deps);
            var currentTime = 3.0;

            graph.PlanSchedule(currentTime);

            // LP: S_A = 0. D_A in [2,10]. D_A >= (currentTime - S_A) = (3-0) = 3. So D_A in [3,10].
            // Minimize makespan will choose D_A = 3. F_A = 3.
            // S_B >= F_A => S_B >= 3. S_B >= currentTime (3). So S_B >= 3.
            // F_B = S_B + 1 = 4.
            // Global shift by S_A (0).
            Assert.Equal(0.0, taskA.PlannedStartTime, 5);
            Assert.Equal(3.0, taskA.PlannedDuration, 5);
            Assert.Equal(3.0, taskA.PlannedFinishTime, 5);

            Assert.Equal(3.0, taskB.PlannedStartTime, 5);
            Assert.Equal(1.0, taskB.PlannedDuration, 5);
            Assert.Equal(4.0, taskB.PlannedFinishTime, 5);
        }

        [Fact]
        public void PlanSchedule_InvalidCurrentTime_ThrowsArgumentOutOfRangeException()
        {
            var taskA = CreateFixedPlannedTask("A");
            var graph = Graph([taskA], []);
            Assert.Throws<ArgumentOutOfRangeException>("currentTime", () => graph.PlanSchedule(-1.0));
        }

        [Fact]
        public void PlanSchedule_RunningTask_ActualStartAfterCurrentTime_ThrowsModelException()
        {
            var taskA = CreateFixedSkill("A", actualStartTime: 10.0);
            var graph = Graph([taskA], []);
            var currentTime = 5.0;
            var ex = Assert.Throws<ScheduleModelException>(() => graph.PlanSchedule(currentTime));
            Assert.Contains(
                $"Running task {taskA.Id} has invalid ActualStartTime ({taskA.ActualStartTime}) relative to currentTime ({currentTime})",
                ex.Message
            );
        }

        [Fact]
        public void YetAnotherTest()
        {
            var taskA = CreateFixedSkill("A", 4);
            var taskB = CreateFixedSkill("B"); // Not started
            var deps = new[]
            {
                Dep(taskA, taskB, DependencyType.FinishToStart)
            };
            var graph = Graph([taskA, taskB], deps);
            const double currentTime = 2.0;

            graph.PlanSchedule(currentTime);

            Assert.Equal(2.0, taskA.PlannedStartTime, 5);
            Assert.Equal(4.0, taskA.PlannedDuration, 5);
            Assert.Equal(6.0, taskA.PlannedFinishTime, 5);

            Assert.Equal(6.0, taskB.PlannedStartTime, 5);
            Assert.Equal(1.0, taskB.PlannedDuration, 5);
            Assert.Equal(7.0, taskB.PlannedFinishTime, 5);
        }

        // A zero-extent ordering carrier implements the Core marker IZeroExtentOrderingCarrier with zero
        // planned duration and no execution state — the shape the scheduler materializes for a leafless
        // container (empty task / empty branch).
        private static IPlannedSkillExecution CreateZeroExtentRep(string? name = null)
        {
            var mock = new Mock<IZeroExtentOrderingCarrier>();
            mock.SetupGet(t => t.Id).Returns(Guid.NewGuid());
            mock.SetupGet(t => t.PlannedDuration).Returns(0.0);
            mock.SetupProperty(t => t.PlannedStartTime, 0);
            mock.SetupProperty(t => t.PlannedFinishTime, 0);
            if (name is not null)
                mock.Setup(t => t.ToString()).Returns(name);
            return mock.Object;
        }

        /// <summary>
        ///     A zero-extent rep is exempt from the current-time floor: on a chain A→rep→B with A already
        ///     finished and the current time well past A's finish, the rep is anchored to A's finish rather
        ///     than pushed to the current time. A genuine not-started successor B is still floored, so the fix
        ///     is surgical — only the carrier is exempt.
        /// </summary>
        [Fact]
        public void PlanSchedule_ZeroExtentRep_AnchorsToPredecessorFinish_WhileRealSuccessorStillFloored()
        {
            var a = CreateFixedSkill("A", 45.0, 0.0, 45.0); // finished [0,45]
            var rep = CreateZeroExtentRep("rep"); // leafless container carrier
            var b = CreateFixedSkill("B", 65.0); // real, not started
            var deps = new[]
            {
                Dep(a, rep, DependencyType.FinishToStart),
                Dep(rep, b, DependencyType.FinishToStart)
            };
            var graph = Graph([a, rep, b], deps);

            graph.PlanSchedule(130.0); // current time far past A's finish

            Assert.Equal(0.0, a.PlannedStartTime, 5);
            Assert.Equal(45.0, a.PlannedFinishTime, 5);

            // Rep sits at A's finish (45), NOT at the current time (130), and stays zero-extent.
            Assert.Equal(45.0, rep.PlannedStartTime, 5);
            Assert.Equal(45.0, rep.PlannedFinishTime, 5);

            // B is a genuine not-started task, so it is still floored to the current time: max(45, 130) = 130.
            Assert.Equal(130.0, b.PlannedStartTime, 5);
            Assert.Equal(195.0, b.PlannedFinishTime, 5);
        }

        /// <summary>
        ///     The scramble at the LP level: on A→rep→B with A and B both already finished and the current time
        ///     past B's finish, the zero-extent rep must not be floored to the current time, so it cannot drag
        ///     the already-finished successor B forward — B keeps its observed [45,110] window.
        /// </summary>
        [Fact]
        public void PlanSchedule_ZeroExtentRep_DoesNotDragFinishedSuccessorForward()
        {
            var a = CreateFixedSkill("A", 45.0, 0.0, 45.0); // finished [0,45]  (e.g. sdf)
            var rep = CreateZeroExtentRep("rep"); // leafless container carrier (e.g. dcd)
            var b = CreateFixedSkill("B", 65.0, 45.0, 110.0); // finished [45,110] (e.g. jkl)
            var deps = new[]
            {
                Dep(a, rep, DependencyType.FinishToStart),
                Dep(rep, b, DependencyType.FinishToStart)
            };
            var graph = Graph([a, rep, b], deps);

            graph.PlanSchedule(130.0); // current time past B's finish

            // Rep anchored to A's finish, not floored to 130.
            Assert.Equal(45.0, rep.PlannedStartTime, 5);
            Assert.Equal(45.0, rep.PlannedFinishTime, 5);

            // Finished B keeps its observed window; it is NOT dragged to the current time through the rep.
            Assert.Equal(45.0, b.PlannedStartTime, 5);
            Assert.Equal(110.0, b.PlannedFinishTime, 5);
        }

        /// <summary>
        ///     A chain of two zero-extent reps (a nested empty compound) between a finished predecessor and a
        ///     finished successor: every carrier anchors to the predecessor's finish and none is floored, so the
        ///     finished successor is again undisturbed.
        /// </summary>
        [Fact]
        public void PlanSchedule_ChainOfTwoZeroExtentReps_AllAnchorToPredecessor_FinishedSuccessorUndisturbed()
        {
            var a = CreateFixedSkill("A", 45.0, 0.0, 45.0); // finished [0,45]
            var rep1 = CreateZeroExtentRep("rep1");
            var rep2 = CreateZeroExtentRep("rep2");
            var b = CreateFixedSkill("B", 65.0, 45.0, 110.0); // finished [45,110]
            var deps = new[]
            {
                Dep(a, rep1, DependencyType.FinishToStart),
                Dep(rep1, rep2, DependencyType.FinishToStart),
                Dep(rep2, b, DependencyType.FinishToStart)
            };
            var graph = Graph([a, rep1, rep2, b], deps);

            graph.PlanSchedule(200.0); // current time far past B's finish

            Assert.Equal(45.0, rep1.PlannedStartTime, 5);
            Assert.Equal(45.0, rep1.PlannedFinishTime, 5);
            Assert.Equal(45.0, rep2.PlannedStartTime, 5);
            Assert.Equal(45.0, rep2.PlannedFinishTime, 5);

            Assert.Equal(45.0, b.PlannedStartTime, 5);
            Assert.Equal(110.0, b.PlannedFinishTime, 5);
        }

        /// <summary>
        ///     The LP faithfully propagates a running predecessor's estimated finish to a not-started successor:
        ///     on running A → rep → not-started B, B starts at max(F_A, currentTime). While A is mid-run
        ///     (currentTime below F_A) that is F_A, so B tracks A's estimated finish exactly. This confirms the
        ///     LP is not the source of a drifting successor — its input (the estimate) is.
        /// </summary>
        [Fact]
        public void PlanSchedule_RunningPredecessor_NotStartedSuccessor_TracksPredecessorEstimatedFinish()
        {
            var a = CreateFixedSkill("A", 65.0, 0.0, estimatedDuration: 48.0); // running, F = 48
            var rep = CreateZeroExtentRep("rep");
            var b = CreateFixedSkill("B", 5.0); // not started
            var deps = new[]
            {
                Dep(a, rep, DependencyType.FinishToStart),
                Dep(rep, b, DependencyType.FinishToStart)
            };
            var graph = Graph([a, rep, b], deps);

            graph.PlanSchedule(10.0); // current time well below A's estimated finish

            Assert.Equal(0.0, a.PlannedStartTime, 5);
            Assert.Equal(48.0, a.PlannedFinishTime, 5);
            Assert.Equal(48.0, rep.PlannedStartTime, 5);
            // B = max(F_rep = 48, currentTime = 10) = 48
            Assert.Equal(48.0, b.PlannedStartTime, 5);
        }

        /// <summary>
        ///     Changing only the running predecessor's estimate moves the not-started successor by exactly that
        ///     delta — the LP is a faithful, linear propagator, so any drift the successor shows is inherited
        ///     from the estimate, not introduced by the schedule.
        /// </summary>
        [Fact]
        public void PlanSchedule_NotStartedSuccessor_MovesExactlyWithPredecessorEstimate()
        {
            double SuccessorStart(double estimate)
            {
                var a = CreateFixedSkill("A", 65.0, 0.0, estimatedDuration: estimate);
                var rep = CreateZeroExtentRep("rep");
                var b = CreateFixedSkill("B", 5.0);
                var deps = new[]
                {
                    Dep(a, rep, DependencyType.FinishToStart),
                    Dep(rep, b, DependencyType.FinishToStart)
                };
                Graph([a, rep, b], deps).PlanSchedule(10.0);
                return b.PlannedStartTime;
            }

            Assert.Equal(2.0, SuccessorStart(50.0) - SuccessorStart(48.0), 5);
        }
    }
}