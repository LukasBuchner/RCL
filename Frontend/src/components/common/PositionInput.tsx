import React, { useState, useEffect, useCallback } from "react";
import { Col, Form, InputGroup, Row } from "react-bootstrap";
import "bootstrap-icons/font/bootstrap-icons.css";

import { Position } from "../../__generated__/graphql.ts";

interface PositionInputProps {
  position: Position;
  onChange: (updatedPosition: Position) => void;
}

// Type for the numeric fields only (excluding __typename)
type PositionFields = Omit<Position, "__typename">;
type PositionFieldKey = keyof PositionFields;

/**
 * A controlled input component for editing Position objects.
 * Uses intermediate string state to allow clearing fields during editing,
 * converting back to numbers on blur or when valid values are entered.
 * This follows the modern React pattern for number input handling.
 */
const PositionInput: React.FC<PositionInputProps> = ({
  position,
  onChange,
}) => {
  // Local string state for each field to allow empty values during editing
  const [localValues, setLocalValues] = useState<
    Record<PositionFieldKey, string>
  >({
    x: String(position.x ?? 0),
    y: String(position.y ?? 0),
    z: String(position.z ?? 0),
    alpha: String(position.alpha ?? 0),
    beta: String(position.beta ?? 0),
    gamma: String(position.gamma ?? 0),
  });

  // Sync local state when position prop changes (e.g., when switching skills)
  useEffect(() => {
    setLocalValues({
      x: String(position.x ?? 0),
      y: String(position.y ?? 0),
      z: String(position.z ?? 0),
      alpha: String(position.alpha ?? 0),
      beta: String(position.beta ?? 0),
      gamma: String(position.gamma ?? 0),
    });
  }, [
    position.x,
    position.y,
    position.z,
    position.alpha,
    position.beta,
    position.gamma,
  ]);

  /**
   * Handle input changes - allows any string value including empty
   */
  const handleInputChange = useCallback(
    (key: PositionFieldKey, value: string) => {
      setLocalValues((prev) => ({ ...prev, [key]: value }));
    },
    [],
  );

  /**
   * Convert string to number and propagate to parent on blur
   * Falls back to 0 for empty or invalid values
   */
  const handleBlur = useCallback(
    (key: PositionFieldKey) => {
      const stringValue = localValues[key];
      const numericValue =
        stringValue === "" || stringValue === "-" ? 0 : parseFloat(stringValue);

      // Update parent with the numeric value
      onChange({
        ...position,
        [key]: isNaN(numericValue) ? 0 : numericValue,
      });

      // Ensure local state shows the final numeric value
      setLocalValues((prev) => ({
        ...prev,
        [key]: String(isNaN(numericValue) ? 0 : numericValue),
      }));
    },
    [localValues, onChange, position],
  );

  /**
   * Handle key press - allow Enter to blur and commit the value
   */
  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === "Enter" && e.currentTarget instanceof HTMLElement) {
      e.currentTarget.blur();
    }
  }, []);

  return (
    <>
      {/* Row for x, y, z */}
      <Row className="g-2 mb-3">
        {(["x", "y", "z"] as const).map((axis) => (
          <Col xs={12} sm={4} key={axis}>
            <InputGroup>
              <InputGroup.Text>{axis}</InputGroup.Text>
              <Form.Control
                type="number"
                placeholder={axis.toUpperCase()}
                value={localValues[axis]}
                onChange={(e) => handleInputChange(axis, e.target.value)}
                onBlur={() => handleBlur(axis)}
                onKeyDown={handleKeyDown}
                aria-label={`Position ${axis.toUpperCase()}`}
              />
            </InputGroup>
          </Col>
        ))}
      </Row>

      {/* Row for alpha, beta, gamma */}
      <Row className="g-2 mb-3">
        {[
          { label: "α", key: "alpha" as const },
          { label: "β", key: "beta" as const },
          { label: "γ", key: "gamma" as const },
        ].map(({ label, key }) => (
          <Col xs={12} sm={4} key={key}>
            <InputGroup>
              <InputGroup.Text>{label}</InputGroup.Text>
              <Form.Control
                type="number"
                placeholder={label}
                value={localValues[key]}
                onChange={(e) => handleInputChange(key, e.target.value)}
                onBlur={() => handleBlur(key)}
                onKeyDown={handleKeyDown}
                aria-label={`Position ${label}`}
              />
            </InputGroup>
          </Col>
        ))}
      </Row>
    </>
  );
};

export default PositionInput;
