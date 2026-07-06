using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Validates that every physical agent referenced in a procedure graph has its assigned
///     <see cref="SkillExecutionNode" />s connected by an FS-first dependency chain — a path that
///     begins with a Finish-to-Start edge and may thread through further FS or SS edges — so that
///     no two skills on the same robot can be dispatched concurrently by the LP scheduler.
/// </summary>
/// <remarks>
///     <para>
///         <b>Algorithm overview (O(A * K * (V + E)))</b>
///     </para>
///     <para>
///         Let A = number of distinct agents, K = maximum skills per agent, V = number of
///         <see cref="SkillExecutionNode" />s, and E = number of FS + SS edges after handle-based
///         filtering. For each agent group the validator:
///         <list type="number">
///             <item>Builds two directed adjacency lists from the raw <see cref="DependencyEdge" />
///             set: an FS-only list used as the path seed, and an any-edge list (FS ∪ SS) used as
///             the path tail. Each edge endpoint is resolved through <see cref="INodeResolver" />
///             (cartesian product expansion for TaskNode sources/targets).</item>
///             <item>Runs one FS-then-Any BFS per skill node to compute its FS-first-reachable set
///             — first step must use an FS edge, subsequent steps may use either FS or SS. This
///             matches the Lean predicate <c>FsThenAny</c>.</item>
///             <item>For every unordered pair (A, B) of skills in the group, skips the pair when
///             the two nodes reside in mutually exclusive router branches, and records a
///             <see cref="SkillPair" /> violation when neither A can reach B nor B can reach A
///             via an FS-first path.</item>
///             <item>Aggregates all missing pairs into one <see cref="AgentSerializationViolation" />
///             per offending agent.</item>
///         </list>
///     </para>
///     <para>
///         The validator operates on <b>raw <see cref="DependencyEdge" />s</b> — not on the
///         event-level prerequisite graph produced by <see cref="DependencyGraphAnalyzer" /> — so it
///         does not observe the internal Router→branch StartToStart edges injected during prerequisite
///         computation, and therefore does not conflate routing control flow with physical serialization.
///     </para>
///     <para>
///         Correctness is formalised in the Lean 4 proof file <c>AgentSerialization.lean</c>,
///         specifically <c>fsThenAny_prevents_overlap</c> and the generalized
///         <c>agent_serialization_sound</c>.  The rule is deliberately conservative — it is
///         graph-only and may flag pairs that are in fact serialized via SS + duration-bound
///         chains that full LP reasoning would resolve — but the suggested fix (add an FS edge)
///         is always safe. Completeness is out of scope; see <c>Backend/docs/agent-serialization/proofs.md</c>.
///     </para>
///     <para>
///         <b>Usage contexts and consistency guarantees</b><br/>
///         When called from <c>ExecutionOrchestrator</c> this check is <b>SAFETY-CRITICAL</b>:
///         the Lean proof's same-graph invariant must hold — the <paramref name="nodes" /> and
///         <paramref name="edges" /> supplied must be the exact snapshot used for execution
///         planning.<br/>
///         When called from <c>ProcedureValidationTracker</c> (subscription delivery) the check
///         is <b>BEST-EFFORT</b> and may reflect procedure state that is up to 1 s stale.
///     </para>
/// </remarks>
/// <param name="hierarchyProcessor">
///     Processes the flat node list into a typed hierarchy used by the adjacency builder and the
///     mutual-exclusivity walk.
/// </param>
/// <param name="nodeResolver">
///     Expands TaskNode IDs to their constituent <see cref="SkillExecutionNode" /> IDs so that
///     TaskNode-level FS edges correctly serialize all contained skills.
/// </param>
/// <param name="agentNameResolver">
///     Resolves a human-readable display name from an agent ID for inclusion in
///     <see cref="AgentSerializationViolation.AgentName" />.
/// </param>
/// <param name="logger">Structured logger for validation diagnostics.</param>
public sealed class AgentSerializationValidator(
    INodeHierarchyProcessor hierarchyProcessor,
    INodeResolver nodeResolver,
    IAgentNameResolver agentNameResolver,
    ILogger<AgentSerializationValidator> logger)
    : IAgentSerializationValidator
{
    private readonly INodeHierarchyProcessor _hierarchyProcessor =
        hierarchyProcessor ?? throw new ArgumentNullException(nameof(hierarchyProcessor));

    private readonly INodeResolver _nodeResolver =
        nodeResolver ?? throw new ArgumentNullException(nameof(nodeResolver));

    private readonly IAgentNameResolver _agentNameResolver =
        agentNameResolver ?? throw new ArgumentNullException(nameof(agentNameResolver));

    private readonly ILogger<AgentSerializationValidator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public IReadOnlyList<AgentSerializationViolation> Validate(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        _logger.LogValidationStart(nodes.Count, edges.Count);

        var hierarchy = _hierarchyProcessor.ProcessHierarchy(nodes);

        // Build a flat lookup of all nodes for ancestry traversal
        var allNodesById = nodes.ToDictionary(n => n.Id);

        // Precompute router -> branch-skill-descendants once; both adjacency builds reuse it.
        // A user-drawn FS or SS edge whose endpoint is a RouterNode is semantically equivalent
        // to the same edge applied to every SkillExecutionNode reachable through the router's
        // branch subtrees (R.start ≤ B.start and B.finish ≤ R.finish for every branch skill B),
        // so reachability through a router is expanded into the adjacency list here rather than
        // at BFS time.
        var routerBranchSkills = BuildRouterBranchSkillMap(hierarchy);

        // Build the adjacency lists once; reused for every agent group.
        //   fsAdjacency  — direct FS successors, used to seed the FS-first path step.
        //   anyAdjacency — FS + SS successors, used to continue the path tail.
        var fsAdjacency = BuildAdjacencyList(edges, hierarchy, routerBranchSkills, false);
        var anyAdjacency = BuildAdjacencyList(edges, hierarchy, routerBranchSkills, true);

        // Group SkillExecutionNodes by the agent they are assigned to, keep only groups of 2+
        var agentGroups = hierarchy.SkillExecutionNodes
            .GroupBy(s => s.SkillExecutionTask.AgentId)
            .Where(g => g.Count() >= 2)
            .ToList();

        _logger.LogAgentGroupCount(agentGroups.Count);

        var violations = new List<AgentSerializationViolation>();

        foreach (var group in agentGroups)
            CheckAgentGroup(group.ToList(), fsAdjacency, anyAdjacency, allNodesById, violations);

        _logger.LogValidationComplete(violations.Count);

        return violations;
    }

    /// <summary>
    ///     Builds a directed adjacency list from the raw <see cref="DependencyEdge" /> set.
    ///     When <paramref name="includeStartToStart" /> is <see langword="false" /> the list
    ///     contains only Finish-to-Start edges (used as the FS-first seed); when
    ///     <see langword="true" /> it also includes Start-to-Start edges (used as the
    ///     any-edge tail during <c>FsThenAny</c> reachability).
    ///     Each source and target node is expanded to its executable descendants via
    ///     <see cref="INodeResolver" /> and then, when the resolved id refers to a
    ///     <see cref="RouterNode" />, further expanded to every skill descendant of the router's
    ///     branch subtrees via <paramref name="routerBranchSkills" />. The cartesian product of
    ///     all (source, target) expanded-ID pairs is added to the adjacency list.
    /// </summary>
    /// <param name="edges">
    ///     All dependency edges in the procedure graph. Edges of dep types other than the
    ///     ones selected by <paramref name="includeStartToStart" /> are discarded.
    /// </param>
    /// <param name="hierarchy">
    ///     The processed node hierarchy required by <see cref="INodeResolver.ResolveToExecutableIds" />
    ///     for TaskNode-to-skill expansion.
    /// </param>
    /// <param name="routerBranchSkills">
    ///     Map from each <see cref="RouterNode" /> ID to every <see cref="SkillExecutionNode" /> ID
    ///     reachable through its branch subtrees, used to propagate router-level FS/SS edges into
    ///     the branch skills that inherit the router's temporal position.
    /// </param>
    /// <param name="includeStartToStart">
    ///     When <see langword="true" />, SS edges are included alongside FS edges. When
    ///     <see langword="false" />, only FS edges are included.
    /// </param>
    /// <returns>
    ///     A dictionary mapping each source executable node ID to the set of executable node IDs it
    ///     can directly reach via one edge of the admitted type(s). Nodes with no outgoing admitted
    ///     edges are not present as keys.
    /// </returns>
    private Dictionary<Guid, HashSet<Guid>> BuildAdjacencyList(
        IReadOnlyList<DependencyEdge> edges,
        NodeHierarchyInfo hierarchy,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> routerBranchSkills,
        bool includeStartToStart)
    {
        var adjacency = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var edge in edges)
        {
            var depType = DependencyGraphAnalyzer.GetDependencyType(edge.SourceHandle, edge.TargetHandle);

            var admit = depType == DependencyType.FinishToStart ||
                        (includeStartToStart && depType == DependencyType.StartToStart);

            if (!admit)
                continue;

            var sourceIds = ExpandRoutersToBranchSkills(
                _nodeResolver.ResolveToFiringEndpointsIds(edge.SourceId, hierarchy), routerBranchSkills).ToList();
            var targetIds = ExpandRoutersToBranchSkills(
                _nodeResolver.ResolveToFiringEndpointsIds(edge.TargetId, hierarchy), routerBranchSkills).ToList();

            // Cartesian product: every resolved source can reach every resolved target
            foreach (var sourceId in sourceIds)
            {
                if (!adjacency.TryGetValue(sourceId, out var neighbors))
                {
                    neighbors = [];
                    adjacency[sourceId] = neighbors;
                }

                foreach (var targetId in targetIds) neighbors.Add(targetId);
            }
        }

        _logger.LogFsAdjacencyBuilt(adjacency.Count);

        return adjacency;
    }

    /// <summary>
    ///     Builds a map from each <see cref="RouterNode" /> ID to the IDs of every
    ///     <see cref="SkillExecutionNode" /> descendant reachable through the router's branch
    ///     subtrees, walking <see cref="NodeHierarchyInfo.ParentToChildrenMapping" /> from the
    ///     router downward.
    /// </summary>
    /// <remarks>
    ///     The walk descends through <see cref="TaskNode" /> branch targets and, defensively,
    ///     through any nested <see cref="RouterNode" />s that might appear inside a branch, so the
    ///     validator remains correct for multi-level router nesting even though the domain
    ///     currently prevents routers from being placed inside another router's branch. Routers
    ///     with no branch-skill descendants map to an empty list, preserving the adjacency
    ///     behaviour of user-drawn edges that reference the router by ID.
    /// </remarks>
    /// <param name="hierarchy">The processed node hierarchy supplying router nodes and parent/children links.</param>
    /// <returns>
    ///     A read-only dictionary keyed by router ID; each value is the list of all skill-execution
    ///     descendants of that router.
    /// </returns>
    private static Dictionary<Guid, IReadOnlyList<Guid>> BuildRouterBranchSkillMap(
        NodeHierarchyInfo hierarchy)
    {
        var result = new Dictionary<Guid, IReadOnlyList<Guid>>(hierarchy.RouterNodes.Count);

        if (hierarchy.RouterNodes.Count == 0)
            return result;

        var skillIdSet = hierarchy.SkillExecutionNodes.Select(s => s.Id).ToHashSet();

        foreach (var router in hierarchy.RouterNodes)
        {
            var skills = new List<Guid>();
            var visited = new HashSet<Guid> { router.Id };
            var stack = new Stack<Guid>();
            stack.Push(router.Id);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();

                if (!hierarchy.ParentToChildrenMapping.TryGetValue(currentId, out var children))
                    continue;

                foreach (var child in children)
                {
                    if (!visited.Add(child.Id))
                        continue;

                    if (skillIdSet.Contains(child.Id))
                        skills.Add(child.Id);

                    stack.Push(child.Id);
                }
            }

            result[router.Id] = skills;
        }

        return result;
    }

    /// <summary>
    ///     Expands an enumerable of resolved executable IDs so that any ID belonging to a
    ///     <see cref="RouterNode" /> also yields every skill ID reachable through that router's
    ///     branch subtrees. Non-router IDs are yielded unchanged.
    /// </summary>
    /// <remarks>
    ///     The router ID itself is preserved in the output so user-drawn edges that reference a
    ///     router directly still participate in adjacency and BFS. The additional branch-skill IDs
    ///     make FS/SS reachability through the router transitive, which eliminates the false
    ///     positive where a skill outside a router and a skill inside its branch were flagged as
    ///     unserialized despite an FS edge touching the router.
    /// </remarks>
    /// <param name="ids">The resolved executable IDs returned by <see cref="INodeResolver.ResolveToExecutableIds" />.</param>
    /// <param name="routerBranchSkills">Router-to-branch-skill map built once per <see cref="Validate" /> call.</param>
    /// <returns>
    ///     A sequence that yields every input ID and, for every router ID, also yields each of its
    ///     branch-skill descendants.
    /// </returns>
    private static IEnumerable<Guid> ExpandRoutersToBranchSkills(
        IEnumerable<Guid> ids,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> routerBranchSkills)
    {
        foreach (var id in ids)
        {
            yield return id;

            if (routerBranchSkills.TryGetValue(id, out var branchSkills))
                foreach (var branchSkillId in branchSkills)
                    yield return branchSkillId;
        }
    }

    /// <summary>
    ///     Computes the set of nodes that are reachable from <paramref name="source" /> via an
    ///     <b>FS-first path</b>: the first edge leaving <paramref name="source" /> must be of
    ///     type Finish-to-Start, and subsequent edges may be of type FS or SS. This mirrors the
    ///     Lean predicate <c>FsThenAny</c> and is the graph-level witness for agent serialization.
    /// </summary>
    /// <remarks>
    ///     The seed set is <c>fsAdjacency[source]</c> — i.e., direct FS successors of
    ///     <paramref name="source" />. From each seed node, the search expands over
    ///     <paramref name="anyAdjacency" /> (FS ∪ SS) to accumulate every node that is
    ///     reachable via an FS edge followed by any mix of FS/SS edges.
    ///     A pair <c>(x, y)</c> is considered serialized by the validator when <c>y</c> appears
    ///     in this set for <c>x</c>, or vice versa; otherwise a violation is recorded.
    /// </remarks>
    /// <param name="source">The node ID from which the FS-first reachability search begins.</param>
    /// <param name="fsAdjacency">
    ///     The FS-only directed adjacency list; supplies the mandatory first-edge seed set.
    /// </param>
    /// <param name="anyAdjacency">
    ///     The FS + SS directed adjacency list; used to extend the path after the first FS edge.
    /// </param>
    /// <returns>
    ///     A <see cref="HashSet{T}" /> of all node IDs reachable from <paramref name="source" />
    ///     via an FS-first path, excluding <paramref name="source" /> itself. An empty set is
    ///     returned when the source has no outgoing FS edges (no FS-first path is possible).
    /// </returns>
    private static HashSet<Guid> ComputeFsThenAnyReachableSet(
        Guid source,
        Dictionary<Guid, HashSet<Guid>> fsAdjacency,
        Dictionary<Guid, HashSet<Guid>> anyAdjacency)
    {
        var reachable = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        // Mandatory first edge: FS successors only.
        if (!fsAdjacency.TryGetValue(source, out var fsSeeds))
            return reachable;

        foreach (var seed in fsSeeds)
            if (reachable.Add(seed))
                queue.Enqueue(seed);

        // Tail: BFS over FS ∪ SS successors.
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!anyAdjacency.TryGetValue(current, out var nextNeighbors))
                continue;

            foreach (var neighbor in nextNeighbors)
                if (reachable.Add(neighbor))
                    queue.Enqueue(neighbor);
        }

        return reachable;
    }

    /// <summary>
    ///     Validates all skills assigned to a single agent, computing FS-first reachability
    ///     between every pair and recording a <see cref="AgentSerializationViolation" /> when
    ///     any unordered pair lacks an FS-first chain in either direction and the pair is not
    ///     exempt through mutual router-branch exclusivity.
    /// </summary>
    /// <param name="agentSkills">
    ///     All <see cref="SkillExecutionNode" />s assigned to the agent being validated.
    ///     Must contain at least two elements.
    /// </param>
    /// <param name="fsAdjacency">
    ///     The FS-only directed adjacency list — the mandatory seed for every FS-first path.
    /// </param>
    /// <param name="anyAdjacency">
    ///     The FS + SS directed adjacency list — used to extend the path tail.
    /// </param>
    /// <param name="allNodesById">
    ///     All nodes in the procedure graph keyed by their ID, used by
    ///     <see cref="AreInMutuallyExclusiveBranches" /> to walk parent chains.
    /// </param>
    /// <param name="violations">
    ///     Mutable list to which a <see cref="AgentSerializationViolation" /> is appended when
    ///     at least one unordered skill pair is missing an FS-first chain.
    /// </param>
    private void CheckAgentGroup(
        List<SkillExecutionNode> agentSkills,
        Dictionary<Guid, HashSet<Guid>> fsAdjacency,
        Dictionary<Guid, HashSet<Guid>> anyAdjacency,
        IReadOnlyDictionary<Guid, Node> allNodesById,
        List<AgentSerializationViolation> violations)
    {
        var agentId = agentSkills[0].SkillExecutionTask.AgentId;

        _logger.LogCheckGroup(agentId, agentSkills.Count);

        // Pre-compute FS-first reachable sets once per skill — O(K * (V + E)) per group
        var reachableSets = new Dictionary<Guid, HashSet<Guid>>(agentSkills.Count);
        foreach (var skill in agentSkills)
            reachableSets[skill.Id] = ComputeFsThenAnyReachableSet(skill.Id, fsAdjacency, anyAdjacency);

        var missingPairs = new List<SkillPair>();

        for (var i = 0; i < agentSkills.Count; i++)
            for (var j = i + 1; j < agentSkills.Count; j++)
            {
                var a = agentSkills[i];
                var b = agentSkills[j];

                // Skills in mutually exclusive router branches can never execute concurrently —
                // no serialization constraint is required between them
                if (AreInMutuallyExclusiveBranches(a.Id, b.Id, allNodesById))
                {
                    _logger.LogSkipMutuallyExclusive(a.Id, b.Id);
                    continue;
                }

                var aReachesB = reachableSets[a.Id].Contains(b.Id);
                var bReachesA = reachableSets[b.Id].Contains(a.Id);

                if (!aReachesB && !bReachesA)
                {
                    _logger.LogMissingFsPair(a.Id, b.Id, agentId);
                    missingPairs.Add(new SkillPair(a.Id, b.Id));
                }
            }

        if (missingPairs.Count == 0)
            return;

        var involvedSkillIds = missingPairs
            .SelectMany(p => new[] { p.SkillA, p.SkillB })
            .Distinct()
            .ToList();

        violations.Add(new AgentSerializationViolation
        {
            AgentId = agentId,
            AgentName = _agentNameResolver.Resolve(agentId),
            UnserializedSkills = involvedSkillIds
                .Select(id => agentSkills.First(s => s.Id == id))
                .Select(s => new UnserializedSkill(s.Id, s.SkillExecutionTask.Skill.Name))
                .ToList(),
            MissingFsPairs = missingPairs
        });

        _logger.LogViolation(agentId, missingPairs.Count, involvedSkillIds.Count);
    }

    /// <summary>
    ///     Determines whether two skill nodes reside in mutually exclusive branches of a shared
    ///     <see cref="RouterNode" /> ancestor — meaning exactly one of them can ever execute in any
    ///     given run, so no FS serialization constraint is required between them.
    /// </summary>
    /// <remarks>
    ///     Two nodes are considered mutually exclusive when they share at least one common
    ///     <see cref="RouterNode" /> ancestor but each descends from a different direct branch target
    ///     of that router.  If both nodes descend from the same branch target of every shared router
    ///     ancestor they are co-reachable and must be serialized.
    /// </remarks>
    /// <param name="nodeA">The ID of the first skill node.</param>
    /// <param name="nodeB">The ID of the second skill node.</param>
    /// <param name="allNodesById">
    ///     All nodes in the procedure graph keyed by ID, used to walk the parent chain.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when the two nodes share a common router ancestor and each enters
    ///     it through a different branch target; <see langword="false" /> in all other cases,
    ///     including when neither node is inside any router.
    /// </returns>
    private static bool AreInMutuallyExclusiveBranches(
        Guid nodeA,
        Guid nodeB,
        IReadOnlyDictionary<Guid, Node> allNodesById)
    {
        var ancestryA = CollectRouterAncestry(nodeA, allNodesById);
        var ancestryB = CollectRouterAncestry(nodeB, allNodesById);

        foreach (var (routerId, branchTargetA) in ancestryA)
            if (ancestryB.TryGetValue(routerId, out var branchTargetB) && branchTargetA != branchTargetB)
                return true;

        return false;
    }

    /// <summary>
    ///     Walks the parent chain of <paramref name="nodeId" /> upward and, for each
    ///     <see cref="RouterNode" /> ancestor, records which direct branch-target child the path
    ///     entered the router through.
    /// </summary>
    /// <remarks>
    ///     A node is considered to enter a router through a branch target when the immediately
    ///     preceding step on the parent-chain walk is listed as the <see cref="ConditionalBranch.TargetNodeId" />
    ///     of one of the router's branches.  Steps that are not branch targets (e.g. a child that is
    ///     a TaskNode owned directly by the router but not a named branch target) are not recorded.
    /// </remarks>
    /// <param name="nodeId">The node whose router ancestry is to be collected.</param>
    /// <param name="allNodesById">All nodes in the procedure graph keyed by ID.</param>
    /// <returns>
    ///     A dictionary mapping each ancestor <see cref="RouterNode" /> ID to the direct child ID
    ///     through which <paramref name="nodeId" /> descends into that router.  Only routers whose
    ///     branch list contains the traversed child as a <see cref="ConditionalBranch.TargetNodeId" />
    ///     are included.
    /// </returns>
    private static Dictionary<Guid, Guid> CollectRouterAncestry(
        Guid nodeId,
        IReadOnlyDictionary<Guid, Node> allNodesById)
    {
        // routerId -> branchTargetId (the direct child of the router on the path from nodeId)
        var ancestry = new Dictionary<Guid, Guid>();
        var currentId = nodeId;

        while (allNodesById.TryGetValue(currentId, out var node) && node.ParentId.HasValue)
        {
            var parentId = node.ParentId.Value;

            if (allNodesById.TryGetValue(parentId, out var parent) && parent is RouterNode
                {
                    RouterTask.Branches: not null
                } router)
            // currentId is a direct child of this router — check whether it is a named branch target
            {
                var matchingBranch = router.RouterTask.Branches
                    .FirstOrDefault(b => b.TargetNodeId == currentId);

                if (matchingBranch != null)
                    ancestry[router.Id] = currentId;
            }

            currentId = parentId;
        }

        return ancestry;
    }
}