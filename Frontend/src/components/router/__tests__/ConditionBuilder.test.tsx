import { render, screen, fireEvent } from "../../../test/test-utils";
import {
  ConditionBuilder,
  SimpleCondition,
  ExpressionCondition,
} from "../ConditionBuilder";
import { VariableDefinition } from "../../Variables/types";

describe("ConditionBuilder", () => {
  const stringVariable: VariableDefinition = {
    name: "quality_result",
    type: "String",
    scope: "PROCEDURE",
    source: "SKILL_OUTPUT",
    isReadOnly: false,
  };

  const numberVariable: VariableDefinition = {
    name: "temperature",
    type: "Number",
    scope: "PROCEDURE",
    source: "AGENT_STATE",
    isReadOnly: false,
  };

  const booleanVariable: VariableDefinition = {
    name: "isActive",
    type: "Boolean",
    scope: "PROCEDURE",
    source: "AGENT_STATE",
    isReadOnly: false,
  };

  const enumVariable: VariableDefinition = {
    name: "color",
    type: "String",
    scope: "PROCEDURE",
    source: "SKILL_OUTPUT",
    isReadOnly: false,
    allowedValues: ["Red", "Green", "Blue"],
  };

  describe("Simple Mode - Rendering", () => {
    it("should render variable, operator, and value fields", () => {
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      expect(screen.getByLabelText(/variable/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/operator/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/value/i)).toBeInTheDocument();
    });
  });

  describe("Variable Selection and Persistence", () => {
    it("should persist variable selection when a variable is selected", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable, numberVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const variableSelect = screen.getByLabelText(
        /variable/i,
      ) as HTMLSelectElement;
      fireEvent.change(variableSelect, { target: { value: "quality_result" } });

      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          variable: "quality_result",
          fullCondition: "", // Empty because value is not yet filled
        }),
      );
    });

    it("should show the selected variable in the dropdown", () => {
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable, numberVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const variableSelect = screen.getByLabelText(
        /variable/i,
      ) as HTMLSelectElement;
      expect(variableSelect.value).toBe("quality_result");
    });
  });

  describe("Type-Aware Operator Filtering", () => {
    it("should show only equals and notEquals operators for Boolean variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "isActive",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[booleanVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const operatorSelect = screen.getByLabelText(/operator/i);
      const options = Array.from(operatorSelect.querySelectorAll("option")).map(
        (opt) => opt.textContent,
      );

      expect(options).toContain("equals");
      expect(options).toContain("not equals");
      expect(options).toHaveLength(2);
      expect(options).not.toContain("greater than");
      expect(options).not.toContain("contains");
    });

    it("should show all comparison operators for Number variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "temperature",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[numberVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const operatorSelect = screen.getByLabelText(/operator/i);
      const options = Array.from(operatorSelect.querySelectorAll("option")).map(
        (opt) => opt.textContent,
      );

      expect(options).toContain("equals");
      expect(options).toContain("not equals");
      expect(options).toContain("greater than");
      expect(options).toContain("less than");
      expect(options).toContain("greater or equal");
      expect(options).toContain("less or equal");
      expect(options).toHaveLength(6);
      expect(options).not.toContain("contains");
    });

    it("should show string operators for String variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const operatorSelect = screen.getByLabelText(/operator/i);
      const options = Array.from(operatorSelect.querySelectorAll("option")).map(
        (opt) => opt.textContent,
      );

      expect(options).toContain("equals");
      expect(options).toContain("not equals");
      expect(options).toContain("contains");
      expect(options).toContain("starts with");
      expect(options).toHaveLength(4);
      expect(options).not.toContain("greater than");
    });

    it("should update operator when variable type changes and operator is invalid", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "contains", // Valid for string
        value: "test",
        fullCondition: 'quality_result contains "test"',
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable, numberVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      // Change to number variable - 'contains' is not valid for numbers
      const variableSelect = screen.getByLabelText(/variable/i);
      fireEvent.change(variableSelect, { target: { value: "temperature" } });

      // Should fall back to first available operator for numbers (equals)
      // Since value is preserved, fullCondition should be regenerated
      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          variable: "temperature",
          operator: "equals", // Changed from 'contains'
          value: "test", // Preserved
          fullCondition: 'temperature == "test"', // Regenerated with new variable and operator
        }),
      );
    });
  });

  describe("Value Input Type Adaptation", () => {
    it("should show dropdown with true/false for Boolean variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "isActive",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[booleanVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i);
      expect(valueInput.tagName).toBe("SELECT");

      const options = Array.from(valueInput.querySelectorAll("option")).map(
        (opt) => (opt as HTMLOptionElement).value,
      );

      expect(options).toContain("true");
      expect(options).toContain("false");
    });

    it("should show dropdown with allowed values for enum variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "color",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[enumVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i);
      expect(valueInput.tagName).toBe("SELECT");

      const options = Array.from(valueInput.querySelectorAll("option")).map(
        (opt) => (opt as HTMLOptionElement).value,
      );

      expect(options).toContain("Red");
      expect(options).toContain("Green");
      expect(options).toContain("Blue");
    });

    it("should show number input for Number variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "temperature",
        operator: "greaterThan",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[numberVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i) as HTMLInputElement;
      expect(valueInput.type).toBe("number");
    });

    it("should show text input for String variables", () => {
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "contains",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i) as HTMLInputElement;
      expect(valueInput.type).toBe("text");
    });
  });

  describe("Full Condition Generation", () => {
    it("should generate fullCondition when all fields are filled", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i);
      fireEvent.change(valueInput, { target: { value: "OK" } });

      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          variable: "quality_result",
          operator: "equals",
          value: "OK",
          fullCondition: 'quality_result == "OK"',
        }),
      );
    });

    it("should always include fullCondition property even when empty", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const variableSelect = screen.getByLabelText(/variable/i);
      fireEvent.change(variableSelect, { target: { value: "quality_result" } });

      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          fullCondition: "", // Present but empty because value is not filled
        }),
      );
    });

    it("should set fullCondition to empty string for incomplete conditions", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "OK",
        fullCondition: 'quality_result == "OK"',
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i);
      fireEvent.change(valueInput, { target: { value: "" } }); // Clear value

      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          variable: "quality_result",
          operator: "equals",
          value: "",
          fullCondition: "", // Empty because condition is incomplete
        }),
      );
    });

    it("should display the fullCondition in an alert when complete", () => {
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "OK",
        fullCondition: 'quality_result == "OK"',
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      expect(screen.getByText(/quality_result == "OK"/i)).toBeInTheDocument();
    });

    it("should not display alert when fullCondition is empty", () => {
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      expect(screen.queryByText(/condition:/i)).not.toBeInTheDocument();
    });
  });

  describe("onChange Callback", () => {
    it("should call onChange with proper SimpleCondition object on variable change", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const variableSelect = screen.getByLabelText(/variable/i);
      fireEvent.change(variableSelect, { target: { value: "quality_result" } });

      expect(handleChange).toHaveBeenCalledWith({
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      });
    });

    it("should call onChange with proper SimpleCondition object on operator change", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "temperature",
        operator: "equals",
        value: "50",
        fullCondition: "temperature == 50",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[numberVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const operatorSelect = screen.getByLabelText(/operator/i);
      fireEvent.change(operatorSelect, { target: { value: "greaterThan" } });

      // Number values are not quoted in the condition string
      expect(handleChange).toHaveBeenCalledWith({
        variable: "temperature",
        operator: "greaterThan",
        value: "50",
        fullCondition: "temperature > 50",
      });
    });

    it("should call onChange with proper SimpleCondition object on value change", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const valueInput = screen.getByLabelText(/value/i);
      fireEvent.change(valueInput, { target: { value: "OK" } });

      expect(handleChange).toHaveBeenCalledWith({
        variable: "quality_result",
        operator: "equals",
        value: "OK",
        fullCondition: 'quality_result == "OK"',
      });
    });

    it("should call onChange when boolean dropdown value changes", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "isActive",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[booleanVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const valueSelect = screen.getByLabelText(/value/i);
      fireEvent.change(valueSelect, { target: { value: "true" } });

      // Boolean values are not quoted in the condition string
      expect(handleChange).toHaveBeenCalledWith({
        variable: "isActive",
        operator: "equals",
        value: "true",
        fullCondition: "isActive == true",
      });
    });

    it("should call onChange when enum dropdown value changes", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "color",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[enumVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const valueSelect = screen.getByLabelText(/value/i);
      fireEvent.change(valueSelect, { target: { value: "Red" } });

      expect(handleChange).toHaveBeenCalledWith({
        variable: "color",
        operator: "equals",
        value: "Red",
        fullCondition: 'color == "Red"',
      });
    });
  });

  describe("Expression Mode", () => {
    it("should render expression textarea in expression mode", () => {
      const expressionValue: ExpressionCondition = {
        expression: "",
      };

      render(
        <ConditionBuilder
          mode="expression"
          variables={[stringVariable]}
          value={expressionValue}
          onChange={vi.fn()}
        />,
      );

      expect(screen.getByLabelText(/expression/i)).toBeInTheDocument();
      expect(screen.queryByLabelText(/variable/i)).not.toBeInTheDocument();
    });

    it("should call onChange when expression changes", () => {
      const handleChange = vi.fn();
      const expressionValue: ExpressionCondition = {
        expression: "",
      };

      render(
        <ConditionBuilder
          mode="expression"
          variables={[stringVariable]}
          value={expressionValue}
          onChange={handleChange}
        />,
      );

      const expressionInput = screen.getByLabelText(/expression/i);
      fireEvent.change(expressionInput, {
        target: { value: "temperature > 50 && pressure < 100" },
      });

      expect(handleChange).toHaveBeenCalledWith({
        expression: "temperature > 50 && pressure < 100",
      });
    });

    it("should validate expression syntax when validate prop is true", () => {
      const expressionValue: ExpressionCondition = {
        expression: "invalid ===",
      };

      render(
        <ConditionBuilder
          mode="expression"
          variables={[stringVariable]}
          value={expressionValue}
          onChange={vi.fn()}
          validate={true}
        />,
      );

      expect(screen.getByText(/invalid syntax/i)).toBeInTheDocument();
    });

    it("should not show validation error when validate prop is false", () => {
      const expressionValue: ExpressionCondition = {
        expression: "invalid ===",
      };

      render(
        <ConditionBuilder
          mode="expression"
          variables={[stringVariable]}
          value={expressionValue}
          onChange={vi.fn()}
          validate={false}
        />,
      );

      expect(screen.queryByText(/invalid syntax/i)).not.toBeInTheDocument();
    });
  });

  describe("Edge Cases", () => {
    it("should disable operator select when no variable is selected", () => {
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const operatorSelect = screen.getByLabelText(
        /operator/i,
      ) as HTMLSelectElement;
      expect(operatorSelect).toBeDisabled();
    });

    it("should handle empty variables array gracefully", () => {
      const simpleValue: SimpleCondition = {
        variable: "",
        operator: "equals",
        value: "",
        fullCondition: "",
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[]}
          value={simpleValue}
          onChange={vi.fn()}
        />,
      );

      const variableSelect = screen.getByLabelText(/variable/i);
      expect(variableSelect).toBeInTheDocument();
      expect(variableSelect.querySelectorAll("option")).toHaveLength(1); // Only "Select variable..." option
    });

    it("should preserve operator when it is valid for the new variable type", () => {
      const handleChange = vi.fn();
      const simpleValue: SimpleCondition = {
        variable: "quality_result",
        operator: "equals", // Valid for both string and number
        value: "test",
        fullCondition: 'quality_result == "test"',
      };

      render(
        <ConditionBuilder
          mode="simple"
          variables={[stringVariable, numberVariable]}
          value={simpleValue}
          onChange={handleChange}
        />,
      );

      const variableSelect = screen.getByLabelText(/variable/i);
      fireEvent.change(variableSelect, { target: { value: "temperature" } });

      // Should preserve 'equals' operator since it's valid for numbers
      expect(handleChange).toHaveBeenCalledWith(
        expect.objectContaining({
          variable: "temperature",
          operator: "equals", // Preserved
        }),
      );
    });
  });
});
