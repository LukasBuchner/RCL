import { useMemo } from "react";
import type { GraphQLFormattedError } from "graphql";
import type { AgentSerializationViolation } from "../__generated__/graphql";
import type { ErrorContext } from "../types/error/errorTypes";

type ExecutionErrorHandler = (gqlError: GraphQLFormattedError) => void;

interface ExecutionErrorHandlerDeps {
  setViolations: (violations: AgentSerializationViolation[]) => void;
  setShowModal: (show: boolean) => void;
  handleError: (error: Error, context?: ErrorContext) => void;
}

/**
 * Returns a stable map of GraphQL error code → handler for execution mutations.
 * Callers dispatch to this map after catching an ApolloError so each error code
 * has a single, focused response with no branching inside onPlayClick.
 *
 * AGENT_SERIALIZATION_VIOLATION: populates the violation state and opens the
 * modal so the user can see which nodes need FS ordering before executing.
 *
 * EXECUTION_ALREADY_IN_PROGRESS: surfaces a warning toast via the shared error
 * infrastructure instead of silently ignoring the duplicate-start attempt.
 */
export const useExecutionErrorHandlers = ({
  setViolations,
  setShowModal,
  handleError,
}: ExecutionErrorHandlerDeps): Record<string, ExecutionErrorHandler> => {
  return useMemo(
    () => ({
      AGENT_SERIALIZATION_VIOLATION: (error) => {
        const violations =
          (error.extensions?.data as AgentSerializationViolation[]) ?? [];
        setViolations(violations);
        setShowModal(true);
      },
      EXECUTION_ALREADY_IN_PROGRESS: () => {
        handleError(new Error("A procedure is already running"), {
          source: "graphql",
        });
      },
    }),
    [setViolations, setShowModal, handleError],
  );
};
