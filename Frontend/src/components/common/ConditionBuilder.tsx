import React, { useEffect, useState } from "react";
import { Col, Form, Row } from "react-bootstrap";
import { useTranslation } from "react-i18next";

export interface Variable {
  name: string;
  type: "string" | "number" | "boolean";
}

export interface ConditionBuilderProps {
  value: string;
  onChange: (expression: string) => void;
  variables: Variable[];
  disabled?: boolean;
}

const OPERATORS = {
  string: ["==", "!="],
  number: ["==", "!=", ">", "<", ">=", "<="],
  boolean: ["==", "!="],
};

const parseExpression = (
  expression: string,
): { variable: string; operator: string; value: string } | null => {
  if (!expression || expression.trim() === "") {
    return null;
  }

  const operators = ["==", "!=", ">=", "<=", ">", "<"];

  for (const op of operators) {
    const parts = expression.split(op);
    if (parts.length === 2) {
      const variable = parts[0].trim();
      let value = parts[1].trim();

      if (value.startsWith("'") && value.endsWith("'")) {
        value = value.slice(1, -1);
      }

      return { variable, operator: op, value };
    }
  }

  return null;
};

const buildExpression = (
  variable: string,
  operator: string,
  value: string,
  type: "string" | "number" | "boolean",
): string => {
  if (!variable || !operator || value === "") {
    return "";
  }

  let formattedValue = value;
  if (type === "string") {
    formattedValue = `'${value}'`;
  }

  return `${variable} ${operator} ${formattedValue}`;
};

const ConditionBuilder: React.FC<ConditionBuilderProps> = ({
  value,
  onChange,
  variables,
  disabled = false,
}) => {
  const { t } = useTranslation();
  const [selectedVariable, setSelectedVariable] = useState<string>("");
  const [selectedOperator, setSelectedOperator] = useState<string>("==");
  const [inputValue, setInputValue] = useState<string>("");

  useEffect(() => {
    const parsed = parseExpression(value);
    if (parsed) {
      setSelectedVariable(parsed.variable);
      setSelectedOperator(parsed.operator);
      setInputValue(parsed.value);
    } else if (value === "") {
      setSelectedVariable("");
      setSelectedOperator("==");
      setInputValue("");
    }
  }, [value]);

  const handleVariableChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newVariable = e.target.value;
    setSelectedVariable(newVariable);

    if (!newVariable) {
      onChange("");
      setInputValue("");
      return;
    }

    const variable = variables.find((v) => v.name === newVariable);
    if (variable) {
      const defaultOperator = OPERATORS[variable.type][0];
      setSelectedOperator(defaultOperator);

      const defaultValue = variable.type === "boolean" ? "true" : "";
      setInputValue(defaultValue);

      if (defaultValue) {
        onChange(
          buildExpression(
            newVariable,
            defaultOperator,
            defaultValue,
            variable.type,
          ),
        );
      }
    }
  };

  const handleOperatorChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newOperator = e.target.value;
    setSelectedOperator(newOperator);

    if (selectedVariable && inputValue) {
      const variable = variables.find((v) => v.name === selectedVariable);
      if (variable) {
        onChange(
          buildExpression(
            selectedVariable,
            newOperator,
            inputValue,
            variable.type,
          ),
        );
      }
    }
  };

  const handleValueChange = (
    e: React.ChangeEvent<
      HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement
    >,
  ) => {
    const newValue = e.target.value;
    setInputValue(newValue);

    if (selectedVariable && newValue) {
      const variable = variables.find((v) => v.name === selectedVariable);
      if (variable) {
        onChange(
          buildExpression(
            selectedVariable,
            selectedOperator,
            newValue,
            variable.type,
          ),
        );
      }
    }
  };

  const currentVariable = variables.find((v) => v.name === selectedVariable);
  const availableOperators = currentVariable
    ? OPERATORS[currentVariable.type]
    : ["==", "!="];

  return (
    <div className="condition-builder">
      <Row className="g-2">
        <Col md={4}>
          <Form.Group>
            <Form.Label>{t("conditions.variable")}</Form.Label>
            <Form.Select
              value={selectedVariable}
              onChange={handleVariableChange}
              disabled={disabled}
              aria-label={t("conditions.variable")}
            >
              <option value="">{t("conditions.selectVariable")}</option>
              {variables.map((variable) => (
                <option key={variable.name} value={variable.name}>
                  {variable.name}
                </option>
              ))}
            </Form.Select>
          </Form.Group>
        </Col>

        <Col md={3}>
          <Form.Group>
            <Form.Label>{t("conditions.operator")}</Form.Label>
            <Form.Select
              value={selectedOperator}
              onChange={handleOperatorChange}
              disabled={disabled || !selectedVariable}
              aria-label={t("conditions.operator")}
            >
              {availableOperators.map((op) => (
                <option key={op} value={op}>
                  {op}
                </option>
              ))}
            </Form.Select>
          </Form.Group>
        </Col>

        <Col md={5}>
          <Form.Group>
            <Form.Label>{t("conditions.value")}</Form.Label>
            {currentVariable?.type === "boolean" ? (
              <Form.Select
                value={inputValue}
                onChange={handleValueChange}
                disabled={disabled || !selectedVariable}
                aria-label={t("conditions.value")}
              >
                <option value="true">true</option>
                <option value="false">false</option>
              </Form.Select>
            ) : (
              <Form.Control
                type={currentVariable?.type === "number" ? "number" : "text"}
                value={inputValue}
                onChange={handleValueChange}
                disabled={disabled || !selectedVariable}
                placeholder={t("conditions.enterValue")}
                aria-label={t("conditions.value")}
              />
            )}
          </Form.Group>
        </Col>
      </Row>

      {value && (
        <div className="mt-2 text-muted small">
          <strong>{t("conditions.expression")}:</strong> <code>{value}</code>
        </div>
      )}
    </div>
  );
};

export default ConditionBuilder;
