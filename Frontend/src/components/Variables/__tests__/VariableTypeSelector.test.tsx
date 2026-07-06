import { render, screen } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import VariableTypeSelector from "../VariableTypeSelector";

describe("VariableTypeSelector", () => {
  const mockOnChange = vi.fn();

  beforeEach(() => {
    mockOnChange.mockClear();
  });

  it("renders with all type options", () => {
    render(<VariableTypeSelector value="String" onChange={mockOnChange} />);

    const select = screen.getByRole("combobox");
    expect(select).toBeInTheDocument();

    const options = screen.getAllByRole("option");
    expect(options).toHaveLength(6);
    expect(screen.getByRole("option", { name: /string/i })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: /number/i })).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: /boolean/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: /position$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: /positiontag/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: /sceneobject/i }),
    ).toBeInTheDocument();
  });

  it("shows currently selected type", () => {
    render(<VariableTypeSelector value="Number" onChange={mockOnChange} />);

    const select = screen.getByRole("combobox") as HTMLSelectElement;
    expect(select.value).toBe("Number");
  });

  it("calls onChange when selection changes", async () => {
    const user = userEvent.setup();

    render(<VariableTypeSelector value="String" onChange={mockOnChange} />);

    const select = screen.getByRole("combobox");
    await user.selectOptions(select, "Boolean");

    expect(mockOnChange).toHaveBeenCalledWith("Boolean");
  });

  it("is disabled when disabled prop is true", () => {
    render(
      <VariableTypeSelector
        value="String"
        onChange={mockOnChange}
        disabled={true}
      />,
    );

    const select = screen.getByRole("combobox");
    expect(select).toBeDisabled();
  });

  it("is enabled by default", () => {
    render(<VariableTypeSelector value="String" onChange={mockOnChange} />);

    const select = screen.getByRole("combobox");
    expect(select).not.toBeDisabled();
  });

  it("handles all value type changes", async () => {
    const user = userEvent.setup();

    render(<VariableTypeSelector value="String" onChange={mockOnChange} />);

    const select = screen.getByRole("combobox");

    await user.selectOptions(select, "Number");
    expect(mockOnChange).toHaveBeenCalledWith("Number");

    await user.selectOptions(select, "Boolean");
    expect(mockOnChange).toHaveBeenCalledWith("Boolean");

    await user.selectOptions(select, "Position");
    expect(mockOnChange).toHaveBeenCalledWith("Position");

    await user.selectOptions(select, "PositionTag");
    expect(mockOnChange).toHaveBeenCalledWith("PositionTag");

    await user.selectOptions(select, "SceneObject");
    expect(mockOnChange).toHaveBeenCalledWith("SceneObject");
  });
});
