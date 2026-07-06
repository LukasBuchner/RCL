import React from "react";
import { ApolloError } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { LoadingOverlay } from "../../loading";
import { ErrorState } from "../../error";
import { ManagementPageContent } from "../ManagementPageContent";
import { MotionGrid } from "../../motion";
import { ManagementSkeleton } from "./ManagementSkeleton";

interface ManagementContainerProps {
  // Loading and error states
  loading?: boolean;
  error?: ApolloError;
  isMutating?: boolean;
  mutatingText?: string;

  // Error handling
  errorTitle?: string;
  errorMessage?: string;
  onRetry?: () => void;

  // Header configuration
  header: React.ReactNode;

  // Empty state configuration
  showEmptyState: boolean;
  emptyState: React.ReactNode;

  // Content
  children: React.ReactNode;

  // Styling
  gridClassName?: string;
  containerClassName?: string;

  // Skeleton configuration
  skeletonVariant?: "agent" | "skill" | "position" | "scene";
  skeletonCount?: number;
}

export const ManagementContainer: React.FC<ManagementContainerProps> = ({
  loading = false,
  error,
  isMutating = false,
  mutatingText,
  errorTitle,
  errorMessage,
  onRetry,
  header,
  showEmptyState,
  emptyState,
  children,
  gridClassName = "flex-grow-1",
  containerClassName = "position-relative h-100",
  skeletonVariant = "agent",
  skeletonCount = 3,
}) => {
  const { t } = useTranslation();

  // Show error state if data failed to load
  if (!loading && error && errorTitle && errorMessage) {
    return (
      <ErrorState
        title={errorTitle}
        message={errorMessage}
        severity="error"
        onRetry={onRetry}
        fullScreen={false}
      />
    );
  }

  // Render content - same structure for both loading and loaded states
  const renderContent = () => {
    if (loading) {
      // Show skeleton cards during loading
      return Array.from({ length: skeletonCount }, (_, i) => (
        <ManagementSkeleton key={i} index={i} variant={skeletonVariant} />
      ));
    }
    // Show actual content when loaded
    return children;
  };

  return (
    <div className={containerClassName}>
      <LoadingOverlay
        show={isMutating}
        text={mutatingText || t("loading.processing")}
      />
      <ManagementPageContent
        header={header}
        showEmptyState={!loading && showEmptyState}
        emptyState={emptyState}
      >
        <MotionGrid className={gridClassName}>{renderContent()}</MotionGrid>
      </ManagementPageContent>
    </div>
  );
};
