import React, { useCallback, useRef } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import useBootstrapTooltips from "../../../hooks/useBootstrapTooltips.ts";
import { useMutation } from "@apollo/client";
import {
  DeleteNodeDocument,
  DeleteNodeInput,
  DeleteNodeMutation,
} from "../../../__generated__/graphql.ts";
import { useClipboardOperations } from "../../../hooks/useClipboardOperations";
import { useNavigate } from "react-router-dom";
import { MotionButton } from "../../motion";
import { useTranslation } from "react-i18next";
import { createLogger } from "../../../utils/logger";

const log = createLogger("SkillToolbar");

interface SkillExecutionNodeToolbarProps {
  nodeId: string;
}

const SkillExecutionNodeToolbar: React.FC<SkillExecutionNodeToolbarProps> = ({
  nodeId,
}) => {
  const { t } = useTranslation();
  const toolbarRef = useRef<HTMLDivElement>(null);
  useBootstrapTooltips(toolbarRef);

  const [deleteNode] = useMutation<DeleteNodeMutation>(DeleteNodeDocument);
  const { handleCopyNode, handleCutNode } = useClipboardOperations(nodeId);

  const onCopyNodeClick = useCallback(() => {
    handleCopyNode();
  }, [handleCopyNode]);

  const onCutNodeClick = useCallback(() => {
    handleCutNode();
  }, [handleCutNode]);

  const onDeleteClick = useCallback(async () => {
    log.trace("Node remove event detected:", nodeId);

    try {
      await deleteNode({
        variables: {
          input: { id: nodeId } as DeleteNodeInput,
        },
      });
      log.trace("onDeleteClick: Node deleted successfully:", nodeId);
    } catch (error) {
      log.error("onDeleteClick: Failed to delete the node:", error);
    }
  }, [deleteNode, nodeId]);

  const navigate = useNavigate();

  const onEditingClick = useCallback(() => {
    navigate(`/skill/${nodeId}/edit`);
  }, [nodeId, navigate]);

  return (
    <div
      ref={toolbarRef}
      className="btn-toolbar"
      role="toolbar"
      aria-label={t("common.actionsForNode")}
    >
      {/* --- Modification Group --- */}
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

      {/* --- Clipboard Group --- */}
      <div
        className="btn-group"
        role="group"
        aria-label={t("common.clipboardActions")}
      >
        <MotionButton
          variant="secondary"
          size="sm"
          onClick={onCopyNodeClick}
          aria-label={t("tooltips.copyNode")}
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
          aria-label={t("tooltips.cutNode")}
          data-bs-toggle="tooltip"
          data-bs-placement="top"
          data-bs-title={t("tooltips.cutNode")}
        >
          <i className="bi bi-scissors"></i>
        </MotionButton>
      </div>
    </div>
  );
};

export default SkillExecutionNodeToolbar;
