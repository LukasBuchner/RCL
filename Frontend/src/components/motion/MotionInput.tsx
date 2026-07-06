import React from "react";
import { HTMLMotionProps, motion, Variants } from "framer-motion";

/**
 * Input animation variants following Bootstrap form conventions
 */
const inputVariants: Variants = {
  default: {
    scale: 1,
    borderColor: "var(--app-border)",
    boxShadow: "none",
    transition: { duration: 0.15, ease: "easeOut" },
  },
  focus: {
    scale: 1.01,
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
    borderColor: "var(--bs-success)",
    boxShadow: "var(--motion-shadow-focus-success)",
    transition: { duration: 0.15, ease: "easeOut" },
  },
};

/**
 * Enhanced input props that extend Bootstrap Form.Control patterns
 */
interface MotionInputProps
  extends Omit<HTMLMotionProps<"input">, "variants" | "size"> {
  /** Bootstrap form control size */
  size?: "sm" | "lg";
  /** Input validation state */
  isValid?: boolean;
  /** Input error state */
  isInvalid?: boolean;
  /** Whether input is disabled */
  disabled?: boolean;
  /** Whether input is readonly */
  readOnly?: boolean;
  /** Input type */
  type?: "text" | "email" | "password" | "number" | "tel" | "url" | "search";
  /** Custom animation variants (overrides default) */
  customVariants?: Variants;
  /** Additional className */
  className?: string;
}

/**
 * Unified motion input component that provides consistent form interactions
 * Uses Framer Motion for smooth focus animations and Bootstrap for styling
 */
export const MotionInput: React.FC<MotionInputProps> = ({
  size,
  isValid = false,
  isInvalid = false,
  disabled = false,
  readOnly = false,
  type = "text",
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
  const variants = customVariants || inputVariants;

  // Determine current state
  const getCurrentState = () => {
    if (isInvalid) return "error";
    if (isValid) return "success";
    return "default";
  };

  return (
    <motion.input
      type={type}
      className={finalClassName}
      variants={variants}
      initial="default"
      animate={getCurrentState()}
      whileFocus={disabled ? undefined : "focus"}
      disabled={disabled}
      readOnly={readOnly}
      {...props}
    />
  );
};
