namespace FHOOE.Freydis.GraphQLServer.Support.Logging;

/// <summary>
///     Provides structured, high-performance source-generated logging for application initialization.
///     Covers <c>InitializationHostedService</c> (orchestration), <c>AgentStartupService</c>
///     (agent loading and activation), <c>SceneInitializationService</c> (position tags and scene objects),
///     and <c>SkillsInitializationService</c> (skill definition loading).
/// </summary>
public static partial class InitializationLogger
{
    // ──────────────────────────────────────────────────
    //  InitializationHostedService — Orchestration
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that the application initialization process is starting.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Starting application initialization")]
    public static partial void LogStartingInitialization(this ILogger logger);

    /// <summary>
    ///     Logs that initialization services are being resolved from the dependency injection container.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Debug,
        Message = "Resolving initialization services")]
    public static partial void LogResolvingServices(this ILogger logger);

    /// <summary>
    ///     Logs that scene initialization completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Debug,
        Message = "Scene initialization completed")]
    public static partial void LogSceneCompleted(this ILogger logger);

    /// <summary>
    ///     Logs that scene initialization is being skipped because PostgreSQL is not available.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Warning,
        Message = "Skipping scene initialization: PostgreSQL is not available")]
    public static partial void LogSkippingScene(this ILogger logger);

    /// <summary>
    ///     Logs that scene initialization failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the scene initialization failure.</param>
    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Error,
        Message = "Scene initialization failed")]
    public static partial void LogSceneFailed(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs that skills initialization completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Debug,
        Message = "Skills initialization completed")]
    public static partial void LogSkillsCompleted(this ILogger logger);

    /// <summary>
    ///     Logs that skills initialization failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the skills initialization failure.</param>
    [LoggerMessage(
        EventId = 5007,
        Level = LogLevel.Error,
        Message = "Skills initialization failed")]
    public static partial void LogSkillsFailed(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs that agent initialization completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5008,
        Level = LogLevel.Debug,
        Message = "Agent initialization completed")]
    public static partial void LogAgentsCompleted(this ILogger logger);

    /// <summary>
    ///     Logs that agent initialization is being skipped because PostgreSQL is not available.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5009,
        Level = LogLevel.Warning,
        Message = "Skipping agent initialization: PostgreSQL is not available")]
    public static partial void LogSkippingAgents(this ILogger logger);

    /// <summary>
    ///     Logs that agent initialization failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the agent initialization failure.</param>
    [LoggerMessage(
        EventId = 5010,
        Level = LogLevel.Error,
        Message = "Agent initialization failed")]
    public static partial void LogAgentsFailed(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs that the entire application initialization process completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5011,
        Level = LogLevel.Information,
        Message = "Application initialization process completed")]
    public static partial void LogInitializationCompleted(this ILogger logger);

    /// <summary>
    ///     Logs a critical error when application initialization encounters an unexpected exception,
    ///     indicating the application may be in a broken state.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the critical initialization failure.</param>
    [LoggerMessage(
        EventId = 5012,
        Level = LogLevel.Critical,
        Message =
            "Application initialization encountered an unexpected error. The application may be in a broken state.")]
    public static partial void LogInitializationCriticalError(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs that the initialization hosted service is stopping.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5013,
        Level = LogLevel.Debug,
        Message = "Initialization hosted service stopping")]
    public static partial void LogServiceStopping(this ILogger logger);

    // ──────────────────────────────────────────────────
    //  AgentStartupService — Agent initialization
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs the agent initialization configuration, showing which agent types are enabled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="dummyEnabled">Whether dummy agents are enabled.</param>
    /// <param name="kukaEnabled">Whether KUKA agents are enabled.</param>
    /// <param name="realEnabled">Whether real agents are enabled.</param>
    /// <param name="dtEnabled">Whether Digital Twin agents are enabled.</param>
    [LoggerMessage(
        EventId = 5100,
        Level = LogLevel.Debug,
        Message =
            "Initializing agents (Dummy: {DummyEnabled}, Kuka: {KukaEnabled}, Real: {RealEnabled}, DigitalTwin: {DtEnabled})")]
    public static partial void LogInitializingAgents(
        this ILogger logger,
        bool dummyEnabled,
        bool kukaEnabled,
        bool realEnabled,
        bool dtEnabled);

    /// <summary>
    ///     Logs that auto-load is disabled for the specified agent type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type whose auto-load is disabled.</param>
    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Debug,
        Message = "{AgentType} agent auto-load is disabled")]
    public static partial void LogAgentAutoLoadDisabled(this ILogger logger, string agentType);

    /// <summary>
    ///     Logs a warning when no configuration file is specified for the given agent type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type missing a configuration file path.</param>
    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Warning,
        Message = "No {AgentType} agent configuration file specified")]
    public static partial void LogNoAgentConfigFile(this ILogger logger, string agentType);

    /// <summary>
    ///     Logs that agents of the specified type are being loaded from a configuration file.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type being loaded.</param>
    /// <param name="configurationFile">The path to the configuration file.</param>
    [LoggerMessage(
        EventId = 5103,
        Level = LogLevel.Debug,
        Message = "Loading {AgentType} agents from {ConfigurationFile}")]
    public static partial void LogLoadingAgents(this ILogger logger, string agentType, string configurationFile);

    /// <summary>
    ///     Logs the number of agents created by the factory for the specified type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of agents the factory created.</param>
    /// <param name="agentType">The agent type that was created.</param>
    [LoggerMessage(
        EventId = 5104,
        Level = LogLevel.Debug,
        Message = "Factory created {Count} {AgentType} agents")]
    public static partial void LogFactoryCreatedAgents(this ILogger logger, int count, string agentType);

    /// <summary>
    ///     Logs an error when activation of an individual agent fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the activation failure.</param>
    /// <param name="agentType">The type of the agent that failed to activate.</param>
    /// <param name="agentName">The display name of the agent that failed.</param>
    /// <param name="agentId">The unique identifier of the agent that failed.</param>
    [LoggerMessage(
        EventId = 5105,
        Level = LogLevel.Error,
        Message = "Failed to activate {AgentType} agent '{AgentName}' (ID: {AgentId})")]
    public static partial void LogAgentActivationFailed(
        this ILogger logger,
        Exception ex,
        string agentType,
        string agentName,
        Guid agentId);

    /// <summary>
    ///     Logs that agents of the specified type are ready, including how many were activated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="agentType">The agent type that is ready.</param>
    /// <param name="activatedCount">The number of agents successfully activated.</param>
    /// <param name="totalCount">The total number of agents that were loaded from configuration.</param>
    [LoggerMessage(
        EventId = 5106,
        Level = LogLevel.Information,
        Message = "{AgentType} agents ready: {ActivatedCount}/{TotalCount} activated")]
    public static partial void LogAgentsReady(
        this ILogger logger,
        string agentType,
        int activatedCount,
        int totalCount);

    /// <summary>
    ///     Logs an error when the agent configuration file cannot be found on disk.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The FileNotFoundException that was thrown.</param>
    /// <param name="agentType">The agent type whose configuration file was not found.</param>
    /// <param name="configurationFile">The path to the missing configuration file.</param>
    [LoggerMessage(
        EventId = 5107,
        Level = LogLevel.Error,
        Message = "{AgentType} agent configuration file not found: {ConfigurationFile}")]
    public static partial void LogAgentConfigFileNotFound(
        this ILogger logger,
        Exception ex,
        string agentType,
        string configurationFile);

    /// <summary>
    ///     Logs an error when loading agents from the configuration file fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the loading failure.</param>
    /// <param name="agentType">The agent type whose loading failed.</param>
    [LoggerMessage(
        EventId = 5108,
        Level = LogLevel.Error,
        Message = "Failed to load {AgentType} agents from configuration file")]
    public static partial void LogAgentLoadFailed(this ILogger logger, Exception ex, string agentType);

    /// <summary>
    ///     Logs an error when the overall agent initialization process fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the initialization failure.</param>
    [LoggerMessage(
        EventId = 5109,
        Level = LogLevel.Error,
        Message = "Failed to initialize agents")]
    public static partial void LogAgentInitFailed(this ILogger logger, Exception ex);

    /// <summary>
    ///     Logs a warning that real agents are enabled in configuration but not yet implemented.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5110,
        Level = LogLevel.Warning,
        Message = "Real agents enabled but not yet implemented")]
    public static partial void LogRealAgentsNotYetImplemented(this ILogger logger);

    // ──────────────────────────────────────────────────
    //  SceneInitializationService — Scene loading
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that scene auto-load is disabled in configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5200,
        Level = LogLevel.Debug,
        Message = "Scene auto-load is disabled")]
    public static partial void LogSceneAutoLoadDisabled(this ILogger logger);

    /// <summary>
    ///     Logs a warning when no scene configuration file path is specified.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5201,
        Level = LogLevel.Warning,
        Message = "No scene configuration file specified")]
    public static partial void LogNoSceneConfigFile(this ILogger logger);

    /// <summary>
    ///     Logs that the scene configuration is being loaded from the specified file.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationFile">The path to the scene configuration file.</param>
    [LoggerMessage(
        EventId = 5202,
        Level = LogLevel.Debug,
        Message = "Loading scene configuration from {ConfigurationFile}")]
    public static partial void LogLoadingSceneConfig(this ILogger logger, string configurationFile);

    /// <summary>
    ///     Logs that position tags are being synced to the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of position tags to sync.</param>
    [LoggerMessage(
        EventId = 5203,
        Level = LogLevel.Debug,
        Message = "Syncing {Count} position tags")]
    public static partial void LogSyncingPositionTags(this ILogger logger, int count);

    /// <summary>
    ///     Logs that a position tag was created in the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tagName">The name of the created position tag.</param>
    /// <param name="tagId">The unique identifier of the created position tag.</param>
    [LoggerMessage(
        EventId = 5204,
        Level = LogLevel.Debug,
        Message = "Created position tag: {TagName} (ID: {TagId})")]
    public static partial void LogCreatedPositionTag(this ILogger logger, string tagName, Guid tagId);

    /// <summary>
    ///     Logs that an existing position tag was updated in the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="tagName">The name of the updated position tag.</param>
    /// <param name="tagId">The unique identifier of the updated position tag.</param>
    [LoggerMessage(
        EventId = 5205,
        Level = LogLevel.Debug,
        Message = "Updated position tag: {TagName} (ID: {TagId})")]
    public static partial void LogUpdatedPositionTag(this ILogger logger, string tagName, Guid tagId);

    /// <summary>
    ///     Logs an error when syncing a specific position tag to the database fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the sync failure.</param>
    /// <param name="tagName">The name of the position tag that failed to sync.</param>
    /// <param name="tagId">The unique identifier of the position tag that failed to sync.</param>
    [LoggerMessage(
        EventId = 5206,
        Level = LogLevel.Error,
        Message = "Failed to sync position tag: {TagName} (ID: {TagId})")]
    public static partial void LogPositionTagSyncFailed(this ILogger logger, Exception ex, string tagName, Guid tagId);

    /// <summary>
    ///     Logs that scene objects are being synced to the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="count">The number of scene objects to sync.</param>
    [LoggerMessage(
        EventId = 5207,
        Level = LogLevel.Debug,
        Message = "Syncing {Count} scene objects")]
    public static partial void LogSyncingSceneObjects(this ILogger logger, int count);

    /// <summary>
    ///     Logs that a scene object was created in the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="objectName">The name of the created scene object.</param>
    /// <param name="objectId">The unique identifier of the created scene object.</param>
    [LoggerMessage(
        EventId = 5208,
        Level = LogLevel.Debug,
        Message = "Created scene object: {ObjectName} (ID: {ObjectId})")]
    public static partial void LogCreatedSceneObject(this ILogger logger, string objectName, Guid objectId);

    /// <summary>
    ///     Logs that an existing scene object was updated in the database.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="objectName">The name of the updated scene object.</param>
    /// <param name="objectId">The unique identifier of the updated scene object.</param>
    [LoggerMessage(
        EventId = 5209,
        Level = LogLevel.Debug,
        Message = "Updated scene object: {ObjectName} (ID: {ObjectId})")]
    public static partial void LogUpdatedSceneObject(this ILogger logger, string objectName, Guid objectId);

    /// <summary>
    ///     Logs an error when syncing a specific scene object to the database fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the sync failure.</param>
    /// <param name="objectName">The name of the scene object that failed to sync.</param>
    /// <param name="objectId">The unique identifier of the scene object that failed to sync.</param>
    [LoggerMessage(
        EventId = 5210,
        Level = LogLevel.Error,
        Message = "Failed to sync scene object: {ObjectName} (ID: {ObjectId})")]
    public static partial void LogSceneObjectSyncFailed(
        this ILogger logger,
        Exception ex,
        string objectName,
        Guid objectId);

    /// <summary>
    ///     Logs a summary of the scene loading result, including counts of position tags
    ///     and scene objects that were created and updated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="positionTagCount">The total number of position tags in the configuration.</param>
    /// <param name="created">The number of position tags that were newly created.</param>
    /// <param name="updated">The number of position tags that were updated.</param>
    /// <param name="sceneObjectCount">The total number of scene objects in the configuration.</param>
    /// <param name="objCreated">The number of scene objects that were newly created.</param>
    /// <param name="objUpdated">The number of scene objects that were updated.</param>
    [LoggerMessage(
        EventId = 5211,
        Level = LogLevel.Information,
        Message =
            "Scene loaded: {PositionTagCount} position tags ({Created} created, {Updated} updated), {SceneObjectCount} objects ({ObjCreated} created, {ObjUpdated} updated)")]
    public static partial void LogSceneLoaded(
        this ILogger logger,
        int positionTagCount,
        int created,
        int updated,
        int sceneObjectCount,
        int objCreated,
        int objUpdated);

    /// <summary>
    ///     Logs an error when the scene configuration file cannot be found on disk.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The FileNotFoundException that was thrown.</param>
    /// <param name="configurationFile">The path to the missing configuration file.</param>
    [LoggerMessage(
        EventId = 5212,
        Level = LogLevel.Error,
        Message = "Scene configuration file not found: {ConfigurationFile}")]
    public static partial void LogSceneConfigFileNotFound(this ILogger logger, Exception ex, string configurationFile);

    /// <summary>
    ///     Logs an error when the overall scene initialization process fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the initialization failure.</param>
    [LoggerMessage(
        EventId = 5213,
        Level = LogLevel.Error,
        Message = "Failed to initialize scene from configuration file")]
    public static partial void LogSceneInitFailed(this ILogger logger, Exception ex);

    // ──────────────────────────────────────────────────
    //  SkillsInitializationService — Skill loading
    // ──────────────────────────────────────────────────

    /// <summary>
    ///     Logs that skills auto-load is disabled in configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5300,
        Level = LogLevel.Debug,
        Message = "Skills auto-load is disabled")]
    public static partial void LogSkillsAutoLoadDisabled(this ILogger logger);

    /// <summary>
    ///     Logs a warning when no skills configuration file path is specified.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    [LoggerMessage(
        EventId = 5301,
        Level = LogLevel.Warning,
        Message = "No skills configuration file specified")]
    public static partial void LogNoSkillsConfigFile(this ILogger logger);

    /// <summary>
    ///     Logs that skill definitions are being loaded from the specified configuration file.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationFile">The path to the skills configuration file.</param>
    [LoggerMessage(
        EventId = 5302,
        Level = LogLevel.Debug,
        Message = "Loading skill definitions from {ConfigurationFile}")]
    public static partial void LogLoadingSkillDefs(this ILogger logger, string configurationFile);

    /// <summary>
    ///     Logs that a single skill definition was loaded from configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillName">The name of the loaded skill definition.</param>
    /// <param name="skillId">The unique identifier of the loaded skill definition.</param>
    [LoggerMessage(
        EventId = 5303,
        Level = LogLevel.Debug,
        Message = "Loaded skill definition: {SkillName} (ID: {SkillId})")]
    public static partial void LogLoadedSkillDef(this ILogger logger, string skillName, Guid skillId);

    /// <summary>
    ///     Logs a summary of how many skill definitions were loaded.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="skillDefinitionCount">The total number of skill definitions loaded.</param>
    [LoggerMessage(
        EventId = 5304,
        Level = LogLevel.Information,
        Message = "Skills loaded: {SkillDefinitionCount} definitions")]
    public static partial void LogSkillsLoaded(this ILogger logger, int skillDefinitionCount);

    /// <summary>
    ///     Logs an error when the skills configuration file cannot be found on disk.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The FileNotFoundException that was thrown.</param>
    /// <param name="configurationFile">The path to the missing configuration file.</param>
    [LoggerMessage(
        EventId = 5305,
        Level = LogLevel.Error,
        Message = "Skills configuration file not found: {ConfigurationFile}")]
    public static partial void LogSkillsConfigFileNotFound(
        this ILogger logger,
        Exception ex,
        string configurationFile);

    /// <summary>
    ///     Logs an error when the overall skills initialization process fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="ex">The exception that caused the initialization failure.</param>
    [LoggerMessage(
        EventId = 5306,
        Level = LogLevel.Error,
        Message = "Failed to initialize skills from configuration file")]
    public static partial void LogSkillsInitFailed(this ILogger logger, Exception ex);
}