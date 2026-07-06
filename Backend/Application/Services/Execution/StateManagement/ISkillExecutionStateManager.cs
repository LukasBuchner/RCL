using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Manages the state of all skill executions in a procedure.
/// </summary>
public interface ISkillExecutionStateManager
{
    /// <summary>
    ///     Initializes the state manager with nodes and agent assignments.
    /// </summary>
    /// <param name="nodes">The list of nodes to initialize state for.</param>
    /// <param name="agentAssignments">Dictionary mapping skill IDs to assigned agents.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes or agentAssignments is null.</exception>
    void Initialize(IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, IRuntimeAgent> agentAssignments);

    /// <summary>
    ///     Gets the state for a specific skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill.</param>
    /// <returns>The skill execution state, or null if not found.</returns>
    SkillExecutionState? GetState(Guid skillId);

    /// <summary>
    ///     Gets all skill execution states.
    /// </summary>
    /// <returns>A read-only collection of all states.</returns>
    IReadOnlyCollection<SkillExecutionState> GetAllStates();

    /// <summary>
    ///     Gets all states with a specific execution status.
    /// </summary>
    /// <param name="status">The execution status to filter by.</param>
    /// <returns>A read-only collection of matching states.</returns>
    IReadOnlyCollection<SkillExecutionState> GetStatesByStatus(ExecutionStatus status);

    /// <summary>
    ///     Updates the state for a specific skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill to update.</param>
    /// <param name="updateAction">The action to perform on the state.</param>
    /// <exception cref="ArgumentNullException">Thrown when updateAction is null.</exception>
    void UpdateState(Guid skillId, Action<SkillExecutionState> updateAction);

    /// <summary>
    ///     Gets the assigned agent for a specific skill.
    /// </summary>
    /// <param name="skillId">The ID of the skill.</param>
    /// <returns>The assigned agent, or null if not found.</returns>
    IRuntimeAgent? GetAssignedAgent(Guid skillId);
}