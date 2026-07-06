using System.Globalization;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using BooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using StringType = FHOOE.Freydis.Domain.Entities.Common.StringType;

namespace FHOOE.Freydis.GraphQLServer.Services;

/// <summary>
///     Provides structured logging for GraphQL mutation operations using high-performance source-generated logging.
/// </summary>
public static partial class GraphQlMutationLogger
{
    /// <summary>
    ///     Formats skill properties into a compact string representation for logging.
    /// </summary>
    /// <param name="properties">The list of skill properties.</param>
    /// <returns>A formatted string of properties, or null if empty.</returns>
    public static string? FormatProperties(List<TypedProperty>? properties)
    {
        if (properties == null || properties.Count == 0)
            return null;

        var formattedProps = properties.Select(p =>
        {
            var value = p.Value.Type switch
            {
                BooleanType when p.Value.Value is bool b => b.ToString(),
                NumberType when p.Value.Value is double d => d.ToString("F2", CultureInfo.InvariantCulture),
                StringType when p.Value.Value is string s => $"\"{s}\"",
                PositionType when p.Value.Value is Position pos =>
                    string.Format(CultureInfo.InvariantCulture,
                        "(X={0:F2},Y={1:F2},Z={2:F2},Alpha={3:F2},Beta={4:F2},Gamma={5:F2})",
                        pos.X, pos.Y, pos.Z, pos.Alpha, pos.Beta, pos.Gamma),
                PositionTagType when p.Value.Value is PositionTag pt => pt.Tag,
                SceneObjectType when p.Value.Value is SceneObject so => so.Name,
                _ => p.Value.Type.GetType().Name
            };
            return $"{p.Name}={value}";
        });

        return string.Join(", ", formattedProps);
    }

    /// <summary>
    ///     Logs detailed information about a node operation (create, update, etc.) at Debug level.
    ///     Dispatches to the appropriate logging call based on the node type, eliminating
    ///     duplicate switch statements across mutation methods.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">The operation name (e.g., "CREATE_NODE", "UPDATE_NODE").</param>
    /// <param name="node">The node to log details for.</param>
    public static void LogNodeOperationDetails(this ILogger logger, string operation, Node node)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        switch (node)
        {
            case TaskNode t:
                logger.LogNodeDetails(
                    operation, t.Id, "TaskNode", t.Task.Name, t.Task.Duration, t.Task.StartTime,
                    t.ParentId, t.Position.X, t.Position.Y);
                break;

            case SkillExecutionNode s:
                logger.LogNodeDetails(
                    operation, s.Id, "SkillExecutionNode", s.SkillExecutionTask.Skill.Name,
                    s.SkillExecutionTask.Duration, s.SkillExecutionTask.StartTime,
                    s.ParentId, s.Position.X, s.Position.Y, s.SkillExecutionTask.AgentId,
                    FormatProperties(s.SkillExecutionTask.Skill.Properties));
                break;

            case RouterNode r:
                logger.LogNodeDetails(
                    operation, r.Id, "RouterNode", r.RouterTask.Name, r.RouterTask.Duration,
                    r.RouterTask.StartTime, r.ParentId, r.Position.X, r.Position.Y);
                break;

            default:
                throw new NotSupportedException($"Unknown node type: {node.GetType().Name}");
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "GRAPHQL_MUTATION | Operation={Operation} | NodeId={NodeId} | NodeType={NodeType} | Name={Name} | Duration={Duration:F2}ms | StartTime={StartTime:F2}ms | ParentId={ParentId} | Position=({PositionX:F2},{PositionY:F2}) | AgentId={AgentId} | Properties={Properties}")]
    private static partial void LogNodeDetails(
        this ILogger logger,
        string operation,
        Guid nodeId,
        string nodeType,
        string name,
        double duration,
        double startTime,
        Guid? parentId,
        double positionX,
        double positionY,
        Guid? agentId = null,
        string? properties = null);
}