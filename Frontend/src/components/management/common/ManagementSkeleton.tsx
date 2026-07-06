import React from "react";
import { MotionCard, MotionListItem } from "../../motion";
import "../../../styles/shimmer.css";

interface ManagementSkeletonProps {
  index: number;
  variant?: "agent" | "skill" | "position" | "scene";
}

export const ManagementSkeleton: React.FC<ManagementSkeletonProps> = ({
  index,
  variant = "agent",
}) => {
  const renderAgentSkeleton = () => (
    <>
      {/* Agent Header */}
      <div className="agent-header p-3 border-bottom">
        <div className="d-flex align-items-center justify-content-between">
          <div className="d-flex align-items-center">
            <div
              className="shimmer skeleton-circle me-3"
              style={{
                width: "32px",
                height: "32px",
              }}
            />
            <div>
              <div className="shimmer skeleton-line skeleton-w-60 mb-2" />
              <div className="shimmer skeleton-line small skeleton-w-33 shimmer-delay-1" />
            </div>
          </div>
          <div className="d-flex gap-1">
            <div className="shimmer skeleton-button shimmer-delay-2" />
            <div className="shimmer skeleton-button shimmer-delay-3" />
          </div>
        </div>
      </div>

      {/* Skills Section */}
      <div className="agent-skills p-3">
        <div className="d-flex align-items-center mb-2">
          <div className="shimmer skeleton-line skeleton-w-25 me-2" />
          <div className="shimmer skeleton-badge skeleton-w-25 ms-auto shimmer-delay-1" />
        </div>
        <div className="d-flex flex-wrap gap-1">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className={`shimmer skeleton-badge skeleton-w-33 shimmer-delay-${i}`}
              style={{ width: "60px", marginBottom: "4px" }}
            />
          ))}
        </div>
      </div>
    </>
  );

  const renderSkillSkeleton = () => (
    <>
      {/* Skill Header */}
      <div className="skill-header p-3 border-bottom">
        <div className="d-flex align-items-center justify-content-between">
          <div className="flex-grow-1">
            <div className="shimmer skeleton-line large skeleton-w-50 mb-2" />
            <div className="shimmer skeleton-line small skeleton-w-75 shimmer-delay-1" />
          </div>
          <div className="d-flex gap-1">
            <div className="shimmer skeleton-button shimmer-delay-2" />
            <div className="shimmer skeleton-button shimmer-delay-3" />
          </div>
        </div>
      </div>

      {/* Properties and Agents */}
      <div className="skill-details p-3">
        <div className="row g-3">
          <div className="col-md-6">
            <div className="d-flex align-items-center mb-2">
              <div className="shimmer skeleton-line skeleton-w-50 me-2" />
              <div className="shimmer skeleton-badge skeleton-w-25 ms-auto shimmer-delay-1" />
            </div>
          </div>
          <div className="col-md-6">
            <div className="d-flex align-items-center mb-2">
              <div className="shimmer skeleton-line skeleton-w-40 me-2" />
              <div className="shimmer skeleton-badge skeleton-w-25 ms-auto shimmer-delay-1" />
            </div>
            <div className="d-flex flex-wrap gap-1 mt-1">
              {[1, 2].map((i) => (
                <div
                  key={i}
                  className={`shimmer skeleton-badge shimmer-delay-${i + 2}`}
                  style={{ width: "50px", marginBottom: "4px" }}
                />
              ))}
            </div>
          </div>
        </div>
      </div>
    </>
  );

  const renderPositionSkeleton = () => (
    <>
      {/* Position Header */}
      <div className="position-header p-3 border-bottom">
        <div className="d-flex align-items-center justify-content-between">
          <div>
            <div className="shimmer skeleton-line large skeleton-w-60" />
          </div>
          <div className="d-flex gap-1">
            <div className="shimmer skeleton-button shimmer-delay-1" />
            <div className="shimmer skeleton-button shimmer-delay-2" />
          </div>
        </div>
      </div>

      {/* Position Details */}
      <div className="position-details p-3">
        <div className="row g-3">
          <div className="col-md-6">
            <div className="shimmer skeleton-line small skeleton-w-50 mb-2" />
            <div className="d-flex gap-2">
              {["X", "Y", "Z"].map((axis, i) => (
                <div
                  key={axis}
                  className={`shimmer skeleton-badge shimmer-delay-${i + 1}`}
                  style={{ width: "40px" }}
                />
              ))}
            </div>
          </div>
          <div className="col-md-6">
            <div className="shimmer skeleton-line small skeleton-w-50 mb-2 shimmer-delay-1" />
            <div className="d-flex gap-2">
              {["α", "β", "γ"].map((angle, i) => (
                <div
                  key={angle}
                  className={`shimmer skeleton-badge shimmer-delay-${i + 2}`}
                  style={{ width: "40px" }}
                />
              ))}
            </div>
          </div>
        </div>
      </div>
    </>
  );

  const renderContent = () => {
    switch (variant) {
      case "skill":
        return renderSkillSkeleton();
      case "position":
      case "scene":
        return renderPositionSkeleton();
      default:
        return renderAgentSkeleton();
    }
  };

  return (
    <MotionListItem index={index}>
      <MotionCard interaction="pronounced" className="mb-3 shadow-sm border">
        {renderContent()}
      </MotionCard>
    </MotionListItem>
  );
};
