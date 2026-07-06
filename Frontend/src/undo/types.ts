import { Edge } from "@xyflow/react";
import { AppNode } from "../types/nodeTypes";

/**
 * Side-effect channel for commands — persists local state changes to the backend
 * via Apollo mutations. Implementations are expected to serialize calls so the
 * network order matches the dispatch order; a delete must reach the server
 * before its paired re-create, and vice versa.
 *
 * All methods are fire-and-forget from the caller's perspective; commands never
 * await persistence.
 */
export interface FlowPersister {
  /** Persists the creation of `node`, respecting its id and type-specific data. */
  persistNodeCreation(node: AppNode): void;
  /** Persists the deletion of the node with `nodeId`. */
  persistNodeDeletion(nodeId: string): void;
  /**
   * Persists an update of `node`. Identity is keyed by `node.id`;
   * implementations dispatch on the node's type to the type-appropriate
   * update mutation, mirroring `persistNodeCreation`.
   */
  persistNodeUpdate(node: AppNode): void;
  /** Persists the creation of `edge`, respecting its id and handles. */
  persistEdgeCreation(edge: Edge): void;
  /** Persists the deletion of the edge with `edgeId`. */
  persistEdgeDeletion(edgeId: string): void;
}

/**
 * Execution context passed to each command's `apply`. Carries the React Flow
 * state setters so commands can mutate local state and a {@link FlowPersister}
 * so side-effecting commands can propagate changes to the backend. Callers
 * without a live backend (tests, previews) use {@link NoOpFlowPersister}
 * instead of threading `null` through the type system.
 */
export interface FlowCommandContext {
  setNodes: (updater: (nodes: AppNode[]) => AppNode[]) => void;
  setEdges: (updater: (edges: Edge[]) => Edge[]) => void;
  persister: FlowPersister;
}

/**
 * No-op {@link FlowPersister} used in local-only scopes (tests, previews,
 * any context without a live backend). Exposing a concrete object lets
 * commands depend on a non-nullable persister — callers with nothing to
 * persist pick this impl explicitly instead of threading `null`.
 */
export const NoOpFlowPersister: FlowPersister = {
  persistNodeCreation: () => {},
  persistNodeDeletion: () => {},
  persistNodeUpdate: () => {},
  persistEdgeCreation: () => {},
  persistEdgeDeletion: () => {},
};

/**
 * Discriminated event delivered by the {@link FlowPersister} when a backend
 * mutation rejects an optimistically-applied local change. Carries enough
 * information for the host to roll back the local state and surface the
 * cause to the user.
 */
export type FlowPersisterErrorEvent =
  | { operation: "createNode"; node: AppNode; error: Error }
  | { operation: "deleteNode"; nodeId: string; error: Error }
  | { operation: "updateNode"; node: AppNode; error: Error }
  | { operation: "createEdge"; edge: Edge; error: Error }
  | { operation: "deleteEdge"; edgeId: string; error: Error };

/**
 * Callback invoked by {@link FlowPersister} when a backend mutation fails.
 * The host is responsible for rolling back the optimistic local change
 * (typically by removing/restoring the affected node or edge from React state)
 * and notifying the user.
 */
export type FlowPersisterErrorHandler = (
  event: FlowPersisterErrorEvent,
) => void;

/**
 * A reversible mutation tracked by the undo manager. Each command captures
 * the pre-image it needs to produce an `inverse` that restores the prior state.
 *
 * Commands are constructed with data already in hand — they do not read live
 * state during `apply`, since the live state may have drifted by the time an
 * undo/redo runs.
 */
export interface FlowCommand {
  /** Human-readable label for the action this command performs. */
  readonly description: string;
  /** Label for applying the inverse, used in undo tooltips. */
  readonly reverseDescription: string;
  /**
   * Logical scope used for conflict eviction. Stack entries with the same
   * scope are dropped together when any one of them is invalidated.
   */
  readonly scopeName: string;
  /** Applies the forward mutation using the given context. */
  apply(ctx: FlowCommandContext): void;
  /** Returns a new command whose `apply` undoes this command's `apply`. */
  inverse(): FlowCommand;
}
