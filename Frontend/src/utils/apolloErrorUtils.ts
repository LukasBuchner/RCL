import { ApolloError } from "@apollo/client";

/**
 * Determines if an error is a WebSocket connection error.
 * These errors are expected during normal WebSocket lifecycle and should not be shown as critical errors.
 * @param {ApolloError | undefined} error - The ApolloError to check
 * @returns {boolean} True if this is a WebSocket connection error
 */
export function isWebSocketError(error: ApolloError | undefined): boolean {
  if (!error) return false;

  const message = error.message.toLowerCase();
  const wsErrorPatterns = [
    "socket closed",
    "websocket closed",
    "websocket connection",
    "connection closed",
  ];

  return wsErrorPatterns.some((pattern) => message.includes(pattern));
}

/**
 * Determines if an ApolloError represents a backend connection failure.
 * This occurs when the GraphQL server is not reachable or the network is down.
 * Excludes WebSocket connection errors which are handled separately.
 * @param {ApolloError | undefined} error - The ApolloError to check
 * @returns {boolean} True if this is a backend connection error, false otherwise
 */
export function isBackendConnectionError(
  error: ApolloError | undefined,
): boolean {
  if (!error) return false;

  // Don't treat WebSocket connection errors as backend connection errors
  if (isWebSocketError(error)) {
    return false;
  }

  // Check if there's a network error present
  if (error.networkError) {
    // Network errors typically mean the backend server is unreachable
    return true;
  }

  // Check for specific error messages that indicate connection issues
  const message = error.message.toLowerCase();
  if (
    message.includes("failed to fetch") ||
    message.includes("network error") ||
    message.includes("networkerror") ||
    message.includes("connection refused") ||
    message.includes("econnrefused") ||
    message.includes("unable to connect")
  ) {
    return true;
  }

  return false;
}

/**
 * Gets a user-friendly error message for Apollo errors.
 * Distinguishes between backend connection failures and other errors.
 *
 * @param error - The ApolloError to get a message for
 * @returns An object with title and message for display
 */
export function getApolloErrorMessage(error: ApolloError | undefined): {
  title: string;
  message: string;
  isConnectionError: boolean;
} {
  if (!error) {
    return {
      title: "Unknown Error",
      message: "An unknown error occurred",
      isConnectionError: false,
    };
  }

  if (isBackendConnectionError(error)) {
    return {
      title: "Backend Connection Failed",
      message:
        "Cannot connect to the backend server. Please ensure the server is running and try again.",
      isConnectionError: true,
    };
  }

  // For GraphQL errors, try to extract a meaningful message
  if (error.graphQLErrors && error.graphQLErrors.length > 0) {
    const firstError = error.graphQLErrors[0];
    return {
      title: "GraphQL Error",
      message: firstError.message,
      isConnectionError: false,
    };
  }

  // Generic error fallback
  return {
    title: "Error",
    message: error.message,
    isConnectionError: false,
  };
}
