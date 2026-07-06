using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Domain.Entities.Procedure;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Initialization;

/// <summary>
///     Initializes procedure execution by loading, assigning execution IDs, calculating initial schedule, preparing agent
///     assignments, and initializing variable contexts.
/// </summary>
/// <remarks>
///     This service encapsulates the initialization logic for starting procedure execution:
///     <list type="number">
///         <item>
///             <description>Loads nodes and edges from repositories</description>
///         </item>
///         <item>
///             <description>Assigns execution IDs to all skill execution nodes</description>
///         </item>
///         <item>
///             <description>Calculates initial schedule with timing calculation orchestrator</description>
///         </item>
///         <item>
///             <description>Builds agent assignments from the schedule</description>
///         </item>
///         <item>
///             <description>Initializes variable context for procedure execution</description>
///         </item>
///     </list>
///     This service follows Single Responsibility Principle by focusing solely on initialization concerns.
/// </remarks>
public interface IExecutionInitializer
{
    /// <summary>
    ///     Initializes execution by loading procedure data, assigning execution IDs, and calculating the initial schedule.
    /// </summary>
    /// <param name="executionStartTime">The UTC time when execution is starting, used as reference for timing calculations.</param>
    /// <param name="cancellationToken">Token to cancel the initialization operation.</param>
    /// <returns>An initialization result containing loaded data, calculated schedule, and agent assignments.</returns>
    Task<ExecutionInitializationResult> InitializeAsync(
        DateTimeOffset executionStartTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initializes execution with procedure-specific data, including variable context initialization.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure to initialize.</param>
    /// <param name="executionId">Unique identifier for this procedure execution instance.</param>
    /// <param name="executionStartTime">The UTC time when execution is starting.</param>
    /// <param name="userProvidedValues">Optional user-provided values for procedure variables.</param>
    /// <param name="cancellationToken">Token to cancel the initialization operation.</param>
    /// <returns>An initialization result containing loaded data, calculated schedule, agent assignments, and variable context.</returns>
    Task<ExecutionInitializationResult> InitializeAsync(
        Guid procedureId,
        Guid executionId,
        DateTimeOffset executionStartTime,
        Dictionary<string, object>? userProvidedValues = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of execution initialization containing all data needed to start procedure execution.
/// </summary>
public record ExecutionInitializationResult
{
    /// <summary>
    ///     Indicates whether initialization was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Error message if initialization failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     The loaded nodes with assigned execution IDs.
    /// </summary>
    public IReadOnlyList<Node> Nodes { get; init; } = new List<Node>();

    /// <summary>
    ///     The loaded dependency edges.
    /// </summary>
    public IReadOnlyList<DependencyEdge> Edges { get; init; } = new List<DependencyEdge>();

    /// <summary>
    ///     The identifier of the procedure this execution belongs to.
    /// </summary>
    public Guid ProcedureId { get; init; }

    /// <summary>
    ///     The calculated initial schedule.
    /// </summary>
    public ScheduleResult? Schedule { get; init; }

    /// <summary>
    ///     Agent assignments mapping node IDs to assigned runtime agents.
    /// </summary>
    public IReadOnlyDictionary<Guid, IRuntimeAgent> AgentAssignments { get; init; } =
        new Dictionary<Guid, IRuntimeAgent>();

    /// <summary>
    ///     The execution start time used for initialization.
    /// </summary>
    public DateTimeOffset ExecutionStartTime { get; init; }

    /// <summary>
    ///     The initialized variable context for this execution (null if procedure has no variables).
    /// </summary>
    public VariableContextEntity? VariableContext { get; init; }
}