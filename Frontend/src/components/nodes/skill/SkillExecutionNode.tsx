import { Node, NodeProps, useEdges } from "@xyflow/react";
import { useMemo } from "react";
import { SkillExecutionBasicData } from "../../../types/nodeTypes.ts";
import SkillExecutionNodeToolbar from "./SkillExecutionNodeToolbar.tsx";
import BaseNode, { nodeBoxWidth } from "../BaseNode.tsx";
import NodeLabel from "../NodeLabel.tsx";

function SkillExecutionNode(
  props: NodeProps<Node<SkillExecutionBasicData, "skillExecutionNode">>,
) {
  const { id, data } = props;
  const edges = useEdges();
  const hasRightEdge = useMemo(
    () => edges.some((e) => e.source === id && e.sourceHandle === "right"),
    [edges, id],
  );

  return (
    <BaseNode
      {...props}
      toolbarContent={<SkillExecutionNodeToolbar nodeId={id} />}
      backgroundColor={
        data.agent ? data.agent.representativeColor : "var(--app-white)"
      }
    >
      {/* Content specific to SkillExecutionNode */}
      <NodeLabel
        name={data.name}
        maxWidth={nodeBoxWidth(data) - 20}
        hasRightEdge={hasRightEdge}
      />
    </BaseNode>
  );
}

export default SkillExecutionNode;
