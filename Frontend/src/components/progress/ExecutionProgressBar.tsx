import React, { useRef, useEffect, useState } from "react";
import { motion } from "framer-motion";

interface ExecutionProgressBarProps {
  progress?: number; // Progress from 0 to 100 as float (e.g., 45.5)
  isExecuting?: boolean;
  height?: number;
  showAnimation?: boolean;
  duration?: number; // Total estimated duration in milliseconds
  startTime?: number; // When execution started (timestamp)
}

const ExecutionProgressBar: React.FC<ExecutionProgressBarProps> = ({
  progress = 0,
  isExecuting = false,
  height = 4,
  showAnimation = true,
  duration,
  startTime,
}) => {
  // Ensure progress is between 0 and 100
  const progressPercentage = Math.max(0, Math.min(100, progress || 0));

  // Use local state to track the current animated progress
  const [animatedProgress, setAnimatedProgress] = useState(0);
  const animationRef = useRef<number | null>(null);

  // Animate to new progress value smoothly
  useEffect(() => {
    // Calculate animation duration knowing updates come every ~1 second
    const calculateAnimationDuration = () => {
      // Since updates come every ~1 second, we want to:
      // 1. Start animation immediately when new progress arrives
      // 2. Take most of the 1-second interval to reach the target
      // 3. Leave a small buffer for the next update

      const progressChange = Math.abs(progressPercentage - animatedProgress);

      if (progressChange < 0.1) return 200; // Very small changes animate quickly

      // Use 80% of the 1-second interval (800ms) for most changes
      // This leaves 200ms buffer before the next update likely arrives
      return 800;
    };

    const targetProgress = progressPercentage;
    const startProgress = animatedProgress;
    const difference = targetProgress - startProgress;

    if (Math.abs(difference) < 0.1) return; // Skip tiny changes

    const animationDuration = calculateAnimationDuration();
    const animationStartTime = Date.now();

    const animate = () => {
      const elapsed = Date.now() - animationStartTime;
      const progress = Math.min(elapsed / animationDuration, 1);

      // Smooth easing optimized for 1-second intervals
      // Use a gentler curve that feels natural for regular updates
      const easeOut = 1 - Math.pow(1 - progress, 2.5);
      const currentProgress = startProgress + difference * easeOut;

      setAnimatedProgress(currentProgress);

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate);
      }
    };

    if (animationRef.current) {
      cancelAnimationFrame(animationRef.current);
    }

    animationRef.current = requestAnimationFrame(animate);

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [progressPercentage, animatedProgress, duration, startTime, isExecuting]);

  // Don't show progress bar if progress is 0 and not executing
  if (animatedProgress === 0 && !isExecuting) {
    return null;
  }

  return (
    <div
      className="progress-container"
      style={{
        position: "absolute",
        bottom: 0,
        left: 0,
        right: 0,
        height: `${height}px`,
        backgroundColor: "var(--bs-border-color-translucent)",
        borderBottomLeftRadius: "10px",
        borderBottomRightRadius: "10px",
        overflow: "hidden",
      }}
    >
      <motion.div
        className="progress-bar"
        style={{
          width: `${animatedProgress}%`,
          height: "100%",
          backgroundColor: isExecuting
            ? "var(--app-success)"
            : "var(--bs-success)",
          borderBottomLeftRadius: animatedProgress > 95 ? "10px" : "0px",
          borderBottomRightRadius: animatedProgress > 95 ? "10px" : "0px",
          position: "relative",
        }}
      >
        {/* Animated shimmer effect when executing */}
        {isExecuting && showAnimation && (
          <motion.div
            animate={{
              x: ["-100%", "100%"],
              transition: {
                repeat: Infinity,
                duration: 2,
                ease: "linear",
              },
            }}
            style={{
              position: "absolute",
              top: 0,
              left: 0,
              width: "50%",
              height: "100%",
              background:
                "linear-gradient(90deg, transparent, rgba(255,255,255,0.4), transparent)",
              borderRadius: "inherit",
            }}
          />
        )}
      </motion.div>
    </div>
  );
};

export default ExecutionProgressBar;
