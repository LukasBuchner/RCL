import { Edge, XYPosition } from "@xyflow/react";
import { AppNode } from "../types/nodeTypes";
import { FlowCommand, FlowCommandContext } from "./types";

/**
 * Moves a node from `from` to `to` in absolute canvas coordinates. Both
 * positions are captured at construction so undo restores the exact prior
 * coordinate regardless of intermediate drag events.
 */
export function moveNodeCommand(
  nodeId: string,
  from: XYPosition,
  to: XYPosition,
): FlowCommand {
  return {
    description: "Move node",
    reverseDescription: "Move node back",
    scopeName: nodeScope(nodeId),
    apply(ctx) {
      ctx.setNodes((nodes) =>
        nodes.map((n) => (n.id === nodeId ? { ...n, position: to } : n)),
      );
    },
    inverse() {
      return moveNodeCommand(nodeId, to, from);
    },
  };
}

/**
 * Inserts `node` into local state and asks the persister to create it on the
 * server. Optional `incidentEdges` are inserted alongside — this is what makes
 * the command usable both for "create a brand-new node" (empty list) and for
 * "restore a previously deleted node" (the list captured at delete time). The
 * inverse is always a {@link deleteNodeCommand} over the same node and edges.
 *
 * Local inserts are idempotent — re-inserting an existing id is a no-op —
 * so when the server subscription later delivers the authoritative copy it
 * does not duplicate.
 */
export function createNodeCommand(
  node: AppNode,
  incidentEdges: Edge[] = [],
): FlowCommand {
  const label = describe(node);
  return {
    description: `Create ${label}`,
    reverseDescription: `Delete ${label}`,
    scopeName: nodeScope(node.id),
    apply(ctx) {
      ctx.setNodes((nodes) =>
        nodes.some((n) => n.id === node.id) ? nodes : [...nodes, node],
      );
      if (incidentEdges.length > 0) {
        ctx.setEdges((edges) => {
          const existing = new Set(edges.map((e) => e.id));
          const additions = incidentEdges.filter((e) => !existing.has(e.id));
          return additions.length === 0 ? edges : [...edges, ...additions];
        });
      }
      ctx.persister.persistNodeCreation(node);
      for (const edge of incidentEdges) ctx.persister.persistEdgeCreation(edge);
    },
    inverse() {
      return deleteNodeCommand(node, incidentEdges);
    },
  };
}

/**
 * Removes `node` and every edge in `incidentEdges` from local state and asks
 * the persister to delete the node on the server — the server cascades edge
 * deletion, so only a single node-level call is issued. Incident edges must
 * be captured at construction time since the forward operation clears them
 * from local state.
 *
 * The inverse is a {@link createNodeCommand} that restores node and edges
 * verbatim so identifiers and handles line up with the pre-deletion snapshot.
 */
export function deleteNodeCommand(
  node: AppNode,
  incidentEdges: Edge[] = [],
): FlowCommand {
  const label = describe(node);
  return {
    description: `Delete ${label}`,
    reverseDescription: `Create ${label}`,
    scopeName: nodeScope(node.id),
    apply(ctx) {
      ctx.setNodes((nodes) => nodes.filter((n) => n.id !== node.id));
      ctx.setEdges((edges) =>
        edges.filter((e) => e.source !== node.id && e.target !== node.id),
      );
      ctx.persister.persistNodeDeletion(node.id);
    },
    inverse() {
      return createNodeCommand(node, incidentEdges);
    },
  };
}

/**
 * Replaces `before` with `after` in local state (both must share the same
 * id) and asks the persister to update the node on the server. The inverse
 * swaps `before` and `after` so repeated undo/redo is symmetric.
 *
 * Identity is keyed by `id`; the persister dispatches to the type-appropriate
 * update mutation based on the node's type, mirroring `persistNodeCreation`.
 *
 * @throws Error if `before.id !== after.id`.
 */
export function updateNodeCommand(
  before: AppNode,
  after: AppNode,
): FlowCommand {
  if (before.id !== after.id) {
    throw new Error(
      `updateNodeCommand requires matching ids: before=${before.id}, after=${after.id}`,
    );
  }
  const afterLabel = describe(after);
  const beforeLabel = describe(before);
  return {
    description: `Update ${afterLabel}`,
    reverseDescription: `Update ${beforeLabel}`,
    scopeName: nodeScope(after.id),
    apply(ctx) {
      ctx.setNodes((nodes) =>
        nodes.map((n) => (n.id === after.id ? after : n)),
      );
      ctx.persister.persistNodeUpdate(after);
    },
    inverse() {
      return updateNodeCommand(after, before);
    },
  };
}

/**
 * Inserts `edge` into local state and asks the persister to create it on the
 * server. Used for both user-initiated edge creation (drag-to-connect) and
 * for restoring an edge after an undo of {@link deleteEdgeCommand} — the
 * edge's id and handles are preserved either way.
 */
export function createEdgeCommand(edge: Edge): FlowCommand {
  return {
    description: "Create edge",
    reverseDescription: "Delete edge",
    scopeName: edgeScope(edge.id),
    apply(ctx) {
      ctx.setEdges((edges) =>
        edges.some((e) => e.id === edge.id) ? edges : [...edges, edge],
      );
      ctx.persister.persistEdgeCreation(edge);
    },
    inverse() {
      return deleteEdgeCommand(edge);
    },
  };
}

/**
 * Removes `edge` from local state and asks the persister to delete it on the
 * server. On undo, a {@link createEdgeCommand} re-inserts it with original id
 * and handles.
 */
export function deleteEdgeCommand(edge: Edge): FlowCommand {
  return {
    description: "Delete edge",
    reverseDescription: "Create edge",
    scopeName: edgeScope(edge.id),
    apply(ctx) {
      ctx.setEdges((edges) => edges.filter((e) => e.id !== edge.id));
      ctx.persister.persistEdgeDeletion(edge.id);
    },
    inverse() {
      return createEdgeCommand(edge);
    },
  };
}

/**
 * Groups multiple commands into a single undo/redo unit. Applied in declared
 * order; the inverse applies each component's inverse in reverse order.
 *
 * @throws Error if `commands` is empty.
 */
export function compositeCommand(
  commands: readonly FlowCommand[],
  description = "Group",
  reverseDescription = "Undo group",
): FlowCommand {
  if (commands.length === 0) {
    throw new Error("compositeCommand requires at least one command");
  }
  const scopeName = `group:${commands.map((c) => c.scopeName).join(",")}`;
  return {
    description,
    reverseDescription,
    scopeName,
    apply(ctx: FlowCommandContext) {
      for (const cmd of commands) cmd.apply(ctx);
    },
    inverse() {
      const reversed = [...commands].reverse().map((c) => c.inverse());
      return compositeCommand(reversed, reverseDescription, description);
    },
  };
}

// ── Private formatters shared across commands ───────────────────────

/** Safe label for a node, falling back to "node" when the data has no name. */
function describe(node: AppNode): string {
  const data = node.data as { name?: string } | undefined;
  const name = data?.name?.trim();
  return name && name.length > 0 ? name : "node";
}

/** Scope identifier used by the undo stack to group node-related entries. */
function nodeScope(id: string): string {
  return `node:${id}`;
}

/** Scope identifier used by the undo stack to group edge-related entries. */
function edgeScope(id: string): string {
  return `edge:${id}`;
}
