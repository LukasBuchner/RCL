import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook } from "@testing-library/react";
import { Edge } from "@xyflow/react";
import { useFlowPersister } from "../useFlowPersister";
import { FlowPersisterErrorEvent } from "../types";

// Drives the mocked Apollo mutators. The hook calls `useMutation` for each of
// the five operations and gets a tuple back; the tuple's first element is the
// mutator function we control here.
const mutators = {
  createNode: vi.fn(),
  deleteNode: vi.fn(),
  updateNode: vi.fn(),
  createEdge: vi.fn(),
  deleteEdge: vi.fn(),
};

vi.mock("@apollo/client", () => {
  return {
    useMutation: (doc: {
      definitions?: Array<{ name?: { value: string } }>;
    }) => {
      const opName = doc?.definitions?.[0]?.name?.value ?? "";
      if (opName.includes("CreateNode")) return [mutators.createNode];
      if (opName.includes("DeleteNode")) return [mutators.deleteNode];
      if (opName.includes("UpdateNode")) return [mutators.updateNode];
      if (opName.includes("CreateDependencyEdge")) return [mutators.createEdge];
      if (opName.includes("DeleteDependencyEdge")) return [mutators.deleteEdge];
      return [vi.fn()];
    },
  };
});

vi.mock("../../types/mapping/node/mapAppNodeToNodeInput", () => ({
  mapAppNodeToNodeInput: (node: unknown) => node,
}));
vi.mock("../../types/mapping/edge/mapEdgeToDependencyEdgeInput", () => ({
  mapEdgeToDependencyEdgeInput: (edge: unknown) => edge,
}));

function makeEdge(id = "e-1"): Edge {
  return { id, source: "a", target: "b" } as Edge;
}

/**
 * Drains the persister's internal queue. The queue is a Promise chain whose
 * length depends on the number of enqueued operations, so we wait one full
 * macrotask after a generous batch of microtask resolutions to ensure every
 * step in the chain has executed.
 */
async function flushPersisterQueue() {
  for (let i = 0; i < 16; i++) await Promise.resolve();
  await new Promise((resolve) => setTimeout(resolve, 0));
  for (let i = 0; i < 16; i++) await Promise.resolve();
}

describe("useFlowPersister error handling", () => {
  beforeEach(() => {
    Object.values(mutators).forEach((m) => m.mockReset());
  });

  it("invokes onError with CreateEdgeFailed when createEdge mutation rejects", async () => {
    const apolloError = new Error("Cannot create finish-side dependency …");
    mutators.createEdge.mockRejectedValue(apolloError);

    const onError = vi.fn();
    const { result } = renderHook(() => useFlowPersister(onError));
    const edge = makeEdge("edge-fail");

    result.current.persistEdgeCreation(edge);
    await flushPersisterQueue();

    expect(onError).toHaveBeenCalledTimes(1);
    const event = onError.mock.calls[0][0] as FlowPersisterErrorEvent;
    expect(event.operation).toBe("createEdge");
    if (event.operation !== "createEdge") throw new Error("type guard");
    expect(event.edge).toBe(edge);
    expect(event.error.message).toContain("Cannot create finish-side");
  });

  it("does not invoke onError when createEdge mutation resolves", async () => {
    mutators.createEdge.mockResolvedValue({ data: {} });

    const onError = vi.fn();
    const { result } = renderHook(() => useFlowPersister(onError));

    result.current.persistEdgeCreation(makeEdge("edge-ok"));
    await flushPersisterQueue();

    expect(onError).not.toHaveBeenCalled();
  });

  it("invokes onError with DeleteEdgeFailed when deleteEdge mutation rejects", async () => {
    mutators.deleteEdge.mockRejectedValue(new Error("network down"));

    const onError = vi.fn();
    const { result } = renderHook(() => useFlowPersister(onError));

    result.current.persistEdgeDeletion("edge-x");
    await flushPersisterQueue();

    expect(onError).toHaveBeenCalledTimes(1);
    const event = onError.mock.calls[0][0] as FlowPersisterErrorEvent;
    expect(event.operation).toBe("deleteEdge");
    if (event.operation !== "deleteEdge") throw new Error("type guard");
    expect(event.edgeId).toBe("edge-x");
  });

  it("non-Error rejections are wrapped in Error before reaching onError", async () => {
    mutators.createEdge.mockRejectedValue("string-rejection");

    const onError = vi.fn();
    const { result } = renderHook(() => useFlowPersister(onError));

    result.current.persistEdgeCreation(makeEdge());
    await flushPersisterQueue();

    expect(onError).toHaveBeenCalledTimes(1);
    const event = onError.mock.calls[0][0] as FlowPersisterErrorEvent;
    expect(event.error).toBeInstanceOf(Error);
    expect(event.error.message).toBe("string-rejection");
  });

  it("a failed mutation does not break the chain — a later operation still runs", async () => {
    mutators.createEdge.mockRejectedValue(new Error("first fails"));
    mutators.deleteEdge.mockResolvedValue({ data: {} });

    const onError = vi.fn();
    const { result } = renderHook(() => useFlowPersister(onError));

    result.current.persistEdgeCreation(makeEdge("e1"));
    result.current.persistEdgeDeletion("e2");
    await flushPersisterQueue();

    expect(mutators.deleteEdge).toHaveBeenCalledTimes(1);
    expect(onError).toHaveBeenCalledTimes(1);
    expect(
      (onError.mock.calls[0][0] as FlowPersisterErrorEvent).operation,
    ).toBe("createEdge");
  });

  it("works with no onError supplied — a failure is silently logged and chain continues", async () => {
    mutators.createEdge.mockRejectedValue(new Error("silent"));
    mutators.deleteEdge.mockResolvedValue({ data: {} });

    const { result } = renderHook(() => useFlowPersister());

    result.current.persistEdgeCreation(makeEdge());
    result.current.persistEdgeDeletion("e2");
    await flushPersisterQueue();

    expect(mutators.deleteEdge).toHaveBeenCalledTimes(1);
  });
});
