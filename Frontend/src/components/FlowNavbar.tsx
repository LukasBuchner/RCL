import React, { useCallback, useEffect, useRef, useState } from "react";
// Bootstrap CSS is imported in App.tsx
import useBootstrapTooltips from "../hooks/useBootstrapTooltips.ts"; // Ensure this path is correct
import { ButtonGroup, Dropdown } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { useClipboardOperations } from "../hooks/useClipboardOperations"; // Ensure this path is correct
import { ApolloError, useMutation } from "@apollo/client";
import {
  AgentSerializationViolation,
  StartLoadedProcedureDocument,
  StartLoadedProcedureMutation,
} from "../__generated__/graphql.ts";
import { MotionButton, MotionContainer } from "./motion";
import { useLocation, useNavigate } from "react-router-dom";
import { ProcedureSelector } from "./management/ProcedureSelector";
import { useExecutionStore } from "../stores/executionStore";
import { useError } from "../contexts/ErrorContext";
import { useValidationResult } from "../hooks/useValidationResult";
import { useExecutionErrorHandlers } from "../hooks/useExecutionErrorHandlers";
import { useValidationStore } from "../stores/validationStore";
import { AgentSerializationModal } from "./modals/AgentSerializationModal";
import { createLogger } from "../utils/logger";

const log = createLogger("FlowNavbar");

// Path to your logo in the public folder
const logoUrl = "/AltmMinimalIconHRCPTransparent.svg";

const FlowNavbar: React.FC = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const isFlowView = location.pathname === "/";
  const toolbarRef = useRef<HTMLDivElement>(null);
  useBootstrapTooltips(toolbarRef); // This hook initializes tooltips within the toolbarRef element

  const { handlePasteNode } = useClipboardOperations();
  const [startProcedure, { loading: isStarting }] =
    useMutation<StartLoadedProcedureMutation>(StartLoadedProcedureDocument);
  const isProcedureRunning = useExecutionStore(
    (state) => state.isProcedureRunning,
  );
  const isPlayDisabled = isStarting || isProcedureRunning;

  const { handleError } = useError();
  const { violations: currentViolations, warningNodeIds } =
    useValidationResult();

  // Keep the validation store in sync so BaseNode can read warning state
  // without needing a separate subscription or prop drilling through ReactFlow.
  // useEffect is appropriate here: this is a store write triggered by external
  // data (subscription), not something that can be done during render.
  const setWarningNodeIds = useValidationStore(
    (state) => state.setWarningNodeIds,
  );
  useEffect(() => {
    setWarningNodeIds(warningNodeIds);
  }, [warningNodeIds, setWarningNodeIds]);
  const [serializationViolations, setSerializationViolations] = useState<
    AgentSerializationViolation[]
  >([]);
  const [showSerializationModal, setShowSerializationModal] = useState(false);

  const errorHandlers = useExecutionErrorHandlers({
    setViolations: setSerializationViolations,
    setShowModal: setShowSerializationModal,
    handleError,
  });

  const onAddTaskClick = useCallback(() => {
    navigate("/task/create", { state: { position: { x: 0, y: 0 } } });
  }, [navigate]);

  const onAddSkillClick = useCallback(() => {
    log.trace("FlowNavbar: navigating to /skill/create");
    navigate("/skill/create", { state: { position: { x: 0, y: 0 } } });
  }, [navigate]);

  const onAddRouterClick = useCallback(() => {
    navigate("/router/create", { state: { position: { x: 0, y: 0 } } });
  }, [navigate]);

  const onPasteNodeClick = useCallback(async () => {
    await handlePasteNode();
  }, [handlePasteNode]);

  const onPlayClick = useCallback(async () => {
    // Layer 1: fast-path check against the subscription state (~1 s stale).
    // If violations are already known, show the modal immediately without
    // waiting for the mutation to bounce back with the same information.
    if (currentViolations.length > 0) {
      setSerializationViolations(currentViolations);
      setShowSerializationModal(true);
      return;
    }

    try {
      const result = await startProcedure();
      if (!result.data?.startLoadedProcedure?.boolean) {
        handleError(new Error("Failed to start procedure"), {
          source: "graphql",
        });
      }
    } catch (error) {
      if (!(error instanceof ApolloError)) {
        handleError(error instanceof Error ? error : new Error(String(error)), {
          source: "graphql",
        });
        return;
      }

      // Layer 3: dispatch structured GraphQL errors to the handler registry.
      const handled = error.graphQLErrors.some((gqlError) => {
        const code = gqlError.extensions?.code as string | undefined;
        const handler = code ? errorHandlers[code] : undefined;
        if (handler) {
          handler(gqlError);
          return true;
        }
        return false;
      });

      if (!handled) {
        handleError(new Error(error.message), { source: "graphql" });
      }
    }
  }, [
    currentViolations,
    startProcedure,
    handleError,
    errorHandlers,
    setSerializationViolations,
    setShowSerializationModal,
  ]);

  return (
    <>
      <nav ref={toolbarRef} className="flow-navbar">
        <div className="navbar-content">
          <div className="navbar-brand-section">
            <div className="brand-icon me-3">
              <img
                src={logoUrl}
                width="35"
                height="35"
                className="d-inline-block align-middle"
                alt="Altm Logo"
              />
            </div>
            <div className="brand-text">
              <h5 className="mb-0 fw-bold text-primary">{t("flow.title")}</h5>
              <small className="text-muted">{t("flow.subtitle")}</small>
            </div>
          </div>

          {isFlowView && (
            <div className="navbar-procedure-section">
              <div className="action-group">
                <span className="action-group-label">
                  {t("flow.procedure")}
                </span>
                <ProcedureSelector />
              </div>
            </div>
          )}

          {isFlowView && (
            <MotionContainer
              animation="slideDown"
              className="navbar-center-actions"
            >
              <div className="action-group">
                <span className="action-group-label">{t("flow.create")}</span>
                <ButtonGroup role="group" aria-label="Creation actions">
                  <MotionButton
                    variant="primary"
                    size="sm"
                    className="action-btn"
                    onClick={onAddTaskClick}
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={t("flow.addTask")}
                  >
                    <i className="bi bi-plus-lg" />
                    <span className="btn-text">{t("flow.task")}</span>
                  </MotionButton>
                  <MotionButton
                    variant="info"
                    size="sm"
                    className="action-btn"
                    onClick={onAddSkillClick}
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={t("flow.addSkill")}
                  >
                    <i className="bi bi-lightning-charge" />
                    <span className="btn-text">{t("flow.skill")}</span>
                  </MotionButton>
                  <MotionButton
                    variant="warning"
                    size="sm"
                    className="action-btn"
                    onClick={onAddRouterClick}
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={t("flow.addRouter")}
                  >
                    <i className="bi bi-signpost-split" />
                    <span className="btn-text">{t("flow.router")}</span>
                  </MotionButton>
                </ButtonGroup>
              </div>

              <div className="action-group">
                <span className="action-group-label">{t("flow.edit")}</span>
                <ButtonGroup
                  role="group"
                  aria-label={t("common.clipboardActions")}
                >
                  <MotionButton
                    variant="secondary"
                    size="sm"
                    className="action-btn"
                    onClick={onPasteNodeClick}
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={t("tooltips.pasteNode")}
                  >
                    <i className="bi bi-clipboard-plus-fill" />
                    <span className="btn-text">{t("flow.paste")}</span>
                  </MotionButton>
                </ButtonGroup>
              </div>

              <div className="action-group">
                <span className="action-group-label">{t("flow.execute")}</span>
                <ButtonGroup
                  role="group"
                  aria-label={t("common.executionActions")}
                >
                  <MotionButton
                    variant={isProcedureRunning ? "warning" : "success"}
                    size="sm"
                    className="action-btn"
                    onClick={onPlayClick}
                    disabled={isPlayDisabled}
                    loading={isStarting}
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={
                      isProcedureRunning
                        ? t("flow.procedureRunning")
                        : t("flow.startProcedure")
                    }
                  >
                    <i
                      className={
                        isStarting
                          ? "bi bi-hourglass-split"
                          : isProcedureRunning
                            ? "bi bi-activity"
                            : "bi bi-play-fill"
                      }
                    />
                    <span className="btn-text">
                      {isProcedureRunning ? t("flow.running") : t("flow.play")}
                    </span>
                  </MotionButton>
                </ButtonGroup>
              </div>
            </MotionContainer>
          )}

          <div className="navbar-view-switcher">
            <div className="action-group">
              <span className="action-group-label">
                {t("navigation.title")}
              </span>
              <ButtonGroup role="group" aria-label={t("common.viewNavigation")}>
                <MotionButton
                  variant={
                    location.pathname === "/" ? "primary" : "outline-primary"
                  }
                  size="sm"
                  className="view-btn"
                  onClick={() => navigate("/")}
                  data-bs-toggle="tooltip"
                  data-bs-placement="bottom"
                  title={t("tooltips.flowView")}
                >
                  <i className="bi bi-diagram-3" />
                  <span className="btn-text">{t("navigation.flow")}</span>
                </MotionButton>
                <MotionButton
                  variant={
                    location.pathname.startsWith("/management")
                      ? "primary"
                      : "outline-primary"
                  }
                  size="sm"
                  className="view-btn"
                  onClick={() => navigate("/management")}
                  data-bs-toggle="tooltip"
                  data-bs-placement="bottom"
                  title={t("tooltips.managementView")}
                >
                  <i className="bi bi-gear-fill" />
                  <span className="btn-text">{t("navigation.management")}</span>
                </MotionButton>
                <Dropdown as={ButtonGroup}>
                  <Dropdown.Toggle
                    as={MotionButton}
                    variant="outline-primary"
                    size="sm"
                    className="view-btn"
                    data-bs-toggle="tooltip"
                    data-bs-placement="bottom"
                    title={t("tooltips.moreOptions")}
                  >
                    <i className="bi bi-three-dots-vertical" />
                  </Dropdown.Toggle>
                  <Dropdown.Menu>
                    <Dropdown.Item onClick={() => navigate("/settings")}>
                      <i className="bi bi-gear me-2" />
                      {t("navigation.settings")}
                    </Dropdown.Item>
                    <Dropdown.Item onClick={() => navigate("/help")}>
                      <i className="bi bi-question-circle me-2" />
                      {t("navigation.help")}
                    </Dropdown.Item>
                  </Dropdown.Menu>
                </Dropdown>
              </ButtonGroup>
            </div>
          </div>
        </div>
      </nav>

      {isFlowView && warningNodeIds.size > 0 && (
        <div
          className="d-flex align-items-center gap-2 px-3 py-1"
          role="alert"
          style={{
            backgroundColor: "var(--bs-warning-bg-subtle)",
            borderBottom: "1px solid var(--bs-warning-border-subtle)",
            fontSize: "0.85rem",
            color: "var(--bs-warning-text-emphasis)",
          }}
        >
          <i className="bi bi-exclamation-triangle-fill" aria-hidden="true" />
          <span>
            {warningNodeIds.size} skill
            {warningNodeIds.size !== 1 ? "s" : ""} could run at the same time
          </span>
          <button
            className="btn btn-link btn-sm p-0 ms-1"
            style={{ color: "inherit", textDecoration: "underline" }}
            onClick={() => {
              setSerializationViolations(currentViolations);
              setShowSerializationModal(true);
            }}
          >
            Details
          </button>
        </div>
      )}

      <AgentSerializationModal
        show={showSerializationModal}
        violations={serializationViolations}
        onClose={() => setShowSerializationModal(false)}
      />
    </>
  );
};

export default FlowNavbar;
