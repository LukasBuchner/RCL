using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides high-performance source-generated logging for execution initialization
///     and variable context building operations.
/// </summary>
public static partial class ExecutionInitLogger
{
    // ──────────────────────────────────────────────────
    //  ExecutionInitializer
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of execution initialization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting execution initialization")]
    public static partial void LogInitializationStarted(this ILogger logger);

    /// <summary>
    ///     Logs the start of execution initialization for a specific procedure and execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    /// <param name="executionId">The unique identifier of the execution run.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting execution initialization for procedure {ProcedureId}, execution {ExecutionId}")]
    public static partial void LogInitializationStartedForProcedure(
        this ILogger logger,
        Guid procedureId,
        Guid executionId);

    /// <summary>
    ///     Logs the number of nodes and edges loaded for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes loaded.</param>
    /// <param name="edgeCount">The number of edges loaded.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded {NodeCount} nodes and {EdgeCount} edges for procedure {ProcedureId}")]
    public static partial void LogNodesAndEdgesLoaded(
        this ILogger logger,
        int nodeCount,
        int edgeCount,
        Guid procedureId);

    /// <summary>
    ///     Logs the number of nodes and edges loaded without a procedure context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The number of nodes loaded.</param>
    /// <param name="edgeCount">The number of edges loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loaded {NodeCount} nodes and {EdgeCount} edges")]
    public static partial void LogNodesAndEdgesLoadedSimple(
        this ILogger logger,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Logs that a procedure was not found during initialization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The unique identifier of the missing procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Procedure {ProcedureId} not found")]
    public static partial void LogProcedureNotFound(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the successful initialization of a variable context with the number of variables.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableCount">The number of variables initialized in the context.</param>
    /// <param name="executionId">The unique identifier of the execution run.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initialized variable context with {VariableCount} variables for execution {ExecutionId}")]
    public static partial void LogVariableContextInitialized(
        this ILogger logger,
        int variableCount,
        Guid executionId);

    /// <summary>
    ///     Logs that the initial schedule calculation failed for a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to calculate initial schedule for procedure {ProcedureId}")]
    public static partial void LogScheduleCalculationFailed(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the number of agents assigned to skill execution nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="assignedCount">The number of agents assigned.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Assigned {AssignedCount} agents to skill execution nodes for procedure {ProcedureId}")]
    public static partial void LogAgentsAssigned(
        this ILogger logger,
        int assignedCount,
        Guid procedureId);

    /// <summary>
    ///     Logs a general failure during execution initialization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to initialize execution")]
    public static partial void LogInitializationFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs a failure during execution initialization for a specific procedure and execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="procedureId">The unique identifier of the procedure.</param>
    /// <param name="executionId">The unique identifier of the execution run.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to initialize execution for procedure {ProcedureId}, execution {ExecutionId}")]
    public static partial void LogInitializationFailedForProcedure(
        this ILogger logger,
        Exception exception,
        Guid procedureId,
        Guid executionId);

    /// <summary>
    ///     Logs a successful agent assignment to a skill execution node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the assigned agent.</param>
    /// <param name="agentName">The name of the assigned agent.</param>
    /// <param name="skillId">The unique identifier of the skill execution node.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Assigned agent {AgentId} ({AgentName}) to skill {SkillId}")]
    public static partial void LogAgentAssignedToSkill(
        this ILogger logger,
        Guid agentId,
        string agentName,
        Guid skillId);

    /// <summary>
    ///     Logs that no runtime agent was found for a given agent ID in a skill node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the missing agent.</param>
    /// <param name="skillId">The unique identifier of the skill execution node.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No runtime agent found for AgentId {AgentId} in skill {SkillId}")]
    public static partial void LogAgentNotFoundForSkill(
        this ILogger logger,
        Guid agentId,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  VariableContextBuilder
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of variable context building for a procedure execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureExecutionId">The unique identifier of the procedure execution.</param>
    /// <param name="variableCount">The number of variable definitions to process.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Building variable context for procedure execution {ProcedureExecutionId} with {VariableCount} variables")]
    public static partial void LogBuildingVariableContext(
        this ILogger logger,
        Guid procedureExecutionId,
        int variableCount);

    /// <summary>
    ///     Logs that a user-provided value is being used for a variable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable.</param>
    /// <param name="value">The user-provided value.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Using user-provided value for variable '{VariableName}': {Value}")]
    public static partial void LogUsingUserProvidedValue(
        this ILogger logger,
        string variableName,
        object? value);

    /// <summary>
    ///     Logs that a default value is being used for a variable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable.</param>
    /// <param name="value">The default value being used.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Using default value for variable '{VariableName}': {Value}")]
    public static partial void LogUsingDefaultValue(
        this ILogger logger,
        string variableName,
        object? value);

    /// <summary>
    ///     Logs the successful completion of variable context building.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="valueCount">The number of values set in the built context.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully built variable context with {ValueCount} values")]
    public static partial void LogVariableContextBuilt(
        this ILogger logger,
        int valueCount);
}