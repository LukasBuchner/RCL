using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Holds the state of a single skill execution.
/// </summary>
public class SkillExecutionState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillExecutionState" /> class.
    /// </summary>
    /// <param name="skillNode">The skill node being executed.</param>
    public SkillExecutionState(Node skillNode)
    {
        SkillNode = skillNode;
        ExecutionStatus = ExecutionStatus.NotStarted;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Gets or sets the skill node being executed.
    /// </summary>
    public Node SkillNode { get; set; }

    /// <summary>
    ///     Gets or sets the current execution status.
    /// </summary>
    public ExecutionStatus ExecutionStatus { get; set; }

    /// <summary>
    ///     Gets or sets the assigned runtime agent.
    /// </summary>
    public IRuntimeAgent? AssignedAgent { get; set; }

    /// <summary>
    ///     Gets or sets the time when execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    ///     Gets or sets the time when execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    ///     Gets the time when this state was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    ///     Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the subscription to the execution stream.
    /// </summary>
    public IDisposable? Subscription { get; set; }

    /// <summary>
    ///     Gets or sets the last progress percentage reported.
    /// </summary>
    public double? LastProgressPercentage { get; set; }

    /// <summary>
    ///     Gets or sets the last progress update received.
    /// </summary>
    public SkillExecutionProgress? LastProgress { get; set; }
}