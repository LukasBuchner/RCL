import { render, screen, fireEvent } from "../../../test/test-utils";
import { VariableSelector } from "../VariableSelector";
import { VariableDefinition } from "../../Variables/types";

describe("VariableSelector", () => {
  const mockVariables: VariableDefinition[] = [
    {
      name: "quality_result",
      type: "String",
      source: "SKILL_OUTPUT",
      description: "Quality check result",
      scope: "PROCEDURE",
      isReadOnly: false,
    },
    {
      name: "temperature",
      type: "Number",
      source: "AGENT_STATE",
      description: "Current temperature",
      scope: "PROCEDURE",
      isReadOnly: false,
    },
  ];

  // TEST 16: Render with variables
  it("should render variable dropdown with options", () => {
    render(
      <VariableSelector
        variables={mockVariables}
        value=""
        onChange={vi.fn()}
        label="Select Variable"
      />,
    );

    expect(screen.getByLabelText("Select Variable")).toBeInTheDocument();
    expect(screen.getByRole("combobox")).toBeInTheDocument();
  });

  // TEST 17: Display variable options
  it("should display all variables in dropdown", () => {
    render(
      <VariableSelector
        variables={mockVariables}
        value=""
        onChange={vi.fn()}
      />,
    );

    const select = screen.getByRole("combobox");

    expect(select).toHaveTextContent("quality_result");
    expect(select).toHaveTextContent("temperature");
  });

  // TEST 18: Show variable metadata
  it("should show selected variable metadata", () => {
    render(
      <VariableSelector
        variables={mockVariables}
        value="quality_result"
        onChange={vi.fn()}
      />,
    );

    // Metadata is displayed in an Alert with "Type:", "Source:", "Description:" labels
    expect(screen.getByText(/Type:/)).toBeInTheDocument();
    expect(screen.getByText(/Source:/)).toBeInTheDocument();
    expect(screen.getByText(/Quality check result/i)).toBeInTheDocument();
  });

  // TEST 19: Handle selection change
  it("should call onChange when variable selected", () => {
    const handleChange = vi.fn();

    render(
      <VariableSelector
        variables={mockVariables}
        value=""
        onChange={handleChange}
      />,
    );

    const select = screen.getByRole("combobox");
    fireEvent.change(select, { target: { value: "temperature" } });

    expect(handleChange).toHaveBeenCalledWith("temperature");
  });

  // TEST 20: Empty state
  it("should show message when no variables available", () => {
    render(<VariableSelector variables={[]} value="" onChange={vi.fn()} />);

    expect(screen.getByText(/No variables defined/i)).toBeInTheDocument();
  });

  // TEST 21: Group by source
  it("should group variables by source", () => {
    render(
      <VariableSelector
        variables={mockVariables}
        value=""
        onChange={vi.fn()}
        groupBySource={true}
      />,
    );

    const select = screen.getByRole("combobox");
    expect(select.innerHTML).toContain("SKILL_OUTPUT");
    expect(select.innerHTML).toContain("AGENT_STATE");
  });
});
