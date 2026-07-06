import React from "react";
import { HTMLMotionProps, motion, Variants } from "framer-motion";
import { useMotionSettings } from "../../hooks/useMotionSettings";
import {
  createVariantsWithTiming,
  reduceMotionVariants,
} from "../../styles/motionVariants";

/**
 * Enhanced button props that extend HTML button attributes
 */
interface MotionButtonProps
  extends Omit<HTMLMotionProps<"button">, "variants" | "children"> {
  /** Bootstrap button variant */
  variant?:
    | "primary"
    | "secondary"
    | "success"
    | "danger"
    | "warning"
    | "info"
    | "light"
    | "dark"
    | "outline-primary"
    | "outline-secondary"
    | "outline-danger";
  /** Button size */
  size?: "sm" | "lg";
  /** Whether the button is disabled */
  disabled?: boolean;
  /** Whether to show loading state */
  loading?: boolean;
  /** Custom animation variants (overrides default) */
  customVariants?: Variants;
  /** Button content */
  children?: React.ReactNode;
}

/**
 * Unified motion button component that provides consistent interactions
 * Uses Framer Motion for smooth animations and Bootstrap for styling
 * Supports ref forwarding for use with Bootstrap components like Dropdown.Toggle
 */
export const MotionButton = React.forwardRef<
  HTMLButtonElement,
  MotionButtonProps
>(
  (
    {
      variant = "primary",
      size,
      disabled = false,
      loading = false,
      customVariants,
      className = "",
      children,
      ...props
    },
    ref,
  ) => {
    const { createTransition, shouldReduceMotion } = useMotionSettings();
    const variants =
      customVariants ||
      (shouldReduceMotion()
        ? reduceMotionVariants.button
        : createVariantsWithTiming(createTransition).interactiveVariants);

    // Build Bootstrap classes
    const baseClasses = ["btn"];

    if (variant) {
      baseClasses.push(`btn-${variant}`);
    }

    if (size) {
      baseClasses.push(`btn-${size}`);
    }

    const finalClassName = `${baseClasses.join(" ")} ${className}`.trim();

    return (
      <motion.button
        ref={ref}
        className={finalClassName}
        variants={variants}
        initial="default"
        animate={disabled ? "disabled" : "default"}
        whileHover={disabled ? undefined : "hover"}
        whileTap={disabled ? undefined : "tap"}
        whileFocus={disabled ? undefined : "hover"}
        disabled={disabled || loading}
        {...props}
      >
        {loading ? (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="d-flex align-items-center"
            transition={
              shouldReduceMotion() ? { duration: 0 } : createTransition(0.2)
            }
          >
            <div
              className="spinner-border spinner-border-sm me-2"
              role="status"
            >
              <span className="visually-hidden">Loading...</span>
            </div>
            {children}
          </motion.div>
        ) : (
          children
        )}
      </motion.button>
    );
  },
);

// Add display name for debugging
MotionButton.displayName = "MotionButton";
