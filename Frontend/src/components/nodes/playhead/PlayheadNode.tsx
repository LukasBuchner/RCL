import { Node, NodeProps } from "@xyflow/react";
import { PlayheadData } from "../../../types/nodeTypes.ts";

function PlayheadNode({
  height,
  data,
}: NodeProps<Node<PlayheadData, "playheadNode">>) {
  return (
    <div
      style={{
        position: "absolute",
        left: `${data.position}px`,
        top: 0,
        width: "4px",
        height: `${height}px`,
        backgroundColor: "var(--app-primary)",
        zIndex: 1000,
      }}
    />
  );
}

export default PlayheadNode;
