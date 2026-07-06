import { render, screen } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import ConditionBuilder from "../ConditionBuilder";

describe("ConditionBuilder", () => {
  const mockOnChange = vi.fn();

  beforeEach(() => {
    mockOnChange.mockClear();
  });

  it("renders variable selector", () => {
    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[
          { name: "status", type: "string" },
          { name: "count", type: "number" },
        ]}
      />,
    );

    expect(screen.getByLabelText(/variable/i)).toBeInTheDocument();
  });

  it("renders operator selector", () => {
    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    expect(screen.getByLabelText(/operator/i)).toBeInTheDocument();
  });

  it("renders value input", () => {
    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    expect(screen.getByLabelText(/value/i)).toBeInTheDocument();
  });

  it("updates expression when user changes variable", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[
          { name: "isActive", type: "boolean" },
          { name: "count", type: "number" },
        ]}
      />,
    );

    // Boolean variables trigger onChange because they have a non-empty default value ("true")
    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "isActive");

    expect(mockOnChange).toHaveBeenCalled();
  });

  it("updates expression when user changes operator", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value="status == 'ready'"
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    const operatorSelect = screen.getByLabelText(/operator/i);
    await user.selectOptions(operatorSelect, "!=");

    expect(mockOnChange).toHaveBeenCalled();
    const lastCall =
      mockOnChange.mock.calls[mockOnChange.mock.calls.length - 1][0];
    expect(lastCall).toContain("!=");
  });

  it("updates expression when user changes value", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value="status == ''"
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    const valueInput = screen.getByLabelText(/value/i);
    await user.clear(valueInput);
    await user.type(valueInput, "active");

    expect(mockOnChange).toHaveBeenCalled();
  });

  it("calls onChange with built expression", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "count", type: "number" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "count");

    const operatorSelect = screen.getByLabelText(/operator/i);
    await user.selectOptions(operatorSelect, ">");

    const valueInput = screen.getByLabelText(/value/i);
    await user.type(valueInput, "10");

    expect(mockOnChange).toHaveBeenLastCalledWith(
      expect.stringContaining("count"),
    );
    expect(mockOnChange).toHaveBeenLastCalledWith(expect.stringContaining(">"));
    expect(mockOnChange).toHaveBeenLastCalledWith(
      expect.stringContaining("10"),
    );
  });

  it("validates expression format", () => {
    render(
      <ConditionBuilder
        value="invalid expression"
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    // Component should still render without errors
    expect(screen.getByLabelText(/variable/i)).toBeInTheDocument();
  });

  it("supports number type variables with numeric input", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "count", type: "number" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "count");

    const valueInput = screen.getByLabelText(/value/i) as HTMLInputElement;
    expect(valueInput.type).toBe("number");
  });

  it("supports string type variables with text input", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "status", type: "string" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "status");

    const valueInput = screen.getByLabelText(/value/i) as HTMLInputElement;
    expect(valueInput.type).toBe("text");
  });

  it("supports boolean type variables with dropdown", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value=""
        onChange={mockOnChange}
        variables={[{ name: "isActive", type: "boolean" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "isActive");

    // For boolean, value should be a select with true/false
    const valueSelect = screen.getByLabelText(/value/i);
    expect(valueSelect.tagName).toBe("SELECT");
  });

  it("pre-fills fields when value is provided", () => {
    render(
      <ConditionBuilder
        value="count > 10"
        onChange={mockOnChange}
        variables={[{ name: "count", type: "number" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(
      /variable/i,
    ) as HTMLSelectElement;
    const operatorSelect = screen.getByLabelText(
      /operator/i,
    ) as HTMLSelectElement;
    const valueInput = screen.getByLabelText(/value/i) as HTMLInputElement;

    expect(variableSelect.value).toBe("count");
    expect(operatorSelect.value).toBe(">");
    expect(valueInput.value).toBe("10");
  });

  it("clears expression when variable is cleared", async () => {
    const user = userEvent.setup();

    render(
      <ConditionBuilder
        value="count > 10"
        onChange={mockOnChange}
        variables={[{ name: "count", type: "number" }]}
      />,
    );

    const variableSelect = screen.getByLabelText(/variable/i);
    await user.selectOptions(variableSelect, "");

    expect(mockOnChange).toHaveBeenCalledWith("");
  });
});
