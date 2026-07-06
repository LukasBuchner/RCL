import { useViewport } from "@xyflow/react";
import { useQuery, gql } from "@apollo/client";

const GET_SCHEDULING_CONFIGURATION = gql`
  query GetSchedulingConfiguration {
    schedulingConfiguration {
      timeToPixelScale
      baseYOffset
      siblingSpacing
      containerTopPadding
      containerBottomPadding
      baseHeight
      routerDropdownHeight
    }
  }
`;

export const useTimelineConfig = () => {
  const { x, y, zoom } = useViewport();
  const { data } = useQuery(GET_SCHEDULING_CONFIGURATION);

  const baseTickInterval =
    data?.schedulingConfiguration?.timeToPixelScale ?? 100; // px in world units, fallback to 100

  // Scale the interval inversely with zoom to maintain visual spacing
  // When zoomed out (small zoom), increase interval to show fewer lines
  // When zoomed in (large zoom), decrease interval to show more detail
  let adjustedTickInterval = baseTickInterval / zoom;

  // Clamp the interval to reasonable bounds
  // Min: Don't let intervals get too small (at least 50px spacing)
  // Max: Don't let intervals get too large (at most 400px spacing)
  const minInterval = 50;
  const maxInterval = 400;
  adjustedTickInterval = Math.max(
    minInterval,
    Math.min(maxInterval, adjustedTickInterval),
  );

  return {
    x,
    y,
    zoom,
    adjustedTickInterval,
    config: data?.schedulingConfiguration,
  };
};
