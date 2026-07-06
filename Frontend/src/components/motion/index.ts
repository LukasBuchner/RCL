/**
 * Unified Motion Components
 *
 * This module provides reusable Framer Motion components that implement
 * consistent interaction patterns across the application following SOLID and DRY principles.
 *
 * Components:
 * - MotionButton: Unified button interactions with Bootstrap integration
 * - MotionCard: Consistent card hover effects with multiple interaction levels
 * - MotionContainer: Layout animations for containers and lists
 *
 * Usage Examples:
 *
 * ```tsx
 * // Interactive button with unified animations
 * <MotionButton variant="primary" onClick={handleClick}>
 *   Save Changes
 * </MotionButton>
 *
 * // Interactive card with subtle hover
 * <MotionCard interaction="subtle" clickable onClick={handleCardClick}>
 *   <div className="card-body">Content</div>
 * </MotionCard>
 *
 * // Animated container with stagger effects
 * <MotionList staggerDelay={0.1}>
 *   {items.map(item => (
 *     <motion.div key={item.id} variants={staggerItemVariants}>
 *       {item.content}
 *     </motion.div>
 *   ))}
 * </MotionList>
 * ```
 */

// Export all motion components
export { MotionButton } from "./MotionButton";
export { MotionCard } from "./MotionCard";
export {
  MotionContainer,
  MotionList,
  staggerItemVariants,
} from "./MotionContainer";
export { MotionListItem } from "./MotionListItem";
export { MotionGrid, gridItemVariants } from "./MotionGrid";
export { MotionEmptyState } from "./MotionEmptyState";
export { MotionInput } from "./MotionInput";
export { MotionTextarea } from "./MotionTextarea";

// Export motion settings utilities
export { useMotionSettings } from "../../hooks/useMotionSettings";
export {
  withMotionSettings,
  useMotionDiv,
  useMotionButton,
  useMotionSpan,
} from "./withMotionSettings";

// Re-export motion variants for direct use
export {
  nodeVariants,
  nodeStateVariants,
  nodeLayoutVariants,
  toolbarVariants,
  interactiveVariants,
  edgeVariants,
  getNodeVariant,
  getInteractionState,
} from "../../styles/motionVariants";
