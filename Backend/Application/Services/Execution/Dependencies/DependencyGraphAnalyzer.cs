using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Dependencies;

/// <summary>
///     Analyzes domain DependencyEdges to create an event-based dependency graph.
///     Uses <see cref="INodeHierarchyProcessor" /> to handle TaskNode/SkillExecutionNode relationships
///     and <see cref="INodeResolver" /> to resolve source node IDs to their executable descendants.
///     Includes RouterNodes in the dependency graph to enable runtime branch evaluation.
/// </summary>
/// <remarks>
///     Formally verified in Sunstone (Lean 4):
///     - PrerequisiteComputation.lean — soundness: no spurious prerequisites are computed
///     - NodeHierarchy.lean — ancestor router finding and branch subtree membership
/// </remarks>
public sealed class DependencyGraphAnalyzer(
    INodeHierarchyProcessor hierarchyProcessor,
    INodeResolver nodeResolver,
    ILogger<DependencyGraphAnalyzer> logger)
    : IDependencyGraphAnalyzer
{
    private readonly INodeHierarchyProcessor _hierarchyProcessor =
        hierarchyProcessor ?? throw new ArgumentNullException(nameof(hierarchyProcessor));

    private readonly INodeResolver _nodeResolver =
        nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));

    private readonly ILogger<DependencyGraphAnalyzer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Analyzes nodes and edges to build an event-based dependency graph.
    ///     Includes both SkillExecutionNodes and RouterNodes in the prerequisites map.
    /// </summary>
    public DependencyGraph AnalyzeDependencies(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        _logger.LogAnalyzingDependencies(nodes.Count, edges.Count);

        // Use NodeHierarchyProcessor to handle hierarchy (DRY principle)
        var hierarchy = _hierarchyProcessor.ProcessHierarchy(nodes);

        // Extract RouterNodes for router-specific processing
        var routerNodes = nodes.OfType<RouterNode>().ToList();

        _logger.LogHierarchyProcessed(
            hierarchy.TaskNodes.Count, hierarchy.SkillExecutionNodes.Count, routerNodes.Count);

        // Build prerequisites map for skills and routers
        var prerequisitesMap = new Dictionary<Guid, SkillEventPrerequisites>();

        // Build a lookup of all nodes by ID for hierarchy traversal
        var allNodesById = nodes.ToDictionary(n => n.Id);

        // Step 1: Add RouterNodes to the prerequisites map
        foreach (var router in routerNodes)
        {
            var routerStartPrereqs = new List<EventPrerequisite>();

            // Find edges targeting this router
            var routerEdges = edges.Where(e => e.TargetId == router.Id).ToList();

            _logger.LogRouterIncomingEdges(router.RouterTask.Name, router.Id, routerEdges.Count);

            foreach (var edge in routerEdges)
            {
                // Resolve source to actual skill IDs
                var sourceSkillIds = ResolveSourceFiringEndpoints(edge.SourceId, hierarchy).ToList();

                foreach (var sourceSkillId in sourceSkillIds)
                {
                    var sourceEventType = GetEventTypeFromHandle(edge.SourceHandle);
                    var depType = GetDependencyType(edge.SourceHandle, edge.TargetHandle);
                    var sourceEventTypeName = sourceEventType.ToString();

                    _logger.LogRouterDependsOnSkill(router.Id, sourceSkillId, sourceEventTypeName);

                    routerStartPrereqs.Add(new EventPrerequisite
                    {
                        DependencySkillId = sourceSkillId,
                        RequiredEventType = sourceEventType,
                        DependencyType = depType
                    });
                }
            }

            // If this router is nested inside another router's branch, add ancestor Router.Start prerequisite
            var ancestorRouter = FindAncestorRouter(router, allNodesById);
            if (ancestorRouter != null)
            {
                _logger.LogNestedRouterPrerequisite(
                    router.RouterTask.Name, router.Id, ancestorRouter.RouterTask.Name, ancestorRouter.Id);

                routerStartPrereqs.Add(new EventPrerequisite
                {
                    DependencySkillId = ancestorRouter.Id,
                    RequiredEventType = EventTriggerType.Start,
                    DependencyType = DependencyType.StartToStart
                });
            }

            prerequisitesMap[router.Id] = new SkillEventPrerequisites
            {
                SkillId = router.Id, // Using SkillId field for router ID (polymorphic usage)
                StartPrerequisites = routerStartPrereqs.AsReadOnly(),
                FinishPrerequisites = new List<EventPrerequisite>().AsReadOnly() // Routers have no finish prereqs
            };

            _logger.LogRouterPrerequisitesComplete(
                router.RouterTask.Name, router.Id, routerStartPrereqs.Count, routerStartPrereqs.Count == 0);
        }

        // Step 2: Add SkillExecutionNodes to the prerequisites map
        foreach (var skill in hierarchy.SkillExecutionNodes)
        {
            var startPrereqs = new List<EventPrerequisite>();
            var finishPrereqs = new List<EventPrerequisite>();

            // Find edges targeting this skill (or its parent task, but NOT ancestor routers)
            var skillEdges = FindEdgesTargetingNodeExcludingRouters(skill, edges, allNodesById).ToList();

            _logger.LogSkillIncomingEdges(
                skill.SkillExecutionTask.Skill.Name, skill.Id, skillEdges.Count);

            foreach (var edge in skillEdges)
            {
                _logger.LogProcessingEdge(
                    edge.SourceId, edge.SourceHandle ?? "null", edge.TargetId, edge.TargetHandle ?? "null");

                // Resolve source to actual skill IDs (may be a task with multiple skills)
                var sourceSkillIds = ResolveSourceFiringEndpoints(edge.SourceId, hierarchy).ToList();

                _logger.LogResolvedSourceSkills(sourceSkillIds.Count);

                foreach (var sourceSkillId in sourceSkillIds)
                {
                    // Determine which event type is required from the source
                    var sourceEventType = GetEventTypeFromHandle(edge.SourceHandle);

                    // Determine which event this triggers on the target
                    var targetEventType = GetEventTypeFromHandle(edge.TargetHandle);

                    // Determine the full dependency type from both handles
                    var depType = GetDependencyType(edge.SourceHandle, edge.TargetHandle);

                    var sourceEventName = sourceEventType.ToString();
                    var targetEventName = targetEventType.ToString();
                    _logger.LogEventTypeMapping(sourceSkillId, sourceEventName, targetEventName);

                    var prerequisite = new EventPrerequisite
                    {
                        DependencySkillId = sourceSkillId,
                        RequiredEventType = sourceEventType,
                        DependencyType = depType
                    };

                    // Add to appropriate prerequisite list
                    if (targetEventType == EventTriggerType.Start)
                        startPrereqs.Add(prerequisite);
                    else // Finish
                        finishPrereqs.Add(prerequisite);
                }
            }

            // Step 3: If skill is inside a router branch, add Router.Start as start prerequisite.
            // Router.Start is published after the router has evaluated and stored its branch selection,
            // so the trigger service's branch filtering can determine which branch this skill belongs to.
            // Router.Finish is reserved for AFTER all selected-branch skills have completed, allowing
            // external nodes with FS edges to the router to wait for the entire branch to finish.
            var ancestorRouter = FindAncestorRouter(skill, allNodesById);
            if (ancestorRouter != null)
            {
                _logger.LogSkillInsideRouter(
                    skill.SkillExecutionTask.Skill.Name, skill.Id, ancestorRouter.RouterTask.Name, ancestorRouter.Id);

                startPrereqs.Add(new EventPrerequisite
                {
                    DependencySkillId = ancestorRouter.Id,
                    RequiredEventType = EventTriggerType.Start,
                    DependencyType = DependencyType.StartToStart
                });
            }

            prerequisitesMap[skill.Id] = new SkillEventPrerequisites
            {
                SkillId = skill.Id,
                StartPrerequisites = startPrereqs.AsReadOnly(),
                FinishPrerequisites = finishPrereqs.AsReadOnly()
            };

            _logger.LogDependencyAnalysis(
                skill.Id,
                skill.SkillExecutionTask.Skill.Name,
                startPrereqs.Count,
                finishPrereqs.Count,
                startPrereqs.Count == 0,
                finishPrereqs.Count > 0);

            foreach (var prereq in startPrereqs)
            {
                // Check if prerequisite is a router or a skill
                var depRouter = routerNodes.FirstOrDefault(r => r.Id == prereq.DependencySkillId);
                var requiredEventName = prereq.RequiredEventType.ToString();
                if (depRouter != null)
                {
                    _logger.LogStartPrereqRouter(depRouter.RouterTask.Name, prereq.DependencySkillId,
                        requiredEventName);
                }
                else
                {
                    var depSkill = hierarchy.SkillExecutionNodes.FirstOrDefault(s => s.Id == prereq.DependencySkillId);
                    var depSkillName = depSkill?.SkillExecutionTask.Skill.Name ?? "UNKNOWN";
                    _logger.LogStartPrerequisite(prereq.DependencySkillId, depSkillName, requiredEventName);
                }
            }

            foreach (var prereq in finishPrereqs)
            {
                var depSkill = hierarchy.SkillExecutionNodes.FirstOrDefault(s => s.Id == prereq.DependencySkillId);
                var finishDepSkillName = depSkill?.SkillExecutionTask.Skill.Name ?? "UNKNOWN";
                var finishRequiredEventName = prereq.RequiredEventType.ToString();
                _logger.LogFinishPrerequisite(prereq.DependencySkillId, finishDepSkillName, finishRequiredEventName);
            }
        }

        // Step 3: Add leafless container tasks (no executable descendants) to the prerequisites map.
        // A leafless task is a zero-extent firing endpoint: a dependency through it gates on its own
        // Start/Finish, so it must be gated (and later dispatched) like a skill or router. It collects edges
        // targeting itself or its non-router ancestors — the same target set as skills — so it inherits the
        // dependencies declared on its enclosing tasks.
        foreach (var task in hierarchy.TaskNodes)
        {
            if (_nodeResolver.ResolveToExecutableIds(task.Id, hierarchy).Any())
                continue; // not leafless — its descendant skills carry the dependencies

            var taskStartPrereqs = new List<EventPrerequisite>();
            var taskFinishPrereqs = new List<EventPrerequisite>();

            foreach (var edge in FindEdgesTargetingNodeExcludingRouters(task, edges, allNodesById))
                foreach (var sourceId in ResolveSourceFiringEndpoints(edge.SourceId, hierarchy))
                {
                    var prerequisite = new EventPrerequisite
                    {
                        DependencySkillId = sourceId,
                        RequiredEventType = GetEventTypeFromHandle(edge.SourceHandle),
                        DependencyType = GetDependencyType(edge.SourceHandle, edge.TargetHandle)
                    };

                    if (GetEventTypeFromHandle(edge.TargetHandle) == EventTriggerType.Start)
                        taskStartPrereqs.Add(prerequisite);
                    else
                        taskFinishPrereqs.Add(prerequisite);
                }

            // Inside a router branch: gate on the ancestor Router.Start, as skills and nested routers do.
            var taskAncestorRouter = FindAncestorRouter(task, allNodesById);
            if (taskAncestorRouter != null)
                taskStartPrereqs.Add(new EventPrerequisite
                {
                    DependencySkillId = taskAncestorRouter.Id,
                    RequiredEventType = EventTriggerType.Start,
                    DependencyType = DependencyType.StartToStart
                });

            prerequisitesMap[task.Id] = new SkillEventPrerequisites
            {
                SkillId = task.Id,
                StartPrerequisites = taskStartPrereqs.AsReadOnly(),
                FinishPrerequisites = taskFinishPrereqs.AsReadOnly()
            };
        }

        var graph = new DependencyGraph
        {
            Prerequisites = prerequisitesMap
        };

        var immediateStartCount = graph.GetImmediateStartSkills().Count();
        var adaptiveSkillCount = graph.GetAdaptiveSkills().Count();
        _logger.LogDependencyGraphComplete(graph.Prerequisites.Count, immediateStartCount, adaptiveSkillCount);

        return graph;
    }

    /// <summary>
    ///     Finds all edges that target the given node or its non-router ancestors. Excludes edges targeting
    ///     RouterNode ancestors since those are handled separately, which prevents the node from inheriting
    ///     router-level edges that would cause duplicate triggering. Used for both skills and leafless
    ///     container tasks so a node inherits the dependencies declared on its enclosing tasks.
    /// </summary>
    /// <param name="node">The node whose targeting edges are collected (a skill or a leafless task).</param>
    /// <param name="edges">All dependency edges in the procedure.</param>
    /// <param name="allNodesById">All nodes keyed by ID, used for the parent-chain walk.</param>
    /// <returns>The edges whose target is the node or one of its non-router ancestors.</returns>
    private static IEnumerable<DependencyEdge> FindEdgesTargetingNodeExcludingRouters(
        Node node,
        IReadOnlyList<DependencyEdge> edges,
        Dictionary<Guid, Node> allNodesById)
    {
        // Collect IDs of the node and its non-router ancestors
        var targetIds = new HashSet<Guid> { node.Id };

        // Traverse up the hierarchy, stopping at (but not including) RouterNodes
        var currentParentId = node.ParentId;
        while (currentParentId.HasValue)
        {
            if (!allNodesById.TryGetValue(currentParentId.Value, out var parentNode))
                break;

            // Stop if we hit a RouterNode - edges to routers are handled separately
            if (parentNode is RouterNode)
                break;

            targetIds.Add(currentParentId.Value);
            currentParentId = parentNode.ParentId;
        }

        return edges.Where(e => targetIds.Contains(e.TargetId));
    }

    /// <summary>
    ///     Finds the closest ancestor RouterNode for a given node, if any.
    ///     Walks up the parent hierarchy from the node until a RouterNode is found.
    ///     Used to add Router.Start prerequisite for skills and nested routers inside router branches.
    /// </summary>
    /// <param name="node">The node to find the ancestor router for.</param>
    /// <param name="allNodesById">Dictionary of all nodes keyed by their ID for efficient lookup.</param>
    /// <returns>The closest ancestor RouterNode, or null if no router ancestor exists.</returns>
    private RouterNode? FindAncestorRouter(
        Node node,
        Dictionary<Guid, Node> allNodesById)
    {
        var nodeName = node switch
        {
            SkillExecutionNode sn => sn.SkillExecutionTask.Skill.Name,
            TaskNode tn => tn.Task.Name,
            RouterNode rn => rn.RouterTask.Name,
            _ => "Unknown"
        };

        var nodeTypeName = node.GetType().Name;
        var parentIdString = node.ParentId?.ToString() ?? "null";
        _logger.LogFindAncestorRouterStart(nodeTypeName, nodeName, node.Id, parentIdString);

        var currentParentId = node.ParentId;
        var depth = 0;
        while (currentParentId.HasValue)
        {
            depth++;
            if (!allNodesById.TryGetValue(currentParentId.Value, out var parentNode))
            {
                var taskNodeCount = allNodesById.Values.OfType<TaskNode>().Count();
                var skillNodeCount = allNodesById.Values.OfType<SkillExecutionNode>().Count();
                var routerNodeCount = allNodesById.Values.OfType<RouterNode>().Count();
                _logger.LogParentNotFoundInHierarchy(
                    currentParentId.Value, depth, allNodesById.Count,
                    taskNodeCount, skillNodeCount, routerNodeCount);
                break;
            }

            var parentTypeName = parentNode.GetType().Name;
            var parentName = parentNode is TaskNode tn ? tn.Task.Name :
                parentNode is RouterNode rn ? rn.RouterTask.Name :
                parentNode is SkillExecutionNode sn ? sn.SkillExecutionTask.Skill.Name : "?";
            _logger.LogFoundParentAtDepth(depth, parentTypeName, parentName, parentNode.Id);

            if (parentNode is RouterNode router)
            {
                _logger.LogFoundAncestorRouter(router.RouterTask.Name, router.Id, depth);
                return router;
            }

            currentParentId = parentNode.ParentId;
        }

        _logger.LogNoAncestorRouterFound(nodeTypeName, nodeName, node.Id, depth);
        return null;
    }

    /// <summary>
    ///     Resolves a dependency-edge endpoint to the firing endpoints a dependency through it gates on.
    ///     Delegates to <see cref="INodeResolver.ResolveToFiringEndpointsIds" />: child skill IDs for a non-empty
    ///     TaskNode, the node's own ID for a SkillExecutionNode or RouterNode, and the node's own ID for a
    ///     leafless container (so a dependency through an empty task or branch gates on that container's own
    ///     Start/Finish events instead of being dropped).
    /// </summary>
    /// <param name="sourceId">The ID of the endpoint node on a dependency edge.</param>
    /// <param name="hierarchy">The processed node hierarchy for the current procedure.</param>
    /// <returns>The resolved firing-endpoint IDs; an empty sequence only when the ID is not found.</returns>
    private IEnumerable<Guid> ResolveSourceFiringEndpoints(Guid sourceId, NodeHierarchyInfo hierarchy)
    {
        return _nodeResolver.ResolveToFiringEndpointsIds(sourceId, hierarchy);
    }

    /// <summary>
    ///     Maps an edge handle to the event type it requires.
    ///     "left" = Start, "right" = Finish; any unrecognized handle, including null, empty, or whitespace, = Finish.
    /// </summary>
    /// <param name="handle">The source or target handle from a dependency edge.</param>
    /// <returns>The <see cref="EventTriggerType" /> the handle denotes.</returns>
    private static EventTriggerType GetEventTypeFromHandle(string? handle)
    {
        return HandleDependencyTypeMapper.ToEventType(handle);
    }

    /// <summary>
    ///     Maps a (sourceHandle, targetHandle) pair to a <see cref="DependencyType" />.
    ///     "right" (or any unrecognized handle, including null) = Finish, "left" = Start.
    ///     This is the C# counterpart of Lean's <c>handlePairToDepType</c>.
    /// </summary>
    /// <param name="sourceHandle">The handle on the source side of the edge.</param>
    /// <param name="targetHandle">The handle on the target side of the edge.</param>
    /// <returns>The <see cref="DependencyType" /> for the handle pair.</returns>
    internal static DependencyType GetDependencyType(string? sourceHandle, string? targetHandle)
    {
        return HandleDependencyTypeMapper.ToDependencyType(sourceHandle, targetHandle);
    }
}