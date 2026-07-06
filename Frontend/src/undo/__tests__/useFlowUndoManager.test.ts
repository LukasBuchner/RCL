import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Edge } from "@xyflow/react";
import React from "react";
import { AppNode } from "../../types/nodeTypes";
import { moveNodeCommand } from "../commands";
import { useFlowUndoManager } from "../useFlowUndoManager";
import { FlowCommandContext, NoOpFlowPersister } from "../types";

describe("useFlowUndoManager", () => {
  it("fresh manager reports nothing to undo or redo", () => {
    const { result } = renderManager();
    expect(result.current.manager.canUndo).toBe(false);
    expect(result.current.manager.canRedo).toBe(false);
    expect(result.current.manager.nextUndoDescription).toBeNull();
    expect(result.current.manager.nextRedoDescription).toBeNull();
  });

  it("dispatch applies command and enables undo", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 5 }),
      );
    });

    expect(result.current.ctx.nodes[0].position).toEqual({ x: 10, y: 5 });
    expect(result.current.manager.canUndo).toBe(true);
    expect(result.current.manager.canRedo).toBe(false);
    expect(result.current.manager.nextUndoDescription).toBe("Move node");
  });

  it("undo reverses the most recent command", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      );
    });
    act(() => {
      result.current.manager.undo();
    });

    expect(result.current.ctx.nodes[0].position).toEqual({ x: 0, y: 0 });
    expect(result.current.manager.canUndo).toBe(false);
    expect(result.current.manager.canRedo).toBe(true);
  });

  it("redo reapplies the most recently undone command", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      );
    });
    act(() => result.current.manager.undo());
    act(() => result.current.manager.redo());

    expect(result.current.ctx.nodes[0].position).toEqual({ x: 10, y: 0 });
    expect(result.current.manager.canUndo).toBe(true);
    expect(result.current.manager.canRedo).toBe(false);
  });

  it("undo is a no-op on empty past stack", () => {
    const { result } = renderManager();
    act(() => result.current.manager.undo());
    expect(result.current.manager.canUndo).toBe(false);
  });

  it("dispatch after undo clears the future stack (branching)", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      );
    });
    act(() => result.current.manager.undo());
    expect(result.current.manager.canRedo).toBe(true);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: -5, y: 0 }),
      );
    });

    expect(result.current.manager.canRedo).toBe(false);
    expect(result.current.ctx.nodes[0].position).toEqual({ x: -5, y: 0 });
  });

  it("traverses multiple commands in LIFO order", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 1, y: 0 }),
      );
    });
    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 1, y: 0 }, { x: 2, y: 0 }),
      );
    });
    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 2, y: 0 }, { x: 3, y: 0 }),
      );
    });

    act(() => result.current.manager.undo());
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 2, y: 0 });
    act(() => result.current.manager.undo());
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 1, y: 0 });
    act(() => result.current.manager.redo());
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 2, y: 0 });
    act(() => result.current.manager.redo());
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 3, y: 0 });
  });

  it("capacity drops oldest past entries when exceeded", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })], [], 2);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 1, y: 0 }),
      );
    });
    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 1, y: 0 }, { x: 2, y: 0 }),
      );
    });
    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 2, y: 0 }, { x: 3, y: 0 }),
      );
    });

    act(() => result.current.manager.undo());
    act(() => result.current.manager.undo());
    expect(result.current.manager.canUndo).toBe(false);
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 1, y: 0 });
  });

  it("clear empties both stacks without touching state", () => {
    const { result } = renderManager([task("a", { x: 0, y: 0 })]);

    act(() => {
      result.current.manager.dispatch(
        moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      );
    });
    act(() => result.current.manager.undo());
    act(() => result.current.manager.clear());

    expect(result.current.manager.canUndo).toBe(false);
    expect(result.current.manager.canRedo).toBe(false);
    expect(result.current.ctx.nodes[0].position).toEqual({ x: 0, y: 0 });
  });
});

// ── Fixture ───────────────────────────────────────────────────────────

interface Harness {
  manager: ReturnType<typeof useFlowUndoManager>;
  ctx: { nodes: AppNode[]; edges: Edge[] };
}

function renderManager(
  initialNodes: AppNode[] = [],
  initialEdges: Edge[] = [],
  capacity?: number,
) {
  return renderHook<Harness, void>(() => {
    const mutableRef = React.useRef({
      nodes: [...initialNodes],
      edges: [...initialEdges],
    });
    const ctxRef = React.useRef<FlowCommandContext>({
      setNodes: (updater) => {
        mutableRef.current.nodes = updater(mutableRef.current.nodes);
      },
      setEdges: (updater) => {
        mutableRef.current.edges = updater(mutableRef.current.edges);
      },
      persister: NoOpFlowPersister,
    });
    const manager = useFlowUndoManager(ctxRef, capacity);
    return { manager, ctx: mutableRef.current };
  });
}

function task(id: string, position: { x: number; y: number }): AppNode {
  return {
    id,
    type: "taskNode",
    position,
    data: {
      name: id,
      description: "",
      startTime: 0,
      duration: 0,
      isExecuting: false,
    },
  } as AppNode;
}
