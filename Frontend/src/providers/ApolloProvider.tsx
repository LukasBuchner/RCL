import React, { createContext, useContext, useEffect, useState } from "react";
import {
  ApolloClient,
  ApolloProvider as BaseApolloProvider,
  gql,
  NormalizedCacheObject,
} from "@apollo/client";
import { createApolloClient } from "../api/createApolloClient";
import { useSettingsStore } from "../stores/settingsStore";
import { useWebSocket } from "../contexts/WebSocketContext";

/**
 * Apollo client context type.
 * @property {ApolloClient<NormalizedCacheObject>} client - The Apollo Client instance
 * @property {() => void} reconnect - Function to reconnect to the GraphQL server
 * @property {boolean} isConnected - Whether the HTTP connection is active
 */
interface ApolloContextType {
  client: ApolloClient<NormalizedCacheObject>;
  reconnect: () => void;
  isConnected: boolean;
}

const ApolloContext = createContext<ApolloContextType | undefined>(undefined);

/**
 * Hook to access Apollo client context.
 * @returns {ApolloContextType} The Apollo context
 * @throws {Error} If used outside of ApolloProvider
 */
// eslint-disable-next-line react-refresh/only-export-components
export const useApollo = () => {
  const context = useContext(ApolloContext);
  if (!context) {
    throw new Error("useApollo must be used within ApolloProvider");
  }
  return context;
};

interface ApolloProviderProps {
  children: React.ReactNode;
}

/**
 * Apollo provider component that manages GraphQL client and connection state.
 * Tracks both HTTP and WebSocket connection status and provides reconnection functionality.
 * @param {ApolloProviderProps} props - Component properties
 * @returns {React.ReactElement} The provider component
 */
export const ApolloProvider: React.FC<ApolloProviderProps> = ({ children }) => {
  const { settings } = useSettingsStore();
  const { setIsConnected: setWebSocketConnected } = useWebSocket();
  const [client, setClient] = useState<ApolloClient<NormalizedCacheObject>>(
    () => createApolloClient(settings.graphql, setWebSocketConnected),
  );
  const [isConnected, setIsConnected] = useState(true);

  // Recreate client when GraphQL settings change
  useEffect(() => {
    const newClient = createApolloClient(
      settings.graphql,
      setWebSocketConnected,
    );
    setClient(newClient);

    // Test connection
    newClient
      .query({
        query: gql`
          query TestConnection {
            __typename
          }
        `,
        errorPolicy: "all",
      })
      .then(() => setIsConnected(true))
      .catch(() => setIsConnected(false));
  }, [settings.graphql, setWebSocketConnected]);

  const reconnect = () => {
    const newClient = createApolloClient(
      settings.graphql,
      setWebSocketConnected,
    );
    setClient(newClient);

    // Test connection
    newClient
      .query({
        query: gql`
          query TestConnection {
            __typename
          }
        `,
        errorPolicy: "all",
      })
      .then(() => setIsConnected(true))
      .catch(() => setIsConnected(false));
  };

  const contextValue: ApolloContextType = {
    client,
    reconnect,
    isConnected,
  };

  return (
    <ApolloContext.Provider value={contextValue}>
      <BaseApolloProvider client={client}>{children}</BaseApolloProvider>
    </ApolloContext.Provider>
  );
};
