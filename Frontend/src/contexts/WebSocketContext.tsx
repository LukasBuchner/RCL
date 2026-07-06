import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useRef,
} from "react";
import { createLogger } from "../utils/logger";

const log = createLogger("WebSocket");

/**
 * Context value for WebSocket connection state.
 * @property {boolean} isConnected - Whether the WebSocket is currently connected.
 * @property {(connected: boolean) => void} setIsConnected - Function to update connection status.
 * @property {boolean} wasDisconnected - Whether the connection was lost and is attempting to reconnect.
 */
interface WebSocketContextValue {
  isConnected: boolean;
  setIsConnected: (connected: boolean) => void;
  wasDisconnected: boolean;
}

const WebSocketContext = createContext<WebSocketContextValue | undefined>(
  undefined,
);

/**
 * Provider component for WebSocket connection state.
 * Tracks whether the GraphQL WebSocket subscription connection is active.
 * Logs connection state changes to the console.
 * Prevents infinite error notifications by tracking connection state.
 * @param {React.ReactNode} children - Child components to render
 * @returns {React.ReactElement} The provider component
 */
export const WebSocketProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [isConnected, setIsConnectedState] = useState(true);
  const [wasDisconnected, setWasDisconnected] = useState(false);
  const hasLoggedDisconnect = useRef(false);
  const isInitialMount = useRef(true);

  /**
   * Updates the WebSocket connection status and logs state changes.
   * Tracks disconnection state to enable reconnection logging.
   * @param {boolean} connected - The new connection status
   */
  const setIsConnected = useCallback(
    (connected: boolean) => {
      // Skip notifications on initial mount
      if (isInitialMount.current) {
        isInitialMount.current = false;
        setIsConnectedState(connected);
        return;
      }

      setIsConnectedState(connected);

      if (!connected) {
        // Connection lost - log once
        setWasDisconnected(true);

        // Only log if we haven't already logged this disconnection
        if (!hasLoggedDisconnect.current) {
          log.warn("WebSocket connection lost. Attempting to reconnect...");
          hasLoggedDisconnect.current = true;
        }
      } else if (wasDisconnected) {
        // Reconnected after a disconnection
        log.info("✅ WebSocket connection restored");

        // Clear the disconnection flag and log flag
        setWasDisconnected(false);
        hasLoggedDisconnect.current = false;
      }
    },
    [wasDisconnected],
  );

  return (
    <WebSocketContext.Provider
      value={{
        isConnected,
        setIsConnected,
        wasDisconnected,
      }}
    >
      {children}
    </WebSocketContext.Provider>
  );
};

/**
 * Hook to access WebSocket connection state.
 * Must be used within a WebSocketProvider.
 * @returns {WebSocketContextValue} The WebSocket context value
 * @throws {Error} If used outside of WebSocketProvider
 */
// eslint-disable-next-line react-refresh/only-export-components
export const useWebSocket = () => {
  const context = useContext(WebSocketContext);
  if (!context) {
    throw new Error("useWebSocket must be used within a WebSocketProvider");
  }
  return context;
};
