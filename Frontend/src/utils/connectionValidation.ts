import { AppNode } from "../types/nodeTypes";

/**
 * Node types that only accept incoming dependencies on their left (start) handle.
 * Mirrors backend Rules 8 and 9: no finish-dep into a router or into a task.
 */
const LEFT_ONLY_TARGET_NODE_TYPES = new Set<string>(["taskNode", "routerNode"]);

/**
 * Whether a node of the given type accepts an incoming connection on its right
 * (finish) target handle. Nodes in [LEFT_ONLY_TARGET_NODE_TYPES] only accept the
 * left (start) handle; all other node types accept both.
 *
 * @param nodeType - The React Flow node `type` string, or `undefined` for unknown.
 * @returns `true` if the right target handle is a legal drop target for this node type.
 */
export function acceptsRightTargetHandle(
  nodeType: string | undefined,
): boolean {
  return !LEFT_ONLY_TARGET_NODE_TYPES.has(nodeType ?? "");
}

/**
 * Computes the set of node IDs that are valid drop targets when dragging
 * an edge from the given source node.
 *
 * A node is a valid target when ALL of the following hold:
 * - It is not the source node itself (no self-loops).
 * - It shares the same `parentId` as the source (same hierarchy level).
 * - It is not a direct child of a RouterNode (`hideHandles` / `isRouterChild`).
 * - It is not a PlayheadNode (playheads have no handles).
 *
 * @param sourceNodeId - The ID of the node the user is dragging from.
 * @param nodes        - The full list of nodes currently in the flow.
 * @returns A `Set<string>` of valid target node IDs.
 */
export function computeValidTargetNodeIds(
  sourceNodeId: string,
  nodes: AppNode[],
): Set<string> {
  const sourceNode = nodes.find((n) => n.id === sourceNodeId);
  if (!sourceNode) return new Set();

  const sourceParentId = sourceNode.parentId ?? null;

  const validIds = new Set<string>();

  for (const node of nodes) {
    // No self-loops
    if (node.id === sourceNodeId) continue;

    // Must share the same parent (same hierarchy level)
    if ((node.parentId ?? null) !== sourceParentId) continue;

    // PlayheadNodes never accept edges
    if (node.type === "playheadNode") continue;

    // Router branch children have handles hidden — skip them
    if (node.data && "hideHandles" in node.data && node.data.hideHandles)
      continue;
    if (node.data && "isRouterChild" in node.data && node.data.isRouterChild)
      continue;

    validIds.add(node.id);
  }

  return validIds;
}

/**
 * Checks whether a specific connection from `sourceId` to `targetId` is valid.
 * Used as the `isValidConnection` callback on `<ReactFlow>` to reject invalid
 * drops before they reach the backend.
 *
 * Rejects connections landing on the right (finish) target handle of node types
 * listed in [LEFT_ONLY_TARGET_NODE_TYPES], mirroring backend Rules 8 and 9.
 *
 * @param sourceId     - The source node ID of the proposed connection.
 * @param targetId     - The target node ID of the proposed connection.
 * @param nodes        - The full list of nodes currently in the flow.
 * @param targetHandle - The target-side handle id (`"left"` or `"right"`), or `null`.
 * @returns `true` if the connection is allowed, `false` otherwise.
 */
export function isConnectionValid(
  sourceId: string,
  targetId: string,
  nodes: AppNode[],
  targetHandle: string | null = null,
): boolean {
  const validTargets = computeValidTargetNodeIds(sourceId, nodes);
  if (!validTargets.has(targetId)) return false;

  if (targetHandle === "right") {
    const targetNode = nodes.find((n) => n.id === targetId);
    if (!acceptsRightTargetHandle(targetNode?.type)) return false;
  }

  return true;
}
