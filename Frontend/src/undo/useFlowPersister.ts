import { Edge } from "@xyflow/react";
import { useMutation } from "@apollo/client";
import { useMemo, useRef } from "react";
import {
  CreateDependencyEdgeDocument,
  CreateDependencyEdgeInput,
  CreateDependencyEdgeMutation,
  CreateNodeDocument,
  CreateNodeInput,
  CreateNodeMutation,
  DeleteDependencyEdgeDocument,
  DeleteDependencyEdgeInput,
  DeleteDependencyEdgeMutation,
  DeleteNodeDocument,
  DeleteNodeInput,
  DeleteNodeMutation,
  UpdateNodeDocument,
  UpdateNodeInput,
  UpdateNodeMutation,
} from "../__generated__/graphql";
import { mapAppNodeToNodeInput } from "../types/mapping/node/mapAppNodeToNodeInput";
import { mapEdgeToDependencyEdgeInput } from "../types/mapping/edge/mapEdgeToDependencyEdgeInput";
import { AppNode } from "../types/nodeTypes";
import {
  FlowPersister,
  FlowPersisterErrorEvent,
  FlowPersisterErrorHandler,
} from "./types";
import { createLogger } from "../utils/logger";

const log = createLogger("FlowPersister");

/**
 * Returns a stable {@link FlowPersister} that routes command side-effects to
 * Apollo mutations. Operations are serialized through an internal promise
 * chain so the network order matches the dispatch order — essential for
 * delete→undo round-trips, where the server must see `delete` before the
 * paired `create`, and vice versa.
 *
 * Individual mutation failures are reported through the optional
 * {@link FlowPersisterErrorHandler} so the host can roll back the optimistic
 * local change and surface the cause to the user. Failures never break the
 * chain; a later operation stays in order even if an earlier one rejected.
 *
 * @param onError Optional callback invoked when a mutation fails. The handler
 *     receives a discriminated event identifying the operation and payload so
 *     it can roll back the right slice of local state.
 */
export function useFlowPersister(
  onError?: FlowPersisterErrorHandler,
): FlowPersister {
  const [createNode] = useMutation<CreateNodeMutation>(CreateNodeDocument);
  const [deleteNode] = useMutation<DeleteNodeMutation>(DeleteNodeDocument);
  const [updateNode] = useMutation<UpdateNodeMutation>(UpdateNodeDocument);
  const [createEdge] = useMutation<CreateDependencyEdgeMutation>(
    CreateDependencyEdgeDocument,
  );
  const [deleteEdge] = useMutation<DeleteDependencyEdgeMutation>(
    DeleteDependencyEdgeDocument,
  );

  const queueRef = useRef<Promise<unknown>>(Promise.resolve());
  const onErrorRef = useRef<FlowPersisterErrorHandler | undefined>(onError);
  onErrorRef.current = onError;

  const enqueue = (
    label: string,
    block: () => Promise<unknown>,
    buildEvent: (error: Error) => FlowPersisterErrorEvent,
  ): void => {
    const wrapped = async () => {
      try {
        await block();
      } catch (rawError) {
        const error =
          rawError instanceof Error ? rawError : new Error(String(rawError));
        onErrorRef.current?.(buildEvent(error));
        throw error;
      }
    };
    queueRef.current = queueRef.current
      .then(wrapped, wrapped)
      .catch((error) => {
        log.error(`FlowPersister: ${label} failed:`, error);
      });
  };

  return useMemo<FlowPersister>(
    () => ({
      persistNodeCreation: (node: AppNode) => {
        const nodeInput = mapAppNodeToNodeInput(node);
        enqueue(
          "createNode",
          () =>
            createNode({
              variables: {
                input: { nodeInput } as CreateNodeInput,
              },
            }),
          (error) => ({ operation: "createNode", node, error }),
        );
      },
      persistNodeDeletion: (id: string) => {
        enqueue(
          "deleteNode",
          () =>
            deleteNode({
              variables: {
                input: { id } as DeleteNodeInput,
              },
            }),
          (error) => ({ operation: "deleteNode", nodeId: id, error }),
        );
      },
      persistNodeUpdate: (node: AppNode) => {
        const nodeInput = mapAppNodeToNodeInput(node);
        enqueue(
          "updateNode",
          () =>
            updateNode({
              variables: {
                input: { nodeInput } as UpdateNodeInput,
              },
            }),
          (error) => ({ operation: "updateNode", node, error }),
        );
      },
      persistEdgeCreation: (edge: Edge) => {
        const dependencyEdge = mapEdgeToDependencyEdgeInput(edge);
        enqueue(
          "createEdge",
          () =>
            createEdge({
              variables: {
                input: { dependencyEdge } as CreateDependencyEdgeInput,
              },
            }),
          (error) => ({ operation: "createEdge", edge, error }),
        );
      },
      persistEdgeDeletion: (id: string) => {
        enqueue(
          "deleteEdge",
          () =>
            deleteEdge({
              variables: {
                input: { id } as DeleteDependencyEdgeInput,
              },
            }),
          (error) => ({ operation: "deleteEdge", edgeId: id, error }),
        );
      },
    }),
    [createNode, deleteNode, updateNode, createEdge, deleteEdge],
  );
}
