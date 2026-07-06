import { ReactNode } from "react";

/**
 * Base props that all components should implement
 */
export interface BaseComponentProps {
  /** Additional CSS classes to apply */
  className?: string;
  /** Custom styles to apply */
  style?: React.CSSProperties;
  /** Test ID for testing purposes */
  testId?: string;
  /** Whether the component is disabled */
  disabled?: boolean;
}

/**
 * Props for interactive components that respond to user interactions
 */
export interface InteractiveComponentProps extends BaseComponentProps {
  /** Called when the component is clicked */
  onClick?: (event: React.MouseEvent) => void;
  /** Called when the component is hovered */
  onHover?: (isHovered: boolean) => void;
  /** Called when the component gains focus */
  onFocus?: (event: React.FocusEvent) => void;
  /** Called when the component loses focus */
  onBlur?: (event: React.FocusEvent) => void;
  /** Whether the component is currently selected */
  selected?: boolean;
  /** Whether the component should show hover effects */
  enableHover?: boolean;
  /** Whether the component should show focus effects */
  enableFocus?: boolean;
  /** Whether the component should show press effects */
  enablePress?: boolean;
}

/**
 * Props for editable components that can be modified
 */
export interface EditableComponentProps extends InteractiveComponentProps {
  /** Whether the component is in edit mode */
  isEditing?: boolean;
  /** Called when edit mode is entered */
  onEditStart?: () => void;
  /** Called when edit mode is exited */
  onEditEnd?: () => void;
  /** Called when the component value changes */
  onChange?: (value: unknown) => void;
  /** Whether the component can be edited */
  readonly?: boolean;
  /** Validation function for input */
  validate?: (value: unknown) => boolean | string;
}

/**
 * Props for container components that wrap other components
 */
export interface ContainerComponentProps extends BaseComponentProps {
  /** Child components to render */
  children: ReactNode;
  /** Whether the container is collapsible */
  collapsible?: boolean;
  /** Whether the container is currently collapsed */
  collapsed?: boolean;
  /** Called when collapse state changes */
  onCollapseChange?: (collapsed: boolean) => void;
}

/**
 * Common interaction states
 */
export interface InteractionState {
  /** Whether the component is currently hovered */
  isHovered: boolean;
  /** Whether the component is currently focused */
  isFocused: boolean;
  /** Whether the component is currently being pressed */
  isPressed: boolean;
  /** Whether the component is currently selected */
  isSelected: boolean;
}

/**
 * Animation and transition configuration
 */
export interface AnimationConfig {
  /** Duration of the animation in milliseconds */
  duration?: number;
  /** Easing function for the animation */
  easing?: string;
  /** Whether animations are enabled */
  enabled?: boolean;
}

/**
 * Elevation and shadow configuration
 */
export interface ElevationConfig {
  /** Base elevation level (0-5) */
  base?: number;
  /** Hover elevation level (0-5) */
  hover?: number;
  /** Focus elevation level (0-5) */
  focus?: number;
  /** Press elevation level (0-5) */
  press?: number;
}
