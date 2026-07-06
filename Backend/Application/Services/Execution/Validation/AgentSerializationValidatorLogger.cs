using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Source-generated, high-performance log methods for <see cref="AgentSerializationValidator" />.
///     All messages are structured so that log aggregation tools can filter, group, and alert on
///     individual fields without parsing the message text.
/// </summary>
public static partial class AgentSerializationValidatorLogger
{
    /// <summary>
    ///     Emitted once at the start of each <c>Validate</c> call, recording input sizes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="nodeCount">Total number of nodes in the procedure graph.</param>
    /// <param name="edgeCount">Total number of dependency edges in the procedure graph.</param>
    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | START | NodeCount={NodeCount} | EdgeCount={EdgeCount}")]
    public static partial void LogValidationStart(
        this ILogger logger,
        int nodeCount,
        int edgeCount);

    /// <summary>
    ///     Emitted after grouping, reporting how many agent groups require pairwise checking.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentGroupCount">Number of agents that have 2 or more assigned skills.</param>
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | GROUPS | AgentGroupCount={AgentGroupCount}")]
    public static partial void LogAgentGroupCount(
        this ILogger logger,
        int agentGroupCount);

    /// <summary>
    ///     Emitted after the FS adjacency list is built, indicating how many nodes have outgoing FS edges.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sourceNodeCount">Number of distinct source nodes in the FS adjacency list.</param>
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | FS_ADJACENCY | SourceNodeCount={SourceNodeCount}")]
    public static partial void LogFsAdjacencyBuilt(
        this ILogger logger,
        int sourceNodeCount);

    /// <summary>
    ///     Emitted at the start of group checking, identifying the agent and its skill count.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The unique identifier of the agent being checked.</param>
    /// <param name="skillCount">The number of skills assigned to this agent.</param>
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | CHECK_GROUP | AgentId={AgentId} | SkillCount={SkillCount}")]
    public static partial void LogCheckGroup(
        this ILogger logger,
        Guid agentId,
        int skillCount);

    /// <summary>
    ///     Emitted for each pair that is skipped because the two nodes reside in mutually exclusive router branches.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillA">The ID of the first skill node in the exempt pair.</param>
    /// <param name="skillB">The ID of the second skill node in the exempt pair.</param>
    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | SKIP_EXCLUSIVE | SkillA={SkillA} | SkillB={SkillB}")]
    public static partial void LogSkipMutuallyExclusive(
        this ILogger logger,
        Guid skillA,
        Guid skillB);

    /// <summary>
    ///     Emitted for each pair that fails the FS reachability check, identifying the agent and both skills.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="skillA">The ID of the first skill node in the missing pair.</param>
    /// <param name="skillB">The ID of the second skill node in the missing pair.</param>
    /// <param name="agentId">The agent both skills are assigned to.</param>
    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | MISSING_PAIR | SkillA={SkillA} | SkillB={SkillB} | AgentId={AgentId}")]
    public static partial void LogMissingFsPair(
        this ILogger logger,
        Guid skillA,
        Guid skillB,
        Guid agentId);

    /// <summary>
    ///     Emitted as a warning when an agent group produces at least one serialization violation,
    ///     summarising the offending agent, the number of missing pairs, and the number of involved skills.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="agentId">The agent ID with the violation.</param>
    /// <param name="pairCount">Number of unordered skill pairs that lack an FS chain.</param>
    /// <param name="skillCount">Number of distinct skill nodes involved in at least one missing pair.</param>
    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Warning,
        Message =
            "AGENT_SERIALIZATION | VIOLATION | AgentId={AgentId} | MissingPairCount={PairCount} | InvolvedSkillCount={SkillCount}")]
    public static partial void LogViolation(
        this ILogger logger,
        Guid agentId,
        int pairCount,
        int skillCount);

    /// <summary>
    ///     Emitted once at the end of each <c>Validate</c> call, summarising the total number of violations found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="violationCount">Total number of <see cref="AgentSerializationViolation" />s produced.</param>
    [LoggerMessage(
        EventId = 5007,
        Level = LogLevel.Debug,
        Message = "AGENT_SERIALIZATION | COMPLETE | ViolationCount={ViolationCount}")]
    public static partial void LogValidationComplete(
        this ILogger logger,
        int violationCount);
}