import { useCallback, useEffect } from "react";
import { useReactFlow } from "@xyflow/react";
import { useClipboardStore } from "../stores/clipboardStore";
import { useClipboardOperations } from "./useClipboardOperations";
import { useQuery } from "@apollo/client";
import {
  GetNodesDocument,
  GetNodesQuery,
  NodeFieldsFragment,
} from "../__generated__/graphql";

/**
 * Options for {@link useKeyboardShortcuts}. When `onUndo` / `onRedo` are
 * provided the hook binds `Ctrl+Z` / `Ctrl+Shift+Z` (and `Cmd+Z` / `Cmd+Shift+Z`
 * on macOS) in addition to the copy/cut/paste bindings.
 */
export interface KeyboardShortcutOptions {
  onUndo?: () => void;
  onRedo?: () => void;
}

/**
 * Hook for keyboard shortcuts in React Flow components.
 *
 * Binds copy/cut/paste against the clipboard store, and optionally undo/redo
 * against callbacks passed by the caller. All bindings are suppressed while
 * the user is typing in an input, textarea, or contenteditable element.
 */
export const useKeyboardShortcuts = (options?: KeyboardShortcutOptions) => {
  const { getNodes, setNodes } = useReactFlow();
  const { copyNode, cutNode } = useClipboardStore();
  const { onUndo, onRedo } = options ?? {};

  // Queries for clipboard operations
  const { data: nodesData } = useQuery<GetNodesQuery>(GetNodesDocument);

  // Copy operation
  const handleCopy = useCallback(async () => {
    const selectedNodes = getNodes().filter((node) => node.selected);
    if (selectedNodes.length !== 1) return;

    const selectedNodeId = selectedNodes[0].id;
    const nodeData = nodesData?.nodes?.find((n) => n.id === selectedNodeId);

    if (nodeData) {
      copyNode(nodeData as NodeFieldsFragment);
      const allNodes = nodesData?.nodes || [];

      const markNodeAndChildrenForCopy = (nodeIdToMark: string) => {
        setNodes((nodes) =>
          nodes.map((node) => {
            if (node.id === nodeIdToMark) {
              return {
                ...node,
                data: { ...node.data, markedForCopy: true },
              };
            }
            return node;
          }),
        );
        const childNodes = allNodes.filter((n) => n.parentId === nodeIdToMark);
        childNodes.forEach((child) => markNodeAndChildrenForCopy(child.id));
      };
      markNodeAndChildrenForCopy(selectedNodeId);
    }
  }, [getNodes, nodesData, copyNode, setNodes]);

  // Cut operation
  const handleCut = useCallback(async () => {
    const selectedNodes = getNodes().filter((node) => node.selected);
    if (selectedNodes.length !== 1) return;

    const selectedNodeId = selectedNodes[0].id;
    const nodeData = nodesData?.nodes?.find((n) => n.id === selectedNodeId);

    if (nodeData) {
      cutNode(nodeData as NodeFieldsFragment);
      const allNodes = nodesData?.nodes || [];

      const markNodeAndChildrenForCut = (nodeIdToMark: string) => {
        setNodes((nodes) =>
          nodes.map((node) => {
            if (node.id === nodeIdToMark) {
              return {
                ...node,
                data: { ...node.data, markedForCut: true },
              };
            }
            return node;
          }),
        );
        const childNodes = allNodes.filter((n) => n.parentId === nodeIdToMark);
        childNodes.forEach((child) => markNodeAndChildrenForCut(child.id));
      };
      markNodeAndChildrenForCut(selectedNodeId);
    }
  }, [getNodes, nodesData, cutNode, setNodes]);

  // For paste, we need to import the full paste logic or use the existing hook
  const { handlePasteNode } = useClipboardOperations();

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Check if user is typing in an input field
      const target = event.target as HTMLElement;
      if (
        target.tagName === "INPUT" ||
        target.tagName === "TEXTAREA" ||
        target.isContentEditable
      ) {
        return;
      }

      const isMac = navigator.platform.toUpperCase().indexOf("MAC") >= 0;
      const ctrlKey = isMac ? event.metaKey : event.ctrlKey;

      if (ctrlKey && event.key === "c") {
        event.preventDefault();
        handleCopy();
      } else if (ctrlKey && event.key === "x") {
        event.preventDefault();
        handleCut();
      } else if (ctrlKey && event.key === "v") {
        event.preventDefault();
        // Get current selected node for paste target
        const selectedNodes = getNodes().filter((node) => node.selected);
        const parentId =
          selectedNodes.length === 1 ? selectedNodes[0].id : undefined;
        handlePasteNode(parentId);
      } else if (
        ctrlKey &&
        !event.shiftKey &&
        event.key.toLowerCase() === "z"
      ) {
        if (onUndo) {
          event.preventDefault();
          onUndo();
        }
      } else if (
        ctrlKey &&
        (event.key === "y" ||
          (event.shiftKey && event.key.toLowerCase() === "z"))
      ) {
        if (onRedo) {
          event.preventDefault();
          onRedo();
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [handleCopy, handleCut, handlePasteNode, getNodes, onUndo, onRedo]);
};
