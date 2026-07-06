import React, { useCallback, useRef } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import useBootstrapTooltips from "../../../hooks/useBootstrapTooltips";
import { useMutation } from "@apollo/client";
import {
  DeleteNodeDocument,
  DeleteNodeInput,
  DeleteNodeMutation,
} from "../../../__generated__/graphql";
import { useClipboardOperations } from "../../../hooks/useClipboardOperations";
import { useNavigate } from "react-router-dom";
import { MotionButton } from "../../motion";
import { useTranslation } from "react-i18next";
import { createLogger } from "../../../utils/logger";

const log = createLogger("RouterToolbar");

interface RouterNodeToolbarProps {
  nodeId: string;
  parentId?: string | undefined;
}

const RouterNodeToolbar: React.FC<RouterNodeToolbarProps> = ({ nodeId }) => {
  const { t } = useTranslation();
  const toolbarRef = useRef<HTMLDivElement>(null);
  useBootstrapTooltips(toolbarRef);
  const [deleteNode] = useMutation<DeleteNodeMutation>(DeleteNodeDocument);
  const navigate = useNavigate();
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

  const onEditingClick = useCallback(() => {
    navigate(`/router/${nodeId}/edit`);
  }, [nodeId, navigate]);

  return (
    <div
      ref={toolbarRef}
      className="btn-toolbar"
      role="toolbar"
      aria-label={t("common.actionsForNode")}
    >
      <div
        className="btn-group me-2"
        role="group"
        aria-label={t("common.editDeleteActions")}
      >
        <MotionButton
          variant="success"
          size="sm"
          onClick={onEditingClick}
          aria-label="Edit"
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
          aria-label="Delete"
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.deleteNode")}
        >
          <i className="bi bi-trash3"></i>
        </MotionButton>
      </div>

      <div
        className="btn-group"
        role="group"
        aria-label={t("common.clipboardActions")}
      >
        <MotionButton
          variant="secondary"
          size="sm"
          onClick={onCopyNodeClick}
          aria-label="Copy"
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.copyNode")}
        >
          <i className="bi bi-clipboard"></i>
        </MotionButton>
        <MotionButton
          variant="warning"
          size="sm"
          onClick={onCutNodeClick}
          aria-label="Cut"
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.cutNode")}
        >
          <i className="bi bi-scissors"></i>
        </MotionButton>
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

export default RouterNodeToolbar;
