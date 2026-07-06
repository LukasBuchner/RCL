import { render, screen, waitFor } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import VariableEditor from "../VariableEditor";
import { VariableDefinition } from "../types";

describe("VariableEditor", () => {
  const mockOnSave = vi.fn();
  const mockOnClose = vi.fn();

  beforeEach(() => {
    mockOnSave.mockClear();
    mockOnClose.mockClear();
    mockOnSave.mockResolvedValue(undefined);
  });

  it("renders in add mode with empty form", () => {
    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    expect(screen.getByText(/add variable/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^name/i)).toHaveValue("");
    expect(screen.getByLabelText(/description/i)).toHaveValue("");
  });

  it("renders in edit mode with pre-filled values", () => {
    const editingVariable: VariableDefinition = {
      name: "testVar",
      type: "Number",
      defaultValue: "42",
      scope: "PROCEDURE",
      source: "USER_DEFINED",
      description: "Test variable",
      isReadOnly: false,
    };

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={["testVar"]}
        editingVariable={editingVariable}
      />,
    );

    expect(screen.getByText(/edit variable/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^name/i)).toHaveValue("testVar");
    expect(screen.getByLabelText(/description/i)).toHaveValue("Test variable");
  });

  it("name field is disabled in edit mode", () => {
    const editingVariable: VariableDefinition = {
      name: "testVar",
      type: "String",
      scope: "PROCEDURE",
      source: "USER_DEFINED",
      isReadOnly: false,
    };

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={["testVar"]}
        editingVariable={editingVariable}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    expect(nameInput).toBeDisabled();
  });

  it("shows validation error for duplicate name", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={["existingVar", "anotherVar"]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "existingVar");

    // UnifiedModal uses "Create" button in add mode
    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(screen.getByText(/already exists/i)).toBeInTheDocument();
    });

    expect(mockOnSave).not.toHaveBeenCalled();
  });

  it("disables submit button when name is empty", () => {
    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const createButton = screen.getByRole("button", { name: /create/i });
    expect(createButton).toBeDisabled();
  });

  it("calls onSave with correct VariableDefinitionInput structure", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "myVariable");

    const descriptionInput = screen.getByLabelText(/description/i);
    await user.type(descriptionInput, "My test variable");

    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith({
        name: "myVariable",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: "My test variable",
        isReadOnly: false,
      });
    });
  });

  it("closes modal after successful save", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "myVariable");

    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  it("shows error message if save fails", async () => {
    const user = userEvent.setup();
    mockOnSave.mockRejectedValue(new Error("Failed to save"));

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "myVariable");

    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(screen.getByText(/failed to save/i)).toBeInTheDocument();
    });

    expect(mockOnClose).not.toHaveBeenCalled();
  });

  it("default value input changes based on selected type - string", () => {
    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const defaultValueInput = screen.getByLabelText(
      /default value/i,
    ) as HTMLInputElement;
    expect(defaultValueInput.type).toBe("text");
  });

  it("default value input changes based on selected type - number", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const typeSelect = screen.getByLabelText(/type/i);
    await user.selectOptions(typeSelect, "Number");

    const defaultValueInput = screen.getByLabelText(
      /default value/i,
    ) as HTMLInputElement;
    expect(defaultValueInput.type).toBe("number");
  });

  it("default value input changes based on selected type - boolean", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const typeSelect = screen.getByLabelText(/type/i);
    await user.selectOptions(typeSelect, "Boolean");

    const defaultValueSelect = screen.getByLabelText(/default value/i);
    expect(defaultValueSelect.tagName).toBe("SELECT");
  });

  it("does not render modal when isOpen is false", () => {
    render(
      <VariableEditor
        isOpen={false}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    expect(screen.queryByText(/add variable/i)).not.toBeInTheDocument();
  });

  it("calls onClose when cancel button clicked", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const cancelButton = screen.getByRole("button", { name: /cancel/i });
    await user.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalled();
  });

  it("includes default value in save payload when provided", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "myVariable");

    const typeSelect = screen.getByLabelText(/type/i);
    await user.selectOptions(typeSelect, "Number");

    const defaultValueInput = screen.getByLabelText(/default value/i);
    await user.type(defaultValueInput, "42");

    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(
        expect.objectContaining({
          name: "myVariable",
          type: "Number",
          defaultValue: "42",
        }),
      );
    });
  });

  it("validates variable name format", async () => {
    const user = userEvent.setup();

    render(
      <VariableEditor
        isOpen={true}
        onClose={mockOnClose}
        onSave={mockOnSave}
        existingVariableNames={[]}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/i);
    await user.type(nameInput, "invalid-name!");

    const createButton = screen.getByRole("button", { name: /create/i });
    await user.click(createButton);

    await waitFor(() => {
      expect(screen.getByText(/invalid.*name/i)).toBeInTheDocument();
    });

    expect(mockOnSave).not.toHaveBeenCalled();
  });
});
