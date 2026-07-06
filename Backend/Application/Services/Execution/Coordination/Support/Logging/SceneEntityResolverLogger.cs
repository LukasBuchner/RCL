using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Coordination.Support.Logging;

/// <summary>
///     Provides structured logging for the <see cref="SceneEntityResolver" /> using
///     high-performance source-generated logging.
/// </summary>
public static partial class SceneEntityResolverLogger
{
    /// <summary>
    ///     Logs the initial cache state after construction.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tagCount">Number of position tags in the cache.</param>
    /// <param name="objectCount">Number of scene objects in the cache.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SceneEntityResolver initialized: {TagCount} position tags, {ObjectCount} scene objects")]
    public static partial void LogResolverInitialized(
        this ILogger logger,
        int tagCount,
        int objectCount);

    /// <summary>
    ///     Logs that a stale PositionTag property was refreshed with current data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tagName">The human-readable tag name.</param>
    /// <param name="tagId">The unique identifier of the position tag.</param>
    /// <param name="oldX">Previous X coordinate.</param>
    /// <param name="oldY">Previous Y coordinate.</param>
    /// <param name="oldZ">Previous Z coordinate.</param>
    /// <param name="newX">Current X coordinate.</param>
    /// <param name="newY">Current Y coordinate.</param>
    /// <param name="newZ">Current Z coordinate.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Refreshed stale PositionTag '{TagName}' ({TagId}): ({OldX:F3},{OldY:F3},{OldZ:F3}) → ({NewX:F3},{NewY:F3},{NewZ:F3})")]
    public static partial void LogPositionTagRefreshed(
        this ILogger logger,
        string tagName,
        Guid tagId,
        double oldX, double oldY, double oldZ,
        double newX, double newY, double newZ);

    /// <summary>
    ///     Logs that a stale SceneObject property was refreshed with current data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="objectName">The scene object name.</param>
    /// <param name="objectId">The unique identifier of the scene object.</param>
    /// <param name="oldX">Previous X coordinate.</param>
    /// <param name="oldY">Previous Y coordinate.</param>
    /// <param name="oldZ">Previous Z coordinate.</param>
    /// <param name="newX">Current X coordinate.</param>
    /// <param name="newY">Current Y coordinate.</param>
    /// <param name="newZ">Current Z coordinate.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Refreshed stale SceneObject '{ObjectName}' ({ObjectId}): ({OldX:F3},{OldY:F3},{OldZ:F3}) → ({NewX:F3},{NewY:F3},{NewZ:F3})")]
    public static partial void LogSceneObjectRefreshed(
        this ILogger logger,
        string objectName,
        Guid objectId,
        double oldX, double oldY, double oldZ,
        double newX, double newY, double newZ);

    /// <summary>
    ///     Logs that a PositionTag referenced by a skill was not found in the live cache;
    ///     the skill executes with the stale snapshot position.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tagId">The unique identifier of the position tag not found in cache.</param>
    /// <param name="staleX">The stale X coordinate from the snapshot.</param>
    /// <param name="staleY">The stale Y coordinate from the snapshot.</param>
    /// <param name="staleZ">The stale Z coordinate from the snapshot.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "PositionTag '{TagId}' referenced by skill not found in live cache; executing with stale snapshot position ({StaleX:F3},{StaleY:F3},{StaleZ:F3})")]
    public static partial void LogPositionTagCacheMiss(
        this ILogger logger,
        Guid tagId,
        double staleX,
        double staleY,
        double staleZ);

    /// <summary>
    ///     Logs that a SceneObject referenced by a skill was not found in the live cache;
    ///     the skill executes with the stale snapshot position and name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="objectId">The unique identifier of the scene object not found in cache.</param>
    /// <param name="staleName">The stale name from the snapshot.</param>
    /// <param name="staleX">The stale X coordinate from the snapshot.</param>
    /// <param name="staleY">The stale Y coordinate from the snapshot.</param>
    /// <param name="staleZ">The stale Z coordinate from the snapshot.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "SceneObject '{ObjectId}' referenced by skill not found in live cache; executing with stale snapshot ('{StaleName}' at {StaleX:F3},{StaleY:F3},{StaleZ:F3})")]
    public static partial void LogSceneObjectCacheMiss(
        this ILogger logger,
        Guid objectId,
        string staleName,
        double staleX,
        double staleY,
        double staleZ);
}