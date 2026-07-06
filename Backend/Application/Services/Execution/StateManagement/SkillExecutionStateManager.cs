using System.Collections.Concurrent;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Manages the state of all skill executions in a procedure.
/// </summary>
public class SkillExecutionStateManager(ILogger<SkillExecutionStateManager> logger) : ISkillExecutionStateManager
{
    private readonly ConcurrentDictionary<Guid, IRuntimeAgent> _agentAssignments = new();
    private readonly ConcurrentDictionary<Guid, SkillExecutionState> _skillStates = new();

    /// <inheritdoc />
    public void Initialize(IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, IRuntimeAgent> agentAssignments)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(agentAssignments);

        _skillStates.Clear();
        _agentAssignments.Clear();

        var skillExecutionNodes = nodes
            .OfType<SkillExecutionNode>()
            .ToList();

        foreach (var skillNode in skillExecutionNodes)
        {
            _skillStates[skillNode.Id] = new SkillExecutionState(skillNode);

            if (agentAssignments.TryGetValue(skillNode.Id, out var agent)) _agentAssignments[skillNode.Id] = agent;
        }

        logger.LogStateManagerInitialized(skillExecutionNodes.Count, _agentAssignments.Count);
    }

    /// <inheritdoc />
    public SkillExecutionState? GetState(Guid skillId)
    {
        return _skillStates.GetValueOrDefault(skillId);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<SkillExecutionState> GetAllStates()
    {
        return _skillStates.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<SkillExecutionState> GetStatesByStatus(ExecutionStatus status)
    {
        return _skillStates.Values
            .Where(s => s.ExecutionStatus == status)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Enforces monotone transitions: once <see cref="SkillExecutionState.ExecutionStatus" />
    ///     is terminal (<see cref="ExecutionStatusExtensions.IsTerminal" />), the mutation is
    ///     rejected and the call is a no-op with a warning log. The check is performed under
    ///     a per-state monitor lock so concurrent transitions cannot violate the invariant by
    ///     interleaving check-then-mutate. The Lean <c>h_no_regress</c> hypothesis of
    ///     <c>DualLoopConvergence.completion_decreases_measure</c> is enforced here at runtime.
    /// </remarks>
    public void UpdateState(Guid skillId, Action<SkillExecutionState> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        if (!_skillStates.TryGetValue(skillId, out var state))
        {
            var skillName = ResolveSkillName(skillId);
            logger.LogStateNotFound(skillName, skillId);
            return;
        }

        lock (state)
        {
            if (state.ExecutionStatus.IsTerminal())
            {
                var skillName = ResolveSkillName(skillId);
                logger.LogTerminalStateTransitionRejected(skillName, skillId, state.ExecutionStatus);
                return;
            }

            updateAction(state);
        }
    }

    /// <inheritdoc />
    public IRuntimeAgent? GetAssignedAgent(Guid skillId)
    {
        return _agentAssignments.GetValueOrDefault(skillId);
    }

    /// <summary>
    ///     Resolves a human-readable skill name from the state dictionary.
    /// </summary>
    private string ResolveSkillName(Guid skillId)
    {
        if (_skillStates.TryGetValue(skillId, out var state) && state.SkillNode is SkillExecutionNode skillNode)
            return skillNode.SkillExecutionTask.Skill.Name ?? "Unnamed Skill";
        return "Unknown Skill";
    }
}