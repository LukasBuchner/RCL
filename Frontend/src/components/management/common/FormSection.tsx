import React from "react";
import { Col, Form, Row } from "react-bootstrap";

interface FormFieldProps {
  label: string;
  type?: string;
  value: string | number;
  onChange: (value: string) => void;
  required?: boolean;
  placeholder?: string;
  as?: "input" | "textarea" | "select";
  rows?: number;
  min?: number;
  max?: number;
  step?: number;
  children?: React.ReactNode; // For select options
}

export const FormField: React.FC<FormFieldProps> = ({
  label,
  type = "text",
  value,
  onChange,
  required = false,
  placeholder,
  as = "input",
  rows,
  min,
  max,
  step,
  children,
}) => {
  const controlProps: {
    as: "input" | "textarea" | "select";
    type: string;
    value: string | number;
    onChange: (
      e: React.ChangeEvent<
        HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
      >,
    ) => void;
    required: boolean;
    placeholder?: string;
    min?: number;
    max?: number;
    step?: number;
    rows?: number;
  } = {
    as,
    type,
    value,
    onChange: (
      e: React.ChangeEvent<
        HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
      >,
    ) => onChange(e.target.value),
    required,
    placeholder,
    min,
    max,
    step,
  };

  if (as === "textarea" && rows) {
    controlProps.rows = rows;
  }

  return (
    <Form.Group className="mb-3">
      <Form.Label>{label}</Form.Label>
      <Form.Control {...controlProps}>{children}</Form.Control>
    </Form.Group>
  );
};

interface FormRowProps {
  children: React.ReactNode;
}

export const FormRow: React.FC<FormRowProps> = ({ children }) => {
  return <Row>{children}</Row>;
};

interface FormColumnProps {
  md?: number;
  children: React.ReactNode;
}

export const FormColumn: React.FC<FormColumnProps> = ({
  md = 12,
  children,
}) => {
  return <Col md={md}>{children}</Col>;
};

interface FormSectionProps {
  title?: string;
  children: React.ReactNode;
}

export const FormSection: React.FC<FormSectionProps> = ({
  title,
  children,
}) => {
  return (
    <div className="form-section mb-4">
      {title && <h6 className="mb-3">{title}</h6>}
      {children}
    </div>
  );
};
