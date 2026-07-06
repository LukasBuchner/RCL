import React from "react";
import { Form } from "react-bootstrap";
import { ValueType } from "./types";

export interface VariableTypeSelectorProps {
  value: ValueType;
  onChange: (type: ValueType) => void;
  disabled?: boolean;
}

const VariableTypeSelector: React.FC<VariableTypeSelectorProps> = ({
  value,
  onChange,
  disabled = false,
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onChange(e.target.value as ValueType);
  };

  return (
    <Form.Select value={value} onChange={handleChange} disabled={disabled}>
      <option value="String">String</option>
      <option value="Number">Number</option>
      <option value="Boolean">Boolean</option>
      <option value="Position">Position</option>
      <option value="PositionTag">PositionTag</option>
      <option value="SceneObject">SceneObject</option>
    </Form.Select>
  );
};

export default VariableTypeSelector;
