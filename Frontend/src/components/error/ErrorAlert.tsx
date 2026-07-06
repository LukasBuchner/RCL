import React from "react";
import { Alert } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionButton } from "../motion";
import { ErrorSeverity } from "../../types/error/errorTypes";

interface ErrorAlertProps {
  title?: string;
  message?: string;
  severity?: ErrorSeverity;
  onRetry?: () => void;
  onDismiss?: () => void;
  className?: string;
  size?: "sm" | "md" | "lg";
}

const severityConfig = {
  info: {
    variant: "info",
    icon: "bi-info-circle",
  },
  warning: {
    variant: "warning",
    icon: "bi-exclamation-triangle",
  },
  error: {
    variant: "danger",
    icon: "bi-x-circle",
  },
  critical: {
    variant: "danger",
    icon: "bi-shield-exclamation",
  },
} as const;

export const ErrorAlert: React.FC<ErrorAlertProps> = ({
  title,
  message,
  severity = "error",
  onRetry,
  onDismiss,
  className = "",
  size = "md",
}) => {
  const { t } = useTranslation();
  const config = severityConfig[severity];
  const [isRetrying, setIsRetrying] = React.useState(false);

  const resolvedTitle = title ?? t("errors.defaultErrorTitle");
  const resolvedMessage = message ?? t("errors.defaultErrorMessage");

  const handleRetry = async () => {
    if (onRetry) {
      setIsRetrying(true);
      try {
        await onRetry();
      } finally {
        setIsRetrying(false);
      }
    }
  };

  const alertSize =
    size === "sm" ? "small" : size === "lg" ? "large" : undefined;

  return (
    <Alert
      variant={config.variant}
      dismissible={!!onDismiss}
      onClose={onDismiss}
      className={`${className} ${alertSize ? `alert-${alertSize}` : ""}`}
    >
      <div className="d-flex align-items-start">
        <i
          className={`${config.icon} me-2 ${size === "sm" ? "fs-6" : "fs-5"}`}
        ></i>
        <div className="flex-grow-1">
          <Alert.Heading className={size === "sm" ? "fs-6" : "fs-5"}>
            {resolvedTitle}
          </Alert.Heading>
          <p className={`mb-0 ${size === "sm" ? "small" : ""}`}>
            {resolvedMessage}
          </p>

          {onRetry && (
            <div className="mt-2">
              <MotionButton
                variant="outline-danger"
                onClick={handleRetry}
                disabled={isRetrying}
                size={size === "md" ? undefined : size}
              >
                {isRetrying ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2" />
                    {t("common.retrying")}
                  </>
                ) : (
                  <>
                    <i className="bi bi-arrow-clockwise me-2"></i>
                    {t("common.tryAgain")}
                  </>
                )}
              </MotionButton>
            </div>
          )}
        </div>
      </div>
    </Alert>
  );
};
