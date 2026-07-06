import React from "react";
import { ErrorProvider } from "../contexts/ErrorContext";
import { LoadingProvider } from "../contexts/LoadingContext";
import { ProcedureProvider } from "../contexts/ProcedureContext";
import { ErrorBoundary, ErrorToastContainer } from "./error";
import { useGlobalTooltipHide } from "../hooks";
import { createLogger } from "../utils/logger";

const log = createLogger("ErrorBoundary");

interface AppProvidersProps {
  children: React.ReactNode;
}

/**
 * Root provider component that wraps the application with all necessary context providers.
 * Provides error handling, loading states, and procedure management.
 * Note: WebSocketProvider and ApolloProvider are in main.tsx to ensure proper dependency order.
 * @param {AppProvidersProps} props - Component properties
 * @returns {React.ReactElement} The wrapped application with all providers
 */
export const AppProviders: React.FC<AppProvidersProps> = ({ children }) => {
  useGlobalTooltipHide();

  return (
    <ErrorProvider>
      <LoadingProvider>
        <ProcedureProvider>
          <ErrorBoundary
            onError={(error, errorInfo) => {
              log.error("Global error boundary:", error, errorInfo);
            }}
          >
            {children}
            <ErrorToastContainer />
          </ErrorBoundary>
        </ProcedureProvider>
      </LoadingProvider>
    </ErrorProvider>
  );
};
