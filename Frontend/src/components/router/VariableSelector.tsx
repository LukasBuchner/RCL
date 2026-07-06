import React, { useMemo } from "react";
import { Form, Alert } from "react-bootstrap";
import { VariableDefinition, VariableSource } from "../Variables/types";

export interface VariableSelectorProps {
  variables: VariableDefinition[];
  value: string;
  onChange: (variableName: string) => void;
  label?: string;
  helpText?: string;
  groupBySource?: boolean;
}

export const VariableSelector: React.FC<VariableSelectorProps> = ({
  variables,
  value,
  onChange,
  label = "Variable",
  helpText,
  groupBySource = false,
}) => {
  const selectedVariable = useMemo(() => {
    return variables.find((v) => v.name === value);
  }, [variables, value]);

  const variablesBySource = useMemo(() => {
    if (!groupBySource) return null;

    const grouped: Record<VariableSource, VariableDefinition[]> = {
      USER_DEFINED: [],
      SKILL_OUTPUT: [],
      AGENT_STATE: [],
      SYSTEM: [],
    };

    variables.forEach((variable) => {
      grouped[variable.source].push(variable);
    });

    return grouped;
  }, [variables, groupBySource]);

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onChange(e.target.value);
  };

  if (variables.length === 0) {
    return (
      <Alert variant="info">
        No variables defined for this procedure. Please add variables first.
      </Alert>
    );
  }

  return (
    <div className="variable-selector">
      <Form.Group className="mb-3">
        <Form.Label htmlFor="variable-select">{label}</Form.Label>
        <Form.Select
          id="variable-select"
          value={value}
          onChange={handleChange}
          aria-label={label}
        >
          <option value="">Select a variable...</option>

          {groupBySource && variablesBySource
            ? Object.entries(variablesBySource).map(([source, vars]) => {
                if (vars.length === 0) return null;
                return (
                  <optgroup key={source} label={source}>
                    {vars.map((variable) => (
                      <option key={variable.name} value={variable.name}>
                        {variable.name} ({variable.type})
                      </option>
                    ))}
                  </optgroup>
                );
              })
            : variables.map((variable) => (
                <option key={variable.name} value={variable.name}>
                  {variable.name} ({variable.type})
                </option>
              ))}
        </Form.Select>

        {helpText && <Form.Text className="text-muted">{helpText}</Form.Text>}
      </Form.Group>

      {selectedVariable && (
        <Alert variant="info" className="mt-2">
          <div>
            <strong>Type:</strong> {selectedVariable.type}
          </div>
          <div>
            <strong>Source:</strong> {selectedVariable.source}
          </div>
          {selectedVariable.description && (
            <div>
              <strong>Description:</strong> {selectedVariable.description}
            </div>
          )}
        </Alert>
      )}
    </div>
  );
};
