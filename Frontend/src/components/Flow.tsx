import { ApolloError, useLazyQuery, useSubscription } from "@apollo/client";
import {
  GetDependencyEdgesDocument,
  GetDependencyEdgesQuery,
  GetNodesDocument,
  GetNodesQuery,
  OnDependencyEdgesChangedDocument,
  OnDependencyEdgesChangedSubscription,
  OnNodesChangedDocument,
  OnNodesChangedSubscription,
} from "../__generated__/graphql.ts";
import { useError } from "../contexts/ErrorContext";
import { useProcedure } from "../contexts/ProcedureContext";
import { useWebSocket } from "../contexts/WebSocketContext";
import {
  applyEdgeChanges,
  applyNodeChanges,
  Connection,
  Controls,
  Edge,
  EdgeChange,
  MiniMap,
  Node,
  NodeChange,
  Panel,
  ReactFlow,
  XYPosition,
} from "@xyflow/react";
import { mapNodeFieldsFragmentsToAppNodes } from "../types/mapping/node/mapNodeFieldsFragmentsToAppNodes.ts";
import { mapEdgeFieldsFragmentsToAppEdges } from "../types/mapping/edge/mapEdgeFieldsFragmentsToAppEdges.ts";
import { createLogger } from "../utils/logger";
import { AppNode } from "../types/nodeTypes.ts";
import React, { useCallback, useEffect, useRef, useState } from "react";
import { motion } from "framer-motion";
import { v4 as uuid } from "uuid";
import { useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import SkillExecutionNode from "./nodes/skill/SkillExecutionNode.tsx";
import TaskNode from "./nodes/task/TaskNode.tsx";
import PlayheadNode from "./nodes/playhead/PlayheadNode.tsx";
import RouterNode from "./nodes/router/RouterNode.tsx";
import DependencyEdge from "./DependencyEdge.tsx";
import { LoadingState } from "./loading";
import { BackendErrorOverlay, ErrorState } from "./error";
import { useApolloError } from "../hooks";
import { useKeyboardShortcuts } from "../hooks/useKeyboardShortcuts";
import {
  compositeCommand,
  createEdgeCommand,
  deleteEdgeCommand,
  deleteNodeCommand,
  FlowCommand,
  FlowCommandContext,
  FlowPersisterErrorEvent,
  FlowUndoProvider,
  moveNodeCommand,
  NoOpFlowPersister,
  useFlowPersister,
  useFlowUndoManager,
} from "../undo";
import ConfiguredBackground from "./ConfiguredBackground.tsx";
import TaskConfigModal from "./modals/TaskConfigModal.tsx";
import SkillConfigModal from "./modals/SkillConfigModal.tsx";
import RouterConfigModal from "./modals/RouterConfigModal.tsx";
import { isBackendConnectionError } from "../utils/apolloErrorUtils";
import { useConnectionStore } from "../stores/connectionStore";
import { useExecutionStore } from "../stores/executionStore";
import {
  computeValidTargetNodeIds,
  isConnectionValid,
} from "../utils/connectionValidation";
import "@xyflow/react/dist/style.css";

const nodeTypes = {
  taskNode: TaskNode,
  skillExecutionNode: SkillExecutionNode,
  playhead: PlayheadNode,
  routerNode: RouterNode,
};
const edgeTypes = { dependencyEdge: DependencyEdge };

const log = createLogger("Flow");

/**
 * Extracts the user-facing message from a mutation error. Apollo wraps server
 * errors in {@link ApolloError}; the validator's human-readable message lives
 * in `graphQLErrors[0].message`. Falls back to the top-level `message` for
 * network errors and to a stringified form for anything else.
 */
function extractUserFacingMessage(error: Error): string {
  if (error instanceof ApolloError) {
    const first = error.graphQLErrors[0]?.message;
    if (first && first.length > 0) return first;
  }
  if (error.message && error.message.length > 0) return error.message;
  return String(error);
}

export default function Flow() {
  const { t } = useTranslation();
  const { loadedProcedure, isBackendError: procedureBackendError } =
    useProcedure();
  const { isConnected: isWebSocketConnected } = useWebSocket();
  const [nodes, setNodes] = useState<AppNode[]>([]);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [searchParams] = useSearchParams();
  const reactFlowInstance = useRef<{
    setCenter: (
      x: number,
      y: number,
      options?: { zoom?: number; duration?: number },
    ) => void;
  } | null>(null);

  // Undo/redo manager — see src/undo/useFlowUndoManager. Uses a ref-based
  // context so the manager always sees the latest React Flow state setters.
  // The persister defaults to NoOp so first-render dispatches (rare but
  // possible during fast mounts) do not try to call an undefined endpoint.
  const { addError } = useError();
  const handlePersisterError = useCallback(
    (event: FlowPersisterErrorEvent) => {
      const message = extractUserFacingMessage(event.error);
      addError({ message, severity: "error" });

      switch (event.operation) {
        case "createEdge":
          setEdges((current) => current.filter((e) => e.id !== event.edge.id));
          break;
        case "createNode":
          setNodes((current) => current.filter((n) => n.id !== event.node.id));
          setEdges((current) =>
            current.filter(
              (e) => e.source !== event.node.id && e.target !== event.node.id,
            ),
          );
          break;
        // Update / delete failures rely on the next subscription emission to
        // reconcile local state — there is no captured pre-image to restore
        // here. The toast tells the user what happened.
        case "updateNode":
        case "deleteNode":
        case "deleteEdge":
          break;
      }
    },
    [addError, setNodes, setEdges],
  );
  const persister = useFlowPersister(handlePersisterError);
  const ctxRef = useRef<FlowCommandContext>({
    setNodes: () => {},
    setEdges: () => {},
    persister: NoOpFlowPersister,
  });
  const undo = useFlowUndoManager(ctxRef);
  useEffect(() => {
    ctxRef.current = { setNodes, setEdges, persister };
  }, [setNodes, setEdges, persister]);

  // Enable keyboard shortcuts for copy/cut/paste and undo/redo
  useKeyboardShortcuts({
    onUndo: undo.undo,
    onRedo: undo.redo,
  });

  // Tracks positions at drag start so we can emit a single MoveNodeCommand at
  // drag stop, coalescing the many intermediate `position` changes React Flow
  // emits per frame into one undoable unit per gesture.
  const dragStartPositions = useRef<Map<string, XYPosition>>(new Map());

  // Sync execution state into the store so FlowNavbar can reflect it
  useEffect(() => {
    const running = nodes.some(
      (n) => n.data && "isExecuting" in n.data && n.data.isExecuting,
    );
    useExecutionStore.getState().setRunning(running);
  }, [nodes]);

  // --- Loading and Error States ---
  const [nodesLoading, setNodesLoading] = useState(true);
  const [edgesLoading, setEdgesLoading] = useState(true);
  const [nodesError, setNodesError] = useState<ApolloError | undefined>(
    undefined,
  );
  const [edgesError, setEdgesError] = useState<ApolloError | undefined>(
    undefined,
  );

  // Reset loading states when procedure changes
  useEffect(() => {
    if (!loadedProcedure) {
      // No procedure loaded, set loading to false and clear any data
      setNodesLoading(false);
      setEdgesLoading(false);
      setNodes([]);
      setEdges([]);
      setNodesError(undefined);
      setEdgesError(undefined);
    } else {
      // Procedure loaded, reset to loading state
      setNodesLoading(true);
      setEdgesLoading(true);
    }
  }, [loadedProcedure]);

  // Flag to prevent duplicate mutations when updates come from subscriptions
  const isUpdatingFromSubscription = useRef(false);

  // Handle Apollo errors
  useApolloError(nodesError, { componentName: "Flow", operation: "GetNodes" });
  useApolloError(edgesError, {
    componentName: "Flow",
    operation: "GetDependencyEdges",
  });

  // --- Initial Data Queries ---
  const [fetchNodes] = useLazyQuery<GetNodesQuery>(GetNodesDocument, {
    fetchPolicy: "network-only",
    onCompleted: (data) => {
      if (data?.nodes !== undefined) {
        const mappedNodes = mapNodeFieldsFragmentsToAppNodes(data.nodes);

        const routerMetadataStr = localStorage.getItem("routerMetadata");
        const routerMetadataMap = routerMetadataStr
          ? JSON.parse(routerMetadataStr)
          : {};

        const nodesWithRouterMetadata = mappedNodes.map((node) => {
          const metadata = routerMetadataMap[node.id];
          if (metadata && node.type === "taskNode") {
            return {
              ...node,
              type: "routerNode" as const,
              data: {
                ...node.data,
                selector: metadata.selector,
                branches: metadata.branches,
                selectedBranch: metadata.selectedBranch,
              },
            };
          }
          return node;
        });

        setNodes(nodesWithRouterMetadata);
        setNodesLoading(false);
        setNodesError(undefined);
      }
    },
    onError: (error) => {
      setNodesError(error);
      setNodesLoading(false);
    },
  });

  const [fetchEdges] = useLazyQuery<GetDependencyEdgesQuery>(
    GetDependencyEdgesDocument,
    {
      fetchPolicy: "network-only",
      onCompleted: (data) => {
        if (data?.dependencyEdges !== undefined) {
          const mappedEdges = mapEdgeFieldsFragmentsToAppEdges(
            data.dependencyEdges,
          );
          setEdges(mappedEdges);
          setEdgesLoading(false);
          setEdgesError(undefined);
        }
      },
      onError: (error) => {
        setEdgesError(error);
        setEdgesLoading(false);
      },
    },
  );

  // Fetch initial data when procedure is loaded
  useEffect(() => {
    if (loadedProcedure) {
      fetchNodes();
      fetchEdges();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadedProcedure]);

  // --- Mutations ---
  // Node/edge create + delete mutations are owned by `useFlowPersister` so the
  // undo system can serialize them against their inverses on the wire.
  // `updateNode` stays absent until the update paths are ported to commands.

  // --- Subscriptions ---
  // These subscriptions return the complete list, not incremental updates
  // Only subscribe when a procedure is loaded
  useSubscription<OnNodesChangedSubscription>(OnNodesChangedDocument, {
    skip: !loadedProcedure,
    onData: ({ client, data: { data } }) => {
      if (data?.nodesChanged) {
        // Update Apollo cache for other components
        client.writeQuery({
          query: GetNodesDocument,
          data: { nodes: data.nodesChanged },
        });

        // Set flag to prevent onNodesChange from triggering mutations
        isUpdatingFromSubscription.current = true;

        const mappedNodes = mapNodeFieldsFragmentsToAppNodes(data.nodesChanged);

        log.debugLazy(() => [
          "node sub",
          {
            payloadCount: data.nodesChanged.length,
            mappedCount: mappedNodes.length,
            withParentId: mappedNodes.filter((n) => n.parentId).length,
            routerCount: mappedNodes.filter((n) => n.type === "routerNode")
              .length,
          },
        ]);

        // Restore router metadata for nodes that were created as routers
        // but came back from the backend as TaskNodes
        const routerMetadataStr = localStorage.getItem("routerMetadata");
        const routerMetadataMap = routerMetadataStr
          ? JSON.parse(routerMetadataStr)
          : {};

        const nodesWithRouterMetadata = mappedNodes.map((node) => {
          const metadata = routerMetadataMap[node.id];
          if (metadata && node.type === "taskNode") {
            log.traceLazy(() => [
              "restoring router metadata",
              node.id,
              metadata,
            ]);
            return {
              ...node,
              type: "routerNode" as const,
              data: {
                ...node.data,
                selector: metadata.selector,
                branches: metadata.branches,
                selectedBranch: metadata.selectedBranch,
              },
            };
          }
          return node;
        });

        log.debugLazy(() => [
          "node sub -> setNodes",
          { finalCount: nodesWithRouterMetadata.length },
        ]);

        setNodes(nodesWithRouterMetadata);
        setNodesLoading(false);
        setNodesError(undefined);

        // Reset flag after React has processed the state update
        setTimeout(() => {
          isUpdatingFromSubscription.current = false;
        }, 0);
      }
    },
    onError: (error) => {
      setNodesError(error);
      setNodesLoading(false);
    },
  });

  useSubscription<OnDependencyEdgesChangedSubscription>(
    OnDependencyEdgesChangedDocument,
    {
      skip: !loadedProcedure,
      onData: ({ client, data: { data } }) => {
        if (data?.dependencyEdgesChanged) {
          // Update Apollo cache for other components
          client.writeQuery({
            query: GetDependencyEdgesDocument,
            data: { dependencyEdges: data.dependencyEdgesChanged },
          });

          const mappedEdges = mapEdgeFieldsFragmentsToAppEdges(
            data.dependencyEdgesChanged,
          );
          log.debugLazy(() => [
            "edge sub",
            {
              payloadCount: data.dependencyEdgesChanged.length,
              mappedCount: mappedEdges.length,
              prevStateCount: edges.length,
            },
          ]);
          setEdges(mappedEdges);
          setEdgesLoading(false);
          setEdgesError(undefined);
        }
      },
      onError: (error) => {
        setEdgesError(error);
        setEdgesLoading(false);
      },
    },
  );

  // --- Handlers ---
  const onNodesChange = useCallback(
    (changes: NodeChange<AppNode>[]) => {
      // Split removes out — they are routed through the undo manager so
      // deletions become undoable. Subscription-sourced updates bypass
      // command routing and apply verbatim.
      if (isUpdatingFromSubscription.current) {
        setNodes((nds) => applyNodeChanges(changes, nds));
        return;
      }

      const removes = changes.filter((c) => c.type === "remove");
      const others = changes.filter((c) => c.type !== "remove");

      if (others.length > 0) {
        setNodes((nds) => applyNodeChanges(others, nds));
      }

      if (removes.length === 0) return;

      const commands: FlowCommand[] = [];
      for (const change of removes) {
        const node = nodes.find((n) => n.id === change.id);
        if (!node) continue;
        const incident = edges.filter(
          (e) => e.source === node.id || e.target === node.id,
        );
        commands.push(deleteNodeCommand(node, incident));
      }
      if (commands.length === 1) {
        undo.dispatch(commands[0]);
      } else if (commands.length > 1) {
        undo.dispatch(
          compositeCommand(commands, "Delete nodes", "Restore nodes"),
        );
      }
    },
    [setNodes, nodes, edges, undo],
  );

  const onEdgesChange = useCallback(
    (changes: EdgeChange[]) => {
      const removes = changes.filter((c) => c.type === "remove");
      const others = changes.filter((c) => c.type !== "remove");

      if (others.length > 0) {
        setEdges((eds) => applyEdgeChanges(others, eds));
      }

      if (removes.length === 0) return;

      const commands: FlowCommand[] = [];
      for (const change of removes) {
        const edge = edges.find((e) => e.id === change.id);
        if (!edge) continue;
        commands.push(deleteEdgeCommand(edge));
      }
      if (commands.length === 1) {
        undo.dispatch(commands[0]);
      } else if (commands.length > 1) {
        undo.dispatch(
          compositeCommand(commands, "Delete edges", "Restore edges"),
        );
      }
    },
    [setEdges, edges, undo],
  );

  const onConnect = useCallback(
    (connection: Connection) => {
      const newEdge: Edge = {
        ...connection,
        type: "dependencyEdge",
        id: uuid(),
      };
      // Dispatching routes the local insert + the persistence call through a
      // single command so the connection is undoable.
      undo.dispatch(createEdgeCommand(newEdge));
    },
    [undo],
  );

  const onConnectStart = useCallback(
    (
      _event: MouseEvent | TouchEvent,
      params: { nodeId: string | null; handleId: string | null },
    ) => {
      if (!params.nodeId) return;
      const validTargets = computeValidTargetNodeIds(params.nodeId, nodes);
      useConnectionStore
        .getState()
        .startConnection(params.nodeId, params.handleId, validTargets);
    },
    [nodes],
  );

  const onConnectEnd = useCallback(() => {
    useConnectionStore.getState().endConnection();
  }, []);

  const onNodeDragStart = useCallback(
    (
      _event: React.MouseEvent,
      _node: Node<AppNode["data"]>,
      draggedNodes: Node<AppNode["data"]>[],
    ) => {
      dragStartPositions.current = new Map(
        draggedNodes.map((n) => [n.id, { ...n.position }]),
      );
    },
    [],
  );

  const onNodeDragStop = useCallback(
    (
      _event: React.MouseEvent,
      _node: Node<AppNode["data"]>,
      draggedNodes: Node<AppNode["data"]>[],
    ) => {
      const starts = dragStartPositions.current;
      dragStartPositions.current = new Map();
      const commands: FlowCommand[] = [];
      for (const n of draggedNodes) {
        const from = starts.get(n.id);
        if (!from) continue;
        if (from.x === n.position.x && from.y === n.position.y) continue;
        commands.push(moveNodeCommand(n.id, from, { ...n.position }));
      }
      if (commands.length === 1) {
        undo.dispatch(commands[0]);
      } else if (commands.length > 1) {
        undo.dispatch(
          compositeCommand(commands, "Move nodes", "Move nodes back"),
        );
      }
    },
    [undo],
  );

  const handleIsValidConnection = useCallback(
    (connection: Connection | Edge) => {
      return isConnectionValid(
        connection.source,
        connection.target,
        nodes,
        connection.targetHandle ?? null,
      );
    },
    [nodes],
  );

  // Handle deep linking
  useEffect(() => {
    const nodeId = searchParams.get("node");
    const view = searchParams.get("view");

    if (nodeId && nodes.length > 0 && reactFlowInstance.current) {
      const node = nodes.find((n) => n.id === nodeId);
      if (node) {
        // Focus on the specific node
        reactFlowInstance.current.setCenter(node.position.x, node.position.y, {
          zoom: 1.5,
          duration: 800,
        });
        // Select the node
        setNodes((nds) =>
          nds.map((n) => ({
            ...n,
            selected: n.id === nodeId,
          })),
        );
      }
    }

    // Handle timeline view parameter
    if (view === "timeline") {
      // Timeline visibility is handled in RootLayout based on the route
    }
  }, [nodes, searchParams]);

  // --- Render ---
  log.traceLazy(() => ["render", { nodes: nodes.length, edges: edges.length }]);

  // Check if there's a backend connection error from any source
  const hasBackendError =
    isBackendConnectionError(nodesError) ||
    isBackendConnectionError(edgesError) ||
    procedureBackendError ||
    !isWebSocketConnected;

  // Show backend error overlay if connection fails
  if (hasBackendError) {
    return (
      <BackendErrorOverlay
        onRetry={() => {
          window.location.reload();
        }}
      />
    );
  }

  // Only show loading state if a procedure is loaded and data is still loading
  if (loadedProcedure && (nodesLoading || edgesLoading)) {
    return <LoadingState text={t("loading.flowData")} />;
  }

  // Show regular error state for non-connection errors
  const queryError = nodesError || edgesError;
  if (queryError) {
    return (
      <ErrorState
        title={t("flowErrors.loadingError")}
        message={t("flowErrors.loadingErrorDescription")}
        severity="error"
        onRetry={() => {
          window.location.reload();
        }}
      />
    );
  }

  return (
    <FlowUndoProvider manager={undo} persister={persister}>
      <motion.div
        className="d-flex flex-column h-100 min-vh-0"
        initial={{ opacity: 0, scale: 0.98 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{
          duration: 0.8,
          ease: [0.25, 0.46, 0.45, 0.94],
          scale: {
            duration: 1,
            ease: [0.34, 1.56, 0.64, 1],
          },
        }}
      >
        {/* Only show "No Procedure Loaded" banner if there's NO backend error */}
        {!loadedProcedure && !hasBackendError && (
          <div
            className="alert alert-info d-flex align-items-center"
            role="alert"
            style={{
              position: "absolute",
              top: "50%",
              left: "50%",
              transform: "translate(-50%, -50%)",
              zIndex: 1000,
              maxWidth: "600px",
              boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
            }}
          >
            <i className="bi bi-info-circle-fill me-2"></i>
            <div>
              <strong>{t("flow.noProcedureLoaded")}</strong>
              <div className="small">{t("flow.noProcedureDescription")}</div>
            </div>
          </div>
        )}
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          onConnectStart={onConnectStart}
          onConnectEnd={onConnectEnd}
          onNodeDragStart={onNodeDragStart}
          onNodeDragStop={onNodeDragStop}
          isValidConnection={handleIsValidConnection}
          onInit={(instance) => {
            reactFlowInstance.current = instance;
          }}
          nodeTypes={nodeTypes}
          edgeTypes={edgeTypes}
          zoomOnDoubleClick={false}
          fitView={true}
          style={{
            width: "100%",
            height: "100%",
            backgroundColor: "var(--app-react-flow-bg)",
          }}
          className="flex-grow-1"
        >
          <Controls />
          <Panel position="top-right">
            <div className="btn-group" role="group" aria-label="Undo and redo">
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                disabled={!undo.canUndo}
                onClick={undo.undo}
                title={undo.nextUndoDescription ?? t("actions.undo")}
                aria-label={t("actions.undo")}
              >
                <i
                  className="bi bi-arrow-counterclockwise"
                  aria-hidden="true"
                />
              </button>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                disabled={!undo.canRedo}
                onClick={undo.redo}
                title={undo.nextRedoDescription ?? t("actions.redo")}
                aria-label={t("actions.redo")}
              >
                <i className="bi bi-arrow-clockwise" aria-hidden="true" />
              </button>
            </div>
          </Panel>
          <MiniMap
            nodeStrokeWidth={3}
            zoomable
            pannable
            nodeColor={(node: Node<AppNode>) => {
              return node.style?.color || "var(--app-card-bg)";
            }}
          />
          <ConfiguredBackground />
        </ReactFlow>

        {/* Modals rendered locally within Flow component */}
        <TaskConfigModal />
        <SkillConfigModal />
        <RouterConfigModal />
      </motion.div>
    </FlowUndoProvider>
  );
}
