import React from "react";
import { motion } from "framer-motion";
import { useMotionSettings } from "../../hooks/useMotionSettings";

/**
 * Higher-order component that wraps any component with motion settings support
 * Automatically handles reduce motion preferences and animation speed
 */
export function withMotionSettings<P extends Record<string, unknown>>(
  Component: React.ComponentType<P>,
  fallbackComponent?: React.ComponentType<P>,
) {
  const WrappedComponent = React.forwardRef<HTMLElement, P>((props, ref) => {
    const { shouldReduceMotion } = useMotionSettings();

    if (shouldReduceMotion() && fallbackComponent) {
      const FallbackComponent = fallbackComponent;
      const propsWithRef = { ...props, ref } as P;
      return <FallbackComponent {...propsWithRef} />;
    }

    if (shouldReduceMotion()) {
      // If it's a motion component, try to extract the non-motion equivalent
      if (Component === motion.div) {
        return (
          <div
            ref={ref as React.Ref<HTMLDivElement>}
            {...(props as React.HTMLAttributes<HTMLDivElement>)}
          />
        );
      }
      if (Component === motion.button) {
        return (
          <button
            ref={ref as React.Ref<HTMLButtonElement>}
            {...(props as React.ButtonHTMLAttributes<HTMLButtonElement>)}
          />
        );
      }
      if (Component === motion.span) {
        return (
          <span
            ref={ref as React.Ref<HTMLSpanElement>}
            {...(props as React.HTMLAttributes<HTMLSpanElement>)}
          />
        );
      }
      // For other components, render normally
    }

    const propsWithRef = { ...props, ref } as P;
    return <Component {...propsWithRef} />;
  });

  WrappedComponent.displayName = `withMotionSettings(${Component.displayName || Component.name})`;

  return WrappedComponent;
}

/**
 * Hook to conditionally use motion.div or regular div based on settings
 */
export function useMotionDiv() {
  const { shouldReduceMotion } = useMotionSettings();
  return shouldReduceMotion() ? ("div" as const) : motion.div;
}

/**
 * Hook to conditionally use motion.button or regular button based on settings
 */
export function useMotionButton() {
  const { shouldReduceMotion } = useMotionSettings();
  return shouldReduceMotion() ? ("button" as const) : motion.button;
}

/**
 * Hook to conditionally use motion.span or regular span based on settings
 */
export function useMotionSpan() {
  const { shouldReduceMotion } = useMotionSettings();
  return shouldReduceMotion() ? ("span" as const) : motion.span;
}
