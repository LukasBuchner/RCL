import { describe, expect, it, vi } from "vitest";
import { Edge } from "@xyflow/react";
import { AppNode } from "../../types/nodeTypes";
import {
  compositeCommand,
  createEdgeCommand,
  createNodeCommand,
  deleteEdgeCommand,
  deleteNodeCommand,
  moveNodeCommand,
  updateNodeCommand,
} from "../commands";
import { FlowCommandContext, FlowPersister, NoOpFlowPersister } from "../types";

describe("moveNodeCommand", () => {
  it("apply updates the node's position in-place", () => {
    const ctx = makeCtx([task("a", { x: 0, y: 0 })]);
    moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 5 }).apply(ctx);

    expect(ctx.nodes[0].position).toEqual({ x: 10, y: 5 });
  });

  it("inverse reverses the move exactly", () => {
    const ctx = makeCtx([task("a", { x: 0, y: 0 })]);
    const cmd = moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 5 });
    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.nodes[0].position).toEqual({ x: 0, y: 0 });
  });

  it("apply on missing node is a no-op", () => {
    const ctx = makeCtx([task("a", { x: 0, y: 0 })]);
    moveNodeCommand("ghost", { x: 0, y: 0 }, { x: 10, y: 5 }).apply(ctx);

    expect(ctx.nodes[0].position).toEqual({ x: 0, y: 0 });
  });

  it("scopeName encodes the target node", () => {
    const cmd = moveNodeCommand("n-42", { x: 0, y: 0 }, { x: 1, y: 1 });
    expect(cmd.scopeName).toBe("node:n-42");
  });
});

describe("createEdgeCommand / deleteEdgeCommand", () => {
  it("delete-edge apply removes edge and persists deletion", () => {
    const ctx = makeCtx([], [edge("a-b", "a", "b")]);
    deleteEdgeCommand(ctx.edges[0]).apply(ctx);

    expect(ctx.edges).toEqual([]);
    expect(ctx.persisterLog).toEqual(["edge-del:a-b"]);
  });

  it("delete-edge inverse restores edge locally and persists creation", () => {
    const e = edge("a-b", "a", "b");
    const ctx = makeCtx([], [e]);
    const cmd = deleteEdgeCommand(e);
    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.edges).toEqual([e]);
    expect(ctx.persisterLog).toEqual(["edge-del:a-b", "edge-new:a-b"]);
  });

  it("create-edge apply inserts and persists creation", () => {
    const ctx = makeCtx([task("a"), task("b")]);
    const e = edge("a-b", "a", "b");
    createEdgeCommand(e).apply(ctx);

    expect(ctx.edges).toEqual([e]);
    expect(ctx.persisterLog).toEqual(["edge-new:a-b"]);
  });

  it("create-edge inverse deletes", () => {
    const ctx = makeCtx([task("a"), task("b")]);
    const e = edge("a-b", "a", "b");
    const cmd = createEdgeCommand(e);
    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.edges).toEqual([]);
    expect(ctx.persisterLog).toEqual(["edge-new:a-b", "edge-del:a-b"]);
  });

  it("create-edge apply is idempotent when edge already present", () => {
    const e = edge("a-b", "a", "b");
    const ctx = makeCtx([], [e]);
    createEdgeCommand(e).apply(ctx);

    expect(ctx.edges).toEqual([e]);
  });
});

describe("createNodeCommand / deleteNodeCommand", () => {
  it("create-node apply inserts and persists creation", () => {
    const ctx = makeCtx();
    const n = task("n1");

    createNodeCommand(n).apply(ctx);

    expect(ctx.nodes).toEqual([n]);
    expect(ctx.persisterLog).toEqual(["node-new:n1"]);
  });

  it("create-node inverse deletes the node", () => {
    const ctx = makeCtx();
    const n = task("n1");
    const cmd = createNodeCommand(n);
    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.nodes).toEqual([]);
    expect(ctx.persisterLog).toEqual(["node-new:n1", "node-del:n1"]);
  });

  it("create-node round-trip apply→undo→redo restores final state", () => {
    const ctx = makeCtx();
    const n = task("n1");
    const cmd = createNodeCommand(n);
    cmd.apply(ctx);
    const undo = cmd.inverse();
    undo.apply(ctx);
    undo.inverse().apply(ctx);

    expect(ctx.nodes).toEqual([n]);
    expect(ctx.persisterLog).toEqual([
      "node-new:n1",
      "node-del:n1",
      "node-new:n1",
    ]);
  });

  it("delete-node apply removes node plus all incident edges", () => {
    const a = task("a");
    const b = task("b");
    const c = task("c");
    const ab = edge("a-b", "a", "b");
    const bc = edge("b-c", "b", "c");
    const ctx = makeCtx([a, b, c], [ab, bc]);

    deleteNodeCommand(b, [ab, bc]).apply(ctx);

    expect(ctx.nodes.map((n) => n.id)).toEqual(["a", "c"]);
    expect(ctx.edges).toEqual([]);
    expect(ctx.persisterLog).toEqual(["node-del:b"]);
  });

  it("delete-node inverse restores node and incident edges and persists each", () => {
    const a = task("a");
    const b = task("b");
    const c = task("c");
    const ab = edge("a-b", "a", "b");
    const bc = edge("b-c", "b", "c");
    const ctx = makeCtx([a, b, c], [ab, bc]);
    const cmd = deleteNodeCommand(b, [ab, bc]);

    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.nodes.map((n) => n.id).sort()).toEqual(["a", "b", "c"]);
    expect(ctx.edges.map((e) => e.id).sort()).toEqual(["a-b", "b-c"]);
    expect(ctx.persisterLog).toEqual([
      "node-del:b",
      "node-new:b",
      "edge-new:a-b",
      "edge-new:b-c",
    ]);
  });

  it("delete-node with NoOp persister still mutates local state", () => {
    const a = task("a");
    const b = task("b");
    const ab = edge("a-b", "a", "b");
    const ctx: FakeCtx = {
      nodes: [a, b],
      edges: [ab],
      persisterLog: [],
      setNodes: () => {},
      setEdges: () => {},
      persister: NoOpFlowPersister,
    };
    ctx.setNodes = (updater) => {
      ctx.nodes = updater(ctx.nodes);
    };
    ctx.setEdges = (updater) => {
      ctx.edges = updater(ctx.edges);
    };

    deleteNodeCommand(b, [ab]).apply(ctx);

    expect(ctx.nodes.map((n) => n.id)).toEqual(["a"]);
    expect(ctx.edges).toEqual([]);
  });
});

describe("updateNodeCommand", () => {
  it("apply replaces the node in state and persists the update", () => {
    const before = task("n1", { x: 0, y: 0 });
    const after: AppNode = {
      ...before,
      data: { ...before.data, name: "renamed" },
    } as AppNode;
    const ctx = makeCtx([before]);

    updateNodeCommand(before, after).apply(ctx);

    expect(ctx.nodes).toEqual([after]);
    expect(ctx.persisterLog).toEqual(["node-upd:n1"]);
  });

  it("inverse restores the prior node and persists the before image", () => {
    const before = task("n1", { x: 0, y: 0 });
    const after: AppNode = {
      ...before,
      data: { ...before.data, name: "renamed" },
    } as AppNode;
    const ctx = makeCtx([before]);
    const cmd = updateNodeCommand(before, after);

    cmd.apply(ctx);
    cmd.inverse().apply(ctx);

    expect(ctx.nodes).toEqual([before]);
    expect(ctx.persisterLog).toEqual(["node-upd:n1", "node-upd:n1"]);
  });

  it("round-trip apply→undo→redo lands on after", () => {
    const before = task("n1", { x: 0, y: 0 });
    const after: AppNode = {
      ...before,
      data: { ...before.data, name: "renamed" },
    } as AppNode;
    const ctx = makeCtx([before]);
    const cmd = updateNodeCommand(before, after);

    cmd.apply(ctx);
    const undo = cmd.inverse();
    undo.apply(ctx);
    undo.inverse().apply(ctx);

    expect(ctx.nodes).toEqual([after]);
    expect(ctx.persisterLog).toEqual([
      "node-upd:n1",
      "node-upd:n1",
      "node-upd:n1",
    ]);
  });

  it("apply on missing node is a no-op on state but still persists", () => {
    const before = task("ghost", { x: 0, y: 0 });
    const after: AppNode = {
      ...before,
      data: { ...before.data, name: "renamed" },
    } as AppNode;
    const ctx = makeCtx();

    updateNodeCommand(before, after).apply(ctx);

    expect(ctx.nodes).toEqual([]);
    expect(ctx.persisterLog).toEqual(["node-upd:ghost"]);
  });

  it("throws when before and after have different ids", () => {
    const before = task("a");
    const after = task("b");
    expect(() => updateNodeCommand(before, after)).toThrow(/matching ids/);
  });

  it("scopeName is stable across apply and inverse for conflict grouping", () => {
    const before = task("n1");
    const after: AppNode = {
      ...before,
      data: { ...before.data, name: "x" },
    } as AppNode;
    const cmd = updateNodeCommand(before, after);
    expect(cmd.scopeName).toBe(cmd.inverse().scopeName);
  });
});

describe("compositeCommand", () => {
  it("applies children in declared order", () => {
    const ctx = makeCtx([task("a", { x: 0, y: 0 }), task("b", { x: 0, y: 0 })]);
    compositeCommand([
      moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      moveNodeCommand("b", { x: 0, y: 0 }, { x: 0, y: 20 }),
    ]).apply(ctx);

    expect(byId(ctx, "a").position).toEqual({ x: 10, y: 0 });
    expect(byId(ctx, "b").position).toEqual({ x: 0, y: 20 });
  });

  it("inverse reverses order and inverts each", () => {
    const ctx = makeCtx([task("a", { x: 0, y: 0 }), task("b", { x: 0, y: 0 })]);
    const group = compositeCommand([
      moveNodeCommand("a", { x: 0, y: 0 }, { x: 10, y: 0 }),
      moveNodeCommand("b", { x: 0, y: 0 }, { x: 0, y: 20 }),
    ]);
    group.apply(ctx);
    group.inverse().apply(ctx);

    expect(byId(ctx, "a").position).toEqual({ x: 0, y: 0 });
    expect(byId(ctx, "b").position).toEqual({ x: 0, y: 0 });
  });

  it("rejects empty command list", () => {
    expect(() => compositeCommand([])).toThrow(/at least one command/);
  });
});

// ── Fixtures ──────────────────────────────────────────────────────────

interface FakeCtx extends FlowCommandContext {
  nodes: AppNode[];
  edges: Edge[];
  persisterLog: string[];
}

function makeCtx(
  initialNodes: AppNode[] = [],
  initialEdges: Edge[] = [],
): FakeCtx {
  const state: FakeCtx = {
    nodes: [...initialNodes],
    edges: [...initialEdges],
    persisterLog: [],
    setNodes: vi.fn(),
    setEdges: vi.fn(),
    persister: NoOpFlowPersister,
  };
  state.setNodes = (updater) => {
    state.nodes = updater(state.nodes);
  };
  state.setEdges = (updater) => {
    state.edges = updater(state.edges);
  };
  const persister: FlowPersister = {
    persistNodeCreation: (node) =>
      state.persisterLog.push(`node-new:${node.id}`),
    persistNodeDeletion: (id) => state.persisterLog.push(`node-del:${id}`),
    persistNodeUpdate: (node) => state.persisterLog.push(`node-upd:${node.id}`),
    persistEdgeCreation: (edge) =>
      state.persisterLog.push(`edge-new:${edge.id}`),
    persistEdgeDeletion: (id) => state.persisterLog.push(`edge-del:${id}`),
  };
  state.persister = persister;
  return state;
}

function task(id: string, position = { x: 0, y: 0 }): AppNode {
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

function edge(id: string, source: string, target: string): Edge {
  return {
    id,
    source,
    target,
    sourceHandle: "right",
    targetHandle: "left",
    type: "dependencyEdge",
  };
}

function byId(ctx: FakeCtx, id: string): AppNode {
  const node = ctx.nodes.find((n) => n.id === id);
  if (!node) throw new Error(`node ${id} missing from ctx`);
  return node;
}
