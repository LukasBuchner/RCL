using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Services.Validation;

/// <summary>
///     Validates that a dependency edge between two nodes is structurally valid before persistence.
///     Enforces self-loop prevention, node existence, router branch isolation, cross-procedure checks,
///     hierarchy level matching, duplicate prevention, event-level cycle detection,
///     target-handle restrictions on routers and tasks, and adaptive-capability validation
///     for finish-side dependencies into skill executions.
/// </summary>
public class DependencyEdgeValidator : IDependencyEdgeValidator
{
    private readonly IDependencyEdgeApplicationService _edgeService;
    private readonly INodeAgentMapper _nodeAgentMapper;
    private readonly INodeApplicationService _nodeService;
    private readonly IProcedureContext _procedureContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DependencyEdgeValidator"/> class.
    /// </summary>
    /// <param name="nodeService">The node application service used to look up node entities and their parents.</param>
    /// <param name="edgeService">The dependency edge application service used to query existing edges for duplicate and cycle detection.</param>
    /// <param name="procedureContext">The procedure context providing the currently loaded procedure ID for cross-procedure validation.</param>
    /// <param name="nodeAgentMapper">
    ///     The node-to-agent mapper used to resolve the runtime agent and skill assigned to a
    ///     <see cref="SkillExecutionNode" /> for adaptive-capability validation. Shared with
    ///     the planning- and execution-aware duration providers so the validator agrees with
    ///     the production scheduling pipeline on what counts as "agent assigned and online".
    /// </param>
    public DependencyEdgeValidator(
        INodeApplicationService nodeService,
        IDependencyEdgeApplicationService edgeService,
        IProcedureContext procedureContext,
        INodeAgentMapper nodeAgentMapper)
    {
        _nodeService = nodeService;
        _edgeService = edgeService;
        _procedureContext = procedureContext;
        _nodeAgentMapper = nodeAgentMapper;
    }

    /// <inheritdoc />
    public async Task ValidateAsync(
        Guid sourceId, Guid targetId,
        string? sourceHandle, string? targetHandle,
        Guid? excludeEdgeId = null)
    {
        // Rule 1: Self-loop prevention.
        if (sourceId == targetId)
            throw new GraphQLException(
                $"Cannot create a dependency edge from a node to itself. " +
                $"Node '{sourceId}' was specified as both source and target.");

        // Rule 2: Node existence.
        var sourceNode = await _nodeService.GetNodeByIdAsync(sourceId);
        if (sourceNode is null)
            throw new GraphQLException($"Source node '{sourceId}' was not found.");

        var targetNode = await _nodeService.GetNodeByIdAsync(targetId);
        if (targetNode is null)
            throw new GraphQLException($"Target node '{targetId}' was not found.");

        // Rule 3: First-level branch children of router nodes cannot participate in edges.
        await ThrowIfRouterBranchChildAsync(sourceNode);
        await ThrowIfRouterBranchChildAsync(targetNode);

        // Rule 8: Router nodes may only be targeted on the start (left) handle.
        // Routers have no externally-controllable finish behavior — Router.Finish is published
        // internally when all selected-branch skills complete. FF/SF edges targeting a router
        // would create false SCCs in the dependency graph.
        if (targetNode is RouterNode &&
            !string.Equals(targetHandle, "left", StringComparison.OrdinalIgnoreCase))
            throw new GraphQLException(
                $"Cannot target the finish handle of router node '{targetId}'. " +
                "Router nodes only accept start-handle (left) dependencies.");

        // Rule 9: Task nodes may only be targeted on the start (left) handle.
        // Tasks are grouping containers — their finish is derived from children completing.
        // An FF/SF edge targeting a task propagates to ALL child skills via
        // DependencyGraphAnalyzer, making every child adaptive.
        if (targetNode is TaskNode &&
            !string.Equals(targetHandle, "left", StringComparison.OrdinalIgnoreCase))
            throw new GraphQLException(
                $"Cannot target the finish handle of task node '{targetId}'. " +
                "Task nodes only accept start-handle (left) dependencies.");

        // Rule 10: Adaptive capability for finish-side dependencies into skill executions.
        // An SF or FF edge into a SkillExecutionNode adds a finish prerequisite, which
        // forces the runtime to dispatch the target through the adaptive execution branch
        // (ExecuteAdaptiveSkillAsync). The assigned agent must support adaptive execution
        // for the target's skill, otherwise the dependency would set up a guaranteed
        // runtime failure.
        await EnsureAdaptiveCapabilityForFinishSideTargetAsync(targetNode, targetHandle);

        // Rule 4: Cross-procedure — both nodes must belong to the currently loaded procedure.
        var currentProcedureId = _procedureContext.CurrentProcedureId;
        if (currentProcedureId.HasValue)
        {
            if (sourceNode.ProcedureId != currentProcedureId.Value)
                throw new GraphQLException(
                    $"Source node '{sourceId}' belongs to procedure '{sourceNode.ProcedureId}', " +
                    $"which is not the currently loaded procedure '{currentProcedureId.Value}'. " +
                    $"Edges may only connect nodes within the active procedure.");

            if (targetNode.ProcedureId != currentProcedureId.Value)
                throw new GraphQLException(
                    $"Target node '{targetId}' belongs to procedure '{targetNode.ProcedureId}', " +
                    $"which is not the currently loaded procedure '{currentProcedureId.Value}'. " +
                    $"Edges may only connect nodes within the active procedure.");
        }

        // Rule 5: Edges may only connect nodes that share the same parent.
        if (sourceNode.ParentId != targetNode.ParentId)
            throw new GraphQLException(
                $"Cannot create edge between nodes at different hierarchical levels. " +
                $"Source node '{sourceId}' belongs to parent '{sourceNode.ParentId?.ToString() ?? "none (top-level)"}', " +
                $"while target node '{targetId}' belongs to parent '{targetNode.ParentId?.ToString() ?? "none (top-level)"}'. " +
                $"Edges may only connect nodes that share the same parent or are both top-level nodes.");

        // Load existing edges once for duplicate and cycle checks.
        var existingEdges = await _edgeService.GetAllDependencyEdgesAsync();

        // Rule 6: Duplicate edge prevention.
        var isDuplicate = existingEdges.Any(e =>
            e.SourceId == sourceId && e.TargetId == targetId &&
            (excludeEdgeId is null || e.Id != excludeEdgeId.Value));
        if (isDuplicate)
            throw new GraphQLException(
                $"A dependency edge from node '{sourceId}' to node '{targetId}' already exists. " +
                $"Duplicate edges are not permitted.");

        // Rule 7: Event-level cycle detection.
        // The event-level waits-for graph uses (nodeId, eventType) vertices.
        // A cycle means a structural deadlock (circular wait). This check
        // covers ALL dependency types (FS, SS, FF, SF), not just FS.
        var filteredEdges = existingEdges
            .Where(e => excludeEdgeId is null || e.Id != excludeEdgeId.Value)
            .ToList();

        if (HasEventLevelCycle(sourceId, targetId, sourceHandle, targetHandle, filteredEdges))
            throw new GraphQLException(
                $"Cannot create this edge because it would create a circular dependency " +
                $"in the event graph. A cycle means a structural deadlock where nodes " +
                $"wait for each other indefinitely.");
    }

    /// <summary>
    ///     Throws a <see cref="GraphQLException"/> if the given node is a <see cref="TaskNode"/>
    ///     whose direct parent is a <see cref="RouterNode"/>, indicating it is a branch entry-point
    ///     that must remain edge-isolated.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>A task representing the asynchronous check operation.</returns>
    /// <exception cref="GraphQLException">
    ///     Thrown when the node is a first-level branch child of a router node.
    /// </exception>
    private async Task ThrowIfRouterBranchChildAsync(Node node)
    {
        if (node is TaskNode && node.ParentId.HasValue)
        {
            var parentNode = await _nodeService.GetNodeByIdAsync(node.ParentId.Value);
            if (parentNode is RouterNode)
                throw new GraphQLException(
                    $"Cannot create edge involving node '{node.Id}': this node is a branch child of router node " +
                    $"'{parentNode.Id}'. Dependency edges cannot be connected to or from the first-level branch " +
                    $"children of router nodes.");
        }
    }

    /// <summary>
    ///     Enforces that when the proposed edge targets the finish (F) side of a
    ///     <see cref="SkillExecutionNode" />, the runtime agent currently assigned to that
    ///     node's <c>SkillExecutionTask.AgentId</c> can execute the node's skill adaptively.
    /// </summary>
    /// <remarks>
    ///     A finish-side target (<paramref name="targetHandle" /> not equal to "left",
    ///     case-insensitive — including <c>null</c>, "right", or any other value) adds a
    ///     finish prerequisite, which causes the runtime to dispatch the node through the
    ///     adaptive execution branch instead of the standard branch. If the assigned agent
    ///     does not support adaptive execution for the skill, the dependency would set up
    ///     a guaranteed runtime failure and is rejected here.
    ///
    ///     Resolution is delegated to <see cref="INodeAgentMapper" /> for parity with the
    ///     planning- and execution-aware duration providers. A null mapping — caused by an
    ///     unknown target node type, an unassigned or unknown <c>AgentId</c>, or no runtime
    ///     agent currently registered for that <c>AgentId</c> — is treated as an
    ///     offline/unassigned condition and rejected, because the agent must be online for
    ///     timing calculation regardless of this rule.
    /// </remarks>
    /// <param name="targetNode">The persisted target node of the proposed edge.</param>
    /// <param name="targetHandle">
    ///     The target handle string. <c>"left"</c> means start side; any other value
    ///     (including <c>null</c> and "right") means finish side, consistent with
    ///     <c>DependencyGraphAnalyzer.GetEventTypeFromHandle</c>.
    /// </param>
    /// <returns>A task representing the asynchronous capability check.</returns>
    /// <exception cref="GraphQLException">
    ///     Thrown when the target is a finish-side <see cref="SkillExecutionNode" /> and
    ///     either (a) the assigned agent is unassigned or not currently online, or
    ///     (b) <see cref="IRuntimeAgent.CanExecuteAdaptivelyAsync" /> returns <c>false</c>.
    /// </exception>
    private async Task EnsureAdaptiveCapabilityForFinishSideTargetAsync(
        Node targetNode, string? targetHandle)
    {
        if (targetNode is not SkillExecutionNode skillNode)
            return;

        if (string.Equals(targetHandle, "left", StringComparison.OrdinalIgnoreCase))
            return;

        var mapping = await _nodeAgentMapper.MapAsync(skillNode);
        if (mapping is null)
            throw new GraphQLException(
                $"Cannot create finish-side dependency to skill execution node '{skillNode.Id}': " +
                "the assigned agent is unassigned or not currently online. " +
                "SF/FF dependencies require the target's agent to be assigned and connected " +
                "so the runtime can dispatch adaptive execution.");

        var (skill, domainAgent, runtimeAgent) = mapping.Value;
        var canAdapt = await runtimeAgent.CanExecuteAdaptivelyAsync(skill);
        if (!canAdapt)
            throw new GraphQLException(
                $"Cannot create finish-side dependency to skill '{skill.Name}': " +
                $"the assigned agent '{domainAgent.Name}' cannot execute this skill adaptively. " +
                "SF/FF dependencies require the target's agent to support adaptive execution.");
    }

    /// <summary>
    ///     Checks whether adding the proposed edge would create a cycle in the
    ///     event-level waits-for graph. Vertices are (nodeId, isStart) pairs.
    ///     Edges model blocking: the target event waits for the source event.
    ///     Implicit edges (N, Finish) → (N, Start) model "finish requires start."
    ///     A cycle in this graph corresponds to a structural deadlock.
    /// </summary>
    private static bool HasEventLevelCycle(
        Guid sourceId, Guid targetId,
        string? sourceHandle, string? targetHandle,
        IReadOnlyList<DependencyEdge> existingEdges)
    {
        var adj = new Dictionary<(Guid NodeId, bool IsStart), List<(Guid NodeId, bool IsStart)>>();

        // Collect all node IDs for implicit edges
        var nodeIds = new HashSet<Guid> { sourceId, targetId };
        foreach (var edge in existingEdges)
        {
            nodeIds.Add(edge.SourceId);
            nodeIds.Add(edge.TargetId);
        }

        // Implicit edges: (N, Finish) → (N, Start) for every node
        foreach (var nodeId in nodeIds)
            AddEventEdge(adj, (nodeId, false), (nodeId, true));

        // Existing dependency edges: target event waits for source event
        foreach (var edge in existingEdges)
        {
            var srcIsStart = string.Equals(edge.SourceHandle, "left", StringComparison.OrdinalIgnoreCase);
            var tgtIsStart = string.Equals(edge.TargetHandle, "left", StringComparison.OrdinalIgnoreCase);
            AddEventEdge(adj, (edge.TargetId, tgtIsStart), (edge.SourceId, srcIsStart));
        }

        // Proposed new edge: target event waits for source event
        var newSrcIsStart = string.Equals(sourceHandle, "left", StringComparison.OrdinalIgnoreCase);
        var newTgtIsStart = string.Equals(targetHandle, "left", StringComparison.OrdinalIgnoreCase);
        var from = (sourceId, newSrcIsStart);
        var to = (targetId, newTgtIsStart);
        AddEventEdge(adj, to, from);

        // Cycle check: BFS from 'from' to 'to'. If reachable, the new edge
        // closes a cycle (to → from already exists in the path, adding to → from as edge).
        var visited = new HashSet<(Guid, bool)>();
        var queue = new Queue<(Guid, bool)>();
        queue.Enqueue(from);
        visited.Add(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == to)
                return true;

            if (!adj.TryGetValue(current, out var neighbors))
                continue;

            foreach (var neighbor in neighbors)
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
        }

        return false;
    }

    private static void AddEventEdge(
        Dictionary<(Guid, bool), List<(Guid, bool)>> adj,
        (Guid, bool) from, (Guid, bool) to)
    {
        if (!adj.TryGetValue(from, out var neighbors))
        {
            neighbors = [];
            adj[from] = neighbors;
        }

        neighbors.Add(to);
    }
}