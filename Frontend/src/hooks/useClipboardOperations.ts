import { useCallback, useState } from "react";
import { useQuery } from "@apollo/client";
import { useReactFlow } from "@xyflow/react";
import {
  GetDependencyEdgesDocument,
  GetDependencyEdgesQuery,
  GetNodeByIdDocument,
  GetNodeByIdQuery,
  GetNodesDocument,
  GetNodesQuery,
  NodeFieldsFragment,
} from "../__generated__/graphql";
import { useClipboardStore } from "../stores/clipboardStore";
import { mapEdgeFieldsFragmentsToAppEdges } from "../types/mapping/edge/mapEdgeFieldsFragmentsToAppEdges";
import {
  mapNodeFieldsFragmentsToAppNodes,
  mapNodeFieldsFragmentToAppNode,
} from "../types/mapping/node/mapNodeFieldsFragmentsToAppNodes";
import { compositeCommand, useFlowUndo } from "../undo";
import { v4 as uuid } from "uuid";
import { buildPastePlan } from "./pastePlan";

/**
 * Custom hook for clipboard operations (copy, cut, paste). Copy and cut are
 * purely local visual marks — not undoable, not persisted. Paste is
 * undoable as a single atomic unit: the operation builds a flat list of
 * `FlowCommand`s (create N nodes + M internal edges, plus for cut: delete
 * touching edges + originals) via the pure `buildPastePlan` helper, then
 * dispatches them as one `compositeCommand` through the undo manager.
 */
export const useClipboardOperations = (nodeId?: string) => {
  const { setNodes } = useReactFlow();
  const { copyNode, cutNode, clearClipboard, hasClipboardData } =
    useClipboardStore();
  const { manager: undoManager } = useFlowUndo();

  // Paste goes through the undo manager, which dispatches synchronously;
  // the persister handles network I/O on its own queue. The flag stays
  // around to gate the UI against rapid double-invocations.
  const [isPasting, setIsPasting] = useState(false);

  // --- Queries ---
  const { data: nodesData } = useQuery<GetNodesQuery>(GetNodesDocument);
  const { data: nodeData, loading: nodeLoading } = useQuery<GetNodeByIdQuery>(
    GetNodeByIdDocument,
    {
      variables: { id: nodeId! },
      skip: !nodeId,
      fetchPolicy: "no-cache",
    },
  );
  const { data: edgesData, loading: edgesLoading } =
    useQuery<GetDependencyEdgesQuery>(GetDependencyEdgesDocument, {
      fetchPolicy: "cache-and-network",
    });

  // --- Copy ---
  const handleCopyNode = useCallback(() => {
    if (!nodeId || nodeLoading || !nodeData?.nodeById) return;
    copyNode(nodeData.nodeById as NodeFieldsFragment);
    markTreeInState(nodeId, "markedForCopy", nodesData?.nodes ?? [], setNodes);
  }, [nodeId, nodeData, nodeLoading, nodesData, copyNode, setNodes]);

  // --- Cut ---
  const handleCutNode = useCallback(() => {
    if (!nodeId || nodeLoading || !nodeData?.nodeById) return;
    cutNode(nodeData.nodeById as NodeFieldsFragment);
    markTreeInState(nodeId, "markedForCut", nodesData?.nodes ?? [], setNodes);
  }, [nodeId, nodeData, nodeLoading, nodesData, cutNode, setNodes]);

  // --- Paste ---
  const handlePasteNode = useCallback(
    (pasteTargetParentId?: string) => {
      if (isPasting || edgesLoading || !hasClipboardData()) return;
      setIsPasting(true);
      try {
        const { clipboardData } = useClipboardStore.getState();
        if (!clipboardData) return;

        const { node: originalRootFragment, isCut } = clipboardData;
        const allOriginalAppNodes = mapNodeFieldsFragmentsToAppNodes(
          nodesData?.nodes ?? [],
        );
        const allOriginalAppEdges = mapEdgeFieldsFragmentsToAppEdges(
          edgesData?.dependencyEdges ?? [],
        );
        const clipboardRoot =
          mapNodeFieldsFragmentToAppNode(originalRootFragment);

        const plan = buildPastePlan({
          clipboardRoot,
          clipboardIsCut: isCut,
          allOriginalNodes: allOriginalAppNodes,
          allOriginalEdges: allOriginalAppEdges,
          pasteTargetParentId,
          idGenerator: uuid,
        });

        if (plan.length === 0) return;

        undoManager.dispatch(
          compositeCommand(
            plan,
            isCut ? "Move nodes" : "Paste nodes",
            isCut ? "Restore moved nodes" : "Remove pasted nodes",
          ),
        );

        if (isCut) {
          clearClipboard();
        } else {
          clearCopyMarks(nodesData?.nodes ?? [], setNodes);
        }
      } finally {
        setIsPasting(false);
      }
    },
    [
      isPasting,
      edgesLoading,
      hasClipboardData,
      nodesData,
      edgesData,
      undoManager,
      clearClipboard,
      setNodes,
    ],
  );

  return {
    handleCopyNode,
    handleCutNode,
    handlePasteNode,
    isPasting,
  };
};

// ── Local helpers — purely visual state changes, not part of undo ──────

/** Applies `marker: true` to the given node and every descendant in-place. */
function markTreeInState(
  rootId: string,
  marker: "markedForCopy" | "markedForCut",
  allNodes: readonly NodeFieldsFragment[],
  setNodes: ReturnType<typeof useReactFlow>["setNodes"],
): void {
  const idsToMark = new Set<string>();
  const queue: string[] = [rootId];
  while (queue.length > 0) {
    const current = queue.pop()!;
    if (idsToMark.has(current)) continue;
    idsToMark.add(current);
    for (const n of allNodes) {
      if (n.parentId === current) queue.push(n.id);
    }
  }
  setNodes((nodes) =>
    nodes.map((n) =>
      idsToMark.has(n.id) ? { ...n, data: { ...n.data, [marker]: true } } : n,
    ),
  );
}

/** Clears the `markedForCopy` flag from any node that currently carries it. */
function clearCopyMarks(
  allNodes: readonly NodeFieldsFragment[],
  setNodes: ReturnType<typeof useReactFlow>["setNodes"],
): void {
  const markedIds = new Set(
    allNodes
      .map((n) => mapNodeFieldsFragmentToAppNode(n))
      .filter((n) => n.data.markedForCopy)
      .map((n) => n.id),
  );
  if (markedIds.size === 0) return;
  setNodes((nodes) =>
    nodes.map((n) =>
      markedIds.has(n.id)
        ? { ...n, data: { ...n.data, markedForCopy: false } }
        : n,
    ),
  );
}
