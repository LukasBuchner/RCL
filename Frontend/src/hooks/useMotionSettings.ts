import { useMemo } from "react";
import { useSettingsStore } from "../stores/settingsStore";
import { Transition, Easing } from "framer-motion";

/**
 * Hook to get current motion settings and apply them to animations
 * Respects user preferences for animation speed and reduce motion
 */
export const useMotionSettings = () => {
  const { settings } = useSettingsStore();
  const { animationSpeed, reduceMotion } = settings.appearance;

  return useMemo(() => {
    const baseSpeedMultiplier = reduceMotion ? 0 : animationSpeed;

    /**
     * Applies motion settings to a transition object
     */
    const applyMotionSettings = (transition: Transition): Transition => {
      if (reduceMotion) {
        return {
          ...transition,
          duration: 0,
          delay: 0,
        };
      }

      return {
        ...transition,
        duration: transition.duration
          ? transition.duration / animationSpeed
          : undefined,
        delay: transition.delay ? transition.delay / animationSpeed : undefined,
      };
    };

    /**
     * Creates a transition with motion settings applied
     */
    const createTransition = (
      duration: number = 0.2,
      ease: Easing | Easing[] = "easeOut",
      delay: number = 0,
    ): Transition => {
      return applyMotionSettings({
        duration,
        ease,
        delay,
      });
    };

    /**
     * Gets the current animation speed multiplier
     */
    const getSpeedMultiplier = () => baseSpeedMultiplier;

    /**
     * Checks if motion should be reduced
     */
    const shouldReduceMotion = () => reduceMotion;

    return {
      applyMotionSettings,
      createTransition,
      getSpeedMultiplier,
      shouldReduceMotion,
      animationSpeed,
      reduceMotion,
    };
  }, [animationSpeed, reduceMotion]);
};
