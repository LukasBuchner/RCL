import { useEffect } from "react";
import { ApolloError } from "@apollo/client";
import { useError } from "../contexts/ErrorContext";
import { isWebSocketError } from "../utils/apolloErrorUtils";
import { createLogger } from "../utils/logger";

const log = createLogger("Apollo");

/**
 * Hook to handle Apollo GraphQL errors and display them to users.
 * Automatically filters out WebSocket connection errors which are handled separately.
 * @param {ApolloError | undefined} error - The Apollo error to handle
 * @param {Object} options - Configuration options
 * @param {string} options.componentName - Name of the component where the error occurred
 * @param {string} options.operation - Name of the GraphQL operation that failed
 * @param {boolean} options.showToast - Whether to show a toast notification
 * @param {(error: ApolloError) => void} options.onError - Custom error handler callback
 */
export const useApolloError = (
  error: ApolloError | undefined,
  options?: {
    componentName?: string;
    operation?: string;
    showToast?: boolean;
    onError?: (error: ApolloError) => void;
  },
) => {
  const { handleError } = useError();

  useEffect(() => {
    if (error) {
      // Filter out WebSocket connection errors - they're handled by WebSocketContext
      if (isWebSocketError(error)) {
        log.debug(
          "WebSocket error detected in useApolloError, skipping notification",
        );
        return;
      }

      // Call custom error handler if provided
      options?.onError?.(error);

      // Handle different types of Apollo errors
      if (error.networkError) {
        handleError(new Error("Network error: " + error.message), {
          source: "network",
          operation: options?.operation,
          component: options?.componentName,
        });
      } else if (error.graphQLErrors?.length > 0) {
        // Handle each GraphQL error
        error.graphQLErrors.forEach((gqlError) => {
          handleError(new Error(gqlError.message), {
            source: "graphql",
            operation: options?.operation,
            component: options?.componentName,
          });
        });
      } else {
        // Generic error
        handleError(error, {
          source: "unknown",
          operation: options?.operation,
          component: options?.componentName,
        });
      }
    }
  }, [error, handleError, options]);
};
