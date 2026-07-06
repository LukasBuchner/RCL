import { Spinner, ProgressBar } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionContainer } from "../motion";
import { motion } from "framer-motion";

interface LoadingStateProps {
  text?: string;
  progress?: number;
  size?: "sm" | "md" | "lg";
  fullScreen?: boolean;
  overlay?: boolean;
}

const sizeConfig = {
  sm: {
    spinnerSize: "",
    fontSize: "small",
    progressHeight: 8,
  },
  md: {
    spinnerSize: "",
    fontSize: "base",
    progressHeight: 12,
  },
  lg: {
    spinnerSize: "lg",
    fontSize: "h5",
    progressHeight: 16,
  },
};

export default function LoadingState({
  text,
  progress,
  size = "md",
  fullScreen = true,
  overlay = false,
}: LoadingStateProps) {
  const { t } = useTranslation();
  const config = sizeConfig[size];

  // Use translation as default if not provided
  const displayText = text || t("loading.loading");

  const content = (
    <div className="text-center">
      {progress === undefined ? (
        <Spinner
          animation="border"
          variant="primary"
          className={config.spinnerSize}
        />
      ) : (
        <div style={{ width: size === "lg" ? "300px" : "200px" }}>
          <ProgressBar
            now={progress}
            animated
            striped
            style={{ height: config.progressHeight }}
          />
          <small className="text-muted mt-1">{Math.round(progress)}%</small>
        </div>
      )}
      <div className={`mt-3 text-muted ${config.fontSize}`}>{displayText}</div>
    </div>
  );

  if (overlay) {
    return (
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        className="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
        style={{
          backgroundColor: "var(--app-loading-overlay)",
          zIndex: 1040,
          backdropFilter: "blur(2px)",
        }}
      >
        {content}
      </motion.div>
    );
  }

  if (fullScreen) {
    return (
      <MotionContainer className="d-flex flex-column align-items-center justify-content-center flex-grow-1 py-5">
        {content}
      </MotionContainer>
    );
  }

  return content;
}
