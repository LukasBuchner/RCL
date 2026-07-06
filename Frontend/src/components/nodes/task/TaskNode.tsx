import { Node, NodeProps, useEdges } from "@xyflow/react";
import { useMemo } from "react";
import TaskNodeToolbar from "./TaskNodeToolbar.tsx";
import { TaskBasicData } from "../../../types/nodeTypes.ts"; // Renamed TaskNode to avoid conflict
import BaseNode, { MIN_CONTAINER_WIDTH, nodeBoxWidth } from "../BaseNode.tsx";
import NodeLabel from "../NodeLabel.tsx";

function TaskNode(props: NodeProps<Node<TaskBasicData, "taskNode">>) {
  const { id, parentId, data } = props;
  const edges = useEdges();
  const hasRightEdge = useMemo(
    () => edges.some((e) => e.source === id && e.sourceHandle === "right"),
    [edges, id],
  );

  return (
    <BaseNode
      {...props} // Spread all NodeProps
      toolbarContent={<TaskNodeToolbar nodeId={id} parentId={parentId} />}
      minWidth={MIN_CONTAINER_WIDTH}
    >
      {/* Content specific to TaskNode */}
      <NodeLabel
        name={data.name}
        maxWidth={nodeBoxWidth(data, MIN_CONTAINER_WIDTH) - 20}
        hasRightEdge={hasRightEdge}
      />
    </BaseNode>
  );
}

export default TaskNode;
