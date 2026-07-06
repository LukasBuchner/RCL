import React from "react";
import { MotionCard, MotionListItem } from "../../motion";

interface ManagementCardProps {
  index: number;
  interaction?: "subtle" | "pronounced" | "property";
  children: React.ReactNode;
  className?: string;
}

export const ManagementCard = React.forwardRef<
  HTMLDivElement,
  ManagementCardProps
>(
  (
    {
      index,
      interaction = "pronounced",
      children,
      className = "mb-3 shadow-sm border",
    },
    ref,
  ) => {
    return (
      <MotionListItem index={index} ref={ref}>
        <MotionCard interaction={interaction} className={className}>
          {children}
        </MotionCard>
      </MotionListItem>
    );
  },
);

ManagementCard.displayName = "ManagementCard";
