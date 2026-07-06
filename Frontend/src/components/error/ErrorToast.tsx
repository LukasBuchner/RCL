import React from "react";
import { Toast, ToastContainer } from "react-bootstrap";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { useError } from "../../contexts/ErrorContext";

const severityConfig = {
  info: {
    bg: "info",
    icon: "bi-info-circle",
  },
  warning: {
    bg: "warning",
    icon: "bi-exclamation-triangle",
  },
  error: {
    bg: "danger",
    icon: "bi-x-circle",
  },
  critical: {
    bg: "danger",
    icon: "bi-shield-exclamation",
  },
} as const;

export const ErrorToastContainer: React.FC = () => {
  const { t } = useTranslation();
  const { errors, removeError } = useError();

  // Only show toast errors (non-critical)
  const toastErrors = errors.filter((error) => error.severity !== "critical");

  return (
    <ToastContainer position="top-end" className="p-3" style={{ zIndex: 1050 }}>
      <AnimatePresence>
        {toastErrors.map((error) => (
          <motion.div
            key={error.id}
            initial={{ opacity: 0, x: 100 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: 100 }}
            transition={{ type: "spring", stiffness: 500, damping: 30 }}
          >
            <Toast
              onClose={() => removeError(error.id)}
              delay={error.severity === "error" ? 10000 : 5000}
              autohide
              bg={severityConfig[error.severity].bg}
            >
              <Toast.Header>
                <i
                  className={`${severityConfig[error.severity].icon} me-2`}
                ></i>
                <strong className="me-auto">
                  {error.severity === "info"
                    ? t("errors.info")
                    : error.severity === "warning"
                      ? t("errors.warning")
                      : t("errors.error")}
                </strong>
                <small>{new Date(error.timestamp).toLocaleTimeString()}</small>
              </Toast.Header>
              <Toast.Body
                className={
                  error.severity === "error" || error.severity === "critical"
                    ? "text-white"
                    : ""
                }
              >
                {error.message}
                {error.retry && (
                  <div className="mt-2">
                    <button
                      className="btn btn-sm btn-outline-light"
                      onClick={() => {
                        error.retry?.();
                        removeError(error.id);
                      }}
                    >
                      <i className="bi bi-arrow-clockwise me-1"></i>
                      {t("actions.retry")}
                    </button>
                  </div>
                )}
              </Toast.Body>
            </Toast>
          </motion.div>
        ))}
      </AnimatePresence>
    </ToastContainer>
  );
};
