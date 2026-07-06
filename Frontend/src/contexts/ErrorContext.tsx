import React, { createContext, useContext, useState, useCallback } from "react";
import { AppError, ErrorContext as ErrorCtx } from "../types/error/errorTypes";
import { v4 as uuidv4 } from "uuid";

interface ErrorContextValue {
  errors: AppError[];
  addError: (error: Omit<AppError, "id" | "timestamp">) => void;
  removeError: (id: string) => void;
  clearErrors: () => void;
  handleError: (error: Error, context?: ErrorCtx) => void;
}

const ErrorContext = createContext<ErrorContextValue | undefined>(undefined);

export const ErrorProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [errors, setErrors] = useState<AppError[]>([]);

  const addError = useCallback((error: Omit<AppError, "id" | "timestamp">) => {
    const newError: AppError = {
      ...error,
      id: uuidv4(),
      timestamp: new Date(),
    };
    setErrors((prev) => [...prev, newError]);
  }, []);

  const removeError = useCallback((id: string) => {
    setErrors((prev) => prev.filter((error) => error.id !== id));
  }, []);

  const clearErrors = useCallback(() => {
    setErrors([]);
  }, []);

  const handleError = useCallback(
    (error: Error, context?: ErrorCtx) => {
      // Determine severity based on error type
      let severity: AppError["severity"] = "error";
      let code: string | undefined;

      // Handle GraphQL errors
      if (error.message.includes("GraphQL") || context?.source === "graphql") {
        if (error.message.includes("UNAUTHENTICATED")) {
          severity = "critical";
          code = "AUTH_ERROR";
        } else if (error.message.includes("NOT_FOUND")) {
          severity = "warning";
          code = "NOT_FOUND";
        }
      }

      // Handle network errors
      if (
        error.message.includes("NetworkError") ||
        context?.source === "network"
      ) {
        severity = "critical";
        code = "NETWORK_ERROR";
      }

      addError({
        message: error.message,
        severity,
        code,
        details: {
          stack: error.stack,
          context,
        },
      });
    },
    [addError],
  );

  return (
    <ErrorContext.Provider
      value={{ errors, addError, removeError, clearErrors, handleError }}
    >
      {children}
    </ErrorContext.Provider>
  );
};

// eslint-disable-next-line react-refresh/only-export-components
export const useError = () => {
  const context = useContext(ErrorContext);
  if (!context) {
    throw new Error("useError must be used within an ErrorProvider");
  }
  return context;
};
