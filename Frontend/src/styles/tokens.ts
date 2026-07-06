/**
 * Design tokens for consistent styling across the application
 * Updated to work with Framer Motion animations
 */

/**
 * Animation timing and easing functions
 * Compatible with Framer Motion transition configurations
 */
export const animations = {
  duration: {
    fast: 0.15,
    normal: 0.2,
    slow: 0.3,
    slower: 0.5,
  },
  easing: {
    ease: "ease",
    easeIn: "ease-in",
    easeOut: "ease-out",
    easeInOut: "ease-in-out",
    smooth: "easeOut",
    bounce: "backOut",
  },
} as const;

/**
 * Elevation levels and corresponding shadows
 */
export const elevation = {
  none: {
    boxShadow: "none",
    transform: "translateY(0px)",
  },
  level1: {
    boxShadow: "var(--motion-shadow-base)",
    transform: "translateY(-1px)",
  },
  level2: {
    boxShadow: "var(--motion-shadow-hover)",
    transform: "translateY(-2px)",
  },
  level3: {
    boxShadow: "var(--motion-shadow-medium)",
    transform: "translateY(-3px)",
  },
  level4: {
    boxShadow: "var(--motion-shadow-large)",
    transform: "translateY(-4px)",
  },
  level5: {
    boxShadow: "var(--motion-shadow-focus)",
    transform: "translateY(-6px)",
  },
} as const;

/**
 * Scale transforms for different interaction states
 */
export const scale = {
  none: 1,
  subtle: 1.01,
  small: 1.02,
  medium: 1.05,
  large: 1.1,
} as const;

/**
 * Opacity values for different states
 */
export const opacity = {
  disabled: 0.6,
  muted: 0.8,
  normal: 1,
  cut: 0.9,
  copy: 0.95,
} as const;

/**
 * Border styles for different states
 */
export const borders = {
  none: "none",
  default: "1px solid var(--app-border)",
  selected: "1px solid var(--app-primary)",
  executing: "2px solid var(--app-success)",
  cut: "2px dashed var(--app-warning)",
  copy: "2px dashed var(--app-info)",
  focus: "2px solid var(--app-primary)",
  error: "2px solid var(--app-danger)",
} as const;

/**
 * Predefined interaction styles
 * Note: These are kept for backward compatibility
 * New components should use Framer Motion variants from motionVariants.ts
 */
export const interactions = {
  hover: {
    ...elevation.level2,
    transform: `scale(${scale.small}) ${elevation.level2.transform}`,
    transition: `all ${animations.duration.normal}s ${animations.easing.smooth}`,
  },
  focus: {
    ...elevation.level2,
    transform: `scale(${scale.small}) ${elevation.level2.transform}`,
    outline: "none",
    border: borders.focus,
    transition: `all ${animations.duration.normal}s ${animations.easing.smooth}`,
  },
  press: {
    ...elevation.level1,
    transform: `scale(${scale.subtle}) ${elevation.level1.transform}`,
    transition: `all ${animations.duration.fast}s ${animations.easing.smooth}`,
  },
  disabled: {
    opacity: opacity.disabled,
    cursor: "not-allowed",
    transform: "none",
    boxShadow: "none",
  },
} as const;

/**
 * Utility function to create consistent transitions
 * Updated for Framer Motion compatibility
 */
export const createTransition = (
  properties: string | string[],
  duration: keyof typeof animations.duration = "normal",
  easing: keyof typeof animations.easing = "smooth",
): string => {
  const props = Array.isArray(properties) ? properties : [properties];
  return props
    .map(
      (prop) =>
        `${prop} ${animations.duration[duration]}s ${animations.easing[easing]}`,
    )
    .join(", ");
};

/**
 * Utility function to create Framer Motion transition config
 */
export const createMotionTransition = (
  duration: keyof typeof animations.duration = "normal",
  easing: keyof typeof animations.easing = "smooth",
) => ({
  duration: animations.duration[duration],
  ease: animations.easing[easing],
});

/**
 * Utility function to get elevation styles
 */
export const getElevation = (level: keyof typeof elevation) => elevation[level];

/**
 * Utility function to get scale value
 */
export const getScale = (level: keyof typeof scale) => scale[level];

/**
 * Utility function to get opacity value
 */
export const getOpacity = (state: keyof typeof opacity) => opacity[state];

/**
 * Utility function to get border style
 */
export const getBorder = (state: keyof typeof borders) => borders[state];
