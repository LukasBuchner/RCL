import React from "react";
import { motion } from "framer-motion";
import { MotionButton } from "./MotionButton";

/**
 * Empty state animation variants
 */
const emptyStateVariants = {
  hidden: {
    opacity: 0,
    scale: 0.9,
    y: 20,
  },
  visible: {
    opacity: 1,
    scale: 1,
    y: 0,
    transition: {
      duration: 0.4,
      ease: [0.0, 0.0, 0.2, 1.0] as const,
      delay: 0.2,
    },
  },
};

/**
 * Empty state props
 */
interface MotionEmptyStateProps {
  /** Icon class for the empty state */
  icon: string;
  /** Title text */
  title: string;
  /** Description text */
  description: string;
  /** Button text for the call to action */
  buttonText?: string;
  /** Button click handler */
  onButtonClick?: () => void;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Unified empty state component with entrance animation
 * Provides consistent empty state appearance across all lists
 */
export const MotionEmptyState: React.FC<MotionEmptyStateProps> = ({
  icon,
  title,
  description,
  buttonText,
  onButtonClick,
  className = "",
}) => {
  return (
    <motion.div
      className={`empty-state text-center p-4 border rounded ${className}`.trim()}
      variants={emptyStateVariants}
      initial="hidden"
      animate="visible"
    >
      <motion.div
        className="mb-3"
        initial={{ scale: 0 }}
        animate={{ scale: 1 }}
        transition={{ delay: 0.3, type: "spring", stiffness: 200 }}
      >
        <i className={`bi ${icon} h2 text-muted`}></i>
      </motion.div>
      <h6 className="text-muted mb-2">{title}</h6>
      <p className="text-muted small mb-3">{description}</p>
      {buttonText && onButtonClick && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
        >
          <MotionButton variant="primary" onClick={onButtonClick}>
            <i className="bi bi-plus me-1"></i>
            {buttonText}
          </MotionButton>
        </motion.div>
      )}
    </motion.div>
  );
};
