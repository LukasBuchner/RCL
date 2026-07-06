import React, { createContext, useContext, useState, useCallback } from "react";

interface LoadingState {
  isLoading: boolean;
  loadingText?: string;
  progress?: number;
}

interface LoadingContextValue {
  globalLoading: LoadingState;
  loadingStates: Map<string, LoadingState>;
  setGlobalLoading: (state: LoadingState) => void;
  setLoading: (key: string, state: LoadingState) => void;
  clearLoading: (key: string) => void;
  isAnyLoading: () => boolean;
}

const LoadingContext = createContext<LoadingContextValue | undefined>(
  undefined,
);

export const LoadingProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [globalLoading, setGlobalLoading] = useState<LoadingState>({
    isLoading: false,
  });
  const [loadingStates, setLoadingStates] = useState<Map<string, LoadingState>>(
    new Map(),
  );

  const setLoading = useCallback((key: string, state: LoadingState) => {
    setLoadingStates((prev) => {
      const newMap = new Map(prev);
      newMap.set(key, state);
      return newMap;
    });
  }, []);

  const clearLoading = useCallback((key: string) => {
    setLoadingStates((prev) => {
      const newMap = new Map(prev);
      newMap.delete(key);
      return newMap;
    });
  }, []);

  const isAnyLoading = useCallback(() => {
    if (globalLoading.isLoading) return true;
    for (const [, state] of loadingStates) {
      if (state.isLoading) return true;
    }
    return false;
  }, [globalLoading.isLoading, loadingStates]);

  return (
    <LoadingContext.Provider
      value={{
        globalLoading,
        loadingStates,
        setGlobalLoading,
        setLoading,
        clearLoading,
        isAnyLoading,
      }}
    >
      {children}
    </LoadingContext.Provider>
  );
};

// eslint-disable-next-line react-refresh/only-export-components
export const useLoading = () => {
  const context = useContext(LoadingContext);
  if (!context) {
    throw new Error("useLoading must be used within a LoadingProvider");
  }
  return context;
};

// Hook for managing loading state with automatic cleanup
// eslint-disable-next-line react-refresh/only-export-components
export const useLoadingState = (key: string) => {
  const { setLoading, clearLoading } = useLoading();

  React.useEffect(() => {
    return () => {
      clearLoading(key);
    };
  }, [key, clearLoading]);

  const startLoading = useCallback(
    (loadingText?: string, progress?: number) => {
      setLoading(key, { isLoading: true, loadingText, progress });
    },
    [key, setLoading],
  );

  const stopLoading = useCallback(() => {
    clearLoading(key);
  }, [key, clearLoading]);

  const updateProgress = useCallback(
    (progress: number, loadingText?: string) => {
      setLoading(key, { isLoading: true, loadingText, progress });
    },
    [key, setLoading],
  );

  return { startLoading, stopLoading, updateProgress };
};
