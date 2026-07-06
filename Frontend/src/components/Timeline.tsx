import React, { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useTimelineConfig } from "../hooks/useTimelineConfig.ts";
import { createLogger } from "../utils/logger";

const log = createLogger("Timeline");

// Nice intervals for rounding the tick spacing to human-friendly values
const NICE_INTERVALS = [
  1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000, 10000,
];

function getNiceInterval(rawInterval: number): number {
  if (rawInterval <= 0) return NICE_INTERVALS[0];
  if (rawInterval <= NICE_INTERVALS[0]) return NICE_INTERVALS[0];

  const maxNiceInterval = NICE_INTERVALS[NICE_INTERVALS.length - 1];
  if (rawInterval > maxNiceInterval) {
    const scaledInterval =
      Math.round(rawInterval / maxNiceInterval) * maxNiceInterval;
    return scaledInterval > 0 ? scaledInterval : maxNiceInterval;
  }

  let closestNiceInterval = NICE_INTERVALS[0];
  let minDiff = Math.abs(rawInterval - closestNiceInterval);

  for (const currentNiceInterval of NICE_INTERVALS) {
    const currentDiff = Math.abs(rawInterval - currentNiceInterval);

    if (currentDiff < minDiff) {
      minDiff = currentDiff;
      closestNiceInterval = currentNiceInterval;
    } else if (currentDiff > minDiff && currentNiceInterval > rawInterval) {
      break;
    }
  }
  return closestNiceInterval;
}

const Timeline: React.FC = () => {
  const { t } = useTranslation();
  const { x, zoom, adjustedTickInterval, config } = useTimelineConfig();
  const [componentWidth, setComponentWidth] = useState<number>(0);
  const timelineRef = useRef<HTMLDivElement>(null);

  // Get timeToPixelScale from backend config (default to 1 if not loaded)
  const timeToPixelScale = config?.timeToPixelScale ?? 1;

  /* ----- Resize observer: keep width in sync with container ----- */
  useEffect(() => {
    const timelineElement = timelineRef.current;
    if (!timelineElement) return;

    const resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setComponentWidth(entry.contentRect.width);
      }
    });

    resizeObserver.observe(timelineElement);
    setComponentWidth(timelineElement.offsetWidth);

    return () => resizeObserver.unobserve(timelineElement);
  }, []);

  /* --------------------- Label generation ---------------------- */
  const labels: Array<{ timeValue: number; pixelPosition: number }> = [];

  if (componentWidth > 0 && zoom > 0) {
    // Use the tick interval supplied by the custom hook, then round to the nearest nice value
    const timeUnitInterval = getNiceInterval(adjustedTickInterval);

    if (timeUnitInterval > 0) {
      // World coordinate at the left edge of the viewport
      // Account for timeToPixelScale: pixels = timeUnits * timeToPixelScale * zoom
      const viewStartInTimeUnits = -x / (zoom * timeToPixelScale);
      const firstVisibleTimeValue =
        Math.ceil(viewStartInTimeUnits / timeUnitInterval) * timeUnitInterval;

      // Cap label count to ensure performance
      const maxLabels = Math.min(
        2 * (componentWidth / (timeUnitInterval * timeToPixelScale * zoom)) + 2,
        1000,
      );

      for (
        let count = 0, currentTimeValue = firstVisibleTimeValue;
        count < maxLabels;
        currentTimeValue += timeUnitInterval, count++
      ) {
        // CSS left position: timeValue * timeToPixelScale gives world pixels, then apply viewport transform
        const pixelPosition = currentTimeValue * timeToPixelScale * zoom + x;

        // Cull when we are well beyond the visible area to the right
        if (pixelPosition > componentWidth + 50) {
          if (labels.length > 0 && pixelPosition > 0) break;
        }

        // Push the label if it is within (or near) the viewport bounds; otherwise break when far to the left
        if (pixelPosition >= -50) {
          labels.push({ timeValue: currentTimeValue, pixelPosition });
        } else if (labels.length > 0 && pixelPosition < -componentWidth) {
          break;
        }
      }
    } else {
      log.warn(
        "Timeline: timeUnitInterval is zero or negative; skipping label generation.",
      );
    }
  }

  /* --------------------- Render ---------------------- */
  return (
    <div
      ref={timelineRef}
      className="position-relative w-100 border-bottom py-2 overflow-hidden z-3"
      style={{
        height: "50px",
        backgroundColor: "var(--app-surface)",
        borderBottomColor: "var(--app-border)",
      }}
    >
      <div className="position-relative w-100 h-100">
        {labels.map(({ timeValue, pixelPosition }) => (
          <span
            key={timeValue}
            className="position-absolute translate-middle-x small"
            style={{
              left: `${pixelPosition}px`,
              top: "10px",
              whiteSpace: "nowrap",
              color: "var(--app-text-muted)",
            }}
            title={t("timeline.time", { value: timeValue })}
          >
            {timeValue}
          </span>
        ))}
      </div>
    </div>
  );
};

export default Timeline;
