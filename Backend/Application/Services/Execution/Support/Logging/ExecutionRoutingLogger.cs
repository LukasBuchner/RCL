using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Provides high-performance source-generated logging for router evaluation
///     operations during procedure execution.
/// </summary>
public static partial class ExecutionRoutingLogger
{
    /// <summary>
    ///     Logs the start of router evaluation with the number of branches.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router being evaluated.</param>
    /// <param name="routerId">The unique identifier of the router.</param>
    /// <param name="branchCount">The number of branches configured on the router.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Evaluating router '{RouterName}' (ID: {RouterId}) with {BranchCount} branches")]
    public static partial void LogEvaluatingRouter(
        this ILogger logger,
        string routerName,
        Guid routerId,
        int branchCount);

    /// <summary>
    ///     Logs the selector details of a router for debugging purposes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="selectorType">The type name of the selector, or "(null)" if none.</param>
    /// <param name="expression">The selector expression, or "(null/empty)" if none.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router '{RouterName}' Selector: Type={SelectorType}, Expression='{Expression}'")]
    public static partial void LogRouterSelectorDetails(
        this ILogger logger,
        string routerName,
        string selectorType,
        string expression);

    /// <summary>
    ///     Logs details of a single branch on a router for debugging purposes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="priority">The priority of the branch.</param>
    /// <param name="condition">The branch condition expression, or "(null)" if none.</param>
    /// <param name="targetId">The target node ID of the branch.</param>
    /// <param name="isDefault">Whether this is the default branch.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Router '{RouterName}' Branch: Name='{BranchName}', Priority={Priority}, Condition='{Condition}', TargetNodeId={TargetId}, IsDefault={IsDefault}")]
    public static partial void LogRouterBranchDetails(
        this ILogger logger,
        string routerName,
        string branchName,
        int priority,
        string condition,
        Guid? targetId,
        bool isDefault);

    /// <summary>
    ///     Logs the variable context available to a router for branch evaluation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="variables">A formatted string of variable name-value pairs.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Router '{RouterName}' Variable context: [{Variables}]")]
    public static partial void LogRouterVariableContext(
        this ILogger logger,
        string routerName,
        string variables);

    /// <summary>
    ///     Logs that a selected branch has no target node configured.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="branchName">The name of the branch with no target.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Router '{RouterName}' selected branch '{BranchName}' has no target node configured")]
    public static partial void LogBranchNoTargetNode(
        this ILogger logger,
        string routerName,
        string branchName);

    /// <summary>
    ///     Logs the successful selection of a branch and its target node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="branchName">The name of the selected branch.</param>
    /// <param name="targetNodeId">The unique identifier of the target node.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Router '{RouterName}' selected branch '{BranchName}' → Target Node: {TargetNodeId}")]
    public static partial void LogBranchSelected(
        this ILogger logger,
        string routerName,
        string branchName,
        Guid targetNodeId);

    /// <summary>
    ///     Logs that the router selection is kept in-memory only and not persisted during execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="routerName">The name of the router.</param>
    /// <param name="branchName">The name of the selected branch.</param>
    /// <param name="selectedAt">The UTC timestamp when the selection was made.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Router '{RouterName}' selected '{BranchName}' at {SelectedAt} (in-memory only, not persisted during execution)")]
    public static partial void LogBranchSelectionInMemory(
        this ILogger logger,
        string routerName,
        string branchName,
        DateTime selectedAt);
}