import React from "react";
import { Form, Modal } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionButton, MotionContainer } from "../motion";

/**
 * Props for the unified modal component
 */
interface UnifiedModalProps {
  /** Whether the modal is open */
  show: boolean;
  /** Callback when modal should close */
  onHide: () => void;
  /** Modal title */
  title: string;
  /** Icon class for the modal title (e.g., "bi-plus-lg") */
  icon?: string;
  /** Modal size */
  size?: "sm" | "lg" | "xl";
  /** Whether the modal should be centered */
  centered?: boolean;
  /** Form submission handler */
  onSubmit?: (event: React.FormEvent) => void;
  /** Whether the form is valid (affects submit button state) */
  isValid?: boolean;
  /** Whether we're in edit mode (affects submit button text) */
  isEditing?: boolean;
  /** Custom submit button text (overrides default Create/Update) */
  submitText?: string;
  /** Custom cancel button text (defaults to Cancel) */
  cancelText?: string;
  /** Whether the submit button should show loading state */
  loading?: boolean;
  /** Custom footer content (overrides default buttons) */
  customFooter?: React.ReactNode;
  /** Callback fired after the modal has finished its exit animation and is removed from the DOM */
  onExited?: () => void;
  /** Modal body content */
  children: React.ReactNode;
}

/**
 * Unified modal component that provides consistent appearance and behavior
 * across all modal implementations in the application
 */
export const UnifiedModal: React.FC<UnifiedModalProps> = ({
  show,
  onHide,
  title,
  icon,
  size = "lg",
  centered = true,
  onSubmit,
  isValid = true,
  isEditing = false,
  submitText,
  cancelText,
  loading = false,
  customFooter,
  onExited,
  children,
}) => {
  const { t } = useTranslation();

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    onSubmit?.(event);
  };

  const resolvedCancelText = cancelText ?? t("actions.cancel");
  const defaultSubmitText =
    submitText || (isEditing ? t("actions.update") : t("actions.create"));

  const modalContent = (
    <>
      <Modal.Header
        closeButton
        style={{
          backgroundColor: "var(--app-surface)",
          borderBottomColor: "var(--app-border)",
        }}
      >
        <Modal.Title className="d-flex align-items-center">
          {icon && <i className={`bi ${icon} text-primary me-2`}></i>}
          {title}
        </Modal.Title>
      </Modal.Header>

      <Modal.Body>
        <MotionContainer animation="slideUp">{children}</MotionContainer>
      </Modal.Body>

      <Modal.Footer>
        {customFooter ? (
          customFooter
        ) : (
          <>
            <MotionButton variant="secondary" type="button" onClick={onHide}>
              <i className="bi bi-x-circle me-2"></i>
              {resolvedCancelText}
            </MotionButton>
            <MotionButton
              variant="primary"
              type="submit"
              disabled={!isValid}
              loading={loading}
            >
              <i className="bi bi-check-circle me-2"></i>
              {defaultSubmitText}
            </MotionButton>
          </>
        )}
      </Modal.Footer>
    </>
  );

  return (
    <Modal
      show={show}
      onHide={onHide}
      onExited={onExited}
      size={size}
      centered={centered}
    >
      {onSubmit ? (
        <Form onSubmit={handleSubmit}>{modalContent}</Form>
      ) : (
        modalContent
      )}
    </Modal>
  );
};
