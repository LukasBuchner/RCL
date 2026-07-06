import React, { useState } from "react";
import { Button, Modal, Badge } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { AnimatePresence } from "framer-motion";
import { VariableDefinition } from "./types";
import { ManagementCard } from "../management/common";
import { MotionButton } from "../motion";
import "../management/styles/management.css";
import { createLogger } from "../../utils/logger";

const log = createLogger("Variables");

export interface VariableListProps {
  variables: VariableDefinition[];
  onEdit: (variable: VariableDefinition) => void;
  onDelete: (variableName: string) => void;
  onAdd: () => void;
}

/**
 * Maps variable types to Bootstrap icon class names.
 * @param type - The variable type
 * @returns The icon class name
 */
const getTypeIcon = (type: string): string => {
  const iconMap: Record<string, string> = {
    String: "bi-quote",
    Number: "bi-123",
    Boolean: "bi-toggle-on",
    Position: "bi-geo-alt",
    PositionTag: "bi-bookmark",
    SceneObject: "bi-box",
    Enum: "bi-list-ul",
    List: "bi-collection",
  };
  return iconMap[type] || "bi-code";
};

/**
 * Maps variable types to badge variant colors.
 * @param type - The variable type
 * @returns The badge variant
 */
const getTypeBadgeVariant = (type: string): string => {
  const variantMap: Record<string, string> = {
    String: "primary",
    Number: "success",
    Boolean: "warning",
    Position: "info",
    PositionTag: "secondary",
    SceneObject: "dark",
    Enum: "danger",
    List: "light",
  };
  return variantMap[type] || "secondary";
};

/**
 * Component that displays variables in a card-based grid layout.
 */
const VariableList: React.FC<VariableListProps> = ({
  variables,
  onEdit,
  onDelete,
}) => {
  const { t } = useTranslation();
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  const handleDeleteClick = (variableName: string) => {
    setDeleteConfirm(variableName);
  };

  const handleDeleteConfirm = async () => {
    if (deleteConfirm) {
      try {
        await onDelete(deleteConfirm);
        setDeleteConfirm(null);
      } catch (err) {
        log.error("Failed to delete variable:", err);
      }
    }
  };

  const handleDeleteCancel = () => {
    setDeleteConfirm(null);
  };

  return (
    <>
      <div className="variables-grid">
        <AnimatePresence mode="popLayout">
          {variables.map((variable, index) => (
            <ManagementCard key={variable.name} index={index}>
              {/* Variable Header */}
              <div className="variable-header p-3 border-bottom">
                <div className="d-flex align-items-center justify-content-between">
                  <div className="d-flex align-items-center flex-grow-1 gap-3">
                    <div
                      className="d-flex align-items-center justify-content-center rounded-circle flex-shrink-0"
                      style={{
                        width: "40px",
                        height: "40px",
                        background:
                          "var(--bs-" +
                          getTypeBadgeVariant(variable.type) +
                          "-bg-subtle)",
                        border:
                          "2px solid var(--bs-" +
                          getTypeBadgeVariant(variable.type) +
                          ")",
                      }}
                    >
                      <i
                        className={`${getTypeIcon(variable.type)} text-${getTypeBadgeVariant(variable.type)}`}
                        style={{ fontSize: "1.2rem" }}
                      ></i>
                    </div>
                    <div className="flex-grow-1 min-w-0">
                      <div className="d-flex align-items-center gap-2 mb-1">
                        <h6 className="mb-0 fw-semibold text-truncate">
                          {variable.name}
                        </h6>
                        <Badge
                          bg={getTypeBadgeVariant(variable.type)}
                          className="px-2"
                          style={{ fontSize: "0.7rem" }}
                        >
                          {variable.type}
                        </Badge>
                        {variable.isReadOnly && (
                          <Badge
                            bg="secondary"
                            className="px-2"
                            style={{ fontSize: "0.7rem" }}
                          >
                            <i className="bi bi-lock-fill"></i>
                          </Badge>
                        )}
                      </div>
                      {variable.description && (
                        <p className="mb-0 text-muted small text-truncate">
                          {variable.description}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="d-flex gap-1 flex-shrink-0">
                    <MotionButton
                      variant="outline-primary"
                      size="sm"
                      onClick={() => onEdit(variable)}
                      className="d-flex align-items-center"
                      aria-label={`Edit ${variable.name}`}
                    >
                      <i className="bi bi-pencil"></i>
                    </MotionButton>
                    <MotionButton
                      variant="outline-danger"
                      size="sm"
                      onClick={() => handleDeleteClick(variable.name)}
                      disabled={variable.isReadOnly}
                      className="d-flex align-items-center"
                      aria-label={`Delete ${variable.name}`}
                    >
                      <i className="bi bi-trash"></i>
                    </MotionButton>
                  </div>
                </div>
              </div>

              {/* Variable Details */}
              <div className="variable-details p-3">
                <div className="d-flex flex-wrap gap-3 align-items-center">
                  {/* Scope */}
                  <div className="d-flex align-items-center gap-2">
                    <i className="bi bi-bounding-box text-muted"></i>
                    <span className="small">
                      <span className="text-muted">
                        {t("variables.variableScope")}:
                      </span>{" "}
                      <span className="fw-medium">{variable.scope}</span>
                    </span>
                  </div>

                  {/* Source */}
                  <div className="d-flex align-items-center gap-2">
                    <i className="bi bi-diagram-3 text-muted"></i>
                    <span className="small">
                      <span className="text-muted">
                        {t("variables.variableSource")}:
                      </span>{" "}
                      <span className="fw-medium">{variable.source}</span>
                    </span>
                  </div>

                  {/* Default Value */}
                  {variable.defaultValue && (
                    <div className="d-flex align-items-center gap-2">
                      <i className="bi bi-star text-muted"></i>
                      <span className="small">
                        <span className="text-muted">
                          {t("variables.defaultValue")}:
                        </span>{" "}
                        <Badge
                          bg="light"
                          text="dark"
                          className="font-monospace"
                        >
                          {variable.defaultValue}
                        </Badge>
                      </span>
                    </div>
                  )}
                </div>

                {/* Additional details for Enum types */}
                {variable.type === "Enum" &&
                  variable.allowedValues &&
                  variable.allowedValues.length > 0 && (
                    <div className="mt-3 pt-3 border-top">
                      <div className="d-flex align-items-start gap-2">
                        <i className="bi bi-list-check text-muted mt-1"></i>
                        <div className="flex-grow-1">
                          <small className="text-muted d-block mb-2">
                            Allowed Values:
                          </small>
                          <div className="d-flex flex-wrap gap-1">
                            {variable.allowedValues.map((value, idx) => (
                              <Badge
                                key={idx}
                                bg="light"
                                text="dark"
                                className="px-2 py-1 font-monospace"
                              >
                                {value}
                              </Badge>
                            ))}
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                {/* Additional details for List types */}
                {variable.type === "List" && variable.elementType && (
                  <div className="mt-3 pt-3 border-top">
                    <div className="d-flex align-items-center gap-2">
                      <i className="bi bi-box-seam text-muted"></i>
                      <small className="text-muted">Element Type:</small>
                      <Badge
                        bg={getTypeBadgeVariant(variable.elementType)}
                        className="px-2"
                      >
                        <i
                          className={`${getTypeIcon(variable.elementType)} me-1`}
                        ></i>
                        {variable.elementType}
                      </Badge>
                    </div>
                  </div>
                )}
              </div>
            </ManagementCard>
          ))}
        </AnimatePresence>
      </div>

      <Modal show={deleteConfirm !== null} onHide={handleDeleteCancel}>
        <Modal.Header closeButton>
          <Modal.Title>{t("variables.deleteConfirm")}</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          Are you sure you want to delete the variable{" "}
          <strong>{deleteConfirm}</strong>? This action cannot be undone.
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleDeleteCancel}>
            {t("actions.cancel")}
          </Button>
          <Button variant="danger" onClick={handleDeleteConfirm}>
            {t("actions.delete")}
          </Button>
        </Modal.Footer>
      </Modal>
    </>
  );
};

export default VariableList;
