import { ComponentPreview, Previews } from "@react-buddy/ide-toolbox";
import { PaletteTree } from "./palette";
import Flow from "../components/Flow.tsx";
import DependencyEdge from "../components/DependencyEdge.tsx";
import { Position } from "@xyflow/system";
import TaskNodeToolbar from "../components/nodes/task/TaskNodeToolbar.tsx";
import PlayheadNode from "../components/nodes/playhead/PlayheadNode.tsx";
import { VariableSelector } from "../components/router/VariableSelector.tsx";

const ComponentPreviews = () => {
  const PlayheadData = {
    position: 0, // Example position, adjust as needed
  };

  return (
    <Previews palette={<PaletteTree />}>
      <ComponentPreview path="/Flow">
        <Flow />
      </ComponentPreview>
      <ComponentPreview path="/DependencyEdge">
        <DependencyEdge
          id="edge-1"
          source="node-1" // Required: source node ID
          target="node-2" // Required: target node ID
          sourceX={1} // Source node X position
          sourceY={1} // Source node Y position
          targetX={5} // Target node X position
          targetY={5} // Target node Y position
          sourcePosition={Position.Right} // Connection position on source node
          targetPosition={Position.Left} // Connection position on target node
        />
      </ComponentPreview>
      <ComponentPreview path="/ComponentPreviews">
        <ComponentPreviews />
      </ComponentPreview>
      <ComponentPreview path="/TaskNodeToolbar">
        <TaskNodeToolbar nodeId={""} parentId={undefined} />
      </ComponentPreview>
      <ComponentPreview path="/PlayheadNode">
        <PlayheadNode
          height={25}
          data={PlayheadData}
          id=""
          type={"playheadNode"}
          dragging={false}
          zIndex={0}
          selectable={false}
          deletable={false}
          selected={false}
          draggable={false}
          isConnectable={false}
          positionAbsoluteX={0}
          positionAbsoluteY={0}
        />
      </ComponentPreview>
      <ComponentPreview path="/VariableSelector">
        <VariableSelector variables={[]} value="" onChange={() => {}} />
      </ComponentPreview>
    </Previews>
  );
};

export default ComponentPreviews;
