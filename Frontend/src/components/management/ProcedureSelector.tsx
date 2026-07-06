import React, { useCallback, useState } from "react";
import { ButtonGroup, Form, Modal } from "react-bootstrap";
import { useMutation, useQuery } from "@apollo/client";
import { useTranslation } from "react-i18next";
import {
  CreateProcedureDocument,
  CreateProcedureMutation,
  CreateProcedureMutationVariables,
  GetAllProceduresDocument,
  GetAllProceduresQuery,
} from "../../__generated__/graphql";
import { useProcedure } from "../../contexts/ProcedureContext";
import { useError } from "../../hooks";
import { MotionButton, MotionContainer } from "../motion";
import { LoadingOverlay } from "../loading";
import { ErrorState } from "../error";
import {
  getApolloErrorMessage,
  isBackendConnectionError,
} from "../../utils/apolloErrorUtils";

export const ProcedureSelector: React.FC = () => {
  const { t } = useTranslation();
  const {
    loadedProcedure,
    isLoading: contextLoading,
    loadProcedure,
    unloadProcedure,
  } = useProcedure();
  const { addError } = useError();

  const [selectedProcedureId, setSelectedProcedureId] = useState<string>("");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newProcedureName, setNewProcedureName] = useState("");
  const [newProcedureDescription, setNewProcedureDescription] = useState("");

  const {
    data,
    error: queryError,
    refetch,
  } = useQuery<GetAllProceduresQuery>(GetAllProceduresDocument, {
    fetchPolicy: "cache-and-network",
  });

  const [createProcedure, { loading: createLoading }] = useMutation<
    CreateProcedureMutation,
    CreateProcedureMutationVariables
  >(CreateProcedureDocument);

  const procedures = data?.procedures || [];
  const isLoadingAction = contextLoading || createLoading;

  const handleLoadProcedure = useCallback(async () => {
    if (!selectedProcedureId) {
      addError({
        message: t("procedures.selectToLoad"),
        severity: "warning",
      });
      return;
    }

    try {
      await loadProcedure(selectedProcedureId);
    } catch (err) {
      addError({
        message: t("procedures.failedToLoad"),
        severity: "error",
        details: { error: err },
      });
    }
  }, [selectedProcedureId, loadProcedure, addError, t]);

  const handleUnloadProcedure = useCallback(async () => {
    try {
      await unloadProcedure();
      setSelectedProcedureId("");
    } catch (err) {
      addError({
        message: t("procedures.failedToUnload"),
        severity: "error",
        details: { error: err },
      });
    }
  }, [unloadProcedure, addError, t]);

  const handleCreateProcedure = useCallback(async () => {
    if (!newProcedureName.trim()) {
      addError({
        message: t("procedures.nameRequired"),
        severity: "warning",
      });
      return;
    }

    try {
      const result = await createProcedure({
        variables: {
          input: {
            name: newProcedureName,
            description: newProcedureDescription || undefined,
          },
        },
      });

      if (result.data?.createProcedure.procedure) {
        setShowCreateModal(false);
        setNewProcedureName("");
        setNewProcedureDescription("");
        await refetch();
      }
    } catch (err) {
      addError({
        message: t("procedures.failedToCreate"),
        severity: "error",
        details: { error: err },
      });
    }
  }, [
    newProcedureName,
    newProcedureDescription,
    createProcedure,
    refetch,
    addError,
    t,
  ]);

  const handleOpenCreateModal = useCallback(() => {
    setShowCreateModal(true);
  }, []);

  const handleCloseCreateModal = useCallback(() => {
    setShowCreateModal(false);
    setNewProcedureName("");
    setNewProcedureDescription("");
  }, []);

  // Don't show anything if there's a backend connection error - it's handled by BackendErrorOverlay in Flow
  if (queryError && isBackendConnectionError(queryError)) {
    return (
      <div
        className="d-flex align-items-center"
        style={{
          border: "1px solid var(--app-border)",
          borderRadius: "6px",
          padding: "0.35rem 0.75rem",
          backgroundColor: "var(--app-surface)",
          opacity: 0.5,
        }}
      >
        <span className="text-muted small">
          <i className="bi bi-exclamation-triangle me-2" />
          {t("procedures.backendUnavailable")}
        </span>
      </div>
    );
  }

  // Show other GraphQL or non-connection errors normally
  if (queryError) {
    const errorInfo = getApolloErrorMessage(queryError);
    return (
      <ErrorState
        title={errorInfo.title}
        message={errorInfo.message}
        severity="error"
        onRetry={async () => {
          await refetch();
        }}
        fullScreen={false}
      />
    );
  }

  return (
    <>
      <MotionContainer
        animation="slideDown"
        className="procedure-selector-container"
      >
        <div className="d-flex align-items-center gap-2">
          {loadedProcedure ? (
            <div
              className="d-flex align-items-center procedure-selector-box"
              style={{
                border: "1px solid var(--app-border)",
                borderRadius: "6px",
                padding: "0.35rem 0.75rem",
                backgroundColor: "var(--app-surface)",
                gap: "0.75rem",
                justifyContent: "space-between",
              }}
            >
              <div className="text-truncate" style={{ flex: 1 }}>
                <strong
                  className="text-primary"
                  style={{ fontSize: "0.85rem" }}
                >
                  {loadedProcedure.name}
                </strong>
              </div>
              <MotionButton
                variant="danger"
                size="sm"
                onClick={handleUnloadProcedure}
                loading={isLoadingAction}
                disabled={isLoadingAction}
                data-bs-toggle="tooltip"
                data-bs-placement="bottom"
                title={t("procedures.unloadCurrent")}
                style={{ marginLeft: "0.25rem", flexShrink: 0 }}
              >
                <i className="bi bi-x-circle me-1" />
                {t("procedures.unload")}
              </MotionButton>
            </div>
          ) : (
            <div
              className="d-flex align-items-center procedure-selector-box"
              style={{
                border: "1px solid var(--app-border)",
                borderRadius: "6px",
                padding: "0.35rem 0.75rem",
                backgroundColor: "var(--app-surface)",
                gap: "0.75rem",
              }}
            >
              <Form.Select
                size="sm"
                value={selectedProcedureId}
                onChange={(e) => setSelectedProcedureId(e.target.value)}
                disabled={isLoadingAction}
                style={{ flex: 1, minWidth: 0 }}
              >
                <option value="">{t("procedures.selectProcedure")}</option>
                {procedures.map((proc) => (
                  <option key={proc.id} value={proc.id}>
                    {proc.name}
                  </option>
                ))}
              </Form.Select>

              <ButtonGroup style={{ flexShrink: 0 }}>
                <MotionButton
                  variant="primary"
                  size="sm"
                  onClick={handleLoadProcedure}
                  disabled={!selectedProcedureId || isLoadingAction}
                  loading={isLoadingAction}
                  data-bs-toggle="tooltip"
                  data-bs-placement="bottom"
                  title={t("procedures.loadSelected")}
                >
                  <i className="bi bi-folder-open me-1" />
                  {t("procedures.load")}
                </MotionButton>
                <MotionButton
                  variant="success"
                  size="sm"
                  onClick={handleOpenCreateModal}
                  disabled={isLoadingAction}
                  data-bs-toggle="tooltip"
                  data-bs-placement="bottom"
                  title={t("procedures.createNew")}
                >
                  <i className="bi bi-plus-lg me-1" />
                  {t("procedures.new")}
                </MotionButton>
              </ButtonGroup>
            </div>
          )}
        </div>
      </MotionContainer>

      <Modal
        show={showCreateModal}
        onHide={handleCloseCreateModal}
        centered
        backdrop="static"
      >
        <Modal.Header
          closeButton
          style={{
            backgroundColor: "var(--app-surface)",
            borderBottomColor: "var(--app-border)",
          }}
        >
          <Modal.Title className="d-flex align-items-center">
            <i className="bi bi-plus-lg text-primary me-2" />
            {t("procedures.createNewProcedure")}
          </Modal.Title>
        </Modal.Header>

        <Modal.Body>
          <MotionContainer animation="slideUp">
            <Form>
              <Form.Group className="mb-3">
                <Form.Label>
                  {t("procedures.procedureName")}{" "}
                  <span className="text-danger">*</span>
                </Form.Label>
                <Form.Control
                  type="text"
                  placeholder={t("procedures.enterProcedureName")}
                  value={newProcedureName}
                  onChange={(e) => setNewProcedureName(e.target.value)}
                  autoFocus
                />
              </Form.Group>

              <Form.Group className="mb-3">
                <Form.Label>{t("procedures.description")}</Form.Label>
                <Form.Control
                  as="textarea"
                  rows={3}
                  placeholder={t("procedures.enterDescription")}
                  value={newProcedureDescription}
                  onChange={(e) => setNewProcedureDescription(e.target.value)}
                />
              </Form.Group>
            </Form>
          </MotionContainer>
        </Modal.Body>

        <Modal.Footer>
          <MotionButton variant="secondary" onClick={handleCloseCreateModal}>
            <i className="bi bi-x-circle me-2" />
            {t("actions.cancel")}
          </MotionButton>
          <MotionButton
            variant="primary"
            onClick={handleCreateProcedure}
            disabled={!newProcedureName.trim()}
            loading={createLoading}
          >
            <i className="bi bi-check-circle me-2" />
            {t("actions.create")}
          </MotionButton>
        </Modal.Footer>

        {createLoading && <LoadingOverlay show={true} />}
      </Modal>
    </>
  );
};
