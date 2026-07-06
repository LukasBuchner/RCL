using System.Diagnostics;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;
using CoreExec = FHOOE.Freydis.Scheduling.Core.IPlannedSkillExecution;
using ZeroExtentFiringPlaceholder =
    FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.ZeroExtentFiringPlaceholder;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;

/// <summary>
///     Orchestrates building the <see cref="IExecutionGraph" /> by mapping nodes to agents,
///     analyzing capabilities, and wiring dependencies (with task-level propagation).
/// </summary>
public class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    private readonly IEdgeTypeMapper _edgeTypeMapper;
    private readonly INodeHierarchyProcessor _hierarchyProcessor;
    private readonly ILogger<ExecutionGraphBuilder> _logger;
    private readonly INodeResolver _nodeResolver;

    /// <summary>
    ///     Initializes a new <see cref="ExecutionGraphBuilder" />.
    /// </summary>
    /// <param name="logger">Logger for graph-build diagnostics.</param>
    /// <param name="edgeTypeMapper">Maps a dependency edge to its scheduling dependency type.</param>
    /// <param name="hierarchyProcessor">
    ///     Processes the procedure nodes into a containment hierarchy whose parent-to-children mapping drives
    ///     structural resolution of containers to their executable leaves.
    /// </param>
    /// <param name="nodeResolver">
    ///     Resolves a node ID to the executable node IDs it represents in the dependency graph, used to
    ///     classify a container as leafless when it represents no executable work.
    /// </param>
    public ExecutionGraphBuilder(
        ILogger<ExecutionGraphBuilder> logger,
        IEdgeTypeMapper edgeTypeMapper,
        INodeHierarchyProcessor hierarchyProcessor,
        INodeResolver nodeResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _edgeTypeMapper = edgeTypeMapper ?? throw new ArgumentNullException(nameof(edgeTypeMapper));
        _hierarchyProcessor = hierarchyProcessor ?? throw new ArgumentNullException(nameof(hierarchyProcessor));
        _nodeResolver = nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));
    }

    /// <inheritdoc />
    public async Task<IExecutionGraph?> BuildAsync(
        IReadOnlyList<Node> procedureNodes,
        IReadOnlyList<DependencyEdge> procedureEdges,
        ISkillDurationProvider durationProvider,
        bool strictMode = false)
    {
        ArgumentNullException.ThrowIfNull(durationProvider);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogGraphBuildStart(
            procedureNodes.Count,
            procedureEdges.Count,
            strictMode);

        // 1) Analyze skill execution nodes using duration provider
        var skillExecutionNodes = procedureNodes.OfType<SkillExecutionNode>().ToList();

        var execTasks = skillExecutionNodes
            .Select(async node =>
            {
                try
                {
                    return await durationProvider.AnalyzeAsync(node).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogNodeAnalysisFailure(node.Id, ex.Message);
                    return null;
                }
            });

        var execResults = (await Task.WhenAll(execTasks))
            .Where(x => x is not null)
            .Cast<IPlannedSkillExecution>()
            .ToList();

        if (execResults.Count < skillExecutionNodes.Count)
            _logger.LogSkillAnalysisResult(
                execResults.Count,
                skillExecutionNodes.Count,
                skillExecutionNodes.Count - execResults.Count);

        if (execResults.Count == 0)
        {
            _logger.LogEmptyGraphWarning("No nodes could be mapped to executable skills");
            return null;
        }

        // build lookup for skill executions by node ID
        var execByNode = execResults.ToDictionary(e => e.Id);

        // build child index: parent task ID → child nodes
        var childIndex = BuildChildIndex(procedureNodes);

        // Process the containment hierarchy once so leaflessness can be decided structurally against the
        // same resolution the runtime dependency analysis and the timeline display use.
        var hierarchy = _hierarchyProcessor.ProcessHierarchy(procedureNodes);

        // 2) Materialize a zero-extent firing endpoint for every leafless container that is a dependency-edge
        // endpoint (an empty task, or a router whose selected branch carries no executable work). This is a
        // pre-pass so the resolution loop never meets a leafless endpoint that resolves to nothing: a chain
        // A -> empty -> B is carried through the placeholder as A -> empty• -> B rather than dropped.
        var placeholders = MaterializeLeaflessEndpoints(procedureEdges, execByNode, childIndex, hierarchy);
        _logger.LogLeaflessMaterialized(placeholders.Count);

        // Enforce, loudly and independent of strict mode, the invariants leafless-materialization completeness
        // rests on: no leafless endpoint may slip through unmaterialized (its edge would be silently dropped),
        // and no edge may be sourced on a router's branch-entry task.
        ValidateLeaflessEndpointsMaterialized(procedureEdges, placeholders, execByNode, childIndex, hierarchy);
        ValidateNoBranchEntrySourcedEdges(procedureNodes, procedureEdges, hierarchy);

        // 3) Wire up dependencies, propagating task-level edges to descendant skills and resolving leafless
        // endpoints to their zero-extent placeholders.
        var dependencies = ExpandDependencies(
            procedureEdges,
            execByNode,
            placeholders,
            childIndex,
            strictMode);

        stopwatch.Stop();
        _logger.LogGraphBuildComplete(
            execResults.Count,
            stopwatch.Elapsed.TotalMilliseconds);

        // The leafless placeholders join the skill executions as zero-extent LP tasks: they get Start/Finish/
        // Duration variables and the Finish = Start pin (PlannedDuration 0), but carry no agent or skill, so
        // the timeline display, completion tracking, and agent-serialization all skip them.
        var skillExecutions = execResults
            .Cast<CoreExec>()
            .Concat(placeholders.Values)
            .ToList();

        return new ExecutionGraph
        {
            SkillExecutions = skillExecutions.AsReadOnly(),
            Dependencies = dependencies.AsReadOnly()
        };
    }

    /// <summary>
    ///     Builds an index of parent-task ID to its immediate child nodes.
    /// </summary>
    private static Dictionary<Guid, List<Node>> BuildChildIndex(IReadOnlyList<Node> nodes)
    {
        var index = new Dictionary<Guid, List<Node>>();
        foreach (var node in nodes)
        {
            if (node.ParentId is not { } parentId) continue;
            if (!index.TryGetValue(parentId, out var list))
            {
                list = [];
                index[parentId] = list;
            }

            list.Add(node);
        }

        return index;
    }

    /// <summary>
    ///     Determines which leafless-container nodes (a genuinely empty task, or a router whose selected branch
    ///     carries no executable work) appear as a dependency-edge endpoint, and materializes a zero-extent
    ///     <see cref="ZeroExtentFiringPlaceholder" /> for each. Leaflessness is decided structurally against
    ///     the same <see cref="INodeResolver.ResolveToExecutableIds" /> resolution the runtime dependency
    ///     analysis and the timeline display use, so the three layers agree on which containers are empty. A
    ///     router is a branch boundary that resolves to itself, so it is probed through its branch children: it
    ///     is leafless exactly when every branch target resolves to no executable work. A task whose only child
    ///     skill failed duration analysis still resolves to that child structurally, so it is NOT leafless and
    ///     is not materialized — its (missing) extent is surfaced rather than masked by a zero-extent rep.
    /// </summary>
    /// <param name="edges">The procedure's dependency edges, whose endpoints are scanned.</param>
    /// <param name="execByNode">Lookup of node ID to the executable skill execution analyzed for it.</param>
    /// <param name="childIndex">Index of parent-task ID to its immediate child nodes.</param>
    /// <param name="hierarchy">The processed containment hierarchy used for structural resolution.</param>
    /// <returns>A map from each leafless edge-endpoint node ID to its zero-extent placeholder.</returns>
    private Dictionary<Guid, ZeroExtentFiringPlaceholder> MaterializeLeaflessEndpoints(
        IReadOnlyList<DependencyEdge> edges,
        Dictionary<Guid, IPlannedSkillExecution> execByNode,
        Dictionary<Guid, List<Node>> childIndex,
        NodeHierarchyInfo hierarchy)
    {
        var routerIds = hierarchy.RouterNodes.Select(r => r.Id).ToHashSet();

        var placeholders = new Dictionary<Guid, ZeroExtentFiringPlaceholder>();
        foreach (var id in edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct())
            if (IsLeaflessContainer(id, execByNode, childIndex, hierarchy, routerIds))
                placeholders[id] = new ZeroExtentFiringPlaceholder { Id = id };

        return placeholders;
    }

    /// <summary>
    ///     Decides whether a node is a leafless container — a genuinely empty task, or a router whose every
    ///     branch resolves to no executable work — using the same
    ///     <see cref="INodeResolver.ResolveToExecutableIds" /> resolution the runtime dependency analysis and
    ///     the timeline display use, so the three layers agree on which containers are empty. A node already
    ///     mapped to an executable skill, or one that resolves to descendant skills (even skills whose duration
    ///     analysis later failed), is not leafless and is therefore not materialized.
    /// </summary>
    /// <param name="id">The node ID to classify.</param>
    /// <param name="execByNode">Lookup of node ID to the executable skill execution analyzed for it.</param>
    /// <param name="childIndex">Index of parent-task ID to its immediate child nodes.</param>
    /// <param name="hierarchy">The processed containment hierarchy used for structural resolution.</param>
    /// <param name="routerIds">The set of router node IDs, used to probe a router through its branches.</param>
    /// <returns><c>true</c> when the node represents no executable work; otherwise <c>false</c>.</returns>
    private bool IsLeaflessContainer(
        Guid id,
        Dictionary<Guid, IPlannedSkillExecution> execByNode,
        Dictionary<Guid, List<Node>> childIndex,
        NodeHierarchyInfo hierarchy,
        HashSet<Guid> routerIds)
    {
        if (execByNode.ContainsKey(id))
            return false;

        if (routerIds.Contains(id))
            return !childIndex.TryGetValue(id, out var branches)
                   || branches.All(b => !_nodeResolver.ResolveToExecutableIds(b.Id, hierarchy).Any());

        return !_nodeResolver.ResolveToExecutableIds(id, hierarchy).Any();
    }

    /// <summary>
    ///     Enforces the post-materialization invariant that every leafless edge-endpoint was materialized as a
    ///     zero-extent rep. A leafless endpoint reaching dependency expansion unmaterialized would resolve to
    ///     no execution and have its dependency silently dropped, reintroducing the early-fire defect (a
    ///     successor firing before its predecessor through an empty container). This fails the build loudly,
    ///     independent of strict mode, rather than let the ordering vanish — present-but-unanalyzable work,
    ///     which resolves to skills structurally and is a separate concern, is unaffected.
    /// </summary>
    /// <param name="edges">The procedure's dependency edges, whose endpoints are checked.</param>
    /// <param name="placeholders">The leafless placeholders materialized by the pre-pass.</param>
    /// <param name="execByNode">Lookup of node ID to the executable skill execution analyzed for it.</param>
    /// <param name="childIndex">Index of parent-task ID to its immediate child nodes.</param>
    /// <param name="hierarchy">The processed containment hierarchy used for structural resolution.</param>
    /// <exception cref="InvalidOperationException">A leafless edge-endpoint was not materialized.</exception>
    private void ValidateLeaflessEndpointsMaterialized(
        IReadOnlyList<DependencyEdge> edges,
        Dictionary<Guid, ZeroExtentFiringPlaceholder> placeholders,
        Dictionary<Guid, IPlannedSkillExecution> execByNode,
        Dictionary<Guid, List<Node>> childIndex,
        NodeHierarchyInfo hierarchy)
    {
        var routerIds = hierarchy.RouterNodes.Select(r => r.Id).ToHashSet();
        foreach (var id in edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct())
            if (IsLeaflessContainer(id, execByNode, childIndex, hierarchy, routerIds) &&
                !placeholders.ContainsKey(id))
                throw new InvalidOperationException(
                    $"Graph build invariant violated: leafless endpoint '{id}' was not materialized as a zero-extent rep; its dependency edge would be dropped and the successor could fire before the predecessor.");
    }

    /// <summary>
    ///     Enforces the domain invariant that a router's branch-entry task has no outgoing dependency edge (a
    ///     router's out-edges are sourced on the router itself). The completeness of leafless materialization
    ///     relies on this invariant — a branch-entry-sourced edge would escape the router's zero-extent rep and
    ///     order work against an entry that never independently fires — so a violation fails the build loudly,
    ///     independent of strict mode.
    /// </summary>
    /// <param name="nodes">All procedure nodes, used to find each edge source's parent.</param>
    /// <param name="edges">The procedure's dependency edges.</param>
    /// <param name="hierarchy">The processed hierarchy supplying the router node set.</param>
    /// <exception cref="InvalidOperationException">An edge is sourced on a router's branch-entry task.</exception>
    private static void ValidateNoBranchEntrySourcedEdges(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        NodeHierarchyInfo hierarchy)
    {
        var routerIds = hierarchy.RouterNodes.Select(r => r.Id).ToHashSet();
        var parentOf = nodes
            .Where(n => n.ParentId.HasValue)
            .ToDictionary(n => n.Id, n => n.ParentId!.Value);

        foreach (var edge in edges)
            if (parentOf.TryGetValue(edge.SourceId, out var parent) && routerIds.Contains(parent))
                throw new InvalidOperationException(
                    $"Graph build invariant violated: dependency source '{edge.SourceId}' is a branch-entry task of router '{parent}', but a branch-entry task must have no outgoing dependency edge (a router's out-edges are sourced on the router itself).");
    }

    /// <summary>
    ///     Expands each dependency edge into leaf-to-leaf dependencies. A skill or a materialized leafless
    ///     placeholder resolves to itself; any other container resolves to its descendant skill executions. A
    ///     chain through a leafless container is carried via its zero-extent placeholder
    ///     (<c>A -&gt; empty• -&gt; B</c>) rather than dropped, so the successor is ordered after the
    ///     predecessor. The resulting <c>(Source, Target, Type)</c> set is de-duplicated.
    /// </summary>
    /// <param name="edges">The procedure's dependency edges.</param>
    /// <param name="execByNode">Lookup of node ID to the executable skill execution analyzed for it.</param>
    /// <param name="placeholderByNode">Zero-extent placeholders for leafless edge-endpoints (the pre-pass).</param>
    /// <param name="childIndex">Index of parent-task ID to its immediate child nodes.</param>
    /// <param name="strictMode">
    ///     When <c>true</c>, an edge endpoint that resolves to no execution aborts the build with an
    ///     <see cref="InvalidOperationException" /> naming the offending node (a present-but-unanalyzable
    ///     skill); when <c>false</c> that edge contributes no dependency.
    /// </param>
    /// <returns>The de-duplicated expanded skill-to-skill (and skill-to-rep) dependencies.</returns>
    private List<Dependency> ExpandDependencies(
        IReadOnlyList<DependencyEdge> edges,
        Dictionary<Guid, IPlannedSkillExecution> execByNode,
        Dictionary<Guid, ZeroExtentFiringPlaceholder> placeholderByNode,
        Dictionary<Guid, List<Node>> childIndex,
        bool strictMode)
    {
        var descendantCache = new Dictionary<Guid, List<CoreExec>>();
        var allDependencies = new List<Dependency>();
        var seen = new HashSet<(Guid Source, Guid Target, DependencyType Type)>();

        List<CoreExec> GetDescendants(Guid nodeId)
        {
            if (descendantCache.TryGetValue(nodeId, out var cached))
                return cached;

            var list = new List<CoreExec>();
            if (childIndex.TryGetValue(nodeId, out var children))
                foreach (var child in children)
                    if (execByNode.TryGetValue(child.Id, out var exec))
                        list.Add(exec);
                    else
                        list.AddRange(GetDescendants(child.Id));

            descendantCache[nodeId] = list;
            return list;
        }

        // Resolve an edge endpoint to the executions a dependency through it gates: a skill or a materialized
        // leafless placeholder resolves to itself; any other container resolves to its descendant skills.
        List<CoreExec> Resolve(Guid id)
        {
            if (execByNode.TryGetValue(id, out var skill))
                return [skill];
            if (placeholderByNode.TryGetValue(id, out var placeholder))
                return [placeholder];
            return GetDescendants(id);
        }

        foreach (var edge in edges)
        {
            var sourceExecs = Resolve(edge.SourceId);
            var targetExecs = Resolve(edge.TargetId);

            if (sourceExecs.Count == 0 || targetExecs.Count == 0)
            {
                // A leafless container is materialized in the pre-pass, so a 0-resolution endpoint here is a
                // present-but-unanalyzable skill (no exec, not leafless): surface it in strict mode, drop the
                // edge otherwise — never invent an ordering across work that was not planned.
                if (strictMode)
                {
                    if (sourceExecs.Count == 0)
                        throw new InvalidOperationException(
                            $"Graph build failed in strict mode. Dependency source node '{edge.SourceId}' resolved to 0 executable skills (its skills failed planning analysis or it carries no executable work).");
                    throw new InvalidOperationException(
                        $"Graph build failed in strict mode. Dependency target node '{edge.TargetId}' resolved to 0 executable skills (its skills failed planning analysis or it carries no executable work).");
                }

                _logger.LogDependencyEdgeDropped(edge.SourceId, edge.TargetId, sourceExecs.Count, targetExecs.Count);
                continue;
            }

            var type = _edgeTypeMapper.Map(edge);
            foreach (var src in sourceExecs)
                foreach (var tgt in targetExecs)
                    if (seen.Add((src.Id, tgt.Id, type)))
                        allDependencies.Add(new Dependency
                        { Id = Guid.NewGuid(), Source = src, Target = tgt, Type = type });
        }

        return allDependencies;
    }
}