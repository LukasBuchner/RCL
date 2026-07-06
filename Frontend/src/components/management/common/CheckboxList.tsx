import React from "react";
import { Form } from "react-bootstrap";
import { ApolloError } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { ErrorAlert } from "../../error";

interface CheckboxItem {
  id: string;
  label: string;
  description?: string;
}

interface CheckboxListProps {
  label: string;
  items: CheckboxItem[];
  selectedIds: string[];
  onToggle: (id: string) => void;
  loading?: boolean;
  error?: ApolloError;
  emptyText?: string;
  errorTitle?: string;
  errorDescription?: string;
  onRetry?: () => void;
  maxHeight?: string;
}

export const CheckboxList: React.FC<CheckboxListProps> = ({
  label,
  items,
  selectedIds,
  onToggle,
  loading = false,
  error,
  emptyText = "No items available",
  errorTitle = "Failed to load items",
  errorDescription = "Unable to load the items list. Items can still be saved without assignments.",
  onRetry,
  maxHeight = "200px",
}) => {
  const { t } = useTranslation();

  return (
    <Form.Group className="mb-3">
      <Form.Label>{label}</Form.Label>
      {error ? (
        <ErrorAlert
          title={errorTitle}
          message={errorDescription}
          severity="warning"
          onRetry={onRetry}
          size="sm"
          className="mb-2"
        />
      ) : loading ? (
        <div className="border rounded p-3 d-flex align-items-center justify-content-center">
          <span className="spinner-border spinner-border-sm me-2"></span>
          <small className="text-muted">{t("loading.loading")}</small>
        </div>
      ) : (
        <div
          className="border rounded p-3"
          style={{ maxHeight, overflowY: "auto" }}
        >
          {items.length === 0 ? (
            <small className="text-muted fst-italic">{emptyText}</small>
          ) : (
            items.map((item) => (
              <Form.Check
                key={item.id}
                type="checkbox"
                label={
                  item.description
                    ? `${item.label} - ${item.description}`
                    : item.label
                }
                checked={selectedIds.includes(item.id)}
                onChange={() => onToggle(item.id)}
                className="mb-1"
              />
            ))
          )}
        </div>
      )}
    </Form.Group>
  );
};
