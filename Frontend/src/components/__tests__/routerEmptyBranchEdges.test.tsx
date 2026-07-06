import { render } from "@testing-library/react";
import { ReactFlow, ReactFlowProvider, type Edge } from "@xyflow/react";
import { MockedProvider } from "@apollo/client/testing";
import { mapNodeFieldsFragmentsToAppNodes } from "../../types/mapping/node/mapNodeFieldsFragmentsToAppNodes";
import { NodeFieldsFragment } from "../../__generated__/graphql";
import { AppNode, TaskBasicData } from "../../types/nodeTypes";
import TaskNode from "../nodes/task/TaskNode";
import SkillExecutionNode from "../nodes/skill/SkillExecutionNode";
import RouterNode from "../nodes/router/RouterNode";

// The render path the app uses (Flow.tsx).
const nodeTypes = {
  taskNode: TaskNode,
  skillExecutionNode: SkillExecutionNode,
  routerNode: RouterNode,
};

// NOTE ON SCOPE (verified empirically below): jsdom has no layout engine, so React Flow never measures
// nodes (ResizeObserver is a no-op) and therefore renders NO edge DOM at all — a normal skill->skill flow
// produces zero `.react-flow__edge` elements just like the router case. Edge *drawing* is consequently not
// assertable here; it must be checked in a real browser (Playwright) or the live app. What IS reliable in
// jsdom, and what these tests assert, is (1) the node mapping and (2) that every handle the two router
// edges anchor on is rendered — i.e. the frontend gives React Flow valid anchors for `fu->fgh` and
// `fgh->sdf`. If the edges still fail to draw with all anchors present, the cause is React Flow geometry
// (the zero-width router), not a missing/mis-identified handle or the mapping.

function skillFragment(
  id: string,
  name: string,
  width: number,
): NodeFieldsFragment {
  return {
    __typename: "SkillExecutionNode",
    id,
    position: { x: 0, y: 0 },
    parentId: null,
    extent: null,
    width,
    height: 50,
    selectable: true,
    selected: false,
    draggable: true,
    dragging: false,
    hidden: false,
    skillExecutionTask: {
      name,
      description: "",
      startTime: 0,
      duration: width / 100,
      isExecuting: false,
      progress: 0,
      skill: { id: `${id}-skill`, name, description: "", agents: [] },
      agent: {
        id: `${id}-agent`,
        name: "Agent",
        representativeColor: "#3366cc",
      },
    },
  } as unknown as NodeFieldsFragment;
}

function routerFragment(id: string, name: string): NodeFieldsFragment {
  // Empty selected branch -> backend reports honest width 0 and duration 0.
  return {
    __typename: "RouterNode",
    id,
    position: { x: 0, y: 200 },
    parentId: null,
    extent: null,
    width: 0,
    height: 116,
    selectable: true,
    selected: false,
    draggable: true,
    dragging: false,
    hidden: false,
    routerTask: {
      name,
      description: "",
      startTime: 0,
      duration: 0,
      isExecuting: false,
      progress: 0,
      selector: { __typename: "SimpleVariableSelector", expression: "x" },
      selectedBranchName: "Default",
      manuallySelectedBranch: "Default",
      branches: [
        {
          name: "Default",
          condition: "",
          priority: 999,
          targetNodeId: "branch-1",
        },
      ],
    },
  } as unknown as NodeFieldsFragment;
}

function emptyBranchTaskFragment(
  id: string,
  name: string,
  parentId: string,
): NodeFieldsFragment {
  return {
    __typename: "TaskNode",
    id,
    position: { x: 0, y: 56 },
    parentId,
    extent: "parent",
    width: 0,
    height: 50,
    selectable: true,
    selected: false,
    draggable: true,
    dragging: false,
    hidden: false,
    task: {
      name,
      description: "",
      startTime: 0,
      duration: 0,
      isExecuting: false,
      progress: 0,
    },
  } as unknown as NodeFieldsFragment;
}

function fsEdge(source: string, target: string): Edge {
  return {
    id: `${source}->${target}`,
    source,
    target,
    sourceHandle: "right",
    targetHandle: "left",
  };
}

function renderFlow(nodes: AppNode[], edges: Edge[]) {
  return render(
    <MockedProvider mocks={[]}>
      <div style={{ width: 1200, height: 800 }}>
        <ReactFlowProvider>
          <ReactFlow
            nodes={nodes}
            edges={edges}
            nodeTypes={nodeTypes}
            fitView
          />
        </ReactFlowProvider>
      </div>
    </MockedProvider>,
  );
}

function handlesOf(container: HTMLElement, nodeId: string) {
  return Array.from(
    container.querySelectorAll(`[data-id="${nodeId}"] .react-flow__handle`),
  ).map((h) => ({
    handleId: h.getAttribute("data-handleid"),
    isSource: h.className.includes("source"),
    isTarget: h.className.includes("target"),
  }));
}

describe("Router with empty selected branch — node mapping", () => {
  it("does NOT mark the router itself with hideHandles; only its empty branch child", () => {
    const mapped = mapNodeFieldsFragmentsToAppNodes([
      routerFragment("fgh", "fgh"),
      emptyBranchTaskFragment("branch-1", "Default Branch", "fgh"),
    ]);

    const router = mapped.find((n) => n.id === "fgh")!;
    const branch = mapped.find((n) => n.id === "branch-1")!;

    expect(router.type).toBe("routerNode");
    // The router must keep renderable handles — its in/out edges anchor on them.
    expect((router.data as { hideHandles?: boolean }).hideHandles).not.toBe(
      true,
    );

    // The empty branch child (one layer deeper) is the only node marked to hide handles.
    expect((branch.data as TaskBasicData).hideHandles).toBe(true);
    expect(
      (branch.data as TaskBasicData & { isRouterChild?: boolean })
        .isRouterChild,
    ).toBe(true);
    expect(branch.parentId).toBe("fgh");
  });
});

describe("Router with empty selected branch — React Flow render", () => {
  it("renders every handle the router's in/out edges anchor on (router width 0)", () => {
    const nodes = mapNodeFieldsFragmentsToAppNodes([
      skillFragment("fu", "fu", 4000),
      routerFragment("fgh", "fgh"),
      emptyBranchTaskFragment("branch-1", "Default Branch", "fgh"),
      skillFragment("sdf", "sdf", 5500),
    ]);
    const edges = [fsEdge("fu", "fgh"), fsEdge("fgh", "sdf")];

    const { container } = renderFlow(nodes, edges);

    const fu = handlesOf(container, "fu");
    const fgh = handlesOf(container, "fgh");
    const sdf = handlesOf(container, "sdf");

    // eslint-disable-next-line no-console
    console.log("HANDLES fu:", JSON.stringify(fu));
    // eslint-disable-next-line no-console
    console.log("HANDLES fgh (router, width 0):", JSON.stringify(fgh));
    // eslint-disable-next-line no-console
    console.log("HANDLES sdf:", JSON.stringify(sdf));

    // The router node itself renders.
    expect(
      container.querySelector('.react-flow__node[data-id="fgh"]'),
    ).not.toBeNull();

    // fu -> fgh anchors: fu's right (source) and fgh's left (target).
    expect(fu.some((h) => h.handleId === "right" && h.isSource)).toBe(true);
    expect(fgh.some((h) => h.handleId === "left" && h.isTarget)).toBe(true);

    // fgh -> sdf anchors: fgh's right (source) and sdf's left (target).
    expect(fgh.some((h) => h.handleId === "right" && h.isSource)).toBe(true);
    expect(sdf.some((h) => h.handleId === "left" && h.isTarget)).toBe(true);
  });
});
