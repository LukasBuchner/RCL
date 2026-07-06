import React from "react";
import { Alert } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionButton, MotionContainer } from "../motion";
import { ErrorSeverity } from "../../types/error/errorTypes";

/**
 * Props for the ErrorState component.
 * @property {string} [title] - The error title. Falls back to translation if not provided.
 * @property {string} [message] - The error message. Falls back to translation if not provided.
 * @property {ErrorSeverity} [severity] - The severity level (info, warning, error, critical). Defaults to 'error'.
 * @property {() => void | Promise<void>} [onRetry] - Callback function when the retry button is clicked.
 * @property {() => void} [onDismiss] - Callback function when the alert is dismissed.
 * @property {boolean} [showIcon] - Whether to show the severity icon. Defaults to true.
 * @property {boolean} [fullScreen] - Whether to display in full-screen mode with MotionContainer. Defaults to true.
 * @property {boolean} [overlayMode] - Whether to display as a fixed overlay with dark backdrop. Defaults to false.
 * @property {string} [customIcon] - Custom Bootstrap icon class to override the default severity icon.
 * @property {string} [imageMaxWidth] - Maximum width for the error meme image. Defaults to '300px'.
 * @property {React.ReactNode} [children] - Custom content to display instead of the default message.
 */
interface ErrorStateProps {
  title?: string;
  message?: string;
  severity?: ErrorSeverity;
  onRetry?: () => void | Promise<void>;
  onDismiss?: () => void;
  showIcon?: boolean;
  fullScreen?: boolean;
  overlayMode?: boolean;
  customIcon?: string;
  imageMaxWidth?: string;
  children?: React.ReactNode;
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

/**
 * Displays an error state with customizable severity, icon, and layout options.
 * Supports full-screen mode, overlay mode, and custom content rendering.
 * Shows a retry button when onRetry is provided, and displays an error meme image for critical/error severities.
 * @param {ErrorStateProps} props - Component properties
 * @returns {React.ReactElement} The rendered error state component
 */
export default function ErrorState({
  title,
  message,
  severity = "error",
  onRetry,
  onDismiss,
  showIcon = true,
  fullScreen = true,
  overlayMode = false,
  customIcon,
  imageMaxWidth = "300px",
  children,
}: ErrorStateProps) {
  const { t } = useTranslation();
  const config = severityConfig[severity];
  const [isRetrying, setIsRetrying] = React.useState(false);

  // Use translations as defaults if not provided
  const displayTitle = title || t("common.oopsError");
  const displayMessage = message || t("common.unexpectedError");

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

  const content = (
    <Alert
      variant={config.variant}
      dismissible={!!onDismiss}
      onClose={onDismiss}
    >
      <div className="d-flex align-items-start">
        {showIcon && (
          <i className={`${customIcon || config.icon} fs-3 me-3`}></i>
        )}
        <div className="flex-grow-1">
          <Alert.Heading>{displayTitle}</Alert.Heading>
          {children ? children : <p className="mb-3">{displayMessage}</p>}

          {onRetry && (
            <>
              <hr />
              <div className="d-flex justify-content-end">
                <MotionButton
                  variant="outline-danger"
                  onClick={handleRetry}
                  disabled={isRetrying}
                  size="sm"
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
            </>
          )}
        </div>
      </div>

      {(severity === "critical" || severity === "error") && (
        <div className="mt-3 text-center">
          <img
            src="/LabLineArt.png"
            alt="Error meme"
            className="img-fluid rounded"
            style={{ maxWidth: imageMaxWidth }}
          />
          <p className="mb-0 fst-italic text-muted small mt-2">
            {t("common.technicalSupport")}
          </p>
        </div>
      )}
    </Alert>
  );

  if (overlayMode) {
    return (
      <div
        style={{
          position: "fixed",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: "rgba(0, 0, 0, 0.85)",
          zIndex: 9999,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: "1rem",
        }}
      >
        <div style={{ maxWidth: "600px", width: "100%" }}>{content}</div>
      </div>
    );
  }

  if (fullScreen) {
    return (
      <MotionContainer className="d-flex flex-column align-items-center justify-content-center flex-grow-1 py-5">
        <div style={{ maxWidth: "600px", width: "100%" }}>{content}</div>
      </MotionContainer>
    );
  }

  return content;
}
