using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for KUKA iiwa 14 agent operations.
///     Covers <see cref="Agents.Kuka.KukaIiwa14RuntimeAgent" /> (skill execution, OPC UA browsing,
///     execution monitoring), <see cref="Agents.Kuka.Services.KukaAgentFactory" /> (configuration loading,
///     agent creation), and <see cref="Agents.Kuka.Services.KukaIiwa14DataReader" /> (joint/torque reading,
///     execution estimates) through a single shared static logger class.
/// </summary>
public static partial class KukaAgentLogger
{
    // ──────────────────────────────────────────────────
    //  KukaIiwa14RuntimeAgent — Skill estimation
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the agent is starting execution estimation for a skill.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the agent performing the estimation.</param>
    /// <param name="skillName">The name of the skill being estimated.</param>
    /// <param name="skillId">The unique identifier of the skill being estimated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent '{AgentName}' estimating execution for skill '{SkillName}' (ID: {SkillId})")]
    public static partial void LogEstimatingExecution(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs a warning when the agent does not have a requested skill.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the agent that lacks the skill.</param>
    /// <param name="skillName">The name of the missing skill.</param>
    /// <param name="skillId">The unique identifier of the missing skill.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Agent '{AgentName}' does not have skill '{SkillName}' (ID: {SkillId})")]
    public static partial void LogAgentMissingSkill(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs an error when the OPC UA session is not connected and estimation cannot proceed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot estimate execution: OPC UA session is not connected")]
    public static partial void LogCannotEstimateNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs the received execution estimate for a skill from the OPC UA server.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the agent that received the estimate.</param>
    /// <param name="skillName">The name of the skill that was estimated.</param>
    /// <param name="duration">The estimated execution duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Agent '{AgentName}' received execution estimate for skill '{SkillName}': {Duration:F2} seconds")]
    public static partial void LogExecutionEstimateReceived(
        this ILogger logger,
        string agentName,
        string skillName,
        double duration);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14RuntimeAgent — Skill execution
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when the OPC UA session is not connected and skill execution cannot proceed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot execute skill: OPC UA session is not connected")]
    public static partial void LogCannotExecuteNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when the robot node cannot be found in the OPC UA Objects folder.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Could not find robot node 'KUKA iiwa 14' in Objects")]
    public static partial void LogRobotNodeNotFoundInObjects(this ILogger logger);

    /// <summary>
    ///     Logs an error when the ExecutePathAsync method cannot be found on the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Could not find ExecutePathAsync method on robot node")]
    public static partial void LogExecutePathAsyncMethodNotFound(this ILogger logger);

    /// <summary>
    ///     Logs that a skill is being executed via OPC UA, including the target pose parameters.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the skill being executed.</param>
    /// <param name="skillId">The unique identifier of the skill being executed.</param>
    /// <param name="x">The target X position in millimeters.</param>
    /// <param name="y">The target Y position in millimeters.</param>
    /// <param name="z">The target Z position in millimeters.</param>
    /// <param name="alpha">The target alpha rotation in degrees.</param>
    /// <param name="beta">The target beta rotation in degrees.</param>
    /// <param name="gamma">The target gamma rotation in degrees.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Executing skill '{SkillName}' (ID: {SkillId}) via OPC UA - Pose: ({X}, {Y}, {Z}), Orientation: ({Alpha}, {Beta}, {Gamma})")]
    public static partial void LogExecutingSkillViaPose(
        this ILogger logger,
        string skillName,
        Guid skillId,
        double x,
        double y,
        double z,
        double alpha,
        double beta,
        double gamma);

    /// <summary>
    ///     Logs an error when the ExecutePathAsync method call returns no output.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "ExecutePathAsync method call returned no output")]
    public static partial void LogExecutePathAsyncNoOutput(this ILogger logger);

    /// <summary>
    ///     Logs a warning when ExecutePathAsync returns false, indicating execution is already in progress.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ExecutePathAsync returned false - execution already in progress")]
    public static partial void LogExecutePathAsyncAlreadyInProgress(this ILogger logger);

    /// <summary>
    ///     Logs an error with exception details when skill execution fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the skill execution failure.</param>
    /// <param name="skillName">The name of the skill that failed.</param>
    /// <param name="skillId">The unique identifier of the skill that failed.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error executing skill '{SkillName}' (ID: {SkillId})")]
    public static partial void LogSkillExecutionError(
        this ILogger logger,
        Exception ex,
        string skillName,
        Guid skillId);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14RuntimeAgent — OPC UA browsing
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the agent is browsing the Objects folder to locate the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the robot being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Browsing Objects folder to find robot node '{RobotName}'")]
    public static partial void LogBrowsingForRobotNode(
        this ILogger logger,
        string robotName);

    /// <summary>
    ///     Logs a warning when OPC UA browse returns no results for the Objects folder.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Browse returned no results for Objects folder")]
    public static partial void LogBrowseNoResultsForObjectsFolder(this ILogger logger);

    /// <summary>
    ///     Logs an error when OPC UA browse fails with a bad status code.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="status">The OPC UA <see cref="StatusCode"/> from the failed browse operation.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Browse failed with status: {Status}")]
    public static partial void LogBrowseFailedWithStatus(
        this ILogger logger,
        StatusCode status);

    /// <summary>
    ///     Logs that the robot node was found with its OPC UA NodeId.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the found robot node.</param>
    /// <param name="nodeId">The OPC UA NodeId of the found robot node.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found robot node '{RobotName}' with NodeId: {NodeId}")]
    public static partial void LogFoundRobotNode(
        this ILogger logger,
        string robotName,
        object? nodeId);

    /// <summary>
    ///     Logs a warning when the robot node cannot be found during browsing.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the robot that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Robot node '{RobotName}' not found")]
    public static partial void LogRobotNodeNotFound(
        this ILogger logger,
        string robotName);

    /// <summary>
    ///     Logs that browsing for the robot node was cancelled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Browse for robot node was cancelled")]
    public static partial void LogBrowseForRobotNodeCancelled(this ILogger logger);

    /// <summary>
    ///     Logs an OPC UA service error that occurred while browsing for the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The OPC UA service result exception.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OPC UA service error while browsing for robot node")]
    public static partial void LogOpcUaServiceErrorBrowsingRobotNode(
        this ILogger logger,
        Exception ex);

    /// <summary>
    ///     Logs an unexpected error that occurred while browsing for the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The unexpected exception.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unexpected error browsing for robot node")]
    public static partial void LogUnexpectedErrorBrowsingRobotNode(
        this ILogger logger,
        Exception ex);

    /// <summary>
    ///     Logs that the agent is browsing a node to find a specific method.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeId">The OPC UA NodeId of the parent node being browsed.</param>
    /// <param name="methodName">The name of the method being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Browsing node {NodeId} to find method {MethodName}")]
    public static partial void LogBrowsingForMethod(
        this ILogger logger,
        object? nodeId,
        string methodName);

    /// <summary>
    ///     Logs a warning when OPC UA browse returns no results for a specific node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeId">The OPC UA NodeId that returned no browse results.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Browse returned no results for node {NodeId}")]
    public static partial void LogBrowseNoResultsForNode(
        this ILogger logger,
        object? nodeId);

    /// <summary>
    ///     Logs that a method node was found with its OPC UA NodeId.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="methodName">The name of the found method.</param>
    /// <param name="nodeId">The OPC UA NodeId of the found method.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found method {MethodName} with NodeId: {NodeId}")]
    public static partial void LogFoundMethod(
        this ILogger logger,
        string methodName,
        object? nodeId);

    /// <summary>
    ///     Logs a warning when a method cannot be found on a node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="methodName">The name of the method that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Method {MethodName} not found")]
    public static partial void LogMethodNotFound(
        this ILogger logger,
        string methodName);

    /// <summary>
    ///     Logs that browsing for a method was cancelled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="methodName">The name of the method whose browse was cancelled.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Browse for method {MethodName} was cancelled")]
    public static partial void LogBrowseForMethodCancelled(
        this ILogger logger,
        string methodName);

    /// <summary>
    ///     Logs an OPC UA service error that occurred while browsing for a method.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The OPC UA service result exception.</param>
    /// <param name="methodName">The name of the method being browsed when the error occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OPC UA service error while browsing for method {MethodName}")]
    public static partial void LogOpcUaServiceErrorBrowsingMethod(
        this ILogger logger,
        Exception ex,
        string methodName);

    /// <summary>
    ///     Logs an unexpected error that occurred while browsing for a method.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The unexpected exception.</param>
    /// <param name="methodName">The name of the method being browsed when the error occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unexpected error browsing for method {MethodName}")]
    public static partial void LogUnexpectedErrorBrowsingMethod(
        this ILogger logger,
        Exception ex,
        string methodName);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14RuntimeAgent — Execution monitoring
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a skill execution has completed successfully, including the elapsed duration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the skill that completed.</param>
    /// <param name="duration">The elapsed execution duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skill '{SkillName}' execution completed - Duration: {Duration:F2}s")]
    public static partial void LogSkillExecutionCompleted(
        this ILogger logger,
        string skillName,
        double duration);

    /// <summary>
    ///     Logs a warning when a skill execution ends with a non-success status.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the skill that ended abnormally.</param>
    /// <param name="status">The final execution status string.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skill '{SkillName}' execution ended with status: {Status}")]
    public static partial void LogSkillExecutionEndedWithStatus(
        this ILogger logger,
        string skillName,
        string? status);

    /// <summary>
    ///     Logs that execution monitoring was cancelled for a skill.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the skill whose monitoring was cancelled.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Execution monitoring cancelled for skill '{SkillName}'")]
    public static partial void LogExecutionMonitoringCancelled(
        this ILogger logger,
        string skillName);

    /// <summary>
    ///     Logs a warning when at least one OPC UA read in the execution-state polling loop returns a bad
    ///     status code. The affected fields are substituted with fallback values ("unknown" for status,
    ///     0 for progress, elapsed, and remaining), producing a fabricated progress report for the execution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The unique identifier of the skill execution being monitored.</param>
    /// <param name="statusCode">The OPC UA <see cref="StatusCode"/> from the status-field read.</param>
    /// <param name="progressCode">The OPC UA <see cref="StatusCode"/> from the progress-field read.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "KUKA iiwa execution-state OPC UA read returned bad status for execution {ExecutionId}; substituting status='unknown' and 0 for progress/elapsed/remaining (statusCode={StatusCode}, progressCode={ProgressCode})")]
    public static partial void LogExecutionStateReadFailed(
        this ILogger logger,
        Guid executionId,
        StatusCode statusCode,
        StatusCode progressCode);

    /// <summary>
    ///     Logs an error with exception details when execution progress monitoring fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the monitoring failure.</param>
    /// <param name="skillName">The name of the skill whose monitoring failed.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error monitoring execution progress for skill '{SkillName}'")]
    public static partial void LogExecutionMonitoringError(
        this ILogger logger,
        Exception ex,
        string skillName);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14RuntimeAgent — Data access errors
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when joint values cannot be read because the OPC UA session is not connected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot read joint values: OPC UA session is not connected")]
    public static partial void LogCannotReadJointValuesNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when torque values cannot be read because the OPC UA session is not connected.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot read torque values: OPC UA session is not connected")]
    public static partial void LogCannotReadTorqueValuesNotConnected(this ILogger logger);

    // ──────────────────────────────────────────────────
    //  KukaAgentFactory — Configuration loading
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that KUKA agent configuration is being loaded from a file.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="filePath">The path to the configuration file being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loading KUKA agent configuration from file: {FilePath}")]
    public static partial void LogLoadingFromFile(
        this ILogger logger,
        string filePath);

    /// <summary>
    ///     Logs an error when the specified configuration file is not found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="filePath">The path to the missing configuration file.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Configuration file not found: {FilePath}")]
    public static partial void LogConfigurationFileNotFound(
        this ILogger logger,
        string filePath);

    /// <summary>
    ///     Logs an error with exception details when loading configuration from a file fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the load failure.</param>
    /// <param name="filePath">The path to the file that failed to load.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load configuration from file: {FilePath}")]
    public static partial void LogLoadConfigurationFailed(
        this ILogger logger,
        Exception ex,
        string filePath);

    /// <summary>
    ///     Logs that JSON configuration parsing has started for KUKA agents.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Parsing JSON configuration for KUKA agents")]
    public static partial void LogParsingJsonConfiguration(this ILogger logger);

    /// <summary>
    ///     Logs a warning when JSON configuration deserialization results in null.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Configuration deserialized to null, returning empty list")]
    public static partial void LogConfigurationDeserializedToNull(this ILogger logger);

    /// <summary>
    ///     Logs an error with exception details when JSON configuration parsing fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The JSON exception that caused the parse failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to parse JSON configuration")]
    public static partial void LogJsonParsingFailed(
        this ILogger logger,
        Exception ex);

    // ──────────────────────────────────────────────────
    //  KukaAgentFactory — Agent creation
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that creation of KUKA agents from configuration has started.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of agents to create from configuration.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating {Count} KUKA agents from configuration")]
    public static partial void LogCreatingAgentsFromConfiguration(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs that provider data loading has started for position tags, scene objects, and skill definitions.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loading position tags, scene objects, and skill definitions from providers")]
    public static partial void LogLoadingProviderData(this ILogger logger);

    /// <summary>
    ///     Logs the counts of loaded provider data (position tags, scene objects, and skill definitions).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="positionTagCount">The number of position tags loaded.</param>
    /// <param name="sceneObjectCount">The number of scene objects loaded.</param>
    /// <param name="skillDefinitionCount">The number of skill definitions loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Loaded {PositionTagCount} position tags, {SceneObjectCount} scene objects, and {SkillDefinitionCount} skill definitions")]
    public static partial void LogProviderDataLoaded(
        this ILogger logger,
        int positionTagCount,
        int sceneObjectCount,
        int skillDefinitionCount);

    /// <summary>
    ///     Logs that a KUKA agent was successfully created from configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the created agent.</param>
    /// <param name="agentId">The unique identifier assigned to the created agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created KUKA agent: {AgentName} (ID: {AgentId})")]
    public static partial void LogKukaAgentCreated(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs an error with exception details when creation of a KUKA agent fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the agent creation failure.</param>
    /// <param name="agentName">The name of the agent that failed to create.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create KUKA agent: {AgentName}")]
    public static partial void LogKukaAgentCreationFailed(
        this ILogger logger,
        Exception ex,
        string agentName);

    /// <summary>
    ///     Logs that all KUKA agents were successfully created from configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of agents that were successfully created.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully created {Count} KUKA agents")]
    public static partial void LogKukaAgentsCreated(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs that creation of a specific KUKA agent has started.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The name of the agent being created.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Creating KUKA agent: {AgentName}")]
    public static partial void LogCreatingKukaAgent(
        this ILogger logger,
        string agentName);

    /// <summary>
    ///     Logs that a skill was resolved from its definition for a specific agent.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the resolved skill.</param>
    /// <param name="agentName">The name of the agent the skill is being resolved for.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Resolved skill {SkillName} from definition for agent {AgentName}")]
    public static partial void LogSkillResolved(
        this ILogger logger,
        string skillName,
        string agentName);

    /// <summary>
    ///     Logs that a certificate manager was created for a KUKA agent with encryption enabled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The name of the agent the certificate manager was created for.</param>
    /// <param name="storePath">The certificate store path used by the certificate manager.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created certificate manager for agent {AgentName} with store path: {StorePath}")]
    public static partial void LogCertificateManagerCreated(
        this ILogger logger,
        string agentName,
        string storePath);

    /// <summary>
    ///     Logs that a KUKA agent was created with a specific number of skills.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentName">The display name of the created agent.</param>
    /// <param name="skillCount">The number of skills associated with the created agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created KUKA agent {AgentName} with {SkillCount} skills")]
    public static partial void LogKukaAgentCreatedWithSkills(
        this ILogger logger,
        string agentName,
        int skillCount);

    /// <summary>
    ///     Logs a warning when LoadAgentsAsync is called without a configuration file path.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "LoadAgentsAsync called without configuration file path. Returning empty list. Use CreateFromJsonFileAsync with a specific file path instead.")]
    public static partial void LogLoadAgentsWithoutFilePath(this ILogger logger);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14DataReader — Joint values
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when the OPC UA session is not connected and joint values cannot be read.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot read joint values: OPC UA session is not connected")]
    public static partial void LogDataReaderCannotReadJointValuesNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when the OPC UA Read operation returns null results for joint values.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OPC UA Read operation returned null results for joint values")]
    public static partial void LogJointValuesNullResults(this ILogger logger);

    /// <summary>
    ///     Logs a warning when reading a specific joint axis fails with a bad status code.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="joint">The 1-based joint axis number that failed to read.</param>
    /// <param name="status">The OPC UA <see cref="StatusCode"/> from the failed read operation.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to read joint A{Joint}: {Status}")]
    public static partial void LogJointReadFailed(
        this ILogger logger,
        int joint,
        StatusCode status);

    /// <summary>
    ///     Logs the successfully read joint values at trace level.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="jointValues">A formatted string of all joint values.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Read joint values: [{JointValues}]")]
    public static partial void LogJointValuesRead(
        this ILogger logger,
        string jointValues);

    /// <summary>
    ///     Logs an error with exception details when reading joint values from the OPC UA server fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the read failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error reading joint values from OPC UA server")]
    public static partial void LogJointValuesReadError(
        this ILogger logger,
        Exception ex);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14DataReader — Torque values
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when the OPC UA session is not connected and torque values cannot be read.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot read torque values: OPC UA session is not connected")]
    public static partial void LogDataReaderCannotReadTorqueValuesNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when the OPC UA Read operation returns null results for torque values.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OPC UA Read operation returned null results for torque values")]
    public static partial void LogTorqueValuesNullResults(this ILogger logger);

    /// <summary>
    ///     Logs a warning when reading a specific torque axis fails with a bad status code.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="torque">The 1-based torque axis number that failed to read.</param>
    /// <param name="status">The OPC UA <see cref="StatusCode"/> from the failed read operation.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to read torque T{Torque}: {Status}")]
    public static partial void LogTorqueReadFailed(
        this ILogger logger,
        int torque,
        StatusCode status);

    /// <summary>
    ///     Logs the successfully read torque values at trace level.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="torqueValues">A formatted string of all torque values.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Read torque values: [{TorqueValues}]")]
    public static partial void LogTorqueValuesRead(
        this ILogger logger,
        string torqueValues);

    /// <summary>
    ///     Logs an error with exception details when reading torque values from the OPC UA server fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the read failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error reading torque values from OPC UA server")]
    public static partial void LogTorqueValuesReadError(
        this ILogger logger,
        Exception ex);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14DataReader — Execution estimate
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs an error when the OPC UA session is not connected and the method cannot be called.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot call method: OPC UA session is not connected")]
    public static partial void LogCannotCallMethodNotConnected(this ILogger logger);

    /// <summary>
    ///     Logs an error when the robot node cannot be found in the OPC UA Objects folder (data reader context).
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the robot that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Could not find robot node '{RobotName}' in Objects")]
    public static partial void LogDataReaderRobotNodeNotFound(
        this ILogger logger,
        string robotName);

    /// <summary>
    ///     Logs an error when the GetExecutionEstimateAsync method cannot be found on the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Could not find GetExecutionEstimateAsync method on robot node")]
    public static partial void LogGetExecutionEstimateMethodNotFound(this ILogger logger);

    /// <summary>
    ///     Logs the OPC UA method call details including object and method node IDs.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="objectId">The OPC UA NodeId of the object containing the method.</param>
    /// <param name="methodId">The OPC UA NodeId of the method being called.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Calling OPC UA method - ObjectId: {ObjectId}, MethodId: {MethodId}")]
    public static partial void LogCallingOpcUaMethod(
        this ILogger logger,
        object? objectId,
        object? methodId);

    /// <summary>
    ///     Logs the 6DOF pose parameters being passed to the OPC UA method.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="x">The target X position in millimeters.</param>
    /// <param name="y">The target Y position in millimeters.</param>
    /// <param name="z">The target Z position in millimeters.</param>
    /// <param name="alpha">The target alpha rotation in degrees.</param>
    /// <param name="beta">The target beta rotation in degrees.</param>
    /// <param name="gamma">The target gamma rotation in degrees.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Method parameters: x={X}, y={Y}, z={Z}, alpha={Alpha}, beta={Beta}, gamma={Gamma}")]
    public static partial void LogMethodParameters(
        this ILogger logger,
        double x,
        double y,
        double z,
        double alpha,
        double beta,
        double gamma);

    /// <summary>
    ///     Logs an error when the OPC UA method call returns no output arguments.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OPC UA method call returned no output arguments")]
    public static partial void LogMethodCallNoOutput(this ILogger logger);

    /// <summary>
    ///     Logs the received execution estimate duration from the OPC UA method call.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="duration">The estimated execution duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Execution estimate received: {Duration:F2} seconds")]
    public static partial void LogDataReaderExecutionEstimateReceived(
        this ILogger logger,
        double duration);

    /// <summary>
    ///     Logs an error with exception details when calling the GetExecutionEstimateAsync OPC UA method fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the method call failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error calling OPC UA GetExecutionEstimateAsync method")]
    public static partial void LogGetExecutionEstimateError(
        this ILogger logger,
        Exception ex);

    // ──────────────────────────────────────────────────
    //  KukaIiwa14DataReader — OPC UA browsing (sync)
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the data reader is browsing the Objects folder to locate the robot node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the robot being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Browsing Objects folder to find robot node '{RobotName}'")]
    public static partial void LogDataReaderBrowsingForRobotNode(
        this ILogger logger,
        string robotName);

    /// <summary>
    ///     Logs a warning when the data reader browse returns no results for the Objects folder.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Browse returned no results for Objects folder")]
    public static partial void LogDataReaderBrowseNoResultsForObjectsFolder(this ILogger logger);

    /// <summary>
    ///     Logs an error when the data reader browse fails with a bad status code.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="status">The OPC UA <see cref="StatusCode"/> from the failed browse operation.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Browse failed with status: {Status}")]
    public static partial void LogDataReaderBrowseFailedWithStatus(
        this ILogger logger,
        StatusCode status);

    /// <summary>
    ///     Logs that the data reader found the robot node with its OPC UA NodeId.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the found robot node.</param>
    /// <param name="nodeId">The OPC UA NodeId of the found robot node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found robot node '{RobotName}' with NodeId: {NodeId}")]
    public static partial void LogDataReaderFoundRobotNode(
        this ILogger logger,
        string robotName,
        object? nodeId);

    /// <summary>
    ///     Logs a warning when the data reader cannot find the robot node among the Objects folder children.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="robotName">The name of the robot that was not found.</param>
    /// <param name="count">The number of children found in the Objects folder.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Robot node '{RobotName}' not found among {Count} children of Objects folder")]
    public static partial void LogDataReaderRobotNodeNotFoundAmongChildren(
        this ILogger logger,
        string robotName,
        int count);

    /// <summary>
    ///     Logs the available objects in the Objects folder for debugging purposes.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="objects">A comma-separated string of available object names.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Available objects: {Objects}")]
    public static partial void LogAvailableObjects(
        this ILogger logger,
        string objects);

    /// <summary>
    ///     Logs an error with exception details when browsing for the robot node fails in the data reader.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the browse failure.</param>
    /// <param name="robotName">The name of the robot that was being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error browsing for robot node '{RobotName}'")]
    public static partial void LogDataReaderBrowsingRobotNodeError(
        this ILogger logger,
        Exception ex,
        string robotName);

    /// <summary>
    ///     Logs that the data reader is browsing a node to find a specific method.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeId">The OPC UA NodeId of the parent node being browsed.</param>
    /// <param name="methodName">The name of the method being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Browsing node {NodeId} to find method {MethodName}")]
    public static partial void LogDataReaderBrowsingForMethod(
        this ILogger logger,
        object? nodeId,
        string methodName);

    /// <summary>
    ///     Logs a warning when the data reader browse returns no results for a specific node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="nodeId">The OPC UA NodeId that returned no browse results.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Browse returned no results for node {NodeId}")]
    public static partial void LogDataReaderBrowseNoResultsForNode(
        this ILogger logger,
        object? nodeId);

    /// <summary>
    ///     Logs that the data reader found a method node with its OPC UA NodeId.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="methodName">The name of the found method.</param>
    /// <param name="nodeId">The OPC UA NodeId of the found method.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found method {MethodName} with NodeId: {NodeId}")]
    public static partial void LogDataReaderFoundMethod(
        this ILogger logger,
        string methodName,
        object? nodeId);

    /// <summary>
    ///     Logs a warning when the data reader cannot find a method node among the children of a parent node.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="methodName">The name of the method that was not found.</param>
    /// <param name="count">The number of children found on the parent node.</param>
    /// <param name="nodeId">The OPC UA NodeId of the parent node that was browsed.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Method {MethodName} not found among {Count} children of node {NodeId}")]
    public static partial void LogDataReaderMethodNotFoundAmongChildren(
        this ILogger logger,
        string methodName,
        int count,
        object? nodeId);

    /// <summary>
    ///     Logs the available children of a node for debugging purposes.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="children">A comma-separated string of available child node names.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Available children: {Children}")]
    public static partial void LogAvailableChildren(
        this ILogger logger,
        string children);

    /// <summary>
    ///     Logs an error with exception details when browsing for a method fails in the data reader.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the browse failure.</param>
    /// <param name="methodName">The name of the method that was being searched for.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error browsing for method {MethodName}")]
    public static partial void LogDataReaderBrowsingMethodError(
        this ILogger logger,
        Exception ex,
        string methodName);
}