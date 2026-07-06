using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;

namespace FHOOE.Freydis.Application.Services.Common;

/// <summary>
///     Resolves a node ID to the set of executable node IDs it represents in the event-based dependency graph.
/// </summary>
/// <remarks>
///     Resolution descends the containment hierarchy of the procedure domain:
///     <list type="bullet">
///         <item>
///             <term>TaskNode</term>
///             <description>Expands to the union of its descendants' executable IDs, recursing through nested task nodes to any depth.</description>
///         </item>
///         <item>
///             <term>SkillExecutionNode</term>
///             <description>Resolves to its own ID — it is already the executable unit.</description>
///         </item>
///         <item>
///             <term>RouterNode</term>
///             <description>Resolves to its own ID and terminates the branch; routers publish Start/Finish events under their own identity, so resolution does not descend into a router's branch subtree.</description>
///         </item>
///     </list>
///     A node ID absent from the hierarchy resolves to an empty sequence and a warning is logged. The result is duplicate-free.
/// </remarks>
public interface INodeResolver
{
    /// <summary>
    ///     Resolves a node ID to the executable node IDs it stands for in the dependency graph.
    /// </summary>
    /// <param name="nodeId">
    ///     The ID of the node to resolve. May refer to a TaskNode, SkillExecutionNode, or RouterNode.
    /// </param>
    /// <param name="hierarchy">
    ///     The processed node hierarchy that supplies the TaskToSkillMapping, SkillExecutionNodes, and RouterNodes
    ///     needed for resolution.
    /// </param>
    /// <returns>
    ///     The executable node IDs the node represents: the executable IDs of all descendants (skills, with routers as
    ///     branch boundaries) when the node is a TaskNode, or a single-element sequence containing
    ///     <paramref name="nodeId" /> itself when the node is a SkillExecutionNode or RouterNode. Returns an empty
    ///     sequence when <paramref name="nodeId" /> is not found in the hierarchy.
    /// </returns>
    IEnumerable<Guid> ResolveToExecutableIds(Guid nodeId, NodeHierarchyInfo hierarchy);

    /// <summary>
    ///     Resolves a node ID to the firing endpoints a dependency through it gates on: the executable leaves
    ///     it represents when it has any, or the node itself when it is a leafless container (a task or router
    ///     branch with no executable descendant). The result is never empty for a node present in the
    ///     hierarchy, so a dependency whose endpoint is a leafless container is preserved as a gate on that
    ///     container's own Start/Finish events instead of being dropped.
    /// </summary>
    /// <param name="nodeId">
    ///     The ID of the node to resolve. May refer to a TaskNode, SkillExecutionNode, or RouterNode.
    /// </param>
    /// <param name="hierarchy">The processed node hierarchy used for resolution.</param>
    /// <returns>
    ///     The result of <see cref="ResolveToExecutableIds" /> when it is non-empty; otherwise a
    ///     single-element sequence containing <paramref name="nodeId" /> when the node is present in the
    ///     hierarchy; an empty sequence when it is absent.
    /// </returns>
    IEnumerable<Guid> ResolveToFiringEndpointsIds(Guid nodeId, NodeHierarchyInfo hierarchy);
}