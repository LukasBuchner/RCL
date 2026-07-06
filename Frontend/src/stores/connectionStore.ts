import { create } from "zustand";

/** State tracked while the user is dragging an edge from a source handle. */
interface ConnectionState {
  /** The node ID the user started dragging from, or `null` when idle. */
  sourceNodeId: string | null;
  /** The handle id (`"left"` | `"right"`) the user started dragging from, or `null` when idle. */
  sourceHandle: string | null;
  /** IDs of nodes that are valid drop targets, or `null` when idle. */
  validTargetNodeIds: Set<string> | null;

  /** Called when edge dragging begins. */
  startConnection: (
    sourceNodeId: string,
    sourceHandle: string | null,
    validTargetIds: Set<string>,
  ) => void;
  /** Called when edge dragging ends (drop or cancel). */
  endConnection: () => void;
}

export const useConnectionStore = create<ConnectionState>((set) => ({
  sourceNodeId: null,
  sourceHandle: null,
  validTargetNodeIds: null,

  startConnection: (sourceNodeId, sourceHandle, validTargetIds) =>
    set({ sourceNodeId, sourceHandle, validTargetNodeIds: validTargetIds }),

  endConnection: () =>
    set({ sourceNodeId: null, sourceHandle: null, validTargetNodeIds: null }),
}));
