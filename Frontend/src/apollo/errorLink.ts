import { onError } from "@apollo/client/link/error";
import { createLogger } from "../utils/logger";

const log = createLogger("Apollo");

/**
 * Checks if an error is a WebSocket connection error that should be silenced.
 * These errors are expected during normal WebSocket lifecycle (reconnections, closures)
 * and should not be shown as error notifications to users.
 * @param {unknown} error - The error to check
 * @returns {boolean} True if this is a WebSocket error that should be silenced
 */
function isWebSocketConnectionError(error: unknown): boolean {
  const errorMessage = error?.toString().toLowerCase() || "";

  // Common WebSocket error patterns
  const wsErrorPatterns = [
    "socket closed",
    "websocket closed",
    "connection closed",
    "websocket connection",
    "failed to connect",
    "connection failed",
    "socket is not open",
    "connection error",
  ];

  return wsErrorPatterns.some((pattern) => errorMessage.includes(pattern));
}

/**
 * Creates an Apollo error link that handles GraphQL and network errors.
 * Filters out WebSocket connection errors to prevent infinite error notifications.
 * Logs all errors to console for debugging while selectively propagating errors to the UI.
 * @returns {ErrorLink} Configured error link for Apollo Client
 */
export const createErrorLink = () => {
  return onError(({ graphQLErrors, networkError }) => {
    if (graphQLErrors) {
      graphQLErrors.forEach(({ message, locations, path, extensions }) => {
        log.error(
          `[GraphQL error]: Message: ${message}, Location: ${locations}, Path: ${path}`,
          extensions,
        );
      });
    }

    if (networkError) {
      // Check if this is a WebSocket connection error
      const isWsError = isWebSocketConnectionError(networkError);

      if (isWsError) {
        // Log WebSocket errors at a lower level since they're expected during reconnection
        log.warn(
          `[WebSocket]: Connection issue detected (auto-reconnecting)`,
          networkError.message,
        );

        // Don't propagate WebSocket connection errors to the error context
        // The WebSocket context will handle connection state changes
        return;
      }

      // Log other network errors normally
      log.error(`[Network error]: ${networkError}`);

      // Handle specific network errors
      if ("statusCode" in networkError) {
        const statusCode = (networkError as { statusCode: number }).statusCode;

        if (statusCode === 401) {
          // Handle unauthorized
          log.error("Unauthorized access - redirecting to login");
          // You can dispatch a custom event or use your auth system here
        } else if (statusCode >= 500) {
          // Server errors
          log.error("Server error occurred");
        }
      }
    }

    // You can also retry failed operations
    // return forward(operation);
  });
};
