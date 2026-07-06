import React from "react";
import { HTMLMotionProps, motion, Variants } from "framer-motion";
import { useMotionSettings } from "../../hooks/useMotionSettings";
import {
  createVariantsWithTiming,
  reduceMotionVariants,
} from "../../styles/motionVariants";

/**
 * Enhanced card props
 */
interface MotionCardProps
  extends Omit<HTMLMotionProps<"div">, "variants" | "children"> {
  /** Card interaction style */
  interaction?: "subtle" | "pronounced" | "property" | "none";
  /** Whether the card is clickable */
  clickable?: boolean;
  /** Whether to show hover cursor */
  showCursor?: boolean;
  /** Custom animation variants (overrides default) */
  customVariants?: Variants;
  /** Card content */
  children?: React.ReactNode;
}

/**
 * Unified motion card component for consistent card interactions
 * Provides different interaction levels based on use case
 * Supports ref forwarding for integration with other components
 */
export const MotionCard = React.forwardRef<HTMLDivElement, MotionCardProps>(
  (
    {
      interaction = "subtle",
      clickable = false,
      showCursor = true,
      customVariants,
      className = "",
      children,
      style,
      ...props
    },
    ref,
  ) => {
    const { createTransition, shouldReduceMotion } = useMotionSettings();

    const variants =
      customVariants ||
      (interaction !== "none"
        ? shouldReduceMotion()
          ? reduceMotionVariants.card
          : createVariantsWithTiming(createTransition).cardVariants[interaction]
        : undefined);

    // Build final className
    const baseClasses = ["card"];
    const finalClassName = `${baseClasses.join(" ")} ${className}`.trim();

    // Build final style
    const finalStyle = {
      cursor: clickable && showCursor ? "pointer" : undefined,
      ...(style || {}),
    };

    if (interaction === "none" || !variants) {
      // Filter out motion-specific props
      const {
        onDrag: _onDrag,
        onDragStart: _onDragStart,
        onDragEnd: _onDragEnd,
        onAnimationStart: _onAnimationStart,
        ...htmlProps
      } = props as HTMLMotionProps<"div">;
      return (
        <div
          ref={ref}
          className={finalClassName}
          style={finalStyle as React.CSSProperties}
          role={clickable ? "button" : undefined}
          tabIndex={clickable ? 0 : undefined}
          {...(htmlProps as unknown as React.HTMLAttributes<HTMLDivElement>)}
        >
          {children}
        </div>
      );
    }

    return (
      <motion.div
        ref={ref}
        className={finalClassName}
        style={finalStyle}
        variants={variants}
        initial="default"
        animate="default"
        whileHover={clickable ? "hover" : undefined}
        whileTap={clickable ? "tap" : undefined}
        whileFocus={clickable ? "hover" : undefined}
        role={clickable ? "button" : undefined}
        tabIndex={clickable ? 0 : undefined}
        {...props}
      >
        {children}
      </motion.div>
    );
  },
);

// Add display name for debugging
MotionCard.displayName = "MotionCard";
