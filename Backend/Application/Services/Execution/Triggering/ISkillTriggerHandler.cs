using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Domain.Entities.Procedure;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Handles triggering and lifecycle management of skill executions.
///     Manages both regular and adaptive skills, including planned finish time updates.
/// </summary>
public interface ISkillTriggerHandler
{
    /// <summary>
    ///     Triggers execution of a skill node.
    ///     Determines if the skill is adaptive or regular and calls the appropriate coordinator method.
    /// </summary>
    /// <param name="skillId">The ID of the skill to trigger.</param>
    /// <param name="skillNodes">The skill nodes lookup.</param>
    /// <param name="dependencyGraph">The dependency graph for prerequisite lookups.</param>
    /// <param name="variableContext">Variable context for property bindings.</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution.</param>
    /// <returns>A disposable subscription for the skill's Rx stream, or null if triggering was skipped.</returns>
    IDisposable? TriggerSkill(
        Guid skillId,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        DependencyGraph dependencyGraph,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Updates the planned finish time for an adaptive skill.
    ///     Silently ignores non-adaptive skills (no subject exists for them).
    /// </summary>
    /// <param name="skillId">The ID of the adaptive skill node.</param>
    /// <param name="newPlannedFinishTime">The new planned finish time in seconds.</param>
    void UpdatePlannedFinish(Guid skillId, double newPlannedFinishTime);

    /// <summary>
    ///     Resets handler state for the next execution.
    /// </summary>
    void Reset();
}