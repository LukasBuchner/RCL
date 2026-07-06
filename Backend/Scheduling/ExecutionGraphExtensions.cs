using System.Globalization;
using FHOOE.Freydis.Scheduling.Core;
using FHOOE.Freydis.Scheduling.Support.Logging;
using Google.OrTools.LinearSolver;
using Microsoft.Extensions.Logging;
using static System.Math;

namespace FHOOE.Freydis.Scheduling;

/// <summary>
///     Utility algorithms for <see cref="IExecutionGraph" /> and related scheduling components.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - LPSchedulingValidity.lean — FS cycle infeasibility, SCC DAG structure, solution composability
///     - AdaptiveDurationExtension.lean — adaptive duration change safety
/// </remarks>
public static class ExecutionGraphExtensions
{
    /// <summary>
    ///     One reusable GLOP solver per worker thread. The first call on a thread pays the
    ///     OR-Tools native instantiation cost; subsequent calls reset state with
    ///     <c>Solver.Clear()</c>, eliminating per-call solver-creation P/Invoke overhead.
    ///     Safe under <c>Parallel.ForEach</c> because each worker thread sees its own
    ///     instance, never shared across threads.
    /// </summary>
    private static readonly ThreadLocal<Solver> PooledGlopSolver = new(() =>
        Solver.CreateSolver("GLOP")
        ?? throw new InvalidOperationException("OR-Tools could not create an LP solver (GLOP)."));

    /// <summary>
    ///     The maximum number of parallel workers used when solving independent SCCs.
    ///     Pinned to <see cref="Environment.ProcessorCount" /> so the scheduling pipeline
    ///     consumes every available logical core during the parallel LP phase.
    /// </summary>
    private static readonly ParallelOptions SccParallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    /// <summary>
    ///     Finds all strongly connected components (SCCs) in the <paramref name="graph" /> using Tarjan's algorithm.
    ///     Each component is returned as its own <see cref="IStronglyConnectedComponent" />.
    /// </summary>
    /// <param name="graph">The execution graph to analyse.</param>
    /// <returns>A read-only list of strongly connected components found in the graph.</returns>
    /// <remarks>
    ///     Complexity: O(V+E) where V is the number of tasks and E is the number of dependencies.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="graph" /> is <c>null</c>.</exception>
    public static IReadOnlyList<IStronglyConnectedComponent> GetStronglyConnectedComponents(
        this IExecutionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.SkillExecutions.Count == 0)
            return [];

        // Build outgoing edges with SS/FF/SF treated as bidirectional "glue"
        // This ensures tasks coupled by these constraint types are grouped together in SCCs
        var outgoingEdges = BuildOutgoingEdgesWithBidirectionalGlue(graph.Dependencies);

        var tarjanContext = new TarjanContext(graph.SkillExecutions.Count, outgoingEdges, graph.Dependencies);

        foreach (var task in graph.SkillExecutions)
            if (!tarjanContext.Indices.ContainsKey(task))
                StrongConnect(task, tarjanContext);

        return tarjanContext.Components;
    }

    /// <summary>
    ///     Builds the outgoing edges dictionary for SCC detection, treating SS/FF/SF dependencies
    ///     as bidirectional "glue" edges that couple tasks together.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why bidirectional for SS/FF/SF?</b>
    ///     </para>
    ///     <para>
    ///         - <b>FS (Finish-to-Start)</b>: Pure sequencing (A finishes, then B starts).
    ///           A can be fully determined before B. Kept unidirectional.
    ///     </para>
    ///     <para>
    ///         - <b>SS (Start-to-Start)</b>: S_B >= S_A. Times are coupled; solving A and B together
    ///           allows the LP to find optimal solutions. Made bidirectional.
    ///     </para>
    ///     <para>
    ///         - <b>FF (Finish-to-Finish)</b>: F_B >= F_A. Times are coupled; tasks should be
    ///           solved together. Made bidirectional.
    ///     </para>
    ///     <para>
    ///         - <b>SF (Start-to-Finish)</b>: F_B >= S_A. Cross-coupling between start and finish
    ///           times requires joint solving. Made bidirectional.
    ///     </para>
    ///     <para>
    ///         By adding reverse edges for SS/FF/SF, Tarjan's algorithm will naturally group
    ///         coupled tasks into the same SCC, eliminating the need for separate ConstrainedGroup detection.
    ///     </para>
    /// </remarks>
    /// <param name="dependencies">The dependencies between tasks.</param>
    /// <returns>A dictionary mapping each task to its outgoing edges (including synthetic reverse edges for SS/FF/SF).</returns>
    private static Dictionary<IPlannedSkillExecution, IReadOnlyList<IPlannedSkillExecution>>
        BuildOutgoingEdgesWithBidirectionalGlue(IReadOnlyList<Dependency> dependencies)
    {
        var edges = new Dictionary<IPlannedSkillExecution, List<IPlannedSkillExecution>>();

        foreach (var dep in dependencies)
        {
            // Add the original edge: Source → Target
            if (!edges.TryGetValue(dep.Source, out var sourceList))
            {
                sourceList = [];
                edges[dep.Source] = sourceList;
            }

            sourceList.Add(dep.Target);

            // For SS/FF/SF, also add reverse edge: Target → Source (bidirectional "glue")
            if (dep.Type != DependencyType.FinishToStart)
            {
                if (!edges.TryGetValue(dep.Target, out var targetList))
                {
                    targetList = [];
                    edges[dep.Target] = targetList;
                }

                targetList.Add(dep.Source);
            }
        }

        // Convert to IReadOnlyList
        return edges.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<IPlannedSkillExecution>)kvp.Value);
    }

    /// <summary>
    ///     Recursive helper for Tarjan's algorithm to find SCCs.
    /// </summary>
    /// <param name="v">The current task being visited.</param>
    /// <param name="context">The context containing algorithm state (<see cref="TarjanContext" />).</param>
    private static void StrongConnect(IPlannedSkillExecution v, TarjanContext context)
    {
        context.Indices[v] = context.Index;
        context.LowLinks[v] = context.Index;
        context.Index++;
        context.Stack.Push(v);
        context.OnStack.Add(v);

        if (context.OutgoingEdges.TryGetValue(v, out var targets))
            foreach (var w in targets)
                if (!context.Indices.TryGetValue(w, out var value)) // w has not been visited yet
                {
                    StrongConnect(w, context);
                    context.LowLinks[v] = Min(context.LowLinks[v], context.LowLinks[w]);
                }
                else if (context.OnStack.Contains(w)) // w is on stack, so (v, w) is a back-edge
                {
                    context.LowLinks[v] = Min(context.LowLinks[v], value);
                }

        if (context.LowLinks[v] != context.Indices[v]) return; // v is not the root of an SCC

        // v is the root of an SCC
        var sccTasks = new List<IPlannedSkillExecution>();
        IPlannedSkillExecution poppedTask;
        do
        {
            poppedTask = context.Stack.Pop();
            context.OnStack.Remove(poppedTask);
            sccTasks.Add(poppedTask);
        } while (!ReferenceEquals(poppedTask, v));

        var sccDeps = context.AllDependencies
            .Where(d => sccTasks.Contains(d.Source) && sccTasks.Contains(d.Target))
            .ToList();

        // Assuming StronglyConnectedComponent is a concrete class implementing IStronglyConnectedComponent
        context.Components.Add(new StronglyConnectedComponent { SkillExecutions = sccTasks, Dependencies = sccDeps });
    }

    /// <summary>
    ///     Creates a feasible schedule (start, finish, and duration for each task) that honours all dependencies,
    ///     considering current task states and a specified current time, using linear programming.
    /// </summary>
    /// <param name="graph">The execution graph to schedule.</param>
    /// <param name="currentTime">The current time of the system. Tasks not yet started cannot begin before this time.</param>
    /// <returns>The same <paramref name="graph" /> instance, populated with planned times and durations.</returns>
    /// <remarks>
    ///     <para>
    ///         - Finished tasks retain their actual start, finish, and duration.
    ///         - Running tasks start at their <c>ActualStartTime</c>. Their duration is determined by the LP solver
    ///         (for adaptive tasks, within min/max bounds and >= elapsed time) or uses <c>EstimatedDuration</c>
    ///         (or original <c>PlannedDuration</c>) for fixed tasks.
    ///         - Not-yet-started tasks begin at or after <paramref name="currentTime" />.
    ///         - The entire schedule is normalised so that the earliest task (actual or planned) starts at <c>t=0</c>.
    ///         - <see cref="IPlannedSkillExecution.PlannedDuration" /> is updated to reflect the duration used by the solver.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="graph" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="currentTime" /> is negative.</exception>
    /// <exception cref="ScheduleModelException">If the graph model is invalid (e.g. duplicate IDs, inconsistent task states).</exception>
    /// <exception cref="ScheduleInfeasibleException">If no feasible schedule exists, according to the LP solver.</exception>
    /// <exception cref="InvalidOperationException">If the OR-Tools solver cannot be created.</exception>
    public static IExecutionGraph SolveWithLinearProgramming(this IExecutionGraph graph, double currentTime = 0)
    {
        return graph.SolveWithLinearProgramming(currentTime, null);
    }

    /// <summary>
    ///     Solves the scheduling problem using OR-Tools Linear Programming (GLOP solver) with optional trace logging.
    /// </summary>
    /// <param name="graph">The execution graph to schedule.</param>
    /// <param name="currentTime">The current time from which to schedule forward.</param>
    /// <param name="logger">Optional logger for trace-level scheduling details.</param>
    /// <returns>The same graph instance with updated timing information.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="graph" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="currentTime" /> is negative.</exception>
    /// <exception cref="ScheduleModelException">If the graph model is invalid (e.g. duplicate IDs, inconsistent task states).</exception>
    /// <exception cref="ScheduleInfeasibleException">If no feasible schedule exists, according to the LP solver.</exception>
    /// <exception cref="InvalidOperationException">If the OR-Tools solver cannot be created.</exception>
    public static IExecutionGraph SolveWithLinearProgramming(this IExecutionGraph graph, double currentTime,
        ILogger? logger)
    {
        return SolveWithLinearProgrammingCore(graph, currentTime, logger, false);
    }

    /// <summary>
    ///     Core LP-solving implementation that optionally skips the up-front structural
    ///     validation. Callers that have already validated the enclosing graph (e.g.
    ///     <see cref="PlanSchedule(IExecutionGraph,double,bool)" /> when invoking the
    ///     solver on each SCC sub-graph) pass <paramref name="skipValidation" /> as
    ///     <see langword="true" /> to avoid redundant work on the parallel critical path.
    /// </summary>
    /// <param name="graph">The execution graph to schedule.</param>
    /// <param name="currentTime">The current time from which to schedule forward.</param>
    /// <param name="logger">Optional logger for trace-level scheduling details.</param>
    /// <param name="skipValidation">
    ///     When <see langword="true" />, skips the call to <c>ValidateModel</c>. Only safe
    ///     when the caller has already validated a graph that contains <paramref name="graph" />
    ///     as a sub-graph.
    /// </param>
    /// <returns>The same graph instance with updated timing information.</returns>
    private static IExecutionGraph SolveWithLinearProgrammingCore(IExecutionGraph graph, double currentTime,
        ILogger? logger, bool skipValidation)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (currentTime < 0)
            throw new ArgumentOutOfRangeException(nameof(currentTime), "Current time cannot be negative.");

        if (graph.SkillExecutions.Count == 0) return graph;

        if (!skipValidation)
            ValidateModel(graph, currentTime);

        // Log graph structure for diagnostics
        var adaptiveCount = graph.SkillExecutions.Count(s => s is IAdaptivePlannedSkillExecution);
        var finishedCount = graph.SkillExecutions.Count(s => s is ISkillExecution { IsFinished: true });
        var runningCount = graph.SkillExecutions.Count(s => s is ISkillExecution { IsRunning: true });
        var pendingCount = graph.SkillExecutions.Count - finishedCount - runningCount;

        logger?.LogGraphStructure(
            graph.SkillExecutions.Count,
            graph.Dependencies.Count,
            adaptiveCount,
            finishedCount,
            runningCount,
            pendingCount,
            currentTime);

        // Reuse this thread's pooled GLOP solver. Clear() wipes all variables, constraints,
        // and the objective so each call starts from a clean model.
        var solver = PooledGlopSolver.Value!;
        solver.Clear();

        logger?.LogSolverInitialized(graph.SkillExecutions.Count, graph.Dependencies.Count);

        var startVars = new Dictionary<Guid, Variable>(graph.SkillExecutions.Count);
        var finishVars = new Dictionary<Guid, Variable>(graph.SkillExecutions.Count);
        var durationVars = new Dictionary<Guid, Variable>();

        foreach (var task in graph.SkillExecutions)
            DefineTaskVariablesAndConstraints(solver, task, currentTime, startVars, finishVars, durationVars, logger);

        logger?.LogDependencyConstraintSetup(graph.Dependencies.Count);
        foreach (var dep in graph.Dependencies)
            AddDependencyConstraint(solver, dep, startVars, finishVars, logger);

        var makespan = solver.MakeNumVar(0.0, double.PositiveInfinity, "MAKESPAN");
        foreach (var fVar in finishVars.Values) solver.Add(makespan >= fVar);
        solver.Minimize(makespan);

        var numVariables = solver.NumVariables();
        var numConstraints = solver.NumConstraints();
        logger?.LogMakespanObjectiveSetUp(numVariables, numConstraints);

        var status = solver.Solve();
        var statusString = status.ToString();
        logger?.LogSolverStatusCompleted(statusString);

        // Log solver result with detailed diagnostics
        var isSuccess = status == Solver.ResultStatus.OPTIMAL || status == Solver.ResultStatus.FEASIBLE;
        logger?.LogSolverResult(
            statusString,
            numVariables,
            numConstraints,
            isSuccess ? makespan.SolutionValue() : null,
            isSuccess ? "SUCCESS" : "INFEASIBLE");

        if (!isSuccess)
            throw new ScheduleInfeasibleException(nameof(graph),
                $"No feasible schedule exists. Solver status = {status}");

        var makespanValue = makespan.SolutionValue();
        logger?.LogApplyingSolutions(graph.SkillExecutions.Count, makespanValue);
        foreach (var task in graph.SkillExecutions)
            UpdateTaskTimesFromSolution(task, startVars, finishVars, durationVars);

        return graph;
    }

    /// <summary>
    ///     The execution-state kind a task carries into the schedule LP. Each kind pins the task's start,
    ///     finish, and duration variables differently.
    /// </summary>
    private enum TaskExecutionState
    {
        /// <summary>A completed task: start, finish, and duration are pinned to its observed actual values.</summary>
        Finished,

        /// <summary>A running task: start is pinned to its observed actual start; duration is its estimate.</summary>
        Running,

        /// <summary>A not-yet-started task: its start is floored to the current time.</summary>
        NotStarted,

        /// <summary>
        ///     A zero-extent ordering carrier (a leafless container's placeholder): floor-exempt, with
        ///     <c>Finish = Start</c>. Its start is bounded only by its dependency predecessors' finishes and the
        ///     makespan objective, so it settles at the maximum finish of its predecessors.
        /// </summary>
        OrderingCarrier
    }

    /// <summary>
    ///     Classifies a task's execution state for the schedule LP. A zero-extent ordering carrier is
    ///     recognised first: it carries no execution state and must not be treated as a not-yet-started task
    ///     subject to the current-time floor. Otherwise the task is finished, running, or not started per its
    ///     <see cref="ISkillExecution" /> state.
    /// </summary>
    /// <param name="task">The task to classify.</param>
    /// <returns>The task's <see cref="TaskExecutionState" />.</returns>
    private static TaskExecutionState ClassifyTaskExecutionState(IPlannedSkillExecution task)
    {
        if (task is IZeroExtentOrderingCarrier)
            return TaskExecutionState.OrderingCarrier;

        return task switch
        {
            ISkillExecution { IsFinished: true } => TaskExecutionState.Finished,
            ISkillExecution { IsRunning: true } => TaskExecutionState.Running,
            _ => TaskExecutionState.NotStarted
        };
    }

    /// <summary>
    ///     Defines solver variables and constraints for a single task based on its state.
    /// </summary>
    /// <param name="solver">The OR-Tools solver instance.</param>
    /// <param name="task">The task to model.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="startVars">Dictionary to store start time variables.</param>
    /// <param name="finishVars">Dictionary to store finish time variables.</param>
    /// <param name="durationVars">Dictionary to store duration variables (for adaptive tasks).</param>
    /// <param name="logger">Optional logger for trace-level details.</param>
    private static void DefineTaskVariablesAndConstraints(
        Solver solver,
        IPlannedSkillExecution task,
        double currentTime,
        Dictionary<Guid, Variable> startVars,
        Dictionary<Guid, Variable> finishVars,
        Dictionary<Guid, Variable> durationVars,
        ILogger? logger = null)
    {
        // Log skill constraint at the start
        var skillExecution = task as ISkillExecution;
        var adaptiveTask = task as IAdaptivePlannedSkillExecution;
        var state = skillExecution?.IsFinished == true ? "FINISHED" :
            skillExecution?.IsRunning == true ? "RUNNING" : "PENDING";

        logger?.LogSkillConstraint(
            task.Id,
            task.Id.ToString("N")[
                ..8], // Use first 8 chars of ID as name since Name property not available in this module
            adaptiveTask != null,
            state,
            task.PlannedDuration,
            adaptiveTask?.MinDuration,
            skillExecution?.ActualStartTime,
            skillExecution?.ActualFinishTime,
            currentTime);

        var idSuffix = task.Id.ToString("N");
        var sVar = solver.MakeNumVar(0.0, double.PositiveInfinity, $"S_{idSuffix}");
        var fVar = solver.MakeNumVar(0.0, double.PositiveInfinity, $"F_{idSuffix}");
        startVars[task.Id] = sVar;
        finishVars[task.Id] = fVar;

        logger?.LogTaskVariablesCreated(task.Id, idSuffix);

        // ISkillExecution provides state (IsFinished, IsRunning, ActualTimes)
        // IAdaptivePlannedSkillExecution provides MinDuration (duration unbounded above)
        // IPlannedSkillExecution provides PlannedDuration (original) and ID

        switch (ClassifyTaskExecutionState(task))
        {
            case TaskExecutionState.Finished:
                {
                    var seFinished = (ISkillExecution)task;
                    logger?.LogFinishedSkillTiming(
                        task.Id, seFinished.ActualStartTime!.Value, seFinished.ActualFinishTime!.Value,
                        seFinished.ActualDuration!.Value);

                    solver.Add(sVar == seFinished.ActualStartTime!.Value);
                    solver.Add(fVar == seFinished.ActualFinishTime!.Value);
                    if (task is IAdaptivePlannedSkillExecution adaptiveFinished) // Finished adaptive task
                    {
                        logger?.LogFinishedAdaptiveSkillDuration(
                            task.Id, idSuffix, seFinished.ActualDuration!.Value, adaptiveFinished.MinDuration);

                        var dVar = solver.MakeNumVar(seFinished.ActualDuration!.Value, seFinished.ActualDuration!.Value,
                            $"D_{idSuffix}");
                        durationVars[task.Id] = dVar;
                        solver.Add(fVar - sVar - dVar == 0.0);
                    }
                    else // Finished the fixed-duration task
                    {
                        logger?.LogFinishedFixedSkillDuration(
                            task.Id, idSuffix, seFinished.ActualDuration!.Value);
                        solver.Add(fVar - sVar == seFinished.ActualDuration!.Value);
                    }

                    break;
                }
            case TaskExecutionState.Running:
                {
                    var seRunning = (ISkillExecution)task;
                    logger?.LogRunningSkillTiming(
                        task.Id, seRunning.ActualStartTime!.Value, currentTime,
                        currentTime - seRunning.ActualStartTime!.Value);

                    solver.Add(sVar == seRunning.ActualStartTime!.Value);
                    if (task is IAdaptivePlannedSkillExecution adaptiveRunning)
                    {
                        var minElapsed = Max(0, currentTime - seRunning.ActualStartTime!.Value);
                        var effectiveMin = Max(adaptiveRunning.MinDuration, minElapsed);
                        logger?.LogRunningAdaptiveSkillDuration(
                            task.Id, idSuffix, adaptiveRunning.MinDuration, minElapsed, effectiveMin);

                        var dVar = solver.MakeNumVar(adaptiveRunning.MinDuration, double.PositiveInfinity,
                            $"D_{idSuffix}");
                        durationVars[task.Id] = dVar;
                        solver.Add(fVar - sVar - dVar == 0.0);
                        solver.Add(dVar >= minElapsed); // Must be at least elapsed
                    }
                    else // Running fixed-duration task
                    {
                        var effectiveDuration = seRunning.EstimatedDuration ?? task.PlannedDuration;
                        logger?.LogRunningFixedSkillDuration(
                            task.Id, idSuffix, effectiveDuration, seRunning.EstimatedDuration,
                            task.PlannedDuration);
                        solver.Add(fVar - sVar == effectiveDuration);
                    }

                    break;
                }
            case TaskExecutionState.OrderingCarrier:
                {
                    // A zero-extent ordering carrier (a leafless container's ZeroExtentFiringPlaceholder) is
                    // exempt from the current-time floor: its start is bounded only by its dependency
                    // predecessors' finishes and the makespan objective, and it has zero extent
                    // (Finish = Start). Flooring it to the current time would push it — and any finished
                    // successor gated behind it — forward.
                    solver.Add(fVar - sVar == 0.0);
                    break;
                }
            // Not started the task
            default:
                {
                    solver.Add(sVar >= currentTime);
                    if (task is IAdaptivePlannedSkillExecution adaptiveNotStarted)
                    {
                        logger?.LogNotStartedAdaptiveSkill(
                            task.Id, idSuffix, currentTime, adaptiveNotStarted.MinDuration);

                        var dVar = solver.MakeNumVar(adaptiveNotStarted.MinDuration, double.PositiveInfinity,
                            $"D_{idSuffix}");
                        durationVars[task.Id] = dVar;
                        solver.Add(fVar - sVar - dVar == 0.0);
                    }
                    else // Not started a fixed-duration task
                    {
                        var effectiveDuration = (task as ISkillExecution)?.EstimatedDuration ?? task.PlannedDuration;
                        logger?.LogNotStartedFixedSkill(
                            task.Id, idSuffix, currentTime, effectiveDuration);
                        solver.Add(fVar - sVar == effectiveDuration);
                    }

                    break;
                }
        }
    }

    /// <summary>
    ///     Checks if an already-executed dependency was violated.
    ///     Returns true if the constraint was violated by the actual execution times.
    /// </summary>
    /// <param name="dep">The dependency to check.</param>
    /// <param name="source">The source skill execution (must be running or finished).</param>
    /// <param name="target">The target skill execution (must be running or finished).</param>
    /// <returns>A tuple containing whether the dependency was violated and a description of the violation.</returns>
    private static (bool Violated, string ViolationDescription) CheckDependencyViolation(
        Dependency dep,
        ISkillExecution source,
        ISkillExecution target)
    {
        return dep.Type switch
        {
            DependencyType.FinishToStart => CheckFinishToStartViolation(source, target),
            DependencyType.StartToStart => CheckStartToStartViolation(source, target),
            DependencyType.FinishToFinish => CheckFinishToFinishViolation(source, target),
            DependencyType.StartToFinish => CheckStartToFinishViolation(source, target),
            _ => (false, string.Empty)
        };
    }

    /// <summary>
    ///     Checks FinishToStart violation: Target.Start should be >= Source.Finish
    /// </summary>
    private static (bool Violated, string ViolationDescription) CheckFinishToStartViolation(
        ISkillExecution source,
        ISkillExecution target)
    {
        // For running/finished skills, use ActualFinishTime if available, otherwise estimate from ActualStart + PlannedDuration
        var sourceFinish = source.ActualFinishTime ?? source.ActualStartTime + source.PlannedDuration;
        var targetStart = target.ActualStartTime!.Value;

        if (targetStart < sourceFinish)
            return (true,
                $"Target started at {targetStart:F3}s but Source finishes at {sourceFinish:F3}s (expected Target.Start >= Source.Finish)");

        return (false, string.Empty);
    }

    /// <summary>
    ///     Checks StartToStart violation: Target.Start should be >= Source.Start
    /// </summary>
    private static (bool Violated, string ViolationDescription) CheckStartToStartViolation(
        ISkillExecution source,
        ISkillExecution target)
    {
        var sourceStart = source.ActualStartTime!.Value;
        var targetStart = target.ActualStartTime!.Value;

        if (targetStart < sourceStart)
            return (true,
                $"Target started at {targetStart:F3}s but Source started at {sourceStart:F3}s (expected Target.Start >= Source.Start)");

        return (false, string.Empty);
    }

    /// <summary>
    ///     Checks FinishToFinish violation: Target.Finish should be >= Source.Finish
    /// </summary>
    private static (bool Violated, string ViolationDescription) CheckFinishToFinishViolation(
        ISkillExecution source,
        ISkillExecution target)
    {
        // For running/finished skills, use ActualFinishTime if available, otherwise estimate from ActualStart + PlannedDuration
        var sourceFinish = source.ActualFinishTime ?? source.ActualStartTime + source.PlannedDuration;
        var targetFinish = target.ActualFinishTime ?? target.ActualStartTime + target.PlannedDuration;

        if (targetFinish < sourceFinish)
            return (true,
                $"Target finishes at {targetFinish:F3}s but Source finishes at {sourceFinish:F3}s (expected Target.Finish >= Source.Finish)");

        return (false, string.Empty);
    }

    /// <summary>
    ///     Checks StartToFinish violation: Target.Finish should be >= Source.Start
    /// </summary>
    private static (bool Violated, string ViolationDescription) CheckStartToFinishViolation(
        ISkillExecution source,
        ISkillExecution target)
    {
        var sourceStart = source.ActualStartTime!.Value;
        // For running/finished skills, use ActualFinishTime if available, otherwise estimate from ActualStart + PlannedDuration
        var targetFinish = target.ActualFinishTime ?? target.ActualStartTime + target.PlannedDuration;

        if (targetFinish < sourceStart)
            return (true,
                $"Target finishes at {targetFinish:F3}s but Source started at {sourceStart:F3}s (expected Target.Finish >= Source.Start)");

        return (false, string.Empty);
    }

    /// <summary>
    ///     Adds a dependency constraint to the solver.
    ///     Skips constraints where both source and target are already executed (running or finished)
    ///     to avoid INFEASIBLE errors when past execution violated the dependency.
    /// </summary>
    /// <param name="solver">The OR-Tools solver instance.</param>
    /// <param name="dep">The dependency to model.</param>
    /// <param name="startVars">Dictionary of start time variables.</param>
    /// <param name="finishVars">Dictionary of finish time variables.</param>
    /// <param name="logger">Optional logger for trace-level details.</param>
    /// <exception cref="ScheduleModelException">If an unsupported dependency type is encountered.</exception>
    private static void AddDependencyConstraint(
        Solver solver,
        Dependency dep,
        Dictionary<Guid, Variable> startVars,
        Dictionary<Guid, Variable> finishVars,
        ILogger? logger = null)
    {
        // Check if both source and target have their RELEVANT TIMES fixed
        var sourceExec = dep.Source as ISkillExecution;
        var targetExec = dep.Target as ISkillExecution;
        var sourceIsAdaptive = dep.Source is IAdaptivePlannedSkillExecution;
        var targetIsAdaptive = dep.Target is IAdaptivePlannedSkillExecution;

        var sourceName = dep.Source.Id.ToString("N")[..8];
        var targetName = dep.Target.Id.ToString("N")[..8];

        // Determine if the relevant times for this dependency are BOTH fixed
        // For adaptive skills, the finish time is NOT fixed while running (it can be adjusted within min/max range)
        var bothRelevantTimesFixed = dep.Type switch
        {
            // FinishToStart: Source finish and target start must both be fixed
            DependencyType.FinishToStart =>
                (sourceExec?.IsFinished == true ||
                 (sourceExec?.IsRunning == true && !sourceIsAdaptive)) && // Source finish is fixed
                (targetExec?.IsRunning == true ||
                 targetExec?.IsFinished == true), // Target start is fixed (always true for running)

            // StartToStart: Both start times must be fixed (always true for running/finished)
            DependencyType.StartToStart =>
                (sourceExec?.IsRunning == true || sourceExec?.IsFinished == true) && // Source start is fixed
                (targetExec?.IsRunning == true || targetExec?.IsFinished == true), // Target start is fixed

            // FinishToFinish: Both finish times must be fixed
            DependencyType.FinishToFinish =>
                (sourceExec?.IsFinished == true ||
                 (sourceExec?.IsRunning == true && !sourceIsAdaptive)) && // Source finish is fixed
                (targetExec?.IsFinished == true ||
                 (targetExec?.IsRunning == true && !targetIsAdaptive)), // Target finish is fixed

            // StartToFinish: Source start and target finish must both be fixed
            DependencyType.StartToFinish =>
                (sourceExec?.IsRunning == true || sourceExec?.IsFinished == true) && // Source start is fixed
                (targetExec?.IsFinished == true ||
                 (targetExec?.IsRunning == true && !targetIsAdaptive)), // Target finish is fixed

            _ => false
        };

        if (bothRelevantTimesFixed)
        {
            // Both relevant times are fixed - cannot change the past
            // Check for violation and log warning if violated
            var (violated, violationDescription) = CheckDependencyViolation(dep, sourceExec!, targetExec!);

            if (violated && logger?.IsEnabled(LogLevel.Warning) == true)
                logger.LogDependencyViolation(
                    dep.Type.ToString(),
                    dep.Source.Id,
                    sourceName,
                    sourceExec!.ActualStartTime,
                    dep.Target.Id,
                    targetName,
                    targetExec!.ActualStartTime,
                    violationDescription);

            // Skip adding constraint (both relevant times are fixed)
            return;
        }

        // At least one skill is pending - add constraint normally
        var sSrc = startVars[dep.Source.Id];
        var fSrc = finishVars[dep.Source.Id];
        var sTgt = startVars[dep.Target.Id];
        var fTgt = finishVars[dep.Target.Id];

        string constraintDescription;
        switch (dep.Type)
        {
            case DependencyType.FinishToStart:
                solver.Add(sTgt - fSrc >= 0.0);
                constraintDescription = $"S_target >= F_source (Start of {targetName} >= Finish of {sourceName})";
                break;
            case DependencyType.StartToStart:
                solver.Add(sTgt - sSrc >= 0.0);
                constraintDescription = $"S_target >= S_source (Start of {targetName} >= Start of {sourceName})";
                break;
            case DependencyType.StartToFinish:
                solver.Add(fTgt - sSrc >= 0.0);
                constraintDescription = $"F_target >= S_source (Finish of {targetName} >= Start of {sourceName})";
                break;
            case DependencyType.FinishToFinish:
                solver.Add(fTgt - fSrc >= 0.0);
                constraintDescription = $"F_target >= F_source (Finish of {targetName} >= Finish of {sourceName})";
                break;
            default:
                throw new ScheduleModelException(nameof(dep), $"Unsupported dependency type {dep.Type}");
        }

        var depTypeString = dep.Type.ToString();
        logger?.LogDependencyConstraint(
            depTypeString,
            dep.Source.Id,
            sourceName,
            dep.Target.Id,
            targetName,
            sourceIsAdaptive,
            targetIsAdaptive,
            constraintDescription);
    }

    /// <summary>
    ///     Updates a task's planned start, finish, and duration based on solver results.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="startVars">Dictionary of solved start time variables.</param>
    /// <param name="finishVars">Dictionary of solved finish time variables.</param>
    /// <param name="durationVars">Dictionary of solved duration variables.</param>
    private static void UpdateTaskTimesFromSolution(
        IPlannedSkillExecution task,
        Dictionary<Guid, Variable> startVars,
        Dictionary<Guid, Variable> finishVars,
        Dictionary<Guid, Variable> durationVars)
    {
        task.PlannedStartTime = startVars[task.Id].SolutionValue();
        task.PlannedFinishTime = finishVars[task.Id].SolutionValue();

        var se = task as ISkillExecution;

        if (durationVars.TryGetValue(task.Id, out var dVar)) // Adaptive task (or finished adaptive)
        {
            var solvedDuration = dVar.SolutionValue();
            task.PlannedDuration = solvedDuration;
        }
        else if (se?.IsFinished == true) // Fixed-duration, finished
        {
            task.PlannedDuration = se.ActualDuration!.Value;
        }
        else
        {
            switch (se?.IsRunning)
            {
                // Fixed-duration, running
                case true when task is not IAdaptivePlannedSkillExecution:
                    {
                        // If an estimated duration was used for the LP, update PlannedDuration.
                        // Otherwise, task.PlannedDuration (original) was used and is still correct.
                        if (se.EstimatedDuration.HasValue) task.PlannedDuration = se.EstimatedDuration.Value;
                        break;
                    }
                // Fixed-duration, not started, with estimate
                case false when !se.IsFinished &&
                                task is not IAdaptivePlannedSkillExecution &&
                                se.EstimatedDuration.HasValue:
                    task.PlannedDuration = se.EstimatedDuration.Value;
                    break;
                    // All other cases: preserve existing PlannedDuration
            }
        }
        // For fixed-duration, not started, no estimate: task.PlannedDuration remains its original value, which was used in LP.
    }


    /// <summary>
    ///     Calculates start, finish, and duration for every <see cref="IPlannedSkillExecution" />
    ///     in the <paramref name="graph" />, considering task states and <paramref name="currentTime" />.
    ///     The graph’s topology remains intact.
    /// </summary>
    /// <param name="graph">The execution graph to plan.</param>
    /// <param name="currentTime">The current time of the system. Tasks not yet started cannot begin before this time.</param>
    /// <param name="applyGlobalShift">
    ///     If <c>true</c> (default), the entire schedule is shifted so the earliest task starts at <c>t=0</c>.
    ///     If <c>false</c>, times are absolute respecting <paramref name="currentTime" /> and actual start times.
    /// </param>
    /// <returns>The same <paramref name="graph" /> instance, populated with planned times and durations.</returns>
    /// <remarks>
    ///     <para>Algorithm outline:</para>
    ///     <list type="number">
    ///         <item>Decompose the graph into strongly connected components (SCCs).</item>
    ///         <item>
    ///             Solve SCCs classified as <see cref="StronglyConnectedComponentKind.AdaptiveCycle" />
    ///             using linear programming (<see cref="SolveWithLinearProgramming" />).
    ///         </item>
    ///         <item>
    ///             Initialise tasks in trivial SCCs:
    ///             - Finished tasks use actual times/duration.
    ///             - Running tasks use actual start; duration is adaptive (min/max/elapsed) or fixed (estimated/original).
    ///             - Not-started tasks start at/after <paramref name="currentTime" />; duration is adaptive (min) or fixed
    ///             (estimated/original).
    ///             - <see cref="IPlannedSkillExecution.PlannedDuration" /> is updated.
    ///         </item>
    ///         <item>Normalise times within each SCC so its local earliest event is at its internal <c>t=0</c>.</item>
    ///         <item>
    ///             Construct a condensation graph (DAG of SCCs) and perform a topological sort to determine
    ///             the absolute start offset for each SCC's internal <c>t=0</c>, respecting inter-SCC dependencies
    ///             and ensuring not-started tasks within SCCs adhere to <paramref name="currentTime" />.
    ///         </item>
    ///         <item>Apply these absolute offsets to all tasks.</item>
    ///         <item>
    ///             Optionally (if <paramref name="applyGlobalShift" /> is true), shift the entire schedule so the overall
    ///             earliest task starts at <c>t=0</c>.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Event-level acyclicity is enforced by <c>ValidateModel</c> before SCC analysis runs,
    ///         so every cycle reaching this point is structurally solvable.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="graph" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="currentTime" /> is negative.</exception>
    /// <exception cref="ScheduleModelException">If the graph model is invalid or contains an event-level cycle.</exception>
    /// <exception cref="ScheduleInfeasibleException">If an adaptive cycle is unsolvable by the LP solver.</exception>
    public static IExecutionGraph PlanSchedule(this IExecutionGraph graph, double currentTime = 0,
        bool applyGlobalShift = false)
    {
        return graph.PlanSchedule(currentTime, applyGlobalShift, null);
    }

    /// <summary>
    ///     Plans the execution schedule with optional trace logging.
    /// </summary>
    /// <param name="graph">The execution graph to schedule.</param>
    /// <param name="currentTime">The current time from which to schedule forward.</param>
    /// <param name="applyGlobalShift">Whether to apply a global time shift to normalize the schedule.</param>
    /// <param name="logger">Optional logger for trace-level scheduling details.</param>
    /// <returns>The same graph instance with updated timing information.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="graph" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="currentTime" /> is negative.</exception>
    /// <exception cref="ScheduleModelException">If the graph model is invalid or an internal error occurs.</exception>
    /// <exception cref="ScheduleInfeasibleException">If a fixed cycle is detected or an adaptive cycle is unsolvable.</exception>
    public static IExecutionGraph PlanSchedule(this IExecutionGraph graph, double currentTime,
        bool applyGlobalShift, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (currentTime < 0)
            throw new ArgumentOutOfRangeException(nameof(currentTime), "Current time cannot be negative.");

        if (graph.SkillExecutions.Count == 0) return graph;
        ValidateModel(graph, currentTime);

        // Step 1: Run SCC detection with SS/FF/SF treated as bidirectional "glue" edges
        // This naturally groups coupled tasks together, eliminating the need for separate ConstrainedGroup detection
        var componentsWithInfo = graph.GetStronglyConnectedComponents()
            .Select((scc, idx) => new ComponentInfo(scc, scc.ClassifyStronglyConnectedComponent(), idx))
            .ToArray();

        // Stores internal start time relative to SCC's anchor, and duration.
        var taskInternalData = new Dictionary<Guid, TaskTimingData>(graph.SkillExecutions.Count);
        // Stores the original absolute time that corresponds to t=0 for each SCC's internal timeline.
        var sccOriginalAbsoluteAnchorTimes = new double[componentsWithInfo.Length];

        // Step 2: Initialize all SCC tasks (LP for multi-task SCCs, direct init for trivial)
        InitializeAllSccTasks(componentsWithInfo, currentTime, taskInternalData, sccOriginalAbsoluteAnchorTimes,
            logger);

        // Step 4: Build condensation graph and calculate final start times
        var taskToSccLookup = BuildTaskToSccLookup(componentsWithInfo);
        var (condensationGraphInDegree, condensationGraphOutEdges) =
            BuildCondensationGraph(graph, taskToSccLookup, componentsWithInfo.Length);

        var sccFinalAbsoluteStartTimes = CalculateSccFinalStartTimes(
            componentsWithInfo,
            currentTime,
            taskInternalData,
            sccOriginalAbsoluteAnchorTimes,
            condensationGraphInDegree,
            condensationGraphOutEdges,
            nameof(graph));

        // Step 5: Extend adaptive durations for inter-SCC FF/SF constraints on running tasks
        ExtendAdaptiveDurationsForInterSccConstraints(graph, taskToSccLookup, taskInternalData,
            sccFinalAbsoluteStartTimes);

        // Step 6: Apply final times to all tasks
        ApplyFinalTimesToGraphTasks(componentsWithInfo, taskInternalData, sccFinalAbsoluteStartTimes);

        if (applyGlobalShift)
            ApplyGlobalTimeShiftToSchedule(graph);

        return graph;
    }

    /// <summary>
    ///     Initialises all tasks within their SCCs, solving adaptive cycles with LP and setting up trivial SCCs.
    ///     Populates taskInternalData and sccOriginalAbsoluteAnchorTimes.
    /// </summary>
    /// <param name="componentsWithInfo">Array of component information.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="taskInternalData">Dictionary to populate with task internal start times and durations.</param>
    /// <param name="sccOriginalAbsoluteAnchorTimes">Array to populate with SCC anchor times.</param>
    /// <param name="logger">Optional logger for trace-level details.</param>
    /// <param name="tasksInConstrainedGroups">Set of task IDs already handled by ConstrainedGroup LP solving.</param>
    private static void InitializeAllSccTasks(
        ComponentInfo[] componentsWithInfo,
        double currentTime,
        Dictionary<Guid, TaskTimingData> taskInternalData,
        double[] sccOriginalAbsoluteAnchorTimes,
        ILogger? logger = null,
        IReadOnlySet<Guid>? tasksInConstrainedGroups = null)
    {
        tasksInConstrainedGroups ??= new HashSet<Guid>();

        var lpSolveSccs = new List<ComponentInfo>(componentsWithInfo.Length);
        var trivialSccs = new List<ComponentInfo>(componentsWithInfo.Length);
        foreach (var entry in componentsWithInfo)
            if (entry.Classification.Kind == StronglyConnectedComponentKind.AdaptiveCycle ||
                entry.Classification.Kind == StronglyConnectedComponentKind.FixedCoupledGroup)
                lpSolveSccs.Add(entry);
            else
                trivialSccs.Add(entry);

        // Each multi-task SCC owns a disjoint set of tasks and a thread-local GLOP solver,
        // so the LP solves are independent. Run them across every available core via the
        // pinned MaxDegreeOfParallelism = Environment.ProcessorCount. Writes during the LP
        // touch only this SCC's task objects. Skip the per-SCC structural validation: the
        // parent graph passed ValidateModel before SCC decomposition, and each SCC is a
        // subset of that already-validated graph.
        if (lpSolveSccs.Count > 0)
            Parallel.ForEach(lpSolveSccs, SccParallelOptions, entry =>
                SolveWithLinearProgrammingCore(entry.Component, currentTime, logger, true));

        // Merge LP outputs into the shared bookkeeping after the parallel phase completes,
        // so the Dictionary writes happen on a single thread.
        foreach (var entry in lpSolveSccs)
        {
            sccOriginalAbsoluteAnchorTimes[entry.OriginalIndex] = 0;

            foreach (var task in entry.Component.SkillExecutions)
                taskInternalData[task.Id] = new TaskTimingData(task.PlannedStartTime, task.PlannedDuration);
        }

        foreach (var entry in trivialSccs)
            sccOriginalAbsoluteAnchorTimes[entry.OriginalIndex] =
                InitializeAndNormalizeTrivialSccTasks(entry.Component, currentTime, taskInternalData, logger,
                    tasksInConstrainedGroups);
    }

    /// <summary>
    ///     Initialises tasks in a trivial SCC and normalises their times relative to the SCC's earliest start time.
    /// </summary>
    /// <param name="scc">The trivial strongly connected component.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="taskInternalData">Dictionary to populate with task internal start times and durations.</param>
    /// <param name="logger">Optional logger for trace-level details.</param>
    /// <param name="tasksInConstrainedGroups">Set of task IDs already handled by ConstrainedGroup LP solving.</param>
    /// <returns>The absolute anchor time (earliest start time) of this SCC before normalisation.</returns>
    private static double InitializeAndNormalizeTrivialSccTasks(
        IStronglyConnectedComponent scc,
        double currentTime,
        Dictionary<Guid, TaskTimingData> taskInternalData,
        ILogger? logger = null,
        IReadOnlySet<Guid>? tasksInConstrainedGroups = null)
    {
        tasksInConstrainedGroups ??= new HashSet<Guid>();

        if (!scc.SkillExecutions.Any()) return currentTime; // Or some other appropriate default for empty SCC

        var minStartTimeInScc = double.PositiveInfinity;
        var sccTaskTimes = new Dictionary<Guid, (double AbsoluteStart, double Duration)>();

        foreach (var task in scc.SkillExecutions)
        {
            // Skip tasks already handled by ConstrainedGroup LP - they already have taskInternalData set
            if (tasksInConstrainedGroups.Contains(task.Id))
            {
                // Use the already-set values for minStartTime calculation
                var existingData = taskInternalData[task.Id];
                var absoluteStart = task.PlannedStartTime; // Already set by LP
                sccTaskTimes[task.Id] = (absoluteStart, existingData.Duration);
                minStartTimeInScc = Min(minStartTimeInScc, absoluteStart);
                continue;
            }

            var se = task as ISkillExecution;
            var adaptiveTask = task as IAdaptivePlannedSkillExecution;
            double initialStartTime;
            double effectiveDuration;
            string stateClassification;

            switch (ClassifyTaskExecutionState(task))
            {
                case TaskExecutionState.Finished:
                    initialStartTime = se!.ActualStartTime!.Value;
                    effectiveDuration = se.ActualDuration!.Value;
                    stateClassification = "FINISHED";

                    logger?.LogPlanClassifyFinished(
                        task.Id, stateClassification, adaptiveTask != null,
                        se.ActualStartTime!.Value, se.ActualFinishTime!.Value, se.ActualDuration!.Value,
                        currentTime, initialStartTime, effectiveDuration);
                    break;

                case TaskExecutionState.Running:
                    initialStartTime = se!.ActualStartTime!.Value;
                    if (task is IAdaptivePlannedSkillExecution adaptiveRunning)
                    {
                        var elapsed = Max(0, currentTime - se.ActualStartTime!.Value);
                        effectiveDuration = Max(adaptiveRunning.MinDuration, elapsed);
                        stateClassification = "RUNNING_ADAPTIVE";

                        logger?.LogPlanClassifyRunningAdaptive(
                            task.Id, stateClassification,
                            se.ActualStartTime!.Value, se.EstimatedDuration,
                            currentTime, elapsed, adaptiveRunning.MinDuration,
                            initialStartTime, effectiveDuration);
                    }
                    else // Fixed-duration running
                    {
                        effectiveDuration = se.EstimatedDuration ?? task.PlannedDuration;
                        stateClassification = "RUNNING_FIXED";

                        logger?.LogPlanClassifyRunningFixed(
                            task.Id, stateClassification,
                            se.ActualStartTime!.Value, se.EstimatedDuration, task.PlannedDuration,
                            currentTime, initialStartTime, effectiveDuration);
                    }

                    break;

                case TaskExecutionState.OrderingCarrier:
                    // A zero-extent ordering carrier (leafless rep) is exempt from the current-time floor:
                    // anchoring it at 0 lets the SCC composition place it at its predecessors' finishes. Flooring
                    // it to the current time would push it — and any finished successor gated behind it — forward.
                    // It has zero extent, so Finish = Start.
                    initialStartTime = 0.0;
                    effectiveDuration = 0.0;
                    break;

                default: // Not started
                    initialStartTime = currentTime; // Dependencies will push tentative start
                    if (task is IAdaptivePlannedSkillExecution adaptiveNotStarted)
                    {
                        effectiveDuration = adaptiveNotStarted.MinDuration;
                        stateClassification = "NOT_STARTED_ADAPTIVE";

                        logger?.LogPlanClassifyNotStartedAdaptive(
                            task.Id, stateClassification,
                            adaptiveNotStarted.MinDuration,
                            currentTime, initialStartTime, effectiveDuration);
                    }
                    else // Fixed-duration, not started
                    {
                        effectiveDuration = se?.EstimatedDuration ?? task.PlannedDuration;
                        stateClassification = "NOT_STARTED_FIXED";

                        logger?.LogPlanClassifyNotStartedFixed(
                            task.Id, stateClassification,
                            se?.EstimatedDuration, task.PlannedDuration,
                            currentTime, initialStartTime, effectiveDuration);
                    }

                    break;
            }

            task.PlannedDuration = effectiveDuration; // Update planned duration based on current state/estimates
            sccTaskTimes[task.Id] = (initialStartTime, effectiveDuration);
            minStartTimeInScc = Min(minStartTimeInScc, initialStartTime);
        }

        // Normalise times within the SCC and populate taskInternalData
        foreach (var task in scc.SkillExecutions)
        {
            var (absoluteStart, duration) = sccTaskTimes[task.Id];
            taskInternalData[task.Id] = new TaskTimingData(absoluteStart - minStartTimeInScc, duration);
        }

        return minStartTimeInScc; // This is the SCC's original absolute anchor time
    }


    /// <summary>
    ///     Builds a lookup dictionary from task ID to its SCC's original index.
    /// </summary>
    /// <param name="componentsWithInfo">Array of component information.</param>
    /// <returns>A dictionary mapping task IDs to SCC indices.</returns>
    private static Dictionary<Guid, int> BuildTaskToSccLookup(ComponentInfo[] componentsWithInfo)
    {
        return componentsWithInfo
            .SelectMany(entry =>
                entry.Component.SkillExecutions.Select(t => (TaskId: t.Id, SccIndex: entry.OriginalIndex)))
            .ToDictionary(x => x.TaskId, x => x.SccIndex);
    }

    /// <summary>
    ///     Builds the condensation graph (in-degrees and out-edges between SCCs).
    /// </summary>
    /// <param name="graph">The original execution graph.</param>
    /// <param name="taskToSccLookup">Lookup from task ID to SCC index.</param>
    /// <param name="sccCount">Total number of SCCs.</param>
    /// <returns>A tuple containing the in-degree array for SCCs and a list of edges between SCCs.</returns>
    /// <exception cref="ScheduleModelException">If a dependency references a task not found in the SCC lookup.</exception>
    private static (int[] InDegree, List<(int FromSccIndex, int ToSccIndex, Dependency Dep)> OutEdges)
        BuildCondensationGraph(IExecutionGraph graph, Dictionary<Guid, int> taskToSccLookup, int sccCount)
    {
        var inDegree = new int[sccCount];
        var outEdges = new List<(int FromSccIndex, int ToSccIndex, Dependency Dep)>();

        foreach (var dep in graph.Dependencies)
        {
            if (!taskToSccLookup.TryGetValue(dep.Source.Id, out var fromSccIdx) ||
                !taskToSccLookup.TryGetValue(dep.Target.Id, out var toSccIdx))
                throw new ScheduleModelException(nameof(graph),
                    "Dependency references a task not found in any SCC component mapping.");

            if (fromSccIdx == toSccIdx) continue; // Skip intra-SCC dependencies

            outEdges.Add((fromSccIdx, toSccIdx, dep));
            inDegree[toSccIdx]++;
        }

        return (inDegree, outEdges);
    }

    /// <summary>
    ///     Calculates the final absolute start times for each SCC's internal t=0 using a topological sort approach.
    /// </summary>
    /// <param name="componentsInfo">Information about each SCC.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="taskInternalData">Internal timing data for each task.</param>
    /// <param name="sccOriginalAnchorTimes">Initial absolute anchor times for SCCs.</param>
    /// <param name="inDegree">In-degree array for the condensation graph.</param>
    /// <param name="condensationEdges">Edges of the condensation graph.</param>
    /// <param name="graphParamName">Name of the graph parameter for exception messages.</param>
    /// <param name="tasksInConstrainedGroups">Set of task IDs already handled by ConstrainedGroup LP solving.</param>
    /// <returns>An array of final absolute start times for each SCC's internal t=0.</returns>
    /// <exception cref="ScheduleModelException">If the condensation graph has a cycle.</exception>
    private static double[] CalculateSccFinalStartTimes(
        ComponentInfo[] componentsInfo,
        double currentTime,
        Dictionary<Guid, TaskTimingData> taskInternalData,
        double[] sccOriginalAnchorTimes,
        int[] inDegree, // This will be modified
        List<(int FromSccIndex, int ToSccIndex, Dependency Dep)> condensationEdges,
        string graphParamName,
        IReadOnlySet<Guid>? tasksInConstrainedGroups = null)
    {
        tasksInConstrainedGroups ??= new HashSet<Guid>();

        var sccCount = componentsInfo.Length;
        var sccFinalStartTimes = new double[sccCount];
        Array.Copy(sccOriginalAnchorTimes, sccFinalStartTimes, sccCount);

        var readyQueue = new Queue<int>(Enumerable.Range(0, sccCount).Where(idx => inDegree[idx] == 0));
        var processedCount = 0;

        while (readyQueue.TryDequeue(out var currentSccIdx))
        {
            processedCount++;
            var currentSccInfo = componentsInfo[currentSccIdx];

            // Ensure not-started tasks in non-adaptive (trivial) SCCs respect currentTime
            // Adaptive SCCs handled this via LP constraints.
            if (currentSccInfo.Classification.Kind != StronglyConnectedComponentKind.AdaptiveCycle)
            {
                var currentSccProposedStartTime = sccFinalStartTimes[currentSccIdx];
                foreach (var taskInScc in currentSccInfo.Component.SkillExecutions)
                    if (taskInScc is ISkillExecution { IsRunning: false, IsFinished: false }) // Not started
                    {
                        var (taskInternalStart, _) = taskInternalData[taskInScc.Id];
                        // This SCC cannot start earlier than currentTime allows for its not-started tasks.
                        // scc_start_time + task_internal_start >= currentTime
                        // scc_start_time >= currentTime - task_internal_start
                        currentSccProposedStartTime = Max(currentSccProposedStartTime, currentTime - taskInternalStart);
                    }

                sccFinalStartTimes[currentSccIdx] = currentSccProposedStartTime;
            }

            // Propagate constraints to successors
            foreach (var (_, targetSccIdx, dep) in condensationEdges.Where(e => e.FromSccIndex == currentSccIdx))
            {
                var (srcTaskInternalStart, srcTaskDuration) = taskInternalData[dep.Source.Id];
                var srcTaskInternalFinish = srcTaskInternalStart + srcTaskDuration;

                var (tgtTaskInternalStart, tgtTaskDuration) =
                    taskInternalData[dep.Target.Id]; // Not used for calculating source event time
                var tgtTaskInternalFinish = tgtTaskInternalStart + tgtTaskDuration;


                double sourceEventAbsoluteTime; // Absolute time of the source event (start/finish of the source task)
                double targetTaskRelevantInternalTime; // Target task's internal time that aligns with the dependency

                switch (dep.Type)
                {
                    case DependencyType.FinishToStart:
                        sourceEventAbsoluteTime = sccFinalStartTimes[currentSccIdx] + srcTaskInternalFinish;
                        targetTaskRelevantInternalTime = tgtTaskInternalStart;
                        break;
                    case DependencyType.StartToStart:
                        sourceEventAbsoluteTime = sccFinalStartTimes[currentSccIdx] + srcTaskInternalStart;
                        targetTaskRelevantInternalTime = tgtTaskInternalStart;
                        break;
                    case DependencyType.StartToFinish:
                        sourceEventAbsoluteTime = sccFinalStartTimes[currentSccIdx] + srcTaskInternalStart;
                        targetTaskRelevantInternalTime = tgtTaskInternalFinish;
                        break;
                    case DependencyType.FinishToFinish:
                        sourceEventAbsoluteTime = sccFinalStartTimes[currentSccIdx] + srcTaskInternalFinish;
                        targetTaskRelevantInternalTime = tgtTaskInternalFinish;
                        break;
                    default:
                        throw new ScheduleModelException(graphParamName,
                            $"Unsupported dependency type: {dep.Type}");
                }

                // Required start for target SCC: target_scc_start + target_task_internal_time >= source_event_absolute_time
                // target_scc_start >= source_event_absolute_time - target_task_internal_time
                var requiredTargetSccAbsoluteStartTime = sourceEventAbsoluteTime - targetTaskRelevantInternalTime;
                sccFinalStartTimes[targetSccIdx] =
                    Max(sccFinalStartTimes[targetSccIdx], requiredTargetSccAbsoluteStartTime);

                if (--inDegree[targetSccIdx] == 0) readyQueue.Enqueue(targetSccIdx);
            }
        }

        if (processedCount < sccCount)
            throw new ScheduleModelException(graphParamName, "Condensation graph contains a cycle, scheduling failed.");

        return sccFinalStartTimes;
    }

    /// <summary>
    ///     Applies the final calculated absolute start times and durations to all tasks in the graph.
    /// </summary>
    /// <param name="componentsInfo">Information about each SCC.</param>
    /// <param name="taskInternalData">Internal timing data for each task.</param>
    /// <param name="sccFinalAbsoluteStartTimes">Final absolute start times for each SCC's internal t=0.</param>
    /// <param name="tasksInConstrainedGroups">Set of task IDs already handled by ConstrainedGroup LP solving.</param>
    private static void ApplyFinalTimesToGraphTasks(
        ComponentInfo[] componentsInfo,
        Dictionary<Guid, TaskTimingData> taskInternalData,
        double[] sccFinalAbsoluteStartTimes,
        IReadOnlySet<Guid>? tasksInConstrainedGroups = null)
    {
        tasksInConstrainedGroups ??= new HashSet<Guid>();

        foreach (var (stronglyConnectedComponent, _, sccIdx) in componentsInfo)
        {
            var finalSccAnchorTime = sccFinalAbsoluteStartTimes[sccIdx];

            foreach (var task in stronglyConnectedComponent.SkillExecutions)
            {
                // Skip tasks already handled by ConstrainedGroup LP - their times are already set
                if (tasksInConstrainedGroups.Contains(task.Id))
                    continue;

                var (internalStart, duration) = taskInternalData[task.Id];

                // For running tasks, preserve ActualStartTime (it's fixed and cannot be changed)
                if (task is ISkillExecution { IsRunning: true, ActualStartTime: not null } se)
                    task.PlannedStartTime = se.ActualStartTime.Value;
                else
                    task.PlannedStartTime = finalSccAnchorTime + internalStart;

                task.PlannedDuration = duration; // Duration was set during SCC initialization
                task.PlannedFinishTime = task.PlannedStartTime + task.PlannedDuration;
            }
        }
    }

    /// <summary>
    ///     Extends adaptive task durations to satisfy inter-SCC FF and SF constraints
    ///     for running tasks whose start time is locked.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For running adaptive tasks, the start time is fixed (ActualStartTime), so the only
    ///         way to satisfy constraints that affect the finish time is to extend the duration.
    ///     </para>
    ///     <para>
    ///         Constraints handled:
    ///         <list type="bullet">
    ///             <item><description>FF (Fj >= Fi): Target finish must be >= source finish</description></item>
    ///             <item><description>SF (Fj >= Si): Target finish must be >= source start</description></item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="graph">The execution graph containing tasks and dependencies.</param>
    /// <param name="taskToSccIndex">Mapping from task ID to SCC index.</param>
    /// <param name="taskInternalData">Mutable dictionary of task timing data to update.</param>
    /// <param name="sccFinalStartTimes">Final start times for each SCC.</param>
    /// <param name="tasksInConstrainedGroups">Set of task IDs already handled by ConstrainedGroup LP solving.</param>
    private static void ExtendAdaptiveDurationsForInterSccConstraints(
        IExecutionGraph graph,
        Dictionary<Guid, int> taskToSccIndex,
        Dictionary<Guid, TaskTimingData> taskInternalData,
        double[] sccFinalStartTimes,
        IReadOnlySet<Guid>? tasksInConstrainedGroups = null)
    {
        tasksInConstrainedGroups ??= new HashSet<Guid>();

        foreach (var dep in graph.Dependencies)
        {
            // Only handle FF and SF constraints (both affect target's finish time)
            if (dep.Type != DependencyType.FinishToFinish && dep.Type != DependencyType.StartToFinish)
                continue;

            // Skip if target is in a ConstrainedGroup (already handled by LP)
            if (tasksInConstrainedGroups.Contains(dep.Target.Id))
                continue;

            var srcSccIdx = taskToSccIndex[dep.Source.Id];
            var tgtSccIdx = taskToSccIndex[dep.Target.Id];

            // Only inter-SCC dependencies
            if (srcSccIdx == tgtSccIdx)
                continue;

            // Target must be adaptive running task
            if (dep.Target is not IAdaptivePlannedSkillExecution adaptiveTarget)
                continue;

            var se = adaptiveTarget as ISkillExecution;
            if (se?.IsRunning != true || !se.ActualStartTime.HasValue)
                continue;

            // Calculate required target finish time based on constraint type
            var srcData = taskInternalData[dep.Source.Id];
            double requiredTargetFinish;

            if (dep.Type == DependencyType.FinishToFinish)
            {
                // FF: Fj >= Fi → target finish must be >= source finish
                var srcFinish = sccFinalStartTimes[srcSccIdx] + srcData.InternalStart + srcData.Duration;
                requiredTargetFinish = srcFinish;
            }
            else // StartToFinish
            {
                // SF: Fj >= Si → target finish must be >= source start
                var srcStart = sccFinalStartTimes[srcSccIdx] + srcData.InternalStart;
                requiredTargetFinish = srcStart;
            }

            // Calculate required duration for target
            var targetStart = se.ActualStartTime.Value;
            var requiredDuration = requiredTargetFinish - targetStart;

            // Extend if needed. Adaptive durations are unbounded above, so there is no ceiling to respect.
            var tgtData = taskInternalData[adaptiveTarget.Id];
            if (requiredDuration > tgtData.Duration)
                taskInternalData[adaptiveTarget.Id] = tgtData with { Duration = requiredDuration };
        }
    }

    /// <summary>
    ///     Applies a global time shift to the entire schedule so the earliest task starts at t=0.
    /// </summary>
    /// <param name="graph">The execution graph with populated planned times.</param>
    private static void ApplyGlobalTimeShiftToSchedule(IExecutionGraph graph)
    {
        if (!graph.SkillExecutions.Any()) return;

        var globalShiftValue = graph.SkillExecutions.Min(t => t.PlannedStartTime);

        // Avoid tiny shifts due to floating point inaccuracies if effectively zero.
        if (Abs(globalShiftValue) < 1e-9) globalShiftValue = 0;

        if (globalShiftValue == 0) return;

        foreach (var task in graph.SkillExecutions)
        {
            task.PlannedStartTime -= globalShiftValue;
            task.PlannedFinishTime -= globalShiftValue;
        }
    }

    /// <summary>
    ///     Validates the structural and temporal consistency of the execution graph model
    ///     before scheduling. Enforces unique task identifiers, complete dependency
    ///     endpoints, self-loop prohibition, semantic-duplicate edge prohibition,
    ///     event-level acyclicity (the master invariant from the formal model), adaptive
    ///     duration bounds, and actual time/state sanity for running and finished tasks.
    /// </summary>
    /// <param name="graph">The graph to validate.</param>
    /// <param name="currentTime">The current time, used for validating running task states.</param>
    /// <exception cref="ScheduleModelException">If any validation rule is violated.</exception>
    private static void ValidateModel(IExecutionGraph graph, double currentTime)
    {
        var duplicateIds = graph.SkillExecutions
            .GroupBy(se => se.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        if (duplicateIds.Length > 0)
            throw new ScheduleModelException(nameof(graph),
                $"Duplicate task identifier(s): {string.Join(", ", duplicateIds)}");

        var taskSet = new HashSet<Guid>(graph.SkillExecutions.Select(s => s.Id));
        foreach (var dep in graph.Dependencies)
        {
            if (!taskSet.Contains(dep.Source.Id))
                throw new ScheduleModelException(nameof(graph),
                    $"Dependency source task {dep.Source.Id} not found in SkillExecutions.");
            if (!taskSet.Contains(dep.Target.Id))
                throw new ScheduleModelException(nameof(graph),
                    $"Dependency target task {dep.Target.Id} not found in SkillExecutions.");
        }

        foreach (var dep in graph.Dependencies)
            if (dep.Source.Id == dep.Target.Id)
                throw new ScheduleModelException(nameof(graph),
                    $"Self-loop dependency on task {dep.Source.Id} is not allowed.");

        var duplicateEdge = graph.Dependencies
            .GroupBy(d => (d.Source.Id, d.Target.Id, d.Type))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateEdge is not null)
        {
            var (src, tgt, type) = duplicateEdge.Key;
            throw new ScheduleModelException(nameof(graph),
                $"Duplicate dependency edge: source={src}, target={tgt}, type={type}.");
        }

        var cycleSkillId = FindEventLevelCycle(graph);
        if (cycleSkillId.HasValue)
            throw new ScheduleModelException(nameof(graph),
                $"Event-level dependency cycle detected involving task {cycleSkillId.Value}.");

        foreach (var task in graph.SkillExecutions)
        {
            if (task is IAdaptivePlannedSkillExecution { MinDuration: < 0 } adaptiveTask)
                throw new ScheduleModelException(nameof(graph),
                    $"Adaptive task {adaptiveTask.Id} has negative MinDuration ({adaptiveTask.MinDuration}).");

            if (task is not ISkillExecution skillExecution) continue;

            if (skillExecution is { ActualStartTime: not null, ActualFinishTime: not null } &&
                skillExecution.ActualStartTime.Value > skillExecution.ActualFinishTime.Value)
                throw new ScheduleModelException(nameof(graph),
                    $"Task {skillExecution.Id} has ActualStartTime ({skillExecution.ActualStartTime}) > ActualFinishTime ({skillExecution.ActualFinishTime}).");

            if (skillExecution.IsRunning &&
                (!skillExecution.ActualStartTime.HasValue || skillExecution.ActualStartTime.Value > currentTime))
                throw new ScheduleModelException(nameof(graph),
                    $"Running task {skillExecution.Id} has invalid ActualStartTime ({skillExecution.ActualStartTime?.ToString(CultureInfo.InvariantCulture) ?? "null"}) relative to currentTime ({currentTime}).");

            if (skillExecution.IsFinished &&
                (!skillExecution.ActualStartTime.HasValue || !skillExecution.ActualFinishTime.HasValue ||
                 !skillExecution.ActualDuration.HasValue))
                throw new ScheduleModelException(nameof(graph),
                    $"Finished task {skillExecution.Id} has incomplete actual time/duration data.");
        }
    }

    /// <summary>
    ///     Identifies a back edge in the event-level waits-for graph derived from the
    ///     execution graph. The lifted graph has one vertex per (task, event-kind) pair
    ///     with edges for each dependency type plus implicit Start → Finish duration
    ///     linkage. A back edge during DFS proves a cycle, which corresponds to a
    ///     structural deadlock in the event semantics.
    /// </summary>
    /// <param name="graph">The execution graph to inspect.</param>
    /// <returns>
    ///     The identifier of a task whose event vertex closes a cycle, or
    ///     <see langword="null" /> if the lifted graph is acyclic.
    /// </returns>
    private static Guid? FindEventLevelCycle(IExecutionGraph graph)
    {
        var adjacency = new Dictionary<(Guid, EventKind), List<(Guid, EventKind)>>();

        foreach (var task in graph.SkillExecutions)
            AddEventEdge(adjacency, (task.Id, EventKind.Start), (task.Id, EventKind.Finish));

        foreach (var dep in graph.Dependencies)
        {
            var srcId = dep.Source.Id;
            var tgtId = dep.Target.Id;
            switch (dep.Type)
            {
                case DependencyType.FinishToStart:
                    AddEventEdge(adjacency, (srcId, EventKind.Finish), (tgtId, EventKind.Start));
                    break;
                case DependencyType.StartToStart:
                    AddEventEdge(adjacency, (srcId, EventKind.Start), (tgtId, EventKind.Start));
                    break;
                case DependencyType.FinishToFinish:
                    AddEventEdge(adjacency, (srcId, EventKind.Finish), (tgtId, EventKind.Finish));
                    break;
                case DependencyType.StartToFinish:
                    AddEventEdge(adjacency, (srcId, EventKind.Start), (tgtId, EventKind.Finish));
                    break;
            }
        }

        var color = new Dictionary<(Guid, EventKind), int>();
        foreach (var task in graph.SkillExecutions)
        {
            color[(task.Id, EventKind.Start)] = 0;
            color[(task.Id, EventKind.Finish)] = 0;
        }

        foreach (var vertex in color.Keys.ToArray())
        {
            if (color[vertex] != 0) continue;
            var backEdgeTarget = DfsForEventLevelCycle(vertex, adjacency, color);
            if (backEdgeTarget.HasValue) return backEdgeTarget.Value.SkillId;
        }

        return null;
    }

    /// <summary>
    ///     Appends an edge to the lifted-event-graph adjacency map, creating the neighbor
    ///     list on first use.
    /// </summary>
    /// <param name="adjacency">The adjacency map being populated.</param>
    /// <param name="from">The edge tail vertex.</param>
    /// <param name="to">The edge head vertex.</param>
    private static void AddEventEdge(
        Dictionary<(Guid, EventKind), List<(Guid, EventKind)>> adjacency,
        (Guid, EventKind) from,
        (Guid, EventKind) to)
    {
        if (!adjacency.TryGetValue(from, out var neighbors))
        {
            neighbors = [];
            adjacency[from] = neighbors;
        }

        neighbors.Add(to);
    }

    /// <summary>
    ///     Recursive depth-first traversal with white/gray/black coloring that locates a
    ///     back edge in the lifted event graph.
    /// </summary>
    /// <param name="node">The vertex currently being visited.</param>
    /// <param name="adjacency">The lifted-event-graph adjacency map.</param>
    /// <param name="color">Visit state per vertex: 0 = unvisited, 1 = on stack, 2 = finished.</param>
    /// <returns>
    ///     The neighbor whose state proves a back edge (cycle), or <see langword="null" />
    ///     if no cycle is reachable from <paramref name="node" />.
    /// </returns>
    private static (Guid SkillId, EventKind Kind)? DfsForEventLevelCycle(
        (Guid, EventKind) node,
        Dictionary<(Guid, EventKind), List<(Guid, EventKind)>> adjacency,
        Dictionary<(Guid, EventKind), int> color)
    {
        color[node] = 1;

        if (adjacency.TryGetValue(node, out var neighbors))
            foreach (var neighbor in neighbors)
            {
                if (color[neighbor] == 1) return neighbor;
                if (color[neighbor] != 0) continue;
                var result = DfsForEventLevelCycle(neighbor, adjacency, color);
                if (result.HasValue) return result;
            }

        color[node] = 2;
        return null;
    }

    /// <summary>
    ///     Distinguishes the two event kinds carried by every scheduled task in the lifted
    ///     event graph used for acyclicity validation.
    /// </summary>
    private enum EventKind
    {
        /// <summary>The task's start event.</summary>
        Start,

        /// <summary>The task's finish event.</summary>
        Finish
    }

    /// <summary>
    ///     Context class for Tarjan's algorithm variables.
    /// </summary>
    private sealed class TarjanContext
    {
        /// <summary>All dependencies in the graph.</summary>
        public readonly IReadOnlyList<Dependency> AllDependencies;

        /// <summary>List of found strongly connected components.</summary>
        public readonly List<IStronglyConnectedComponent> Components;

        /// <summary>DFS indices of nodes.</summary>
        public readonly Dictionary<IPlannedSkillExecution, int> Indices;

        /// <summary>Low-link values of nodes.</summary>
        public readonly Dictionary<IPlannedSkillExecution, int> LowLinks;

        /// <summary>Set of nodes currently on the stack.</summary>
        public readonly HashSet<IPlannedSkillExecution> OnStack;

        /// <summary>Outgoing dependencies for each task.</summary>
        public readonly IReadOnlyDictionary<IPlannedSkillExecution, IReadOnlyList<IPlannedSkillExecution>>
            OutgoingEdges;

        /// <summary>Stack for keeping track of visited nodes.</summary>
        public readonly Stack<IPlannedSkillExecution> Stack;

        /// <summary>Current index for assigning to nodes.</summary>
        public int Index;

        /// <summary>
        ///     Initialises a new instance of the <see cref="TarjanContext" /> class.
        /// </summary>
        /// <param name="capacity">Initial capacity for dictionaries, based on task count.</param>
        /// <param name="outgoingEdges">Lookup for outgoing edges for each task.</param>
        /// <param name="allDependencies">All dependencies in the graph.</param>
        public TarjanContext(int capacity,
            IReadOnlyDictionary<IPlannedSkillExecution, IReadOnlyList<IPlannedSkillExecution>> outgoingEdges,
            IReadOnlyList<Dependency> allDependencies)
        {
            Indices = new Dictionary<IPlannedSkillExecution, int>(capacity);
            LowLinks = new Dictionary<IPlannedSkillExecution, int>(capacity);
            Stack = new Stack<IPlannedSkillExecution>(capacity);
            OnStack = new HashSet<IPlannedSkillExecution>(capacity);
            Components = [];
            OutgoingEdges = outgoingEdges;
            AllDependencies = allDependencies;
        }
    }

    /// <summary>
    ///     Represents timing data for a task, internal to its SCC.
    /// </summary>
    /// <param name="InternalStart">The task's start time relative to its SCC's anchor time (internal t=0).</param>
    /// <param name="Duration">The task's duration.</param>
    private record struct TaskTimingData(double InternalStart, double Duration);

    /// <summary>
    ///     Represents information about an SCC, its classification, and original index.
    /// </summary>
    /// <param name="Component">The strongly connected component.</param>
    /// <param name="Classification">The classification of the SCC (e.g. Trivial, AdaptiveCycle).</param>
    /// <param name="OriginalIndex">The original index of the SCC in the list of components.</param>
    private sealed record ComponentInfo(
        IStronglyConnectedComponent Component,
        StronglyConnectedComponentInfo Classification,
        int OriginalIndex);
}