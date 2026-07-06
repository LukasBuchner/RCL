import React from "react";
import { MotionButton } from "../../motion";

interface ManagementHeaderProps {
  icon: string;
  title: string;
  count?: number;
  addButtonText: string;
  onAddClick: () => void;
  addButtonDisabled?: boolean;
}

export const ManagementHeader: React.FC<ManagementHeaderProps> = ({
  icon,
  title,
  count,
  addButtonText,
  onAddClick,
  addButtonDisabled = false,
}) => {
  return (
    <>
      <div className="d-flex align-items-center">
        <i className={`${icon} text-primary me-2`}></i>
        <h5 className="mb-0 fw-semibold">{title}</h5>
        {typeof count === "number" && count > 0 && (
          <span className="badge bg-primary ms-2">{count}</span>
        )}
      </div>
      <MotionButton
        variant="primary"
        size="sm"
        onClick={onAddClick}
        disabled={addButtonDisabled}
      >
        <i className="bi bi-plus me-1"></i>
        {addButtonText}
      </MotionButton>
    </>
  );
};
