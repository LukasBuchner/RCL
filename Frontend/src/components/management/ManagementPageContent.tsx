import React from "react";

interface ManagementPageContentProps {
  /** Header content */
  header: React.ReactNode;
  /** Main content - typically a MotionGrid with AnimatePresence */
  children: React.ReactNode;
  /** Optional empty state component */
  emptyState?: React.ReactNode;
  /** Whether to show the empty state */
  showEmptyState?: boolean;
}

/**
 * Reusable container for management page content with consistent styling
 * Provides proper border-radius, scrolling, and layout structure
 */
export const ManagementPageContent: React.FC<ManagementPageContentProps> = ({
  header,
  children,
  emptyState,
  showEmptyState = false,
}) => {
  return (
    <div
      className="h-100 d-flex flex-column position-relative"
      style={{
        borderRadius: "12px",
        overflow: "hidden",
        backgroundColor: "var(--app-card-bg)",
      }}
    >
      {/* Header */}
      <div
        className="d-flex justify-content-between align-items-center p-3 border border-bottom-0"
        style={{
          backgroundColor: "var(--app-surface)",
          borderTopLeftRadius: "12px",
          borderTopRightRadius: "12px",
          flexShrink: 0,
          borderColor: "var(--app-border)",
          position: "sticky",
          top: 0,
          zIndex: 10,
          boxShadow: "0 2px 4px var(--app-shadow)",
        }}
      >
        {header}
      </div>

      {/* Content Area */}
      <div
        className="flex-grow-1 position-relative border-start border-end border-bottom"
        style={{
          overflow: "auto",
          borderBottomLeftRadius: "12px",
          borderBottomRightRadius: "12px",
          borderColor: "var(--app-border)",
        }}
      >
        {showEmptyState ? emptyState : children}
      </div>
    </div>
  );
};

export default ManagementPageContent;
