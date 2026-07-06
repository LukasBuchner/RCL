import React from "react";
import { Spinner } from "react-bootstrap";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";

interface LoadingOverlayProps {
  show: boolean;
  text?: string;
  backdrop?: boolean;
  blur?: boolean;
  spinnerVariant?: string;
}

export const LoadingOverlay: React.FC<LoadingOverlayProps> = ({
  show,
  text,
  backdrop = true,
  blur = true,
  spinnerVariant = "primary",
}) => {
  const { t } = useTranslation();
  const resolvedText = text ?? t("loading.processing");

  return (
    <AnimatePresence>
      {show && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
          className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
          style={{
            backgroundColor: backdrop
              ? "var(--app-loading-backdrop)"
              : "transparent",
            backdropFilter: blur ? "blur(2px)" : "none",
            zIndex: 10,
            pointerEvents: backdrop ? "auto" : "none",
          }}
        >
          <div className="text-center">
            <Spinner animation="border" variant={spinnerVariant} />
            {resolvedText && (
              <div className="mt-2 text-muted small">{resolvedText}</div>
            )}
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};
