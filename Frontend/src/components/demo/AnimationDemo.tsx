import React from "react";
import { MotionButton, MotionCard, useMotionSettings } from "../motion";

/**
 * Demo component to showcase animation speed and reduce motion settings
 * This can be added to the settings page or used for testing
 */
export const AnimationDemo: React.FC = () => {
  const { animationSpeed, shouldReduceMotion } = useMotionSettings();

  return (
    <div className="animation-demo p-4 border rounded">
      <h5 className="mb-3">Animation Demo</h5>

      <div className="mb-3">
        <small className="text-muted">
          Current Settings: Speed {animationSpeed}x, Reduce Motion:{" "}
          {shouldReduceMotion() ? "On" : "Off"}
        </small>
      </div>

      <div className="d-flex gap-3 flex-wrap">
        <MotionButton variant="primary">Hover Me</MotionButton>

        <MotionCard
          interaction="pronounced"
          clickable
          className="p-3"
          style={{ minWidth: "150px" }}
        >
          <div className="text-center">
            <i className="bi bi-star text-warning fs-4"></i>
            <div className="mt-1">Interactive Card</div>
          </div>
        </MotionCard>

        <MotionCard
          interaction="subtle"
          className="p-3"
          style={{ minWidth: "150px" }}
        >
          <div className="text-center">
            <i className="bi bi-heart text-danger fs-4"></i>
            <div className="mt-1">Subtle Card</div>
          </div>
        </MotionCard>
      </div>

      <div className="mt-3">
        <p className="small text-muted mb-0">
          Try changing the animation speed or enabling &quot;Reduce Motion&quot;
          in settings to see the difference!
        </p>
      </div>
    </div>
  );
};
