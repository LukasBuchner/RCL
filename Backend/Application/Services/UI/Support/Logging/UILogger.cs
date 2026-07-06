using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.UI.Support.Logging;

/// <summary>
///     Provides structured logging for UI positioning and visibility operations using
///     high-performance source-generated logging.
/// </summary>
public static partial class UiLogger
{
    // --- NodeWidthCalculator ---

    /// <summary>
    ///     Logs the start of a width calculation pass for all nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes to calculate widths for.</param>
    /// <param name="scale">The time-to-pixel scale factor being used.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Calculating widths for {NodeCount} nodes with TimeToPixelScale={Scale}")]
    public static partial void LogWidthCalculationStarted(
        this ILogger logger,
        int nodeCount,
        double scale);

    /// <summary>
    ///     Logs the calculated width for an individual node based on its duration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="nodeType">The CLR type name of the node.</param>
    /// <param name="duration">The duration value used for the width calculation.</param>
    /// <param name="width">The resulting calculated width in pixels.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Node {NodeId} ({NodeType}): duration={Duration:F2}, width={Width:F2}")]
    public static partial void LogNodeWidthCalculated(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double duration,
        double width);

    /// <summary>
    ///     Logs that a container node has been expanded to fit its widest descendant.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the container node.</param>
    /// <param name="nodeType">The CLR type name of the container node.</param>
    /// <param name="oldWidth">The original duration-based width before expansion.</param>
    /// <param name="newWidth">The expanded width that accommodates children.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Container {NodeId} ({NodeType}) expanded from {OldWidth:F2} to {NewWidth:F2} to fit children")]
    public static partial void LogContainerWidthExpanded(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double oldWidth,
        double newWidth);

    /// <summary>
    ///     Logs the completion of width calculation for all nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="widthCount">The number of node widths that were calculated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Width calculation completed: {WidthCount} widths calculated")]
    public static partial void LogWidthCalculationCompleted(
        this ILogger logger,
        int widthCount);

    // --- NodeHeightCalculator ---

    /// <summary>
    ///     Logs the start of a height calculation pass for all nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">The total number of nodes to calculate heights for.</param>
    /// <param name="mappingCount">The number of parent-child relationships in the hierarchy.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Calculating heights for {NodeCount} nodes with {MappingCount} parent-child relationships")]
    public static partial void LogHeightCalculationStarted(
        this ILogger logger,
        int nodeCount,
        int mappingCount);

    /// <summary>
    ///     Logs the calculated height for a container node based on its children.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeType">The CLR type name of the container node.</param>
    /// <param name="nodeId">The unique identifier of the container node.</param>
    /// <param name="height">The calculated container height in pixels.</param>
    /// <param name="childCount">The number of direct children in the container.</param>
    /// <param name="parentId">The parent node identifier, or null for root containers.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_HEIGHT_CALC | Container {NodeType} {NodeId} height: {Height:F2} ({ChildCount} children, ParentId={ParentId})")]
    public static partial void LogContainerHeightCalculated(
        this ILogger logger,
        string nodeType,
        Guid nodeId,
        double height,
        int childCount,
        Guid? parentId);

    /// <summary>
    ///     Logs the base height assigned to a leaf node.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the leaf node.</param>
    /// <param name="nodeType">The CLR type name of the leaf node.</param>
    /// <param name="baseHeight">The base height assigned to the leaf node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Processing leaf node {NodeId}, Type: {NodeType}, BaseHeight: {BaseHeight}")]
    public static partial void LogLeafNodeHeight(
        this ILogger logger,
        Guid nodeId,
        string nodeType,
        double baseHeight);

    /// <summary>
    ///     Logs the height calculation for a leaf RouterNode that includes a dropdown for branches.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the router node.</param>
    /// <param name="branchCount">The number of branches in the router.</param>
    /// <param name="baseHeight">The base height before adding the dropdown.</param>
    /// <param name="dropdownHeight">The additional height for the branch dropdown.</param>
    /// <param name="totalHeight">The total calculated height including the dropdown.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message =
            "NODE_HEIGHT_CALC | Leaf RouterNode {NodeId} with {BranchCount} branches: base {BaseHeight:F2} + dropdown {DropdownHeight:F2} = {TotalHeight:F2}")]
    public static partial void LogLeafRouterNodeWithBranches(
        this ILogger logger,
        Guid nodeId,
        int branchCount,
        double baseHeight,
        double dropdownHeight,
        double totalHeight);

    /// <summary>
    ///     Logs the height calculation for a leaf RouterNode that has no branches.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The unique identifier of the router node.</param>
    /// <param name="baseHeight">The base height assigned to the branchless router node.</param>
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "NODE_HEIGHT_CALC | Leaf RouterNode {NodeId} has no branches, using base height {BaseHeight:F2}")]
    public static partial void LogLeafRouterNodeNoBranches(
        this ILogger logger,
        Guid nodeId,
        double baseHeight);

    /// <summary>
    ///     Logs the completion of height calculation for all nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="heightCount">The number of node heights that were calculated.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Height calculation completed: {HeightCount} heights calculated")]
    public static partial void LogHeightCalculationCompleted(
        this ILogger logger,
        int heightCount);

    // --- NodeHidingService ---

    /// <summary>
    ///     Logs the result of applying hidden state to nodes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="hiddenCount">The number of nodes that were hidden.</param>
    /// <param name="visibleCount">The number of nodes that remain visible.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Applied hidden state: {HiddenCount} nodes hidden, {VisibleCount} nodes visible")]
    public static partial void LogHiddenStateApplied(
        this ILogger logger,
        int hiddenCount,
        int visibleCount);
}