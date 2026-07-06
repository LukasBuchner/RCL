import { Edge } from "@xyflow/react";
import { AppNode } from "../types/nodeTypes";
import {
  createEdgeCommand,
  createNodeCommand,
  deleteEdgeCommand,
  deleteNodeCommand,
  FlowCommand,
} from "../undo";

/**
 * Input to {@link buildPastePlan}. Pure data — no Apollo types, no side
 * effects, no UUID randomness. The `idGenerator` injection lets tests assert
 * against deterministic ids without mocking `uuid()` globally.
 */
export interface PastePlanInput {
  /** The node that was copied or cut — becomes the root of the pasted subtree. */
  clipboardRoot: AppNode;
  /** `true` if the clipboard was populated via cut (originals must be removed). */
  clipboardIsCut: boolean;
  /** Every node currently in the procedure, used to walk the descendant graph. */
  allOriginalNodes: readonly AppNode[];
  /** Every edge currently in the procedure, used for internal and touching-edge sets. */
  allOriginalEdges: readonly Edge[];
  /** Parent id under which the pasted root should land; `undefined` means top-level. */
  pasteTargetParentId: string | undefined;
  /** Function returning a fresh id for each node/edge in the pasted subtree. */
  idGenerator: () => string;
}

/**
 * Returns the ordered {@link FlowCommand} list a paste operation should run
 * as a single composite. The output is suitable for
 * `compositeCommand(plan, "Paste", "Undo paste")`.
 *
 * Ordering invariants (matter for reference-safe forward execution):
 *   1. Create new nodes first (parents before children, guaranteed by DFS).
 *   2. Create internal edges next (both endpoints now exist).
 *   3. For cut only: delete old edges next (no dangling references to
 *      about-to-be-deleted nodes).
 *   4. For cut only: delete old nodes last.
 *
 * The inverse is derived automatically by `compositeCommand`: reverse order,
 * invert each command. Deleting the new nodes cleanly rolls back the whole
 * paste when the user hits undo.
 */
export function buildPastePlan(input: PastePlanInput): FlowCommand[] {
  const {
    clipboardRoot,
    clipboardIsCut,
    allOriginalNodes,
    allOriginalEdges,
    pasteTargetParentId,
    idGenerator,
  } = input;

  // ── Walk the descendant set from the clipboard root ──────────────────
  const originalSubtreeIds = collectSubtreeIds(
    clipboardRoot.id,
    allOriginalNodes,
  );

  // ── Assign new ids and build cloned AppNodes ─────────────────────────
  const originalToNew = new Map<string, string>();
  const newNodesInOrder: AppNode[] = [];
  const dfs = (originalId: string, newParentId: string | null) => {
    const original =
      originalId === clipboardRoot.id
        ? clipboardRoot
        : allOriginalNodes.find((n) => n.id === originalId);
    if (!original) return;

    const newId = idGenerator();
    originalToNew.set(originalId, newId);
    newNodesInOrder.push(cloneWithIdAndParent(original, newId, newParentId));

    const children = allOriginalNodes.filter((n) => n.parentId === originalId);
    for (const child of children) dfs(child.id, newId);
  };
  dfs(clipboardRoot.id, pasteTargetParentId ?? null);

  const plan: FlowCommand[] = [];

  // Forward step 1: create new nodes.
  for (const node of newNodesInOrder) {
    plan.push(createNodeCommand(node));
  }

  // Forward step 2: create internal edges (both endpoints in the subtree).
  for (const edge of allOriginalEdges) {
    const newSource = originalToNew.get(edge.source);
    const newTarget = originalToNew.get(edge.target);
    if (newSource === undefined || newTarget === undefined) continue;
    plan.push(
      createEdgeCommand({
        ...edge,
        id: idGenerator(),
        source: newSource,
        target: newTarget,
      }),
    );
  }

  if (!clipboardIsCut) return plan;

  // Forward step 3: delete originals of edges touching the cut subtree.
  for (const edge of allOriginalEdges) {
    if (
      originalSubtreeIds.has(edge.source) ||
      originalSubtreeIds.has(edge.target)
    ) {
      plan.push(deleteEdgeCommand(edge));
    }
  }

  // Forward step 4: delete originals of cut nodes.
  for (const node of allOriginalNodes) {
    if (!originalSubtreeIds.has(node.id)) continue;
    const incident = allOriginalEdges.filter(
      (e) => e.source === node.id || e.target === node.id,
    );
    plan.push(deleteNodeCommand(node, incident));
  }

  return plan;
}

/** Iteratively collects every descendant id of `rootId` (inclusive). */
function collectSubtreeIds(
  rootId: string,
  allNodes: readonly AppNode[],
): Set<string> {
  const visited = new Set<string>();
  const queue: string[] = [rootId];
  while (queue.length > 0) {
    const current = queue.pop()!;
    if (visited.has(current)) continue;
    visited.add(current);
    for (const n of allNodes) {
      if (n.parentId === current && !visited.has(n.id)) queue.push(n.id);
    }
  }
  return visited;
}

/**
 * Returns a shallow clone of `original` with a fresh id, the supplied parent,
 * and any stale copy/cut markers cleared. Keeps all other data (including the
 * type-specific payload) intact.
 */
function cloneWithIdAndParent(
  original: AppNode,
  newId: string,
  newParentId: string | null,
): AppNode {
  const { markedForCopy, markedForCut, ...cleanData } = original.data as {
    markedForCopy?: boolean;
    markedForCut?: boolean;
    [k: string]: unknown;
  };
  return {
    ...original,
    id: newId,
    parentId: newParentId ?? undefined,
    extent: newParentId ? "parent" : undefined,
    data: cleanData as AppNode["data"],
  } as AppNode;
}
