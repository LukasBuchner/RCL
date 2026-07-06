import { Handle, Node, NodeProps, NodeToolbar, Position } from "@xyflow/react";
import { ReactNode } from "react";
import { motion } from "framer-motion";
import { BaseNodeData } from "../../types/nodeTypes";
import { InteractiveComponentProps } from "../../types/componentTypes";
import {
  getNodeVariant,
  nodeStateVariants,
  nodeVariants,
  toolbarVariants,
} from "../../styles/motionVariants";
import ExecutionProgressBar from "../progress/ExecutionProgressBar";
import { useShallow } from "zustand/react/shallow";
import { useConnectionStore } from "../../stores/connectionStore";
import { useValidationStore } from "../../stores/validationStore";
import { acceptsRightTargetHandle } from "../../utils/connectionValidation";

// Container nodes (Task, Router) keep a minimum usable box width so a zero-extent container stays
// visible and clickable. The backend reports an honest width of 0 for an empty container, and each
// client applies its own minimum rather than the backend baking a presentation constant into the model.
export const MIN_CONTAINER_WIDTH = 120;

// Container nodes floor their box width at minWidth; leaf nodes size to their scheduled duration.
export function nodeBoxWidth(
  data: Pick<BaseNodeData, "width" | "duration">,
  minWidth?: number,
): number {
  return minWidth !== undefined
    ? Math.max(data.width ?? 0, minWidth)
    : (data.width ?? data.duration);
}

// Define the additional props BaseNode needs
interface BaseNodeAdditionalProps
  extends Pick<InteractiveComponentProps, "onClick" | "onHover"> {
  children: ReactNode;
  toolbarContent: ReactNode;
  backgroundColor?: string;
  minWidth?: number;
}

function BaseNode<T extends BaseNodeData = BaseNodeData>({
  id,
  type,
  selected,
  data,
  height,
  children,
  toolbarContent,
  backgroundColor,
  minWidth,
  onClick,
  onHover,
}: NodeProps<Node<T>> & BaseNodeAdditionalProps) {
  // Connection-drag state (progressive disclosure, mirroring the Android app):
  //   - Idle            → every handle is visible so the user can initiate drags.
  //   - Drag in flight  → only the drag-origin source handle and legal drop targets remain.
  const connection = useConnectionStore(
    useShallow((state) => ({
      isConnecting: state.validTargetNodeIds !== null,
      isSourceNode: state.sourceNodeId === id,
      isValidTarget:
        state.validTargetNodeIds !== null && state.validTargetNodeIds.has(id),
      sourceHandle: state.sourceHandle,
    })),
  );

  const hasWarning = useValidationStore((state) =>
    state.warningNodeIds.has(id),
  );

  const shouldHideAllHandles = data.hideHandles ?? false;
  const rightTargetBlocked = !acceptsRightTargetHandle(type);

  // Per-handle visibility. When not connecting, show everything except
  // target handles forbidden by Rules 8/9 (overlap the source handle visually,
  // but React Flow must not treat them as drop targets).
  const showLeftTarget =
    !shouldHideAllHandles &&
    (!connection.isConnecting ? true : connection.isValidTarget);
  const showRightTarget =
    !shouldHideAllHandles &&
    !rightTargetBlocked &&
    (!connection.isConnecting ? true : connection.isValidTarget);
  const showLeftSource =
    !shouldHideAllHandles &&
    (!connection.isConnecting
      ? true
      : connection.isSourceNode && connection.sourceHandle === "left");
  const showRightSource =
    !shouldHideAllHandles &&
    (!connection.isConnecting
      ? true
      : connection.isSourceNode && connection.sourceHandle === "right");

  // Determine the current state variant
  const stateVariant = getNodeVariant(
    data.isExecuting || false,
    false, // disabled - could be added later
    selected || false,
    data.markedForCut || false,
    data.markedForCopy || false,
    hasWarning,
  );

  // Handle hover state changes
  const handleHoverStart = () => {
    onHover?.(true);
  };

  const handleHoverEnd = () => {
    onHover?.(false);
  };

  return (
    <motion.div
      className="card"
      variants={nodeVariants}
      initial="default"
      animate={data.isExecuting ? "executing" : "default"}
      whileHover="hover"
      whileTap="tap"
      whileFocus="focus"
      onHoverStart={handleHoverStart}
      onHoverEnd={handleHoverEnd}
      onClick={onClick}
      style={{
        borderRadius: "10px",
        paddingLeft: "10px",
        paddingTop: "12.5px",
        width: `${nodeBoxWidth(data, minWidth)}px`,
        height: `${height}px`,
        position: "relative",
        overflow: "visible",
        backgroundColor: backgroundColor || "var(--app-card-bg)",
        cursor: "pointer",
      }}
    >
      {/* Apply state-specific styling */}
      <motion.div
        variants={nodeStateVariants}
        animate={stateVariant}
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          borderRadius: "10px",
          pointerEvents: "none",
        }}
      />

      {showLeftTarget && (
        <Handle
          type="target"
          position={Position.Left}
          id="left"
          className="border rounded-circle"
          style={{
            width: 15,
            height: 15,
            backgroundColor: "var(--app-surface)",
            borderColor: "var(--app-border)",
          }}
        />
      )}
      {showLeftSource && (
        <Handle
          type="source"
          position={Position.Left}
          id="left"
          className="border rounded-circle"
          style={{
            width: 15,
            height: 15,
            backgroundColor: "var(--app-surface)",
            borderColor: "var(--app-border)",
          }}
        />
      )}

      <NodeToolbar
        position={Position.Top}
        className="card p-2 rounded shadow-sm"
        style={{
          backgroundColor: "var(--app-card-bg)",
          borderColor: "var(--app-border)",
        }}
      >
        <motion.div
          variants={toolbarVariants}
          initial="hidden"
          animate="visible"
        >
          {toolbarContent}
        </motion.div>
      </NodeToolbar>

      {hasWarning && (
        <div
          aria-label="Warning: agent serialization issue"
          title="Two or more skills assigned to the same agent may run at the same time. Add a finish-to-start connection to define execution order."
          style={{
            position: "absolute",
            top: -10,
            right: -10,
            zIndex: 10,
            pointerEvents: "none",
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            width: 22,
            height: 22,
            borderRadius: "50%",
            backgroundColor: "var(--bs-warning-text-emphasis)",
            color: "#fff",
            border: "2px solid #fff",
            boxShadow: "0 2px 6px rgba(0, 0, 0, 0.35)",
            lineHeight: 1,
          }}
        >
          <i
            className="bi bi-exclamation-triangle-fill"
            style={{ fontSize: 11 }}
          />
        </div>
      )}

      {children}

      {/* Progress bar for executing nodes */}
      <ExecutionProgressBar
        progress={data.progress}
        isExecuting={data.isExecuting}
        height={6}
        duration={data.duration ? data.duration * 1000 : undefined} // Convert seconds to milliseconds
        startTime={data.startTime ? data.startTime * 1000 : undefined} // Convert seconds to milliseconds
      />

      {showRightTarget && (
        <Handle
          type="target"
          position={Position.Right}
          id="right"
          className="border rounded-circle"
          style={{
            width: 15,
            height: 15,
            backgroundColor: "var(--app-surface)",
            borderColor: "var(--app-border)",
          }}
        />
      )}
      {showRightSource && (
        <Handle
          type="source"
          position={Position.Right}
          id="right"
          className="border rounded-circle"
          style={{
            width: 15,
            height: 15,
            backgroundColor: "var(--app-surface)",
            borderColor: "var(--app-border)",
          }}
        />
      )}
    </motion.div>
  );
}

export default BaseNode;
