export type ErrorSeverity = "info" | "warning" | "error" | "critical";

export interface AppError {
  id: string;
  message: string;
  severity: ErrorSeverity;
  code?: string;
  timestamp: Date;
  retry?: () => void | Promise<void>;
  dismiss?: () => void;
  details?: Record<string, unknown>;
}

export interface ErrorContext {
  source: "graphql" | "network" | "validation" | "system" | "unknown";
  operation?: string;
  component?: string;
}

export interface ErrorRecoveryStrategy {
  canRetry: boolean;
  maxRetries?: number;
  retryDelay?: number;
  fallback?: () => void;
}
