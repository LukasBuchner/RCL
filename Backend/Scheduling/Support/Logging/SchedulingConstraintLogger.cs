using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Scheduling.Support.Logging;

/// <summary>
///     Provides structured logging for OR-Tools constraint setup during scheduling.
///     Uses high-performance source-generated logging for zero-allocation logging.
/// </summary>
public static partial class SchedulingConstraintLogger
{
    /// <summary>
    ///     Logs information about a skill execution being added to the OR-Tools model.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "CONSTRAINT_SKILL | SkillId={SkillId} | Name={SkillName} | IsAdaptive={IsAdaptive} | " +
                  "State={State} | PlannedDuration={PlannedDuration:F3}s | MinDuration={MinDuration:F3}s | " +
                  "ActualStart={ActualStart:F3}s | ActualFinish={ActualFinish:F3}s | " +
                  "CurrentTime={CurrentTime:F3}s")]
    public static partial void LogSkillConstraint(
        this ILogger logger,
        Guid skillId,
        string skillName,
        bool isAdaptive,
        string state,
        double plannedDuration,
        double? minDuration,
        double? actualStart,
        double? actualFinish,
        double currentTime);

    /// <summary>
    ///     Logs information about a dependency constraint being added to the OR-Tools model.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CONSTRAINT_DEPENDENCY | Type={DependencyType} | Source={SourceId} ({SourceName}) -> Target={TargetId} ({TargetName}) | " +
            "SourceIsAdaptive={SourceIsAdaptive} | TargetIsAdaptive={TargetIsAdaptive} | " +
            "Constraint={ConstraintDescription}")]
    public static partial void LogDependencyConstraint(
        this ILogger logger,
        string dependencyType,
        Guid sourceId,
        string sourceName,
        Guid targetId,
        string targetName,
        bool sourceIsAdaptive,
        bool targetIsAdaptive,
        string constraintDescription);

    /// <summary>
    ///     Logs the complete dependency graph structure before scheduling.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "CONSTRAINT_GRAPH | TotalSkills={TotalSkills} | TotalDependencies={TotalDependencies} | " +
                  "AdaptiveSkills={AdaptiveSkills} | FinishedSkills={FinishedSkills} | RunningSkills={RunningSkills} | " +
                  "PendingSkills={PendingSkills} | CurrentTime={CurrentTime:F3}s")]
    public static partial void LogGraphStructure(
        this ILogger logger,
        int totalSkills,
        int totalDependencies,
        int adaptiveSkills,
        int finishedSkills,
        int runningSkills,
        int pendingSkills,
        double currentTime);

    /// <summary>
    ///     Logs OR-Tools solver status and details.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "CONSTRAINT_SOLVER | Status={SolverStatus} | Variables={VariableCount} | Constraints={ConstraintCount} | " +
            "Makespan={Makespan:F3}s | Result={Result}")]
    public static partial void LogSolverResult(
        this ILogger logger,
        string solverStatus,
        int variableCount,
        int constraintCount,
        double? makespan,
        string result);

    /// <summary>
    ///     Logs a specific constraint equation for debugging infeasibility issues.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "CONSTRAINT_EQUATION | Type={ConstraintType} | Equation={Equation} | Description={Description}")]
    public static partial void LogConstraintEquation(
        this ILogger logger,
        string constraintType,
        string equation,
        string description);

    /// <summary>
    ///     Logs potential circular dependency detection.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "CONSTRAINT_CYCLE_DETECTED | Path={CyclePath} | DependencyTypes={DependencyTypes} | " +
                  "Reason={Reason}")]
    public static partial void LogCycleDetected(
        this ILogger logger,
        string cyclePath,
        string dependencyTypes,
        string reason);

    /// <summary>
    ///     Logs when a dependency constraint is violated by already-executed skills.
    ///     This occurs during re-scheduling when skills that are already running or finished
    ///     started in an order that violates a dependency, but cannot be changed (we cannot change the past).
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "CONSTRAINT_DEPENDENCY_VIOLATION | Type={DependencyType} | " +
                  "Source={SourceId} ({SourceName}) started at {SourceStart:F3}s -> " +
                  "Target={TargetId} ({TargetName}) started at {TargetStart:F3}s | " +
                  "Violation={ViolationDescription} | Action=Skipping constraint (cannot change the past)")]
    public static partial void LogDependencyViolation(
        this ILogger logger,
        string dependencyType,
        Guid sourceId,
        string sourceName,
        double? sourceStart,
        Guid targetId,
        string targetName,
        double? targetStart,
        string violationDescription);

    #region Task Variable Creation

    /// <summary>
    ///     Logs the creation of OR-Tools start and finish decision variables for a task.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used for naming solver variables.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Created OR-Tools variables for skill {SkillId}: S_{IdSuffix}, F_{IdSuffix}")]
    public static partial void LogTaskVariablesCreated(
        this ILogger logger,
        Guid skillId,
        string idSuffix);

    #endregion

    #region Constrained Group

    /// <summary>
    ///     Logs that a constrained group of coupled tasks is being solved using LP.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskCount">The number of tasks in the constrained group.</param>
    /// <param name="dependencyCount">The number of internal dependencies in the group.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Solving ConstrainedGroup with {TaskCount} tasks and {DependencyCount} dependencies using LP")]
    public static partial void LogConstrainedGroupSolving(
        this ILogger logger,
        int taskCount,
        int dependencyCount);

    #endregion

    #region LP Solver Lifecycle

    /// <summary>
    ///     Logs that the OR-Tools LP solver has been initialized with the given skill and dependency counts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of skill executions in the model.</param>
    /// <param name="dependencyCount">The number of dependencies in the model.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "OR-Tools LP Solver (GLOP) initialized for {SkillCount} skills with {DependencyCount} dependencies")]
    public static partial void LogSolverInitialized(
        this ILogger logger,
        int skillCount,
        int dependencyCount);

    /// <summary>
    ///     Logs that dependency constraints are being set up in the solver.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dependencyCount">The number of dependency constraints being configured.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Setting up {DependencyCount} dependency constraints")]
    public static partial void LogDependencyConstraintSetup(
        this ILogger logger,
        int dependencyCount);

    /// <summary>
    ///     Logs that the makespan objective has been configured and the solver is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableCount">The total number of decision variables in the model.</param>
    /// <param name="constraintCount">The total number of constraints in the model.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "OR-Tools makespan objective set up. Starting solver with {VariableCount} variables and {ConstraintCount} constraints")]
    public static partial void LogMakespanObjectiveSetUp(
        this ILogger logger,
        int variableCount,
        int constraintCount);

    /// <summary>
    ///     Logs the solver completion status.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solverStatus">The string representation of the solver result status.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "OR-Tools solver completed with status: {SolverStatus}")]
    public static partial void LogSolverStatusCompleted(
        this ILogger logger,
        string solverStatus);

    /// <summary>
    ///     Logs that solver solutions are being applied to tasks.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillCount">The number of skill executions receiving solutions.</param>
    /// <param name="makespan">The solved makespan value.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Applying OR-Tools solutions to {SkillCount} skills. Makespan: {Makespan}")]
    public static partial void LogApplyingSolutions(
        this ILogger logger,
        int skillCount,
        double makespan);

    #endregion

    #region Finished Task Constraints

    /// <summary>
    ///     Logs timing details for a finished skill execution whose times are fixed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="startTime">The actual start time of the finished skill.</param>
    /// <param name="finishTime">The actual finish time of the finished skill.</param>
    /// <param name="duration">The actual duration of the finished skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Skill {SkillId} is FINISHED: StartTime={StartTime}, FinishTime={FinishTime}, Duration={Duration}")]
    public static partial void LogFinishedSkillTiming(
        this ILogger logger,
        Guid skillId,
        double startTime,
        double finishTime,
        double duration);

    /// <summary>
    ///     Logs duration details for a finished adaptive skill, whose actual duration is now fixed
    ///     and replaces the original min/max range.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used for naming the duration variable.</param>
    /// <param name="actualDuration">The actual duration achieved by the skill.</param>
    /// <param name="minDuration">The original minimum duration bound.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Finished adaptive skill {SkillId}: Fixed duration variable D_{IdSuffix}={ActualDuration} (was min={MinDuration})")]
    public static partial void LogFinishedAdaptiveSkillDuration(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double actualDuration,
        double minDuration);

    /// <summary>
    ///     Logs the fixed duration constraint for a finished fixed-duration skill.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used in the constraint equation naming.</param>
    /// <param name="actualDuration">The actual duration of the finished skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Finished fixed skill {SkillId}: Duration constraint F_{IdSuffix} - S_{IdSuffix} = {ActualDuration}")]
    public static partial void LogFinishedFixedSkillDuration(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double actualDuration);

    #endregion

    #region Running Task Constraints

    /// <summary>
    ///     Logs timing details for a currently running skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="startTime">The actual start time of the running skill.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="elapsed">The elapsed time since the skill started.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Skill {SkillId} is RUNNING: StartTime={StartTime}, CurrentTime={CurrentTime}, Elapsed={Elapsed}")]
    public static partial void LogRunningSkillTiming(
        this ILogger logger,
        Guid skillId,
        double startTime,
        double currentTime,
        double elapsed);

    /// <summary>
    ///     Logs duration variable bounds for a running adaptive skill execution,
    ///     showing the effective range after accounting for elapsed time.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used for naming the duration variable.</param>
    /// <param name="minDuration">The original minimum duration bound.</param>
    /// <param name="minElapsed">The minimum elapsed time since the skill started.</param>
    /// <param name="effectiveMin">The effective minimum duration (max of minDuration and minElapsed).</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Running adaptive skill {SkillId}: Duration variable D_{IdSuffix} ∈ [max({MinDuration}, {MinElapsed}), ∞) = [{EffectiveMin}, ∞)")]
    public static partial void LogRunningAdaptiveSkillDuration(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double minDuration,
        double minElapsed,
        double effectiveMin);

    /// <summary>
    ///     Logs the fixed duration constraint for a running fixed-duration skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used in the constraint equation naming.</param>
    /// <param name="effectiveDuration">The effective duration used for the constraint.</param>
    /// <param name="estimatedDuration">The estimated duration, if available.</param>
    /// <param name="plannedDuration">The originally planned duration.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Running fixed skill {SkillId}: Duration constraint F_{IdSuffix} - S_{IdSuffix} = {EffectiveDuration} (estimated={EstimatedDuration}, planned={PlannedDuration})")]
    public static partial void LogRunningFixedSkillDuration(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double effectiveDuration,
        double? estimatedDuration,
        double plannedDuration);

    #endregion

    #region Not-Started Task Constraints

    /// <summary>
    ///     Logs constraint setup for a not-started adaptive skill execution,
    ///     including the start time lower bound and duration variable range.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used for naming solver variables.</param>
    /// <param name="currentTime">The current system time (lower bound for start).</param>
    /// <param name="minDuration">The minimum duration bound.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NOT STARTED adaptive skill {SkillId}: StartTime S_{IdSuffix} >= {CurrentTime}, Duration variable D_{IdSuffix} ∈ [{MinDuration}, ∞)")]
    public static partial void LogNotStartedAdaptiveSkill(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double currentTime,
        double minDuration);

    /// <summary>
    ///     Logs constraint setup for a not-started fixed-duration skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="idSuffix">The ID suffix used in the constraint equation naming.</param>
    /// <param name="currentTime">The current system time (lower bound for start).</param>
    /// <param name="effectiveDuration">The effective fixed duration used for the constraint.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NOT STARTED fixed skill {SkillId}: StartTime S_{IdSuffix} >= {CurrentTime}, Duration constraint F_{IdSuffix} - S_{IdSuffix} = {EffectiveDuration}")]
    public static partial void LogNotStartedFixedSkill(
        this ILogger logger,
        Guid skillId,
        string idSuffix,
        double currentTime,
        double effectiveDuration);

    #endregion

    #region Plan Classification

    /// <summary>
    ///     Logs classification details for a finished skill during plan scheduling,
    ///     recording the actual timing values used for constraint setup.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="state">The state classification label (e.g. "FINISHED").</param>
    /// <param name="isAdaptive">Whether the skill is adaptive (has min/max duration range).</param>
    /// <param name="actualStart">The actual start time of the finished skill.</param>
    /// <param name="actualFinish">The actual finish time of the finished skill.</param>
    /// <param name="actualDuration">The actual duration of the finished skill.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="initialStart">The initial start time assigned during classification.</param>
    /// <param name="effectiveDuration">The effective duration used for scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SKILL_TIMING | Phase=PLAN_CLASSIFY | SkillId={SkillId} | State={State} | IsAdaptive={IsAdaptive} | " +
            "ActualStart={ActualStart:F3}s | ActualFinish={ActualFinish:F3}s | ActualDuration={ActualDuration:F3}s | " +
            "CurrentTime={CurrentTime:F3}s | InitialStart={InitialStart:F3}s | EffectiveDuration={EffectiveDuration:F3}s")]
    public static partial void LogPlanClassifyFinished(
        this ILogger logger,
        Guid skillId,
        string state,
        bool isAdaptive,
        double actualStart,
        double actualFinish,
        double actualDuration,
        double currentTime,
        double initialStart,
        double effectiveDuration);

    /// <summary>
    ///     Logs classification details for a running adaptive skill during plan scheduling,
    ///     including elapsed time and the adaptive duration range.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="state">The state classification label (e.g. "RUNNING_ADAPTIVE").</param>
    /// <param name="actualStart">The actual start time of the running skill.</param>
    /// <param name="estimatedDuration">The estimated duration, if available.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="elapsed">The elapsed time since the skill started.</param>
    /// <param name="minDuration">The minimum duration bound.</param>
    /// <param name="initialStart">The initial start time assigned during classification.</param>
    /// <param name="effectiveDuration">The effective duration used for scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SKILL_TIMING | Phase=PLAN_CLASSIFY | SkillId={SkillId} | State={State} | IsAdaptive=true | " +
            "ActualStart={ActualStart:F3}s | EstimatedDuration={EstimatedDuration:F3}s | " +
            "CurrentTime={CurrentTime:F3}s | Elapsed={Elapsed:F3}s | MinDuration={MinDuration:F3}s | " +
            "InitialStart={InitialStart:F3}s | EffectiveDuration={EffectiveDuration:F3}s")]
    public static partial void LogPlanClassifyRunningAdaptive(
        this ILogger logger,
        Guid skillId,
        string state,
        double actualStart,
        double? estimatedDuration,
        double currentTime,
        double elapsed,
        double minDuration,
        double initialStart,
        double effectiveDuration);

    /// <summary>
    ///     Logs classification details for a running fixed-duration skill during plan scheduling.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="state">The state classification label (e.g. "RUNNING_FIXED").</param>
    /// <param name="actualStart">The actual start time of the running skill.</param>
    /// <param name="estimatedDuration">The estimated duration, if available.</param>
    /// <param name="plannedDuration">The originally planned duration.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="initialStart">The initial start time assigned during classification.</param>
    /// <param name="effectiveDuration">The effective duration used for scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SKILL_TIMING | Phase=PLAN_CLASSIFY | SkillId={SkillId} | State={State} | IsAdaptive=false | " +
            "ActualStart={ActualStart:F3}s | EstimatedDuration={EstimatedDuration:F3}s | PlannedDuration={PlannedDuration:F3}s | " +
            "CurrentTime={CurrentTime:F3}s | InitialStart={InitialStart:F3}s | EffectiveDuration={EffectiveDuration:F3}s")]
    public static partial void LogPlanClassifyRunningFixed(
        this ILogger logger,
        Guid skillId,
        string state,
        double actualStart,
        double? estimatedDuration,
        double plannedDuration,
        double currentTime,
        double initialStart,
        double effectiveDuration);

    /// <summary>
    ///     Logs classification details for a not-started adaptive skill during plan scheduling.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="state">The state classification label (e.g. "NOT_STARTED_ADAPTIVE").</param>
    /// <param name="minDuration">The minimum duration bound.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="initialStart">The initial start time assigned during classification.</param>
    /// <param name="effectiveDuration">The effective duration used for scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SKILL_TIMING | Phase=PLAN_CLASSIFY | SkillId={SkillId} | State={State} | IsAdaptive=true | " +
            "MinDuration={MinDuration:F3}s | " +
            "CurrentTime={CurrentTime:F3}s | InitialStart={InitialStart:F3}s | EffectiveDuration={EffectiveDuration:F3}s")]
    public static partial void LogPlanClassifyNotStartedAdaptive(
        this ILogger logger,
        Guid skillId,
        string state,
        double minDuration,
        double currentTime,
        double initialStart,
        double effectiveDuration);

    /// <summary>
    ///     Logs classification details for a not-started fixed-duration skill during plan scheduling.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillId">The unique identifier of the skill execution.</param>
    /// <param name="state">The state classification label (e.g. "NOT_STARTED_FIXED").</param>
    /// <param name="estimatedDuration">The estimated duration, if available.</param>
    /// <param name="plannedDuration">The originally planned duration.</param>
    /// <param name="currentTime">The current system time.</param>
    /// <param name="initialStart">The initial start time assigned during classification.</param>
    /// <param name="effectiveDuration">The effective duration used for scheduling.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "SKILL_TIMING | Phase=PLAN_CLASSIFY | SkillId={SkillId} | State={State} | IsAdaptive=false | " +
            "EstimatedDuration={EstimatedDuration:F3}s | PlannedDuration={PlannedDuration:F3}s | " +
            "CurrentTime={CurrentTime:F3}s | InitialStart={InitialStart:F3}s | EffectiveDuration={EffectiveDuration:F3}s")]
    public static partial void LogPlanClassifyNotStartedFixed(
        this ILogger logger,
        Guid skillId,
        string state,
        double? estimatedDuration,
        double plannedDuration,
        double currentTime,
        double initialStart,
        double effectiveDuration);

    #endregion
}