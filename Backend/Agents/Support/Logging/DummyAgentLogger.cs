using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured logging for dummy agent operations using high-performance source-generated logging.
///     Covers logging for <c>DummyRuntimeAgent</c>, <c>DummyAgentFactory</c>,
///     and <c>ConfigurableDummyRuntimeAgent</c>.
/// </summary>
public static partial class DummyAgentLogger
{
    // ──────────────────────────────────────────────────────────────
    //  DummyRuntimeAgent — GetExecutionEstimateAsync
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of execution estimation for a skill on a dummy agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent performing the estimation.</param>
    /// <param name="skillName">The name of the skill being estimated.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Agent '{AgentName}' estimating execution for skill '{SkillName}' (ID: {SkillId})")]
    public static partial void LogDummyEstimatingExecution(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs a warning when the agent does not have the requested skill in its available skills.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="skillName">The name of the missing skill.</param>
    /// <param name="skillId">The unique identifier of the missing skill.</param>
    /// <param name="availableSkills">A formatted string listing the agent's available skills.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Agent '{AgentName}' does not have skill '{SkillName}' (ID: {SkillId}) in available skills. Available skills: {AvailableSkills}")]
    public static partial void LogSkillNotAvailable(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId,
        string availableSkills);

    /// <summary>
    ///     Logs whether a skill can adapt based on its name containing 'Adaptive'.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="canAdapt">Whether the skill supports adaptive execution.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Skill '{SkillName}' can adapt: {CanAdapt} (based on name containing 'Adaptive')")]
    public static partial void LogSkillCanAdapt(
        this ILogger logger,
        string skillName,
        bool canAdapt);

    /// <summary>
    ///     Logs the properties of a skill for debugging purposes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="propertyCount">The number of properties the skill has.</param>
    /// <param name="properties">A formatted string listing the skill properties and their values.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Skill '{SkillName}' has {PropertyCount} properties: {Properties}")]
    public static partial void LogSkillProperties(
        this ILogger logger,
        string skillName,
        int propertyCount,
        string properties);

    /// <summary>
    ///     Logs when a NominalDuration property is found on a skill.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="duration">The nominal duration value found.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found NominalDuration property for skill '{SkillName}': {Duration}")]
    public static partial void LogNominalDurationFound(
        this ILogger logger,
        string skillName,
        double duration);

    /// <summary>
    ///     Logs a warning when no NominalDuration property is found and a random duration is used instead.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="duration">The randomly generated duration being used.</param>
    /// <param name="propertyFound">Whether a property with the name 'NominalDuration' was found at all.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "No NominalDuration property found for skill '{SkillName}'. Using random duration: {Duration}. TypedProperty search result: {PropertyFound}")]
    public static partial void LogNominalDurationMissing(
        this ILogger logger,
        string skillName,
        double duration,
        bool propertyFound);

    /// <summary>
    ///     Logs the creation of a fixed (non-adaptive) execution estimate.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="nominalDuration">The estimated nominal duration in seconds.</param>
    /// <param name="canAdapt">Whether the skill supports adaptive execution (always false for fixed).</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Agent '{AgentName}' created FIXED execution estimate for skill '{SkillName}': NominalDuration={NominalDuration}, CanAdapt={CanAdapt}")]
    public static partial void LogFixedEstimateCreated(
        this ILogger logger,
        string agentName,
        string skillName,
        double nominalDuration,
        bool canAdapt);

    /// <summary>
    ///     Logs the creation of an adaptive execution estimate with the minimum adaptive duration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="nominalDuration">The estimated nominal duration in seconds.</param>
    /// <param name="minAdaptive">The minimum adaptive duration in seconds.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Agent '{AgentName}' created ADAPTIVE execution estimate for skill '{SkillName}': NominalDuration={NominalDuration}, MinAdaptive={MinAdaptive:F2}")]
    public static partial void LogAdaptiveEstimateCreated(
        this ILogger logger,
        string agentName,
        string skillName,
        double nominalDuration,
        double? minAdaptive);

    /// <summary>
    ///     Logs the generation of an output value for a skill property during execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="propertyName">The name of the output property.</param>
    /// <param name="outputValue">The generated output value.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Generated output for skill '{SkillName}' property '{PropertyName}': {OutputValue}")]
    public static partial void LogGeneratedOutput(
        this ILogger logger,
        string skillName,
        string propertyName,
        object outputValue);

    /// <summary>
    ///     Logs a warning when no execution estimate is available for the requested skill and a hardcoded
    ///     nominal duration is substituted, indicating the agent is executing a skill outside its
    ///     available-skills list.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent executing the skill.</param>
    /// <param name="skillName">The name of the skill for which no estimate was found.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="fallbackDuration">The hardcoded nominal duration in seconds substituted for the missing estimate.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Dummy agent '{AgentName}' has no execution estimate for skill '{SkillName}' ({SkillId}); falling back to hardcoded nominal duration {FallbackDuration}s")]
    public static partial void LogExecuteFallbackNominalDuration(
        this ILogger logger,
        string agentName,
        string skillName,
        Guid skillId,
        double fallbackDuration);

    // ──────────────────────────────────────────────────────────────
    //  DummyRuntimeAgent — ExecuteSkillAdaptivelyAsync
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a finish signal has been received for an adaptive skill execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill being executed.</param>
    /// <param name="agentName">The name of the agent executing the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Finish signal received for skill {SkillName} (Agent: {AgentName})")]
    public static partial void LogFinishSignalReceived(
        this ILogger logger,
        string skillName,
        string agentName);

    /// <summary>
    ///     Logs an updated planned finish time received during adaptive execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="target">The updated target duration in seconds.</param>
    /// <param name="skillName">The name of the skill being executed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Agent {AgentName} received updated planned finish: {Target:F1}s for skill {SkillName}")]
    public static partial void LogUpdatedPlannedFinish(
        this ILogger logger,
        string agentName,
        double target,
        string skillName);

    /// <summary>
    ///     Logs that an adaptive skill execution is completing via the finish signal.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="currentTime">The elapsed time in seconds when the finish signal was processed.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skill {SkillName} (Agent: {AgentName}) completing via finish signal at {CurrentTime:F1}s")]
    public static partial void LogCompletingViaFinishSignal(
        this ILogger logger,
        string skillName,
        string agentName,
        double currentTime);

    /// <summary>
    ///     Logs a warning when an adaptive skill execution is cancelled via the cancellation token.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skill {SkillName} (Agent: {AgentName}) cancelled via cancellation token")]
    public static partial void LogSkillCancelled(
        this ILogger logger,
        string skillName,
        string agentName);

    // ──────────────────────────────────────────────────────────────
    //  DummyAgentFactory
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of loading a dummy agent configuration from a file.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="filePath">The path to the configuration file being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loading dummy agent configuration from file: {FilePath}")]
    public static partial void LogDummyLoadingFromFile(
        this ILogger logger,
        string filePath);

    /// <summary>
    ///     Logs an error when the configuration file is not found on disk.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="filePath">The path to the missing configuration file.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Configuration file not found: {FilePath}")]
    public static partial void LogDummyConfigurationFileNotFound(
        this ILogger logger,
        string filePath);

    /// <summary>
    ///     Logs an error when loading configuration from a file fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during file loading.</param>
    /// <param name="filePath">The path to the configuration file that failed to load.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load configuration from file: {FilePath}")]
    public static partial void LogConfigurationLoadFailed(
        this ILogger logger,
        Exception exception,
        string filePath);

    /// <summary>
    ///     Logs the start of JSON configuration parsing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Parsing JSON configuration for dummy agents")]
    public static partial void LogDummyParsingJsonConfiguration(
        this ILogger logger);

    /// <summary>
    ///     Logs a warning when the deserialized configuration is null.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Configuration deserialized to null, returning empty list")]
    public static partial void LogDummyConfigurationDeserializedToNull(
        this ILogger logger);

    /// <summary>
    ///     Logs an error when JSON configuration parsing fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The JSON exception that occurred.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to parse JSON configuration")]
    public static partial void LogDummyJsonParsingFailed(
        this ILogger logger,
        Exception exception);

    /// <summary>
    ///     Logs the number of dummy agents being created from configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of agents to create.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Creating {Count} dummy agents from configuration")]
    public static partial void LogDummyCreatingAgentsFromConfiguration(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs the start of loading scene data from providers.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loading position tags, scene objects, and skill definitions from providers")]
    public static partial void LogLoadingSceneData(
        this ILogger logger);

    /// <summary>
    ///     Logs the counts of loaded position tags, scene objects, and skill definitions.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="positionTagCount">The number of position tags loaded.</param>
    /// <param name="sceneObjectCount">The number of scene objects loaded.</param>
    /// <param name="skillDefinitionCount">The number of skill definitions loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Loaded {PositionTagCount} position tags, {SceneObjectCount} scene objects, and {SkillDefinitionCount} skill definitions")]
    public static partial void LogSceneDataLoaded(
        this ILogger logger,
        int positionTagCount,
        int sceneObjectCount,
        int skillDefinitionCount);

    /// <summary>
    ///     Logs the successful creation of a dummy agent with its name and ID.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the created agent.</param>
    /// <param name="agentId">The unique identifier of the created agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created dummy agent: {AgentName} (ID: {AgentId})")]
    public static partial void LogAgentCreated(
        this ILogger logger,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs an error when creating a dummy agent fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during agent creation.</param>
    /// <param name="agentName">The name of the agent that failed to create.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create dummy agent: {AgentName}")]
    public static partial void LogAgentCreationFailed(
        this ILogger logger,
        Exception exception,
        string agentName);

    /// <summary>
    ///     Logs the successful creation of all dummy agents.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of agents successfully created.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully created {Count} dummy agents")]
    public static partial void LogAllAgentsCreated(
        this ILogger logger,
        int count);

    /// <summary>
    ///     Logs the start of creating a single dummy agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the agent being created.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Creating dummy agent: {AgentName}")]
    public static partial void LogCreatingAgent(
        this ILogger logger,
        string agentName);

    /// <summary>
    ///     Logs that a skill was resolved from a skill definition for an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the resolved skill.</param>
    /// <param name="agentName">The name of the agent the skill belongs to.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Resolved skill {SkillName} from definition for agent {AgentName}")]
    public static partial void LogSkillResolvedFromDefinition(
        this ILogger logger,
        string skillName,
        string agentName);

    /// <summary>
    ///     Logs that an inline skill was created for an agent (backward compatibility path).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the inline skill.</param>
    /// <param name="agentName">The name of the agent the skill belongs to.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Created inline skill {SkillName} for agent {AgentName}")]
    public static partial void LogInlineSkillCreated(
        this ILogger logger,
        string skillName,
        string agentName);

    /// <summary>
    ///     Logs the successful creation of a configurable dummy agent with its skill count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentName">The name of the created agent.</param>
    /// <param name="skillCount">The number of skills assigned to the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created configurable dummy agent {AgentName} with {SkillCount} skills")]
    public static partial void LogConfigurableAgentCreated(
        this ILogger logger,
        string agentName,
        int skillCount);

    /// <summary>
    ///     Logs a warning when LoadAgentsAsync is called without a configuration file path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "LoadAgentsAsync called without configuration file path. Returning empty list. Use CreateFromJsonFileAsync with a specific file path instead.")]
    public static partial void LogLoadAgentsCalledWithoutFilePath(
        this ILogger logger);

    // ──────────────────────────────────────────────────────────────
    //  ConfigurableDummyRuntimeAgent
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of getting an execution estimate for a skill on a configurable agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Getting execution estimate for skill '{SkillName}' (ID: {SkillId}) on agent '{AgentName}'")]
    public static partial void LogGettingExecutionEstimate(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    /// <summary>
    ///     Logs that a skill configuration was found by ID match.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found skill configuration for skill '{SkillName}' (ID: {SkillId})")]
    public static partial void LogSkillConfigFoundById(
        this ILogger logger,
        string skillName,
        Guid skillId);

    /// <summary>
    ///     Logs that a skill configuration was found by name match as a fallback.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Found skill configuration by name match for skill '{SkillName}'")]
    public static partial void LogSkillConfigFoundByName(
        this ILogger logger,
        string skillName);

    /// <summary>
    ///     Logs a warning when no skill configuration is found for a skill on an agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No skill configuration found for skill '{SkillName}' (ID: {SkillId}) on agent '{AgentName}'")]
    public static partial void LogSkillConfigNotFound(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    /// <summary>
    ///     Logs the details of a created execution estimate for a configurable agent.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="canAdapt">Whether the skill supports adaptive execution.</param>
    /// <param name="nominalDuration">The nominal duration in seconds.</param>
    /// <param name="minDuration">The minimum adaptive duration in seconds, or null if not adaptive.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Created execution estimate for skill '{SkillName}' on agent '{AgentName}': CanAdapt={CanAdapt}, Nominal={NominalDuration}, Min={MinDuration}")]
    public static partial void LogExecutionEstimateCreated(
        this ILogger logger,
        string skillName,
        string agentName,
        bool canAdapt,
        double nominalDuration,
        double? minDuration);

    /// <summary>
    ///     Logs the start of checking whether a skill can be executed adaptively.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "Checking if skill '{SkillName}' (ID: {SkillId}) can be executed adaptively on agent '{AgentName}'")]
    public static partial void LogCheckingAdaptiveCapability(
        this ILogger logger,
        string skillName,
        Guid skillId,
        string agentName);

    /// <summary>
    ///     Logs the result of checking whether a skill can be executed adaptively.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="canExecuteAdaptively">Whether the skill can be executed adaptively.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Skill '{SkillName}' on agent '{AgentName}' can execute adaptively: {CanExecuteAdaptively}")]
    public static partial void LogAdaptiveCapabilityResult(
        this ILogger logger,
        string skillName,
        string agentName,
        bool canExecuteAdaptively);
}