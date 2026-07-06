import { Easing, Transition, Variants } from "framer-motion";

/**
 * Framer Motion variants for unified component interactions
 * These variants now support dynamic timing based on user settings
 */

/**
 * Static variants for reduce motion - no transformations, only style changes
 */
export const reduceMotionVariants = {
  button: {
    default: {},
    hover: {
      filter: "brightness(0.9)",
    },
    tap: {
      filter: "brightness(0.8)",
    },
    disabled: {
      opacity: 0.6,
    },
  } satisfies Variants,

  card: {
    default: {},
    hover: {
      backgroundColor: "var(--motion-bg-hover-subtle)",
    },
    tap: {
      backgroundColor: "var(--motion-bg-active-subtle)",
    },
  } satisfies Variants,

  listItem: {
    hidden: {
      opacity: 0,
    },
    visible: {
      opacity: 1,
      transition: { duration: 0 },
    },
    exit: {
      opacity: 0,
      transition: { duration: 0 },
    },
    hover: {
      backgroundColor: "var(--motion-bg-hover-primary)",
      borderLeftColor: "var(--app-primary)",
      borderLeftWidth: "4px",
    },
    tap: {
      backgroundColor: "var(--motion-bg-active-primary)",
      borderLeftColor: "var(--app-primary)",
      borderLeftWidth: "4px",
    },
  } satisfies Variants,
};

/**
 * Factory function to create variants with configurable timing
 */
export const createVariantsWithTiming = (
  createTransition: (
    duration: number,
    ease?: Easing | Easing[],
    delay?: number,
  ) => Transition,
) => ({
  /**
   * Base node interaction variants
   */
  nodeVariants: {
    default: {
      scale: 1,
      boxShadow: "0 2px 4px var(--motion-shadow-base)",
      transition: createTransition(0.2, "easeOut"),
    },
    hover: {
      scale: 1.02,
      boxShadow: "0 8px 16px var(--motion-shadow-hover)",
      transition: createTransition(0.2, "easeOut"),
    },
    tap: {
      scale: 0.98,
      boxShadow: "0 2px 4px var(--motion-shadow-base)",
      transition: createTransition(0.1, "easeOut"),
    },
    focus: {
      scale: 1.02,
      boxShadow: "0 8px 16px var(--motion-shadow-hover)",
      transition: createTransition(0.2, "easeOut"),
    },
    executing: {
      scale: 1.05,
      boxShadow: [
        "0 12px 24px var(--motion-shadow-focus)",
        "0 16px 32px var(--motion-shadow-focus)",
        "0 12px 24px var(--motion-shadow-focus)",
      ],
      borderColor: "var(--bs-success)",
      transition: {
        scale: createTransition(0.3, "easeOut"),
        boxShadow: {
          repeat: Infinity,
          duration: 2,
          ease: "easeInOut",
        },
        borderColor: createTransition(0.3, "easeOut"),
      },
    },
    disabled: {
      scale: 1,
      opacity: 0.6,
      boxShadow: "none",
      transition: createTransition(0.2, "easeOut"),
    },
  } satisfies Variants,

  /**
   * Variants for nodes with different states (cut, copy, etc.)
   */
  nodeStateVariants: {
    normal: {
      opacity: 1,
      borderStyle: "solid",
      borderWidth: "1px",
      borderColor: "var(--bs-border-color)",
      transition: createTransition(0.15, "easeOut"),
    },
    selected: {
      opacity: 1,
      borderStyle: "solid",
      borderWidth: "1px",
      borderColor: "var(--app-primary)",
      transition: createTransition(0.15, "easeOut"),
    },
    executing: {
      opacity: 1,
      borderStyle: "solid",
      borderWidth: "2px",
      borderColor: [
        "var(--app-success)",
        "rgba(25, 135, 84, 0.8)",
        "var(--app-success)",
      ],
      boxShadow: [
        "0 0 0 2px rgba(25, 135, 84, 0.3)",
        "0 0 0 4px rgba(25, 135, 84, 0.2)",
        "0 0 0 2px rgba(25, 135, 84, 0.3)",
      ],
      transition: {
        borderColor: {
          repeat: Infinity,
          duration: 2,
          ease: "easeInOut",
        },
        boxShadow: {
          repeat: Infinity,
          duration: 2,
          ease: "easeInOut",
        },
      },
    },
    markedForCut: {
      opacity: 0.9,
      borderStyle: "dashed",
      borderWidth: "2px",
      borderColor: "var(--bs-warning)",
      transition: createTransition(0.15, "easeOut"),
    },
    markedForCopy: {
      opacity: 0.95,
      borderStyle: "dashed",
      borderWidth: "2px",
      borderColor: "var(--bs-info)",
      transition: createTransition(0.15, "easeOut"),
    },
    hasWarning: {
      opacity: 1,
      borderStyle: "solid",
      borderWidth: "2px",
      borderColor: "var(--bs-warning)",
      boxShadow: "0 0 0 2px rgba(255, 193, 7, 0.3)",
      transition: createTransition(0.15, "easeOut"),
    },
  } satisfies Variants,

  /**
   * Variants for node entrance/exit animations
   */
  nodeLayoutVariants: {
    hidden: {
      opacity: 0,
      scale: 0.8,
      y: 20,
    },
    visible: {
      opacity: 1,
      scale: 1,
      y: 0,
      transition: createTransition(0.3, "easeOut"),
    },
    exit: {
      opacity: 0,
      scale: 0.8,
      y: -20,
      transition: createTransition(0.2, "easeIn"),
    },
  } satisfies Variants,

  /**
   * Variants for toolbar animations
   */
  toolbarVariants: {
    hidden: {
      opacity: 0,
      y: -10,
      scale: 0.95,
    },
    visible: {
      opacity: 1,
      y: 0,
      scale: 1,
      transition: createTransition(0.2, "easeOut"),
    },
  } satisfies Variants,

  /**
   * Variants for interactive elements (buttons, controls)
   */
  interactiveVariants: {
    default: {
      scale: 1,
      opacity: 1,
      transition: createTransition(0.15, "easeOut"),
    },
    hover: {
      scale: 1.05,
      opacity: 1,
      transition: createTransition(0.15, "easeOut"),
    },
    tap: {
      scale: 0.95,
      opacity: 0.8,
      transition: createTransition(0.1, "easeOut"),
    },
    disabled: {
      scale: 1,
      opacity: 0.5,
      transition: createTransition(0.15, "easeOut"),
    },
  } satisfies Variants,

  /**
   * Variants for edge animations (dependency connections)
   */
  edgeVariants: {
    hidden: {
      pathLength: 0,
      opacity: 0,
    },
    visible: {
      pathLength: 1,
      opacity: 1,
      transition: {
        pathLength: createTransition(0.5, "easeInOut"),
        opacity: createTransition(0.3, "easeOut"),
      },
    },
    hover: {
      strokeWidth: 1.5,
      opacity: 1,
      transition: createTransition(0.2, "easeOut"),
    },
  } satisfies Variants,

  /**
   * Card interaction variants for different use cases
   */
  cardVariants: {
    subtle: {
      default: {
        scale: 1,
        y: 0,
        boxShadow: "0 2px 4px var(--motion-shadow-base)",
        transition: createTransition(0.2, "easeOut"),
      },
      hover: {
        scale: 1.01,
        y: -1,
        boxShadow: "0 4px 8px var(--motion-shadow-hover)",
        transition: createTransition(0.2, "easeOut"),
      },
      tap: {
        scale: 0.99,
        y: 0,
        boxShadow: "0 2px 4px var(--motion-shadow-base)",
        transition: createTransition(0.1, "easeOut"),
      },
    } satisfies Variants,
    pronounced: {
      default: {
        scale: 1,
        y: 0,
        boxShadow: "0 2px 4px var(--motion-shadow-base)",
        transition: createTransition(0.2, "easeOut"),
      },
      hover: {
        scale: 1.02,
        y: -2,
        boxShadow: "0 8px 16px var(--motion-shadow-hover)",
        transition: createTransition(0.2, "easeOut"),
      },
      tap: {
        scale: 0.98,
        y: -1,
        boxShadow: "0 4px 8px var(--motion-shadow-medium)",
        transition: createTransition(0.1, "easeOut"),
      },
    } satisfies Variants,
    property: {
      default: {
        scale: 1,
        y: 0,
        boxShadow: "0 2px 4px var(--motion-shadow-base)",
        transition: createTransition(0.2, "easeOut"),
      },
      hover: {
        scale: 1.05,
        y: -1,
        boxShadow: "0 6px 12px var(--motion-shadow-hover)",
        transition: createTransition(0.2, "easeOut"),
      },
      tap: {
        scale: 1.02,
        y: 0,
        boxShadow: "0 4px 8px var(--motion-shadow-medium)",
        transition: createTransition(0.1, "easeOut"),
      },
    } satisfies Variants,
  },

  /**
   * List item stagger variants
   */
  listItemVariants: {
    hidden: {
      opacity: 0,
      y: 20,
      scale: 0.95,
    },
    visible: (index: number) => ({
      opacity: 1,
      y: 0,
      scale: 1,
      transition: createTransition(0.4, "easeOut", index * 0.1),
    }),
    exit: {
      opacity: 0,
      y: -20,
      scale: 0.95,
      transition: createTransition(0.2, "easeOut"),
    },
    hover: {
      backgroundColor: "var(--motion-bg-hover-primary)",
      borderLeftColor: "var(--app-primary)",
      borderLeftWidth: "4px",
      transition: createTransition(0.15, "easeOut"),
    },
    tap: {
      backgroundColor: "var(--motion-bg-active-primary)",
      borderLeftColor: "var(--app-primary)",
      borderLeftWidth: "4px",
      transition: createTransition(0.1, "easeOut"),
    },
  } satisfies Variants,
});

// Export the static variants with default timing for backward compatibility
const defaultTransition = (
  duration: number,
  ease: Easing | Easing[] = "easeOut",
  delay: number = 0,
): Transition => ({
  duration,
  ease,
  delay,
});

export const {
  nodeVariants,
  nodeStateVariants,
  nodeLayoutVariants,
  toolbarVariants,
  interactiveVariants,
  edgeVariants,
  cardVariants,
  listItemVariants,
} = createVariantsWithTiming(defaultTransition);

/**
 * Common spring configuration for smooth animations
 */
export const springConfig = {
  type: "spring",
  stiffness: 300,
  damping: 30,
};

/**
 * Utility function to combine variants based on conditions
 */
export const getNodeVariant = (
  isExecuting: boolean,
  isDisabled: boolean,
  isSelected: boolean,
  markedForCut: boolean,
  markedForCopy: boolean,
  hasWarning: boolean = false,
): string => {
  if (isDisabled) return "disabled";
  if (isExecuting) return "executing";
  if (markedForCut) return "markedForCut";
  if (markedForCopy) return "markedForCopy";
  if (isSelected) return "selected";
  if (hasWarning) return "hasWarning";
  return "normal";
};

/**
 * Utility function to get the appropriate interaction state
 */
export const getInteractionState = (
  isHovered: boolean,
  isPressed: boolean,
  isFocused: boolean,
): string => {
  if (isPressed) return "tap";
  if (isHovered || isFocused) return "hover";
  return "default";
};
