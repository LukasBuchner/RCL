import { useState, useCallback } from "react";
import { useError } from "../contexts/ErrorContext";
import { useLoadingState } from "../contexts/LoadingContext";

interface AsyncState<T> {
  data: T | null;
  isLoading: boolean;
  error: Error | null;
  execute: (asyncFunction: () => Promise<T>) => Promise<T | null>;
  reset: () => void;
}

export function useAsyncState<T>(
  key: string,
  options?: {
    onSuccess?: (data: T) => void;
    onError?: (error: Error) => void;
    showErrorToast?: boolean;
  },
): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const { handleError } = useError();
  const { startLoading, stopLoading } = useLoadingState(key);

  const execute = useCallback(
    async (asyncFunction: () => Promise<T>): Promise<T | null> => {
      setIsLoading(true);
      setError(null);
      startLoading();

      try {
        const result = await asyncFunction();
        setData(result);
        options?.onSuccess?.(result);
        return result;
      } catch (err) {
        const error = err instanceof Error ? err : new Error(String(err));
        setError(error);

        if (options?.showErrorToast !== false) {
          handleError(error);
        }

        options?.onError?.(error);
        return null;
      } finally {
        setIsLoading(false);
        stopLoading();
      }
    },
    [startLoading, stopLoading, handleError, options],
  );

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setIsLoading(false);
    stopLoading();
  }, [stopLoading]);

  return { data, isLoading, error, execute, reset };
}
