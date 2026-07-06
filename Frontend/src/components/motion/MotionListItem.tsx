import React from "react";
import { HTMLMotionProps, motion, Variants } from "framer-motion";
import { useMotionSettings } from "../../hooks/useMotionSettings";
import {
  createVariantsWithTiming,
  reduceMotionVariants,
} from "../../styles/motionVariants";

/**
 * Motion list item props
 */
interface MotionListItemProps
  extends Omit<HTMLMotionProps<"div">, "variants" | "children"> {
  /** Item content */
  children?: React.ReactNode;
  /** Animation delay multiplier for stagger effect */
  index?: number;
  /** Custom animation variants */
  customVariants?: Variants;
  /** Whether to use layout animations */
  layout?: boolean;
  /** Whether to enable hover animations */
  enableHover?: boolean;
  /** Whether to enable tap animations */
  enableTap?: boolean;
  /** Whether the item is interactive/clickable */
  interactive?: boolean;
}

/**
 * Unified motion list item component for consistent list animations
 * Provides staggered entrance animations, smooth layout transitions, and interactive states
 * Supports ref forwarding for use with Framer Motion's AnimatePresence
 */
export const MotionListItem = React.forwardRef<
  HTMLDivElement,
  MotionListItemProps
>(
  (
    {
      index = 0,
      customVariants,
      layout = true,
      enableHover = true,
      enableTap = true,
      interactive = false,
      className = "",
      style,
      children,
      ...props
    },
    ref,
  ) => {
    const { createTransition, shouldReduceMotion } = useMotionSettings();
    const variants =
      customVariants ||
      (shouldReduceMotion()
        ? reduceMotionVariants.listItem
        : createVariantsWithTiming(createTransition).listItemVariants);

    return (
      <motion.div
        ref={ref}
        layout={layout}
        variants={variants}
        initial="hidden"
        animate="visible"
        exit="exit"
        whileHover={enableHover ? "hover" : undefined}
        whileTap={enableTap ? "tap" : undefined}
        whileFocus={interactive ? "hover" : undefined}
        custom={index}
        className={`motion-list-item ${className}`}
        style={{
          position: "relative",
          borderRadius: "8px",
          borderLeft: "4px solid transparent",
          ...(style || {}),
        }}
        role={interactive ? "button" : undefined}
        tabIndex={interactive ? 0 : undefined}
        {...props}
      >
        {children}
      </motion.div>
    );
  },
);

// Add display name for debugging
MotionListItem.displayName = "MotionListItem";
