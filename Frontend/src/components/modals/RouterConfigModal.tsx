import React, { useEffect, useReducer, useState } from "react";
import { Button, Card, Col, Form, Row } from "react-bootstrap";
import { UnifiedModal } from "../common/UnifiedModal";
import { MODAL_CONFIGS, useRouterModal } from "../../hooks/useRouterModal";
import { v4 as uuidv4 } from "uuid";
import { produce } from "immer";
import { useQuery } from "@apollo/client";
import { useError } from "../../hooks";
import { LoadingOverlay } from "../loading";
import { useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useReactFlow } from "@xyflow/react";
import {
  GetNodeByIdDocument,
  GetNodeByIdQuery,
} from "../../__generated__/graphql";
import { createNodeCommand, updateNodeCommand, useFlowUndo } from "../../undo";
import { RouterNode } from "../../types/nodeTypes";
import {
  ConditionBuilder,
  ConditionValue,
  SimpleCondition,
} from "../router/ConditionBuilder";
import { GetProcedureVariablesDocument } from "../../graphql/variables";
import {
  convertVariableFromGraphQL,
  VariableDefinition,
  VariableDefinitionGraphQL,
} from "../Variables/types";
import { useProcedure } from "../../contexts/ProcedureContext";
import {
  RouterSelectorMapper,
  type UIBranch,
} from "../../utils/routerSelectorMapping";
import { createLogger } from "../../utils/logger";

const log = createLogger("RouterConfigModal");

interface LocationState {
  position?: { x: number; y: number };
  parentId?: string;
}

export interface RouterBranch {
  id: string;
  name: string;
  condition: string;
  priority: number;
  targetNodeId?: string;
  useSimpleMode?: boolean; // Flag to indicate if using simple condition builder
}

interface RouterNodeState {
  id: string;
  position: { x: number; y: number };
  parentId?: string;
  name: string;
  description: string;
  selectorExpression: string;
  branches: RouterBranch[];
}

/**
 * Converts a condition string to ConditionBuilder format.
 * For simple conditions like 'variableName == "value"', extracts the parts.
 * For complex conditions, returns default values.
 */
function conditionStringToBuilderFormat(conditionStr: string): SimpleCondition {
  if (!conditionStr || conditionStr.trim() === "") {
    return {
      variable: "",
      operator: "equals",
      value: "",
      fullCondition: "", // Always include fullCondition, even if empty
    };
  }

  // Try to parse simple format: variable operator "value"
  const simpleMatch = conditionStr.match(
    /^(\w+)\s*(==|!=|>|<|>=|<=|contains|startsWith)\s*("(?:[^"]*)"|'(?:[^']*)'|[^\s]+)\s*$/,
  );
  if (simpleMatch) {
    const [, variable, operator, rawValue] = simpleMatch;
    const operatorMap: Record<string, SimpleCondition["operator"]> = {
      "==": "equals",
      "!=": "notEquals",
      ">": "greaterThan",
      "<": "lessThan",
      ">=": "greaterOrEqual",
      "<=": "lessOrEqual",
      contains: "contains",
      startsWith: "startsWith",
    };
    const value = rawValue.replace(/^['"]|['"]$/g, "");

    return {
      variable: variable || "",
      operator: operatorMap[operator] || "equals",
      value: value || "",
      fullCondition: conditionStr,
    };
  }

  // For complex or unparseable conditions, return empty state
  return {
    variable: "",
    operator: "equals",
    value: "",
    fullCondition: "", // Always include fullCondition, even if empty
  };
}

const blankFragment = (
  position: { x: number; y: number } = { x: 0, y: 0 },
  parentId?: string,
): RouterNodeState => ({
  id: uuidv4(),
  position,
  parentId,
  name: "",
  description: "",
  selectorExpression: "",
  branches: [
    {
      id: uuidv4(),
      name: "Default",
      condition: "",
      priority: 999,
      targetNodeId: undefined,
      useSimpleMode: true,
    },
  ],
});

type Action =
  | {
      type: "OPEN_CREATE";
      position: { x: number; y: number };
      parentId?: string;
    }
  | { type: "OPEN_EDIT"; fragment: RouterNodeState }
  | { type: "CLOSE" }
  | {
      type: "SET_FIELD";
      key: "name" | "description" | "selectorExpression";
      value: string;
    }
  | { type: "ADD_BRANCH" }
  | { type: "REMOVE_BRANCH"; branchId: string }
  | {
      type: "UPDATE_BRANCH";
      branchId: string;
      key: "name" | "condition";
      value: string;
    };

function reducer(state: RouterNodeState, action: Action): RouterNodeState {
  return produce(state, (draft) => {
    switch (action.type) {
      case "OPEN_CREATE":
        return blankFragment(action.position, action.parentId);
      case "OPEN_EDIT":
        return action.fragment;
      case "CLOSE":
        return blankFragment();
      case "SET_FIELD": {
        draft[action.key] = action.value;
        return;
      }
      case "ADD_BRANCH": {
        const newBranch: RouterBranch = {
          id: uuidv4(),
          name: `Branch ${draft.branches.length}`,
          condition: "",
          priority: draft.branches.length - 1,
          targetNodeId: undefined,
          useSimpleMode: true,
        };
        draft.branches.splice(draft.branches.length - 1, 0, newBranch);
        return;
      }
      case "REMOVE_BRANCH": {
        const index = draft.branches.findIndex((b) => b.id === action.branchId);
        if (index !== -1 && index < draft.branches.length - 1) {
          draft.branches.splice(index, 1);
          draft.branches.forEach((b, i) => {
            if (i < draft.branches.length - 1) {
              b.priority = i;
            }
          });
        }
        return;
      }
      case "UPDATE_BRANCH": {
        const branch = draft.branches.find((b) => b.id === action.branchId);
        if (branch) {
          branch[action.key] = action.value;
        }
        return;
      }
      default:
        return;
    }
  });
}

const RouterConfigModal: React.FC = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const locationState = location.state as LocationState | null;
  const { isModalOpen, isEditing, entityId, closeModal } = useRouterModal(
    MODAL_CONFIGS.ROUTER_CONFIG,
  );
  const { getNode } = useReactFlow();
  const { loadedProcedure } = useProcedure();

  // Both create and update flow through the undo manager so they are
  // undoable end-to-end. Loading flags stay `false` because dispatching a
  // command is synchronous — the persister handles network I/O on its own
  // serialized queue.
  const { manager: undoManager } = useFlowUndo();
  const createLoading = false;
  const updateLoading = false;
  const [node, dispatch] = useReducer(reducer, blankFragment());
  const { addError } = useError();

  /**
   * Local state for tracking partial conditions while user is building them.
   * This prevents the reset cycle where:
   * 1. User selects variable -> condition builder returns partial state with empty fullCondition
   * 2. Empty fullCondition gets stored in reducer
   * 3. Next render parses empty string -> returns empty state
   * 4. ConditionBuilder receives empty state and resets UI
   *
   * Solution: Keep partial conditions in local state, only persist complete conditions to reducer.
   */
  const [branchConditionStates, setBranchConditionStates] = useState<
    Record<string, SimpleCondition>
  >({});

  /**
   * Gets the condition value to display in the ConditionBuilder for a specific branch.
   * Prioritizes local partial state over persisted condition string to preserve user progress.
   *
   * @param branch The branch to get the condition value for
   * @returns The condition value in ConditionBuilder format
   */
  const getConditionValueForBranch = (
    branch: RouterBranch,
  ): SimpleCondition => {
    // If we have partial state for this branch (user is actively editing), use that
    if (branchConditionStates[branch.id]) {
      return branchConditionStates[branch.id];
    }
    // Otherwise, parse from the persisted condition string
    return conditionStringToBuilderFormat(branch.condition);
  };

  /**
   * Handles branch removal by dispatching the action and cleaning up local state.
   *
   * @param branchId The ID of the branch to remove
   */
  const handleRemoveBranch = (branchId: string) => {
    dispatch({ type: "REMOVE_BRANCH", branchId });
    // Clean up local condition state for the removed branch
    setBranchConditionStates((prev) => {
      const newState = { ...prev };
      delete newState[branchId];
      return newState;
    });
  };

  const { data: nodeData } = useQuery<GetNodeByIdQuery>(GetNodeByIdDocument, {
    variables: { id: entityId || "" },
    skip: !entityId,
    fetchPolicy: "no-cache",
  });

  const {
    data: procedureData,
    loading: variablesLoading,
    refetch: refetchVariables,
  } = useQuery(GetProcedureVariablesDocument, {
    variables: { procedureId: loadedProcedure?.id || "" },
    skip: !loadedProcedure?.id,
    fetchPolicy: "network-only",
  });

  const procedureVariables: VariableDefinition[] = (
    procedureData?.procedureById?.variables || []
  ).map((v: VariableDefinitionGraphQL) => convertVariableFromGraphQL(v));

  // Clean up local condition state when modal closes
  useEffect(() => {
    if (!isModalOpen) {
      setBranchConditionStates({});
    }
  }, [isModalOpen]);

  useEffect(() => {
    if (!isModalOpen) return;

    // Refetch variables when modal opens to get latest data (including auto-created skill output variables)
    refetchVariables();

    if (isEditing && entityId && nodeData?.nodeById) {
      if (nodeData.nodeById.__typename === "RouterNode") {
        const routerNode = nodeData.nodeById;
        dispatch({
          type: "OPEN_EDIT",
          fragment: {
            id: entityId,
            position: routerNode.position,
            parentId: routerNode.parentId ?? undefined,
            name: routerNode.routerTask.name || "Router",
            description: routerNode.routerTask.description || "",
            selectorExpression:
              routerNode.routerTask.selector?.expression || "",
            branches:
              routerNode.routerTask.branches?.length > 0
                ? routerNode.routerTask.branches.map((b) => ({
                    id: uuidv4(),
                    name: b.name,
                    condition: b.condition || "",
                    priority: b.priority,
                    targetNodeId: b.targetNodeId ?? undefined,
                  }))
                : [
                    {
                      id: uuidv4(),
                      name: "Default",
                      condition: "",
                      priority: 999,
                      targetNodeId: undefined,
                    },
                  ],
          },
        });
      } else if (nodeData.nodeById.__typename === "TaskNode") {
        const taskNode = nodeData.nodeById;

        // Load router metadata from localStorage for legacy TaskNode routers
        const routerMetadataStr = localStorage.getItem("routerMetadata");
        const routerMetadataMap = routerMetadataStr
          ? JSON.parse(routerMetadataStr)
          : {};
        const metadata = routerMetadataMap[entityId];

        if (metadata) {
          dispatch({
            type: "OPEN_EDIT",
            fragment: {
              id: entityId,
              position: taskNode.position,
              parentId: taskNode.parentId ?? undefined,
              name: taskNode.task?.name || "Router",
              description: taskNode.task?.description || "",
              selectorExpression: metadata.selector?.expression || "",
              branches:
                metadata.branches?.length > 0
                  ? metadata.branches.map(
                      (b: {
                        name: string;
                        condition?: string;
                        priority: number;
                        targetNodeId?: string;
                      }) => ({
                        id: uuidv4(),
                        name: b.name,
                        condition: b.condition || "",
                        priority: b.priority,
                        targetNodeId: b.targetNodeId,
                      }),
                    )
                  : [
                      {
                        id: uuidv4(),
                        name: "Default",
                        condition: "",
                        priority: 999,
                        targetNodeId: undefined,
                      },
                    ],
            },
          });
        } else {
          dispatch({
            type: "OPEN_EDIT",
            fragment: {
              id: entityId,
              position: taskNode.position,
              parentId: taskNode.parentId ?? undefined,
              name: taskNode.task?.name || "Router",
              description: taskNode.task?.description || "",
              selectorExpression: "",
              branches: [
                {
                  id: uuidv4(),
                  name: "Default",
                  condition: "",
                  priority: 999,
                  targetNodeId: undefined,
                },
              ],
            },
          });
        }
      }
    } else if (!isEditing) {
      dispatch({
        type: "OPEN_CREATE",
        position: locationState?.position ?? { x: 0, y: 0 },
        parentId: locationState?.parentId,
      });
    }
  }, [
    isModalOpen,
    isEditing,
    entityId,
    nodeData,
    locationState,
    refetchVariables,
  ]);

  const handleClose = () => {
    if (createLoading || updateLoading) {
      return;
    }
    dispatch({ type: "CLOSE" });
    closeModal();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const { id, position, parentId, name, description, branches } = node;

      if (!name.trim()) {
        addError({
          message: t("errors.routerNameRequired"),
          severity: "warning",
        });
        return;
      }

      // Validate that at least one branch has a condition
      const hasConditions = branches.some(
        (b) => b.condition && b.condition.trim() !== "",
      );
      if (!hasConditions) {
        addError({
          message: t("errors.branchConditionRequired"),
          severity: "warning",
        });
        return;
      }

      // Convert branches to UIBranch format for the mapper
      const uiBranches: UIBranch[] = branches.map((b) => ({
        name: b.name,
        condition: b.condition || null,
        priority: b.priority,
        targetNodeId: b.targetNodeId || "",
      }));

      // Use RouterSelectorMapper to auto-infer the selector from branch conditions.
      // The NodeInput construction for the mutation is handled inside
      // useFlowPersister via mapAppNodeToNodeInput — we build the AppNode here,
      // the persister serializes it for the wire.
      const backendData = RouterSelectorMapper.toBackend(uiBranches);
      const inferredExpression = backendData.selector.expression;

      if (isEditing && entityId) {
        // ------------------ EDIT ------------------
        // Pre-image: current AppNode in React Flow state. `getNode` returns
        // the router with its overlay metadata already applied — the exact
        // snapshot we need to restore on undo. Abort if React Flow doesn't
        // know about it (shouldn't happen in a loaded procedure).
        const current = getNode(entityId) as RouterNode | undefined;
        if (!current) {
          log.error("Router node not in React Flow state:", entityId);
          return;
        }

        const routerMetadata = {
          selector: {
            __typename: backendData.selector.__typename ?? "ExpressionSelector",
            expression: inferredExpression,
          },
          branches: branches.map((b) => ({
            name: b.name,
            condition: b.condition || undefined,
            priority: b.priority,
            targetNodeId: b.targetNodeId || "",
          })),
        };

        // Write metadata before dispatch so the subscription handler sees
        // the new overlay when the server echoes the updated node back.
        const existingMetadata = JSON.parse(
          localStorage.getItem("routerMetadata") || "{}",
        );
        existingMetadata[entityId] = routerMetadata;
        localStorage.setItem(
          "routerMetadata",
          JSON.stringify(existingMetadata),
        );

        const after: RouterNode = {
          ...current,
          data: {
            ...current.data,
            name,
            description: description || `Router: ${name}`,
            ...routerMetadata,
          },
        };
        undoManager.dispatch(updateNodeCommand(current, after));
      } else {
        // ------------------ CREATE ------------------
        // Store router metadata first — the subscription handler reads this
        // when the server echoes the created node back as a plain TaskNode.
        const routerMetadata = {
          selector: {
            __typename: backendData.selector.__typename,
            expression: inferredExpression,
          },
          branches: branches.map((b) => ({
            name: b.name,
            condition: b.condition || undefined,
            priority: b.priority,
            targetNodeId: b.targetNodeId || undefined,
          })),
        };
        const existingMetadata = JSON.parse(
          localStorage.getItem("routerMetadata") || "{}",
        );
        existingMetadata[id] = routerMetadata;
        localStorage.setItem(
          "routerMetadata",
          JSON.stringify(existingMetadata),
        );

        // Dispatch through the undo manager so a freshly created router
        // node can be undone. Building a RouterNode directly means the
        // command's optimistic insert lands as the correct node type, with
        // no post-insert type-fixup pass needed.
        const newNode: RouterNode = {
          id,
          type: "routerNode",
          position,
          parentId: parentId ?? undefined,
          extent: parentId ? "parent" : undefined,
          data: {
            name,
            description: description || `Router: ${name}`,
            startTime: 0,
            duration: 200,
            isExecuting: false,
            selector: {
              __typename:
                backendData.selector.__typename ?? "ExpressionSelector",
              expression: inferredExpression,
            },
            branches: branches.map((b) => ({
              name: b.name,
              condition: b.condition || undefined,
              priority: b.priority,
              targetNodeId: b.targetNodeId || "",
            })),
          },
        };
        undoManager.dispatch(createNodeCommand(newNode));
      }

      handleClose();
    } catch (error) {
      log.error(
        `Failed to ${isEditing ? "update" : "create"} router node:`,
        error,
      );
    }
  };

  const isDefaultBranch = (branch: RouterBranch) => {
    return branch.priority === 999;
  };

  return (
    <UnifiedModal
      show={isModalOpen}
      onHide={handleClose}
      title={isEditing ? t("router.editRouter") : t("router.configureRouter")}
      icon="bi-signpost-split"
      onSubmit={handleSubmit}
      isValid={
        !!node.name.trim() &&
        node.branches.some((b) => b.condition && b.condition.trim() !== "")
      }
      isEditing={isEditing}
      submitText={
        isEditing ? t("router.updateRouter") : t("router.createRouter")
      }
    >
      <LoadingOverlay
        show={createLoading || updateLoading || variablesLoading}
        text={
          variablesLoading
            ? t("router.loadingVariables")
            : isEditing
              ? t("router.updatingRouter")
              : t("router.creatingRouter")
        }
      />

      <Form.Group className="mb-3" controlId="router-name">
        <Form.Label>
          <i className="bi bi-signpost-split me-2"></i>
          {t("router.routerName")}
        </Form.Label>
        <Form.Control
          type="text"
          placeholder={t("router.enterRouterName")}
          value={node.name}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "name",
              value: e.target.value,
            })
          }
          autoFocus
        />
      </Form.Group>

      <Form.Group className="mb-3" controlId="router-description">
        <Form.Label>
          <i className="bi bi-card-text me-2"></i>
          {t("modals.description")}
        </Form.Label>
        <Form.Control
          as="textarea"
          rows={2}
          placeholder={t("router.enterRouterDescription")}
          value={node.description}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "description",
              value: e.target.value,
            })
          }
        />
      </Form.Group>

      <div className="mb-3">
        <div className="d-flex justify-content-between align-items-center mb-2">
          <h6 className="mb-0">
            <i className="bi bi-diagram-3 me-2"></i>
            {t("router.branches")}
          </h6>
          <Button
            variant="outline-primary"
            size="sm"
            onClick={() => dispatch({ type: "ADD_BRANCH" })}
          >
            <i className="bi bi-plus-circle me-1"></i>
            {t("router.addBranch")}
          </Button>
        </div>

        {node.branches.map((branch, index) => (
          <Card key={branch.id} className="mb-2 branch-item">
            <Card.Body className="py-2">
              <Row className="align-items-center">
                <Col xs={12} className="mb-2">
                  <div className="d-flex justify-content-between align-items-center">
                    <strong>
                      {isDefaultBranch(branch) ? (
                        <span className="text-muted">
                          <i className="bi bi-arrow-return-right me-1"></i>
                          {t("router.defaultBranch")}
                        </span>
                      ) : (
                        <span>
                          <i className="bi bi-arrow-right-circle me-1"></i>
                          {t("router.branchIndex", { index: index + 1 })}
                          <span className="text-muted ms-2 small">
                            ({t("router.priority")} {branch.priority})
                          </span>
                        </span>
                      )}
                    </strong>
                    {!isDefaultBranch(branch) && (
                      <Button
                        variant="outline-danger"
                        size="sm"
                        onClick={() => handleRemoveBranch(branch.id)}
                        aria-label={t("router.removeBranch", {
                          index: index + 1,
                        })}
                      >
                        <i className="bi bi-trash"></i>
                      </Button>
                    )}
                  </div>
                </Col>

                {!isDefaultBranch(branch) && (
                  <>
                    <Col xs={12}>
                      <Form.Group className="mb-2">
                        <Form.Label className="small mb-1">
                          {t("router.branchName")}
                        </Form.Label>
                        <Form.Control
                          type="text"
                          size="sm"
                          placeholder={t("router.enterBranchName")}
                          value={branch.name}
                          onChange={(e) =>
                            dispatch({
                              type: "UPDATE_BRANCH",
                              branchId: branch.id,
                              key: "name",
                              value: e.target.value,
                            })
                          }
                          aria-label={t("router.branchNameAriaLabel", {
                            index: index + 1,
                          })}
                        />
                      </Form.Group>
                    </Col>
                    <Col xs={12}>
                      <div className="mb-2">
                        <Form.Label className="small mb-1">
                          {t("router.condition")}
                        </Form.Label>
                        <ConditionBuilder
                          mode="simple"
                          variables={procedureVariables}
                          value={getConditionValueForBranch(branch)}
                          onChange={(conditionValue: ConditionValue) => {
                            if ("fullCondition" in conditionValue) {
                              // Always update local state to preserve partial progress
                              setBranchConditionStates((prev) => ({
                                ...prev,
                                [branch.id]: conditionValue,
                              }));

                              // Only persist to reducer when we have a complete condition (non-empty fullCondition)
                              if (
                                conditionValue.fullCondition &&
                                conditionValue.fullCondition.trim() !== ""
                              ) {
                                dispatch({
                                  type: "UPDATE_BRANCH",
                                  branchId: branch.id,
                                  key: "condition",
                                  value: conditionValue.fullCondition,
                                });
                              }
                            }
                          }}
                        />
                      </div>
                    </Col>
                  </>
                )}
              </Row>
            </Card.Body>
          </Card>
        ))}
      </div>
    </UnifiedModal>
  );
};

export default RouterConfigModal;
