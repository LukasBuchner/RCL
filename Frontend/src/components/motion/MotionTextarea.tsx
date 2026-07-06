import React from "react";
import { HTMLMotionProps, motion, Variants } from "framer-motion";

/**
 * Textarea animation variants following Bootstrap form conventions
 */
const textareaVariants: Variants = {
  default: {
    scale: 1,
    borderColor: "var(--app-border)",
    boxShadow: "none",
    transition: { duration: 0.15, ease: "easeOut" },
  },
  focus: {
    scale: 1.005,
    borderColor: "var(--app-primary)",
    boxShadow: "var(--motion-shadow-focus)",
    transition: { duration: 0.15, ease: "easeOut" },
  },
  error: {
    scale: 1,
    borderColor: "var(--app-danger)",
    boxShadow: "var(--motion-shadow-focus-danger)",
    transition: { duration: 0.15, ease: "easeOut" },
  },
  success: {
    scale: 1,
    borderColor: "var(--app-success)",
    boxShadow: "var(--motion-shadow-focus-success)",
    transition: { duration: 0.15, ease: "easeOut" },
  },
};

/**
 * Enhanced textarea props that extend Bootstrap Form.Control patterns
 */
interface MotionTextareaProps
  extends Omit<HTMLMotionProps<"textarea">, "variants" | "rows"> {
  /** Bootstrap form control size */
  size?: "sm" | "lg";
  /** Textarea validation state */
  isValid?: boolean;
  /** Textarea error state */
  isInvalid?: boolean;
  /** Whether textarea is disabled */
  disabled?: boolean;
  /** Whether textarea is readonly */
  readOnly?: boolean;
  /** Number of visible text lines */
  rows?: number;
  /** Custom animation variants (overrides default) */
  customVariants?: Variants;
  /** Additional className */
  className?: string;
}

/**
 * Unified motion textarea component that provides consistent form interactions
 * Uses Framer Motion for smooth focus animations and Bootstrap for styling
 */
export const MotionTextarea: React.FC<MotionTextareaProps> = ({
  size,
  isValid = false,
  isInvalid = false,
  disabled = false,
  readOnly = false,
  rows = 3,
  customVariants,
  className = "",
  ...props
}) => {
  // Build Bootstrap classes
  const baseClasses = ["form-control"];

  if (size) {
    baseClasses.push(`form-control-${size}`);
  }

  if (isValid) {
    baseClasses.push("is-valid");
  }

  if (isInvalid) {
    baseClasses.push("is-invalid");
  }

  const finalClassName = `${baseClasses.join(" ")} ${className}`.trim();

  // Determine which variants to use
  const variants = customVariants || textareaVariants;

  // Determine current state
  const getCurrentState = () => {
    if (isInvalid) return "error";
    if (isValid) return "success";
    return "default";
  };

  return (
    <motion.textarea
      className={finalClassName}
      variants={variants}
      initial="default"
      animate={getCurrentState()}
      whileFocus={disabled ? undefined : "focus"}
      disabled={disabled}
      readOnly={readOnly}
      rows={rows}
      {...props}
    />
  );
};
