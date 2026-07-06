import React, { useMemo } from "react";
import { Alert, Form } from "react-bootstrap";
import { ValueType, VariableDefinition } from "../Variables/types";

/**
 * Represents a simple condition with variable, operator, and value.
 */
export interface SimpleCondition {
  variable: string;
  operator: Operator;
  value: string;
  fullCondition?: string;
}

/**
 * Represents a complex expression condition.
 */
export interface ExpressionCondition {
  expression: string;
}

export type ConditionValue = SimpleCondition | ExpressionCondition;

/**
 * Supported operators for condition building.
 */
export type Operator =
  | "equals"
  | "notEquals"
  | "greaterThan"
  | "lessThan"
  | "greaterOrEqual"
  | "lessOrEqual"
  | "contains"
  | "startsWith";

/**
 * Props for the ConditionBuilder component.
 */
export interface ConditionBuilderProps {
  mode: "simple" | "expression";
  variables: VariableDefinition[];
  value: ConditionValue;
  onChange: (condition: ConditionValue) => void;
  validate?: boolean;
}

const STRING_OPERATORS: Operator[] = [
  "equals",
  "notEquals",
  "contains",
  "startsWith",
];

const NUMBER_OPERATORS: Operator[] = [
  "equals",
  "notEquals",
  "greaterThan",
  "lessThan",
  "greaterOrEqual",
  "lessOrEqual",
];

const BOOLEAN_OPERATORS: Operator[] = ["equals", "notEquals"];

const OPERATOR_SYMBOLS: Record<Operator, string> = {
  equals: "==",
  notEquals: "!=",
  greaterThan: ">",
  lessThan: "<",
  greaterOrEqual: ">=",
  lessOrEqual: "<=",
  contains: "contains",
  startsWith: "startsWith",
};

const OPERATOR_LABELS: Record<Operator, string> = {
  equals: "equals",
  notEquals: "not equals",
  greaterThan: "greater than",
  lessThan: "less than",
  greaterOrEqual: "greater or equal",
  lessOrEqual: "less or equal",
  contains: "contains",
  startsWith: "starts with",
};

/**
 * Returns the available operators for a given variable type.
 */
function getOperatorsForType(type: ValueType): Operator[] {
  switch (type) {
    case "String":
      return STRING_OPERATORS;
    case "Number":
      return NUMBER_OPERATORS;
    case "Boolean":
      return BOOLEAN_OPERATORS;
    default:
      return ["equals", "notEquals"];
  }
}

/**
 * Builds a complete condition string from its parts.
 * Wraps string values in quotes, leaves boolean and number values unquoted.
 *
 * @param variable - The variable name.
 * @param operator - The comparison operator.
 * @param value - The value to compare against.
 * @param variableType - The type of the variable (determines quoting behavior).
 * @returns The complete condition string.
 */
function buildConditionString(
  variable: string,
  operator: Operator,
  value: string,
  variableType?: ValueType,
): string {
  const symbol = OPERATOR_SYMBOLS[operator];

  // Boolean values should not be quoted (true/false are keywords)
  if (variableType === "Boolean" && (value === "true" || value === "false")) {
    return `${variable} ${symbol} ${value}`;
  }

  // Number values should not be quoted
  if (variableType === "Number" && !isNaN(parseFloat(value))) {
    return `${variable} ${symbol} ${value}`;
  }

  // String and other values should be quoted
  return `${variable} ${symbol} "${value}"`;
}

/**
 * Type guard to check if a condition is a SimpleCondition.
 */
function isSimpleCondition(value: ConditionValue): value is SimpleCondition {
  return "variable" in value && "operator" in value;
}

/**
 * Type guard to check if a condition is an ExpressionCondition.
 */
function isExpressionCondition(
  value: ConditionValue,
): value is ExpressionCondition {
  return "expression" in value;
}

/**
 * Validates an expression condition for common syntax errors.
 * Returns an error message if invalid, or null if valid.
 */
function validateExpression(expression: string): string | null {
  if (!expression || expression.trim() === "") {
    return null;
  }

  if (expression.includes("===")) {
    return "Invalid syntax: Use == instead of ===";
  }

  const invalidPatterns = [/\s===$/, /^\s*===/, /\s+=+\s*$/];

  for (const pattern of invalidPatterns) {
    if (pattern.test(expression)) {
      return "Invalid syntax: Malformed expression";
    }
  }

  return null;
}

/**
 * ConditionBuilder component for creating router branch conditions.
 *
 * Supports two modes:
 * - Simple: Three-field UI (variable dropdown, operator dropdown, value input)
 * - Expression: Free-form textarea for complex boolean expressions
 *
 * Features:
 * - Type-aware operator filtering based on variable type
 * - Automatic condition string building in simple mode
 * - Validation feedback in expression mode
 * - Real-time condition preview
 *
 * @example
 * ```tsx
 * <ConditionBuilder
 *   mode="simple"
 *   variables={procedureVariables}
 *   value={{ variable: 'temperature', operator: 'greaterThan', value: '50' }}
 *   onChange={(condition) => console.log(condition)}
 * />
 * ```
 */
export const ConditionBuilder: React.FC<ConditionBuilderProps> = ({
  mode,
  variables,
  value,
  onChange,
  validate = false,
}) => {
  const selectedVariable = useMemo(() => {
    if (isSimpleCondition(value)) {
      return variables.find((v) => v.name === value.variable);
    }
    return undefined;
  }, [variables, value]);

  const availableOperators = useMemo(() => {
    if (!selectedVariable) {
      return [];
    }
    return getOperatorsForType(selectedVariable.type);
  }, [selectedVariable]);

  const validationError = useMemo(() => {
    if (!validate || !isExpressionCondition(value)) {
      return null;
    }
    return validateExpression(value.expression);
  }, [validate, value]);

  const handleVariableChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    if (isSimpleCondition(value)) {
      const newVariable = e.target.value;
      const variable = variables.find((v) => v.name === newVariable);

      const operators = variable ? getOperatorsForType(variable.type) : [];

      const newOperator = operators.includes(value.operator)
        ? value.operator
        : operators[0];

      const newValue: SimpleCondition = {
        ...value,
        variable: newVariable,
        operator: newOperator,
      };

      // Always set fullCondition (empty string if incomplete, full condition if complete)
      if (newVariable && newOperator && value.value) {
        newValue.fullCondition = buildConditionString(
          newVariable,
          newOperator,
          value.value,
          variable?.type,
        );
      } else {
        newValue.fullCondition = ""; // Empty string for incomplete conditions
      }

      onChange(newValue);
    }
  };

  const handleOperatorChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    if (isSimpleCondition(value)) {
      const newOperator = e.target.value as Operator;
      const newValue: SimpleCondition = {
        ...value,
        operator: newOperator,
      };

      // Always set fullCondition (empty string if incomplete, full condition if complete)
      if (value.variable && newOperator && value.value) {
        newValue.fullCondition = buildConditionString(
          value.variable,
          newOperator,
          value.value,
          selectedVariable?.type,
        );
      } else {
        newValue.fullCondition = ""; // Empty string for incomplete conditions
      }

      onChange(newValue);
    }
  };

  const handleValueChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (isSimpleCondition(value)) {
      const newValueStr = e.target.value;
      const newValue: SimpleCondition = {
        ...value,
        value: newValueStr,
      };

      // Always set fullCondition (empty string if incomplete, full condition if complete)
      if (value.variable && value.operator && newValueStr) {
        newValue.fullCondition = buildConditionString(
          value.variable,
          value.operator,
          newValueStr,
          selectedVariable?.type,
        );
      } else {
        newValue.fullCondition = ""; // Empty string for incomplete conditions
      }

      onChange(newValue);
    }
  };

  const handleExpressionChange = (
    e: React.ChangeEvent<HTMLTextAreaElement>,
  ) => {
    if (isExpressionCondition(value)) {
      onChange({
        expression: e.target.value,
      });
    }
  };

  if (mode === "expression") {
    return (
      <div className="condition-builder">
        <Form.Group className="mb-3">
          <Form.Label htmlFor="expression-input">Expression</Form.Label>
          <Form.Control
            as="textarea"
            id="expression-input"
            value={isExpressionCondition(value) ? value.expression : ""}
            onChange={handleExpressionChange}
            rows={3}
            placeholder="e.g., temperature > 50 && pressure < 100"
            aria-label="Expression"
          />
        </Form.Group>

        {validationError && <Alert variant="danger">{validationError}</Alert>}
      </div>
    );
  }

  return (
    <div className="condition-builder">
      <Form.Group className="mb-3">
        <Form.Label htmlFor="variable-select">Variable</Form.Label>
        <Form.Select
          id="variable-select"
          value={isSimpleCondition(value) ? value.variable : ""}
          onChange={handleVariableChange}
          aria-label="Variable"
        >
          <option value="">Select variable...</option>
          {variables.map((variable) => (
            <option key={variable.name} value={variable.name}>
              {variable.name}
            </option>
          ))}
        </Form.Select>
      </Form.Group>

      <Form.Group className="mb-3">
        <Form.Label htmlFor="operator-select">Operator</Form.Label>
        <Form.Select
          id="operator-select"
          value={isSimpleCondition(value) ? value.operator : "equals"}
          onChange={handleOperatorChange}
          disabled={!selectedVariable}
          aria-label="Operator"
        >
          {availableOperators.map((op) => (
            <option key={op} value={op}>
              {OPERATOR_LABELS[op]}
            </option>
          ))}
        </Form.Select>
      </Form.Group>

      <Form.Group className="mb-3">
        <Form.Label htmlFor="value-input">Value</Form.Label>
        {(() => {
          // Determine if we should show a dropdown based on type and operator
          const currentOperator = isSimpleCondition(value)
            ? value.operator
            : "equals";
          const hasAllowedValues =
            selectedVariable?.allowedValues &&
            selectedVariable.allowedValues.length > 0;

          // Handler for dropdown changes
          const handleDropdownChange = (
            e: React.ChangeEvent<HTMLSelectElement>,
          ) => {
            if (isSimpleCondition(value)) {
              const newValueStr = e.target.value;
              const newValue: SimpleCondition = {
                ...value,
                value: newValueStr,
              };

              // Always set fullCondition (empty string if incomplete, full condition if complete)
              if (value.variable && value.operator && newValueStr) {
                newValue.fullCondition = buildConditionString(
                  value.variable,
                  value.operator,
                  newValueStr,
                  selectedVariable?.type,
                );
              } else {
                newValue.fullCondition = ""; // Empty string for incomplete conditions
              }

              onChange(newValue);
            }
          };

          // For enum types with allowed values, show dropdown
          if (
            hasAllowedValues &&
            (currentOperator === "equals" || currentOperator === "notEquals")
          ) {
            return (
              <Form.Select
                id="value-input"
                value={isSimpleCondition(value) ? value.value : ""}
                onChange={handleDropdownChange}
                aria-label="Value"
              >
                <option value="">Select value...</option>
                {selectedVariable.allowedValues!.map((allowedValue) => (
                  <option key={allowedValue} value={allowedValue}>
                    {allowedValue}
                  </option>
                ))}
              </Form.Select>
            );
          }

          // For boolean with equals/notEquals, show dropdown
          const shouldShowBooleanDropdown =
            selectedVariable?.type === "Boolean" &&
            (currentOperator === "equals" || currentOperator === "notEquals");

          if (shouldShowBooleanDropdown) {
            return (
              <Form.Select
                id="value-input"
                value={isSimpleCondition(value) ? value.value : ""}
                onChange={handleDropdownChange}
                aria-label="Value"
              >
                <option value="">Select value...</option>
                <option value="true">true</option>
                <option value="false">false</option>
              </Form.Select>
            );
          }

          // For numbers, use number input type
          if (selectedVariable?.type === "Number") {
            return (
              <Form.Control
                type="number"
                id="value-input"
                value={isSimpleCondition(value) ? value.value : ""}
                onChange={handleValueChange}
                placeholder="Enter number..."
                aria-label="Value"
                step="any"
              />
            );
          }

          // For strings and other types, use text input
          return (
            <Form.Control
              type="text"
              id="value-input"
              value={isSimpleCondition(value) ? value.value : ""}
              onChange={handleValueChange}
              placeholder={
                currentOperator === "contains"
                  ? "Enter text to find..."
                  : currentOperator === "startsWith"
                    ? "Enter starting text..."
                    : selectedVariable?.type === "String"
                      ? "Enter text..."
                      : "Enter value..."
              }
              aria-label="Value"
            />
          );
        })()}
      </Form.Group>

      {isSimpleCondition(value) && value.fullCondition && (
        <Alert variant="info">
          <strong>Condition:</strong> {value.fullCondition}
        </Alert>
      )}
    </div>
  );
};
