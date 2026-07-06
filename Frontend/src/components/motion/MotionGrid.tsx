import React from "react";
import {
  motion,
  HTMLMotionProps,
  AnimatePresence,
  Variants,
} from "framer-motion";

/**
 * Grid container variants with stagger support
 */
const gridContainerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.08,
      delayChildren: 0.05,
    },
  },
  exit: {
    opacity: 0,
    transition: {
      staggerChildren: 0.05,
      staggerDirection: -1,
    },
  },
};

/**
 * Grid item variants with scale and fade effects
 */
// eslint-disable-next-line react-refresh/only-export-components
export const gridItemVariants = {
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
      type: "spring" as const,
      stiffness: 300,
      damping: 24,
    },
  },
  exit: {
    opacity: 0,
    scale: 0.9,
    y: -20,
    transition: {
      duration: 0.2,
      ease: [0.4, 0.0, 1.0, 1.0] as const,
    },
  },
};

/**
 * Motion grid props
 */
interface MotionGridProps
  extends Omit<HTMLMotionProps<"div">, "variants" | "children"> {
  /** Grid content */
  children?: React.ReactNode;
  /** Whether to animate presence changes */
  animatePresence?: boolean;
  /** Custom container variants */
  customVariants?: Variants;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Unified motion grid component for animated grid layouts
 * Provides staggered animations for grid items
 * Supports ref forwarding for proper container integration
 */
export const MotionGrid = React.forwardRef<HTMLDivElement, MotionGridProps>(
  (
    {
      animatePresence = true,
      customVariants,
      className = "",
      children,
      ...props
    },
    ref,
  ) => {
    const variants = customVariants || gridContainerVariants;
    const finalClassName = `${className}`.trim();

    const gridContent = (
      <motion.div
        ref={ref}
        className={finalClassName}
        variants={variants}
        initial="hidden"
        animate="visible"
        exit="exit"
        style={{
          padding: "1rem 1rem 2rem 1rem",
          ...props.style,
        }}
        {...props}
      >
        {children}
      </motion.div>
    );

    if (animatePresence) {
      return <AnimatePresence mode="popLayout">{gridContent}</AnimatePresence>;
    }

    return gridContent;
  },
);

// Add display name for debugging
MotionGrid.displayName = "MotionGrid";
