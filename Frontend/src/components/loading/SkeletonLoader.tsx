import React from "react";
import { motion } from "framer-motion";

interface SkeletonLoaderProps {
  width?: string | number;
  height?: string | number;
  variant?: "text" | "circular" | "rectangular" | "rounded";
  className?: string;
  count?: number;
  gap?: number;
}

export const SkeletonLoader: React.FC<SkeletonLoaderProps> = ({
  width = "100%",
  height = 20,
  variant = "rectangular",
  className = "",
  count = 1,
  gap = 8,
}) => {
  const getRadius = () => {
    switch (variant) {
      case "circular":
        return "50%";
      case "rounded":
        return "8px";
      case "text":
        return "4px";
      default:
        return "0";
    }
  };

  const skeletonStyle: React.CSSProperties = {
    width: variant === "circular" ? height : width,
    height,
    borderRadius: getRadius(),
    background:
      "linear-gradient(90deg, var(--app-skeleton-base) 25%, var(--app-skeleton-highlight) 50%, var(--app-skeleton-base) 75%)",
    backgroundSize: "200% 100%",
  };

  const skeletons = Array.from({ length: count }, (_, index) => (
    <div
      key={index}
      className={`overflow-hidden ${className}`}
      style={{
        ...skeletonStyle,
        marginBottom: index < count - 1 ? gap : 0,
      }}
    >
      <motion.div
        animate={{
          x: ["-100%", "100%"],
        }}
        transition={{
          repeat: Infinity,
          duration: 1.5,
          ease: "easeInOut",
        }}
        style={{
          width: "100%",
          height: "100%",
          background: "var(--app-skeleton-shine)",
        }}
      />
    </div>
  ));

  return <>{skeletons}</>;
};

// Preset skeleton components for common use cases
export const SkeletonCard: React.FC<{ className?: string }> = ({
  className,
}) => (
  <div className={`p-3 border rounded ${className}`}>
    <SkeletonLoader variant="rectangular" height={200} className="mb-3" />
    <SkeletonLoader variant="text" count={2} />
  </div>
);

export const SkeletonListItem: React.FC<{ className?: string }> = ({
  className,
}) => (
  <div className={`d-flex align-items-center p-3 border-bottom ${className}`}>
    <SkeletonLoader
      variant="circular"
      width={40}
      height={40}
      className="me-3"
    />
    <div className="flex-grow-1">
      <SkeletonLoader variant="text" width="60%" className="mb-1" />
      <SkeletonLoader variant="text" width="40%" height={14} />
    </div>
  </div>
);

export const SkeletonTable: React.FC<{ rows?: number; columns?: number }> = ({
  rows = 5,
  columns = 4,
}) => (
  <div className="table-responsive">
    <table className="table">
      <thead>
        <tr>
          {Array.from({ length: columns }, (_, i) => (
            <th key={i}>
              <SkeletonLoader variant="text" />
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {Array.from({ length: rows }, (_, rowIndex) => (
          <tr key={rowIndex}>
            {Array.from({ length: columns }, (_, colIndex) => (
              <td key={colIndex}>
                <SkeletonLoader variant="text" />
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);
