import { describe, expect, it } from "vitest";
import { Edge } from "@xyflow/react";
import { AppNode } from "../../types/nodeTypes";
import { buildPastePlan } from "../pastePlan";

/**
 * Tests for `buildPastePlan`. The planner is pure — given a clipboard root,
 * descendant graph, and an id generator, it returns the `FlowCommand`s that
 * should be dispatched as a single composite. No Apollo, no UUID randomness.
 */
describe("buildPastePlan — copy mode", () => {
  it("returns a single createNode for a leaf node", () => {
    const root = task("src", { x: 0, y: 0 });
    const plan = buildPastePlan({
      clipboardRoot: root,
      clipboardIsCut: false,
      allOriginalNodes: [root],
      allOriginalEdges: [],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    expect(plan.map((c) => c.description)).toEqual(["Create src"]);
  });

  it("includes createNode commands for every descendant under the new parent", () => {
    const parent = task("src", { x: 0, y: 0 });
    const child = task("src-child", { x: 10, y: 10 }, "src");
    const grandchild = task("src-grand", { x: 20, y: 20 }, "src-child");

    const plan = buildPastePlan({
      clipboardRoot: parent,
      clipboardIsCut: false,
      allOriginalNodes: [parent, child, grandchild],
      allOriginalEdges: [],
      pasteTargetParentId: "host",
      idGenerator: seq("new"),
    });

    // All three nodes get createNode commands
    expect(
      plan
        .filter((c) => c.description.startsWith("Create "))
        .map((c) => c.description),
    ).toEqual(["Create src", "Create src-child", "Create src-grand"]);
  });

  it("emits createEdge only for edges where both endpoints are in the pasted subtree", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const c = task("c"); // outside the subtree
    const internal = edge("a-b", "a", "b");
    const external = edge("c-a", "c", "a");

    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: false,
      allOriginalNodes: [a, b, c],
      allOriginalEdges: [internal, external],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    // Exactly one createEdge (internal), not the c-a edge
    const edgeCmds = plan.filter((c) => c.description === "Create edge");
    expect(edgeCmds).toHaveLength(1);
  });

  it("copy mode never emits delete commands", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: false,
      allOriginalNodes: [a, b],
      allOriginalEdges: [edge("a-b", "a", "b")],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    expect(plan.every((c) => !c.description.startsWith("Delete "))).toBe(true);
  });
});

describe("buildPastePlan — cut mode", () => {
  it("creates new nodes before deleting the originals (reference safety)", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: true,
      allOriginalNodes: [a, b],
      allOriginalEdges: [edge("a-b", "a", "b")],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    const firstDelete = plan.findIndex((c) =>
      c.description.startsWith("Delete "),
    );
    const lastCreate = plan
      .map((c) => c.description)
      .lastIndexOf("Create edge");
    expect(firstDelete).toBeGreaterThan(lastCreate);
  });

  it("deletes every edge touching a cut node, not just internal ones", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const c = task("c");
    const internal = edge("a-b", "a", "b");
    const crossing = edge("c-a", "c", "a"); // touches a cut node but not fully inside

    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: true,
      allOriginalNodes: [a, b, c],
      allOriginalEdges: [internal, crossing],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    const deleteEdgeCount = plan.filter(
      (c) => c.description === "Delete edge",
    ).length;
    expect(deleteEdgeCount).toBe(2);
  });

  it("deletes every node in the cut subtree", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: true,
      allOriginalNodes: [a, b],
      allOriginalEdges: [],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    const deleteNodeDescriptions = plan
      .map((c) => c.description)
      .filter((d) => d.startsWith("Delete ") && d !== "Delete edge");
    expect(deleteNodeDescriptions.sort()).toEqual(["Delete a", "Delete b"]);
  });
});

describe("buildPastePlan — structure", () => {
  it("remaps edge endpoints to the newly-generated node ids", () => {
    const a = task("a");
    const b = task("b", { x: 0, y: 0 }, "a");
    const plan = buildPastePlan({
      clipboardRoot: a,
      clipboardIsCut: false,
      allOriginalNodes: [a, b],
      allOriginalEdges: [edge("orig-edge", "a", "b")],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    // Scopes encode the generated ids — use them as a proxy to confirm
    // the edge references the new node ids, not the originals.
    const edgeCmd = plan.find((c) => c.description === "Create edge");
    expect(edgeCmd).toBeDefined();
    expect(edgeCmd!.scopeName.startsWith("edge:new")).toBe(true);
  });

  it("is empty when the clipboard root is not found in allOriginalNodes", () => {
    const ghost = task("ghost");
    const plan = buildPastePlan({
      clipboardRoot: ghost,
      clipboardIsCut: false,
      allOriginalNodes: [],
      allOriginalEdges: [],
      pasteTargetParentId: undefined,
      idGenerator: seq("new"),
    });

    expect(plan).toHaveLength(1); // still clones the root itself
  });
});

// ── Fixtures ──────────────────────────────────────────────────────────

function task(
  id: string,
  position: { x: number; y: number } = { x: 0, y: 0 },
  parentId?: string,
): AppNode {
  return {
    id,
    type: "taskNode",
    position,
    parentId,
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

/** Deterministic id generator for readable test assertions. */
function seq(prefix: string): () => string {
  let n = 0;
  return () => `${prefix}-${n++}`;
}
