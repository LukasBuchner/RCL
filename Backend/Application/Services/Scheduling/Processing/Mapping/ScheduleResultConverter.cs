using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Implementation of schedule result converter.
///     Converts timing calculation results to schedule results.
/// </summary>
public partial class ScheduleResultConverter : IScheduleResultConverter
{
    private readonly ILogger<ScheduleResultConverter> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScheduleResultConverter" /> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ScheduleResultConverter(ILogger<ScheduleResultConverter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ScheduleResult ConvertTimingToScheduleResult(TimingResult timingResult,
        IReadOnlyList<Node> nodesWithPositions)
    {
        ArgumentNullException.ThrowIfNull(timingResult);
        ArgumentNullException.ThrowIfNull(nodesWithPositions);

        var nodeSchedules = new List<NodeSchedule>();

        if (timingResult.DetailedTimingInfo == null)
            return new ScheduleResult
            {
                Success = true,
                NodeSchedules = nodeSchedules.AsReadOnly(),
                ErrorMessage = null,
                UpdatedNodes = nodesWithPositions
            };

        foreach (var (nodeId, timingInfo) in timingResult.DetailedTimingInfo)
        {
            var originalNode = nodesWithPositions.FirstOrDefault(n => n.Id == nodeId);
            if (originalNode is null)
                LogUnmatchedTimingEntry(_logger, nodeId, timingInfo.NodeType);
            var nodeSchedule = new NodeSchedule
            {
                NodeId = nodeId,
                Duration = timingInfo.Duration,
                AbsoluteStartTime = timingInfo.AbsoluteStartTime,
                AbsoluteFinishTime = timingInfo.AbsoluteFinishTime,
                RelativeStartTime = timingInfo.RelativeStartTime,
                RelativeFinishTime = timingInfo.RelativeFinishTime,
                NodeType = ConvertToScheduleNodeType(timingInfo.NodeType),
                ParentNodeId = GetParentNodeId(originalNode)
            };
            nodeSchedules.Add(nodeSchedule);
        }

        return new ScheduleResult
        {
            Success = true,
            NodeSchedules = nodeSchedules.AsReadOnly(),
            ErrorMessage = null,
            UpdatedNodes = nodesWithPositions
        };
    }

    /// <summary>
    ///     Converts NodeTimingType to NodeScheduleType.
    /// </summary>
    private static NodeScheduleType ConvertToScheduleNodeType(NodeTimingType timingType)
    {
        return timingType switch
        {
            NodeTimingType.SkillExecution => NodeScheduleType.SkillExecutionNode,
            NodeTimingType.Task => NodeScheduleType.TaskNode,
            NodeTimingType.Router => NodeScheduleType.RouterNode,
            NodeTimingType.Original => NodeScheduleType.TaskNode, // Assume original timings are for task nodes
            _ => NodeScheduleType.TaskNode
        };
    }

    /// <summary>
    ///     Gets the parent node ID for a given node.
    /// </summary>
    private static Guid? GetParentNodeId(Node? node)
    {
        return node switch
        {
            SkillExecutionNode skillNode => skillNode.ParentId,
            TaskNode taskNode => taskNode.ParentId,
            RouterNode routerNode => routerNode.ParentId,
            _ => null
        };
    }

    /// <summary>
    ///     Logs at Warning level that a detailed-timing entry references a node id absent from the positioned
    ///     node list, so the resulting schedule entry has a null parent id.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeId">The timing entry's node identifier.</param>
    /// <param name="nodeType">The timing node type recorded on the entry.</param>
    [LoggerMessage(
        LogLevel.Warning,
        "SCHEDULE_CONVERT | Timing entry NodeId={NodeId} ({NodeType}) has no matching node; ParentNodeId will be null")]
    private static partial void LogUnmatchedTimingEntry(ILogger logger, Guid nodeId, NodeTimingType nodeType);
}