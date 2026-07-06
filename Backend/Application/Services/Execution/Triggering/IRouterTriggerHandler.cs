using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Handles triggering and lifecycle management of router nodes during execution.
///     Evaluates router conditions, stores selections, publishes NotSelected events
///     for non-selected branches, and monitors branch completion.
/// </summary>
public interface IRouterTriggerHandler
{
    /// <summary>
    ///     Triggers evaluation and execution of a router node.
    /// </summary>
    /// <param name="routerId">The ID of the router to trigger.</param>
    /// <param name="routerNode">The router node to evaluate.</param>
    /// <param name="routerNodes">All router nodes for name resolution.</param>
    /// <param name="skillNodes">All skill nodes for name resolution.</param>
    /// <param name="variableContext">Variable context for router evaluation.</param>
    /// <param name="cancellationToken">Cancellation token for stopping execution.</param>
    Task TriggerRouterAsync(
        Guid routerId,
        RouterNode routerNode,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if a node is in the selected branch of any router it depends on.
    /// </summary>
    /// <param name="nodeId">The node to check.</param>
    /// <param name="prerequisites">The node's prerequisites.</param>
    /// <param name="routerNodes">All router nodes.</param>
    /// <param name="skillNodes">All skill nodes for name resolution.</param>
    /// <returns>True if the node should be triggered; false if it's in a non-selected branch.</returns>
    bool IsSelectedBranch(
        Guid nodeId,
        SkillEventPrerequisites prerequisites,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes);

    /// <summary>
    ///     Gets the current router selections made during execution.
    /// </summary>
    /// <returns>Dictionary of router selections, or null if no routers have been evaluated yet.</returns>
    IReadOnlyDictionary<Guid, Guid>? GetRouterSelections();

    /// <summary>
    ///     Resets handler state for the next execution.
    /// </summary>
    void Reset();
}