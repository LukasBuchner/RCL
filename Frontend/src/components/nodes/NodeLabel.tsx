import { useLayoutEffect, useRef, useState } from "react";

interface NodeLabelProps {
  /** The text to display */
  name: string;
  /** Maximum width (px) available inside the node for the label */
  maxWidth: number;
  /** Whether to render the name in bold (default: true) */
  bold?: boolean;
  /** Optional font size override (e.g. "0.75rem" for RouterNode) */
  fontSize?: string;
  /** Whether the node has an edge leaving from its right side */
  hasRightEdge?: boolean;
}

/**
 * Renders a node label inside the node when it fits, or to the left of the
 * node when it overflows. Uses a hidden measurement span to detect overflow
 * before the first paint.
 */
function NodeLabel({
  name,
  maxWidth,
  bold = true,
  fontSize,
  hasRightEdge = false,
}: NodeLabelProps) {
  const measureRef = useRef<HTMLSpanElement>(null);
  const [overflows, setOverflows] = useState(false);

  useLayoutEffect(() => {
    if (measureRef.current) {
      setOverflows(measureRef.current.offsetWidth > maxWidth);
    }
  }, [name, maxWidth]);

  const fontWeight = bold ? "bold" : 600;

  return (
    <>
      {/* Hidden span used only for measuring the full text width */}
      <span
        ref={measureRef}
        aria-hidden
        style={{
          position: "absolute",
          visibility: "hidden",
          whiteSpace: "nowrap",
          fontWeight,
          fontSize: fontSize ?? "inherit",
        }}
      >
        {name}
      </span>

      {overflows ? (
        <div
          style={{
            position: "absolute",
            left: "100%",
            top: "50%",
            transform: "translateY(-50%)",
            whiteSpace: "nowrap",
            marginLeft: hasRightEdge ? "34px" : "14px",
            pointerEvents: "none",
            fontWeight,
            fontSize: fontSize ?? "inherit",
            color: "var(--app-text)",
          }}
        >
          {name}
        </div>
      ) : (
        <div
          className="text-truncate"
          style={{
            maxWidth: `${maxWidth}px`,
            fontWeight,
            fontSize: fontSize ?? "inherit",
          }}
        >
          {name}
        </div>
      )}
    </>
  );
}

export default NodeLabel;
