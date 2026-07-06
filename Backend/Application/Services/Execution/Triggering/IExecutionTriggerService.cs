using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Domain.Entities.Procedure;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Monitors execution events and triggers skills/routers when their prerequisites are met.
///     Core orchestration service that bridges the event bus with skill execution and router evaluation.
/// </summary>
public interface IExecutionTriggerService
{
    /// <summary>
    ///     Starts monitoring execution events and triggering skills/routers based on the dependency graph.
    /// </summary>
    /// <param name="dependencyGraph">The dependency graph containing event prerequisites for each skill.</param>
    /// <param name="nodes">All nodes in the procedure (including TaskNodes for routers and SkillExecutionNodes for skills).</param>
    /// <param name="variableContext">Variable context for router evaluation. Required when procedure contains router nodes.</param>
    /// <exception cref="ArgumentNullException">Thrown when dependencyGraph, nodes, or variableContext is null.</exception>
    void Start(DependencyGraph dependencyGraph, IReadOnlyList<Node> nodes, VariableContextEntity? variableContext);

    /// <summary>
    ///     Stops monitoring and cleans up all subscriptions.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    ///     Updates the planned finish time for an adaptive skill.
    ///     Called by the orchestrator when re-scheduling occurs.
    /// </summary>
    /// <param name="skillId">The ID of the adaptive skill node.</param>
    /// <param name="newPlannedFinishTime">The new planned finish time in seconds.</param>
    void UpdatePlannedFinish(Guid skillId, double newPlannedFinishTime);

    /// <summary>
    ///     Gets the current router selections made during execution.
    ///     Maps router node ID to the selected target node ID (chosen branch).
    /// </summary>
    /// <returns>Dictionary of router selections, or null if no routers have been evaluated yet.</returns>
    IReadOnlyDictionary<Guid, Guid>? GetRouterSelections();

    /// <summary>
    ///     Updates planned finish times for all running adaptive skills based on the current schedule.
    ///     Skips skills that have emitted a terminal event (Finish, Failed, NotSelected)
    ///     on the bus, because terminal skills do not need planned-finish updates.
    /// </summary>
    /// <param name="nodes">The current scheduled nodes.</param>
    void UpdateAdaptivePlannedFinishTimes(IReadOnlyList<Node> nodes);

    /// <summary>
    ///     <see cref="IObserver{T}" /> surface that receives per-execution node snapshots and
    ///     forwards adaptive planned-finish-time updates to the per-skill subjects held by
    ///     <c>SkillTriggerHandler</c>. Its <c>OnNext</c> delegates to
    ///     <see cref="UpdateAdaptivePlannedFinishTimes" />; <c>OnError</c> is logged and
    ///     swallowed; <c>OnCompleted</c> is a deliberate no-op so that a per-execution source
    ///     observable completing does not tear down the per-skill subjects.
    /// </summary>
    IObserver<IReadOnlyList<Node>> PlannedFinishObserver { get; }
}