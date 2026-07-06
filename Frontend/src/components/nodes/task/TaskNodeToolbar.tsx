import React, { useCallback, useRef } from "react"; // Removed useEffect import
import "bootstrap/dist/css/bootstrap.min.css";
import useBootstrapTooltips from "../../../hooks/useBootstrapTooltips.ts";
import { useMutation } from "@apollo/client";
import {
  DeleteNodeDocument,
  DeleteNodeInput,
  DeleteNodeMutation,
} from "../../../__generated__/graphql.ts";
import { useReactFlow } from "@xyflow/react";
import { useClipboardOperations } from "../../../hooks/useClipboardOperations";
import { useNavigate } from "react-router-dom";
import { MotionButton } from "../../motion";
import { TaskBasicData } from "../../../types/nodeTypes.ts";
import { useTranslation } from "react-i18next";
import { createLogger } from "../../../utils/logger";

const log = createLogger("TaskToolbar");

interface TaskNodeToolbarProps {
  nodeId: string;
  parentId: string | undefined;
}

const TaskNodeToolbar: React.FC<TaskNodeToolbarProps> = ({ nodeId }) => {
  const { t } = useTranslation();
  const toolbarRef = useRef<HTMLDivElement>(null);
  useBootstrapTooltips(toolbarRef);
  const [deleteNode] = useMutation<DeleteNodeMutation>(DeleteNodeDocument);
  const navigate = useNavigate();
  const { getNode } = useReactFlow();
  const { handleCopyNode, handleCutNode, handlePasteNode } =
    useClipboardOperations(nodeId);

  const onCopyNodeClick = useCallback(() => {
    handleCopyNode();
  }, [handleCopyNode]);

  const onCutNodeClick = useCallback(() => {
    handleCutNode();
  }, [handleCutNode]);

  const onPasteNodeClick = useCallback(async () => {
    await handlePasteNode(nodeId);
  }, [handlePasteNode, nodeId]);

  const onDeleteClick = useCallback(async () => {
    log.trace("Node remove event detected:", nodeId);

    try {
      await deleteNode({
        variables: {
          input: { id: nodeId } as DeleteNodeInput,
        },
      });
      log.trace("onNodesChange: Node deleted successfully:", nodeId);
    } catch (error) {
      log.error("onNodesChange: Failed to delete the node:", error);
    }
  }, [deleteNode, nodeId]);

  const onAddSubTaskNodeClick = useCallback(async () => {
    const parentNode = getNode(nodeId);

    if (parentNode) {
      navigate("/task/create", {
        state: {
          parentId: nodeId,
          position: {
            x: parentNode.position.x + 50,
            y: parentNode.position.y + 100,
          },
        },
      });
    }
  }, [nodeId, getNode, navigate]);

  const onAddSkillClick = useCallback(() => {
    const parentNode = getNode(nodeId);

    if (parentNode) {
      navigate("/skill/create", {
        state: {
          parentId: nodeId,
          position: {
            x: parentNode.position.x + 50,
            y: parentNode.position.y + 100,
          },
        },
      });
    }
  }, [nodeId, getNode, navigate]);

  const onAddRouterClick = useCallback(() => {
    const parentNode = getNode(nodeId);

    if (parentNode) {
      navigate("/router/create", {
        state: {
          parentId: nodeId,
          position: {
            x: parentNode.position.x + 50,
            y: parentNode.position.y + 100,
          },
        },
      });
    }
  }, [nodeId, getNode, navigate]);

  const onEditingClick = useCallback(() => {
    navigate(`/task/${nodeId}/edit`);
  }, [nodeId, navigate]);

  const node = getNode(nodeId);
  const isRouterChild = Boolean(
    (node?.data as TaskBasicData | undefined)?.isRouterChild,
  );

  return (
    // Add the ref to the main toolbar div
    <div
      ref={toolbarRef} // Assign the ref here
      className="btn-toolbar"
      role="toolbar"
      aria-label={t("common.actionsForNode")}
    >
      {/* --- Creation Group --- */}
      <div
        className="btn-group me-2"
        role="group"
        aria-label={t("common.creationActions")}
      >
        <MotionButton
          variant="primary"
          size="sm"
          onClick={onAddSubTaskNodeClick}
          aria-label={t("tooltips.createSubtask")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.createSubtask")}
        >
          <i className="bi bi-plus-lg"></i>
        </MotionButton>
        <MotionButton
          variant="info"
          size="sm"
          onClick={onAddSkillClick}
          aria-label={t("tooltips.addSkillExecution")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.addSkillExecution")}
        >
          <i className="bi bi-lightning-charge"></i>
        </MotionButton>
        <MotionButton
          variant="warning"
          size="sm"
          onClick={onAddRouterClick}
          aria-label={t("tooltips.addRouter")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.addRouter")}
        >
          <i className="bi bi-signpost-split"></i>
        </MotionButton>
      </div>

      {/* --- Modification Group --- */}
      {!isRouterChild && (
        <div
          className="btn-group me-2"
          role="group"
          aria-label={t("common.editDeleteActions")}
        >
          <MotionButton
            variant="success"
            size="sm"
            onClick={onEditingClick}
            aria-label={t("tooltips.editNode")}
            data-bs-toggle="tooltip"
            data-bs-placement="top"
            data-bs-title={t("tooltips.editNode")}
          >
            <i className="bi bi-pencil-square"></i>
          </MotionButton>
          <MotionButton
            variant="danger"
            size="sm"
            onClick={onDeleteClick}
            aria-label={t("tooltips.deleteNode")}
            data-bs-toggle="tooltip"
            data-bs-placement="top"
            data-bs-title={t("tooltips.deleteNode")}
          >
            <i className="bi bi-trash3"></i>
          </MotionButton>
        </div>
      )}

      {/* --- Clipboard Group --- */}
      <div
        className="btn-group"
        role="group"
        aria-label={t("common.clipboardActions")}
      >
        <MotionButton
          variant="secondary"
          size="sm"
          onClick={onCopyNodeClick} // Assuming copyNode might not need nodeId directly if context is used
          aria-label={t("tooltips.copyNode")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.copyNode")}
        >
          <i className="bi bi-clipboard"></i>
        </MotionButton>
        {!isRouterChild && (
          <MotionButton
            variant="warning"
            size="sm"
            onClick={onCutNodeClick}
            aria-label={t("tooltips.cutNode")}
            data-bs-toggle="tooltip"
            data-bs-placement="top"
            data-bs-title={t("tooltips.cutNode")}
          >
            <i className="bi bi-scissors"></i>
          </MotionButton>
        )}
        <MotionButton
          variant="secondary"
          size="sm"
          onClick={onPasteNodeClick}
          aria-label={t("tooltips.pasteNode")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.pasteNode")}
        >
          <i className="bi bi-clipboard-plus"></i>
        </MotionButton>
      </div>
    </div>
  );
};

export default TaskNodeToolbar;
