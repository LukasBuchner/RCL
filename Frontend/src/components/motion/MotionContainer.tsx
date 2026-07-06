import React from "react";
import {
  motion,
  HTMLMotionProps,
  AnimatePresence,
  Variants,
} from "framer-motion";

/**
 * Container animation variants for layout changes
 */
const containerVariants: Record<string, Variants> = {
  // Fade in/out animations
  fade: {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: { duration: 0.3, ease: "easeOut" as const },
    },
    exit: {
      opacity: 0,
      transition: { duration: 0.2, ease: "easeIn" as const },
    },
  },

  // Slide animations
  slideUp: {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.3, ease: "easeOut" as const },
    },
    exit: {
      opacity: 0,
      y: -20,
      transition: { duration: 0.2, ease: "easeIn" as const },
    },
  },

  slideDown: {
    hidden: { opacity: 0, y: -20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.3, ease: "easeOut" as const },
    },
    exit: {
      opacity: 0,
      y: 20,
      transition: { duration: 0.2, ease: "easeIn" as const },
    },
  },

  // Scale animations
  scale: {
    hidden: { opacity: 0, scale: 0.9 },
    visible: {
      opacity: 1,
      scale: 1,
      transition: { duration: 0.3, ease: "easeOut" as const },
    },
    exit: {
      opacity: 0,
      scale: 0.9,
      transition: { duration: 0.2, ease: "easeIn" as const },
    },
  },

  // Stagger children animations
  stagger: {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.1,
        delayChildren: 0.1,
      },
    },
    exit: {
      opacity: 0,
      transition: {
        staggerChildren: 0.05,
        staggerDirection: -1,
      },
    },
  },
};

/**
 * Child item variants for stagger animations
 */
// eslint-disable-next-line react-refresh/only-export-components
export const staggerItemVariants: Variants = {
  hidden: { opacity: 0, y: 10 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.2, ease: "easeOut" as const },
  },
  exit: {
    opacity: 0,
    y: -10,
    transition: { duration: 0.15, ease: "easeIn" as const },
  },
};

/**
 * Enhanced container props
 */
interface MotionContainerProps
  extends Omit<HTMLMotionProps<"div">, "variants" | "children"> {
  /** Animation type for container */
  animation?: "fade" | "slideUp" | "slideDown" | "scale" | "stagger" | "none";
  /** Whether to animate presence (show/hide) */
  animatePresence?: boolean;
  /** Whether the container is visible (for animatePresence) */
  show?: boolean;
  /** Custom animation variants (overrides default) */
  customVariants?: Variants;
  /** Container content */
  children?: React.ReactNode;
  /** Layout animation */
  layout?: boolean;
}

/**
 * Unified motion container for consistent layout animations
 * Supports entrance/exit animations and stagger effects
 */
export const MotionContainer: React.FC<MotionContainerProps> = ({
  animation = "fade",
  animatePresence = false,
  show = true,
  customVariants,
  layout = false,
  children,
  ...props
}) => {
  // Determine which variants to use
  const variants =
    customVariants ||
    (animation !== "none" ? containerVariants[animation] : undefined);

  const motionDiv = (
    <motion.div
      variants={variants}
      initial={animation !== "none" ? "hidden" : false}
      animate={animation !== "none" ? "visible" : undefined}
      exit={animation !== "none" ? "exit" : undefined}
      layout={layout}
      {...props}
    >
      {children}
    </motion.div>
  );

  if (animatePresence) {
    return <AnimatePresence mode="wait">{show && motionDiv}</AnimatePresence>;
  }

  if (animation === "none") {
    // Filter out motion-specific props
    const {
      onDrag: _onDrag,
      onDragStart: _onDragStart,
      onDragEnd: _onDragEnd,
      ...htmlProps
    } = props as HTMLMotionProps<"div">;
    return (
      <div {...(htmlProps as React.HTMLAttributes<HTMLDivElement>)}>
        {children}
      </div>
    );
  }

  return motionDiv;
};

/**
 * Specialized stagger container for lists
 */
interface MotionListProps extends Omit<MotionContainerProps, "animation"> {
  /** Stagger delay between children */
  staggerDelay?: number;
}

export const MotionList: React.FC<MotionListProps> = ({
  staggerDelay = 0.1,
  customVariants,
  children,
  ...props
}) => {
  const staggerVariants = customVariants || {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: staggerDelay,
        delayChildren: 0.1,
      },
    },
    exit: {
      opacity: 0,
      transition: {
        staggerChildren: staggerDelay / 2,
        staggerDirection: -1,
      },
    },
  };

  return (
    <MotionContainer customVariants={staggerVariants} {...props}>
      {children}
    </MotionContainer>
  );
};
