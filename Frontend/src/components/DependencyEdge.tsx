import {
  BaseEdge,
  EdgeLabelRenderer,
  EdgeProps,
  getSmoothStepPath,
  useReactFlow,
} from "@xyflow/react";
import React, { useCallback, useEffect, useRef } from "react";
import { useMutation } from "@apollo/client";
import {
  DeleteDependencyEdgeDocument,
  DeleteDependencyEdgeInput,
  DeleteDependencyEdgeMutation,
  DeleteDependencyEdgeMutationVariables,
} from "../__generated__/graphql.ts";
import { MotionButton } from "./motion";
import { useTranslation } from "react-i18next";
import { useSettingsStore } from "../stores/settingsStore";
import { createLogger } from "../utils/logger";

const log = createLogger("DependencyEdge");

// Bootstrap tooltip type definitions
interface BootstrapTooltipInstance {
  dispose: () => void;
  hide: () => void;
  show: () => void;
  toggle: () => void;
}

interface BootstrapTooltipConstructor {
  new (
    element: Element,
    options?: Record<string, unknown>,
  ): BootstrapTooltipInstance;

  getInstance(element: Element): BootstrapTooltipInstance | null;
}

interface BootstrapNamespace {
  Tooltip?: BootstrapTooltipConstructor;
}

/**
 * Derives the dependency type abbreviation from source and target handle IDs.
 * Mirrors the backend EdgeTypeMapper logic (right/left handle pairs).
 */
function getDependencyTypeLabel(
  sourceHandleId?: string | null,
  targetHandleId?: string | null,
): string {
  const source = sourceHandleId?.trim().toLowerCase();
  const target = targetHandleId?.trim().toLowerCase();

  if (source === "right" && target === "left") return "FS";
  if (source === "left" && target === "left") return "SS";
  if (source === "left" && target === "right") return "SF";
  if (source === "right" && target === "right") return "FF";

  return "FS"; // Default: Finish-to-Start
}

/** Whether the source handle represents the Finish end of the source task. */
function isSourceFinish(handleId?: string | null): boolean {
  return (handleId?.trim().toLowerCase() ?? "right") === "right";
}

/** Whether the target handle represents the Start end of the target task. */
function isTargetStart(handleId?: string | null): boolean {
  return (handleId?.trim().toLowerCase() ?? "left") === "left";
}

export default function DependencyEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  sourceHandleId,
  targetHandleId,
  style: baseStyle = {},
  selected,
}: EdgeProps) {
  const { t } = useTranslation();
  const showEdgeLabels = useSettingsStore(
    (state) => state.settings.appearance.showEdgeLabels,
  );
  const toolbarRef = useRef<HTMLDivElement>(null);
  const tooltipInstanceRef = useRef<BootstrapTooltipInstance | null>(null);

  // Initialize tooltips with a small delay for EdgeLabelRenderer
  useEffect(() => {
    if (selected && toolbarRef.current) {
      const timer = setTimeout(() => {
        const bootstrap = (
          window as Window & { bootstrap?: BootstrapNamespace }
        ).bootstrap;
        if (bootstrap?.Tooltip) {
          const tooltipElement = toolbarRef.current?.querySelector(
            '[data-bs-toggle="tooltip"]',
          );
          if (
            tooltipElement &&
            !bootstrap.Tooltip.getInstance(tooltipElement)
          ) {
            tooltipInstanceRef.current = new bootstrap.Tooltip(tooltipElement);
          }
        }
      }, 10);

      return () => {
        clearTimeout(timer);
        // Clean up tooltip when component unmounts or selection changes
        if (tooltipInstanceRef.current) {
          tooltipInstanceRef.current.dispose();
          tooltipInstanceRef.current = null;
        }
      };
    } else {
      // Clean up tooltip when not selected
      if (tooltipInstanceRef.current) {
        tooltipInstanceRef.current.dispose();
        tooltipInstanceRef.current = null;
      }
    }
  }, [selected]);

  // Clean up tooltip on component unmount
  useEffect(() => {
    return () => {
      if (tooltipInstanceRef.current) {
        tooltipInstanceRef.current.dispose();
        tooltipInstanceRef.current = null;
      }
    };
  }, []);

  const [edgePath, labelX, labelY] = getSmoothStepPath({
    borderRadius: 30,
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const [deleteEdge] = useMutation<DeleteDependencyEdgeMutation>(
    DeleteDependencyEdgeDocument,
  );

  const { setEdges } = useReactFlow();

  const onDeleteClick = useCallback(async () => {
    // Clean up tooltip before deleting edge
    if (tooltipInstanceRef.current) {
      tooltipInstanceRef.current.dispose();
      tooltipInstanceRef.current = null;
    }

    setEdges((edges) => edges.filter((edge) => edge.id !== id));

    try {
      await deleteEdge({
        variables: {
          input: { id } as DeleteDependencyEdgeInput,
        } as DeleteDependencyEdgeMutationVariables,
      });
    } catch (error) {
      log.error("Failed to delete edge:", error);
    }
  }, [deleteEdge, setEdges, id]);

  const dependencyTypeLabel = getDependencyTypeLabel(
    sourceHandleId,
    targetHandleId,
  );

  // Define conditional styles
  const edgeStyle = selected
    ? { ...baseStyle, stroke: "var(--app-primary)" }
    : baseStyle;

  const edgeColor =
    (edgeStyle as React.CSSProperties).stroke ??
    "var(--xy-edge-stroke-default)";

  const markerFill = selected ? "var(--app-primary)" : edgeColor;

  // Select source/target marker IDs based on which end of each task is constrained
  const srcFinish = isSourceFinish(sourceHandleId);
  const tgtStart = isTargetStart(targetHandleId);
  const sourceMarkerId = srcFinish ? `srcBar-${id}` : `srcCircle-${id}`;
  const targetMarkerId = tgtStart ? `tgtTriangle-${id}` : `tgtDiamond-${id}`;

  return (
    <>
      <svg>
        <defs>
          {/* Source: Bar (Finish) — perpendicular stop-line, "from the end" */}
          {srcFinish && (
            <marker
              id={`srcBar-${id}`}
              markerWidth="4"
              markerHeight="10"
              refX="2"
              refY="5"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <rect
                x="0.5"
                y="1"
                width="3"
                height="8"
                rx="1"
                fill={markerFill}
              />
            </marker>
          )}

          {/* Source: Circle (Start) — origin dot, "from the beginning" */}
          {!srcFinish && (
            <marker
              id={`srcCircle-${id}`}
              markerWidth="8"
              markerHeight="8"
              refX="4"
              refY="4"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <circle cx="4" cy="4" r="3" fill={markerFill} />
            </marker>
          )}

          {/* Target: Triangle (Start) — standard arrowhead, "into the start" */}
          {tgtStart && (
            <marker
              id={`tgtTriangle-${id}`}
              markerWidth="12"
              markerHeight="12"
              refX="6"
              refY="3"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0,0 L0,6 L6,3 z" fill={markerFill} />
            </marker>
          )}

          {/* Target: Diamond (Finish) — milestone endpoint, "must reach this end" */}
          {!tgtStart && (
            <marker
              id={`tgtDiamond-${id}`}
              markerWidth="12"
              markerHeight="12"
              refX="8"
              refY="4"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0,4 L4,0 L8,4 L4,8 z" fill={markerFill} />
            </marker>
          )}
        </defs>
      </svg>

      <BaseEdge
        path={edgePath}
        markerStart={`url(#${sourceMarkerId})`}
        markerEnd={`url(#${targetMarkerId})`}
        style={edgeStyle}
      />

      <EdgeLabelRenderer>
        <div
          ref={toolbarRef}
          style={{
            position: "absolute",
            transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
            fontSize: 12,
            pointerEvents: "all",
            zIndex: 10000,
          }}
          className="nodrag nopan"
        >
          {selected ? (
            <MotionButton
              variant="danger"
              size="sm"
              onClick={onDeleteClick}
              aria-label={t("tooltips.deleteEdge")}
              data-bs-toggle="tooltip"
              data-bs-placement="top"
              data-bs-title={t("tooltips.deleteEdge")}
            >
              <i className="bi bi-trash3"></i>
            </MotionButton>
          ) : (
            showEdgeLabels && (
              <span
                style={{
                  backgroundColor: "var(--app-card-bg)",
                  border: "1px solid var(--app-border)",
                  borderRadius: 4,
                  padding: "1px 4px",
                  fontSize: 10,
                  fontWeight: 600,
                  color: "var(--app-text-muted)",
                  userSelect: "none",
                }}
              >
                {dependencyTypeLabel}
              </span>
            )
          )}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}
