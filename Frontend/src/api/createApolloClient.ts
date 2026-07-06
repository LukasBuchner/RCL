import {
  ApolloClient,
  from,
  HttpLink,
  InMemoryCache,
  split,
} from "@apollo/client";
import { createClient } from "graphql-ws";
import { GraphQLWsLink } from "@apollo/client/link/subscriptions";
import { getMainDefinition } from "@apollo/client/utilities";
import possibleTypes from "../../possibleTypes.json";
import { createErrorLink } from "../apollo/errorLink";
import { GraphQLSettings } from "../types/settings";
import { createLogger } from "../utils/logger";

const log = createLogger("Apollo");

/**
 * Calculates the retry delay for WebSocket reconnection using exponential backoff.
 * @param {number} retries - The number of retry attempts that have been made
 * @returns {Promise<void>} A promise that resolves after the calculated delay
 */
function retryDelayWithExponentialBackoff(retries: number): Promise<void> {
  // Start with 1 second, double each time, max out at 30 seconds
  const delay = Math.min(1000 * Math.pow(2, retries), 30000);
  log.info(`WebSocket reconnection attempt ${retries + 1} in ${delay}ms`);
  return new Promise((resolve) => setTimeout(resolve, delay));
}

/**
 * Creates an Apollo Client with GraphQL HTTP and WebSocket connections.
 * Includes automatic reconnection logic with exponential backoff for WebSocket connections.
 * @param {GraphQLSettings} settings - GraphQL configuration settings
 * @param {(connected: boolean) => void} [onWebSocketStatusChange] - Optional callback for WebSocket connection status changes
 * @returns {ApolloClient} Configured Apollo Client instance
 */
export function createApolloClient(
  settings: GraphQLSettings,
  onWebSocketStatusChange?: (connected: boolean) => void,
) {
  // HTTP link
  const httpLink = new HttpLink({
    uri: settings.httpEndpoint,
  });

  // WebSocket link for subscriptions (only if enabled)
  const wsLink = settings.enableSubscriptions
    ? new GraphQLWsLink(
        createClient({
          url: settings.wsEndpoint,
          connectionParams: {
            timeout: settings.timeout,
          },
          // Enable automatic reconnection with exponential backoff
          retryAttempts: Infinity, // Keep trying to reconnect indefinitely
          shouldRetry: () => true, // Always attempt to reconnect
          retryWait: retryDelayWithExponentialBackoff,
          // Lazy connection - only connect when needed
          lazy: false,
          on: {
            connected: () => {
              log.info("WebSocket connected");
              onWebSocketStatusChange?.(true);
            },
            closed: (event) => {
              log.warn("WebSocket closed", event);
              onWebSocketStatusChange?.(false);
            },
            error: (error) => {
              log.error("WebSocket error", error);
              // Note: Don't change connection status here as it will be handled by closed event
            },
          },
        }),
      )
    : null;

  // Split link for subscriptions vs queries/mutations
  const splitLink = wsLink
    ? split(
        ({ query }) => {
          const definition = getMainDefinition(query);
          return (
            definition.kind === "OperationDefinition" &&
            definition.operation === "subscription"
          );
        },
        wsLink,
        httpLink,
      )
    : httpLink;

  // Create error link
  const errorLink = createErrorLink();

  // Combine links
  const link = from([errorLink, splitLink]);

  // Create Apollo Client with custom type policies
  return new ApolloClient({
    link,
    cache: new InMemoryCache({
      possibleTypes,
      typePolicies: {
        Query: {
          fields: {
            nodeById: {
              read(_, { args, toReference }) {
                return toReference({
                  __typename: "Node",
                  id: args?.id,
                });
              },
            },
            agentById: {
              read(_, { args, toReference }) {
                return toReference({
                  __typename: "Agent",
                  id: args?.id,
                });
              },
            },
            skillById: {
              read(_, { args, toReference }) {
                return toReference({
                  __typename: "Skill",
                  id: args?.id,
                });
              },
            },
            dependencyEdges: {
              merge(_, incoming) {
                return incoming;
              },
            },
            nodes: {
              merge(_, incoming) {
                return incoming;
              },
            },
          },
        },
        Subscription: {
          fields: {
            nodesChanged: {
              merge(_, incoming) {
                return incoming;
              },
            },
            dependencyEdgesChanged: {
              merge(_, incoming) {
                return incoming;
              },
            },
          },
        },
        Agent: {
          fields: {
            skills: {
              merge(_, incoming) {
                return incoming;
              },
            },
          },
        },
        Skill: {
          fields: {
            agents: {
              merge(_, incoming) {
                return incoming;
              },
            },
            properties: {
              merge(_, incoming) {
                return incoming;
              },
            },
          },
        },
      },
    }),
    defaultOptions: {
      watchQuery: {
        errorPolicy: "all",
      },
      query: {
        errorPolicy: "all",
      },
    },
  });
}
