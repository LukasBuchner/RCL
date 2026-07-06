using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;

/// <summary>
///     Provides structured logging for entity management operations using high-performance source-generated logging.
/// </summary>
public static partial class EntityManagementLogger
{
    /// <summary>
    ///     Logs the start of an entity creation operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity being created.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityName">The name of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Creating {EntityType} entity {EntityId} ({EntityName})")]
    public static partial void LogCreateStart(
        this ILogger logger,
        string entityType,
        Guid entityId,
        string entityName);

    /// <summary>
    ///     Logs successful completion of an entity creation operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that was created.</param>
    /// <param name="entityId">The unique identifier of the created entity.</param>
    /// <param name="entityName">The name of the created entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created {EntityType} {EntityName} ({EntityId})")]
    public static partial void LogCreateSuccess(
        this ILogger logger,
        string entityType,
        Guid entityId,
        string entityName);

    /// <summary>
    ///     Logs the start of an entity update operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity being updated.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityName">The name of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updating {EntityType} entity {EntityId} ({EntityName})")]
    public static partial void LogUpdateStart(
        this ILogger logger,
        string entityType,
        Guid entityId,
        string entityName);

    /// <summary>
    ///     Logs successful completion of an entity update operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that was updated.</param>
    /// <param name="entityId">The unique identifier of the updated entity.</param>
    /// <param name="entityName">The name of the updated entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Updated {EntityType} {EntityName} ({EntityId})")]
    public static partial void LogUpdateSuccess(
        this ILogger logger,
        string entityType,
        Guid entityId,
        string entityName);

    /// <summary>
    ///     Logs failure of an entity update operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that failed to update.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityName">The name of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to update {EntityType} {EntityName} ({EntityId})")]
    public static partial void LogUpdateFailed(
        this ILogger logger,
        string entityType,
        Guid entityId,
        string entityName);

    /// <summary>
    ///     Logs the start of an entity deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity being deleted.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleting {EntityType} entity {EntityId}")]
    public static partial void LogDeleteStart(
        this ILogger logger,
        string entityType,
        Guid entityId);

    /// <summary>
    ///     Logs successful completion of an entity deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that was deleted.</param>
    /// <param name="entityId">The unique identifier of the deleted entity.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleted {EntityType} ({EntityId})")]
    public static partial void LogDeleteSuccess(
        this ILogger logger,
        string entityType,
        Guid entityId);

    /// <summary>
    ///     Logs failure of an entity deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that failed to delete.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete {EntityType} ({EntityId})")]
    public static partial void LogDeleteFailed(
        this ILogger logger,
        string entityType,
        Guid entityId);

    /// <summary>
    ///     Logs a get all entities read operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entities being retrieved.</param>
    /// <param name="count">The number of entities retrieved.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Retrieved {Count} {EntityType} entities")]
    public static partial void LogGetAll(
        this ILogger logger,
        string entityType,
        int count);

    /// <summary>
    ///     Logs a get entity by ID read operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity being retrieved.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Retrieving {EntityType} entity {EntityId}")]
    public static partial void LogGetById(
        this ILogger logger,
        string entityType,
        Guid entityId);

    /// <summary>
    ///     Logs a successful entity change notification.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity for which the notification was sent.</param>
    /// <param name="count">The number of entities in the notification.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Sent {EntityType} change notification with {Count} entities")]
    public static partial void LogNotificationSent(
        this ILogger logger,
        string entityType,
        int count);

    /// <summary>
    ///     Logs a failed entity change notification.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity for which the notification failed.</param>
    /// <param name="exception">The exception that caused the notification failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to send {EntityType} change notification")]
    public static partial void LogNotificationFailed(
        this ILogger logger,
        string entityType,
        Exception exception);

    /// <summary>
    ///     Logs a bulk entity creation summary for startup/initialization scenarios.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entities that were created.</param>
    /// <param name="count">The number of entities created.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Initialized {Count} {EntityType} entities")]
    public static partial void LogBulkCreateSummary(
        this ILogger logger,
        int count,
        string entityType);

    /// <summary>
    ///     Logs a significant entity state change that operators should be aware of.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type of entity that changed state.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="oldState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{EntityType} {EntityName} changed from {OldState} to {NewState}")]
    public static partial void LogStateChange(
        this ILogger logger,
        string entityType,
        string entityName,
        string oldState,
        string newState);

    // ── Node Operations ─────────────────────────────────────────────────

    /// <summary>
    ///     Logs that a branch TargetNodeId is being preserved during a RouterNode update
    ///     because the incoming update did not include it.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetNodeId">The TargetNodeId being preserved.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="routerNodeId">The ID of the RouterNode being updated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Preserving TargetNodeId {TargetNodeId} for branch '{BranchName}' on RouterNode {RouterNodeId}")]
    public static partial void LogPreservingBranchTargetNodeId(
        this ILogger logger,
        Guid targetNodeId,
        string branchName,
        Guid routerNodeId);

    /// <summary>
    ///     Logs that a skill has no output properties and variable creation is being skipped.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillName">The name of the skill with no output properties.</param>
    /// <param name="nodeId">The ID of the node that was created.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skill '{SkillName}' has no output properties, skipping variable creation for node {NodeId}")]
    public static partial void LogNoOutputProperties(
        this ILogger logger,
        string skillName,
        Guid nodeId);

    /// <summary>
    ///     Logs the start of automatic procedure variable creation for skill output properties.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of output properties to create variables for.</param>
    /// <param name="skillName">The name of the skill whose outputs are being mapped to variables.</param>
    /// <param name="nodeId">The ID of the node that triggered variable creation.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating {Count} procedure variables for output properties of skill '{SkillName}' on node {NodeId}")]
    public static partial void LogCreatingOutputVariables(
        this ILogger logger,
        int count,
        string skillName,
        Guid nodeId);

    /// <summary>
    ///     Logs that a procedure variable was successfully created for a skill output property.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the created variable.</param>
    /// <param name="typeName">The type of the created variable.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Created procedure variable '{VariableName}' of type '{TypeName}' for skill output property")]
    public static partial void LogOutputVariableCreated(
        this ILogger logger,
        string variableName,
        string typeName);

    /// <summary>
    ///     Logs that a variable already exists and was skipped during output variable creation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the existing variable.</param>
    /// <param name="procedureId">The ID of the procedure containing the variable.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Variable '{VariableName}' already exists in procedure {ProcedureId}, skipping creation")]
    public static partial void LogOutputVariableAlreadyExists(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs a failure to create a variable for a skill output property.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable that failed to create.</param>
    /// <param name="procedureId">The ID of the procedure in which creation was attempted.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to create variable '{VariableName}' for skill output property in procedure {ProcedureId}")]
    public static partial void LogOutputVariableCreationFailed(
        this ILogger logger,
        string variableName,
        Guid procedureId,
        Exception exception);

    /// <summary>
    ///     Logs that a RouterNode has no branches and branch TaskNode creation is being skipped.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The ID of the RouterNode.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "RouterNode {NodeId} has no branches, skipping branch TaskNode creation")]
    public static partial void LogNoBranches(
        this ILogger logger,
        Guid nodeId);

    /// <summary>
    ///     Logs the start of automatic branch TaskNode creation for a RouterNode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of branch TaskNodes to create.</param>
    /// <param name="nodeId">The ID of the RouterNode.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating {Count} branch TaskNodes for RouterNode {NodeId}")]
    public static partial void LogCreatingBranchTaskNodes(
        this ILogger logger,
        int count,
        Guid nodeId);

    /// <summary>
    ///     Logs successful creation of a branch TaskNode for a specific branch on a RouterNode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="taskNodeId">The ID of the created TaskNode.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="routerNodeId">The ID of the parent RouterNode.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Created branch TaskNode {TaskNodeId} for branch '{BranchName}' on RouterNode {RouterNodeId}")]
    public static partial void LogBranchTaskNodeCreated(
        this ILogger logger,
        Guid taskNodeId,
        string branchName,
        Guid routerNodeId);

    /// <summary>
    ///     Logs a failure to create a branch TaskNode for a specific branch on a RouterNode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the branch that failed.</param>
    /// <param name="routerNodeId">The ID of the parent RouterNode.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create branch TaskNode for branch '{BranchName}' on RouterNode {RouterNodeId}")]
    public static partial void LogBranchTaskNodeCreationFailed(
        this ILogger logger,
        string branchName,
        Guid routerNodeId,
        Exception exception);

    /// <summary>
    ///     Logs that the first branch was automatically selected for a RouterNode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="branchName">The name of the auto-selected branch.</param>
    /// <param name="routerNodeId">The ID of the RouterNode.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto-selected first branch '{BranchName}' for RouterNode {RouterNodeId}")]
    public static partial void LogAutoSelectedBranch(
        this ILogger logger,
        string? branchName,
        Guid routerNodeId);

    /// <summary>
    ///     Logs successful creation and linking of all branch TaskNodes for a RouterNode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of branch TaskNodes created and linked.</param>
    /// <param name="nodeId">The ID of the RouterNode.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully created and linked {Count} branch TaskNodes for RouterNode {NodeId}")]
    public static partial void LogBranchTaskNodesLinked(
        this ILogger logger,
        int count,
        Guid nodeId);

    /// <summary>
    ///     Logs the start of variable removal for a deleted skill node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of variables to remove.</param>
    /// <param name="skillName">The name of the skill whose variables are being removed.</param>
    /// <param name="nodeId">The ID of the node being deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Removing {Count} procedure variables for skill '{SkillName}' on deleted node {NodeId}")]
    public static partial void LogRemovingOutputVariables(
        this ILogger logger,
        int count,
        string skillName,
        Guid nodeId);

    /// <summary>
    ///     Logs successful removal of a procedure variable for a deleted skill node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the removed variable.</param>
    /// <param name="nodeId">The ID of the deleted node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Removed procedure variable '{VariableName}' for deleted skill node {NodeId}")]
    public static partial void LogOutputVariableRemoved(
        this ILogger logger,
        string variableName,
        Guid nodeId);

    /// <summary>
    ///     Logs that a variable was already removed before node deletion cleanup reached it.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable.</param>
    /// <param name="nodeId">The ID of the deleted node.</param>
    /// <param name="message">The exception message describing why the variable was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Variable '{VariableName}' was already removed for node {NodeId}: {Message}")]
    public static partial void LogOutputVariableAlreadyRemoved(
        this ILogger logger,
        string variableName,
        Guid nodeId,
        string message);

    /// <summary>
    ///     Logs a failure to remove a variable for a deleted skill node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable that failed to remove.</param>
    /// <param name="nodeId">The ID of the deleted node.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to remove variable '{VariableName}' for deleted skill node {NodeId}")]
    public static partial void LogOutputVariableRemovalFailed(
        this ILogger logger,
        string variableName,
        Guid nodeId,
        Exception exception);

    // ── Procedure Operations ────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of a procedure load operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loading procedure {ProcedureId}")]
    public static partial void LogProcedureLoadStart(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that the requested procedure is already loaded (idempotent call).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the already-loaded procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Procedure {ProcedureId} is already loaded")]
    public static partial void LogProcedureAlreadyLoaded(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that a currently loaded procedure is being unloaded before loading a new one.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="currentProcedureId">The ID of the procedure being unloaded.</param>
    /// <param name="newProcedureId">The ID of the new procedure being loaded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Unloading currently loaded procedure {CurrentProcedureId} before loading {NewProcedureId}")]
    public static partial void LogProcedureSwitch(
        this ILogger logger,
        Guid currentProcedureId,
        Guid newProcedureId);

    /// <summary>
    ///     Logs successful completion of a procedure load operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the loaded procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully loaded procedure {ProcedureId}")]
    public static partial void LogProcedureLoadSuccess(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that no procedure is currently loaded during an unload attempt.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No procedure is currently loaded")]
    public static partial void LogNoProcedureLoaded(
        this ILogger logger);

    /// <summary>
    ///     Logs the start of a procedure unload operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure being unloaded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Unloading procedure {ProcedureId}")]
    public static partial void LogProcedureUnloadStart(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs successful completion of a procedure unload operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the unloaded procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully unloaded procedure {ProcedureId}")]
    public static partial void LogProcedureUnloadSuccess(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs the start of a procedure creation operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the procedure being created.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating new procedure with name '{Name}'")]
    public static partial void LogProcedureCreateStart(
        this ILogger logger,
        string name);

    /// <summary>
    ///     Logs successful completion of a procedure creation operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the created procedure.</param>
    /// <param name="name">The name of the created procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully created procedure {ProcedureId} with name '{Name}'")]
    public static partial void LogProcedureCreateSuccess(
        this ILogger logger,
        Guid procedureId,
        string name);

    /// <summary>
    ///     Logs the start of a procedure deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure being deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleting procedure {ProcedureId}")]
    public static partial void LogProcedureDeleteStart(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that a procedure was not found during a delete attempt.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure that was not found.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Procedure {ProcedureId} not found, cannot delete")]
    public static partial void LogProcedureNotFound(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that a procedure is being unloaded before deletion.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure being unloaded before deletion.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Unloading procedure {ProcedureId} before deletion")]
    public static partial void LogProcedureUnloadBeforeDelete(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs that a procedure and its associated nodes and edges are being cascade-deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure being cascade-deleted.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleting procedure {ProcedureId} (nodes and edges cascade)")]
    public static partial void LogProcedureCascadeDelete(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs successful completion of a procedure deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the deleted procedure.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully deleted procedure {ProcedureId}")]
    public static partial void LogProcedureDeleteSuccess(
        this ILogger logger,
        Guid procedureId);

    /// <summary>
    ///     Logs failure of a procedure deletion operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="procedureId">The ID of the procedure that failed to delete.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete procedure {ProcedureId}")]
    public static partial void LogProcedureDeleteFailed(
        this ILogger logger,
        Guid procedureId);

    // ── Variable Operations ─────────────────────────────────────────────

    /// <summary>
    ///     Logs the start of adding a variable to a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being added.</param>
    /// <param name="procedureId">The ID of the procedure receiving the variable.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Adding variable '{VariableName}' to procedure {ProcedureId}")]
    public static partial void LogVariableAddStart(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs successful addition of a variable to a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the added variable.</param>
    /// <param name="procedureId">The ID of the procedure containing the variable.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully added variable '{VariableName}' to procedure {ProcedureId}")]
    public static partial void LogVariableAddSuccess(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs the start of updating a variable in a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being updated.</param>
    /// <param name="procedureId">The ID of the procedure containing the variable.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Updating variable '{VariableName}' in procedure {ProcedureId}")]
    public static partial void LogVariableUpdateStart(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs successful update of a variable in a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the updated variable.</param>
    /// <param name="procedureId">The ID of the procedure containing the variable.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully updated variable '{VariableName}' in procedure {ProcedureId}")]
    public static partial void LogVariableUpdateSuccess(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs the start of removing a variable from a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the variable being removed.</param>
    /// <param name="procedureId">The ID of the procedure from which the variable is being removed.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removing variable '{VariableName}' from procedure {ProcedureId}")]
    public static partial void LogVariableRemoveStart(
        this ILogger logger,
        string variableName,
        Guid procedureId);

    /// <summary>
    ///     Logs successful removal of a variable from a procedure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="variableName">The name of the removed variable.</param>
    /// <param name="procedureId">The ID of the procedure from which the variable was removed.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Successfully removed variable '{VariableName}' from procedure {ProcedureId}")]
    public static partial void LogVariableRemoveSuccess(
        this ILogger logger,
        string variableName,
        Guid procedureId);
}