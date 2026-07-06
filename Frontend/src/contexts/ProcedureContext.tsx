import React, {
  createContext,
  useContext,
  useCallback,
  useEffect,
} from "react";
import { ApolloError, useLazyQuery, useMutation } from "@apollo/client";
import {
  GetLoadedProcedureDocument,
  GetLoadedProcedureQuery,
  LoadProcedureDocument,
  LoadProcedureMutation,
  LoadProcedureMutationVariables,
  UnloadProcedureDocument,
  UnloadProcedureMutation,
} from "../__generated__/graphql";
import { isBackendConnectionError } from "../utils/apolloErrorUtils";
import { createLogger } from "../utils/logger";

const log = createLogger("Procedure");

type LoadedProcedureType = NonNullable<
  GetLoadedProcedureQuery["loadedProcedure"]
>;

interface ProcedureContextValue {
  loadedProcedure: LoadedProcedureType | null;
  isLoading: boolean;
  error: ApolloError | undefined;
  isBackendError: boolean;
  loadProcedure: (id: string) => Promise<void>;
  unloadProcedure: () => Promise<void>;
  refetchLoadedProcedure: () => Promise<void>;
}

const ProcedureContext = createContext<ProcedureContextValue | undefined>(
  undefined,
);

export const ProcedureProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [getLoadedProcedure, { data, loading, error, refetch }] =
    useLazyQuery<GetLoadedProcedureQuery>(GetLoadedProcedureDocument, {
      fetchPolicy: "network-only",
    });

  const [loadProcedureMutation, { loading: loadingMutation }] = useMutation<
    LoadProcedureMutation,
    LoadProcedureMutationVariables
  >(LoadProcedureDocument);

  const [unloadProcedureMutation, { loading: unloadingMutation }] =
    useMutation<UnloadProcedureMutation>(UnloadProcedureDocument);

  useEffect(() => {
    getLoadedProcedure();
  }, [getLoadedProcedure]);

  const loadProcedure = useCallback(
    async (id: string) => {
      try {
        const result = await loadProcedureMutation({
          variables: { id },
        });

        if (result.data?.loadProcedure.procedure) {
          await refetch();
        }
      } catch (err) {
        log.error("Failed to load procedure:", err);
        throw err;
      }
    },
    [loadProcedureMutation, refetch],
  );

  const unloadProcedure = useCallback(async () => {
    try {
      const result = await unloadProcedureMutation();

      if (result.data?.unloadProcedure.success) {
        await refetch();
      }
    } catch (err) {
      log.error("Failed to unload procedure:", err);
      throw err;
    }
  }, [unloadProcedureMutation, refetch]);

  const refetchLoadedProcedure = useCallback(async () => {
    if (refetch) {
      await refetch();
    }
  }, [refetch]);

  const loadedProcedure = data?.loadedProcedure || null;
  const isLoading = loading || loadingMutation || unloadingMutation;
  const isBackendError = isBackendConnectionError(error);

  return (
    <ProcedureContext.Provider
      value={{
        loadedProcedure,
        isLoading,
        error,
        isBackendError,
        loadProcedure,
        unloadProcedure,
        refetchLoadedProcedure,
      }}
    >
      {children}
    </ProcedureContext.Provider>
  );
};

// eslint-disable-next-line react-refresh/only-export-components
export const useProcedure = () => {
  const context = useContext(ProcedureContext);
  if (!context) {
    throw new Error("useProcedure must be used within a ProcedureProvider");
  }
  return context;
};
