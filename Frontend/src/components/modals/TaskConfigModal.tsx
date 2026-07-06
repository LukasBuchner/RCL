import React, { useEffect, useReducer } from "react";
import { Form } from "react-bootstrap";
import { UnifiedModal } from "../common/UnifiedModal";
import { MODAL_CONFIGS, useRouterModal } from "../../hooks/useRouterModal";
import { v4 as uuidv4 } from "uuid";
import { produce } from "immer";
import { useQuery } from "@apollo/client";
import { useError } from "../../hooks";
import { LoadingOverlay } from "../loading";
import { useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  GetNodeByIdDocument,
  GetNodeByIdQuery,
  TaskNodeFieldsFragment,
} from "../../__generated__/graphql";
import { createNodeCommand, updateNodeCommand, useFlowUndo } from "../../undo";
import { TaskNode } from "../../types/nodeTypes";
import { createLogger } from "../../utils/logger";

const log = createLogger("TaskConfigModal");

// State passed through location.state from navigation
interface LocationState {
  position?: { x: number; y: number };
  parentId?: string;
}

/**********************************************************************
 * 1. Helper – blank fragment for "create" mode
 *********************************************************************/
const blankFragment = (
  position: { x: number; y: number } = { x: 0, y: 0 },
  parentId?: string,
): TaskNodeFieldsFragment => ({
  __typename: "TaskNode",
  id: uuidv4(),
  parentId: parentId ?? null,
  extent: parentId ? "parent" : null,
  width: null,
  height: 50,
  selectable: true,
  selected: false,
  draggable: true,
  dragging: false,
  hidden: false,
  position: { __typename: "NodePosition", ...position },
  task: {
    __typename: "Task",
    name: "",
    description: "",
    startTime: 0,
    duration: 200,
    isExecuting: false,
  },
});

/**********************************************************************
 * 2. Reducer actions
 *********************************************************************/
type Action =
  | {
      type: "OPEN_CREATE";
      position: { x: number; y: number };
      parentId?: string;
    }
  | { type: "OPEN_EDIT"; fragment: TaskNodeFieldsFragment }
  | { type: "CLOSE" }
  | {
      type: "SET_FIELD";
      key: "name" | "description" | "duration";
      value: string | number;
    };

/**********************************************************************
 * 3. Reducer – Immer
 *********************************************************************/
function reducer(
  state: TaskNodeFieldsFragment,
  action: Action,
): TaskNodeFieldsFragment {
  return produce(state, (draft) => {
    switch (action.type) {
      case "OPEN_CREATE":
        return blankFragment(action.position, action.parentId);
      case "OPEN_EDIT":
        return action.fragment;
      case "CLOSE":
        return blankFragment();
      case "SET_FIELD": {
        if (action.key === "name") draft.task.name = action.value as string;
        if (action.key === "description")
          draft.task.description = action.value as string;
        if (action.key === "duration")
          draft.task.duration = action.value as number;
        return;
      }
      default:
        return;
    }
  });
}

/**********************************************************************
 * 4. Component
 *********************************************************************/
const TaskConfigModal: React.FC = () => {
  const { t } = useTranslation();

  // Get router state and location state
  const location = useLocation();
  const locationState = location.state as LocationState | null;
  const { isModalOpen, isEditing, entityId, closeModal } = useRouterModal(
    MODAL_CONFIGS.TASK_CONFIG,
  );

  // Both create and update flow through the undo manager so they are
  // undoable end-to-end. Loading flags stay `false` because dispatching a
  // command is synchronous — the persister handles the network round-trip
  // on its own queue.
  const { manager: undoManager } = useFlowUndo();
  const createLoading = false;
  const updateLoading = false;
  const [node, dispatch] = useReducer(reducer, blankFragment());
  const { addError } = useError();

  // GraphQL query to get node by ID
  const { data: nodeData } = useQuery<GetNodeByIdQuery>(GetNodeByIdDocument, {
    variables: { id: entityId || "" },
    skip: !entityId,
    fetchPolicy: "no-cache",
  });

  // Modal lifecycle sync
  useEffect(() => {
    if (!isModalOpen) return;

    if (
      isEditing &&
      entityId &&
      nodeData?.nodeById &&
      nodeData.nodeById.__typename === "TaskNode"
    ) {
      dispatch({ type: "OPEN_EDIT", fragment: nodeData.nodeById });
    } else if (!isEditing) {
      dispatch({
        type: "OPEN_CREATE",
        position: locationState?.position ?? { x: 0, y: 0 },
        parentId: locationState?.parentId,
      });
    }
  }, [isModalOpen, isEditing, entityId, nodeData, locationState]);

  // Reset form when modal is closed
  const handleClose = () => {
    // Don't close modal if a mutation is in progress
    if (createLoading || updateLoading) {
      return;
    }
    dispatch({ type: "CLOSE" });
    closeModal();
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const { id, position, parentId, task } = node;

      // Validate required fields
      if (!task.name.trim()) {
        addError({
          message: t("errors.taskNameRequired"),
          severity: "warning",
        });
        return;
      }

      if (isEditing && entityId) {
        // ------------------ EDIT ------------------
        if (
          !nodeData?.nodeById ||
          nodeData.nodeById.__typename !== "TaskNode"
        ) {
          log.error("Node not found or not a task node");
          return;
        }

        // Build a `before` AppNode from the server-side fragment (pre-edit
        // snapshot) and an `after` AppNode with the user's form edits applied.
        // Dispatch through the undo manager so edits are reversible.
        const serverNode = nodeData.nodeById;
        const before: TaskNode = {
          id: serverNode.id,
          type: "taskNode",
          position: {
            x: serverNode.position.x,
            y: serverNode.position.y,
          },
          parentId: serverNode.parentId ?? undefined,
          extent: serverNode.extent === "parent" ? "parent" : undefined,
          data: {
            name: serverNode.task.name,
            description: serverNode.task.description ?? "",
            startTime: serverNode.task.startTime,
            duration: serverNode.task.duration,
            isExecuting: serverNode.task.isExecuting ?? false,
          },
        };
        const after: TaskNode = {
          ...before,
          data: {
            ...before.data,
            name: task.name,
            description: task.description ?? "",
            duration: task.duration,
          },
        };
        undoManager.dispatch(updateNodeCommand(before, after));
      } else {
        // ------------------ CREATE ------------------
        // Dispatch through the undo manager so a freshly created task node
        // can be undone. The manager's command handles both the optimistic
        // local insert and the backend persist via the shared persister.
        const newNode: TaskNode = {
          id,
          type: "taskNode",
          position,
          parentId: parentId ?? undefined,
          extent: parentId ? "parent" : undefined,
          data: {
            name: task.name,
            description: task.description ?? "",
            startTime: 0,
            duration: task.duration,
            isExecuting: false,
          },
        };
        undoManager.dispatch(createNodeCommand(newNode));
      }

      // Close the modal
      handleClose();
    } catch (error) {
      log.error(
        `Failed to ${isEditing ? "update" : "create"} task node:`,
        error,
      );
    }
  };

  return (
    <UnifiedModal
      show={isModalOpen}
      onHide={handleClose}
      title={isEditing ? t("modals.editTask") : t("modals.configureTask")}
      icon="bi-list-task"
      onSubmit={handleSubmit}
      isValid={!!node.task.name.trim()}
      isEditing={isEditing}
      submitText={isEditing ? t("modals.updateTask") : t("modals.createTask")}
    >
      <LoadingOverlay
        show={createLoading || updateLoading}
        text={isEditing ? t("modals.updatingTask") : t("modals.creatingTask")}
      />
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-tag me-2"></i>
          {t("modals.taskName")}
        </Form.Label>
        <Form.Control
          type="text"
          placeholder={t("modals.enterTaskName")}
          value={node.task.name}
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

      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-card-text me-2"></i>
          {t("modals.description")}
        </Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          placeholder={t("modals.enterTaskDescription")}
          value={node.task.description ?? ""}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "description",
              value: e.target.value,
            })
          }
        />
      </Form.Group>

      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-clock me-2"></i>
          {t("modals.duration")}
        </Form.Label>
        <Form.Control
          type="number"
          min={0}
          value={node.task.duration}
          placeholder={t("modals.enterDuration")}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "duration",
              value: parseInt(e.target.value) || 200,
            })
          }
        />
        <Form.Text className="text-muted">
          {t("modals.durationDescription")}
        </Form.Text>
      </Form.Group>
    </UnifiedModal>
  );
};

export default TaskConfigModal;
