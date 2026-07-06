import { NodeFieldsFragment } from "../../../__generated__/graphql";
import {
  AppNode,
  RouterBasicData,
  RouterNode,
  SkillExecutionBasicData,
  SkillExecutionNode,
  TaskBasicData,
  TaskNode,
} from "../../nodeTypes";
import { createLogger } from "../../../utils/logger";
import { MIN_CONTAINER_WIDTH } from "../../../components/nodes/BaseNode";

const log = createLogger("NodeMapper");

/**
 * Floors a backend-provided container width to a minimum so the rendered box stays
 * large enough for dependency edges to attach to. The backend reports an honest width
 * of 0 for an empty (zero-extent) task, router, or branch container; each client applies
 * its own attachable minimum. Leaf widths are sized by their scheduled duration and are
 * not floored here.
 *
 * @param width - The width supplied by the backend, possibly 0, null, or undefined.
 * @returns The width clamped to at least MIN_CONTAINER_WIDTH, or undefined when no width
 *   is supplied so React Flow can fall back to intrinsic sizing.
 */
function floorContainerWidth(
  width: number | null | undefined,
): number | undefined {
  if (width === null || width === undefined) {
    return undefined;
  }
  return Math.max(width, MIN_CONTAINER_WIDTH);
}

// TODO: Remove this once RouterNode type is properly added to GraphQL schema
type RouterNodeFragment = NodeFieldsFragment & {
  __typename: "RouterNode";
  routerTask: {
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean;
    progress?: number;
    selector: { expression: string };
    branches?: Array<{
      name: string;
      condition: string | null;
      priority: number;
      targetNodeId: string;
    }>;
  };
};

/**
 * Maps a single NodeFieldsFragment to the corresponding AppNode.
 */
export function mapNodeFieldsFragmentToAppNode(
  fragment: NodeFieldsFragment | RouterNodeFragment,
): AppNode {
  switch (fragment.__typename) {
    case "TaskNode": {
      const f = fragment;

      log.traceLazy(() => [
        "TaskNode",
        {
          nodeId: f.id,
          nodeName: f.task.name,
          "from backend - parentId": f.parentId,
          "from backend - extent": f.extent,
          "from backend - position": f.position,
          "mapping to parentId": f.parentId ?? undefined,
          "mapping to extent": f.extent ?? undefined,
        },
      ]);

      return {
        id: f.id,
        type: "taskNode" as const,
        position: { x: f.position.x, y: f.position.y },
        parentId: f.parentId ?? undefined,
        extent: f.extent ?? undefined,
        width: floorContainerWidth(f.width),
        height: f.height ?? undefined,
        selectable: f.selectable ?? undefined,
        selected: undefined,
        draggable: f.draggable ?? undefined,
        dragging: undefined,
        hidden: f.hidden ?? undefined,
        data: {
          name: f.task.name,
          description: f.task.description ?? "",
          startTime: f.task.startTime,
          duration: f.task.duration,
          width: floorContainerWidth(f.width),
          isExecuting: f.task.isExecuting,
          progress: f.task.progress ?? undefined,
        } as TaskBasicData,
      } as TaskNode;
    }

    case "SkillExecutionNode": {
      const f = fragment;

      log.traceLazy(() => [
        "SkillExecutionNode",
        {
          nodeId: f.id,
          nodeName: f.skillExecutionTask.name,
          "from backend - parentId": f.parentId,
          "from backend - extent": f.extent,
          "from backend - position": f.position,
          "mapping to parentId": f.parentId ?? undefined,
          "mapping to extent": f.extent ?? undefined,
        },
      ]);

      return {
        id: f.id,
        type: "skillExecutionNode" as const,
        position: { x: f.position.x, y: f.position.y },
        parentId: f.parentId ?? undefined,
        extent: f.extent ?? undefined,
        width: f.width ?? undefined,
        style: {
          color: f.skillExecutionTask.agent.representativeColor,
        },
        height: f.height ?? undefined,
        selectable: f.selectable ?? undefined,
        selected: undefined,
        draggable: f.draggable ?? undefined,
        dragging: undefined,
        hidden: f.hidden ?? undefined,
        data: {
          name: f.skillExecutionTask.name,
          description: f.skillExecutionTask.description ?? "",
          startTime: f.skillExecutionTask.startTime,
          duration: f.skillExecutionTask.duration,
          width: f.width ?? undefined,
          isExecuting: f.skillExecutionTask.isExecuting,
          progress: f.skillExecutionTask.progress ?? undefined,
          skill: {
            id: f.skillExecutionTask.skill.id,
            name: f.skillExecutionTask.skill.name,
            description: f.skillExecutionTask.skill.description,
            agents: f.skillExecutionTask.skill.agents,
          },
          agent: f.skillExecutionTask.agent,
        } as SkillExecutionBasicData,
      } as SkillExecutionNode;
    }

    case "RouterNode": {
      const f = fragment;

      log.traceLazy(() => [
        "RouterNode",
        {
          nodeId: f.id,
          nodeName: f.routerTask.name,
          "from backend - selectedBranchName (execution)":
            f.routerTask.selectedBranchName,
          "from backend - manuallySelectedBranch (design)":
            f.routerTask.manuallySelectedBranch,
          "mapping to data.selectedBranch":
            f.routerTask.selectedBranchName ??
            f.routerTask.manuallySelectedBranch ??
            undefined,
          "mapping to data.manuallySelectedBranch":
            f.routerTask.manuallySelectedBranch ?? undefined,
        },
      ]);

      return {
        id: f.id,
        type: "routerNode" as const,
        position: { x: f.position.x, y: f.position.y },
        parentId: f.parentId ?? undefined,
        extent: f.extent ?? undefined,
        width: floorContainerWidth(f.width),
        height: f.height ?? undefined,
        selectable: f.selectable ?? undefined,
        selected: undefined,
        draggable: f.draggable ?? undefined,
        dragging: undefined,
        hidden: f.hidden ?? undefined,
        data: {
          name: f.routerTask.name,
          description: f.routerTask.description ?? "",
          startTime: f.routerTask.startTime,
          duration: f.routerTask.duration,
          width: floorContainerWidth(f.width),
          isExecuting: f.routerTask.isExecuting ?? false,
          progress: f.routerTask.progress ?? undefined,
          selector: f.routerTask.selector,
          branches: f.routerTask.branches ?? [],
          selectedBranch:
            f.routerTask.selectedBranchName ??
            f.routerTask.manuallySelectedBranch ??
            undefined,
          manuallySelectedBranch:
            f.routerTask.manuallySelectedBranch ?? undefined,
        } as RouterBasicData,
      } as RouterNode;
    }

    default:
      throw new Error(`Unknown node type: ${fragment}`);
  }
}

/**
 * Sorts nodes topologically so that parent nodes come before their children.
 * React Flow requires this ordering for proper parent-child relationships.
 */
function sortNodesParentsFirst(nodes: AppNode[]): AppNode[] {
  // Build a map of node id to node for quick lookup
  const nodeMap = new Map<string, AppNode>();
  nodes.forEach((node) => nodeMap.set(node.id, node));

  // Calculate depth for each node (how many ancestors it has)
  const getDepth = (
    node: AppNode,
    visited: Set<string> = new Set(),
  ): number => {
    if (!node.parentId) return 0;
    if (visited.has(node.id)) return 0; // Prevent infinite loops
    visited.add(node.id);

    const parent = nodeMap.get(node.parentId);
    if (!parent) return 0;
    return 1 + getDepth(parent, visited);
  };

  // Sort by depth (parents first, then children)
  return [...nodes].sort((a, b) => getDepth(a) - getDepth(b));
}

function markRouterChildNodes(nodes: AppNode[]): AppNode[] {
  const routerIds = new Set(
    nodes.filter((node) => node.type === "routerNode").map((node) => node.id),
  );

  return nodes.map((node) => {
    if (
      node.type !== "taskNode" ||
      !node.parentId ||
      !routerIds.has(node.parentId)
    ) {
      return node;
    }

    return {
      ...node,
      data: {
        ...(node.data as TaskBasicData),
        hideHandles: true,
        isRouterChild: true,
      },
    };
  });
}

/**
 * Maps an array of NodeFieldsFragments to AppNodes using the helper.
 * Nodes are sorted so that parent nodes come before their children (required by React Flow).
 */
export function mapNodeFieldsFragmentsToAppNodes(
  fragments: NodeFieldsFragment[],
): AppNode[] {
  const mappedNodes = fragments.map(mapNodeFieldsFragmentToAppNode);
  const nodesWithRouterHandles = markRouterChildNodes(mappedNodes);
  return sortNodesParentsFirst(nodesWithRouterHandles);
}
