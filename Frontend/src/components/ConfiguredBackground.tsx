import React from "react";
import { Background, BackgroundVariant } from "@xyflow/react";
import { useTimelineConfig } from "../hooks/useTimelineConfig";

const ConfiguredBackground: React.FC = () => {
  const { adjustedTickInterval } = useTimelineConfig();

  return (
    <Background
      variant={BackgroundVariant.Lines}
      gap={[adjustedTickInterval, 99999]}
      lineWidth={1}
      color="var(--app-border-subtle, rgba(128, 128, 128, 0.15))"
    />
  );
};

export default ConfiguredBackground;
